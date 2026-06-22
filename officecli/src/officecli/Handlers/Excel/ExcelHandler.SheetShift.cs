// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0
//
// Sheet-wide range-mutation walker. Used by every operation that needs to
// keep range-bearing OOXML structures in sync after a row/column insert,
// delete, move, or copy: cellRef-shift in the SheetData (still done by the
// caller because it requires direction-specific reverse iteration), then
// every sheet-level structure that anchors on an A1 ref/sqref/range:
//
//   - mergeCells
//   - conditionalFormatting (sqref list + rule <formula> text)
//   - dataValidations (sqref list + formula1/formula2 text)
//   - autoFilter (single ref)
//   - hyperlinks (per-cell anchor)
//   - table ref + autoFilter ref + calc-column/totals-row formula text
//     (in TableDefinitionPart)
//   - cell formulas (CellFormula.Text and the shared/array CellFormula.Reference)
//   - workbook-level definedNames text (for refs that target this sheet)
//
// The caller supplies axis-specific mappers; the walker handles the
// per-section iteration, the "drop entry when mapper returns null"
// semantics, the "drop container when last entry vanishes" cascade, and
// the per-part Save() bookkeeping (TableDefinitionPart.Save / Workbook.Save).
//
// Out of scope for this walker (intentionally):
//   - <Columns> width/style metadata (column-only, op-asymmetric — handled
//     directly by the column-shift callers).
//   - SheetData cell/row renumbering (axis-direction-specific reverse
//     iteration — handled directly by callers).
//   - CalcChain invalidation (workbook-level concern handled by callers).

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using X14 = DocumentFormat.OpenXml.Office2010.Excel;
using Xm = DocumentFormat.OpenXml.Office.Excel;
using ThreadedCmt = DocumentFormat.OpenXml.Office2019.Excel.ThreadedComments;

namespace OfficeCli.Handlers;

