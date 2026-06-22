// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using OfficeCli.Core.TableStyles;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{
    // ==================== Table Rendering ====================

    private static void RenderTable(StringBuilder sb, GraphicFrame gf, Dictionary<string, string> themeColors, string? dataPath = null,
        (long x, long y, long cx, long cy)? overridePos = null)
    {
        var dataPathAttr = string.IsNullOrEmpty(dataPath) ? "" : $" data-path=\"{HtmlEncode(dataPath)}\"";
        var table = gf.Descendants<Drawing.Table>().FirstOrDefault();
        if (table == null) return;

        var offset = gf.Transform?.Offset;
        var extents = gf.Transform?.Extents;
        if (offset == null || extents == null) return;

        // R14-2: when nested in a group, the caller re-projects position/size into
        // the group's child coordinate system (CalcGroupChildPos).
        var x = overridePos?.x ?? offset.X?.Value ?? 0;
        var y = overridePos?.y ?? offset.Y?.Value ?? 0;
        var cx = overridePos?.cx ?? extents.Cx?.Value ?? 0;
        var cy = overridePos?.cy ?? extents.Cy?.Value ?? 0;

        // PowerPoint stores the graphicFrame's declared layout height in <p:xfrm>,
        // but tables auto-grow vertically to fit explicit row heights — declared cy
        // can underreport actual rendered height. With overflow:hidden on the
        // container, this clips trailing rows (slide 6 of test-samples/07.pptx
        // declared 72pt for a 5×30.2pt = 151pt table). Honor the larger of the
        // two so all rows render.
        var rowHeightSum = table.Elements<Drawing.TableRow>().Sum(r => r.Height?.Value ?? 0);
        if (rowHeightSum > cy) cy = rowHeightSum;

        // Same idea on the horizontal axis. <a:gridCol w="…"> stores absolute
        // EMU per column; PowerPoint renders the table at Σ gridCol.w and lets
        // it overflow the graphicFrame's <p:ext cx> (e.g. after `add column`
        // appends a new col, cx is unchanged while the grid grows). The frame
        // does not clip — the slide canvas does. Use Σ gridCol.w as the true
        // table width; fall back to cx only when <a:tblGrid> is absent.
        var gridCols = table.TableGrid?.Elements<Drawing.GridColumn>().ToList();
        long gridWidthSum = gridCols?.Sum(gc => gc.Width?.Value ?? 0) ?? 0;
        var tableWidthEmu = gridWidthSum > 0 ? gridWidthSum : cx;

        // Detect table style + banding flags. All cell-level styling
        // (fill, text color, borders) is now resolved through
        // Core/TableStyles/TableStyleResolver — no local catalogue lives in
        // this file. Unknown style ids resolve to null and the cell falls
        // back to "no fill / no border" (correct for un-styled tables).
        var tblPr = table.GetFirstChild<Drawing.TableProperties>();
        var tableStyleId = tblPr?.GetFirstChild<Drawing.TableStyleId>()?.InnerText;
        bool hasFirstRow = tblPr?.FirstRow?.Value == true;
        bool hasBandRow = tblPr?.BandRow?.Value == true;
        bool hasLastRow = tblPr?.LastRow?.Value == true;
        bool hasFirstCol = tblPr?.FirstColumn?.Value == true;
        bool hasLastCol = tblPr?.LastColumn?.Value == true;
        bool hasBandCol = tblPr?.BandColumn?.Value == true;
        int totalRows = table.Elements<Drawing.TableRow>().Count();
        int totalCols = gridCols?.Count ?? 0;

        sb.AppendLine($"    <div class=\"table-container\"{dataPathAttr} style=\"left:{Units.EmuToPt(x)}pt;top:{Units.EmuToPt(y)}pt;width:{Units.EmuToPt(tableWidthEmu)}pt;height:{Units.EmuToPt(cy)}pt\">");
        sb.AppendLine("      <table class=\"slide-table\">");

        // Column widths — emit absolute pt per <a:gridCol w>, not percentages.
        // table-layout:fixed + width:100% on .slide-table then preserves these
        // widths (container width == Σ gridCol.w so they add up exactly).
        if (gridCols != null && gridCols.Count > 0)
        {
            sb.Append("        <colgroup>");
            foreach (var gc in gridCols)
            {
                var w = gc.Width?.Value ?? 0;
                if (w > 0)
                    sb.Append($"<col style=\"width:{Units.EmuToPt(w):0.##}pt\">");
                else
                    sb.Append($"<col style=\"width:{(100.0 / gridCols.Count):0.##}%\">");
            }
            sb.AppendLine("</colgroup>");
        }

        int rowIndex = 0;
        foreach (var row in table.Elements<Drawing.TableRow>())
        {
            // Honor explicit per-row height from <a:tr h="EMU">. Without this,
            // every row collapses to equal height (HTML table default), losing
            // the per-row sizing users set via `set tr[N] --prop height=`.
            var rowH = row.Height?.Value ?? 0;
            var rowStyle = rowH > 0 ? $" style=\"height:{Units.EmuToPt(rowH):0.##}pt\"" : "";
            sb.AppendLine($"        <tr{rowStyle}>");
            int skipCols = 0;
            int colIndex = 0;  // Tracked for the new per-cell TableStyleResolver below.
            bool isHeaderRow = hasFirstRow && rowIndex == 0;
            bool isBandedOdd = hasBandRow && (!hasFirstRow ? rowIndex % 2 == 0 : rowIndex > 0 && (rowIndex - 1) % 2 == 0);

            foreach (var cell in row.Elements<Drawing.TableCell>())
            {
                var cellStyles = new List<string>();

                // Cell fill
                var tcPr = cell.TableCellProperties ?? cell.GetFirstChild<Drawing.TableCellProperties>();
                var cellSolid = tcPr?.GetFirstChild<Drawing.SolidFill>();
                var cellColor = ResolveFillColor(cellSolid, themeColors);
                bool hasExplicitFill = cellColor != null;
                if (cellColor != null)
                    cellStyles.Add($"background:{cellColor}");

                var cellGrad = tcPr?.GetFirstChild<Drawing.GradientFill>();
                if (cellGrad != null)
                {
                    cellStyles.Add($"background:{GradientToCss(cellGrad, themeColors)}");
                    hasExplicitFill = true;
                }

                // Resolve fill / text color / borders for this cell through
                // the Core/TableStyles catalogue. Returns null for unknown
                // style ids (custom styles, no style at all); in that case
                // the cell renders with no style-provided fill or borders,
                // which matches OOXML "no style" semantics.
                var resolved = TableStyleResolver.Resolve(
                    tableStyleId,
                    new CellPosition(
                        RowIndex: rowIndex, ColIndex: colIndex,
                        RowCount: totalRows, ColCount: totalCols,
                        HasFirstRow: hasFirstRow, HasLastRow: hasLastRow,
                        HasFirstCol: hasFirstCol, HasLastCol: hasLastCol,
                        HasBandedRows: hasBandRow, HasBandedCols: hasBandCol),
                    themeColors);

                if (!hasExplicitFill && resolved != null)
                {
                    if (resolved.Fill != null) cellStyles.Add($"background:{resolved.Fill}");
                    if (resolved.TextColor != null) cellStyles.Add($"color:{resolved.TextColor}");
                }

                // Vertical alignment
                if (tcPr?.Anchor?.HasValue == true)
                {
                    var va = tcPr.Anchor.InnerText switch
                    {
                        "ctr" => "middle",
                        "b" => "bottom",
                        _ => "top"
                    };
                    cellStyles.Add($"vertical-align:{va}");
                }

                // Cell text direction (a:tcPr vert="…"): rotate text via CSS
                // writing-mode. Mirrors the Word handler's tcDir → writing-mode
                // mapping (WordHandler.HtmlPreview.Css.cs). PowerPoint stores
                // the direction on the tcPr "vert" attribute:
                //   vert    → top-to-bottom (vertical-rl)
                //   vert270 → bottom-to-top (vertical-rl + 180° rotate)
                //   eaVert  → East-Asian top-to-bottom, upright glyphs
                //   wordArtVert → stacked upright (approximate with upright)
                var vertDir = tcPr?.Vertical?.HasValue == true ? tcPr.Vertical.InnerText : null;
                if (vertDir != null)
                {
                    var wm = vertDir switch
                    {
                        "vert" => "vertical-rl",
                        "vert270" => "vertical-rl;transform:rotate(180deg)",
                        "eaVert" => "vertical-rl;text-orientation:upright",
                        "wordArtVert" or "wordArtVertRtl" => "vertical-rl;text-orientation:upright",
                        _ => null,
                    };
                    if (wm != null) cellStyles.Add($"writing-mode:{wm}");
                }

                // Cell text formatting
                var firstRun = cell.Descendants<Drawing.Run>().FirstOrDefault();
                if (firstRun?.RunProperties != null)
                {
                    var rp = firstRun.RunProperties;
                    if (rp.FontSize?.HasValue == true)
                        cellStyles.Add($"font-size:{rp.FontSize.Value / 100.0:0.##}pt");
                    // else: inherit from table style / slideMaster (no hardcoded default)
                    if (rp.Bold?.Value == true)
                        cellStyles.Add("font-weight:bold");
                    // Italic / underline / strike — mirror RenderRun so cell text
                    // formatting matches shape text. Previously only bold was read,
                    // silently dropping <a:rPr i="1"/u="…"/strike="…"> on table cells.
                    if (rp.Italic?.Value == true)
                        cellStyles.Add("font-style:italic");
                    var decorations = new List<string>();
                    if (rp.Underline?.HasValue == true && rp.Underline.Value != Drawing.TextUnderlineValues.None)
                        decorations.Add("underline");
                    if (rp.Strike?.HasValue == true && rp.Strike.Value != Drawing.TextStrikeValues.NoStrike)
                        decorations.Add("line-through");
                    if (decorations.Count > 0)
                        cellStyles.Add($"text-decoration:{string.Join(" ", decorations)}");
                    var fontVal = rp.GetFirstChild<Drawing.LatinFont>()?.Typeface?.Value
                        ?? rp.GetFirstChild<Drawing.EastAsianFont>()?.Typeface?.Value;
                    if (fontVal != null && !fontVal.StartsWith("+", StringComparison.Ordinal))
                        cellStyles.Add(CssFontFamilyWithFallback(fontVal));
                    var runColor = ResolveFillColor(rp.GetFirstChild<Drawing.SolidFill>(), themeColors);
                    if (runColor != null)
                        cellStyles.Add($"color:{runColor}");
                }

                // Cell borders (per-edge). Priority cascade:
                //   1. Explicit <a:lnL/R/T/B> on this cell (per-cell override)
                //   2. TableStyleResolver output (built-in style catalogue)
                //   3. default thin-black border when the table has no resolvable
                //      style at all (tableStyleId null/unknown), else "none"
                // Explicit <a:lnL> with <a:noFill/> yields "none" via
                // TableBorderToCss and short-circuits cleanly. The resolver
                // computes per-cell borders based on position (outer vs.
                // inner edges) following the style's <a:tcBdr> region rules.
                // When no style resolves, native PowerPoint still applies the
                // deck's default table style (thin black grid); mirror that with
                // a 1pt solid #000000 fallback rather than rendering border-less.
                // CONSISTENCY(table-borders): Npt solid #color idiom.
                string FormatBorder(ResolvedBorder? rb)
                    => rb != null ? $"{Units.EmuToPt(rb.WidthEmu):0.##}pt {rb.Dash} {rb.Color}"
                       : (resolved == null ? "1pt solid #000000" : "none");
                var borderLeft = tcPr?.GetFirstChild<Drawing.LeftBorderLineProperties>();
                var borderRight = tcPr?.GetFirstChild<Drawing.RightBorderLineProperties>();
                var borderTop = tcPr?.GetFirstChild<Drawing.TopBorderLineProperties>();
                var borderBottom = tcPr?.GetFirstChild<Drawing.BottomBorderLineProperties>();
                var bl = TableBorderToCss(borderLeft, themeColors) ?? FormatBorder(resolved?.Left);
                var br = TableBorderToCss(borderRight, themeColors) ?? FormatBorder(resolved?.Right);
                var bt = TableBorderToCss(borderTop, themeColors) ?? FormatBorder(resolved?.Top);
                var bb = TableBorderToCss(borderBottom, themeColors) ?? FormatBorder(resolved?.Bottom);
                cellStyles.Add($"border-left:{bl}");
                cellStyles.Add($"border-right:{br}");
                cellStyles.Add($"border-top:{bt}");
                cellStyles.Add($"border-bottom:{bb}");

                // Diagonal borders (<a:lnTlToBr> / <a:lnBlToTr>) — HTML has no
                // native diagonal-border; emit an absolute-positioned inline
                // SVG overlay inside the <td>. The <td> becomes position:relative
                // only when diagonals are actually present to minimize CSS
                // regression surface.
                var borderTlBr = tcPr?.GetFirstChild<Drawing.TopLeftToBottomRightBorderLineProperties>();
                var borderBlTr = tcPr?.GetFirstChild<Drawing.BottomLeftToTopRightBorderLineProperties>();
                var tlBrCss = TableBorderToCss(borderTlBr, themeColors);
                var blTrCss = TableBorderToCss(borderBlTr, themeColors);
                bool hasDiag = (tlBrCss != null && tlBrCss != "none")
                            || (blTrCss != null && blTrCss != "none");
                if (hasDiag)
                    cellStyles.Add("position:relative");

                // Cell margins/padding
                var marL = tcPr?.LeftMargin?.Value;
                var marR = tcPr?.RightMargin?.Value;
                var marT = tcPr?.TopMargin?.Value;
                var marB = tcPr?.BottomMargin?.Value;
                if (marL.HasValue || marR.HasValue || marT.HasValue || marB.HasValue)
                {
                    var pT = Units.EmuToPt(marT ?? 45720);
                    var pR = Units.EmuToPt(marR ?? 91440);
                    var pB = Units.EmuToPt(marB ?? 45720);
                    var pL = Units.EmuToPt(marL ?? 91440);
                    cellStyles.Add($"padding:{pT}pt {pR}pt {pB}pt {pL}pt");
                }

                // Paragraph alignment
                var firstPara = cell.TextBody?.Elements<Drawing.Paragraph>().FirstOrDefault();
                if (firstPara?.ParagraphProperties?.Alignment?.HasValue == true)
                {
                    var align = firstPara.ParagraphProperties.Alignment.InnerText switch
                    {
                        "ctr" => "center",
                        "r" => "right",
                        "just" => "justify",
                        _ => "left"
                    };
                    cellStyles.Add($"text-align:{align}");
                }

                // Render the cell's paragraphs through the same path shape text
                // bodies use (RenderTextBody → one <div class="para"> per
                // paragraph), so a multi-paragraph cell shows each paragraph on
                // its own line instead of being flattened to InnerText (which
                // concatenated "Line 1Line 2Line 3"). Per-paragraph alignment /
                // spacing / bullets carry over for free. The <td>-level font /
                // color / align styles above still apply and are inherited by
                // the para divs, so a single-paragraph cell is visually
                // unchanged. Empty text body falls back to "" (renders nothing).
                var cellBody = new StringBuilder();
                if (cell.TextBody != null)
                    RenderTextBody(cellBody, cell.TextBody, themeColors);
                var cellHtml = cellBody.ToString();
                var styleStr = cellStyles.Count > 0 ? $" style=\"{string.Join(";", cellStyles)}\"" : "";

                // Column/row span (GridSpan and RowSpan are on the TableCell, not TableCellProperties)
                var gridSpan = cell.GridSpan?.Value;
                var rowSpan = cell.RowSpan?.Value;
                var spanAttrs = "";
                if (gridSpan > 1) spanAttrs += $" colspan=\"{gridSpan}\"";
                if (rowSpan > 1) spanAttrs += $" rowspan=\"{rowSpan}\"";

                // Skip merged continuation cells. hMerge cells consume one slot
                // of the active skipCols counter; vMerge cells (vertical merge
                // continuation) do not affect horizontal accounting. All three
                // skip paths still advance colIndex by 1 — the continuation
                // cells occupy a real column position in the grid, they just
                // don't render their own <td>.
                if (cell.HorizontalMerge?.Value == true)
                {
                    if (skipCols > 0) skipCols--;
                    colIndex++;
                    continue;
                }
                if (cell.VerticalMerge?.Value == true)
                {
                    colIndex++;
                    continue;
                }
                if (skipCols > 0)
                {
                    skipCols--;
                    colIndex++;
                    continue;
                }

                if (gridSpan > 1) skipCols = (int)gridSpan - 1;

                var diagOverlay = "";
                if (hasDiag)
                {
                    var diagLines = new StringBuilder();
                    if (tlBrCss != null && tlBrCss != "none")
                    {
                        var (stroke, widthPt) = ParseBorderCssForSvg(tlBrCss);
                        diagLines.Append($"<line x1=\"0\" y1=\"0\" x2=\"100%\" y2=\"100%\" stroke=\"{stroke}\" stroke-width=\"{widthPt:0.##}\"/>");
                    }
                    if (blTrCss != null && blTrCss != "none")
                    {
                        var (stroke, widthPt) = ParseBorderCssForSvg(blTrCss);
                        diagLines.Append($"<line x1=\"0\" y1=\"100%\" x2=\"100%\" y2=\"0\" stroke=\"{stroke}\" stroke-width=\"{widthPt:0.##}\"/>");
                    }
                    diagOverlay = $"<svg class=\"cell-diag\" width=\"100%\" height=\"100%\" style=\"position:absolute;inset:0;pointer-events:none;overflow:visible\" preserveAspectRatio=\"none\">{diagLines}</svg>";
                }

                sb.AppendLine($"          <td{spanAttrs}{styleStr}>{diagOverlay}{cellHtml}</td>");
                colIndex += Math.Max((int)(gridSpan ?? 1), 1);
            }
            sb.AppendLine("        </tr>");
            rowIndex++;
        }

        sb.AppendLine("      </table>");
        sb.AppendLine("    </div>");
    }

    /// <summary>
    /// Convert a table cell border line properties element to a CSS border value.
    /// Returns null if the border has NoFill or is absent.
    /// </summary>
    private static string? TableBorderToCss(OpenXmlCompositeElement? borderProps, Dictionary<string, string> themeColors)
    {
        if (borderProps == null) return null;
        if (borderProps.GetFirstChild<Drawing.NoFill>() != null) return "none";

        var solidFill = borderProps.GetFirstChild<Drawing.SolidFill>();
        var color = ResolveFillColor(solidFill, themeColors) ?? "#000000";

        // Width attribute is on the element itself (w attr in EMU).
        // An EXPLICIT w="0" means "no border" (PowerPoint renders no line on
        // that edge); only an ABSENT w falls back to the default thin line.
        double widthPt = 1.0;
        long? widthEmu = borderProps switch
        {
            Drawing.LeftBorderLineProperties lb when lb.Width?.HasValue == true => lb.Width.Value,
            Drawing.RightBorderLineProperties rb when rb.Width?.HasValue == true => rb.Width.Value,
            Drawing.TopBorderLineProperties tb when tb.Width?.HasValue == true => tb.Width.Value,
            Drawing.BottomBorderLineProperties bb when bb.Width?.HasValue == true => bb.Width.Value,
            Drawing.TopLeftToBottomRightBorderLineProperties tlbr when tlbr.Width?.HasValue == true => tlbr.Width.Value,
            Drawing.BottomLeftToTopRightBorderLineProperties bltr when bltr.Width?.HasValue == true => bltr.Width.Value,
            _ => null
        };
        if (widthEmu == 0) return "none";
        if (widthEmu.HasValue) widthPt = widthEmu.Value / EmuConverter.EmuPerPointF;

        if (widthPt < 0.5) widthPt = 0.5;

        var dash = borderProps.GetFirstChild<Drawing.PresetDash>();
        var style = "solid";
        if (dash?.Val?.HasValue == true)
        {
            // CONSISTENCY(dash-pattern): map mixed dash-dot patterns to "dashed" (CSS has no native dashDot).
            // Previously fell through to "solid", which silently dropped the dash pattern.
            style = dash.Val.InnerText switch
            {
                "dash" or "lgDash" or "sysDash" => "dashed",
                "dot" or "sysDot" => "dotted",
                "dashDot" or "lgDashDot" or "lgDashDotDot"
                    or "sysDashDot" or "sysDashDotDot" => "dashed",
                _ => "solid"
            };
        }

        return $"{widthPt:0.##}pt {style} {color}";
    }

    /// <summary>
    /// Parse the "Npt style #color" shorthand produced by TableBorderToCss
    /// back into (stroke-color, stroke-width-in-pt) for SVG diagonal lines.
    /// Format is deterministic: "{w:0.##}pt {solid|dashed|dotted} {color}".
    /// </summary>
    private static (string stroke, double widthPt) ParseBorderCssForSvg(string css)
    {
        var parts = css.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        double widthPt = 1.0;
        string stroke = "#000000";
        if (parts.Length >= 1)
        {
            var w = parts[0];
            if (w.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
                w = w[..^2];
            double.TryParse(w, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out widthPt);
        }
        if (parts.Length >= 3)
            stroke = parts[2];
        return (stroke, widthPt);
    }

    // Per-cell style resolution (fill, text color, borders) moved to
    // Core/TableStyles/TableStyleResolver. This file now contains only
    // OOXML→HTML rendering glue; the built-in PowerPoint table-style
    // catalogue (11 family templates × 7 accent variants = 74 GUIDs) lives
    // under Core/TableStyles/ with one file per family. See
    // Core/TableStyles/CLAUDE.md context in Handlers/Pptx/CLAUDE.md.
}
