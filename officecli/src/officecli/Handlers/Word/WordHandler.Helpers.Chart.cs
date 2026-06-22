// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeCli.Core;
using Vml = DocumentFormat.OpenXml.Vml;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using M = DocumentFormat.OpenXml.Math;

namespace OfficeCli.Handlers;

public partial class WordHandler
{

    // ==================== Extended Chart Helpers ====================

    private const string WordChartExUri = "http://schemas.microsoft.com/office/drawing/2014/chartex";
    private const string WordChartUri = "http://schemas.openxmlformats.org/drawingml/2006/chart";

    /// <summary>
    /// Count all charts (both standard ChartPart and ExtendedChartPart) in the document.
    /// </summary>
    private static int CountWordCharts(MainDocumentPart mainPart)
    {
        return mainPart.ChartParts.Count() + mainPart.ExtendedChartParts.Count();
    }

    /// <summary>
    /// Represents a chart part in Word that could be either a standard ChartPart or an ExtendedChartPart.
    /// </summary>
    private class WordChartInfo
    {
        public ChartPart? StandardPart { get; set; }
        public ExtendedChartPart? ExtendedPart { get; set; }
        public DW.DocProperties? DocProperties { get; set; }
        /// <summary>
        /// The <c>wp:inline</c> element that hosts this chart — needed by
        /// chart position Set to mutate the <c>wp:extent</c> child. Null when
        /// the chart is anchored (floating); see <see cref="Anchor"/>.
        /// </summary>
        public DW.Inline? Inline { get; set; }
        /// <summary>
        /// The <c>wp:anchor</c> element that hosts this chart when it is a
        /// floating (anchored) chart. Null for an inline chart. Charts can be
        /// wrapped in either a <c>wp:inline</c> or a <c>wp:anchor</c> just like
        /// pictures, so both must be enumerated for round-trip.
        /// </summary>
        public DW.Anchor? Anchor { get; set; }
        /// <summary>The hosting frame element (inline or anchor) in document order.</summary>
        public OpenXmlElement? Container => (OpenXmlElement?)Inline ?? Anchor;
        /// <summary>The frame's <c>wp:extent</c> regardless of inline/anchor host.</summary>
        public DW.Extent? Extent => Inline?.Extent ?? Anchor?.Extent;
        public bool IsExtended => ExtendedPart != null;
    }

