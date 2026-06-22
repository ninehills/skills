// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

namespace OfficeCli.Core;

/// <summary>
/// Locale → default font mapping for fresh blank documents. Mirrors the
/// data-driven approach used by mature producers (VCL.xcu): given a locale tag, pick
/// reasonable defaults for the Latin / EastAsian / ComplexScript font slots.
///
/// We deliberately keep this small (one line per locale family) rather than
/// trying to model every Office localization. When no locale is supplied,
/// returning all-empty values lets the host application substitute its own
/// UI-locale defaults — the behaviour BlankDocCreator already had after
/// we removed the "宋体" hardcode.
///
/// Font names are chosen for cross-platform availability (typefaces commonly
/// shipped on Windows and macOS, plus Apple Sans equivalents).
/// </summary>
public static class LocaleFontRegistry
{
    /// <summary>
    /// Resolve a locale tag (e.g. "zh-CN", "ja", "ar-SA") to a per-script
    /// font triple. Returns (null, null, null) when no locale is supplied
    /// or the tag is unknown — callers should treat that as "leave the
    /// docDefaults blank, let the host application decide".
    /// </summary>
    public static (string? Latin, string? EastAsia, string? ComplexScript) Resolve(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return (null, null, null);

        // Match on language-only first; full tag lookups (e.g. zh-Hant) are
        // routed through the language-only entry unless a region-specific
        // variant exists.
        var lower = locale.Replace('_', '-').ToLowerInvariant();
        var lang = lower.Split('-')[0];

        // Fully-tagged regional variants take precedence.
        switch (lower)
        {
            case "zh-tw" or "zh-hk" or "zh-mo" or "zh-hant":
                return ("Times New Roman", "新細明體", null);
            case "zh-cn" or "zh-sg" or "zh-hans":
                return ("Times New Roman", "等线", null);
        }

        // Language-only fall-throughs.
        return lang switch
        {
            "zh" => ("Times New Roman", "等线", null),
            "ja" => ("Times New Roman", "游明朝", null),
            "ko" => ("Times New Roman", "맑은 고딕", null),
            "ar" => ("Times New Roman", null, "Arabic Typesetting"),
            "he" => ("Times New Roman", null, "Times New Roman"),
            "th" => ("Times New Roman", null, "Tahoma"),
            "fa" => ("Times New Roman", null, "B Nazanin"),
            "ur" => ("Times New Roman", null, "Jameel Noori Nastaleeq"),
            "hi" => ("Times New Roman", null, "Mangal"),
            "en" or "fr" or "de" or "es" or "it" or "pt" or "nl" or "ru" or "pl"
                => ("Times New Roman", null, null),
            _ => (null, null, null)
        };
    }

    /// <summary>
    /// OS user culture captured once at process startup, before the rest of
    /// the cli forces the thread-current culture to Invariant for
    /// deterministic OOXML / JSON / CSS output (see Program.cs). Read by
    /// <see cref="ResolveEffectiveLocale"/> to recover the user's actual
    /// language even though <c>CultureInfo.CurrentCulture</c> reports
    /// Invariant from inside command handlers. Set by Program.cs and
    /// treated as immutable from then on. Tests assign directly to
    /// override.
    /// </summary>
    public static string? OsLocaleSnapshot { get; set; }

    /// <summary>
    /// Pick the effective locale for a newly-created document: explicit
    /// `--locale` wins when supplied; otherwise fall back to the OS user
    /// culture captured at startup so Arabic/Hebrew/Chinese/… users get a
    /// doc shaped for their language without having to repeat the locale
    /// on every invocation.
    ///
    /// The startup snapshot honors CFLocale on macOS, $LANG / $LC_ALL on
    /// Linux, and the OS user UI culture on Windows. In CI / Docker
    /// images without locale config the runtime reports InvariantCulture
    /// (empty Name), and bare `LANG=C` / `LANG=POSIX` map to the same —
    /// treat all three as "no locale" so AI agents and pipelines don't
    /// accidentally bake the build machine's culture into output docs.
    /// </summary>
    public static string? ResolveEffectiveLocale(string? explicitLocale)
    {
        if (!string.IsNullOrWhiteSpace(explicitLocale)) return explicitLocale;
        var name = OsLocaleSnapshot;
        if (string.IsNullOrEmpty(name)) return null;
        if (name.Equals("C", StringComparison.OrdinalIgnoreCase)) return null;
        if (name.Equals("POSIX", StringComparison.OrdinalIgnoreCase)) return null;
        // Latin-script Western locales (en/fr/de/es/it/pt/nl/ru/pl/…) carry
        // no locale-specific instruction — the doc would get the same
        // Calibri / LTR baseline either way. Skip them so a default-shell
        // English/French/… user keeps the modern Calibri baseline rather
        // than the older Times New Roman we'd otherwise bake from
        // <see cref="Resolve"/>. Explicit `--locale en-US` still goes
        // through (it expresses intent — "I want this Latin font slot
        // pinned"). This is the auto-detect-only filter.
        var lang = name.Replace('_', '-').ToLowerInvariant().Split('-')[0];
        if (lang is "en" or "fr" or "de" or "es" or "it" or "pt" or "nl" or "ru" or "pl")
            return null;
        return name;
    }