public partial class ExcelHandler
{
    /// <summary>
    /// Apply a per-axis ref/formula rewrite across every range-bearing
    /// structure on a sheet. The per-section semantics (drop entry on null,
    /// drop container when empty, save part) are handled internally so the
    /// caller only supplies the axis-specific mappers.
    /// </summary>
    /// <param name="worksheet">The worksheet part being mutated.</param>
    /// <param name="sheetName">Sheet name; threaded to FormulaRefShifter for
    /// the sheet-scope guard (refs targeting other sheets are left alone).</param>
    /// <param name="refMapper">Per-range rewrite. Returns the new ref string,
    /// or null to drop the entry. Used for mergeCells, sqref lists,
    /// autoFilter, hyperlinks, table refs, and the shared/array formula
    /// <c>ref</c> attribute.</param>
    /// <param name="formulaTextMapper">Per-formula-text rewrite (used for
    /// CellFormula.Text and DefinedName text). Pass null to skip formula
    /// and named-range rewriting (rare — only ops that don't touch
    /// formula content).</param>
    private void ApplySheetRangeMutations(
        WorksheetPart worksheet,
        string sheetName,
        Func<string, string?> refMapper,
        Func<string, string>? formulaTextMapper,
        Func<int, int>? rowMarkerShift = null,
        Func<int, int>? colMarkerShift = null)
    {
        var ws = GetSheet(worksheet);

        // 1. mergeCells
        var mergeCells = ws.GetFirstChild<MergeCells>();
        if (mergeCells != null)
        {
            foreach (var mc in mergeCells.Elements<MergeCell>().ToList())
            {
                if (mc.Reference?.Value == null) continue;
                var shifted = refMapper(mc.Reference.Value);
                if (shifted == null) mc.Remove();
                else mc.Reference = shifted;
            }
            if (!mergeCells.HasChildren) mergeCells.Remove();
        }

        // 2. conditionalFormatting sqref + rule formulas
        foreach (var cf in ws.Elements<ConditionalFormatting>().ToList())
        {
            // cellIs/formula rules carry cell refs inside <formula> (e.g. $C2>5)
            // that must follow the displacement, same as cell formulas.
            // databar/colorScale/iconSet value objects can also carry a formula
            // in their `val` attribute (<cfvo type="formula" val="A10">); shift
            // those too. Other cfvo types (num/percent/min/max/...) are not refs
            // and pass through untouched.
            if (formulaTextMapper != null)
                foreach (var rule in cf.Elements<ConditionalFormattingRule>())
                {
                    foreach (var f in rule.Elements<Formula>())
                        if (!string.IsNullOrEmpty(f.Text)) f.Text = formulaTextMapper(f.Text);
                    foreach (var cfvo in rule.Descendants<ConditionalFormatValueObject>())
                        if (cfvo.Type?.Value == ConditionalFormatValueObjectValues.Formula
                            && !string.IsNullOrEmpty(cfvo.Val?.Value))
                            cfvo.Val = formulaTextMapper(cfvo.Val!.Value!);
                }
            if (cf.SequenceOfReferences?.HasValue != true) continue;
            var newRefs = cf.SequenceOfReferences.Items
                .Where(r => r.Value != null)
                .Select(r => refMapper(r.Value!))
                .OfType<string>().ToList();
            if (newRefs.Count == 0) cf.Remove();
            else cf.SequenceOfReferences = new ListValue<StringValue>(newRefs.Select(r => new StringValue(r)));
        }

        // 3. dataValidations sqref
        var dvs = ws.GetFirstChild<DataValidations>();
        if (dvs != null)
        {
            foreach (var dv in dvs.Elements<DataValidation>().ToList())
            {
                // Relative refs inside the validation formula (e.g. INDIRECT(B2))
                // must follow the displacement too. Literal lists ("Yes,No") carry
                // no refs and pass through the shifter unchanged.
                if (formulaTextMapper != null)
                {
                    if (!string.IsNullOrEmpty(dv.Formula1?.Text)) dv.Formula1.Text = formulaTextMapper(dv.Formula1.Text);
                    if (!string.IsNullOrEmpty(dv.Formula2?.Text)) dv.Formula2.Text = formulaTextMapper(dv.Formula2.Text);
                }
                if (dv.SequenceOfReferences?.HasValue != true) continue;
                var newRefs = dv.SequenceOfReferences.Items
                    .Where(r => r.Value != null)
                    .Select(r => refMapper(r.Value!))
                    .OfType<string>().ToList();
                if (newRefs.Count == 0) dv.Remove();
                else dv.SequenceOfReferences = new ListValue<StringValue>(newRefs.Select(r => new StringValue(r)));
            }
            if (!dvs.HasChildren) dvs.Remove();
        }

        // 4. autoFilter
        var af = ws.GetFirstChild<AutoFilter>();
        if (af?.Reference?.Value != null)
        {
            var shifted = refMapper(af.Reference.Value);
            if (shifted != null) af.Reference = shifted;
            else af.Remove();
        }

        // 5. hyperlinks (per-cell anchor)
        var hyperlinks = ws.GetFirstChild<Hyperlinks>();
        if (hyperlinks != null)
        {
            foreach (var hl in hyperlinks.Elements<Hyperlink>().ToList())
            {
                if (hl.Reference?.Value == null) continue;
                var shifted = refMapper(hl.Reference.Value);
                if (shifted == null) hl.Remove();
                else hl.Reference = shifted;
            }
            if (!hyperlinks.HasChildren) hyperlinks.Remove();
        }

        // 6. tables (separate part, must be saved if mutated)
        foreach (var tablePart in worksheet.TableDefinitionParts)
        {
            var tbl = tablePart.Table;
            if (tbl == null) continue;
            bool tblDirty = false;
            if (tbl.Reference?.Value != null)
            {
                var shifted = refMapper(tbl.Reference.Value);
                if (shifted != null && !string.Equals(shifted, tbl.Reference.Value, StringComparison.Ordinal))
                {
                    tbl.Reference = shifted;
                    tblDirty = true;
                }
            }
            if (tbl.AutoFilter?.Reference?.Value != null)
            {
                var shifted = refMapper(tbl.AutoFilter.Reference.Value);
                if (shifted != null && !string.Equals(shifted, tbl.AutoFilter.Reference.Value, StringComparison.Ordinal))
                {
                    tbl.AutoFilter.Reference = shifted;
                    tblDirty = true;
                }
            }
            // Calculated-column and totals-row formulas carry cell refs (e.g.
            // SUM(B3:B5)) that must follow the displacement. Structured refs
            // (Table1[Col]) are name-based and pass through the shifter unchanged.
            if (formulaTextMapper != null && tbl.TableColumns != null)
            {
                foreach (var tc in tbl.TableColumns.Elements<TableColumn>())
                {
                    if (!string.IsNullOrEmpty(tc.CalculatedColumnFormula?.Text))
                    {
                        tc.CalculatedColumnFormula.Text = formulaTextMapper(tc.CalculatedColumnFormula.Text);
                        tblDirty = true;
                    }
                    if (!string.IsNullOrEmpty(tc.TotalsRowFormula?.Text))
                    {
                        tc.TotalsRowFormula.Text = formulaTextMapper(tc.TotalsRowFormula.Text);
                        tblDirty = true;
                    }
                }
            }
            if (tblDirty) tbl.Save();
        }

        // 6b. drawing anchors (separate DrawingsPart). Pictures / shapes / charts
        // anchor via twoCell/oneCell from+to markers whose <xdr:row>/<xdr:col>
        // are 0-based indices — shift them so the object moves and resizes with
        // the cells, the way Excel treats "move and size with cells" objects.
        if (rowMarkerShift != null || colMarkerShift != null)
        {
            var wsDr = worksheet.DrawingsPart?.WorksheetDrawing;
            if (wsDr != null)
            {
                bool drDirty = false;
                var markers = wsDr.Descendants<Xdr.FromMarker>().Cast<OpenXmlCompositeElement>()
                    .Concat(wsDr.Descendants<Xdr.ToMarker>());
                foreach (var m in markers)
                {
                    if (rowMarkerShift != null)
                    {
                        var rid = m.GetFirstChild<Xdr.RowId>();
                        if (rid != null && int.TryParse(rid.Text, out var r))
                        {
                            var nr = rowMarkerShift(r);
                            if (nr != r) { rid.Text = nr.ToString(); drDirty = true; }
                        }
                    }
                    if (colMarkerShift != null)
                    {
                        var cid = m.GetFirstChild<Xdr.ColumnId>();
                        if (cid != null && int.TryParse(cid.Text, out var c))
                        {
                            var nc = colMarkerShift(c);
                            if (nc != c) { cid.Text = nc.ToString(); drDirty = true; }
                        }
                    }
                }
                if (drDirty) wsDr.Save();
            }
        }

        // 6c. chart series references (<c:f> in each ChartPart under DrawingsPart),
        // e.g. Sheet1!$B$1:$B$5. Route through formulaTextMapper so refs targeting
        // this sheet follow the displacement and refs to other sheets are left
        // alone (the shifter's sheet-scope guard handles that).
        if (formulaTextMapper != null && worksheet.DrawingsPart != null)
        {
            foreach (var chartPart in worksheet.DrawingsPart.ChartParts)
            {
                var cs = chartPart.ChartSpace;
                if (cs == null) continue;
                bool chDirty = false;
                foreach (var f in cs.Descendants<C.Formula>())
                {
                    if (string.IsNullOrEmpty(f.Text)) continue;
                    var nf = formulaTextMapper(f.Text);
                    if (!string.Equals(nf, f.Text, StringComparison.Ordinal)) { f.Text = nf; chDirty = true; }
                }
                if (chDirty) cs.Save();
            }
        }

        // 6d. comments (separate WorksheetCommentsPart). The comment's cell anchor
        // must follow the displacement; a comment whose own cell is deleted is
        // dropped (refMapper returns null for a single cell on the deleted line).
        var commentsPart = worksheet.WorksheetCommentsPart;
        var cmtList = commentsPart?.Comments?.GetFirstChild<CommentList>();
        if (cmtList != null)
        {
            bool cmtDirty = false;
            foreach (var cmt in cmtList.Elements<Comment>().ToList())
            {
                if (cmt.Reference?.Value == null) continue;
                var shifted = refMapper(cmt.Reference.Value);
                if (shifted == null) { cmt.Remove(); cmtDirty = true; }
                else if (!string.Equals(shifted, cmt.Reference.Value, StringComparison.Ordinal))
                {
                    cmt.Reference = shifted;
                    cmtDirty = true;
                }
            }
            if (cmtDirty) commentsPart!.Comments!.Save();
        }

        // 6e. sparklines (x14 extension list on the worksheet). Each sparkline
        // carries a data range (<xne:f>, sheet-qualified) and a location
        // (<xne:sqref>, the host cell). Shift the formula via formulaTextMapper
        // and the location via refMapper; drop a sparkline whose host cell is
        // deleted (refMapper returns null).
        foreach (var spk in ws.Descendants<X14.Sparkline>().ToList())
        {
            if (formulaTextMapper != null && !string.IsNullOrEmpty(spk.Formula?.Text))
                spk.Formula.Text = formulaTextMapper(spk.Formula.Text);
            if (!string.IsNullOrEmpty(spk.ReferenceSequence?.Text))
            {
                var shifted = refMapper(spk.ReferenceSequence.Text);
                if (shifted == null) spk.Remove();
                else spk.ReferenceSequence.Text = shifted;
            }
        }

        // 6f. sortState (worksheet DOM): the sorted range and each sort-key
        // column range follow the displacement; the whole state (or a single
        // condition) is dropped when its range collapses onto the deleted line.
        var sortState = ws.GetFirstChild<SortState>();
        if (sortState?.Reference?.Value != null)
        {
            var newRef = refMapper(sortState.Reference.Value);
            if (newRef == null) sortState.Remove();
            else
            {
                if (!string.Equals(newRef, sortState.Reference.Value, StringComparison.Ordinal))
                    sortState.Reference = newRef;
                foreach (var sc in sortState.Elements<SortCondition>().ToList())
                {
                    if (sc.Reference?.Value == null) continue;
                    var scRef = refMapper(sc.Reference.Value);
                    if (scRef == null) sc.Remove();
                    else if (!string.Equals(scRef, sc.Reference.Value, StringComparison.Ordinal))
                        sc.Reference = scRef;
                }
            }
        }

        // 6g. sheetView selection (cosmetic: the saved active cell + selected
        // ranges). Shift so the cursor lands on the same logical cells; entries
        // collapsing onto the deleted line are left as-is (Excel re-derives).
        var sheetViews = ws.GetFirstChild<SheetViews>();
        if (sheetViews != null)
        {
            foreach (var sv in sheetViews.Elements<SheetView>())
            {
                foreach (var sel in sv.Elements<Selection>())
                {
                    if (sel.ActiveCell?.Value != null)
                    {
                        var ac = refMapper(sel.ActiveCell.Value);
                        if (ac != null && !string.Equals(ac, sel.ActiveCell.Value, StringComparison.Ordinal))
                            sel.ActiveCell = ac;
                    }
                    if (sel.SequenceOfReferences?.HasValue == true)
                    {
                        var newRefs = sel.SequenceOfReferences.Items
                            .Where(r => r.Value != null)
                            .Select(r => refMapper(r.Value!))
                            .OfType<string>().ToList();
                        if (newRefs.Count > 0)
                            sel.SequenceOfReferences = new ListValue<StringValue>(newRefs.Select(r => new StringValue(r)));
                    }
                }
            }
        }

        // 6h. x14 conditional formatting (databar/colorScale/iconSet 2010
        // extension in the worksheet extLst): its own selection (<xm:sqref>) and
        // any formula-type value object ref (<xm:f>) must follow the displacement
        // — the classic <conditionalFormatting> twin is handled in section 2, but
        // this extension carries a separate sqref that would otherwise drift.
        foreach (var x14cf in ws.Descendants<X14.ConditionalFormatting>().ToList())
        {
            var rs = x14cf.GetFirstChild<Xm.ReferenceSequence>();
            if (rs != null && !string.IsNullOrEmpty(rs.Text))
            {
                var mapped = rs.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => refMapper(p)).OfType<string>().ToList();
                if (mapped.Count == 0) { x14cf.Remove(); continue; }
                rs.Text = string.Join(" ", mapped);
            }
            if (formulaTextMapper != null)
                foreach (var cfvo in x14cf.Descendants<X14.ConditionalFormattingValueObject>())
                    if (cfvo.Type?.Value == X14.ConditionalFormattingValueObjectTypeValues.Formula)
                    {
                        var f = cfvo.GetFirstChild<Xm.Formula>();
                        if (f != null && !string.IsNullOrEmpty(f.Text)) f.Text = formulaTextMapper(f.Text);
                    }
        }

