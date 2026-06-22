// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

namespace OfficeCli.Core;

/// <summary>
/// Shared EMU (English Metric Unit) parsing and formatting.
/// 1 inch = 914400 EMU, 1 cm = 360000 EMU, 1 pt = 12700 EMU, 1 px = 9525 EMU.
/// Accepts: raw EMU integer, or suffixed with cm/in/pt/px/emu.
/// </summary>
internal static class EmuConverter
{
    /// <summary>EMU per point as an integer. 1 pt = 12700 EMU (exact).
    /// Use for integer-arithmetic conversions where the operand is already int/long
    /// (preserves the surrounding cast/truncation semantics).</summary>
    public const int EmuPerPoint = 12700;

    /// <summary>EMU per point as a double. 1 pt = 12700 EMU (exact).
    /// Use for floating-point conversions (<c>pt * EmuPerPointF</c>, <c>emu / EmuPerPointF</c>).</summary>
    public const double EmuPerPointF = 12700.0;

    /// <summary>Convert points to EMU, rounding to the nearest EMU. 1 pt = 12700 EMU.</summary>
    public static long PointsToEmu(double points) => (long)Math.Round(points * EmuPerPointF);

    /// <summary>Convert EMU to points (no rounding). 1 pt = 12700 EMU.</summary>
    public static double EmuToPoints(long emu) => emu / EmuPerPointF;

    /// <summary>EMU per pixel at 96 DPI. 1 px = 9525 EMU (exact). int / double pair,
    /// same usage rule as <see cref="EmuPerPoint"/>. (Renderers that scale for retina
    /// use their own local ratio — do not assume px is always 9525.)</summary>
    public const int EmuPerPx = 9525;
    public const double EmuPerPxF = 9525.0;

    /// <summary>EMU per centimetre. 1 cm = 360000 EMU (exact). int / double pair.</summary>
    public const int EmuPerCm = 360000;
    public const double EmuPerCmF = 360000.0;

    /// <summary>EMU per inch. 1 in = 914400 EMU (exact). int / double pair.</summary>
    public const int EmuPerInch = 914400;
    public const double EmuPerInchF = 914400.0;

    /// <summary>
    /// Parse a dimension/position string into EMU (long).
    /// Supported formats: "914400" (raw EMU), "914400emu", "2.54cm", "1in", "72pt", "96px".
    /// Negative values are allowed (for positions like x, y).
    /// Throws ArgumentException on invalid input.
    /// </summary>
    public static long ParseEmu(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Invalid length value: input is null or empty.");

        value = value.Trim();

        long result;

        // mm = cm/10. CSS-natural sibling of cm; AI assistants reach for mm
        // before pt/in. Test in longer-suffix order so "mm" is matched before
        // any bare-number fallback. ("Q" = mm/4 in CSS, also accepted.)
        if (value.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            result = ParseWithUnit(value, 2, 36000.0, "mm");
        }
        else if (value.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
        {
            result = ParseWithUnit(value, 2, EmuPerCmF, "cm");
        }
        else if (value.EndsWith("in", StringComparison.OrdinalIgnoreCase))
        {
            result = ParseWithUnit(value, 2, EmuPerInchF, "in");
        }
        else if (value.EndsWith("pc", StringComparison.OrdinalIgnoreCase))
        {
            // pica = 12pt (CSS / typographic standard).
            result = ParseWithUnit(value, 2, 12.0 * EmuPerPointF, "pc");
        }
        else if (value.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            result = ParseWithUnit(value, 2, EmuPerPointF, "pt");
        }
        else if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            result = ParseWithUnit(value, 2, EmuPerPxF, "px");
        }
        else if (value.EndsWith("Q", StringComparison.OrdinalIgnoreCase))
        {
            // CSS quarter-millimeter (Q = mm/4). Rare but harmless to support.
            result = ParseWithUnit(value, 1, 9000.0, "Q");
        }
        else if (value.EndsWith("emu", StringComparison.OrdinalIgnoreCase))
        {
            // Explicit emu suffix — symmetric with FormatEmu's tiny-value fallback.
            var numberPart = value[..^3];
            if (string.IsNullOrWhiteSpace(numberPart))
                throw new ArgumentException($"Invalid length value '{value}': missing numeric value before 'emu' unit.");
            if (!long.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                throw new ArgumentException($"Invalid length value '{value}': '{numberPart}' before 'emu' unit is not an integer.");
        }
        else if (HasKnownUnitSuffix(value, out var unit))
        {
            // 'em' / 'ex' / 'rem' depend on font context that ParseEmu does not have.
            throw new ArgumentException(
                $"Invalid length value '{value}': unit '{unit}' is not supported (requires font context). " +
                $"Supported units: cm, mm, in, pt, pc, px, Q, emu (or raw EMU integer).");
        }
        else
        {
            // Raw EMU value: integer form preferred. Bare scientific notation
            // ("1e6", "1.5e2") is also accepted for parity with ParseFontSize
            // — but a plain decimal like "0.0001" is NOT (fractional EMU is
            // meaningless; users wanting sub-EMU sizes have unit suffixes).
            // Reject NaN/Infinity early so downstream emit never produces a
            // "NaNpt" / "Infinitypt" string.
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                // already parsed
            }
            else if ((value.Contains('e') || value.Contains('E'))
                     && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                if (double.IsNaN(d) || double.IsInfinity(d))
                    throw new ArgumentException(
                        $"Invalid length value '{value}': must be a finite number.");
                if (d > long.MaxValue || d < long.MinValue)
                    throw new ArgumentException(
                        $"Invalid length value '{value}': exceeds the maximum EMU range.");
                result = (long)Math.Round(d);
            }
            else
            {
                throw new ArgumentException(
                    $"Invalid length value '{value}': expected a number with optional unit suffix (cm, mm, in, pt, pc, px, Q, emu).");
            }
        }

        return result;
    }

