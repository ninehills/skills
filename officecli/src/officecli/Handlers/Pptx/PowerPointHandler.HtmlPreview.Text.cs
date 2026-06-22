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
    // ==================== Text Rendering ====================

    private static void RenderTextBody(StringBuilder sb, OpenXmlElement textBody, Dictionary<string, string> themeColors,
        Shape? placeholderShape = null, OpenXmlPart? placeholderPart = null, string? fontRefDefaultColor = null)
    {
        // Per-textbody auto-number counters, keyed by scheme type + paragraph level.
        // Resets when switching type/level. Paragraphs aren't wrapped in <ol>, so
        // we count manually and emit the numeric glyph inline.
        var autoNumCounters = new Dictionary<string, int>();
        string? lastAutoKey = null;
        // Resolve the theme font that runs in this textbody should inherit when
        // they carry no explicit Latin typeface (or carry the theme reference
        // "+mj-lt" / "+mn-lt"). Title placeholders inherit the major (heading)
        // typeface; everything else inherits minor (body). GetTextDefaults emits
        // only the body font at slide scope, so without this fallback title
        // placeholders silently render in the body face in HTML preview while
        // PowerPoint renders them in the heading face.
        bool isTitle = IsTitlePlaceholder(placeholderShape);
        string? themeFontFallback = ResolveThemeFontTypeface(placeholderPart, isTitle ? "major" : "minor");
        // Bug #8(B): honor <a:normAutofit fontScale="N"> (1/1000% → ratio). A real
        // PowerPoint deck that shrank text to fit stores the computed scale here;
        // multiply run font sizes by it. Absent/100% (CLI-created files) = no scale.
        // textBody is a p:txBody (Presentation.TextBody), and its a:bodyPr is a
        // Drawing.BodyProperties child — read it generically, not via a cast to
        // Drawing.TextBody (which a p:txBody is not).
        var naf = textBody.GetFirstChild<Drawing.BodyProperties>()?
            .GetFirstChild<Drawing.NormalAutoFit>();
        var nafScale = naf?.FontScale?.Value;
        double fontScale = (nafScale.HasValue && nafScale.Value > 0) ? nafScale.Value / 100000.0 : 1.0;
        // R10-1: honor <a:normAutofit lnSpcReduction="N"> (1/1000% → ratio). When a
        // PowerPoint deck shrank line spacing to fit, it stores the reduction here;
        // multiply paragraph line-heights by (1 - reduction). Absent/0 = no change.
        var lnRed = naf?.LineSpaceReduction?.Value;
        double lnSpcFactor = (lnRed.HasValue && lnRed.Value > 0) ? (1 - lnRed.Value / 100000.0) : 1.0;
        bool isFirstPara = true;
        foreach (var para in textBody.Elements<Drawing.Paragraph>())
        {
            // Resolve per-paragraph font size based on paragraph level
            int? defaultFontSizeHundredths = null;
            // R7-2: inherited default run color from the placeholder/master cascade.
            string? defaultRunColor = null;
            // R7-3: inherited default line-spacing CSS fragment from the cascade.
            string? inheritedLineSpacing = null;
            if (placeholderShape != null && placeholderPart != null)
            {
                int level = para.ParagraphProperties?.Level?.Value ?? 0;
                defaultFontSizeHundredths = ResolvePlaceholderFontSize(placeholderShape, placeholderPart, level);
                defaultRunColor = ResolvePlaceholderDefaultColor(placeholderShape, placeholderPart, themeColors, level);
                inheritedLineSpacing = ResolvePlaceholderLineSpacing(placeholderShape, placeholderPart, level);
            }
            // R11-3: style-matrix fontRef schemeClr is the FINAL fallback run color
            // when no explicit run color and no inherited placeholder color is found.
            defaultRunColor ??= fontRefDefaultColor;
            var paraStyles = new List<string>();

            var pProps = para.ParagraphProperties;
            if (pProps?.Alignment?.HasValue == true)
            {
                var align = pProps.Alignment.InnerText switch
                {
                    "l" => "left",
                    "ctr" => "center",
                    "r" => "right",
                    "just" => "justify",
                    _ => "left"
                };
                paraStyles.Add($"text-align:{align}");
            }

            // Paragraph spacing. PowerPoint ignores spcBef on the FIRST paragraph
            // of a text body (the line sits flush at the body's top inset), so we
            // suppress the margin-top contribution for that paragraph only;
            // subsequent paragraphs keep their spaceBefore.
            // Effective font size (hundredths of a point) for percent-based
            // spacing: spcPct expresses the gap as a percentage of the line's
            // font size (300000 = 300%). Mirror the bullet-size resolution
            // chain (first run size > placeholder/default > 18pt body default).
            var spcFontHundredths = para.Elements<Drawing.Run>().FirstOrDefault()?.RunProperties?.FontSize?.Value
                ?? defaultFontSizeHundredths ?? 1800;
            var sbElem = pProps?.GetFirstChild<Drawing.SpaceBefore>();
            var sbPts = sbElem?.GetFirstChild<Drawing.SpacingPoints>()?.Val?.Value;
            if (sbPts.HasValue && !isFirstPara) paraStyles.Add($"margin-top:{sbPts.Value / 100.0:0.##}pt");
            else
            {
                // SpacingPercent fallback (parity with lineSpacing below). pct
                // is /100000; multiply by the effective font pt to get the gap.
                var sbPct = sbElem?.GetFirstChild<Drawing.SpacingPercent>()?.Val?.Value;
                if (sbPct.HasValue && !isFirstPara)
                    paraStyles.Add($"margin-top:{sbPct.Value / 100000.0 * (spcFontHundredths / 100.0):0.##}pt");
            }
            var saElem = pProps?.GetFirstChild<Drawing.SpaceAfter>();
            var saPts = saElem?.GetFirstChild<Drawing.SpacingPoints>()?.Val?.Value;
            if (saPts.HasValue) paraStyles.Add($"margin-bottom:{saPts.Value / 100.0:0.##}pt");
            else
            {
                var saPct = saElem?.GetFirstChild<Drawing.SpacingPercent>()?.Val?.Value;
                if (saPct.HasValue)
                    paraStyles.Add($"margin-bottom:{saPct.Value / 100000.0 * (spcFontHundredths / 100.0):0.##}pt");
            }

            // Line spacing. R10-1: scale percent/default line-heights by the
            // normAutofit lnSpcReduction factor (fixed-pt line-heights are absolute
            // and unaffected, matching PowerPoint).
            var lsPct = pProps?.GetFirstChild<Drawing.LineSpacing>()?.GetFirstChild<Drawing.SpacingPercent>()?.Val?.Value;
            if (lsPct.HasValue) paraStyles.Add($"line-height:{lsPct.Value / 100000.0 * lnSpcFactor:0.##}");
            var lsPts = pProps?.GetFirstChild<Drawing.LineSpacing>()?.GetFirstChild<Drawing.SpacingPoints>()?.Val?.Value;
            if (lsPts.HasValue) paraStyles.Add($"line-height:{lsPts.Value / 100.0:0.##}pt");
            // R7-3: no explicit lnSpc on the paragraph — inherit the master/layout
            // bodyStyle lvl lnSpc resolved via the placeholder cascade.
            else if (!lsPct.HasValue && inheritedLineSpacing != null)
                paraStyles.Add(inheritedLineSpacing);
            // R10-1: paragraph has no explicit/inherited line spacing but the body
            // has a lnSpcReduction — emit the reduced default multiplier so the
            // reduction is visible in the render.
            else if (!lsPct.HasValue && inheritedLineSpacing == null && lnSpcFactor < 1.0)
                paraStyles.Add($"line-height:{lnSpcFactor:0.##}");

            // Indent / left margin. OOXML hanging-indent idiom (bullet outside, text inside)
            // is marL>=0 paired with indent<0 (|indent|==marL). We translate marL to CSS
            // padding-left (text starts at marL inside the shape content). The negative
            // indent is realised on the bullet span itself (margin-left:-|indent|), NOT
            // via text-indent on the para — text-indent would shift the line into the
            // shape's outer padding box and route bulletless paragraphs (line-spacing only)
            // right-flush via overflow interactions with width:100%.
            // CONSISTENCY(pptx-hanging-indent): bullet pulled left via its own margin.
            bool hasBullet0 = pProps?.GetFirstChild<Drawing.CharacterBullet>() != null
                              || pProps?.GetFirstChild<Drawing.AutoNumberedBullet>() != null;
            // Bulletless hanging indent: marL>0 paired with indent<0 hangs the
            // first line |indent| left of the marL margin (real PowerPoint renders
            // this even without a bullet — officeshot-confirmed). Emit the negative
            // text-indent with padding-left=marL so the first line hangs while
            // wrapped lines align to the margin. Guard the bleed the prior code
            // worried about: only emit the negative shift when marL >= |indent|,
            // so the first line stays inside the shape content box (the hanging-
            // indent idiom always satisfies this). Otherwise clamp to 0.
            if (pProps?.Indent?.HasValue == true && !hasBullet0)
            {
                var indentPt = Units.EmuToPt(pProps.Indent.Value);
                if (indentPt < 0)
                {
                    var marLPt = pProps?.LeftMargin?.HasValue == true
                        ? Units.EmuToPt(pProps.LeftMargin.Value) : 0;
                    if (marLPt < -indentPt) indentPt = 0; // would bleed past content edge
                }
                paraStyles.Add($"text-indent:{indentPt}pt");
            }
            if (pProps?.LeftMargin?.HasValue == true)
                paraStyles.Add($"padding-left:{Units.EmuToPt(pProps.LeftMargin.Value)}pt");
            else if ((pProps?.Level?.Value ?? 0) > 0)
                // R10b: no explicit marL but lvl>0 — approximate the default
                // lstStyle cascade. PowerPoint's built-in body text styles use
                // marL ≈ 0.5in (36pt) per indent level; reproduce that so leveled
                // paragraphs aren't flush-left in the preview. Explicit marL (above)
                // always wins; this only fills the inherited-default gap.
                paraStyles.Add($"padding-left:{(pProps!.Level!.Value) * 36}pt");

            // RTL paragraph (Arabic / Hebrew). <a:pPr rtl="1"/> reverses
            // character order; emit CSS so the browser does the same. Without
            // this, Arabic PPT slides rendered visually mirrored in HTML
            // preview compared to PowerPoint itself.
            if (pProps?.RightToLeft?.Value == true)
                paraStyles.Add("direction:rtl;unicode-bidi:embed");

            // Bullet
            var bulletChar = pProps?.GetFirstChild<Drawing.CharacterBullet>()?.Char?.Value;
            var bulletAuto = pProps?.GetFirstChild<Drawing.AutoNumberedBullet>();
            // Image bullet (<a:buBlip>). SDK 3.x has no typed BulletBlip in this
            // context; detect by local name so a raw-injected buBlip also counts.
            var bulletBlip = pProps?.ChildElements
                .FirstOrDefault(e => e.LocalName == "buBlip");
            var hasBullet = bulletChar != null || bulletAuto != null || bulletBlip != null;

            // Resolve auto-numbered glyph (e.g. "1.", "a.", "iv.") and track per-scheme counter.
            string? autoNumGlyph = null;
            if (bulletAuto != null)
            {
                int paraLevel = pProps?.Level?.Value ?? 0;
                string schemeKey = (bulletAuto.Type?.HasValue == true ? bulletAuto.Type.Value.ToString() : "arabicPeriod") + "@" + paraLevel;
                if (lastAutoKey != schemeKey)
                {
                    autoNumCounters[schemeKey] = 0;
                    lastAutoKey = schemeKey;
                }
                int startAt = bulletAuto.StartAt?.Value ?? 1;
                int n = autoNumCounters.TryGetValue(schemeKey, out var c) ? c : 0;
                int index = (n == 0 ? startAt : startAt + n);
                autoNumCounters[schemeKey] = n + 1;
                // Use the OOXML *value* (e.g. "alphaLcPeriod"), not the C# enum
                // member name ("AlphaLowerCharacterPeriod") — they differ, and the
                // glyph mapping keys off the OOXML token.
                string schemeToken = bulletAuto.Type?.HasValue == true && !string.IsNullOrEmpty(bulletAuto.Type.InnerText)
                    ? bulletAuto.Type.InnerText
                    : "arabicPeriod";
                autoNumGlyph = FormatAutoNumberGlyph(schemeToken, index);
            }
            else
            {
                lastAutoKey = null;
            }

            sb.Append($"<div class=\"para\" style=\"{string.Join(";", paraStyles)}\">");

            if (hasBullet)
            {
                // Image bullets have no glyph; use a generic marker so the
                // bullet span is non-empty (the source blip relationship is not
                // resolved into an <img> here \u2014 fallback marker only).
                var bullet = autoNumGlyph ?? bulletChar ?? (bulletBlip != null ? "\u25a0" : "\u2022");
                var buStyles = new List<string>();

                // Bullet font (<a:buFont typeface="..."/>) \u2014 apply font-family so a
                // symbol-font glyph (e.g. Wingdings "l") renders with the right face.
                var buFontTypeface = pProps?.GetFirstChild<Drawing.BulletFont>()?.Typeface?.Value;
                if (!string.IsNullOrEmpty(buFontTypeface))
                    buStyles.Add($"font-family:{buFontTypeface}");

                // Bullet color: explicit buClr > first run color > default (inherit)
                var buClrFill = pProps?.GetFirstChild<Drawing.BulletColor>()
                    ?.GetFirstChild<Drawing.SolidFill>();
                var bulletColor = ResolveFillColor(buClrFill, themeColors);
                if (bulletColor == null)
                {
                    // Follow first run text color
                    var firstRun = para.Elements<Drawing.Run>().FirstOrDefault();
                    var firstRunFill = firstRun?.RunProperties?.GetFirstChild<Drawing.SolidFill>();
                    bulletColor = ResolveFillColor(firstRunFill, themeColors);
                }
                if (bulletColor != null) buStyles.Add($"color:{bulletColor}");

                // Bullet size: explicit buSzPts/buSzPct > first run size > default size
                var buSzPts = pProps?.GetFirstChild<Drawing.BulletSizePoints>();
                var buSzPct = pProps?.GetFirstChild<Drawing.BulletSizePercentage>();
                if (buSzPts?.Val?.HasValue == true)
                {
                    buStyles.Add($"font-size:{buSzPts.Val.Value / 100.0:0.##}pt");
                }
                else
                {
                    // Determine base font size from first run or default
                    var firstRun = para.Elements<Drawing.Run>().FirstOrDefault();
                    var baseSizeHundredths = firstRun?.RunProperties?.FontSize?.Value ?? defaultFontSizeHundredths;
                    if (baseSizeHundredths.HasValue)
                    {
                        var pct = buSzPct?.Val?.HasValue == true ? buSzPct.Val.Value / 100000.0 : 1.0;
                        buStyles.Add($"font-size:{baseSizeHundredths.Value / 100.0 * pct:0.##}pt");
                    }
                }

                // Hanging-indent tab gap: size bullet span to match the negative
                // indent so text starts at marL regardless of bullet glyph width.
                // OOXML marL (e.g. 457200 EMU = 0.5in = 36pt) paired with indent
                // = -marL creates the hanging layout; we mirror it in CSS by
                // sizing the bullet to |indent| AND pulling it left with margin-left
                // by the same amount, so the bullet sits at the para outer edge
                // (shape content-left + 0) while text continues at marL inside.
                // We do NOT use text-indent here — text-indent on the para offsets
                // the line into the shape's outer padding box, putting the bullet
                // physically outside the shape's content area.
                long indentEmu = pProps?.Indent?.Value ?? 0;
                if (indentEmu < 0)
                {
                    var gapPt = Units.EmuToPt(-indentEmu);
                    buStyles.Add($"display:inline-block");
                    buStyles.Add($"width:{gapPt}pt");
                    buStyles.Add($"margin-left:-{gapPt}pt");
                }
                var buStyle = buStyles.Count > 0 ? $" style=\"{string.Join(";", buStyles)}\"" : "";
                sb.Append($"<span class=\"bullet\"{buStyle}>{HtmlEncode(bullet)}</span>");
            }

            // Check for OfficeMath (a14:m inside mc:AlternateContent) in paragraph XML
            var paraXml = para.OuterXml;
            if (paraXml.Contains("oMath"))
            {
                // AlternateContent is opaque to Descendants() — parse from XML
                var mathMatch = System.Text.RegularExpressions.Regex.Match(paraXml,
                    @"<m:oMathPara[^>]*>.*?</m:oMathPara>|<m:oMath[^>]*>.*?</m:oMath>",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                if (mathMatch.Success)
                {
                    var mathXml = $"<wrapper xmlns:m=\"http://schemas.openxmlformats.org/officeDocument/2006/math\">{mathMatch.Value}</wrapper>";
                    try
                    {
                        var wrapper = new OpenXmlUnknownElement("wrapper");
                        wrapper.InnerXml = mathMatch.Value;
                        var oMath = wrapper.Descendants().FirstOrDefault(e => e.LocalName == "oMathPara" || e.LocalName == "oMath");
                        if (oMath != null)
                        {
                            var latex = FormulaParser.ToLatex(oMath);
                            sb.Append($"<span class=\"katex-formula\" data-formula=\"{HtmlEncode(latex)}\"></span>");
                        }
                    }
                    catch { }
                }
            }

            var hasMath = paraXml.Contains("oMath");
            var runs = para.Elements<Drawing.Run>().ToList();
            // A paragraph is visually empty when it has no runs OR all its runs
            // carry empty <a:t> (RenderRun emits nothing for empty text). Real
            // PowerPoint still reserves a full line of vertical space for such a
            // paragraph, sized to its effective font size (the run's/endParaRPr's
            // sz, default 18pt). Without a placeholder, an empty-text run div
            // collapses to zero height and the blank line disappears. Emit a
            // sized &nbsp; so the line occupies its proper height.
            bool hasVisibleText = runs.Any(r => !string.IsNullOrEmpty(r.Text?.Text));
            if (!hasVisibleText && !hasMath)
            {
                // Empty paragraph (blank line) — size the &nbsp; to the effective
                // font size so it isn't zero-height. Precedence: first run rPr sz
                // > endParaRPr sz > inherited placeholder default > 18pt.
                var emptySzHundredths = runs.FirstOrDefault()?.RunProperties?.FontSize?.Value
                    ?? para.GetFirstChild<Drawing.EndParagraphRunProperties>()?.FontSize?.Value
                    ?? defaultFontSizeHundredths
                    ?? 1800;
                sb.Append($"<span style=\"font-size:{emptySzHundredths / 100.0 * fontScale:0.##}pt\">&nbsp;</span>");
            }
            else
            {
                foreach (var run in runs)
                {
                    RenderRun(sb, run, themeColors, defaultFontSizeHundredths, placeholderPart, themeFontFallback, fontScale, defaultRunColor);
                }
            }

            // Line breaks within paragraph
            foreach (var br in para.Elements<Drawing.Break>())
                sb.Append("<br>");

            sb.AppendLine("</div>");
            isFirstPara = false;
        }
    }

    private static void RenderRun(StringBuilder sb, Drawing.Run run, Dictionary<string, string> themeColors,
        int? defaultFontSizeHundredths = null, OpenXmlPart? part = null, string? themeFontFallback = null,
        double fontScale = 1.0, string? defaultRunColor = null)
    {
        var text = run.Text?.Text ?? "";
        if (string.IsNullOrEmpty(text)) return;

        var styles = new List<string>();
        var rp = run.RunProperties;

        // Hyperlink resolution (RUN-level only; shape-level deferred).
        // Read <a:hlinkClick> from run.RunProperties, resolve relationship ID
        // via containing part's HyperlinkRelationships to an external URI.
        string? hyperlinkUrl = null;
        bool hasExplicitColor = rp?.GetFirstChild<Drawing.SolidFill>() != null
            || rp?.GetFirstChild<Drawing.GradientFill>() != null
            || rp?.GetFirstChild<Drawing.SchemeColor>() != null
            || rp?.GetFirstChild<Drawing.RgbColorModelHex>() != null;
        bool hasExplicitUnderline = rp?.Underline?.HasValue == true;
        var hlinkClick = rp?.GetFirstChild<Drawing.HyperlinkOnClick>();
        if (hlinkClick?.Id?.Value is string relId && part != null)
        {
            try
            {
                var rel = part.HyperlinkRelationships.FirstOrDefault(r => r.Id == relId);
                // Reject javascript:/vbscript:/data: etc. — OOXML hyperlink
                // relationships are attacker-controlled and HtmlEncode does not
                // neutralize a dangerous scheme. Mirrors the Word/Excel previews.
                if (rel?.Uri != null && Core.HyperlinkUriValidator.IsSafeScheme(rel.Uri.ToString()))
                    hyperlinkUrl = rel.Uri.ToString();
            }
            catch { }
        }

        // Font. Theme references (typeface starts with "+") are resolved to
        // their concrete major/minor face via the textbody-supplied fallback;
        // runs with no <a:rPr> at all (common on auto-generated title text)
        // also pick up the fallback so a /theme bodyFont / headingFont change
        // is visible in HTML preview.
        var runFont = rp?.GetFirstChild<Drawing.LatinFont>()?.Typeface?.Value
            ?? rp?.GetFirstChild<Drawing.EastAsianFont>()?.Typeface?.Value;
        string? resolvedRunFont = (runFont != null && !runFont.StartsWith("+", StringComparison.Ordinal))
            ? runFont
            : themeFontFallback;
        if (!string.IsNullOrEmpty(resolvedRunFont))
            styles.Add(CssFontFamilyWithFallback(resolvedRunFont));

        // Size — use explicit run size, fall back to inherited placeholder
        // default, else the real-PowerPoint plain-textbox default of 18pt.
        // Decided independently of whether an <a:rPr> element exists: a plain
        // textbox created with no run properties (rp == null) still gets the
        // 18pt default so the browser doesn't fall back to its ~12pt default.
        // Placeholders keep their layout/master size via defaultFontSizeHundredths.
        // Bug #8(B): multiply by the textbody's normAutofit fontScale (1.0 = none).
        if (rp?.FontSize?.HasValue == true)
            styles.Add($"font-size:{rp.FontSize.Value / 100.0 * fontScale:0.##}pt");
        else if (defaultFontSizeHundredths.HasValue)
            styles.Add($"font-size:{defaultFontSizeHundredths.Value / 100.0 * fontScale:0.##}pt");
        else
            styles.Add($"font-size:{18 * fontScale:0.##}pt");

        if (rp != null)
        {

            // Bold
            if (rp.Bold?.Value == true)
                styles.Add("font-weight:bold");

            // Italic
            if (rp.Italic?.Value == true)
                styles.Add("font-style:italic");

            // Underline + Strikethrough — both map to CSS text-decoration, which
            // is a single property: emitting it twice makes the cascade keep only
            // the last (dropping the first line). Build the combined line keyword
            // set ("underline", "line-through") plus an optional decoration STYLE
            // (double/wavy/dotted/dashed) and a thickness, then emit ONE
            // `text-decoration` declaration. text-decoration-style applies to all
            // lines, so a strike+underline mix where one wants double/wavy is a
            // known CSS limitation — the common single-line strike+underline case
            // renders both lines correctly.
            var decoLines = new List<string>();
            string? decoStyle = null;
            string? decoThickness = null;
            if (rp.Underline?.HasValue == true && rp.Underline.Value != Drawing.TextUnderlineValues.None)
            {
                decoLines.Add("underline");
                var u = rp.Underline.Value;
                if (u == Drawing.TextUnderlineValues.Double)
                {
                    // CONSISTENCY(underline-variants): mirrors WordHandler's
                    // emitter. Chromium renders this as two distinct lines at
                    // common font sizes (verified via Word HTML preview at 18pt).
                    decoStyle = "double";
                }
                else if (u == Drawing.TextUnderlineValues.Wavy)
                {
                    decoStyle = "wavy";
                }
                else if (u == Drawing.TextUnderlineValues.WavyHeavy
                    || u == Drawing.TextUnderlineValues.WavyDouble)
                {
                    // best-effort: CSS has no wavy+double; emit wavy thicker.
                    decoStyle = "wavy";
                    decoThickness = "2px";
                }
                else if (u == Drawing.TextUnderlineValues.Dotted)
                {
                    decoStyle = "dotted";
                }
                else if (u == Drawing.TextUnderlineValues.HeavyDotted)
                {
                    decoStyle = "dotted";
                    decoThickness = "2px";
                }
                else if (u == Drawing.TextUnderlineValues.Dash
                    || u == Drawing.TextUnderlineValues.DashLong)
                {
                    decoStyle = "dashed";
                }
                else if (u == Drawing.TextUnderlineValues.DashHeavy
                    || u == Drawing.TextUnderlineValues.DashLongHeavy
                    || u == Drawing.TextUnderlineValues.DotDashHeavy
                    || u == Drawing.TextUnderlineValues.DotDotDashHeavy)
                {
                    decoStyle = "dashed";
                    decoThickness = "2px";
                }
                else if (u == Drawing.TextUnderlineValues.DotDash
                    || u == Drawing.TextUnderlineValues.DotDotDash)
                {
                    // TODO CONSISTENCY(underline-variants): CSS has no dot-dash
                    // pattern; approximate with dashed.
                    decoStyle = "dashed";
                }
                else if (u == Drawing.TextUnderlineValues.Heavy)
                {
                    decoStyle = "solid";
                    decoThickness = "2px";
                }
                // else: exotic combos (Words, HeavyWords, etc.) fall back to plain underline.
            }

            if (rp.Strike?.HasValue == true && rp.Strike.Value != Drawing.TextStrikeValues.NoStrike)
            {
                decoLines.Add("line-through");
                if (rp.Strike.Value == Drawing.TextStrikeValues.DoubleStrike && decoStyle == null)
                {
                    // CONSISTENCY(underline-variants): like underline `double`,
                    // `line-through double` may render visually identical to
                    // single at typical font sizes in Chromium. Known limitation.
                    decoStyle = "double";
                }
            }

            if (decoLines.Count > 0)
            {
                styles.Add($"text-decoration:{string.Join(" ", decoLines)}");
                if (decoStyle != null) styles.Add($"text-decoration-style:{decoStyle}");
                if (decoThickness != null) styles.Add($"text-decoration-thickness:{decoThickness}");

                // Underline color (a:uFill). When distinct from the text color,
                // PowerPoint paints the underline in its own color; emit
                // text-decoration-color so the line isn't drawn in the text color.
                var uFill = rp.GetFirstChild<Drawing.UnderlineFill>();
                if (uFill != null)
                {
                    var uColor = ResolveFillColor(uFill.GetFirstChild<Drawing.SolidFill>(), themeColors);
                    if (uColor != null)
                    {
                        var textColor = ResolveFillColor(rp.GetFirstChild<Drawing.SolidFill>(), themeColors);
                        if (!string.Equals(uColor, textColor, StringComparison.OrdinalIgnoreCase))
                            styles.Add($"text-decoration-color:{uColor}");
                    }
                }
            }

            // Caps (rPr/@cap). all → text-transform:uppercase; small → font-variant-caps:small-caps
            // (browsers fall back to synthetic small-caps when the font lacks the SC variant).
            if (rp.Capital?.HasValue == true && rp.Capital.Value != Drawing.TextCapsValues.None)
            {
                if (rp.Capital.Value == Drawing.TextCapsValues.All)
                    styles.Add("text-transform:uppercase");
                else if (rp.Capital.Value == Drawing.TextCapsValues.Small)
                    styles.Add("font-variant-caps:all-small-caps");
            }

            // Color
            var solidFill = rp.GetFirstChild<Drawing.SolidFill>();
            var color = ResolveFillColor(solidFill, themeColors);
            if (color != null)
                styles.Add($"color:{color}");
            // R7-2: no explicit run color and no gradient/scheme fill — inherit the
            // default color resolved from the master/layout placeholder cascade.
            else if (!hasExplicitColor && hlinkClick == null && defaultRunColor != null)
                styles.Add($"color:{defaultRunColor}");

            // Text highlight (a:highlight). Authored only in real PowerPoint /
            // via raw-set (no officecli prop), but `view` renders arbitrary
            // files so honor it. The highlight's color child has the same shape
            // as a solidFill's (srgbClr / schemeClr), so wrap it in a throwaway
            // SolidFill to reuse ResolveFillColor (theme + transforms).
            var highlight = rp.GetFirstChild<Drawing.Highlight>();
            var hlColorChild = highlight?.GetFirstChild<Drawing.RgbColorModelHex>()
                ?? (OpenXmlElement?)highlight?.GetFirstChild<Drawing.SchemeColor>();
            if (hlColorChild != null)
            {
                var hlFill = new Drawing.SolidFill(hlColorChild.CloneNode(true));
                var hlColor = ResolveFillColor(hlFill, themeColors);
                if (hlColor != null)
                    styles.Add($"background-color:{hlColor}");
            }

            // Gradient text fill
            var gradFill = rp.GetFirstChild<Drawing.GradientFill>();
            if (gradFill != null)
            {
                var gradCss = GradientToCss(gradFill, themeColors);
                if (!string.IsNullOrEmpty(gradCss))
                {
                    styles.Add($"background:{gradCss}");
                    styles.Add("-webkit-background-clip:text");
                    styles.Add("background-clip:text");
                    styles.Add("-webkit-text-fill-color:transparent");
                }
            }

            // R9-3: image (blip) text fill — clip the image to the glyphs, mirroring
            // the gradFill text-fill approach above.
            var runBlipFill = rp.GetFirstChild<Drawing.BlipFill>();
            if (runBlipFill != null && part != null)
            {
                var dataUri = BlipToDataUri(runBlipFill, part);
                if (!string.IsNullOrEmpty(dataUri))
                {
                    styles.Add($"background-image:url('{dataUri}')");
                    styles.Add("background-size:cover");
                    styles.Add("-webkit-background-clip:text");
                    styles.Add("background-clip:text");
                    styles.Add("color:transparent");
                    styles.Add("-webkit-text-fill-color:transparent");
                }
            }

            // Run-level text outline (a:rPr/a:ln). PowerPoint strokes each glyph
            // edge; Chromium renders this via -webkit-text-stroke. Width is the
            // a:ln @w in EMU (12700 EMU = 1pt); convert to px (1pt = 4/3 px) so a
            // 3pt outline reads as a ~4px stroke. Color comes from the a:ln's
            // solidFill child (default black when absent). paint-order:stroke fill
            // keeps the fill painted on top so the stroke hugs the glyph outside.
            var runOutline = rp.GetFirstChild<Drawing.Outline>();
            if (runOutline != null)
            {
                double strokePx = runOutline.Width?.HasValue == true
                    ? Units.EmuToPt(runOutline.Width.Value) * 4.0 / 3.0
                    : 1.0;
                var strokeColor = ResolveFillColor(runOutline.GetFirstChild<Drawing.SolidFill>(), themeColors)
                    ?? "#000000";
                styles.Add($"-webkit-text-stroke:{strokePx:0.##}px {strokeColor}");
                styles.Add("paint-order:stroke fill");
            }

            // Character spacing
            if (rp.Spacing?.HasValue == true)
                styles.Add($"letter-spacing:{rp.Spacing.Value / 100.0:0.##}pt");

            // Superscript/subscript
            if (rp.Baseline?.HasValue == true && rp.Baseline.Value != 0)
            {
                if (rp.Baseline.Value > 0)
                    styles.Add("vertical-align:super;font-size:smaller");
                else
                    styles.Add("vertical-align:sub;font-size:smaller");
            }
        }
        // R7-2: run with no <a:rPr> at all — still inherit the cascade default color.
        else if (hlinkClick == null && defaultRunColor != null)
            styles.Add($"color:{defaultRunColor}");

        // Auto-style hyperlink runs that lack explicit color/underline. Uses
        // theme-less fallback #0563C1 (PowerPoint default hyperlink color).
        // Shape-level hyperlinks are deferred (R14-supplemental).
        if (hlinkClick != null)
        {
            if (!hasExplicitColor)
                styles.Add(themeColors.TryGetValue("hlink", out var hlinkHex) && !string.IsNullOrEmpty(hlinkHex)
                    ? $"color:#{hlinkHex}"
                    : "color:#0563C1");
            if (!hasExplicitUnderline) styles.Add("text-decoration:underline");
        }

        // Tab chars (literal U+0009 inside <a:t>, the form the Add path writes)
        // would collapse to a single space in HTML. Replace each with an
        // inline-block spacer so tab-separated columns keep visible spacing,
        // matching how PowerPoint advances to the next tab stop.
        string encoded = HtmlEncode(text).Replace("\t",
            "<span class=\"tab-spacer\" style=\"display:inline-block;width:0.5in\"></span>");

        string inner = styles.Count > 0
            ? $"<span style=\"{string.Join(";", styles)}\">{encoded}</span>"
            : encoded;

        if (!string.IsNullOrEmpty(hyperlinkUrl))
        {
            sb.Append($"<a href=\"{HtmlEncode(hyperlinkUrl)}\" rel=\"noopener\">{inner}</a>");
        }
        else
        {
            sb.Append(inner);
        }
    }

    // Format an auto-numbered bullet glyph (e.g. "1.", "(a)", "iv)") for a given
    // OOXML scheme and 1-based index. Covers the common schemes emitted by
    // ApplyListStyle; unsupported schemes fall back to "N." arabic-period.
    private static string FormatAutoNumberGlyph(string key, int n)
    {
        // `key` is the OOXML buAutoNum value, of form
        // "{alphaLc|alphaUc|romanLc|romanUc|arabic|...}{Period|ParenBoth|ParenR|Plain|Minus}".
        // Match on this token directly — NOT the C# enum member name
        // (TextAutoNumberSchemeValues.AlphaLowerCharacterPeriod.ToString() yields
        // "AlphaLowerCharacterPeriod", which never matched "alphaLc" and silently
        // fell through to decimal for every non-arabic scheme).
        string body;
        if (key.StartsWith("alphaLc", StringComparison.OrdinalIgnoreCase) || key.StartsWith("AlphaLc", StringComparison.OrdinalIgnoreCase))
            body = ToAlpha(n, upper: false);
        else if (key.StartsWith("alphaUc", StringComparison.OrdinalIgnoreCase) || key.StartsWith("AlphaUc", StringComparison.OrdinalIgnoreCase))
            body = ToAlpha(n, upper: true);
        else if (key.StartsWith("romanLc", StringComparison.OrdinalIgnoreCase) || key.StartsWith("RomanLc", StringComparison.OrdinalIgnoreCase))
            body = ToRoman(n).ToLowerInvariant();
        else if (key.StartsWith("romanUc", StringComparison.OrdinalIgnoreCase) || key.StartsWith("RomanUc", StringComparison.OrdinalIgnoreCase))
            body = ToRoman(n);
        else
            body = n.ToString();

        if (key.EndsWith("Period", StringComparison.OrdinalIgnoreCase)) return body + ".";
        if (key.EndsWith("ParenBoth", StringComparison.OrdinalIgnoreCase)) return "(" + body + ")";
        if (key.EndsWith("ParenR", StringComparison.OrdinalIgnoreCase)) return body + ")";
        if (key.EndsWith("Minus", StringComparison.OrdinalIgnoreCase)) return "- " + body + " -";
        if (key.EndsWith("Plain", StringComparison.OrdinalIgnoreCase)) return body;
        return body + ".";
    }

    private static string ToAlpha(int n, bool upper)
    {
        if (n <= 0) n = 1;
        var sb = new StringBuilder();
        while (n > 0)
        {
            n--;
            sb.Insert(0, (char)((upper ? 'A' : 'a') + (n % 26)));
            n /= 26;
        }
        return sb.ToString();
    }

    // True when the shape is a title-class placeholder (title or centeredTitle).
    // Title placeholders inherit the theme major (heading) face; everything else
    // inherits minor (body). SubTitle uses the minor face in PowerPoint, so it
    // is intentionally NOT included here.
    private static bool IsTitlePlaceholder(Shape? shape)
    {
        var ph = shape?.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties
            ?.GetFirstChild<PlaceholderShape>();
        if (ph?.Type?.HasValue != true) return false;
        var t = ph.Type.Value;
        return t == PlaceholderValues.Title || t == PlaceholderValues.CenteredTitle;
    }

    // Resolve the theme major/minor Latin typeface for the slide owning `part`.
    // Returns null when no theme is reachable (e.g. orphan text body, or a part
    // whose master->theme chain is incomplete). kind is "major" or "minor".
    private static string? ResolveThemeFontTypeface(OpenXmlPart? part, string kind)
    {
        var theme = part switch
        {
            SlidePart sp => sp.SlideLayoutPart?.SlideMasterPart?.ThemePart?.Theme,
            SlideLayoutPart lp => lp.SlideMasterPart?.ThemePart?.Theme,
            SlideMasterPart mp => mp.ThemePart?.Theme,
            _ => null,
        };
        var fontScheme = theme?.ThemeElements?.FontScheme;
        var typeface = kind == "major"
            ? fontScheme?.MajorFont?.GetFirstChild<Drawing.LatinFont>()?.Typeface?.Value
            : fontScheme?.MinorFont?.GetFirstChild<Drawing.LatinFont>()?.Typeface?.Value;
        if (string.IsNullOrEmpty(typeface)) return null;
        // Theme entries are sometimes self-referential ("+mj-lt"); skip those.
        if (typeface.StartsWith("+", StringComparison.Ordinal)) return null;
        return typeface;
    }

    private static string ToRoman(int n)
    {
        if (n <= 0) return n.ToString();
        int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        string[] numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        var sb = new StringBuilder();
        for (int i = 0; i < values.Length; i++)
        {
            while (n >= values[i]) { sb.Append(numerals[i]); n -= values[i]; }
        }
        return sb.ToString();
    }
}
