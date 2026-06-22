// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeCli.Core;

namespace OfficeCli.Handlers;

public partial class ExcelHandler
{
    // ==================== Column-predicate row matching ====================
    //
    // `row[Salary>5000]` matches the DATA rows of a table by a column's cell
    // value, addressing the column by its header NAME (or column letter). This
    // is the human/agent-natural "where" form for a normal table — you think in
    // column names, not B/E. It reuses the shared AttributeFilter operator
    // engine for comparison and the ListObject column metadata (names + range +
    // header/totals flags) already surfaced by TableToNode, so no header
    // sniffing or new comparison logic is introduced. P1 scope: real
    // ListObjects only; auto-binds to the single table that owns the
    // referenced column(s). Returns row nodes (`/Sheet/row[N]`) so the result
    // feeds Set/Remove for "match rows then operate".

    // Bracket keys that filter row PROPERTIES (height/hidden/...). Any other key
    // in a row selector is treated as a table COLUMN reference.
    private static readonly HashSet<string> RowAttributeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "height", "hidden", "outlineLevel", "collapsed", "customHeight",
    };

    // Strip a `col.` / `column.` namespace prefix from a predicate key. The
    // prefix forces COLUMN interpretation at parse time; the resolver works on
    // the bare name. Case-insensitive. Returns the key unchanged when absent.
    private static string StripColPrefix(string key)
    {
        var m = Regex.Match(key, @"^col(?:umn)?\.(.+)$", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : key;
    }


    // Match table data rows whose cells satisfy every column predicate. Auto-
    // binds to the single ListObject that owns all referenced columns; throws on
    // no-match or cross-table ambiguity rather than silently picking one.
    private List<DocumentNode> QueryRowsByColumnPredicate(string? sheetFilter, AttributeFilter.FilterExpr expr)
    {
        // Distinct leaf predicates drive column resolution and probe building; the
        // expression tree (which may be `or` / parens, e.g. row[金额>5 or 金额<1])
        // drives the actual row match via MatchesExpr.
        var colConds = AttributeFilter.LeafConditions(expr).ToList();
        // A table that owns every referenced column. ListObjects are
        // authoritative (column names are stored metadata); detected tables are
        // a header-sniff heuristic (stable=false), so they are only consulted
        // when no ListObject matches — a real table never loses to a guess.
        var listObjCands = new List<(string sheetName, WorksheetPart part, string label, string source,
            int dataR1, int dataR2, Dictionary<string, int> colAbsIndex)>();
        var detectedCands = new List<(string sheetName, WorksheetPart part, string label, string source,
            int dataR1, int dataR2, Dictionary<string, int> colAbsIndex)>();

        foreach (var (sheetName, worksheetPart) in GetWorksheets())
        {
            if (sheetFilter != null && !sheetName.Equals(sheetFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            // ListObjects (authoritative column names).
            var realRanges = new List<(int c1, int r1, int c2, int r2)>();
            foreach (var tdp in worksheetPart.TableDefinitionParts)
            {
                var tbl = tdp.Table;
                if (tbl?.Reference?.Value == null) continue;
                if (!TryParseRange(tbl.Reference.Value, out var rng)) continue;
                realRanges.Add(rng);

                var colNames = tbl.GetFirstChild<TableColumns>()?.Elements<TableColumn>()
                    .Select(c => c.Name?.Value ?? "").ToList() ?? new List<string>();
                var resolved = ResolveColumns(colNames, rng.c1, rng.c2, colConds);
                if (resolved == null) continue;

                bool headerRow = (tbl.HeaderRowCount?.Value ?? 1) != 0;
                bool totalRow = (tbl.TotalsRowCount?.Value ?? 0) > 0 || (tbl.TotalsRowShown?.Value ?? false);
                listObjCands.Add((sheetName, worksheetPart, tbl.Name?.Value ?? "table", "table",
                    rng.r1 + (headerRow ? 1 : 0), rng.r2 - (totalRow ? 1 : 0), resolved));
            }

            // Detected tables (header-sniff). Header is the first row of ref, no
            // totals row; data rows are ref minus the header row.
            foreach (var det in DetectTables(sheetName, worksheetPart, realRanges))
            {
                var colNames = (det.Format.TryGetValue("columns", out var cv) ? cv?.ToString() ?? "" : "")
                    .Split(',').ToList();
                var refStr = det.Format.TryGetValue("ref", out var rv) ? rv?.ToString() : null;
                if (!TryParseRange(refStr, out var frng)) continue;
                var resolved = ResolveColumns(colNames, frng.c1, frng.c2, colConds);
                if (resolved == null) continue;
                detectedCands.Add((sheetName, worksheetPart, refStr!, "detected",
                    frng.r1 + 1, frng.r2, resolved));
            }
        }

        var candidates = listObjCands.Count > 0 ? listObjCands : detectedCands;

        if (candidates.Count == 0)
        {
            var cols = string.Join(", ", colConds.Select(c => $"'{StripColPrefix(c.Key)}'"));
            var scope = sheetFilter == null ? "any sheet" : $"sheet '{sheetFilter}'";
            throw new ArgumentException(
                $"row[col op val] found no table on {scope} with column(s) {cols}. " +
                "Column predicates resolve header names (or column letters) against a ListObject or a detected (header-row) table.");
        }
        if (candidates.Count > 1)
        {
            var where = string.Join(", ", candidates.Select(c => $"{c.sheetName}!{c.label}"));
            throw new ArgumentException(
                $"row[col op val] is ambiguous — column(s) exist in {candidates.Count} tables ({where}). " +
                "Scope by sheet, e.g. /SheetName/row[...].");
        }

        var cand = candidates[0];
        var results = new List<DocumentNode>();
        var sheetData = GetSheet(cand.part).GetFirstChild<SheetData>();
        if (sheetData == null) return results;
        var eval = new Core.FormulaEvaluator(sheetData, _doc.WorkbookPart);

        for (int r = cand.dataR1; r <= cand.dataR2; r++)
        {
            // Probe node carries each predicate column's cell value under its
            // key so AttributeFilter evaluates all operators with one engine.
            var probe = new DocumentNode { Type = "cell" };
            foreach (var cond in colConds)
            {
                var cell = FindCell(sheetData, $"{IndexToColumnName(cand.colAbsIndex[cond.Key])}{r}");
                probe.Format[cond.Key] = cell != null ? GetCellDisplayValue(cell, eval) : "";
            }
            if (!AttributeFilter.MatchesExpr(probe, expr)) continue;

            var rowNode = new DocumentNode
            {
                Path = $"/{cand.sheetName}/row[{r}]",
                Type = "row",
                Preview = r.ToString(),
            };
            // Carry each predicate column's value under its column key so the
            // row is self-describing AND the CLI post-filter (which re-applies
            // the same [col op val] conditions on top of these results) resolves
            // the key and re-confirms the match, instead of dropping every row
            // because "Salary" is absent from a plain row node's Format.
            foreach (var cond in colConds)
                rowNode.Format[cond.Key] = probe.Format[cond.Key];
            // Trace which table bound the predicate. source=detected flags a
            // header-sniff (stable=false) match so the caller knows the column
            // resolution was heuristic, mirroring DetectTables' own stable flag.
            rowNode.Format["matchedTable"] = cand.label;
            if (cand.source == "detected") rowNode.Format["tableSource"] = "detected";
            rowNode.ChildCount = sheetData.Elements<Row>()
                .FirstOrDefault(rw => rw.RowIndex?.Value == (uint)r)?.Elements<Cell>().Count() ?? 0;
            results.Add(rowNode);
        }
        return results;
    }

    // Resolve every column predicate against a table's columns. Returns a
    // key→absolute-column-index map, or null if any predicate column is not in
    // this table (so the table is not a candidate).
    private static Dictionary<string, int>? ResolveColumns(
        List<string> colNames, int c1, int c2, List<AttributeFilter.Condition> colConds)
    {
        var resolved = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cond in colConds)
        {
            if (!TryResolveTableColumn(cond.Key, colNames, c1, c2, out var absCol)) return null;
            resolved[cond.Key] = absCol;
        }
        return resolved;
    }

    // Resolve a predicate key to an ABSOLUTE column index within a table's
    // column span [c1..c2]. Header name (case-insensitive) wins over a column
    // letter so a header literally named "B" stays reachable by name.
    private static bool TryResolveTableColumn(string key, List<string> colNames, int c1, int c2, out int absCol)
    {
        absCol = 0;
        key = StripColPrefix(key);   // `col.Salary` → resolve against `Salary`
        var nameIdx = colNames.FindIndex(n => n.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (nameIdx >= 0) { absCol = c1 + nameIdx; return true; }
        if (Regex.IsMatch(key, @"^[A-Za-z]{1,3}$"))
        {
            var letterIdx = ColumnNameToIndex(key.ToUpperInvariant());
            if (letterIdx >= c1 && letterIdx <= c2) { absCol = letterIdx; return true; }
        }
        return false;
    }

    // True when an in-scope table (ListObject or detected) has a column whose
    // name equals `key`. Used to flag a bare `row[key op val]` whose key also
    // names a row PROPERTY (height/hidden/...): rather than silently choosing the
    // property and shadowing the column, the caller errors and points to the
    // `col.key` / `@key` escapes. Only ever called for the ≤5 attribute names, so
    // the table scan is negligible.
    private bool RowKeyCollidesWithColumn(string key, string? sheetFilter)
    {
        foreach (var (sheetName, worksheetPart) in GetWorksheets())
        {
            if (sheetFilter != null && !sheetName.Equals(sheetFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var realRanges = new List<(int c1, int r1, int c2, int r2)>();
            foreach (var tdp in worksheetPart.TableDefinitionParts)
            {
                var tbl = tdp.Table;
                if (tbl?.Reference?.Value == null) continue;
                if (TryParseRange(tbl.Reference.Value, out var rng)) realRanges.Add(rng);
                var colNames = tbl.GetFirstChild<TableColumns>()?.Elements<TableColumn>()
                    .Select(c => c.Name?.Value ?? "") ?? Enumerable.Empty<string>();
                if (colNames.Any(n => n.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            foreach (var det in DetectTables(sheetName, worksheetPart, realRanges))
            {
                var colNames = (det.Format.TryGetValue("columns", out var cv) ? cv?.ToString() ?? "" : "")
                    .Split(',');
                if (colNames.Any(n => n.Trim().Equals(key, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
        }
        return false;
    }
}
