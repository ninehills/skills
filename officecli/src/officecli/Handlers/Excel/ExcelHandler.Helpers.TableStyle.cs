// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeCli.Core;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Drawing = DocumentFormat.OpenXml.Drawing;
using X14 = DocumentFormat.OpenXml.Office2010.Excel;
using XDR = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace OfficeCli.Handlers;

public partial class ExcelHandler
{

    // Make a string safe to use as an Excel table name, displayName, or
    // tableColumn name. Excel refuses to open files where these identifiers
    // look like a cell reference ("tbl1" → column TBL row 1) or are purely
    // numeric ("30").
    //
    // When `userProvided` is true (user explicitly passed --prop name=T1),
    // honor the name verbatim — callers who type `name=T1` expect a table
    // named `T1`, not `T1_`. Excel itself accepts these table identifiers
    // (the cell-reference ambiguity rule applies to defined names, not to
    // tables), so silently rewriting loses fidelity with no gain.
    //
    // When `userProvided` is false (auto-derived default such as
    // `Table{id}`, or tableColumn name read from a header cell) we suffix
    // "_" on cell-reference-shaped names to keep defaults safe.
    internal static string SanitizeTableIdentifier(string? name, bool userProvided = false)
    {
        if (string.IsNullOrEmpty(name)) return "_";
        if (userProvided)
        {
            // Mac Excel rejects the "Tbl{N}" pattern (Excel's internal table
            // identifier prefix), silently renaming with a "_" suffix and
            // triggering "found a problem" repair dialog on open. Block it
            // up front so users get a clear error instead of the repair flow.
            // Windows Excel auto-recovers silently which historically masked
            // this on officeshot Windows-side validation. "Tbl" alone or
            // "Tbl"+letters (e.g. "TblData") are NOT rejected — only the
            // exact Tbl-followed-by-digits pattern collides.
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^[Tt][Bb][Ll]\d+$"))
                throw new ArgumentException(
                    $"Table name '{name}' matches Excel's internal Tbl{{N}} naming pattern and is rejected by Mac Excel. Use 'Table{{N}}' (default) or a descriptive name like 'SalesData'.");
            return name;
        }
        var looksLikeRef = LooksLikeCellReference(name)
            || System.Text.RegularExpressions.Regex.IsMatch(name, @"^[0-9]+$");
        return looksLikeRef ? name + "_" : name;
    }

    // T6 — built-in Excel table style names. Unknown names are rejected at
    // Add time rather than silently passed through to Excel.
    private static readonly HashSet<string> _builtInTableStyles = BuildBuiltInTableStyles();
    private static HashSet<string> BuildBuiltInTableStyles()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tier in new[] { "Light", "Medium", "Dark" })
            for (int i = 1; i <= 28; i++)
                set.Add($"TableStyle{tier}{i}");
        // Pivot styles — users may apply a pivot style to a plain table.
        foreach (var tier in new[] { "Light", "Medium", "Dark" })
            for (int i = 1; i <= 28; i++)
                set.Add($"PivotStyle{tier}{i}");
        set.Add("TableStyleNone");
        return set;
    }

    // BUG-R9-B2: schema (_shared/table.json) documents short-name styles
    // (medium1..medium28, light1..light28, dark1..dark28, none) as valid
    // values, but the validator only accepted the full OOXML "TableStyleX"
    // form. Mirror pptx ResolveTableStyleId behavior: accept short aliases
    // and map to the canonical full name. "none" maps to "TableStyleNone".
    // CONSISTENCY(table-style-naming): xlsx + pptx now both accept
    // medium1/light1/dark1/none short names.
    internal static string? NormalizeTableStyleName(string? styleName)
    {
        if (string.IsNullOrEmpty(styleName)) return styleName;
        var trimmed = styleName.Trim();
        if (string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase))
            return "TableStyleNone";
        // Match short aliases like "medium2", "light1", "dark3" (1..28).
        var m = System.Text.RegularExpressions.Regex.Match(
            trimmed, @"^(light|medium|dark)(\d{1,2})$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[2].Value, out var n) && n >= 1 && n <= 28)
        {
            var tier = char.ToUpperInvariant(m.Groups[1].Value[0]) +
                       m.Groups[1].Value.Substring(1).ToLowerInvariant();
            return $"TableStyle{tier}{n}";
        }
        return styleName;
    }

    internal void ValidateTableStyleName(string? styleName)
    {
        if (string.IsNullOrEmpty(styleName)) return;
        if (_builtInTableStyles.Contains(styleName)) return;
        // Workbook-level customStyles live under <x:tableStyles> on the stylesheet.
        var styles = _doc.WorkbookPart?.WorkbookStylesPart?.Stylesheet;
        var tableStyles = styles?.GetFirstChild<TableStyles>();
        if (tableStyles != null)
        {
            foreach (var ts in tableStyles.Elements<TableStyle>())
                if (ts.Name?.Value == styleName) return;
        }
        throw new ArgumentException(
            $"Unknown table style: '{styleName}'. Use a built-in name like " +
            $"'TableStyleMedium2', or register a custom style on the workbook first.");
    }
}
