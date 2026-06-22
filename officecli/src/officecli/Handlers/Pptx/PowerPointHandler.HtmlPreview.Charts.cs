// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{
    // ==================== Chart Rendering ====================

    // Chart text color — set per-chart, also used by SvgPreview
    private string _chartValueColor = "#D0D8E0";

    private void RenderChart(StringBuilder sb, GraphicFrame gf, SlidePart slidePart, Dictionary<string, string> themeColors, string? dataPath = null,
        (long x, long y, long cx, long cy)? overridePos = null)
    {
        var dataPathAttr = string.IsNullOrEmpty(dataPath) ? "" : $" data-path=\"{HtmlEncode(dataPath)}\"";
        // Position and size from p:xfrm
        var pxfrm = gf.GetFirstChild<DocumentFormat.OpenXml.Presentation.Transform>();
        var off = pxfrm?.GetFirstChild<Drawing.Offset>();
        var ext = pxfrm?.GetFirstChild<Drawing.Extents>();
        if (off == null || ext == null) return;

        // R14-2: when nested in a group, position/size are re-projected into the
        // group's child coordinate system by the caller (CalcGroupChildPos).
        var posX = overridePos?.x ?? off.X?.Value ?? 0;
        var posY = overridePos?.y ?? off.Y?.Value ?? 0;
        var posCx = overridePos?.cx ?? ext.Cx?.Value ?? 0;
        var posCy = overridePos?.cy ?? ext.Cy?.Value ?? 0;

        var x = Units.EmuToPt(posX);
        var y = Units.EmuToPt(posY);
        var w = Units.EmuToPt(posCx);
        var h = Units.EmuToPt(posCy);

        // Get chart part
        var chartEl = gf.Descendants().FirstOrDefault(e => e.LocalName == "chart" && e.NamespaceUri.Contains("chart"));
        var rId = chartEl?.GetAttributes().FirstOrDefault(a => a.LocalName == "id" && a.NamespaceUri.Contains("relationships")).Value;
        if (rId == null) return;

        DocumentFormat.OpenXml.Drawing.Charts.Chart? chart;
        DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea;
        ChartSvgRenderer.ChartInfo info;
        try
        {
            var anyPart = slidePart.GetPartById(rId);
            // cx:chart (extended) path — branch early, extract via ExtractCxChartInfo,
            // skip the regular c:PlotArea pipeline since cx uses its own layout.
            if (anyPart is ExtendedChartPart extPart)
            {
                var cxChart = extPart.ChartSpace?
                    .GetFirstChild<DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing.Chart>();
                if (cxChart == null) return;
                info = ChartSvgRenderer.ExtractCxChartInfo(cxChart);
                chart = null;
                plotArea = null;
            }
            else if (anyPart is ChartPart chartPart)
            {
                chart = chartPart.ChartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
                plotArea = chart?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.PlotArea>();
                if (plotArea == null) return;
                info = ChartSvgRenderer.ExtractChartInfo(plotArea, chart, themeColors);
            }
            else return;
        }
        catch { return; }

        if (info.Series.Count == 0) return;

        // Derive text color from theme
        var chartTextColor = themeColors.TryGetValue("tx1", out var tx1) ? $"#{tx1}"
            : themeColors.TryGetValue("dk1", out var dk1) ? $"#{dk1}" : "#D0D8E0";
        _chartValueColor = chartTextColor;
        var isDarkText = IsColorDark(chartTextColor.TrimStart('#'));

        // Create renderer with theme-derived colors
        var renderer = new ChartSvgRenderer
        {
            ThemeAccentColors = ChartSvgRenderer.BuildThemeAccentColors(themeColors),
            ValueColor = chartTextColor,
            CatColor = chartTextColor,
            AxisColor = chartTextColor,
            GridColor = info.GridlineColor != null ? $"#{info.GridlineColor}" : (isDarkText ? "#ccc" : "#333"),
            AxisLineColor = info.AxisLineColor != null ? $"#{info.AxisLineColor}" : (isDarkText ? "#aaa" : "#555"),
            ValFontPx = info.ValFontPx,
            CatFontPx = info.CatFontPx
        };

        // SVG dimensions (scale EMU to reasonable SVG units)
        var widthEmu = posCx != 0 ? posCx : 3600000;
        var heightEmu = posCy != 0 ? posCy : 2520000;
        var svgW = (int)(widthEmu / 10000.0);
        var svgH = (int)(heightEmu / 10000.0);
        var titleH = string.IsNullOrEmpty(info.Title) ? 0 : 20;
        var chartSvgH = svgH - titleH;

        // Manual layout margins — only regular c:chart has a ManualLayout.
        var plotAreaLayout = plotArea?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Layout>();
        var manualLayout = plotAreaLayout?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.ManualLayout>();
        int marginTop, marginRight, marginBottom, marginLeft;
        if (manualLayout != null)
        {
            var mlX = manualLayout.Left?.Val?.Value ?? 0.0;
            var mlY = manualLayout.Top?.Val?.Value ?? 0.0;
            var mlW = manualLayout.Width?.Val?.Value ?? 1.0;
            var mlH = manualLayout.Height?.Val?.Value ?? 1.0;
            marginLeft = Math.Max((int)(mlX * svgW), 5);
            marginTop = Math.Max((int)(mlY * chartSvgH), 5);
            marginRight = Math.Max((int)((1.0 - mlX - mlW) * svgW), 5);
            marginBottom = Math.Max((int)((1.0 - mlY - mlH) * chartSvgH), 5);
        }
        else
        {
            marginTop = 10; marginRight = 15; marginBottom = 25; marginLeft = 40;
        }

        // Container with chart background
        var bgStyle = info.ChartFillColor != null ? $"background:#{info.ChartFillColor};" : "background:transparent;";
        sb.AppendLine($"    <div class=\"shape\"{dataPathAttr} style=\"left:{x}pt;top:{y}pt;width:{w}pt;height:{h}pt;{bgStyle}display:flex;flex-direction:column;overflow:hidden\">");

        // Title — honor the chart's own title run color when present (raw OOXML
        // hex, needs '#' for valid CSS); otherwise fall back to the theme text color.
        if (!string.IsNullOrEmpty(info.Title))
        {
            var titleColor = info.TitleFontColor != null ? ChartSvgRenderer.CssHexColor(info.TitleFontColor) : chartTextColor;
            sb.AppendLine($"      <div style=\"text-align:center;font-size:{info.TitleFontSize};font-weight:bold;padding:4px;flex-shrink:0;color:{titleColor}\">{ChartSvgRenderer.HtmlEncode(info.Title)}</div>");
        }

        // Legend position drives the plot+legend layout, mirroring the Word/Excel
        // paths. right="r" → row, legend after plot; left="l" → row, legend before;
        // top="t"/"tr" → column, legend before; bottom (default) → below the plot.
        var legendSide = info.HasLegend && info.LegendPos is "r" or "l";
        var legendTop  = info.HasLegend && info.LegendPos is "t" or "tr";

        if (legendTop)
            renderer.RenderLegendHtml(sb, info, chartTextColor);

        if (legendSide)
        {
            var flexDir = info.LegendPos == "l" ? "row-reverse" : "row";
            sb.AppendLine($"      <div style=\"display:flex;flex-direction:{flexDir};align-items:center;gap:8px;flex:1;min-height:0\">");
        }

        sb.AppendLine($"      <svg viewBox=\"0 0 {svgW} {chartSvgH}\" style=\"width:100%;flex:1;min-height:0\" preserveAspectRatio=\"xMidYMin meet\">");

        renderer.RenderChartSvgContent(sb, info, svgW, chartSvgH, marginLeft, marginTop, marginRight, marginBottom);

        sb.AppendLine("      </svg>");

        if (legendSide)
        {
            renderer.RenderLegendHtml(sb, info, chartTextColor);
            sb.AppendLine("      </div>");
        }
        else if (!legendTop)
        {
            renderer.RenderLegendHtml(sb, info, chartTextColor);
        }

        // R16a: render the data table grid when dataTable=true, mirroring the
        // Excel chart path (ExcelHandler.HtmlPreview.Charts.cs). The PPTX path
        // previously omitted this call, so dataTable=true charts showed no grid.
        renderer.RenderDataTableHtml(sb, info);

        sb.AppendLine("    </div>");
    }
}