    /// <summary>
    /// Parse EMU and safely cast to int, throwing on overflow.
    /// </summary>
    public static int ParseEmuAsInt(string value)
    {
        long emu = ParseEmu(value);
        if (emu < 0)
            throw new ArgumentException($"Negative dimension value '{value}' is not allowed. This property requires a non-negative value.");
        if (emu > int.MaxValue)
            throw new OverflowException($"EMU value {emu} (from '{value}') exceeds the maximum allowed value of {int.MaxValue}.");
        return (int)emu;
    }

    /// <summary>
    /// Parse line width value into EMU (int). Bare numbers are treated as points (pt).
    /// Suffixed values (cm/in/pt/px) are parsed normally via ParseEmu.
    /// </summary>
    public static int ParseLineWidth(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Invalid line width value: input is null or empty.");

        var trimmed = value.Trim();
        // If bare integer/decimal with no unit suffix, treat as points.
        // Reject NaN/Infinity here so we never construct a "NaNpt" / "Infinitypt"
        // string that would slip past ParseEmu's suffix branch with a misleading
        // error tail.
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var bare)
            && !HasKnownUnitSuffix(trimmed, out _))
        {
            if (double.IsNaN(bare) || double.IsInfinity(bare))
                throw new ArgumentException($"Invalid line width value '{value}': must be a finite number.");
            trimmed += "pt";
        }
        return ParseEmuAsInt(trimmed);
    }

    /// <summary>
    /// Format an EMU value as a human-readable string (e.g., "2.54cm").
    /// </summary>
    public static string FormatEmu(long emu)
    {
        if (emu == 0) return "0cm";
        // Emit in the first unit whose 2-decimal form round-trips back to the
        // exact source EMU. cm is preferred (most readable for document
        // geometry); pt and inch follow because many PowerPoint-authored values
        // are exact in pt/inch but NOT in cm — e.g. a 13.333in (960pt) slide
        // width = 12192000 EMU is 33.8667cm, which "0.##" cm rounds to 33.87cm
        // and re-parses as 12193200 EMU (off by 1200). Only when no cm/pt/inch
        // form survives the round trip (a value not cleanly expressible in any
        // human unit) do we fall back to raw `<n>emu`. This keeps Get readback
        // BOTH exact (no dump→replay position drift) AND unit-qualified
        // (cm/in/pt) — instead of dumping ugly raw EMU whenever cm alone misses.
        return TryFormatUnit(emu, EmuPerCmF, "cm")
            ?? TryFormatUnit(emu, EmuPerPointF, "pt")
            ?? TryFormatUnit(emu, EmuPerInchF, "in")
            ?? emu.ToString(CultureInfo.InvariantCulture) + "emu";
    }

    /// <summary>
    /// Like <see cref="FormatEmu"/>, but tries pt FIRST (then cm, in). Used for
    /// slide-size paired readback (slideWidth / slideHeight) where the default
    /// widescreen size (12192000 x 6858000 EMU) is exact in pt for the width
    /// (960pt) but exact in cm for the height (19.05cm) — so the default
    /// cm-first ordering mixes units across a semantically paired property.
    /// Preferring pt keeps both members on the same unit for any pt-exact
    /// size, with cm/in still reachable for sizes that are clean cm but not
    /// clean pt (e.g. A4).
    /// </summary>
    public static string FormatEmuPtFirst(long emu)
    {
        if (emu == 0) return "0pt";
        return TryFormatUnit(emu, EmuPerPointF, "pt")
            ?? TryFormatUnit(emu, EmuPerCmF, "cm")
            ?? TryFormatUnit(emu, EmuPerInchF, "in")
            ?? emu.ToString(CultureInfo.InvariantCulture) + "emu";
    }

    /// <summary>
    /// Format two paired EMU dimensions (e.g. slideWidth + slideHeight) using
    /// the SAME unit when one exists that exact-round-trips both. Iterates
    /// pt → cm → in; falls back to per-value <see cref="FormatEmu"/> if no
    /// shared unit fits. This prevents semantically paired readback (slide
    /// size, page size, etc.) from mixing units when one value is pt-exact
    /// and the other is cm-exact.
    /// </summary>
    public static (string, string) FormatEmuPaired(long emuA, long emuB)
    {
        foreach (var (perUnit, suffix) in new[] {
            (EmuPerPointF, "pt"), (EmuPerCmF, "cm"), (EmuPerInchF, "in") })
        {
            var a = TryFormatUnit(emuA, perUnit, suffix);
            var b = TryFormatUnit(emuB, perUnit, suffix);
            if (a != null && b != null) return (a, b);
        }
        return (FormatEmu(emuA), FormatEmu(emuB));
    }

    /// <summary>
    /// Format <paramref name="emu"/> in human-readable cm/pt/in even when the
    /// 2-decimal form does NOT round-trip exactly. Used for derived geometry
    /// like table colWidths (where auto-distribution divides the table width
    /// across N columns and produces values that don't survive a 2-decimal
    /// cm round-trip — e.g. 22cm / 3 = 7.33cm = 2640000 EMU, off by 1200 EMU
    /// = 0.13mm imperceptible). Falling back to raw `<n>emu` integers here
    /// gives unreadable output for the common UI case; the small round-trip
    /// loss is acceptable for these derived widths.
    /// </summary>
    public static string FormatEmuLossy(long emu)
    {
        if (emu == 0) return "0cm";
        // Prefer exact (same as FormatEmu) when available; otherwise fall
        // back to nearest cm at 2-decimal precision.
        return TryFormatUnit(emu, EmuPerCmF, "cm")
            ?? TryFormatUnit(emu, EmuPerPointF, "pt")
            ?? TryFormatUnit(emu, EmuPerInchF, "in")
            ?? (emu / EmuPerCmF).ToString("0.##", CultureInfo.InvariantCulture) + "cm";
    }

    /// <summary>
    /// Format <paramref name="emu"/> in <paramref name="suffix"/> units with a
    /// 2-decimal "0.##" form, returning the string only when it re-parses back to
    /// the exact source EMU (non-lossy round trip). Returns null when the value
    /// rounds to 0 in this unit or the round trip loses precision, so the caller
    /// can try the next unit.
    /// </summary>
    private static string? TryFormatUnit(long emu, double emuPerUnit, string suffix)
    {
        var str = (emu / emuPerUnit).ToString("0.##", CultureInfo.InvariantCulture);
        if (str == "0" || str == "-0") return null;
        var reparsed = (long)Math.Round(double.Parse(str, CultureInfo.InvariantCulture) * emuPerUnit);
        return reparsed == emu ? str + suffix : null;
    }

    /// <summary>
    /// Format an EMU value as points (e.g., "2pt"). Used for line widths and other
    /// thin values where points are more natural than centimeters.
    /// </summary>
    public static string FormatLineWidth(long emu)
    {
        // CONSISTENCY(emu-roundtrip): tiny sub-0.01-pt widths (anything below
        // ~127 EMU) collapse to "0pt" under the "0.##" pt format, then
        // re-parse as 0 EMU on replay — silently turning legal hairline
        // <a:ln w="1..126"> strokes into <a:ln w="0"/>. Fall back to a raw
        // "<n>emu" form (round-trips through ParseEmu/ParseLineWidth) when
        // the pt form would round to "0". Round-trip lossiness above the
        // floor (e.g. 180000 EMU -> "14.17pt" -> 179959 EMU) is the
        // pre-existing 0.01-pt readback contract callers expect — only
        // zero-collapse is plugged here.
        if (emu == 0) return "0pt";
        var pt = emu / EmuPerPointF;
        var ptStr = pt.ToString("0.##", CultureInfo.InvariantCulture);
        if (ptStr == "0" || ptStr == "-0")
            return emu.ToString(CultureInfo.InvariantCulture) + "emu";
        return $"{ptStr}pt";
    }

    /// <summary>
    /// Try to parse a dimension string into EMU. Returns false if parsing fails.
    /// </summary>
    public static bool TryParseEmu(string value, out long emu)
    {
        try
        {
            emu = ParseEmu(value);
            return true;
        }
        catch
        {
            emu = 0;
            return false;
        }
    }

    private static long ParseWithUnit(string value, int suffixLen, double factor, string unit)
    {
        var numberPart = value[..^suffixLen];
        if (string.IsNullOrWhiteSpace(numberPart))
            throw new ArgumentException($"Missing numeric value before '{unit}' unit in '{value}'.");

        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || double.IsNaN(number) || double.IsInfinity(number))
            throw new ArgumentException($"Invalid numeric value '{numberPart}' before '{unit}' unit in '{value}'.");

        return (long)Math.Round(number * factor);
    }

    private static bool HasKnownUnitSuffix(string value, out string unit)
    {
        // Font-relative or viewport-relative units that ParseEmu cannot resolve
        // (no font / viewport context at this layer). Listed so the bare-number
        // fallback rejects them with a "not supported" message instead of
        // silently re-interpreting "5em" as raw EMU.
        // Order matters: longer suffixes first so "rem" doesn't shadow "em".
        string[] unsupported = { "rem", "em", "ex", "vw", "vh" };
        foreach (var u in unsupported)
        {
            if (value.EndsWith(u, StringComparison.OrdinalIgnoreCase))
            {
                unit = u;
                return true;
            }
        }
        unit = "";
        return false;
    }
}
