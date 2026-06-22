// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeCli.Core;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DW14 = DocumentFormat.OpenXml.Office2010.Word.Drawing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace OfficeCli.Handlers;

public partial class WordHandler
{
    // ==================== Image Helpers ====================

    private static long ParseEmu(string value) => Core.EmuConverter.ParseEmu(value);

    private uint NextDocPropId()
    {
        // BUG-R14A: drawing object ids (wp:docPr/@id) must be unique across the
        // WHOLE document, not just the main body. A picture added into a
        // footnote/endnote/comment is serialized to that part, where existing
        // drawings (and the footnote/endnote separator drawings) carry their own
        // docPr ids — scanning only the body let the new picture reuse id "1"
        // and triggered an "id should be unique" semantic warning. Scan every
        // part that can host a <w:drawing>.
        var main = _doc.MainDocumentPart;
        uint maxId = 0;
        if (main != null)
        {
            foreach (var root in EnumerateContentRoots(main))
                foreach (var dp in root.Descendants<DW.DocProperties>())
                {
                    if (dp.Id?.HasValue == true && dp.Id.Value > maxId)
                        maxId = dp.Id.Value;
                }
        }
        return maxId + 1;
    }

    private static Run CreateImageRun(string relationshipId, long cx, long cy, string altText, uint docPropId, string? pictureName = null,
        (long L, long T, long R, long B)? effectExtent = null)
    {
        var docPrName = pictureName ?? altText;
        // BUG-DUMP-R29-1: honour the captured <wp:effectExtent> (drawing's
        // visual overflow/effect margin) instead of the old hardcoded
        // 0/0/0/0, which flattened every inline drawing's effect margin and
        // shifted its layout height. Defaults to 0/0/0/0 when the caller has
        // no effectExtent prop (interactive Add).
        var ee = effectExtent ?? (0, 0, 0, 0);
        var inline = new DW.Inline(
            new DW.Extent { Cx = cx, Cy = cy },
            new DW.EffectExtent { LeftEdge = ee.L, TopEdge = ee.T, RightEdge = ee.R, BottomEdge = ee.B },
            new DW.DocProperties { Id = docPropId, Name = docPrName, Description = altText },
            new DW.NonVisualGraphicFrameDrawingProperties(
                new A.GraphicFrameLocks { NoChangeAspect = true }
            ),
            new A.Graphic(
                new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = docPropId, Name = docPrName },
                            new PIC.NonVisualPictureDrawingProperties()
                        ),
                        new PIC.BlipFill(
                            new A.Blip { Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print },
                            new A.Stretch(new A.FillRectangle())
                        ),
                        new PIC.ShapeProperties(
                            new A.Transform2D(
                                new A.Offset { X = 0L, Y = 0L },
                                new A.Extents { Cx = cx, Cy = cy }
                            ),
                            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                        )
                    )
                ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
            )
        )
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U
        };

        return new Run(new Drawing(inline));
    }

    // BUG-R24-WRAPPOLY: rebuild a wrapTight/wrapThrough polygon from the captured
    // "edited;side;x,y x,y …" string (see ImageHelpers GET-side). Falls back to
    // the default full-bounds square (0,0 → 21600,21600) when the prop is absent
    // or malformed, so a typed `add picture wrap=tight` with no polygon behaves
    // exactly as before.
    private static DW.WrapPolygon BuildWrapPolygon(string? serialized)
    {
        DW.WrapPolygon Default() => new(
            new DW.StartPoint { X = 0, Y = 0 },
            new DW.LineTo { X = 21600, Y = 0 },
            new DW.LineTo { X = 21600, Y = 21600 },
            new DW.LineTo { X = 0, Y = 21600 },
            new DW.LineTo { X = 0, Y = 0 }) { Edited = false };
        if (string.IsNullOrWhiteSpace(serialized)) return Default();
        var parts = serialized.Split(';');
        if (parts.Length < 3) return Default();
        var vertTokens = parts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var pts = new List<(int X, int Y)>();
        foreach (var t in vertTokens)
        {
            var xy = t.Split(',');
            if (xy.Length == 2 && int.TryParse(xy[0], out var x) && int.TryParse(xy[1], out var y))
                pts.Add((x, y));
        }
        if (pts.Count < 3) return Default();
        var poly = new DW.WrapPolygon { Edited = parts[0] == "1" };
        poly.AppendChild(new DW.StartPoint { X = pts[0].X, Y = pts[0].Y });
        for (int i = 1; i < pts.Count; i++)
            poly.AppendChild(new DW.LineTo { X = pts[i].X, Y = pts[i].Y });
        return poly;
    }

    // Recover the wrapText side captured in the polygon prop's 2nd field; default
    // bothSides (Word's default and the prior hardcoded value).
    private static DW.WrapTextValues WrapSideFrom(string? serialized)
    {
        var side = serialized?.Split(';') is { Length: >= 2 } p ? p[1] : "bothSides";
        return WrapSideValue(side);
    }

    // Map a wrapText side token (left/right/largest/bothSides) to its enum;
    // default bothSides (Word's default and OOXML required value on wrapSquare).
    private static DW.WrapTextValues WrapSideValue(string? side) => side switch
    {
        "left" => DW.WrapTextValues.Left,
        "right" => DW.WrapTextValues.Right,
        "largest" => DW.WrapTextValues.Largest,
        _ => DW.WrapTextValues.BothSides,
    };

    private static Run CreateAnchorImageRun(string relationshipId, long cx, long cy, string altText,
        string wrap, long hPos, long vPos,
        DW.HorizontalRelativePositionValues hRel, DW.VerticalRelativePositionValues vRel,
        bool behindText, uint docPropId, string? pictureName = null,
        string? hAlign = null, string? vAlign = null, uint relativeHeight = 1U,
        (long L, long T, long R, long B)? effectExtent = null,
        (uint T, uint B, uint L, uint R)? wrapDist = null,
        string? wrapPolygon = null,
        string? sizeRelH = null, string? sizeRelV = null,
        string? wrapSide = null)
    {
        OpenXmlElement wrapElement = wrap.ToLowerInvariant() switch
        {
            "square" => new DW.WrapSquare { WrapText = WrapSideValue(wrapSide) },
            // WrapText is REQUIRED on wrapTight/wrapThrough (same as wrapSquare);
            // omitting it produces schema-invalid XML that real Word refuses to
            // open. BUG-R24-WRAPPOLY: honor a captured source polygon (vertices +
            // edited flag + wrap side) when present; the default full-bounds
            // square otherwise extends the wrap boundary and shifts text.
            "tight" => new DW.WrapTight(BuildWrapPolygon(wrapPolygon))
                { WrapText = WrapSideFrom(wrapPolygon) },
            "through" => new DW.WrapThrough(BuildWrapPolygon(wrapPolygon))
                { WrapText = WrapSideFrom(wrapPolygon) },
            "topandbottom" or "topbottom" => new DW.WrapTopBottom(),
            "none" => new DW.WrapNone() as OpenXmlElement,
            _ => throw new ArgumentException($"Invalid wrap value: '{wrap}'. Valid values: none, square, tight, through, topandbottom.")
        };

        var anchorDocPropId = docPropId;
        var docPrName = pictureName ?? altText;
        // A floating axis is positioned EITHER by an absolute <wp:posOffset>
        // OR by a relative <wp:align> keyword. When an align keyword is given
        // (left/center/right horizontally; top/bottom/center/inside/outside
        // vertically), emit <wp:align> so the picture honours Word's relative
        // placement instead of collapsing to posOffset=0 at the margin origin.
        OpenXmlElement hChild = !string.IsNullOrEmpty(hAlign)
            ? new DW.HorizontalAlignment(hAlign)
            : new DW.PositionOffset(hPos.ToString());
        OpenXmlElement vChild = !string.IsNullOrEmpty(vAlign)
            ? new DW.VerticalAlignment(vAlign)
            : new DW.PositionOffset(vPos.ToString());
        var anchor = new DW.Anchor(
            new DW.SimplePosition { X = 0, Y = 0 },
            new DW.HorizontalPosition(hChild) { RelativeFrom = hRel },
            new DW.VerticalPosition(vChild) { RelativeFrom = vRel },
            new DW.Extent { Cx = cx, Cy = cy },
            // BUG-DUMP-R29-1: see CreateImageRun — restore the anchored
            // drawing's captured effectExtent instead of hardcoding zeros.
            new DW.EffectExtent
            {
                LeftEdge = (effectExtent ?? (0, 0, 0, 0)).L,
                TopEdge = (effectExtent ?? (0, 0, 0, 0)).T,
                RightEdge = (effectExtent ?? (0, 0, 0, 0)).R,
                BottomEdge = (effectExtent ?? (0, 0, 0, 0)).B
            },
            wrapElement,
            new DW.DocProperties { Id = anchorDocPropId, Name = docPrName, Description = altText },
            new DW.NonVisualGraphicFrameDrawingProperties(
                new A.GraphicFrameLocks { NoChangeAspect = true }),
            new A.Graphic(
                new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = anchorDocPropId, Name = docPrName },
                            new PIC.NonVisualPictureDrawingProperties()),
                        new PIC.BlipFill(
                            new A.Blip { Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print },
                            new A.Stretch(new A.FillRectangle())),
                        new PIC.ShapeProperties(
                            new A.Transform2D(
                                new A.Offset { X = 0L, Y = 0L },
                                new A.Extents { Cx = cx, Cy = cy }),
                            new A.PresetGeometry(new A.AdjustValueList())
                                { Preset = A.ShapeTypeValues.Rectangle })
                    )
                ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
            )
        )
        {
            BehindDoc = behindText,
            // BUG: distT/distB/distL/distR (the gap between a floating image
            // and the text wrapping around it) were hardcoded — top/bottom to 0
            // and left/right to 114300 (0.125") regardless of source. A figure
            // with asymmetric wrap distances (distL=0 distR=71755) came back
            // with a symmetric 0.125" margin, shifting every line of the
            // adjacent text. Restore the captured values; default to the old
            // hardcoded margins when the caller has none (interactive add).
            DistanceFromTop = wrapDist?.T ?? 0U,
            DistanceFromBottom = wrapDist?.B ?? 0U,
            DistanceFromLeft = wrapDist?.L ?? 114300U,
            DistanceFromRight = wrapDist?.R ?? 114300U,
            SimplePos = false,
            // BUG-DUMP-R26-1: honour the captured z-order instead of the old
            // hardcoded 1U, which collapsed every overlapping float to the same
            // plane. Defaults to 1U when the caller has no relativeHeight prop.
            RelativeHeight = relativeHeight,
            AllowOverlap = true,
            LayoutInCell = true,
            Locked = false
        };

        // BUG-DUMP-NAR-WP14: re-attach the wp14 relative-sizing extensions
        // (<wp14:sizeRelH>/<wp14:sizeRelV> with <wp14:pctWidth>/<wp14:pctHeight>).
        // These are the last children of wp:anchor; without them a picture sized
        // relative to the page falls back to its absolute extent and reflows.
        // Prop form: "relativeFrom;pct" (e.g. "page;0").
        AppendRelativeSize(anchor, sizeRelH, horizontal: true);
        AppendRelativeSize(anchor, sizeRelV, horizontal: false);

        return new Run(new Drawing(anchor));
    }

    /// <summary>
    /// Append a wp14 relative-size child (sizeRelH or sizeRelV) to an anchor from
    /// a "relativeFrom;pct" prop. No-op when spec is null/blank or malformed.
    /// </summary>
    private static void AppendRelativeSize(DW.Anchor anchor, string? spec, bool horizontal)
    {
        if (string.IsNullOrWhiteSpace(spec)) return;
        var parts = spec.Split(';');
        if (parts.Length < 2) return;
        var relFrom = parts[0].Trim();
        var pct = parts[1].Trim();
        if (horizontal)
        {
            var rw = new DW14.RelativeWidth(new DW14.PercentageWidth(pct))
            {
                ObjectId = new EnumValue<DW14.SizeRelativeHorizontallyValues>(
                    new DW14.SizeRelativeHorizontallyValues(relFrom)),
            };
            anchor.AppendChild(rw);
        }
        else
        {
            var rh = new DW14.RelativeHeight(new DW14.PercentageHeight(pct))
            {
                RelativeFrom = new EnumValue<DW14.SizeRelativeVerticallyValues>(
                    new DW14.SizeRelativeVerticallyValues(relFrom)),
            };
            anchor.AppendChild(rh);
        }
    }

    private static DW.HorizontalRelativePositionValues ParseHorizontalRelative(string value) =>
        value.ToLowerInvariant() switch
        {
            "page" => DW.HorizontalRelativePositionValues.Page,
            "column" => DW.HorizontalRelativePositionValues.Column,
            "character" => DW.HorizontalRelativePositionValues.Character,
            "margin" => DW.HorizontalRelativePositionValues.Margin,
            "leftmargin" => DW.HorizontalRelativePositionValues.LeftMargin,
            "rightmargin" => DW.HorizontalRelativePositionValues.RightMargin,
            "insidemargin" => DW.HorizontalRelativePositionValues.InsideMargin,
            "outsidemargin" => DW.HorizontalRelativePositionValues.OutsideMargin,
            _ => throw new ArgumentException($"Invalid horizontal relative position: '{value}'. Valid values: margin, page, column, character, leftMargin, rightMargin, insideMargin, outsideMargin.")
        };

    private static DW.VerticalRelativePositionValues ParseVerticalRelative(string value) =>
        value.ToLowerInvariant() switch
        {
            "page" => DW.VerticalRelativePositionValues.Page,
            "paragraph" => DW.VerticalRelativePositionValues.Paragraph,
            "line" => DW.VerticalRelativePositionValues.Line,
            "margin" => DW.VerticalRelativePositionValues.Margin,
            "topmargin" => DW.VerticalRelativePositionValues.TopMargin,
            "bottommargin" => DW.VerticalRelativePositionValues.BottomMargin,
            "insidemargin" => DW.VerticalRelativePositionValues.InsideMargin,
            "outsidemargin" => DW.VerticalRelativePositionValues.OutsideMargin,
            _ => throw new ArgumentException($"Invalid vertical relative position: '{value}'. Valid values: margin, page, paragraph, line, topMargin, bottomMargin, insideMargin, outsideMargin.")
        };

    private static string GetDrawingInfo(Drawing drawing)
    {
        var docProps = drawing.Descendants<DW.DocProperties>().FirstOrDefault();
        var extent = drawing.Descendants<DW.Extent>().FirstOrDefault();

        var parts = new List<string>();
        if (docProps?.Description?.Value is string desc && !string.IsNullOrEmpty(desc))
            parts.Add($"alt=\"{desc}\"");
        else if (docProps?.Name?.Value is string name && !string.IsNullOrEmpty(name))
            parts.Add($"name=\"{name}\"");
        if (extent != null)
        {
            var wCm = extent.Cx != null ? $"{extent.Cx.Value / EmuConverter.EmuPerCmF:F1}cm" : "?";
            var hCm = extent.Cy != null ? $"{extent.Cy.Value / EmuConverter.EmuPerCmF:F1}cm" : "?";
            parts.Add($"{wCm}×{hCm}");
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "unknown";
    }

    private DocumentNode CreateImageNode(Drawing drawing, Run run, string path)
    {
        var docProps = drawing.Descendants<DW.DocProperties>().FirstOrDefault();
        var extent = drawing.Descendants<DW.Extent>().FirstOrDefault();

        var node = new DocumentNode
        {
            Path = path,
            Type = "picture",
            Text = docProps?.Description?.Value ?? docProps?.Name?.Value ?? ""
        };
        if (docProps?.Id?.HasValue == true) node.Format["id"] = docProps.Id.Value;
        if (docProps?.Name?.Value != null) node.Format["name"] = docProps.Name.Value;
        // BUG-DUMP-R28-PICHIDDEN: <wp:docPr hidden="1"> marks a drawing invisible.
        // A footer/background logo set is commonly two overlapping anchored
        // pictures — a visible colour logo and a hidden monochrome print variant
        // (LogoColour + LogoMono). Without reading the flag back the dump dropped
        // it, so the mono picture replayed visible and rendered (black) over the
        // colour logo. Surface it so AddPicture restores the hidden state.
        if (docProps?.Hidden?.Value == true) node.Format["hidden"] = true;
        if (extent?.Cx != null) node.Format["width"] = $"{extent.Cx.Value / EmuConverter.EmuPerCmF:F1}cm";
        if (extent?.Cy != null) node.Format["height"] = $"{extent.Cy.Value / EmuConverter.EmuPerCmF:F1}cm";
        if (docProps?.Description?.Value != null) node.Format["alt"] = docProps.Description.Value;

        // BUG-DUMP-R51-1: a click-hyperlink on the image — <a:hlinkClick r:id="…">
        // on the picture's <pic:cNvPr> (NonVisualDrawingProperties.HyperlinkOnClick),
        // or the same on <wp:docPr> (DW.DocProperties.HyperlinkOnClick) — makes the
        // image clickable (e.g. a logo linking to a URL). The dump never read it
        // back, so a clickable image became a plain image. Resolve the r:id through
        // the drawing's host part relationships to the external Target URL and
        // surface it as Format["link"]; an internal anchor (w:anchor, no r:id) is
        // surfaced as the bare anchor string. AddPicture re-creates the rel +
        // hlinkClick from this key.
        var picCNvPr = drawing.Descendants<PIC.NonVisualDrawingProperties>().FirstOrDefault();
        var hlinkClick = picCNvPr?.HyperlinkOnClick ?? docProps?.HyperlinkOnClick;
        if (hlinkClick != null)
        {
            var hlinkRelId = hlinkClick.Id?.Value;
            if (!string.IsNullOrEmpty(hlinkRelId))
            {
                // The hlinkClick always targets through an r:id relationship.
                // Resolve it against whichever part hosts this drawing's
                // relationships (document / header / footer / footnotes / …); the
                // Target is the external URL or, for an internal nav, "#anchor".
                var rel = ResolveHyperlinkRelationship(drawing, hlinkRelId);
                if (rel?.Uri != null)
                    node.Format["link"] = rel.Uri.OriginalString;
            }
        }

        // Surface the backing image part rel id so `get --save <path>`
        // and other downstream consumers can locate the payload without
        // re-walking the Drawing tree.
        var imgBlip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
        if (imgBlip?.Embed?.Value != null)
            node.Format["relId"] = imgBlip.Embed.Value;

        // Mirror the brightness/contrast write encoding from
        // WordHandler.Set.Element.cs:748-781: lumOff carries brightness
        // (lumOff/1000 in -100..100) and lumMod carries contrast
        // ((lumMod-100000)/1000 in -100..100). Defaults (lumOff=0,
        // lumMod=100000) mean "no change" — only surface keys when the
        // stored values differ from those defaults so an untouched picture
        // doesn't gain spurious brightness=0/contrast=0 Format entries.
        if (imgBlip != null)
        {
            var lumOffEl = imgBlip.Elements<DocumentFormat.OpenXml.Drawing.LuminanceOffset>().FirstOrDefault();
            if (lumOffEl?.Val?.Value is int lumOffVal && lumOffVal != 0)
                node.Format["brightness"] = (lumOffVal / 1000).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lumModEl = imgBlip.Elements<DocumentFormat.OpenXml.Drawing.LuminanceModulation>().FirstOrDefault();
            if (lumModEl?.Val?.Value is int lumModVal && lumModVal != 100000)
                node.Format["contrast"] = ((lumModVal - 100000) / 1000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // BUG-DUMP-CROP: surface the picture's crop rectangle <a:srcRect>
        // (inside <a:blipFill>) so dump→batch round-trips the crop. Set already
        // writes srcRect (WordHandler.Set.Element.cs `crop` case: @l/@t/@r/@b are
        // 1000ths-of-a-percent, input is percent l,t,r,b) but Get/dump never read
        // it back, so the rebuilt image was uncropped. Emit the percent l,t,r,b
        // string that the Set `crop` input accepts (e.g. "12,6,18,9"); the
        // picture replay re-applies it via a follow-up `set <pic> crop=…`
        // (WordBatchEmitter.Paragraph.cs picture-emit). srcRect with all-zero
        // sides is the Set "no crop" form and is never written, so a present
        // srcRect always carries at least one non-zero side.
        var srcRect = drawing.Descendants<A.SourceRectangle>().FirstOrDefault();
        if (srcRect != null)
        {
            static string CropPct(int thousandths) =>
                (thousandths / 1000.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            int l = srcRect.Left?.Value ?? 0;
            int t = srcRect.Top?.Value ?? 0;
            int r = srcRect.Right?.Value ?? 0;
            int b = srcRect.Bottom?.Value ?? 0;
            if (l != 0 || t != 0 || r != 0 || b != 0)
                node.Format["crop"] = $"{CropPct(l)},{CropPct(t)},{CropPct(r)},{CropPct(b)}";
        }

        // Distinguish inline from floating (anchor) and, for anchors, expose
        // the wrap mode, position offsets, and behind-text flag so callers
        // can inspect how the image is laid out.
        var inlineEl = drawing.GetFirstChild<DW.Inline>();
        var anchorEl = drawing.GetFirstChild<DW.Anchor>();
        if (inlineEl != null)
        {
            node.Format["wrap"] = "inline";
        }
        else if (anchorEl != null)
        {
            // Surface anchor=true so dump→batch round-trip recreates a
            // floating picture. AddPicture's wrapImpliesAnchor heuristic
            // is false for wrap=none, so without this explicit flag the
            // replay produces an inline picture (BUG-R6-1).
            node.Format["anchor"] = true;
            node.Format["wrap"] = DetectWrapType(anchorEl);
            // BUG-DUMP-WRAPSIDE: wrapSquare carries a wrapText side (left/right/
            // largest/bothSides) controlling which sides text flows past the
            // float. DetectWrapType returns only the type, and the apply path
            // hardcoded bothSides — so a right-floated image with text wrapping
            // on its left ONLY ("left") rebuilt as bothSides, reflowing the
            // surrounding text and pushing the page. Capture the side so it
            // round-trips. (tight/through carry their side in wrap.polygon below.)
            var sqWrapText = anchorEl.GetFirstChild<DW.WrapSquare>()?.WrapText;
            if (sqWrapText?.Value is { } sqv && sqv != DW.WrapTextValues.BothSides)
                node.Format["wrap.side"] =
                    sqv == DW.WrapTextValues.Left ? "left"
                    : sqv == DW.WrapTextValues.Right ? "right"
                    : sqv == DW.WrapTextValues.Largest ? "largest" : "bothSides";
            if (anchorEl.BehindDoc?.Value == true)
                node.Format["behindText"] = true;
            // BUG-R24-WRAPPOLY: a wrapTight / wrapThrough wrap carries a custom
            // <wp:wrapPolygon> whose vertices define the exact text-flow boundary.
            // The apply path (BuildAnchorWrap) hardcoded the default full-bounds
            // polygon (0,0 21600,21600), so a source polygon that hugs the image
            // tighter (e.g. y-max 20750 ≈ 96% height) was replaced by the full
            // square — extending the wrap boundary ~4% of the image height and
            // shifting wrapped/below text by several px (a header logo pushed the
            // whole body down 5px). Capture the polygon verbatim (edited flag +
            // wrapText side + "x,y x,y …" vertices) so the apply path can rebuild
            // the exact boundary. Only present for tight/through.
            var wrapPolyEl = anchorEl.GetFirstChild<DW.WrapTight>()?.WrapPolygon
                          ?? anchorEl.GetFirstChild<DW.WrapThrough>()?.WrapPolygon;
            if (wrapPolyEl != null)
            {
                var verts = new List<string>();
                if (wrapPolyEl.StartPoint is { } sp)
                    verts.Add($"{sp.X?.Value ?? 0},{sp.Y?.Value ?? 0}");
                foreach (var lt in wrapPolyEl.Elements<DW.LineTo>())
                    verts.Add($"{lt.X?.Value ?? 0},{lt.Y?.Value ?? 0}");
                if (verts.Count > 0)
                {
                    var edited = wrapPolyEl.Edited?.Value == true ? "1" : "0";
                    var side = (anchorEl.GetFirstChild<DW.WrapTight>()?.WrapText
                             ?? anchorEl.GetFirstChild<DW.WrapThrough>()?.WrapText)?.InnerText ?? "bothSides";
                    node.Format["wrap.polygon"] = $"{edited};{side};{string.Join(" ", verts)}";
                }
            }
            // BUG-DUMP-R26-1: capture the anchor's z-order (relativeHeight).
            // Distinct values (251664384, 251665408, …) sequence overlapping
            // floats front-to-back; dump never read it and the apply path
            // hardcoded RelativeHeight=1U, collapsing every image to the same
            // z-plane. Surface the raw uint so the Add path round-trips it.
            if (anchorEl.RelativeHeight?.HasValue == true)
                node.Format["relativeHeight"] = anchorEl.RelativeHeight.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // BUG-DUMP-NAR-WP14: capture the wp14 relative-sizing extensions so
            // the Add path can re-attach them. Emitted as "relativeFrom;pct"
            // (e.g. "page;0"). Without this, anchored pictures sized relative to
            // the page lost the extension and re-rendered at absolute size,
            // shifting layout and pagination.
            var relW = anchorEl.GetFirstChild<DW14.RelativeWidth>();
            if (relW != null)
            {
                var pctW = relW.GetFirstChild<DW14.PercentageWidth>()?.InnerText ?? "0";
                node.Format["sizeRelH"] = $"{relW.ObjectId?.InnerText ?? "page"};{pctW}";
            }
            var relH = anchorEl.GetFirstChild<DW14.RelativeHeight>();
            if (relH != null)
            {
                var pctH = relH.GetFirstChild<DW14.PercentageHeight>()?.InnerText ?? "0";
                node.Format["sizeRelV"] = $"{relH.RelativeFrom?.InnerText ?? "page"};{pctH}";
            }

            var hPos = anchorEl.GetFirstChild<DW.HorizontalPosition>();
            if (hPos != null)
            {
                // A floating axis positions EITHER by an absolute <wp:posOffset>
                // OR by a relative <wp:align> keyword (left/center/right). The
                // align form was dropped on dump, so every aligned picture
                // rebuilt with posOffset=0 (collapsed to the margin origin;
                // right-aligned floats stacked under the left ones). Surface the
                // align keyword too so the Add path can reproduce it.
                var alignEl = hPos.GetFirstChild<DW.HorizontalAlignment>();
                if (alignEl != null && !string.IsNullOrEmpty(alignEl.Text))
                    node.Format["hAlign"] = alignEl.Text;
                var offset = hPos.GetFirstChild<DW.PositionOffset>();
                // BUG-R7-11: skip zero-valued offsets. AddPicture defaults the
                // PositionOffset to 0 when no hPosition prop is given, so a
                // dump that originally omitted hPosition would jitter to
                // hPosition=0.0cm after round-trip. Treat 0 as "no
                // positional override" to keep dump→batch idempotent.
                if (offset != null && long.TryParse(offset.Text, out var hEmu) && hEmu != 0)
                    node.Format["hPosition"] = $"{hEmu / EmuConverter.EmuPerCmF:F1}cm";
                if (hPos.RelativeFrom?.HasValue == true)
                    node.Format["hRelative"] = hPos.RelativeFrom.InnerText;
            }

            var vPos = anchorEl.GetFirstChild<DW.VerticalPosition>();
            if (vPos != null)
            {
                // See hAlign note above — capture the vertical <wp:align>
                // keyword (top/bottom/center/inside/outside) too.
                var alignEl = vPos.GetFirstChild<DW.VerticalAlignment>();
                if (alignEl != null && !string.IsNullOrEmpty(alignEl.Text))
                    node.Format["vAlign"] = alignEl.Text;
                var offset = vPos.GetFirstChild<DW.PositionOffset>();
                // BUG-R7-11: see hPosition note above.
                if (offset != null && long.TryParse(offset.Text, out var vEmu) && vEmu != 0)
                    node.Format["vPosition"] = $"{vEmu / EmuConverter.EmuPerCmF:F1}cm";
                if (vPos.RelativeFrom?.HasValue == true)
                    node.Format["vRelative"] = vPos.RelativeFrom.InnerText;
            }
        }

        return node;
    }

    // BUG-DUMP-CROP: shared crop → <a:srcRect> writer used by both Set
    // (WordHandler.Set.Element.cs `crop` case) and Add (AddPicture's property
    // pass), so dump→batch round-trips a cropped image without diverging
    // encodings. `crop` takes 1 or 4 comma-separated percentages (l,t,r,b);
    // the side variants (cropleft/croptop/cropright/cropbottom) take a single
    // percentage. Values are stored as 1000ths-of-a-percent on @l/@t/@r/@b. An
    // all-zero rectangle is the "no crop" form and removes the element. Returns
    // false for an unrecognised key so the caller can report it unsupported.
    internal static bool ApplyCropToBlipFill(PIC.BlipFill blipFill, string key, string value)
    {
        static string StripPct(string s)
        {
            var t = s.Trim();
            return t.EndsWith("%", StringComparison.Ordinal) ? t[..^1].Trim() : t;
        }
        var lk = key.ToLowerInvariant();
        if (lk is not ("crop" or "cropleft" or "cropright" or "croptop" or "cropbottom"))
            return false;
        var srcRect = blipFill.GetFirstChild<A.SourceRectangle>();
        if (srcRect == null)
        {
            srcRect = new A.SourceRectangle();
            // CONSISTENCY(ooxml-element-order): srcRect precedes the fill-mode element.
            var fillMode = (OpenXmlElement?)blipFill.GetFirstChild<A.Stretch>()
                ?? blipFill.GetFirstChild<A.Tile>();
            if (fillMode != null) blipFill.InsertBefore(srcRect, fillMode);
            else blipFill.AppendChild(srcRect);
        }
        if (lk == "crop")
        {
            var parts = value.Split(',');
            if (parts.Length == 4)
            {
                var cv = new double[4];
                for (int ci = 0; ci < 4; ci++)
                {
                    cv[ci] = ParseHelpers.SafeParseDouble(StripPct(parts[ci]), "crop");
                    // Negative srcRect values are legal (ST_Percentage): they
                    // EXPAND the canvas beyond the bitmap (Word's "crop out").
                    // Real documents carry e.g. -2.934; mirror the pptx range.
                    if (cv[ci] < -1000 || cv[ci] > 1000)
                        throw new ArgumentException($"Invalid 'crop' value: '{parts[ci].Trim()}'. Crop percentage must be between -1000 and 1000.");
                }
                srcRect.Left = (int)(cv[0] * 1000);
                srcRect.Top = (int)(cv[1] * 1000);
                srcRect.Right = (int)(cv[2] * 1000);
                srcRect.Bottom = (int)(cv[3] * 1000);
            }
            else if (parts.Length == 1)
            {
                if (!double.TryParse(StripPct(value), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cv1)
                    || cv1 < -1000 || cv1 > 1000)
                    throw new ArgumentException($"Invalid 'crop' value: '{value}'. Expected percentage -1000..1000.");
                var pctAll = (int)(cv1 * 1000);
                srcRect.Left = pctAll; srcRect.Top = pctAll;
                srcRect.Right = pctAll; srcRect.Bottom = pctAll;
            }
            else
            {
                throw new ArgumentException($"Invalid 'crop' value: '{value}'. Expected 1 or 4 comma-separated percentages.");
            }
        }
        else
        {
            if (!double.TryParse(StripPct(value), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cs1)
                || cs1 < 0 || cs1 > 100)
                throw new ArgumentException($"Invalid '{key}' value: '{value}'. Expected percentage 0-100.");
            var pctSide = (int)(cs1 * 1000);
            switch (lk)
            {
                case "cropleft": srcRect.Left = pctSide; break;
                case "croptop": srcRect.Top = pctSide; break;
                case "cropright": srcRect.Right = pctSide; break;
                case "cropbottom": srcRect.Bottom = pctSide; break;
            }
        }
        int L = srcRect.Left?.Value ?? 0;
        int T = srcRect.Top?.Value ?? 0;
        int R = srcRect.Right?.Value ?? 0;
        int B = srcRect.Bottom?.Value ?? 0;
        if (L == 0 && T == 0 && R == 0 && B == 0)
            srcRect.Remove();
        return true;
    }

    // BUG-DUMP-R45-4: drawingml main namespace (the `a:` prefix the captured
    // blip/spPr effect fragments use, with no xmlns of their own).
    private const string DrawingMainNs = "http://schemas.openxmlformats.org/drawingml/2006/main";

    // Parse a verbatim drawingml fragment that uses the bare `a:` prefix (as
    // captured from the source picture XML, with no xmlns declaration) into a
    // list of detached OpenXmlElements. Wraps the fragment in a temp element
    // that declares xmlns:a so the SDK can resolve the prefix, then detaches
    // the parsed children. Mirrors the OpenXmlUnknownElement+InnerXml pattern in
    // ApplyW14TextEffect.
    private static List<OpenXmlElement> ParseDrawingFragment(string fragment)
    {
        var holder = new OpenXmlUnknownElement("a", "tmpFragmentHolder", DrawingMainNs);
        holder.InnerXml = fragment;
        var children = holder.ChildElements.ToList();
        foreach (var c in children) c.Remove();
        return children;
    }

    // BUG-DUMP-R45-4: re-inject the source <a:blip>'s recolor/alpha children
    // (duotone / biLevel / alphaModFix / lum* / clrChange) the fixed
    // CreateImageRun/CreateAnchorImageRun blip rebuild dropped. The r:embed is an
    // attribute on the rebuilt <a:blip> and is preserved; we only append the
    // captured inner children. No-op if the run has no blip (defensive).
    private static void ApplyBlipEffects(Run imgRun, string blipInnerXml)
    {
        var blip = imgRun.Descendants<A.Blip>().FirstOrDefault();
        if (blip == null) return;
        var children = ParseDrawingFragment(blipInnerXml);
        // CT_Blip's child group precedes the extLst; the captured fragment is
        // the source order verbatim, so append in sequence. AppendChild keeps
        // them after any pre-existing child (a fresh blip has none).
        foreach (var c in children)
            blip.AppendChild(c);
    }

    // BUG-DUMP-R45-4: re-inject the source <pic:spPr>'s <a:effectLst> (drop
    // shadow / glow / reflection) the fixed spPr rebuild dropped. Per
    // CT_ShapeProperties order, effectLst follows the geometry/fill/line group,
    // so insert it AFTER the last of xfrm/custGeom/prstGeom/fill/ln that exists
    // (in practice immediately after <a:prstGeom>). No-op if the run has no
    // ShapeProperties (defensive).
    // Whole-<pic:spPr> verbatim replacement: the fixed rebuild loses xfrm
    // flip flags (mirrored logos), a content extent that legitimately differs
    // from the frame's wp:extent, bwMode, and explicit <a:noFill/>/<a:ln>
    // blocks. Swap the rebuilt spPr for the captured source block; the
    // r:embed lives on <a:blip> inside blipFill, untouched here.
    // Whole-<pic:spPr> verbatim replacement: the fixed rebuild loses xfrm
    // flip flags (mirrored logos), a content extent that legitimately differs
    // from the frame's wp:extent, bwMode, and explicit <a:noFill/>/<a:ln>
    // blocks. Swap the rebuilt spPr for the captured source block; the
    // r:embed lives on <a:blip> inside blipFill, untouched here. The captured
    // fragment has no xmlns declarations (it was cut out of document.xml), so
    // stamp the standard prefixes onto the root tag before parsing; an exotic
    // undeclared prefix makes the parse throw and we keep the rebuilt spPr.
    private static void ApplySpPrVerbatim(Run imgRun, string spPrXml)
    {
        var spPr = imgRun.Descendants<PIC.ShapeProperties>().FirstOrDefault();
        if (spPr?.Parent == null) return;
        const string NsDecls =
            " xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"" +
            " xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"" +
            " xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"" +
            " xmlns:a14=\"http://schemas.microsoft.com/office/drawing/2010/main\"";
        // BUG-DUMP-R72-SPPR-IDEMPOTENT: dump→batch is not a fixpoint for the
        // verbatim picture spPr. On the first round-trip the captured fragment
        // (cut from document.xml) has no xmlns:pic/a/r/a14 of its own, so blindly
        // prepending NsDecls is fine. But the SDK serializes the rebuilt spPr WITH
        // those decls, so a *second* dump captures a fragment that already carries
        // them; prepending again yields a duplicate xmlns:pic attribute — invalid
        // XML — and `new PIC.ShapeProperties(withNs)` throws, the catch swallows it,
        // and the picture silently falls back to the generic rebuilt spPr (losing
        // bwMode, xfrm flip flags, custom content extent, noFill, …). Strip the four
        // decls we are about to add from the ROOT open tag first so re-stamping is
        // duplicate-safe and the round-trip converges. Other prefixes the content
        // genuinely needs (a16, adec, …) are left untouched.
        var rootTag = new System.Text.RegularExpressions.Regex(@"^(\s*<pic:spPr)([^>]*?)(/?>)");
        var withNs = rootTag.Replace(spPrXml, m =>
        {
            var attrs = System.Text.RegularExpressions.Regex.Replace(
                m.Groups[2].Value,
                " xmlns:(?:pic|a|r|a14)=\"[^\"]*\"", string.Empty);
            return m.Groups[1].Value + NsDecls + attrs + m.Groups[3].Value;
        }, 1);
        // Fallback for the (defensive) case the root-tag anchor did not match.
        if (!withNs.Contains(" xmlns:pic="))
            withNs = new System.Text.RegularExpressions.Regex("<pic:spPr")
                .Replace(spPrXml, "<pic:spPr" + NsDecls, 1);
        PIC.ShapeProperties fresh;
        try { fresh = new PIC.ShapeProperties(withNs); }
        catch { return; }
        spPr.InsertAfterSelf(fresh);
        spPr.Remove();
    }

    private static void ApplySpPrEffects(Run imgRun, string effectLstXml)
    {
        var spPr = imgRun.Descendants<PIC.ShapeProperties>().FirstOrDefault();
        if (spPr == null) return;
        // Don't double-inject if an effectLst is somehow already present.
        if (spPr.GetFirstChild<A.EffectList>() != null) return;
        var children = ParseDrawingFragment(effectLstXml);
        // The anchor element after which effectLst belongs: the last present of
        // the preceding-group elements (geometry/fill/line). prstGeom is always
        // emitted by the rebuild; fall back to the last child otherwise.
        OpenXmlElement? anchor =
            (OpenXmlElement?)spPr.GetFirstChild<A.PresetGeometry>()
            ?? spPr.GetFirstChild<A.CustomGeometry>()
            ?? (OpenXmlElement?)spPr.GetFirstChild<A.Transform2D>()
            ?? spPr.LastChild;
        foreach (var c in children)
        {
            if (anchor != null)
            {
                anchor.InsertAfterSelf(c);
                anchor = c;
            }
            else
            {
                spPr.AppendChild(c);
            }
        }
    }

    private static string DetectWrapType(DW.Anchor anchor)
    {
        if (anchor.GetFirstChild<DW.WrapNone>() != null) return "none";
        if (anchor.GetFirstChild<DW.WrapSquare>() != null) return "square";
        if (anchor.GetFirstChild<DW.WrapTight>() != null) return "tight";
        if (anchor.GetFirstChild<DW.WrapThrough>() != null) return "through";
        if (anchor.GetFirstChild<DW.WrapTopBottom>() != null) return "topandbottom";
        return "none";
    }

    private static void ReplaceWrapElement(DW.Anchor anchor, string wrapType)
    {
        // Remove any existing wrap element first — at most one is allowed.
        anchor.GetFirstChild<DW.WrapNone>()?.Remove();
        anchor.GetFirstChild<DW.WrapSquare>()?.Remove();
        anchor.GetFirstChild<DW.WrapTight>()?.Remove();
        anchor.GetFirstChild<DW.WrapThrough>()?.Remove();
        anchor.GetFirstChild<DW.WrapTopBottom>()?.Remove();

        OpenXmlElement newWrap = wrapType.ToLowerInvariant() switch
        {
            "square" => new DW.WrapSquare { WrapText = DW.WrapTextValues.BothSides },
            // WrapText is REQUIRED on wrapTight/wrapThrough (same as wrapSquare);
            // omitting it produces schema-invalid XML that real Word refuses to
            // open. Default to bothSides (matches Word's default wrap side).
            "tight" => new DW.WrapTight(new DW.WrapPolygon(
                new DW.StartPoint { X = 0, Y = 0 },
                new DW.LineTo { X = 21600, Y = 0 },
                new DW.LineTo { X = 21600, Y = 21600 },
                new DW.LineTo { X = 0, Y = 21600 },
                new DW.LineTo { X = 0, Y = 0 }
            ) { Edited = false }) { WrapText = DW.WrapTextValues.BothSides },
            "through" => new DW.WrapThrough(new DW.WrapPolygon(
                new DW.StartPoint { X = 0, Y = 0 },
                new DW.LineTo { X = 21600, Y = 0 },
                new DW.LineTo { X = 21600, Y = 21600 },
                new DW.LineTo { X = 0, Y = 21600 },
                new DW.LineTo { X = 0, Y = 0 }
            ) { Edited = false }) { WrapText = DW.WrapTextValues.BothSides },
            "topandbottom" or "topbottom" => new DW.WrapTopBottom(),
            "none" => new DW.WrapNone(),
            _ => throw new ArgumentException(
                $"Invalid wrap value: '{wrapType}'. Valid values: none, square, tight, through, topandbottom.")
        };

        // Insert after EffectExtent (standard OOXML child order for
        // CT_Anchor — PowerPoint and Word silently drop wrap elements
        // placed out of schema order).
        var effectExtent = anchor.GetFirstChild<DW.EffectExtent>();
        if (effectExtent != null)
            effectExtent.InsertAfterSelf(newWrap);
        else
            anchor.PrependChild(newWrap);
    }

    /// <summary>
    /// Resolve a run to its top-level Drawing + Anchor, if the run wraps a
    /// floating picture. Used by Set.cs wrap/position cases so the six
    /// wrap/position properties share one lookup instead of each case
    /// re-running the same GetFirstChild chain.
    /// </summary>
    private static DW.Anchor? ResolveRunAnchor(Run run)
    {
        var drawing = run.GetFirstChild<Drawing>();
        return drawing?.GetFirstChild<DW.Anchor>();
    }

    // ==================== OLE Object Reading ====================
    //
    // Embedded OLE objects live inside <w:object> (EmbeddedObject). A VML
    // <v:shape> child carries the display box ("style=width:Xpt;height:Ypt")
    // and an <o:OLEObject> child carries the ProgID. These elements come
    // through as OpenXmlUnknownElement because they are not strongly typed
    // in the core wordprocessing namespace, so we walk descendants by
    // LocalName rather than by CLR type.

    private DocumentNode CreateOleNode(EmbeddedObject oleObj, Run run, string path)
        => CreateOleNode(oleObj, run, path, _doc.MainDocumentPart);

    // BUG-R10-02: OLE inside HeaderPart/FooterPart stores its relationship
    // on the header/footer part itself — not on MainDocumentPart. When we
    // tried to resolve the rel id against MainDocumentPart, GetPartById
    // threw and the node was marked orphan (no contentType/fileSize).
    // Callers in header/footer iteration must pass the enclosing HeaderPart
    // or FooterPart so the lookup succeeds.
    private DocumentNode CreateOleNode(EmbeddedObject oleObj, Run run, string path, OpenXmlPart? hostPart)
    {
        var node = new DocumentNode
        {
            Path = path,
            Type = "ole",
            Text = ""
        };
        node.Format["objectType"] = "ole";

        // ProgID + backing part rel id live on the nested o:OLEObject element.
        // The rel id ("r:id") points to the EmbeddedObjectPart / EmbeddedPackagePart
        // that holds the binary payload — follow it so we can surface content
        // type and byte length in the node, matching how media/image nodes are
        // enriched elsewhere in this handler.
        var oleElement = oleObj.Descendants().FirstOrDefault(e => e.LocalName == "OLEObject");
        string? progId = null;
        string? relId = null;
        string? drawAspect = null;
        if (oleElement != null)
        {
            foreach (var attr in oleElement.GetAttributes())
            {
                if (attr.LocalName == "ProgID")
                    progId = attr.Value;
                else if (attr.LocalName == "DrawAspect")
                    drawAspect = attr.Value;
                else if (attr.LocalName == "id"
                    && attr.NamespaceUri == "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
                    relId = attr.Value;
            }
        }
        // CONSISTENCY(ole-name): PPT OLE Get surfaces oleObj.Name as
        // Format["name"]. Word has no equivalent attribute on o:OLEObject
        // (VML CT_OleObject has no Name), so AddOle/Set store the friendly
        // name on the surrounding v:shape@alt attribute. Read it back from
        // the same place so Add → Get → Format["name"] round-trips.
        var shapeForName = oleObj.Descendants().FirstOrDefault(e => e.LocalName == "shape");
        if (shapeForName != null)
        {
            var altAttr = shapeForName.GetAttributes().FirstOrDefault(a => a.LocalName == "alt");
            if (!string.IsNullOrEmpty(altAttr.Value))
                node.Format["name"] = altAttr.Value;
        }
        // CONSISTENCY(ole-display): PPT OLE Get returns display=icon when the
        // object is shown as an icon; Word stores the same bit in the
        // o:OLEObject DrawAspect attribute ("Icon" vs "Content"). Normalize
        // to the same lowercase "icon"/"content" vocabulary.
        if (!string.IsNullOrEmpty(drawAspect))
        {
            node.Format["display"] = drawAspect.Equals("Content", StringComparison.OrdinalIgnoreCase)
                ? "content"
                : "icon";
        }
        if (!string.IsNullOrEmpty(progId))
        {
            node.Format["progId"] = progId;
            node.Text = progId;
        }
        if (!string.IsNullOrEmpty(relId))
        {
            node.Format["relId"] = relId;
            // GetPartById throws ArgumentOutOfRangeException when the rel id
            // is not present in the part's relationships — this can happen
            // if the document was hand-edited or partially corrupted. Degrade
            // gracefully by marking the node orphan and skipping enrichment,
            // rather than propagating the crash up through Query.
            try
            {
                var part = hostPart?.GetPartById(relId);
                if (part != null)
                    OfficeCli.Core.OleHelper.PopulateFromPart(node, part, progId);
                else
                    node.Format["orphan"] = true;
            }
            catch (ArgumentOutOfRangeException)
            {
                node.Format["orphan"] = true;
            }
            catch (KeyNotFoundException)
            {
                node.Format["orphan"] = true;
            }
        }

        // Display size lives on the VML v:shape element's style string.
        var shape = oleObj.Descendants().FirstOrDefault(e => e.LocalName == "shape");
        if (shape != null)
        {
            var styleAttr = shape.GetAttributes().FirstOrDefault(a => a.LocalName == "style");
            if (!string.IsNullOrEmpty(styleAttr.Value))
                ParseVmlStyle(styleAttr.Value, node);
        }

        return node;
    }

    /// <summary>
    /// Replace a single dimension (width|height) in a VML v:shape style
    /// string, preserving all other key:value pairs. If the key is not
    /// present, it's appended. Output is the re-joined "k1:v1;k2:v2" form.
    /// </summary>
    internal static string ReplaceVmlStyleDimension(string style, string dimKey, string newValue)
    {
        var parts = (style ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
        var rebuilt = new List<string>();
        var replaced = false;
        foreach (var part in parts)
        {
            var kv = part.Split(':', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals(dimKey, StringComparison.OrdinalIgnoreCase))
            {
                rebuilt.Add($"{kv[0].Trim()}:{newValue}");
                replaced = true;
            }
            else
            {
                rebuilt.Add(part.Trim());
            }
        }
        if (!replaced) rebuilt.Add($"{dimKey}:{newValue}");
        return string.Join(";", rebuilt);
    }

    private static void ParseVmlStyle(string style, DocumentNode node)
    {
        foreach (var part in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', 2);
            if (kv.Length != 2) continue;
            var k = kv[0].Trim().ToLowerInvariant();
            var v = kv[1].Trim();
            if (k == "width") node.Format["width"] = ConvertVmlLengthToCm(v);
            else if (k == "height") node.Format["height"] = ConvertVmlLengthToCm(v);
        }
    }

    private static readonly System.Text.RegularExpressions.Regex _vmlLengthRegex =
        new(@"^\s*([+-]?\d+(?:\.\d+)?)\s*(pt|in|cm|mm|px)?\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    /// <summary>
    /// Convert a VML length literal (e.g. "385.45pt", "2in", "5cm") into
    /// a "Xcm" string matching the picture width/height format. Uses a
    /// regex to split number from unit so that values containing the
    /// substring "in" (like "line:") inside larger tokens can never be
    /// mangled by naive string.Replace calls.
    /// </summary>
    private static string ConvertVmlLengthToCm(string length)
    {
        var m = _vmlLengthRegex.Match(length);
        if (!m.Success) return length;

        if (!double.TryParse(m.Groups[1].Value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value))
            return length;

        var unit = m.Groups[2].Success ? m.Groups[2].Value.ToLowerInvariant() : "pt";
        double cm = unit switch
        {
            "pt" => value * 2.54 / 72.0,
            "in" => value * 2.54,
            "cm" => value,
            "mm" => value / 10.0,
            "px" => value * 2.54 / 96.0,
            _ => value * 2.54 / 72.0,
        };
        return $"{cm:0.##}cm";
    }
}