        // 6i. pivot tables. The cache source range lives in a workbook-level
        // PivotTableCacheDefinitionPart (shift only when its worksheetSource
        // targets this sheet); the pivot's render location lives on the hosting
        // worksheet. Both must follow the displacement so a refresh reads the
        // right data and the table re-lays out in the right place.
        foreach (var cacheDefPart in _doc.WorkbookPart!.GetPartsOfType<PivotTableCacheDefinitionPart>())
        {
            var wsSource = cacheDefPart.PivotCacheDefinition?.CacheSource?.WorksheetSource;
            if (wsSource?.Reference?.Value == null) continue;
            if (!string.Equals(wsSource.Sheet?.Value, sheetName, StringComparison.Ordinal)) continue;
            var newRef = refMapper(wsSource.Reference.Value);
            if (newRef != null && !string.Equals(newRef, wsSource.Reference.Value, StringComparison.Ordinal))
            {
                wsSource.Reference = newRef;
                cacheDefPart.PivotCacheDefinition!.Save();
            }
        }
        foreach (var pivotPart in worksheet.GetPartsOfType<PivotTablePart>())
        {
            var loc = pivotPart.PivotTableDefinition?.Location;
            if (loc?.Reference?.Value == null) continue;
            var newRef = refMapper(loc.Reference.Value);
            if (newRef != null && !string.Equals(newRef, loc.Reference.Value, StringComparison.Ordinal))
            {
                loc.Reference = newRef;
                pivotPart.PivotTableDefinition!.Save();
            }
        }

