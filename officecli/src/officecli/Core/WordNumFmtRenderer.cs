// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;

namespace OfficeCli.Core;

/// <summary>
/// Converts a 1-based counter into the OOXML <c>w:numFmt</c> marker glyphs.
/// Covers the numFmt enum from ECMA-376 §17.18.59 that Word ships with;
/// unknown or unmapped values fall back to decimal.
/// </summary>
public static class WordNumFmtRenderer
{
    public static string Render(int n, string? numFmt)
    {
        var fmt = (numFmt ?? "decimal").ToLowerInvariant();
        // Plain decimal can represent 0 and negatives directly (a <w:start>
        // of 0 must render "0", not a clamped "1" that duplicates the next
        // item's marker). Every other format — letters, roman, the CJK/Korean
        // counting tables, enclosed glyphs — has no zero or negative form, so
        // keep clamping those to 1 (matches Word).
        if (n < 1 && fmt != "decimal") n = 1;
        switch (fmt)
        {
            case "decimal": return n.ToString(CultureInfo.InvariantCulture);
            case "decimalzero": return n < 10 ? $"0{n}" : n.ToString(CultureInfo.InvariantCulture);
            case "upperroman": return ToRoman(n).ToUpperInvariant();
            case "lowerroman": return ToRoman(n).ToLowerInvariant();
            case "upperletter": return ToAlpha(n, uppercase: true);
            case "lowerletter": return ToAlpha(n, uppercase: false);
            case "ordinal": return ToOrdinal(n);
            // Word capitalizes only the FIRST letter of the whole spelled-out
            // phrase: "Twenty-one", "One hundred", "Twenty-first" — not the
            // title-cased "Twenty-One" / "One Hundred" the builders produce.
            case "cardinaltext": return CapitalizeFirst(ToEnglishCardinal(n));
            case "ordinaltext": return CapitalizeFirst(ToEnglishOrdinal(n));
            // Ordinary chinese/taiwanese counting: 十-grouped for 1-99, then
            // digit-by-digit with 〇 for >=100 (100 → 一〇〇). Identical between
            // the two (the only simplified/traditional split, 万 vs 萬, lives at
            // the 10000 place, which is rendered digit-by-digit here).
            case "chinesecounting":
            case "taiwanesecounting":
                return ToChineseCountingHybrid(n);
            // *CountingThousand fully spell the 百/千/万 grouping with 〇 internal
            // zeros (101 → 一百〇一); traditional uses 萬 at the 10000 place.
            case "chinesecountingthousand":
                return BuildCjkGrouped(n, CnDigits, '十', '百', '千', '万', '〇', dropZeros: false, OneStyle.Chinese);
            case "taiwanesecountingthousand":
                return BuildCjkGrouped(n, CnDigits, '十', '百', '千', '萬', '〇', dropZeros: false, OneStyle.Chinese);
            // japaneseCounting: grouped, drops the leading 一 before 十/百/千 and
            // drops internal zeros, but keeps 一 before 万 (二千二十五, 一万).
            case "japanesecounting":
                return BuildCjkGrouped(n, CnDigits, '十', '百', '千', '万', '\0', dropZeros: true, OneStyle.Japanese);
            // Simplified/traditional legal grouping: keep the leading digit before
            // every unit (壹拾/壹佰/壹仟/壹萬), keep 零 zeros. Both use 萬 at 10000.
            case "chineselegalsimplified":
                return BuildCjkGrouped(n, CnLegalSimplDigits, '拾', '佰', '仟', '萬', '零', dropZeros: false, OneStyle.KeepAll);
            case "ideographlegaltraditional":
                return BuildCjkGrouped(n, CnFormalDigits, '拾', '佰', '仟', '萬', '零', dropZeros: false, OneStyle.KeepAll);
            // Digit-by-digit ideograph numerals with 〇 zero (100 → 一〇〇).
            // koreanDigital2 renders these han ideographs despite the "korean"
            // name — NOT sino-Hangul digits.
            case "ideographdigital":
            case "taiwanesedigital":
            case "japanesedigitaltenthousand":
            case "koreandigital2":
                return ToDigitByDigit(n, CnDigits, '〇');
            case "koreandigital":
                return ToKoreanDigital(n);
            case "koreancounting":
                return ToKoreanCounting(n);
            case "koreanlegal":
                return ToKoreanLegal(n);
            case "japaneselegal":
                return ToJapaneseLegal(n);
            // Japanese kana enumeration. Word renders aiueo/aiueoFullWidth as
            // full-width katakana in gojūon order, and iroha/irohaFullWidth as
            // full-width katakana in iroha order. The bare and *FullWidth
            // tokens produce identical full-width glyphs in Word.
            case "aiueo":
            case "aiueofullwidth":
                return ToRecycledTable(n, KatakanaAiueo);
            case "iroha":
            case "irohafullwidth":
                return ToRecycledTable(n, KatakanaIroha);
            // Korean leading consonants (초성), Hangul Compatibility Jamo block.
            case "chosung":
                return ToRecycledTable(n, KoreanChosung);
            case "ideographtraditional":
                return ToHeavenlyStems(n);
            case "ideographzodiac":
                return ToEarthlyBranches(n);
            // Sexagenary (干支) cycle: heavenly-stem + earthly-branch pair,
            // 甲子 乙丑 丙寅 … (60-cycle). Distinct from ideographZodiac, which
            // is the bare earthly branch. Verified via officeshot.
            case "ideographzodiactraditional":
                return ToSexagenary(n);
            case "decimalenclosedcircle":
            case "decimalenclosedcirclechinese":
                return ToEnclosedCircle(n);
            case "decimalenclosedfullstop":
                return ToEnclosedFullStop(n);
            case "decimalenclosedparen":
                return ToEnclosedParen(n);
            case "decimalfullwidth":
            case "decimalfullwidth2":
                return ToFullWidthDigits(n);
            case "decimalhalfwidth":
                return n.ToString(CultureInfo.InvariantCulture);
            case "arabicabjad":
                return ToArabicAbjad(n);
            case "arabicalpha":
                return ToArabicAlpha(n);
            case "hebrew1":
            case "hebrew2":
                return ToHebrewNumeral(n);
            case "thainumbers":
            case "thaicounting":
                return ToThaiDigits(n);
            case "thailetters":
                return ToThaiLetters(n);
            case "hindinumbers":
            case "hindicounting":
            case "hindicardinaltext":
                return ToDevanagariDigits(n);
            // Word INVERTS these two relative to their ECMA-376 names (the same
            // name-vs-glyph mismatch as the korean* formats): the "hindiConsonants"
            // format actually enumerates the Devanagari VOWELS (अ आ इ …) and
            // "hindiVowels" enumerates the CONSONANTS (क ख ग …). Map by what Word
            // renders, not by the literal name. ("hindiLetters" stays a tolerant
            // alias of hindiConsonants.) Past the glyph set Word doubles — the
            // exact overflow set is unverified, so the table recycles (deferred).
            case "hindiconsonants":
            case "hindiletters":
                return ToHindiVowels(n);
            case "hindivowels":
                return ToHindiLetters(n);
            case "russianlower":
                return ToRussianAlpha(n, uppercase: false);
            case "russianupper":
                return ToRussianAlpha(n, uppercase: true);
            // Uppercase hexadecimal: 1..9, A..F, 10→"A", 16→"10". Word renders
            // the counter as base-16 (n.ToString("X")). Verified via officeshot.
            case "hex":
                return n.ToString("X", CultureInfo.InvariantCulture);
            // Footnote symbol cycle * † ‡ § (U+002A, U+2020, U+2021, U+00A7),
            // doubling each glyph past the 4th (5→**, 6→††, …). ECMA-376
            // §17.18.59 "chicago"; verified via officeshot.
            case "chicago":
                return ToChicago(n);
            // Korean syllable enumeration 가 나 다 … (leading consonant + ㅏ,
            // 14 glyphs), recycling past 14. Verified via officeshot.
            case "ganada":
                return ToRecycledTable(n, KoreanGanada);
            // Parenthesized CJK ideograph ㈠ ㈡ … ㈩ (U+3220..U+3229, 1..10);
            // past 10 Word falls back to plain decimal. Verified via officeshot.
            case "ideographenclosedcircle":
                return ToParenthesizedIdeograph(n);
            // Number wrapped in spaced dashes: "- 1 -", "- 2 -". Verified via
            // officeshot (spaces inside both dashes).
            case "numberindash":
                return $"- {n.ToString(CultureInfo.InvariantCulture)} -";
            case "none": return "";
            case "bullet": return "\u2022";
            default: return n.ToString(CultureInfo.InvariantCulture);
        }
    }