    /// <summary>
    /// Locale-implied reading direction. RTL when the locale's primary
    /// script flows right-to-left: Arabic, Hebrew, Yiddish, Urdu, Persian
    /// (Farsi), Kashmiri, Sindhi, Uighur, Pashto, N'Ko, Dhivehi, Syriac,
    /// and Kurdish written in Arabic script. Used by BlankDocCreator to
    /// stamp <w:bidi/> defaults on sectPr and pPrDefault so users with
    /// `--locale ar-SA` (etc.) don't have to set direction=rtl on every
    /// paragraph they add.
    /// </summary>
    public static bool IsRightToLeft(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return false;
        var lang = locale.Replace('_', '-').ToLowerInvariant().Split('-')[0];
        return lang switch
        {
            "ar"   // Arabic
            or "he" // Hebrew
            or "iw" // Hebrew (legacy ISO 639-1)
            or "yi" // Yiddish
            or "ji" // Yiddish (legacy)
            or "ur" // Urdu
            or "fa" // Persian / Farsi
            or "ps" // Pashto
            or "sd" // Sindhi
            or "ks" // Kashmiri
            or "ug" // Uighur
            or "ku" // Kurdish (Arabic-script variants — Sorani most commonly)
            or "ckb" // Central Kurdish (Sorani)
            or "dv" // Dhivehi / Maldivian
            or "syr" // Syriac
            or "nqo" // N'Ko
                => true,
            _ => false
        };
    }

    /// <summary>
    /// Returns a CSS font-family fallback fragment for the locale's CJK script,
    /// used by HTML/SVG renderers when the document's declared font isn't
    /// installed on the rendering machine.
    ///
    /// The returned fragment is comma-separated, individually quoted, NOT
    /// prefixed with a comma — callers concatenate as needed. Empty string
    /// for unknown/unspecified locales: callers should fall through to a
    /// neutral generic family (e.g. <c>sans-serif</c>) so the rendering OS
    /// picks a reasonable default rather than forcing one script's glyphs.
    /// </summary>
    public static string GetCjkCssFallback(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return "";
        var lang = locale.Replace('_', '-').ToLowerInvariant().Split('-')[0];
        return lang switch
        {
            "zh" => "'PingFang SC', 'Microsoft YaHei', 'Noto Sans CJK SC', 'Hiragino Sans GB', 'Songti SC', 'STSong'",
            "ja" => "'Hiragino Sans', 'Hiragino Mincho ProN', 'Yu Gothic', 'Yu Mincho', 'Noto Sans CJK JP', 'MS Gothic'",
            "ko" => "'Apple SD Gothic Neo', 'Malgun Gothic', 'Noto Sans CJK KR', 'Batang'",
            _ => ""
        };
    }

    /// <summary>
    /// Heuristic: detect a CJK locale tag ("zh" / "ja" / "ko") from a font
    /// typeface name. Returns null when the name carries no strong script
    /// signal. Used by renderers to pick the right fallback chain when the
    /// document doesn't declare an explicit eastAsia language tag.
    ///
    /// Order matters: Japanese is checked before Chinese because some JP
    /// font names contain hanzi that overlap with Chinese keywords.
    /// </summary>
    public static string? DetectLocaleFromCjkFontName(string? font)
    {
        if (string.IsNullOrEmpty(font)) return null;
        var lower = font.ToLowerInvariant();

        if (lower.Contains("明朝") || lower.Contains("mincho")
            || lower.Contains("ゴシック") || lower.Contains("hiragino")
            || lower.Contains("yu mincho") || lower.Contains("yu gothic")
            || lower.Contains("ms mincho") || lower.Contains("ms gothic")
            || lower.Contains("meiryo") || lower.Contains("游明朝")
            || lower.Contains("游ゴシック"))
            return "ja";

        if (lower.Contains("바탕") || lower.Contains("굴림") || lower.Contains("돋움")
            || lower.Contains("맑은") || lower == "batang" || lower == "batangche"
            || lower == "gulim" || lower == "dotum" || lower.Contains("malgun")
            || lower.Contains("nanum") || lower.Contains("apple sd gothic"))
            return "ko";

        if (lower.Contains("宋") || lower.Contains("song") || lower.Contains("simsun")
            || lower.Contains("黑") || lower.Contains("hei") || lower.Contains("simhei")
            || lower.Contains("楷") || lower.Contains("kai") || lower.Contains("仿宋")
            || lower.Contains("fangsong") || lower.Contains("pingfang")
            || lower.Contains("yahei") || lower.Contains("等线") || lower.Contains("华文")
            || lower.Contains("方正") || lower.Contains("微软雅黑"))
            return "zh";

        return null;
    }
}
