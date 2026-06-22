// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Core;

/// <summary>
/// Cross-handler builders for DrawingML (`a:` namespace) color/fill elements.
/// Used by both PowerPointHandler (slide shapes, runs) and ExcelHandler (drawing-layer
/// shapes, chart series). Word's <c>w:</c> namespace has its own run-property color
/// model and does not share this helper.
/// </summary>
internal static class DrawingColorBuilder
{
    /// <summary>
    /// Parse a color string and return the appropriate DrawingML color element:
    /// <c>a:srgbClr</c> (with optional <c>a:alpha</c>) for hex/named colors,
    /// or <c>a:schemeClr</c> for theme color names (accent1..6, dk1/lt1/tx1/bg1/hlink/...).
    /// </summary>
    internal static OpenXmlElement BuildColorElement(string value)
    {
        // R8-4: split the trailing color transform chain
        // ("accent1+lumMod50+lumOff20") from the base color before any
        // recognition. Transforms are appended as a:lumMod / a:lumOff /
        // a:shade / a:tint / a:satMod / a:satOff / a:hueMod / a:hueOff
        // children. Pre-R8 these suffixes weren't a vocabulary, so feeding
        // the round-tripped form back through Set silently failed scheme
        // recognition.
        string baseColor = value;
        List<(string Name, int Val)>? transforms = null;
        // Accept '+' (canonical Get round-trip form) or ':' (alternate form
        // some authors reach for when the base is a scheme name). ':' is
        // reserved for gradient prefixes ("radial:", "path:") and pattern
        // foreground ("pct25:FF0000"), so we only honour it when the prefix
        // is a recognised scheme color — otherwise it goes to the gradient
        // / pattern parser as before.
        var plus = value.IndexOf('+');
        if (plus <= 0)
        {
            var colon = value.IndexOf(':');
            if (colon > 0 && TryParseSchemeColor(value.Substring(0, colon)).HasValue)
                plus = colon;
        }
        if (plus > 0)
        {
            baseColor = value.Substring(0, plus);
            // Re-join remaining tokens whether separator was '+' or ':'.
            transforms = ParseColorTransformSuffix(value.Substring(plus + 1));
        }

        OpenXmlElement colorEl;
        var schemeColor = TryParseSchemeColor(baseColor);
        var systemColor = TryParseSystemColor(baseColor);
        if (schemeColor.HasValue)
        {
            colorEl = new Drawing.SchemeColor { Val = schemeColor.Value };
        }
        else if (systemColor != null)
        {
            // <a:sysClr val="window"/> etc. — the gradient/Get readback emits the
            // bare OOXML sysClr name, which the hex parser rejected ("Invalid
            // color value: 'window'"). Rebuild a real a:sysClr element with its
            // conventional lastClr so the value renders even if the consuming
            // app doesn't resolve the live system color.
            colorEl = new Drawing.SystemColor
            {
                Val = systemColor.Value.val,
                LastColor = systemColor.Value.lastClr,
            };
        }
        else
        {
            var (rgb, alpha) = ParseHelpers.SanitizeColorForOoxml(baseColor);
            var rgbEl = new Drawing.RgbColorModelHex { Val = rgb };
            if (alpha.HasValue)
                rgbEl.AppendChild(new Drawing.Alpha { Val = alpha.Value });
            colorEl = rgbEl;
        }
        if (transforms != null)
            AppendColorTransformChildren(colorEl, transforms);
        return colorEl;
    }

