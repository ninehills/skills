// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{

    /// <summary>
    /// True when a table cell's text body carries formatting that the plain
    /// `set tc text=...` rebuild path would drop: a non-empty &lt;a:lstStyle&gt;,
    /// any paragraph &lt;a:pPr&gt; with children (lnSpc/spc/bu*/tabLst/defRPr),
    /// any run &lt;a:rPr&gt; with children (ea/latin/solidFill), or an
    /// &lt;a:endParaRPr&gt; with children. Used to gate the verbatim txBodyRaw
    /// capture so trivial empty-seed cells keep round-tripping through text=.
    /// </summary>
    private static bool CellTextBodyHasRichContent(Drawing.TextBody body)
    {
        var lstStyle = body.GetFirstChild<Drawing.ListStyle>();
        if (lstStyle != null && lstStyle.HasChildren) return true;
        foreach (var para in body.Elements<Drawing.Paragraph>())
        {
            var pPr = para.ParagraphProperties;
            if (pPr != null && pPr.HasChildren) return true;
            foreach (var run in para.Elements<Drawing.Run>())
            {
                var rPr = run.RunProperties;
                if (rPr != null && rPr.HasChildren) return true;
            }
            var endRPr = para.GetFirstChild<Drawing.EndParagraphRunProperties>();
            if (endRPr != null && endRPr.HasChildren) return true;
        }
        return false;
    }

    /// <summary>
    /// Read table cell border properties.
    /// Maps a:lnL/lnR/lnT/lnB → border.left, border.right, border.top, border.bottom in Format.
    /// </summary>
    private static void ReadTableCellBorders(Drawing.TableCellProperties tcPr, DocumentNode node)
    {
        ReadBorderLine(tcPr.LeftBorderLineProperties, "border.left", node);
        ReadBorderLine(tcPr.RightBorderLineProperties, "border.right", node);
        ReadBorderLine(tcPr.TopBorderLineProperties, "border.top", node);
        ReadBorderLine(tcPr.BottomBorderLineProperties, "border.bottom", node);
        ReadBorderLine(tcPr.TopLeftToBottomRightBorderLineProperties, "border.tl2br", node);
        ReadBorderLine(tcPr.BottomLeftToTopRightBorderLineProperties, "border.tr2bl", node);

        // Verbatim border-line passthrough. The granular border.<edge>.* keys
        // model only color/width/dash/compound and SKIP a line entirely when it
        // carries <a:noFill/> (invisible border) — so an intentional invisible
        // border was dropped on rebuild and PowerPoint fell back to DEFAULT
        // VISIBLE borders. The granular path also drops cap/algn attrs and the
        // prstDash/custDash/round/headEnd/tailEnd children. Capture each present
        // border line's OuterXml so the Add/Set border.<edge>.raw key can
        // re-inject it verbatim (attrs + children + the noFill/solidFill choice).
        // CONSISTENCY(border-line-raw-passthrough): mirrors lstStyleRaw / effectsRaw.
        ReadBorderLineRaw(tcPr.LeftBorderLineProperties, "border.left.raw", node);
        ReadBorderLineRaw(tcPr.RightBorderLineProperties, "border.right.raw", node);
        ReadBorderLineRaw(tcPr.TopBorderLineProperties, "border.top.raw", node);
        ReadBorderLineRaw(tcPr.BottomBorderLineProperties, "border.bottom.raw", node);
        ReadBorderLineRaw(tcPr.TopLeftToBottomRightBorderLineProperties, "border.tl2br.raw", node);
        ReadBorderLineRaw(tcPr.BottomLeftToTopRightBorderLineProperties, "border.tr2bl.raw", node);
        // border.all summary when all four edges are uniform — schema declares
        // it as a gettable convenience alongside the per-edge keys.
        if (node.Format.TryGetValue("border.top", out var bt)
            && node.Format.TryGetValue("border.bottom", out var bb)
            && node.Format.TryGetValue("border.left", out var bl)
            && node.Format.TryGetValue("border.right", out var br)
            && Equals(bt, bb) && Equals(bt, bl) && Equals(bt, br))
        {
            node.Format["border.all"] = bt!;
        }
    }

    /// <summary>
    /// Read a single border line's properties (color, width, dash, compound).
    /// Width / dash / compound are emitted independently — a border with only
    /// `w="25400"` (and no SolidFill) still surfaces a `border.width` readback
    /// so callers can see what they wrote. Returns silently only when the
    /// element itself is null, NoFill is set, or none of the child sub-props
    /// (color, width, dash, compound) are present.
    /// </summary>
    private static void ReadBorderLine(OpenXmlCompositeElement? lineProps, string prefix, DocumentNode node)
    {
        if (lineProps == null) return;
        // If NoFill is set, the border is invisible — skip
        if (lineProps.GetFirstChild<Drawing.NoFill>() != null) return;

        // Color (only when a SolidFill is present; gradient/picture borders
        // would need separate handling and aren't surfaced via the simple
        // border.color key).
        string? color = null;
        var solidFill = lineProps.GetFirstChild<Drawing.SolidFill>();
        if (solidFill != null)
        {
            color = ReadColorFromFill(solidFill);
            if (color != null) node.Format[$"{prefix}.color"] = color;
        }

        // Width from "w" attribute (EMU)
        var wAttr = lineProps.GetAttributes().FirstOrDefault(a => a.LocalName == "w");
        bool hasWidth = !string.IsNullOrEmpty(wAttr.Value) && long.TryParse(wAttr.Value, out var wEmu) && wEmu > 0;
        if (hasWidth)
        {
            long.TryParse(wAttr.Value, out var wEmuOut);
            node.Format[$"{prefix}.width"] = FormatEmu(wEmuOut);
        }

        // Dash style from PresetDash
        var dash = lineProps.GetFirstChild<Drawing.PresetDash>();
        bool hasDash = dash?.Val?.HasValue == true;
        if (hasDash)
            node.Format[$"{prefix}.dash"] = dash!.Val!.InnerText;

        // Compound line style (cmpd attribute on the line element).
        var cmpdAttr = lineProps.GetAttributes().FirstOrDefault(a => a.LocalName == "cmpd");
        bool hasCompound = !string.IsNullOrEmpty(cmpdAttr.Value);
        if (hasCompound)
            node.Format[$"{prefix}.compound"] = cmpdAttr.Value!;

        // If none of color / width / dash / compound surfaced, don't emit a
        // summary key — there's nothing meaningful to report.
        if (color is null && !hasWidth && !hasDash && !hasCompound) return;

        // Summary key: "1pt solid FF0000" format for convenience
        var parts = new List<string>();
        if (hasWidth)
        {
            long.TryParse(wAttr.Value, out var wEmu2);
            parts.Add(FormatEmu(wEmu2));
        }
        if (hasDash) parts.Add(dash!.Val!.InnerText!);
        else parts.Add("solid");
        if (color is not null) parts.Add(color);
        node.Format[prefix] = string.Join(" ", parts);
    }

    /// <summary>
    /// Capture a border line element's full OuterXml verbatim (attrs + children)
    /// into the given Format key, so the Add/Set border.&lt;edge&gt;.raw path can
    /// re-inject it without the granular reducer dropping noFill / cap / algn /
    /// prstDash / round / head-tail-end. Emits only when the element is present.
    /// </summary>
    private static void ReadBorderLineRaw(OpenXmlCompositeElement? lineProps, string key, DocumentNode node)
    {
        if (lineProps == null) return;
        node.Format[key] = lineProps.OuterXml;
    }

    // BUG-R6-C: strict GUID format check for direct passthrough.
    // Pattern: {8HEX-4HEX-4HEX-4HEX-12HEX}, ASCII case-insensitive hex only.
    private static readonly System.Text.RegularExpressions.Regex _guidPattern =
        new(@"^\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string ResolveTableStyleId(string value)
    {
        var trimmed = value?.Trim() ?? "";
        // Long-form aliases: mediumstyle1 → medium1
        var alias = System.Text.RegularExpressions.Regex.Replace(
            trimmed, @"^(medium|light|dark)style(\d)", "$1$2",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var guid = OfficeCli.Core.TableStyles.TableStyleRegistry.ShortNameToGuid(alias);
        if (guid != null) return guid;
        if (trimmed.StartsWith("{"))
        {
            if (!_guidPattern.IsMatch(trimmed))
                throw new ArgumentException(
                    $"Invalid table style GUID: '{value}'. Expected pattern {{8HEX-4HEX-4HEX-4HEX-12HEX}}.");
            return trimmed; // Direct GUID passthrough (validated)
        }
        throw new ArgumentException(
            $"Invalid table style: '{value}'. Valid values: medium1..4, light1..3, dark1..2, none, "
            + "compound form like 'dark2-accent1' / 'medium3-accent4', or a direct GUID like {{073A0DAA-...}}.");
    }
}
