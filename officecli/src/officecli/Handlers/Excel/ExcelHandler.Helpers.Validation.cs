// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeCli.Core;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Drawing = DocumentFormat.OpenXml.Drawing;
using X14 = DocumentFormat.OpenXml.Office2010.Excel;
using XDR = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace OfficeCli.Handlers;

public partial class ExcelHandler
{
    // A1 sqref shape: one or more space-separated single-cell or range
    // references (relative or absolute, with optional sheet-bare row/col
    // letters). Reject everything else up front so a conditional-formatting
    // rule cannot land an `INVALID!REF` literal in the sheet — Excel
    // refuses to open the file in that state.
    private static readonly System.Text.RegularExpressions.Regex SqrefShape =
        new(@"^\$?[A-Z]+\$?[0-9]+(:\$?[A-Z]+\$?[0-9]+)?(\s+\$?[A-Z]+\$?[0-9]+(:\$?[A-Z]+\$?[0-9]+)?)*$",
            System.Text.RegularExpressions.RegexOptions.Compiled
            | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    internal static string ValidateSqref(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Invalid {field} '{value}': empty A1 range.");
        if (!SqrefShape.IsMatch(value.Trim()))
            throw new ArgumentException(
                $"Invalid {field} '{value}': expected an A1 reference (e.g. 'A1', 'A1:D10', 'A1 B2:C5').");
        return value;
    }

    /// <summary>
    /// Scan a formula text for plain A1-style cell references and validate
    /// each one against Excel's row/column limits (1-1048576, A-XFD). Skips
    /// quoted strings, sheet-qualified refs (delegated to RejectCrossWorkbookFormula
    /// + sheet existence checks), function names, and structured table refs.
    /// Throws ArgumentException on the first out-of-range reference. (B15)
    /// </summary>
    /// <summary>
    /// Recognise an Excel error sentinel in a cell display value. Covers the
    /// seven standard ECMA-376 codes (#NULL!, #DIV/0!, #VALUE!, #REF!,
    /// #NAME?, #NUM!, #N/A) and modern additions (#SPILL!, #CALC!, #FIELD!,
    /// #BLOCKED!, #CONNECT!, #GETTING_DATA, #UNKNOWN!, …) via structural
    /// shape match. Used by every consumer that wants to bucket formula
    /// errors: view issues subtype routing, view stats counter, view
    /// outline warning. Centralised so the three readers cannot drift on
    /// which codes count as errors. <paramref name="value"/> is the cell's
    /// display value (cachedValue text or evaluator result); the synthetic
    /// <c>#OCLI_NOTEVAL!</c> sentinel is excluded so unevaluated formulas
    /// route to their own subtype.
    /// </summary>
    internal static bool IsExcelErrorValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value == "#OCLI_NOTEVAL!") return false;
        if (!value.StartsWith('#') || value.Length < 2) return false;
        // Every Excel error sentinel starts with `#` followed by an
        // uppercase letter. Covers the seven ECMA-376 codes, the modern
        // additions (#SPILL!, #CALC!, #FIELD!, #BLOCKED!, #CONNECT!,
        // #UNKNOWN!), and async-fetch sentinels (#GETTING_DATA) which
        // lack the trailing `!`. Intentionally lenient — there is no
        // OOXML BNF for the error namespace and Microsoft has added
        // codes over time. The trade-off is that `#FOO` would also match
        // here; the alternative (closed-set whitelist) would break the
        // moment a new error code lands.
        char c = value[1];
        return c >= 'A' && c <= 'Z';
    }

    /// <summary>
    /// Cell-aware overload. Recognises an Excel error in either the cell's
    /// display value (delegates to the string overload) or via the explicit
    /// <c>t="e"</c> data type. The DataType check covers writers that tag
    /// the cell type but leave the cached <c>&lt;v&gt;</c> in some unusual
    /// form (or empty). Shared by <c>view stats</c> and <c>view outline</c>
    /// for the <c>errorCells</c> count; <c>view issues</c> calls the same
    /// helper but additionally requires <c>cell.CellFormula != null</c>
    /// because the <c>formula_eval_error</c> subtype is, by definition,
    /// scoped to formula cells (a literal <c>#VALUE!</c> typed by a user
    /// into a non-formula cell is counted in stats/outline but is not a
    /// formula evaluation failure and has no matching subtype).
    /// </summary>
    internal static bool IsExcelErrorValue(Cell cell, string? displayValue)
    {
        if (IsExcelErrorValue(displayValue)) return true;
        return cell.DataType?.Value == CellValues.Error
            && displayValue != "#OCLI_NOTEVAL!";
    }

    internal static void ValidateFormulaCellRefs(string formula)
    {
        if (string.IsNullOrEmpty(formula)) return;
        var trimmed = formula.TrimStart('=');
        // Strip string literals first ("...") so cell-like substrings inside
        // strings don't trigger validation.
        var sb = new System.Text.StringBuilder(trimmed.Length);
        bool inStr = false;
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (c == '"')
            {
                inStr = !inStr;
                sb.Append(' ');
                continue;
            }
            sb.Append(inStr ? ' ' : c);
        }
        var stripped = sb.ToString();
        // Match A1-style refs: optional $ + 1-3 letters + optional $ + 1-8 digits.
        // (Excel's row ceiling 1048576 is 7-digit, but 8-digit numbers like
        // A10000000 must still be caught so they're rejected with the clean
        // "out-of-range" error rather than slipping through validation.)
        // Avoid matching inside an identifier (e.g. "FOO1") via a leading
        // boundary that requires either start-of-string or a non-letter.
        var rx = new System.Text.RegularExpressions.Regex(
            @"(?<![A-Za-z_])\$?([A-Za-z]{1,3})\$?([0-9]{1,8})\b");
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(stripped))
        {
            var col = m.Groups[1].Value.ToUpperInvariant();
            if (!long.TryParse(m.Groups[2].Value, out var row)) continue;
            // Column index check: ColumnNameToIndex would throw on overflow,
            // but we want a clean validation message. Compute manually.
            int colIdx = 0;
            foreach (var ch in col) colIdx = colIdx * 26 + (ch - 'A' + 1);
            if (colIdx < 1 || colIdx > 16384 || row < 1 || row > 1048576)
            {
                throw new ArgumentException(
                    $"Formula contains out-of-range cell reference '{m.Value}'. " +
                    "Excel limits: rows 1-1048576, columns A-XFD.");
            }
        }
    }

    internal static void ValidateSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Invalid sheet name: name cannot be empty or whitespace.");
        if (name.Length > 31)
            throw new ArgumentException(
                $"Invalid sheet name '{name}': length {name.Length} exceeds Excel's 31-char limit.");
        var forbidden = new[] { '\\', '/', '?', '*', ':', '[', ']' };
        var hit = name.IndexOfAny(forbidden);
        if (hit >= 0)
            throw new ArgumentException(
                $"Invalid sheet name '{name}': contains forbidden character '{name[hit]}'. Excel rejects any of: \\ / ? * : [ ]");
        if (name.StartsWith('\'') || name.EndsWith('\''))
            throw new ArgumentException(
                $"Invalid sheet name '{name}': cannot start or end with an apostrophe (').");
        if (name.Equals("History", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                "Invalid sheet name 'History': reserved by Excel for the change-history sheet.");
    }

    /// <summary>
    /// R35-3: cross-workbook cell formulas like "=[Other.xlsx]Sheet1!A1" or
    /// "=[1]Sheet1!A1" need an externalLinks part to resolve. Without one,
    /// Excel opens the file but the formula shows #REF!. Reject up-front
    /// rather than silently persist a broken formula.
    /// CONSISTENCY(cross-workbook-ref): mirrors the namedrange refersTo
    /// guard in ExcelHandler.Add.Tables.cs (R27-1).
    /// </summary>
    internal static void RejectCrossWorkbookFormula(string formula)
    {
        if (string.IsNullOrEmpty(formula)) return;
        var trimmed = formula.TrimStart('=', ' ', '\t');
        // CONSISTENCY(cross-workbook-vs-structured-ref): the older `^\[` guard
        // also matched OOXML structured table references like `[@Price]` and
        // `[Price]*[Qty]`, falsely rejecting valid Excel-365 formulas. Real
        // cross-workbook refs have one of two shapes:
        //   - numeric workbook index:  `[1]Sheet1!A1`        → `[<digits>]`
        //   - filename + extension:    `[Other.xlsx]Sheet!A1` → `[<name>.xls(x|m|b)?]`
        // Both forms are followed by a sheet reference (`Sheet!...`), but the
        // bracket payload alone is enough to disambiguate from `[@Col]` /
        // `[Col]` structured refs (which contain `@`, alphabetics without an
        // extension, or `:`).
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed,
                @"^\[(\d+|[^\]]*\.xls[xbm]?)\]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new ArgumentException(
                $"Cross-workbook references like '{formula}' require an externalLinks part which officecli doesn't expose; use raw-set for this case");
    }

    // Normalize user-supplied data-validation formula values so Excel accepts
    // them. `type=list` auto-quotes bare lists. `type=time` accepts HH:MM /
    // HH:MM:SS and converts to the Excel time serial fraction. `type=date`
    // accepts YYYY-MM-DD and converts to the Excel date serial. `type=custom`
    // strips a leading '=' since OOXML `<x:formula1>` expects the formula body
    // without one.
    internal static string NormalizeValidationFormula(string value, DataValidationValues? type)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (type == DataValidationValues.List)
        {
            // list: wrap bare "a,b,c" in quotes; leave cell/range refs and
            // already-quoted literals alone. V1: a leading `=` signals a
            // formula-ref (e.g. `=VOpts`, `=$Z$1:$Z$5`) — strip the `=`
            // (OOXML `<x:formula1>` expects the body without one) and
            // pass through without quoting.
            if (value.StartsWith("="))
                return value.Substring(1);
            if (value.StartsWith("\"") || value.Contains("!") || value.Contains(":"))
                return value;
            if (value.Contains(','))
                return $"\"{value}\"";
            return value;
        }
        if (type == DataValidationValues.Time)
        {
            var m = System.Text.RegularExpressions.Regex.Match(value.Trim(), @"^(\d{1,2}):(\d{2})(?::(\d{2}))?$");
            if (m.Success)
            {
                var h = int.Parse(m.Groups[1].Value);
                var mn = int.Parse(m.Groups[2].Value);
                var s = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
                var frac = (h * 3600 + mn * 60 + s) / 86400.0;
                return frac.ToString("0.###############", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        if (type == DataValidationValues.Date)
        {
            if (System.DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            {
                // Excel date serial: days since 1899-12-30 (accounts for the
                // 1900 leap bug baseline).
                var epoch = new System.DateTime(1899, 12, 30);
                return ((int)(dt - epoch).TotalDays).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        if (type == DataValidationValues.Custom)
        {
            if (value.StartsWith("="))
                return value.Substring(1);
        }
        return value;
    }

    // CONSISTENCY(merge-overlap): centralize the "insert one MergeCell"
    // policy. Excel rejects overlapping <mergeCell> entries with a
    // "found a problem" repair dialog, but the OOXML SDK happily
    // appends them. Mirrors the T4 overlap-throws pattern used by
    // tables and AutoFilter+table.
    // - Exact-match ref: no-op (idempotent re-Add stays consistent
    //   with prior dedup behavior).
    // - Geometric overlap with a non-identical range: throw.
    // - Otherwise: append.
    private static readonly System.Text.RegularExpressions.Regex SingleMergeRefPattern =
        new(@"^[A-Z]+[0-9]+(:[A-Z]+[0-9]+)?$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    // CONSISTENCY(merge-comma): callers should run this BEFORE creating an
    // empty <mergeCells> container, so a rejected ref doesn't leave a
    // schema-invalid empty container in the saved file.
    private static void ValidateMergeRefLiteral(string newRangeRef)
    {
        var refUpper = newRangeRef.ToUpperInvariant();
        if (refUpper.Contains(','))
            throw new ArgumentException(
                $"Invalid merge ref '{newRangeRef}': path is a single-target locator (no comma). " +
                $"Move ranges to a prop value, e.g. `set ... '/Sheet1' --prop merge={newRangeRef}`.");
        if (!SingleMergeRefPattern.IsMatch(refUpper))
            throw new ArgumentException(
                $"Invalid merge ref '{newRangeRef}': must be a single A1 cell (e.g. 'B2') or A1:B2 range (e.g. 'B4:E4').");
        // CONSISTENCY(merge-orientation): the ref must read top-left to
        // bottom-right. Z1:A1 / A10:A1 / B2:A1 (any reversed orientation)
        // were silently accepted; Excel itself only writes the canonical
        // form, so callers passing a reversed pair almost certainly typo'd.
        // Reject with a hint to swap, mirroring the orientation guard the
        // sheetShift normalizer applies after the fact (ExcelHandler.Set.cs
        // L1918) and matching how other range-bearing props (validation,
        // table, autofilter) demand canonical orientation up front.
        var colonIdx = refUpper.IndexOf(':');
        if (colonIdx > 0)
        {
            var lhs = refUpper.Substring(0, colonIdx);
            var rhs = refUpper.Substring(colonIdx + 1);
            try
            {
                var (lCol, lRow) = ParseCellReference(lhs);
                var (rCol, rRow) = ParseCellReference(rhs);
                int lColIdx = ColumnNameToIndex(lCol);
                int rColIdx = ColumnNameToIndex(rCol);
                if (lColIdx > rColIdx || lRow > rRow)
                {
                    throw new ArgumentException(
                        $"Invalid merge ref '{newRangeRef}': range must read top-left to bottom-right. " +
                        $"Pass the canonical orientation (e.g. 'A1:B2', not 'B2:A1').");
                }
            }
            catch (ArgumentException) { throw; }
            catch { /* parse failure already handled by SingleMergeRefPattern above */ }
        }
    }

    /// <summary>
    /// Scan a formula body for Sheet-qualified refs (bare `Sheet1!A1`
    /// or quoted `'My Data'!A1`) and return true if any referenced sheet
    /// name does not exist in the current workbook. Used to suppress the
    /// evaluator-based cachedValue fallback when cross-sheet refs point at
    /// a removed sheet — Real Excel shows `#REF!` there; we should not
    /// invent a "0".
    /// </summary>
    private bool FormulaReferencesMissingSheet(string formula)
    {
        if (string.IsNullOrEmpty(formula)) return false;
        var wb = _doc.WorkbookPart?.Workbook;
        if (wb == null) return false;
        var names = new HashSet<string>(
            wb.Descendants<Sheet>().Select(s => s.Name?.Value ?? "").Where(n => n.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        // Strip out double-quoted string literals first — formulas can carry
        // arbitrary text in `"..."` (e.g. `="World!"` or
        // `=INDIRECT("Foo!B2")`) and an unguarded scan would mis-flag the
        // contents as a sheet reference. OOXML escapes inner double quotes
        // by doubling (`""`), so the literal-content pattern matches either
        // `""` or any non-`"` character.
        var scan = System.Text.RegularExpressions.Regex.Replace(
            formula, @"""(""""|[^""])*""", "\"\"");

        // Quoted form: '...'! — inner single quotes escaped as ''
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(scan, @"'((?:[^']|'')+)'!"))
        {
            var name = m.Groups[1].Value.Replace("''", "'");
            if (!names.Contains(name)) return true;
        }
        // Bare form: Name! — letters/digits/underscore/period (Excel allows these unquoted)
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(scan, @"(?<![A-Za-z0-9_'.])([A-Za-z_][A-Za-z0-9_.]*)!"))
        {
            if (!names.Contains(m.Groups[1].Value)) return true;
        }
        return false;
    }

    // R13-1: Excel rejects cell values longer than 32767 chars (2^15 - 1) with
    // 0x800A03EC on save/open. Reject at write time with a clear error rather
    // than silently writing a file Excel will refuse to open.
    internal const int MaxCellTextLength = 32767;

    internal static void EnsureCellValueLength(string? value, string? cellRef = null)
    {
        if (value == null) return;
        if (value.Length > MaxCellTextLength)
        {
            var where = string.IsNullOrEmpty(cellRef) ? "" : $" at {cellRef}";
            throw new ArgumentException(
                $"Cell value{where} exceeds Excel's {MaxCellTextLength}-character limit (got {value.Length})");
        }
    }
}