    // ---------- helpers ----------

    private static string ToRoman(int n)
    {
        if (n <= 0 || n > 3999) return n.ToString(CultureInfo.InvariantCulture);
        int[] vals = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        string[] syms = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        var sb = new StringBuilder();
        for (int i = 0; i < vals.Length; i++)
            while (n >= vals[i]) { sb.Append(syms[i]); n -= vals[i]; }
        return sb.ToString();
    }

    private static string ToAlpha(int n, bool uppercase)
    {
        // Word's behavior: A,B,...,Z,AA,BB,CC,... (repeating letter at 27+), not Excel column-style.
        if (n < 1) n = 1;
        var letter = (char)(((n - 1) % 26) + (uppercase ? 'A' : 'a'));
        // Cap repeat to a sensible upper bound — an adversarial
        // <w:start val="2000000000"/> otherwise allocates a 160MB string
        // per list item (DoS). Word itself stops reasonably at a few
        // dozen repeats in practice.
        var repeat = Math.Min(((n - 1) / 26) + 1, 64);
        return new string(letter, repeat);
    }

    private static string ToOrdinal(int n)
    {
        int mod100 = n % 100, mod10 = n % 10;
        string suffix = (mod100 is >= 11 and <= 13) ? "th" : mod10 switch
        {
            1 => "st", 2 => "nd", 3 => "rd", _ => "th"
        };
        return $"{n}{suffix}";
    }

