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

    // ==================== w14 Text Effects ====================

    private const string W14Ns = "http://schemas.microsoft.com/office/word/2010/wordml";

    /// <summary>
    /// Remove an existing w14 element from RunProperties by local name.
    /// </summary>
    private static void RemoveW14Element(RunProperties rPr, string localName)
    {
        var existing = rPr.ChildElements
            .Where(e => e.LocalName == localName && e.NamespaceUri == W14Ns)
            .ToList();
        foreach (var e in existing) e.Remove();
    }

    /// <summary>
    /// Split a w14 effect value string by ';' (preferred) or '-' (legacy fallback).
    /// ';' is unambiguous; '-' is only used as fallback when no ';' is present.
    /// </summary>
    private static string[] SplitEffectValue(string value) =>
        value.Contains(';') ? value.Split(';') : value.Split('-');

    /// <summary>
    /// Build w14:textOutline XML.
    /// Format: "WIDTH;COLOR" (e.g. "0.5pt;FF0000"), "WIDTH" (defaults to black), or "none"
    /// Width in pt, internally stored in EMU (1pt = 12700 EMU).
    /// Legacy: "WIDTH-COLOR" also accepted.
    /// </summary>
    internal static string BuildW14TextOutline(string value)
    {
        var parts = SplitEffectValue(value);
        var widthPt = ParseHelpers.SafeParseDouble(parts[0].Replace("pt", ""), "textOutline width");
        var widthEmu = (long)(widthPt * EmuConverter.EmuPerPoint);
        var color = parts.Length > 1 ? ParseHelpers.SanitizeColorForOoxml(parts[1]).Rgb : "000000";

        return $@"<w14:textOutline xmlns:w14=""{W14Ns}"" w14:w=""{widthEmu}"" w14:cap=""flat"" w14:cmpd=""sng"" w14:algn=""ctr""><w14:solidFill><w14:srgbClr w14:val=""{color}""/></w14:solidFill><w14:prstDash w14:val=""solid""/></w14:textOutline>";
    }

    /// <summary>
    /// Build w14:textFill XML.
    /// Format: "C1;C2[;ANGLE]" for linear gradient, "radial:C1;C2" for radial, or single color for solid.
    /// Legacy: '-' separator also accepted.
    /// </summary>
    internal static string BuildW14TextFill(string value)
    {
        if (value.StartsWith("radial:", StringComparison.OrdinalIgnoreCase))
        {
            var radParts = SplitEffectValue(value[7..]);
            var (c1, _) = ParseHelpers.SanitizeColorForOoxml(radParts[0]);
            var c2 = radParts.Length > 1 ? ParseHelpers.SanitizeColorForOoxml(radParts[1]).Rgb : c1;
            return $@"<w14:textFill xmlns:w14=""{W14Ns}""><w14:gradFill><w14:gsLst><w14:gs w14:pos=""0""><w14:srgbClr w14:val=""{c1}""/></w14:gs><w14:gs w14:pos=""100000""><w14:srgbClr w14:val=""{c2}""/></w14:gs></w14:gsLst><w14:path w14:path=""circle""><w14:fillToRect w14:l=""50000"" w14:t=""50000"" w14:r=""50000"" w14:b=""50000""/></w14:path></w14:gradFill></w14:textFill>";
        }

        var parts = SplitEffectValue(value);
        if (parts.Length == 1)
        {
            // Solid fill
            var (rgb, _) = ParseHelpers.SanitizeColorForOoxml(parts[0]);
            return $@"<w14:textFill xmlns:w14=""{W14Ns}""><w14:solidFill><w14:srgbClr w14:val=""{rgb}""/></w14:solidFill></w14:textFill>";
        }

        // Linear gradient: C1;C2[;angle]
        var (gc1, _a1) = ParseHelpers.SanitizeColorForOoxml(parts[0]);
        var (gc2, _a2) = ParseHelpers.SanitizeColorForOoxml(parts[1]);
        var angle = parts.Length > 2 ? ParseHelpers.SafeParseInt(parts[2], "textFill angle") * 60000 : 0;
        return $@"<w14:textFill xmlns:w14=""{W14Ns}""><w14:gradFill><w14:gsLst><w14:gs w14:pos=""0""><w14:srgbClr w14:val=""{gc1}""/></w14:gs><w14:gs w14:pos=""100000""><w14:srgbClr w14:val=""{gc2}""/></w14:gs></w14:gsLst><w14:lin w14:ang=""{angle}"" w14:scaled=""1""/></w14:gradFill></w14:textFill>";
    }

    /// <summary>
    /// Build w14:shadow XML.
    /// Format: "COLOR[;BLUR[;ANGLE[;DIST[;OPACITY]]]]"
    /// Defaults: blur=4pt, angle=45°, dist=3pt, opacity=40%
    /// Legacy: '-' separator also accepted.
    /// </summary>
    internal static string BuildW14Shadow(string value)
    {
        var parts = SplitEffectValue(value);
        var (color, _) = ParseHelpers.SanitizeColorForOoxml(parts[0]);
        var blurPt = parts.Length > 1 ? ParseHelpers.SafeParseDouble(parts[1], "shadow blur") : 4.0;
        var angleDeg = parts.Length > 2 ? ParseHelpers.SafeParseDouble(parts[2], "shadow angle") : 45.0;
        var distPt = parts.Length > 3 ? ParseHelpers.SafeParseDouble(parts[3], "shadow distance") : 3.0;
        var opacity = parts.Length > 4 ? ParseHelpers.SafeParseDouble(parts[4], "shadow opacity") : 40.0;

        var blurEmu = (long)(blurPt * EmuConverter.EmuPerPoint);
        var distEmu = (long)(distPt * EmuConverter.EmuPerPoint);
        var angleOoxml = (int)(angleDeg * 60000);
        var alphaVal = (int)(opacity * 1000);

        return $@"<w14:shadow xmlns:w14=""{W14Ns}"" w14:blurRad=""{blurEmu}"" w14:dist=""{distEmu}"" w14:dir=""{angleOoxml}"" w14:sx=""100000"" w14:sy=""100000"" w14:kx=""0"" w14:ky=""0"" w14:algn=""tl""><w14:srgbClr w14:val=""{color}""><w14:alpha w14:val=""{alphaVal}""/></w14:srgbClr></w14:shadow>";
    }

    /// <summary>
    /// Build w14:glow XML.
    /// Format: "COLOR[;RADIUS[;OPACITY]]"
    /// Defaults: radius=8pt, opacity=75%
    /// Legacy: '-' separator also accepted.
    /// </summary>
    internal static string BuildW14Glow(string value)
    {
        var parts = SplitEffectValue(value);
        var (color, _) = ParseHelpers.SanitizeColorForOoxml(parts[0]);
        var radiusPt = parts.Length > 1 ? ParseHelpers.SafeParseDouble(parts[1], "glow radius") : 8.0;
        var opacity = parts.Length > 2 ? ParseHelpers.SafeParseDouble(parts[2], "glow opacity") : 75.0;

        var radiusEmu = (long)(radiusPt * EmuConverter.EmuPerPoint);
        var alphaVal = (int)(opacity * 1000);

        return $@"<w14:glow xmlns:w14=""{W14Ns}"" w14:rad=""{radiusEmu}""><w14:srgbClr w14:val=""{color}""><w14:alpha w14:val=""{alphaVal}""/></w14:srgbClr></w14:glow>";
    }

    /// <summary>
    /// Build w14:reflection XML.
    /// Values: "tight"/"small", "half"/"true", "full"
    /// </summary>
    internal static string BuildW14Reflection(string value)
    {
        var endPos = value.ToLowerInvariant() switch
        {
            "tight" or "small" => 55000,
            "true" or "half" => 90000,
            "full" => 100000,
            _ => int.TryParse(value, out var pct) ? (int)Math.Min((long)pct * 1000, 100000) : 90000
        };

        return $@"<w14:reflection xmlns:w14=""{W14Ns}"" w14:blurRad=""6350"" w14:stA=""52000"" w14:stPos=""0"" w14:endA=""300"" w14:endPos=""{endPos}"" w14:dist=""0"" w14:dir=""5400000"" w14:fadeDir=""5400000"" w14:sx=""100000"" w14:sy=""-100000"" w14:kx=""0"" w14:ky=""0"" w14:algn=""bl""/>";
    }

    /// <summary>
    /// Apply a w14 text effect to a run's RunProperties.
    /// Handles set and remove logic.
    /// </summary>
    /// <summary>
    /// Apply all supported w14 run text effects (textOutline, textFill, shadow,
    /// glow, reflection) present in <paramref name="properties"/> to a run.
    /// Shared by AddRun and AddParagraph's implicit run so `add paragraph
    /// --prop textFill=...` behaves consistently with `--prop bold=...`.
    /// Returns true if any effect was applied. TryGetValue marks the keys
    /// consumed for unsupported-property tracking.
    /// </summary>
    internal static bool ApplyW14Effects(Run run, Dictionary<string, string> properties)
    {
        bool any = false;
        if (properties.TryGetValue("textOutline", out var toVal) || properties.TryGetValue("textoutline", out toVal))
        { ApplyW14TextEffect(run, "textOutline", toVal, BuildW14TextOutline); any = true; }
        if (properties.TryGetValue("textFill", out var tfVal) || properties.TryGetValue("textfill", out tfVal))
        { ApplyW14TextEffect(run, "textFill", tfVal, BuildW14TextFill); any = true; }
        if (properties.TryGetValue("w14shadow", out var wsVal))
        { ApplyW14TextEffect(run, "shadow", wsVal, BuildW14Shadow); any = true; }
        if (properties.TryGetValue("w14glow", out var wgVal))
        { ApplyW14TextEffect(run, "glow", wgVal, BuildW14Glow); any = true; }
        if (properties.TryGetValue("w14reflection", out var wrVal))
        { ApplyW14TextEffect(run, "reflection", wrVal, BuildW14Reflection); any = true; }
        // OpenType typographic toggles (ligatures / numForm / numSpacing). Unlike
        // the effect family above, "none" is a real ST_ enum value — see
        // ApplyW14ValEffect — so these go through a dedicated apply that never
        // strips the element.
        if (properties.TryGetValue("ligatures", out var ligVal))
        { ApplyW14ValEffect(run, "ligatures", ligVal); any = true; }
        if (properties.TryGetValue("numForm", out var nfVal) || properties.TryGetValue("numform", out nfVal))
        { ApplyW14ValEffect(run, "numForm", nfVal); any = true; }
        if (properties.TryGetValue("numSpacing", out var nsVal) || properties.TryGetValue("numspacing", out nsVal))
        { ApplyW14ValEffect(run, "numSpacing", nsVal); any = true; }
        return any;
    }

    // Apply an OpenType typographic w14 toggle (ligatures / numForm / numSpacing)
    // by writing <w14:NAME w14:val="VALUE"/> verbatim. These carry an ST_ enum
    // where "none" means "explicitly disable" (overriding an inherited
    // docDefaults value) — NOT the strip-the-element sentinel ApplyW14TextEffect
    // uses for the effect family. Reusing that path would drop a run's
    // ligatures="none" and let it inherit docDefaults, the very regression this
    // round-trips.
    internal static void ApplyW14ValEffect(Run run, string effectName, string value)
    {
        var rPr = EnsureRunProperties(run);
        RemoveW14Element(rPr, effectName);
        var child = BuildW14ValElement(effectName, value);
        if (child != null)
            InsertW14ChildInSchemaOrder(rPr, child);
    }

    // Build a bare <w14:NAME w14:val="VALUE"/> element (detached, no parent) so
    // both the run path (ApplyW14ValEffect) and the style path (AddStyle's
    // StyleRunProperties) can insert it via their own schema-order placer.
    internal static OpenXmlElement? BuildW14ValElement(string effectName, string value)
    {
        var holder = new OpenXmlUnknownElement("w14", "tmp", W14Ns);
        holder.InnerXml = $@"<w14:{effectName} xmlns:w14=""{W14Ns}"" w14:val=""{System.Security.SecurityElement.Escape(value)}""/>";
        var child = holder.FirstChild;
        child?.Remove();
        return child;
    }

    internal static void ApplyW14TextEffect(Run run, string effectName, string value, Func<string, string> builder)
    {
        var rPr = EnsureRunProperties(run);
        RemoveW14Element(rPr, effectName);

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("false", StringComparison.OrdinalIgnoreCase))
            return;

        var xml = builder(value);
        var element = new OpenXmlUnknownElement("w14", "tmp", W14Ns);
        element.InnerXml = xml;
        var child = element.FirstChild;
        if (child != null)
        {
            child.Remove();
            InsertW14ChildInSchemaOrder(rPr, child);
        }
    }

    // The w14 run-property extension elements have a fixed schema order; a
    // strict validator (and Word) reject them out of sequence with "unexpected
    // child element". Appending in call order produced e.g.
    // textOutline,textFill,shadow,glow,reflection — invalid (BUG-DUMPR2). Insert
    // each effect before the first already-present w14 child that outranks it so
    // the set stays canonical regardless of the order the effects are applied.
    private static readonly string[] W14EffectOrder =
    {
        "glow", "shadow", "reflection", "textOutline", "textFill",
        "scene3d", "props3d", "ligatures", "numForm", "numSpacing",
        "stylisticSets", "cntxtAlts",
    };

    private static void InsertW14ChildInSchemaOrder(RunProperties rPr, OpenXmlElement child)
    {
        static int Rank(string localName)
        {
            var i = Array.IndexOf(W14EffectOrder, localName);
            return i < 0 ? int.MaxValue : i;
        }
        int childRank = Rank(child.LocalName);
        OpenXmlElement? insertBefore = null;
        foreach (var existing in rPr.ChildElements)
        {
            if (existing.NamespaceUri == W14Ns && Rank(existing.LocalName) > childRank)
            {
                insertBefore = existing;
                break;
            }
        }
        if (insertBefore != null) rPr.InsertBefore(child, insertBefore);
        else rPr.AppendChild(child);
    }

    /// <summary>
    /// Read w14 text effect values from RunProperties.
    /// Returns a dictionary of effect names to their parsed values.
    /// </summary>
    internal static void ReadW14TextEffects(RunProperties? rPr, DocumentNode node)
    {
        if (rPr == null) return;

        foreach (var child in rPr.ChildElements)
        {
            if (child.NamespaceUri != W14Ns) continue;

            switch (child.LocalName)
            {
                case "textOutline":
                {
                    var wAttr = child.GetAttributes().FirstOrDefault(a => a.LocalName == "w");
                    var widthEmu = long.TryParse(wAttr.Value, out var w) ? w : 0;
                    var widthPt = widthEmu / EmuConverter.EmuPerPointF;
                    var colorMatch = System.Text.RegularExpressions.Regex.Match(
                        child.InnerXml, @"val=""([0-9A-Fa-f]{6})""");
                    var color = colorMatch.Success ? ParseHelpers.FormatHexColor(colorMatch.Groups[1].Value) : "";
                    node.Format["textOutline"] = string.IsNullOrEmpty(color) ? $"{widthPt:0.##}pt" : $"{widthPt:0.##}pt;{color}";
                    break;
                }
                case "textFill":
                {
                    var innerXml = child.InnerXml;
                    if (innerXml.Contains("gradFill"))
                    {
                        var colors = new List<string>();
                        foreach (System.Text.RegularExpressions.Match m in
                            System.Text.RegularExpressions.Regex.Matches(innerXml, @"val=""([0-9A-Fa-f]{6})"""))
                            colors.Add(m.Groups[1].Value);

                        // Add # prefix to gradient colors
                        for (int ci = 0; ci < colors.Count; ci++)
                            colors[ci] = ParseHelpers.FormatHexColor(colors[ci]);

                        var isRadial = innerXml.Contains("<w14:path");
                        if (isRadial && colors.Count >= 2)
                            node.Format["textFill"] = $"radial:{colors[0]};{colors[1]}";
                        else if (colors.Count >= 2)
                        {
                            var angleMatch = System.Text.RegularExpressions.Regex.Match(innerXml, @"ang=""(\d+)""");
                            var angle = angleMatch.Success ? int.Parse(angleMatch.Groups[1].Value) / 60000.0 : 0.0;
                            var angleStr = angle % 1 == 0 ? $"{(int)angle}" : $"{angle:0.##}";
                            node.Format["textFill"] = $"{colors[0]};{colors[1]};{angleStr}";
                        }
                        else if (colors.Count == 1)
                            node.Format["textFill"] = colors[0];
                    }
                    else if (innerXml.Contains("solidFill"))
                    {
                        var colorMatch = System.Text.RegularExpressions.Regex.Match(
                            innerXml, @"val=""([0-9A-Fa-f]{6})""");
                        if (colorMatch.Success)
                            node.Format["textFill"] = ParseHelpers.FormatHexColor(colorMatch.Groups[1].Value);
                    }
                    break;
                }
                case "shadow":
                {
                    var attrs = child.GetAttributes().ToDictionary(a => a.LocalName, a => a.Value);
                    var colorMatch = System.Text.RegularExpressions.Regex.Match(
                        child.InnerXml, @"val=""([0-9A-Fa-f]{6})""");
                    var color = colorMatch.Success ? ParseHelpers.FormatHexColor(colorMatch.Groups[1].Value) : "#000000";
                    var blurEmu = attrs.TryGetValue("blurRad", out var br) && long.TryParse(br, out var blurVal) ? blurVal : 0;
                    var blurPt = blurEmu / EmuConverter.EmuPerPointF;
                    var dirVal = attrs.TryGetValue("dir", out var dir) && long.TryParse(dir, out var dirLong) ? dirLong : 0;
                    var angleDeg = dirVal / 60000.0;
                    var distEmu = attrs.TryGetValue("dist", out var dist) && long.TryParse(dist, out var distLong) ? distLong : 0;
                    var distPt = distEmu / EmuConverter.EmuPerPointF;
                    // Read alpha (opacity) from inner srgbClr child
                    var alphaMatch = System.Text.RegularExpressions.Regex.Match(
                        child.InnerXml, @"alpha[^>]*val=""(\d+)""");
                    var opacity = alphaMatch.Success && double.TryParse(alphaMatch.Groups[1].Value, out var alphaVal) ? alphaVal / 1000.0 : 100.0;
                    node.Format["w14shadow"] = $"{color};{blurPt:0.##};{angleDeg:0.##};{distPt:0.##};{opacity:0.##}";
                    break;
                }
                case "glow":
                {
                    var radAttr = child.GetAttributes().FirstOrDefault(a => a.LocalName == "rad");
                    var radiusEmu = long.TryParse(radAttr.Value, out var r) ? r : 0;
                    var radiusPt = radiusEmu / EmuConverter.EmuPerPointF;
                    var colorMatch = System.Text.RegularExpressions.Regex.Match(
                        child.InnerXml, @"val=""([0-9A-Fa-f]{6})""");
                    var color = colorMatch.Success ? ParseHelpers.FormatHexColor(colorMatch.Groups[1].Value) : "#000000";
                    // Read alpha (opacity) from inner srgbClr child
                    var alphaMatch = System.Text.RegularExpressions.Regex.Match(
                        child.InnerXml, @"alpha[^>]*val=""(\d+)""");
                    var opacity = alphaMatch.Success && double.TryParse(alphaMatch.Groups[1].Value, out var av) ? av / 1000.0 : 100.0;
                    node.Format["w14glow"] = $"{color};{radiusPt:0.##};{opacity:0.##}";
                    break;
                }
                case "reflection":
                {
                    var endPosAttr = child.GetAttributes().FirstOrDefault(a => a.LocalName == "endPos");
                    var endPos = int.TryParse(endPosAttr.Value, out var ep) ? ep : 90000;
                    node.Format["w14reflection"] = endPos switch
                    {
                        <= 55000 => "tight",
                        <= 90000 => "half",
                        _ => "full"
                    };
                    break;
                }
                // OpenType typographic toggles carrying an ST_ enum val. "none"
                // is a REAL value here (the run explicitly disables the feature to
                // override an inherited docDefaults setting), so it must round-trip
                // verbatim — surfacing the key keeps the run on the explicit raw
                // emit path (BUG-W14-EFFECTS check in WordBatchEmitter.Paragraph.cs)
                // and AddRun re-applies it via ApplyW14ValEffect. Dropping a
                // run's ligatures="none" let it inherit docDefaults
                // "standardContextual", shifting glyph metrics enough to reflow a
                // paragraph and cascade a whole-page pagination drift.
                case "ligatures":
                case "numForm":
                case "numSpacing":
                {
                    var valAttr = child.GetAttributes().FirstOrDefault(a => a.LocalName == "val");
                    node.Format[child.LocalName] = valAttr.Value ?? "none";
                    break;
                }
            }
        }
    }
}