    // R8-4: parse "lumMod50+lumOff20" → [("lumMod",50),("lumOff",20)]. Each
    // token is name + integer percent (0..100). Unknown tokens are dropped
    // silently to keep the input contract lenient — Get emits only the
    // recognised set above, so a stray suffix is the caller's bug, not ours.
    private static readonly HashSet<string> KnownTransforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "lumMod", "lumOff", "shade", "tint", "satMod", "satOff", "hueMod", "hueOff", "alpha"
    };

    private static List<(string Name, int Val)> ParseColorTransformSuffix(string chain)
    {
        var result = new List<(string Name, int Val)>();
        foreach (var token in chain.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            // Two accepted forms:
            //   "lumMod75"        — Get's canonical round-trip form, percent 0..100
            //   "lumMod=75000"    — raw OOXML percentage 0..100000
            //                       (matches the literal a:lumMod@val attribute,
            //                        what users see in PowerPoint XML / docs)
            // Both end up encoded as @val="75000" on the OOXML child. The name is
            // a leading run of letters; the remainder is '='?<signed-int>. Scan the
            // name by letters (not "first digit") so a leading '-' on the value
            // stays with the value instead of being folded into the name.
            int i = 0;
            while (i < token.Length && char.IsLetter(token[i])) i++;
            if (i == 0 || i == token.Length) continue;
            var name = token.Substring(0, i);
            if (!KnownTransforms.Contains(name))
                throw new ArgumentException(
                    $"Unknown color transform '{name}'. Valid: lumMod, lumOff, shade, tint, satMod, satOff, hueMod, hueOff, alpha.");
            bool eqForm = token[i] == '=';
            string numText = eqForm ? token.Substring(i + 1) : token.Substring(i);
            if (!int.TryParse(numText, out var raw))
                throw new ArgumentException(
                    $"Invalid color transform '{token}': value must be an integer.");
            // OOXML splits the transforms into two schema types:
            //   shade / tint / alpha  → ST_PositiveFixedPercentage (0..100%),
            //                           negatives forbidden.
            //   lumMod / lumOff / satMod / satOff / hueMod / hueOff
            //                         → ST_Percentage: SIGNED and may exceed 100%
            //                           (e.g. satMod200% to over-saturate, or
            //                           satOff-10% to desaturate). Rejecting
            //                           negatives wrongly aborted replay of the
            //                           round-trip form Get emits for these
            //                           (satOff val="-10000" → "satOff-10").
            // Only the fixed-percentage family stays clamped 0..100.
            bool fixedPct = name.ToLowerInvariant() is "shade" or "tint" or "alpha";
            int maxPct = fixedPct ? 100 : 1000;       // 1000% headroom for ST_Percentage
            int minPct = fixedPct ? 0 : -1000;        // signed for the Mod/Off family
            int maxRaw = fixedPct ? 100000 : 1000000;
            int minRaw = fixedPct ? 0 : -1000000;
            int pct;
            if (eqForm)
            {
                if (raw < minRaw || raw > maxRaw)
                    throw new ArgumentException(
                        $"Invalid color transform '{token}': raw value {raw} out of range {minRaw}-{maxRaw}.");
                // OOXML raw units are 1/1000 of a percent. Integer division
                // truncates values whose magnitude is 1..999 to 0 (lumMod=75 raw
                // → 0 instead of 7.5%). Reject sub-1000 magnitudes so callers
                // can't silently get a no-op; the percentage form covers that range.
                if (raw != 0 && Math.Abs(raw) < 1000)
                    throw new ArgumentException(
                        $"Invalid color transform '{token}': raw value {raw} below 1000 truncates to 0%; use percentage form '{name}{raw / 1000}' or raw magnitude >= 1000.");
                pct = raw / 1000;
            }
            else
            {
                if (raw < minPct || raw > maxPct)
                    throw new ArgumentException(
                        $"Invalid color transform '{token}': percentage {raw} out of range {minPct}-{maxPct}.");
                pct = raw;
            }
            // Canonicalize: lumMod → lumMod (lowercase first letter? OOXML uses
            // camelCase: lumMod, lumOff, satMod, satOff, hueMod, hueOff,
            // shade, tint). KnownTransforms matches case-insensitively; we
            // re-emit the canonical form here.
            var canonical = name.ToLowerInvariant() switch
            {
                "lummod" => "lumMod",
                "lumoff" => "lumOff",
                "satmod" => "satMod",
                "satoff" => "satOff",
                "huemod" => "hueMod",
                "hueoff" => "hueOff",
                "shade" => "shade",
                "tint" => "tint",
                "alpha" => "alpha",
                _ => name
            };
            result.Add((canonical, pct));
        }
        return result;
    }

    private static void AppendColorTransformChildren(OpenXmlElement colorEl, List<(string Name, int Val)> transforms)
    {
        const string aNs = "http://schemas.openxmlformats.org/drawingml/2006/main";
        foreach (var (name, pct) in transforms)
        {
            var child = new OpenXmlUnknownElement("a", name, aNs);
            // OOXML ST_PositivePercentage / ST_FixedPercentage uses 1000ths
            // of a percent: 100 → 100000, 50 → 50000.
            child.SetAttribute(new OpenXmlAttribute("", "val", null!, (pct * 1000).ToString()));
            colorEl.AppendChild(child);
        }
    }

    /// <summary>
    /// Build an <c>a:solidFill</c> element with the appropriate color child (RGB or scheme).
    /// </summary>
    internal static Drawing.SolidFill BuildSolidFill(string colorValue)
    {
        var solidFill = new Drawing.SolidFill();
        solidFill.Append(BuildColorElement(colorValue));
        return solidFill;
    }

    /// <summary>
    /// Try to parse an OOXML system color name (<c>a:sysClr</c> @val). Returns the
    /// enum value plus a conventional lastClr hex fallback, or null when the input
    /// is not a recognised system color. Only the slots that appear in real decks
    /// (window / windowText, plus the common UI chrome colors) are mapped — the
    /// full ST_SystemColorVal vocabulary is large but rarely authored.
    /// </summary>
    internal static (Drawing.SystemColorValues val, string lastClr)? TryParseSystemColor(string value)
    {
        return value.ToLowerInvariant().Trim() switch
        {
            "window" => (Drawing.SystemColorValues.Window, "FFFFFF"),
            "windowtext" => (Drawing.SystemColorValues.WindowText, "000000"),
            "background" => (Drawing.SystemColorValues.Background, "FFFFFF"),
            "windowframe" => (Drawing.SystemColorValues.WindowFrame, "000000"),
            "highlight" => (Drawing.SystemColorValues.Highlight, "0078D7"),
            "highlighttext" => (Drawing.SystemColorValues.HighlightText, "FFFFFF"),
            "btnface" => (Drawing.SystemColorValues.ButtonFace, "F0F0F0"),
            "btntext" => (Drawing.SystemColorValues.ButtonText, "000000"),
            "graytext" => (Drawing.SystemColorValues.GrayText, "6D6D6D"),
            _ => null
        };
    }

    /// <summary>
    /// Try to parse a theme/scheme color name. Returns null if the input is a hex RGB value.
    /// </summary>
    internal static Drawing.SchemeColorValues? TryParseSchemeColor(string value)
    {
        return value.ToLowerInvariant().TrimStart('#') switch
        {
            "accent1" => Drawing.SchemeColorValues.Accent1,
            "accent2" => Drawing.SchemeColorValues.Accent2,
            "accent3" => Drawing.SchemeColorValues.Accent3,
            "accent4" => Drawing.SchemeColorValues.Accent4,
            "accent5" => Drawing.SchemeColorValues.Accent5,
            "accent6" => Drawing.SchemeColorValues.Accent6,
            "dk1" or "dark1" => Drawing.SchemeColorValues.Dark1,
            "dk2" or "dark2" => Drawing.SchemeColorValues.Dark2,
            "lt1" or "light1" => Drawing.SchemeColorValues.Light1,
            "lt2" or "light2" => Drawing.SchemeColorValues.Light2,
            "tx1" or "text1" => Drawing.SchemeColorValues.Text1,
            "tx2" or "text2" => Drawing.SchemeColorValues.Text2,
            "bg1" or "background1" => Drawing.SchemeColorValues.Background1,
            "bg2" or "background2" => Drawing.SchemeColorValues.Background2,
            "hlink" or "hyperlink" => Drawing.SchemeColorValues.Hyperlink,
            "folhlink" or "followedhyperlink" => Drawing.SchemeColorValues.FollowedHyperlink,
            _ => null
        };
    }
}