    private static readonly string[] EnglishOnes =
    {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    };
    private static readonly string[] EnglishTens =
    {
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    };

    // Short-scale group names, largest first. Each entry's divisor is a
    // power of one thousand. int caps at ~2.1e9 so Billion is the largest
    // reachable group, but Trillion/Quadrillion are listed for clarity and
    // in case the input type ever widens.
    private static readonly (long Divisor, string Name)[] EnglishScales =
    {
        (1_000_000_000_000_000_000L, "Quintillion"),
        (1_000_000_000_000_000L, "Quadrillion"),
        (1_000_000_000_000L, "Trillion"),
        (1_000_000_000L, "Billion"),
        (1_000_000L, "Million"),
        (1_000L, "Thousand"),
    };

    // Sentence-case a spelled-out number: first letter upper, the rest lower
    // (the cardinal/ordinal builders title-case every component for internal
    // assembly; Word renders only the leading capital).
    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

    private static string ToEnglishCardinal(int n) => ToEnglishCardinal((long)n);

    private static string ToEnglishCardinal(long n)
    {
        if (n == 0) return "Zero";
        if (n < 0) return $"Negative {ToEnglishCardinal(-n)}";
        var sb = new StringBuilder();
        // Emit each thousand-power group with its scale word (One Million,
        // Two Thousand, …) instead of recursively re-appending "Thousand".
        foreach (var (divisor, name) in EnglishScales)
        {
            if (n >= divisor)
            {
                sb.Append(ToEnglishCardinal(n / divisor)).Append(' ').Append(name);
                n %= divisor;
                if (n > 0) sb.Append(' ');
            }
        }
        if (n >= 100) { sb.Append(EnglishOnes[n / 100]).Append(" Hundred"); n %= 100; if (n > 0) sb.Append(' '); }
        if (n >= 20) { sb.Append(EnglishTens[n / 10]); n %= 10; if (n > 0) sb.Append('-').Append(EnglishOnes[n]); }
        else if (n > 0) sb.Append(EnglishOnes[n]);
        return sb.ToString();
    }