    /// <summary>
    /// Get all chart parts (standard + extended) in document order by walking Drawing/Inline elements.
    /// </summary>
    private List<WordChartInfo> GetAllWordCharts()
    {
        var result = new List<WordChartInfo>();
        var mainPart = _doc.MainDocumentPart;
        if (mainPart?.Document?.Body == null) return result;

        // Charts can be inserted in main document body, header parts, or footer parts.
        // Each part owns its own ImagePart/ChartPart relationships (round23 S host-part
        // routing), so look up the chart rel against the part the inline belongs to —
        // not always mainPart. Without this, header/footer charts are dropped from
        // GetAllWordCharts and AddChart's path emission falls back to /chart[0].
        var hostScans = new List<(OpenXmlPart Part, OpenXmlElement? Root)>
        {
            (mainPart, mainPart.Document.Body)
        };
        foreach (var hp in mainPart.HeaderParts)
            hostScans.Add((hp, hp.Header));
        foreach (var fp in mainPart.FooterParts)
            hostScans.Add((fp, fp.Footer));

        foreach (var (hostPart, root) in hostScans)
        {
            if (root == null) continue;
            // Charts host in either a <wp:inline> or a <wp:anchor> (floating),
            // exactly like pictures. Walk every <w:drawing> in document order so
            // inline and anchored charts interleave correctly — the batch-emit
            // ChartCursor consumes specs in this same document order. Walking
            // only Descendants<DW.Inline> silently dropped every floating chart.
            foreach (var drawing in root.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>())
            {
                var inline = drawing.GetFirstChild<DW.Inline>();
                var anchor = inline == null ? drawing.GetFirstChild<DW.Anchor>() : null;
                OpenXmlElement? frame = (OpenXmlElement?)inline ?? anchor;
                if (frame == null) continue;

                var graphicData = frame.Descendants<A.GraphicData>().FirstOrDefault();
                if (graphicData == null) continue;

                var docProps = frame.Descendants<DW.DocProperties>().FirstOrDefault();

                if (graphicData.Uri == WordChartUri)
                {
                    var chartRef = graphicData.Descendants<DocumentFormat.OpenXml.Drawing.Charts.ChartReference>().FirstOrDefault();
                    if (chartRef?.Id?.Value == null) continue;
                    try
                    {
                        var chartPart = (ChartPart)hostPart.GetPartById(chartRef.Id.Value);
                        result.Add(new WordChartInfo { StandardPart = chartPart, DocProperties = docProps, Inline = inline, Anchor = anchor });
                    }
                    catch { /* skip invalid references */ }
                }
                else if (graphicData.Uri == WordChartExUri)
                {
                    var relId = GetWordExtendedChartRelId(frame);
                    if (relId == null) continue;
                    try
                    {
                        var extPart = (ExtendedChartPart)hostPart.GetPartById(relId);
                        result.Add(new WordChartInfo { ExtendedPart = extPart, DocProperties = docProps, Inline = inline, Anchor = anchor });
                    }
                    catch { /* skip invalid references */ }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Apply <c>width</c> / <c>height</c> to a Word inline chart's
    /// <c>wp:extent</c>. Accepts unit-qualified sizes (`6cm`, `2in`,
    /// `720pt`) or raw EMU integers via EmuConverter.
    ///
    /// CONSISTENCY(chart-position-set): mirrors the PPTX and Excel path.
    /// Word inline charts have no absolute x/y (they flow with text), so
    /// those keys — if provided — are appended to <paramref name="unsupported"/>
    /// rather than silently dropped.
    /// </summary>
    private static void ApplyWordChartPositionSet(
        WordChartInfo chartInfo, Dictionary<string, string> properties, List<string> unsupported)
    {
        var extent = chartInfo.Extent;
        if (extent == null) return;
        var anchor = chartInfo.Anchor;

        // x/y are meaningless for inline charts (they flow with text). On an
        // anchored (floating) chart they map to the absolute <wp:posOffset>.
        foreach (var k in new[] { "x", "y" })
        {
            var matched = properties.Keys
                .FirstOrDefault(key => key.Equals(k, StringComparison.OrdinalIgnoreCase));
            if (matched == null) continue;
            if (anchor != null
                && OfficeCli.Core.EmuConverter.TryParseEmu(properties[matched], out var off))
            {
                var pos = k == "x" ? anchor.HorizontalPosition : (OpenXmlElement?)anchor.VerticalPosition;
                var posOffset = pos?.GetFirstChild<DW.PositionOffset>();
                if (posOffset != null) { posOffset.Text = off.ToString(System.Globalization.CultureInfo.InvariantCulture); continue; }
            }
            unsupported.Add(matched);
            Console.Error.WriteLine(
                $"Warning: '{matched}' is ignored on this Word chart — inline charts have no absolute position " +
                "and anchored charts using <wp:align> carry no posOffset to mutate.");
        }

        if (properties.TryGetValue("width", out var wStr))
        {
            try { extent.Cx = OfficeCli.Core.EmuConverter.ParseEmu(wStr); }
            catch { unsupported.Add("width"); }
        }

        if (properties.TryGetValue("height", out var hStr))
        {
            try { extent.Cy = OfficeCli.Core.EmuConverter.ParseEmu(hStr); }
            catch { unsupported.Add("height"); }
        }
    }

    /// <summary>
    /// Wrap a chart's <c>a:graphic</c> in either a <c>wp:inline</c> or a
    /// <c>wp:anchor</c> (floating) frame, picking the mode from the same
    /// floating-placement props AddPicture recognizes (<c>anchor=true</c> or a
    /// non-inline <c>wrap=</c>). Inline is the default — interactive
    /// <c>add chart</c> and round-trip of inline charts are unaffected.
    ///
    /// CONSISTENCY(anchor-props): the prop vocabulary (wrap / hposition /
    /// vposition / halign / valign / hrelative / vrelative / behindtext /
    /// relativeHeight / effectExtent / wrapDist) is copied verbatim from
    /// AddPicture's floating branch so charts and pictures round-trip the same
    /// anchor through dump→batch.
    /// </summary>
    private static OpenXmlElement BuildChartFrame(
        A.Graphic graphic, long chartCx, long chartCy, uint docPropId, string chartName,
        Dictionary<string, string> properties)
    {
        (long L, long T, long R, long B)? effectExtent = null;
        if (properties.TryGetValue("effectExtent", out var eeStr) && !string.IsNullOrWhiteSpace(eeStr))
        {
            var ee = eeStr.Split(',');
            if (ee.Length == 4
                && long.TryParse(ee[0].Trim(), out var eeL) && long.TryParse(ee[1].Trim(), out var eeT)
                && long.TryParse(ee[2].Trim(), out var eeR) && long.TryParse(ee[3].Trim(), out var eeB))
                effectExtent = (eeL, eeT, eeR, eeB);
        }

        bool wrapImpliesAnchor = properties.TryGetValue("wrap", out var implicitWrap)
            && !string.IsNullOrEmpty(implicitWrap)
            && !string.Equals(implicitWrap, "none", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(implicitWrap, "inline", StringComparison.OrdinalIgnoreCase);
        bool anchorIsFloating = properties.TryGetValue("anchor", out var anchorVal)
            && !string.IsNullOrEmpty(anchorVal)
            && ParseHelpers.IsValidBooleanString(anchorVal) && IsTruthy(anchorVal);

        if (!anchorIsFloating && !wrapImpliesAnchor)
        {
            return new DW.Inline(
                new DW.Extent { Cx = chartCx, Cy = chartCy },
                new DW.EffectExtent
                {
                    LeftEdge = (effectExtent ?? (0, 0, 0, 0)).L,
                    TopEdge = (effectExtent ?? (0, 0, 0, 0)).T,
                    RightEdge = (effectExtent ?? (0, 0, 0, 0)).R,
                    BottomEdge = (effectExtent ?? (0, 0, 0, 0)).B
                },
                new DW.DocProperties { Id = docPropId, Name = chartName },
                new DW.NonVisualGraphicFrameDrawingProperties(),
                graphic)
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            };
        }

        var wrapType = properties.GetValueOrDefault("wrap", "none");
        // CONSISTENCY(anchor-props): wrap element switch mirrors CreateAnchorImageRun.
        OpenXmlElement wrapElement = wrapType.ToLowerInvariant() switch
        {
            "square" => new DW.WrapSquare { WrapText = DW.WrapTextValues.BothSides },
            "tight" => new DW.WrapTight(new DW.WrapPolygon(
                new DW.StartPoint { X = 0, Y = 0 }, new DW.LineTo { X = 21600, Y = 0 },
                new DW.LineTo { X = 21600, Y = 21600 }, new DW.LineTo { X = 0, Y = 21600 },
                new DW.LineTo { X = 0, Y = 0 }) { Edited = false }) { WrapText = DW.WrapTextValues.BothSides },
            "through" => new DW.WrapThrough(new DW.WrapPolygon(
                new DW.StartPoint { X = 0, Y = 0 }, new DW.LineTo { X = 21600, Y = 0 },
                new DW.LineTo { X = 21600, Y = 21600 }, new DW.LineTo { X = 0, Y = 21600 },
                new DW.LineTo { X = 0, Y = 0 }) { Edited = false }) { WrapText = DW.WrapTextValues.BothSides },
            "topandbottom" or "topbottom" => new DW.WrapTopBottom(),
            "none" => new DW.WrapNone(),
            _ => new DW.WrapSquare { WrapText = DW.WrapTextValues.BothSides }
        };

        long hPos = properties.TryGetValue("hposition", out var hPosStr) ? EmuConverter.ParseEmu(hPosStr) : 0;
        long vPos = properties.TryGetValue("vposition", out var vPosStr) ? EmuConverter.ParseEmu(vPosStr) : 0;
        var hRel = properties.TryGetValue("hrelative", out var hRelStr)
            ? ParseHorizontalRelative(hRelStr) : DW.HorizontalRelativePositionValues.Margin;
        var vRel = properties.TryGetValue("vrelative", out var vRelStr)
            ? ParseVerticalRelative(vRelStr) : DW.VerticalRelativePositionValues.Paragraph;
        var behind = properties.TryGetValue("behindtext", out var behindStr) && IsTruthy(behindStr);
        var hAlign = properties.TryGetValue("halign", out var hAlignStr) && !string.IsNullOrEmpty(hAlignStr) ? hAlignStr : null;
        var vAlign = properties.TryGetValue("valign", out var vAlignStr) && !string.IsNullOrEmpty(vAlignStr) ? vAlignStr : null;
        uint relHeight = properties.TryGetValue("relativeHeight", out var rhStr) && uint.TryParse(rhStr, out var rh) ? rh : 1U;
        (uint T, uint B, uint L, uint R)? wrapDist = null;
        if (properties.TryGetValue("wrapDist", out var wdStr) && !string.IsNullOrWhiteSpace(wdStr))
        {
            var wd = wdStr.Split(',');
            if (wd.Length == 4
                && uint.TryParse(wd[0].Trim(), out var wdT) && uint.TryParse(wd[1].Trim(), out var wdB)
                && uint.TryParse(wd[2].Trim(), out var wdL) && uint.TryParse(wd[3].Trim(), out var wdR))
                wrapDist = (wdT, wdB, wdL, wdR);
        }

        OpenXmlElement hChild = !string.IsNullOrEmpty(hAlign)
            ? new DW.HorizontalAlignment(hAlign) : new DW.PositionOffset(hPos.ToString());
        OpenXmlElement vChild = !string.IsNullOrEmpty(vAlign)
            ? new DW.VerticalAlignment(vAlign) : new DW.PositionOffset(vPos.ToString());

        return new DW.Anchor(
            new DW.SimplePosition { X = 0, Y = 0 },
            new DW.HorizontalPosition(hChild) { RelativeFrom = hRel },
            new DW.VerticalPosition(vChild) { RelativeFrom = vRel },
            new DW.Extent { Cx = chartCx, Cy = chartCy },
            new DW.EffectExtent
            {
                LeftEdge = (effectExtent ?? (0, 0, 0, 0)).L,
                TopEdge = (effectExtent ?? (0, 0, 0, 0)).T,
                RightEdge = (effectExtent ?? (0, 0, 0, 0)).R,
                BottomEdge = (effectExtent ?? (0, 0, 0, 0)).B
            },
            wrapElement,
            new DW.DocProperties { Id = docPropId, Name = chartName },
            new DW.NonVisualGraphicFrameDrawingProperties(),
            graphic)
        {
            BehindDoc = behind,
            DistanceFromTop = wrapDist?.T ?? 0U,
            DistanceFromBottom = wrapDist?.B ?? 0U,
            DistanceFromLeft = wrapDist?.L ?? 114300U,
            DistanceFromRight = wrapDist?.R ?? 114300U,
            SimplePos = false,
            RelativeHeight = relHeight,
            AllowOverlap = true,
            LayoutInCell = true,
            Locked = false
        };
    }

    /// <summary>
    /// Get the relationship ID from an extended chart inline/anchor frame.
    /// </summary>
    private static string? GetWordExtendedChartRelId(OpenXmlElement frame)
    {
        var gd = frame.Descendants<A.GraphicData>().FirstOrDefault(g => g.Uri == WordChartExUri);
        if (gd == null) return null;
        var typed = gd.Descendants<DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing.RelId>().FirstOrDefault();
        if (typed?.Id?.Value != null) return typed.Id.Value;
        foreach (var child in gd.ChildElements)
        {
            var rId = child.GetAttributes().FirstOrDefault(a =>
                a.LocalName == "id" && a.NamespaceUri == "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            if (rId.Value != null) return rId.Value;
        }
        return null;
    }
}