        // 6j. threaded comments (Excel 365, separate WorksheetThreadedCommentsPart).
        // Same storage model as legacy comments — per-cell <threadedComment ref="...">
        // entries — and 6d already shifts the legacy shadow copies, so skipping the
        // threaded part would leave the two anchors disagreeing after an insert or
        // delete. Replies carry the same ref as their root, so dropping every entry
        // whose cell is deleted removes the whole thread.
        foreach (var threadedPart in worksheet.WorksheetThreadedCommentsParts)
        {
            if (threadedPart?.ThreadedComments == null) continue;
            bool tcDirty = false;
            foreach (var tc in threadedPart.ThreadedComments.Elements<ThreadedCmt.ThreadedComment>().ToList())
            {
                if (tc.Ref?.Value == null) continue;
                var shifted = refMapper(tc.Ref.Value);
                if (shifted == null) { tc.Remove(); tcDirty = true; }
                else if (!string.Equals(shifted, tc.Ref.Value, StringComparison.Ordinal))
                {
                    tc.Ref = shifted;
                    tcDirty = true;
                }
            }
            if (tcDirty) threadedPart.ThreadedComments.Save();
        }

        // 7. cell formulas (text + shared/array ref attribute)
        var sheetData = ws.GetFirstChild<SheetData>();
        if (sheetData != null)
        {
            foreach (var row in sheetData.Elements<Row>())
            {
                foreach (var cell in row.Elements<Cell>())
                {
                    if (cell.CellFormula == null) continue;
                    if (formulaTextMapper != null && !string.IsNullOrEmpty(cell.CellFormula.Text))
                        cell.CellFormula.Text = formulaTextMapper(cell.CellFormula.Text);
                    if (cell.CellFormula.Reference?.Value != null)
                    {
                        var shifted = refMapper(cell.CellFormula.Reference.Value);
                        if (shifted != null) cell.CellFormula.Reference = shifted;
                        else cell.CellFormula.Remove();
                    }
                }
            }
        }

        // 8. workbook-level definedNames whose text references this sheet.
        // Routed through formulaTextMapper (typically a FormulaRefShifter.*
        // call) so the sheet-scope guard inside the shifter handles "leave
        // refs to other sheets alone".
        if (formulaTextMapper != null)
        {
            var definedNames = GetWorkbook().GetFirstChild<DefinedNames>();
            if (definedNames != null)
            {
                bool changed = false;
                foreach (var dn in definedNames.Elements<DefinedName>())
                {
                    if (dn.Text == null) continue;
                    var newText = formulaTextMapper(dn.Text);
                    if (!string.Equals(newText, dn.Text, StringComparison.Ordinal))
                    {
                        dn.Text = newText;
                        changed = true;
                    }
                }
                if (changed) GetWorkbook().Save();
            }
        }
    }
}