    private static string ToEnglishOrdinal(int n)
    {
        var card = ToEnglishCardinal(n);
        // Only transform the trailing word.
        var lastSpace = card.LastIndexOf(' ');
        var lastHyphen = card.LastIndexOf('-');
        var split = Math.Max(lastSpace, lastHyphen);
        var head = split >= 0 ? card[..(split + 1)] : "";
        var tail = split >= 0 ? card[(split + 1)..] : card;
        string suffixMap(string w) => w switch
        {
            "One" => "First", "Two" => "Second", "Three" => "Third", "Five" => "Fifth",
            "Eight" => "Eighth", "Nine" => "Ninth", "Twelve" => "Twelfth",
            _ => w.EndsWith("y", StringComparison.Ordinal) ? w[..^1] + "ieth"
                 : w.EndsWith("e", StringComparison.Ordinal) ? w[..^1] + "th"
                 : w + "th"
        };
        return head + suffixMap(tail);
    }

    private static readonly char[] CnDigits = { '零', '一', '二', '三', '四', '五', '六', '七', '八', '九' };
    private static readonly char[] CnFormalDigits = { '零', '壹', '貳', '參', '肆', '伍', '陸', '柒', '捌', '玖' };
    private static readonly char[] CnLegalSimplDigits = { '零', '壹', '贰', '叁', '肆', '伍', '陆', '柒', '捌', '玖' };

    // CJK numeric counting. Real Word uses several DISTINCT algorithms that must
    // not be conflated:
    //  • ordinary chinese/taiwanese counting: grouped (十/二十) for 1-99, then
    //    pure digit-by-digit with 〇 for >=100 (100 → 一〇〇, 2025 → 二〇二五).
    //  • *CountingThousand: fully grouped (一百/一千/一万) with 〇 internal zeros
    //    (101 → 一百〇一).
    //  • japaneseCounting: grouped, drops the leading 一 before 十/百/千 (百, 千)
    //    but keeps it before 万 (一万), and drops internal zeros (二千二十五).
    //  • koreanCounting: grouped sino-Hangul (백/천/만), drops the leading 일
    //    before every unit (만, not 일만), drops internal zeros (이천이십오).
    //  • legal/financial: grouped, KEEPS the leading digit before every unit
    //    (壹拾/壹佰/壹仟/壹萬); the ordinary legals keep 零 zeros, japaneseLegal
    //    drops them.
    // OneStyle captures how the leading "1" before a unit is handled.
    private enum OneStyle { Chinese, Japanese, Korean, KeepAll }

    private static string ToDigitByDigit(int n, char[] digits, char zero)
    {
        if (n == 0) return zero.ToString();
        var s = Math.Abs((long)n).ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder(s.Length + 1);
        if (n < 0) sb.Append('-');
        foreach (var c in s) sb.Append(c == '0' ? zero : digits[c - '0']);
        return sb.ToString();
    }

    /// <summary>Ordinary chinese/taiwanese counting: 1-99 grouped (十/二十), but
    /// >=100 switches to digit-by-digit with 〇 (一〇〇) — Word does not spell the
    /// 百/千 grouping for the plain "counting" formats (that is *CountingThousand).</summary>
    private static string ToChineseCountingHybrid(int n)
        => n is >= 1 and < 100
            ? BuildCjkGrouped(n, CnDigits, '十', '百', '千', '万', '〇', dropZeros: false, OneStyle.Chinese)
            : ToDigitByDigit(n, CnDigits, '〇');

    private static string BuildCjkGrouped(int n, char[] digits, char shi, char bai,
        char qian, char wan, char zero, bool dropZeros, OneStyle one)
    {
        if (n == 0) return dropZeros ? "" : zero.ToString();
        if (n < 0) return "-" + BuildCjkGrouped(-n, digits, shi, bai, qian, wan, zero, dropZeros, one);
        if (n >= 10000)
        {
            int hi = n / 10000, lo = n % 10000;
            // Korean drops the leading 일 before 만 (10000 → 만); the others keep
            // it (一万 / 壱萬).
            string s = hi == 1 && one == OneStyle.Korean
                ? wan.ToString()
                : BuildCjkGrouped(hi, digits, shi, bai, qian, wan, zero, dropZeros, one) + wan;
            if (lo == 0) return s;
            if (lo < 1000 && !dropZeros) s += zero;
            return s + BuildCjkGrouped(lo, digits, shi, bai, qian, wan, zero, dropZeros, one);
        }
        var sb = new StringBuilder();
        int q = n / 1000, b = (n / 100) % 10, sh = (n / 10) % 10, u = n % 10;
        bool emitted = false, pendingZero = false;
        void unit(int dig, char? unitCh)
        {
            if (dig == 0) { if (emitted) pendingZero = true; return; }
            if (pendingZero) { if (!dropZeros) sb.Append(zero); pendingZero = false; }
            bool dropOne = unitCh.HasValue && dig == 1 && one switch
            {
                OneStyle.Chinese => unitCh.Value == shi && !emitted, // 一十→十 only when leading
                OneStyle.Japanese => true,                           // 百/千 (万 handled above)
                OneStyle.Korean => true,
                _ => false,                                          // KeepAll: never drop
            };
            if (dropOne) sb.Append(unitCh!.Value);
            else { sb.Append(digits[dig]); if (unitCh.HasValue) sb.Append(unitCh.Value); }
            emitted = true;
        }
        unit(q, qian);
        unit(b, bai);
        unit(sh, shi);
        unit(u, null);
        return sb.ToString();
    }

    private static readonly string[] HeavenlyStems =
        { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    private static readonly string[] EarthlyBranches =
        { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    // Finite glyph sets: the 10 heavenly stems / 12 earthly branches enumerate
    // 甲..癸 / 子..亥 then fall back to plain decimal once the value exceeds the
    // set — Word does NOT cycle the glyphs back to 甲/子. Matches the decimal
    // fallback of ToParenthesizedIdeograph and the enclosed-glyph renderers.
    private static string ToHeavenlyStems(int n) =>
        n >= 1 && n <= 10 ? HeavenlyStems[n - 1] : n.ToString(CultureInfo.InvariantCulture);
    private static string ToEarthlyBranches(int n) =>
        n >= 1 && n <= 12 ? EarthlyBranches[n - 1] : n.ToString(CultureInfo.InvariantCulture);

    /// <summary>Sexagenary 干支 pair: heavenly-stem (10-cycle) + earthly-branch
    /// (12-cycle), e.g. 甲子 乙丑 … — a 60-element cycle.</summary>
    private static string ToSexagenary(int n)
    {
        if (n < 1) n = 1;
        return HeavenlyStems[(n - 1) % 10] + EarthlyBranches[(n - 1) % 12];
    }

    // Footnote symbol cycle (ECMA-376 §17.18.59 "chicago"): *, †, ‡, § then
    // doubled (**, ††, …), tripled, … as the index passes each table multiple.
    private static readonly char[] ChicagoSymbols = { '*', '†', '‡', '§' };

    private static string ToChicago(int n)
    {
        if (n < 1) n = 1;
        var glyph = ChicagoSymbols[(n - 1) % ChicagoSymbols.Length];
        var repeat = Math.Min(((n - 1) / ChicagoSymbols.Length) + 1, 64);
        return new string(glyph, repeat);
    }

    // Parenthesized CJK ideograph one..ten — U+3220..U+3229 (一..十). Word
    // renders ideographEnclosedCircle with these single glyphs for 1..10 and
    // falls back to plain decimal beyond 10.
    private static string ToParenthesizedIdeograph(int n)
    {
        if (n >= 1 && n <= 10) return ((char)(0x3220 + n - 1)).ToString();
        return n.ToString(CultureInfo.InvariantCulture);
    }

    private static string ToEnclosedCircle(int n)
    {
        // ① .. ⑳ = U+2460..U+2473 (1..20). Word's circled-number glyph set stops
        // at 20; beyond it the marker is plain decimal (not the U+325x/U+32Bx
        // extended enclosed glyphs, and not "(n)").
        if (n >= 1 && n <= 20) return ((char)(0x2460 + n - 1)).ToString();
        return n.ToString(CultureInfo.InvariantCulture);
    }

    // Parenthesized digit glyphs ⑴⑵⑶ … U+2474..U+2487 cover 1..20 (U+2474 is
    // "PARENTHESIZED DIGIT ONE"). Word renders decimalEnclosedParen with these
    // single glyphs, consistent with decimalEnclosedCircle (①) and
    // decimalEnclosedFullstop (⒈). Beyond 20 the marker is plain decimal — any
    // trailing punctuation seen in a list comes from the level's lvlText.
    private static string ToEnclosedParen(int n)
    {
        if (n >= 1 && n <= 20) return ((char)(0x2473 + n)).ToString();
        return n.ToString(CultureInfo.InvariantCulture);
    }

    private static string ToFullWidthDigits(int n)
    {
        var s = n.ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            sb.Append(c is >= '0' and <= '9' ? (char)('\uFF10' + (c - '0')) : c);
        return sb.ToString();
    }

    // Arabic alphabet (abjad order): 1..28
    private static readonly string[] AbjadLetters =
    {
        "أ", "ب", "ج", "د", "ه", "و", "ز", "ح", "ط", "ي",
        "ك", "ل", "م", "ن", "س", "ع", "ف", "ص", "ق", "ر",
        "ش", "ت", "ث", "خ", "ذ", "ض", "ظ", "غ"
    };
    private static string ToArabicAbjad(int n)
        => n >= 1 && n <= AbjadLetters.Length
            ? AbjadLetters[n - 1]
            : n.ToString(CultureInfo.InvariantCulture);

    // Arabic alphabet (alphabetical / hijā'ī order): 1..28
    private static readonly string[] ArabicAlphaLetters =
    {
        "أ", "ب", "ت", "ث", "ج", "ح", "خ", "د", "ذ", "ر",
        "ز", "س", "ش", "ص", "ض", "ط", "ظ", "ع", "غ", "ف",
        "ق", "ك", "ل", "م", "ن", "ه", "و", "ي"
    };
    private static string ToArabicAlpha(int n)
        => n >= 1 && n <= ArabicAlphaLetters.Length
            ? ArabicAlphaLetters[n - 1]
            : n.ToString(CultureInfo.InvariantCulture);

    // Hebrew numerals (gematria), supports 1..999.
    private static string ToHebrewNumeral(int n)
    {
        if (n < 1 || n > 999) return n.ToString(CultureInfo.InvariantCulture);
        string[] ones = { "", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט" };
        string[] tens = { "", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ" };
        string[] hundreds = { "", "ק", "ר", "ש", "ת", "תק", "תר", "תש", "תת", "תתק" };
        var sb = new StringBuilder();
        sb.Append(hundreds[n / 100]);
        int rem = n % 100;
        if (rem == 15) sb.Append("טו");
        else if (rem == 16) sb.Append("טז");
        else { sb.Append(tens[rem / 10]); sb.Append(ones[rem % 10]); }
        return sb.ToString();
    }

    // Word's Cyrillic enumeration set is 29 letters: the full 33-letter alphabet
    // minus ё, й, ъ and ь. ы IS included (between щ and э) — only the soft/hard
    // signs and ё/й are dropped. (26 → ы, 27 → э, 28 → ю, 29 → я.)
    private static readonly string[] RussianAlphaLower =
    {
        "а", "б", "в", "г", "д", "е", "ж", "з", "и", "к",
        "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф",
        "х", "ц", "ч", "ш", "щ", "ы", "э", "ю", "я"
    };
    // Korean numerals ------------------------------------------------------

    private static readonly char[] KoreanSinoDigits = // 〇일이삼사오육칠팔구
        { '〇', '일', '이', '삼', '사', '오', '육', '칠', '팔', '구' };

    /// <summary>koreanDigital is digit-by-digit sino-Hangul with 영 as the zero
    /// glyph (10 → 일영, 100 → 일영영, 2025 → 이영이오). Distinct from the
    /// ideograph digital family, which uses 〇.</summary>
    private static string ToKoreanDigital(int n)
        => ToDigitByDigit(n, KoreanSinoDigits, '영');

    /// <summary>koreanCounting is positional sino-Hangul grouping 십/백/천/만
    /// (10 → 십, 100 → 백, 2025 → 이천이십오): the leading 일 is dropped before
    /// every unit and internal zeros are dropped. NOT digit-by-digit.</summary>
    private static string ToKoreanCounting(int n)
        => BuildCjkGrouped(n, KoreanSinoDigits, '십', '백', '천', '만', '\0', dropZeros: true, OneStyle.Korean);

    // Native Korean counting words (고유어 수사): 하나 둘 셋 … up to 열아홉,
    // then 스물… Real Word renders koreanLegal with these, NOT the Chinese
    // formal hanzi (壹貳參). Verified via officeshot (fams.docx) — koreanLegal
    // items show 하나/둘/셋. Word's enumeration practically tops out in the low
    // tens; beyond the table we fall back to decimal.
    private static readonly string[] KoreanNativeOnes =
        { "", "하나", "둘", "셋", "넷", "다섯", "여섯", "일곱", "여덟", "아홉" };
    private static readonly string[] KoreanNativeTens =
        { "", "열", "스물", "서른", "마흔", "쉰", "예순", "일흔", "여든", "아흔" };

    /// <summary>Korean legal renders native Korean counting words (하나/둘/셋).</summary>
    private static string ToKoreanLegal(int n)
    {
        if (n < 1 || n > 99) return n.ToString(CultureInfo.InvariantCulture);
        var tens = n / 10;
        var ones = n % 10;
        return KoreanNativeTens[tens] + KoreanNativeOnes[ones];
    }

    // Enclosed digit-with-full-stop glyphs ⒈⒉⒊ … U+2488..U+249B cover 1..20
    // (U+2488 is literally named "DIGIT ONE FULL STOP"; the period is intrinsic
    // to the glyph). Word renders decimalEnclosedFullstop with these single
    // glyphs. Beyond 20 the marker is the bare decimal number — it carries no
    // intrinsic period, so any trailing "." comes from the level's lvlText.
    private static string ToEnclosedFullStop(int n)
    {
        if (n >= 1 && n <= 20) return ((char)(0x2487 + n)).ToString();
        return n.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Japanese legal uses modern formal kanji 壱弐参肆伍陸漆捌玖拾.</summary>
    private static readonly char[] JpFormalDigits =
        { '零', '壱', '弐', '参', '肆', '伍', '陸', '漆', '捌', '玖' };
    /// <summary>japaneseLegal: grouped with units 拾/百/阡/萬, keeps the leading
    /// 壱 before every unit (壱拾/壱百/壱阡/壱萬), drops internal zeros (壱百壱).</summary>
    private static string ToJapaneseLegal(int n)
        => BuildCjkGrouped(n, JpFormalDigits, '拾', '百', '阡', '萬', '\0', dropZeros: true, OneStyle.KeepAll);

    // Thai & Devanagari ----------------------------------------------------

    /// <summary>Positional Thai digits ๐๑๒...: 1 → ๑, 25 → ๒๕.</summary>
    private static string ToThaiDigits(int n)
    {
        var s = n.ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            sb.Append(c is >= '0' and <= '9' ? (char)('\u0E50' + (c - '0')) : c);
        return sb.ToString();
    }

    // Thai consonants (44 letters), Word cycles after 44.
    private static string ToThaiLetters(int n)
    {
        // U+0E01..U+0E2E are the 46 code points but ฃ (U+0E03) and ฅ (U+0E05)
        // are obsolete; Word's enumeration skips them.
        char[] letters =
        {
            '\u0E01','\u0E02','\u0E04','\u0E06','\u0E07','\u0E08','\u0E09','\u0E0A','\u0E0B',
            '\u0E0C','\u0E0D','\u0E0E','\u0E0F','\u0E10','\u0E11','\u0E12','\u0E13','\u0E14',
            '\u0E15','\u0E16','\u0E17','\u0E18','\u0E19','\u0E1A','\u0E1B','\u0E1C','\u0E1D',
            '\u0E1E','\u0E1F','\u0E20','\u0E21','\u0E22','\u0E23','\u0E24','\u0E25','\u0E26',
            '\u0E27','\u0E28','\u0E29','\u0E2A','\u0E2B','\u0E2C','\u0E2D','\u0E2E'
        };
        return letters[(n - 1) % letters.Length].ToString();
    }

    /// <summary>Positional Devanagari digits ०१२...: 1 → १, 25 → २५.</summary>
    private static string ToDevanagariDigits(int n)
    {
        var s = n.ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            sb.Append(c is >= '0' and <= '9' ? (char)('\u0966' + (c - '0')) : c);
        return sb.ToString();
    }

    // Devanagari consonants क, ख, ग, ...
    private static string ToHindiLetters(int n)
    {
        char[] letters =
        {
            'क','ख','ग','घ','ङ','च','छ','ज','झ','ञ',
            'ट','ठ','ड','ढ','ण','त','थ','द','ध','न',
            'प','फ','ब','भ','म','य','र','ल','व','श',
            'ष','स','ह'
        };
        return letters[(n - 1) % letters.Length].ToString();
    }

    // Devanagari independent vowels अ आ इ … औ — the 16 contiguous code points
    // U+0905..U+0914 (includes the vocalic ऌ and the candra/short ऍ ऎ ऑ ऒ that
    // Word's enumeration walks in block order).
    private static string ToHindiVowels(int n)
    {
        const int firstVowel = 0x0905; // अ
        const int vowelCount = 16;     // .. U+0914 (औ)
        return ((char)(firstVowel + (n - 1) % vowelCount)).ToString();
    }

    // Japanese full-width katakana, gojūon (あいうえお) order — 46 glyphs.
    private static readonly char[] KatakanaAiueo =
    {
        'ア','イ','ウ','エ','オ','カ','キ','ク','ケ','コ',
        'サ','シ','ス','セ','ソ','タ','チ','ツ','テ','ト',
        'ナ','ニ','ヌ','ネ','ノ','ハ','ヒ','フ','ヘ','ホ',
        'マ','ミ','ム','メ','モ','ヤ','ユ','ヨ','ラ','リ',
        'ル','レ','ロ','ワ','ヲ','ン'
    };

    // Japanese full-width katakana, iroha order — 48 glyphs (includes archaic
    // ヰ/ヱ).
    private static readonly char[] KatakanaIroha =
    {
        'イ','ロ','ハ','ニ','ホ','ヘ','ト','チ','リ','ヌ',
        'ル','ヲ','ワ','カ','ヨ','タ','レ','ソ','ツ','ネ',
        'ナ','ラ','ム','ウ','ヰ','ノ','オ','ク','ヤ','マ',
        'ケ','フ','コ','エ','テ','ア','サ','キ','ユ','メ',
        'ミ','シ','ヱ','ヒ','モ','セ','ス','ン'
    };

    // Korean leading consonants (초성), Hangul Compatibility Jamo — 14 glyphs.
    private static readonly char[] KoreanChosung =
    {
        'ㄱ','ㄴ','ㄷ','ㄹ','ㅁ','ㅂ','ㅅ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    // Korean syllable enumeration (ganada): leading-consonant + vowel ㅏ —
    // 가 나 다 라 마 바 사 아 자 차 카 타 파 하 (14, Hangul Syllables block).
    private static readonly char[] KoreanGanada =
    {
        '가','나','다','라','마','바','사','아','자','차','카','타','파','하'
    };

    /// <summary>Single-symbol enumeration that recycles to repeated glyphs past
    /// the table end (1→A, table.Length→last, +1→AA, …), matching Word's
    /// symbol-recycling behavior. Mirrors <see cref="ToAlpha"/>'s repeat
    /// model with the same DoS cap.</summary>
    private static string ToRecycledTable(int n, char[] table)
    {
        if (n < 1) n = 1;
        var glyph = table[(n - 1) % table.Length];
        var repeat = Math.Min(((n - 1) / table.Length) + 1, 64);
        return new string(glyph, repeat);
    }

    private static string ToRussianAlpha(int n, bool uppercase)
    {
        // Same recycling rule as ToAlpha: а,б,…,я,аа,бб,… (repeating letter
        // past the 28-letter set), with the identical DoS cap of 64 repeats
        // for adversarial <w:start>.
        if (n < 1) n = 1;
        var s = RussianAlphaLower[(n - 1) % RussianAlphaLower.Length];
        var repeat = Math.Min(((n - 1) / RussianAlphaLower.Length) + 1, 64);
        var glyph = new string(s[0], repeat);
        return uppercase ? glyph.ToUpperInvariant() : glyph;
    }
}
