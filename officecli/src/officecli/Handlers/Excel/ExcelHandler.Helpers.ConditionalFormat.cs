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

    /// <summary>
    /// Insert a ConditionalFormatting element after all existing CF elements (preserving add order).
    /// Falls back to after sheetData if no CF exists yet.
    /// </summary>
    private static void InsertConditionalFormatting(Worksheet ws, ConditionalFormatting cfElement)
    {
        var lastCf = ws.Elements<ConditionalFormatting>().LastOrDefault();
        if (lastCf != null)
            lastCf.InsertAfterSelf(cfElement);
        else
        {
            var sheetData = ws.GetFirstChild<SheetData>();
            if (sheetData != null)
                sheetData.InsertAfterSelf(cfElement);
            else
                ws.AppendChild(cfElement);
        }
    }

    /// <summary>
    /// Compute the next available CF priority for a worksheet (max existing + 1).
    /// </summary>
    private static int NextCfPriority(Worksheet ws)
    {
        int max = 0;
        foreach (var cf in ws.Elements<ConditionalFormatting>())
            foreach (var rule in cf.Elements<ConditionalFormattingRule>())
                if (rule.Priority?.HasValue == true && rule.Priority.Value > max)
                    max = rule.Priority.Value;
        return max + 1;
    }

    /// <summary>
    /// CF2: stamp the stopIfTrue attribute onto a CF rule when the user
    /// passed `stopIfTrue=true`. Centralized so every `add cf` branch
    /// (databar / colorscale / iconset / formulacf / cellIs / topN / ...)
    /// honors the same flag.
    /// </summary>
    internal static void ApplyStopIfTrue(ConditionalFormattingRule rule, Dictionary<string, string> properties)
    {
        if (properties.TryGetValue("stopIfTrue", out var v) && ParseHelpers.IsTruthy(v))
            rule.StopIfTrue = true;
    }

    /// <summary>
    /// Ensure the worksheet root declares `xmlns:x14` + `mc:Ignorable="x14"`.
    /// Without both, Excel silently drops the x14 extension block where
    /// sparklines, dataBar 2010+ extensions, and other Office2010 features
    /// live. CONSISTENCY(x14-ignorable): same pattern the sparkline branch
    /// uses inline.
    /// </summary>
    internal static void EnsureWorksheetX14Ignorable(Worksheet ws)
    {
        const string mcNs = "http://schemas.openxmlformats.org/markup-compatibility/2006";
        const string x14Ns = "http://schemas.microsoft.com/office/spreadsheetml/2009/9/main";
        if (ws.LookupNamespace("mc") == null)
            ws.AddNamespaceDeclaration("mc", mcNs);
        if (ws.LookupNamespace("x14") == null)
            ws.AddNamespaceDeclaration("x14", x14Ns);
        var ignorable = ws.MCAttributes?.Ignorable?.Value ?? "";
        if (!ignorable.Split(' ').Contains("x14"))
        {
            ws.MCAttributes ??= new MarkupCompatibilityAttributes();
            ws.MCAttributes.Ignorable = string.IsNullOrEmpty(ignorable) ? "x14" : $"{ignorable} x14";
        }
    }

    /// <summary>
    /// Append an x14:conditionalFormatting block to the worksheet's extLst under
    /// ext URI `{78C0D931-6437-407d-A8EE-F0AAD7539E65}`. Creates the extension
    /// on first call, appends to the existing x14:conditionalFormattings
    /// container on subsequent calls. Also ensures mc:Ignorable="x14" is set.
    /// </summary>
    internal static void EnsureWorksheetX14ConditionalFormatting(Worksheet ws, X14.ConditionalFormatting x14Cf)
    {
        const string cfExtUri = "{78C0D931-6437-407d-A8EE-F0AAD7539E65}";
        const string x14Ns = "http://schemas.microsoft.com/office/spreadsheetml/2009/9/main";

        EnsureWorksheetX14Ignorable(ws);

        var extList = ws.GetFirstChild<WorksheetExtensionList>() ?? ws.AppendChild(new WorksheetExtensionList());
        var ext = extList.Elements<WorksheetExtension>().FirstOrDefault(e => e.Uri == cfExtUri);
        X14.ConditionalFormattings cfContainer;
        if (ext != null)
        {
            cfContainer = ext.GetFirstChild<X14.ConditionalFormattings>()
                ?? ext.AppendChild(new X14.ConditionalFormattings());
        }
        else
        {
            ext = new WorksheetExtension { Uri = cfExtUri };
            ext.AddNamespaceDeclaration("x14", x14Ns);
            cfContainer = new X14.ConditionalFormattings();
            ext.Append(cfContainer);
            extList.Append(ext);
        }
        cfContainer.Append(x14Cf);
    }

    /// <summary>
    /// Get a sparkline group by 1-based index from a worksheet's extension list.
    /// Returns null if not found.
    /// </summary>
    internal X14.SparklineGroup? GetSparklineGroup(WorksheetPart worksheet, int index)
    {
        var ws = GetSheet(worksheet);
        var extList = ws.GetFirstChild<WorksheetExtensionList>();
        if (extList == null) return null;

        var spkExt = extList.Elements<WorksheetExtension>()
            .FirstOrDefault(e => e.Uri == "{05C60535-1F16-4fd2-B633-E4A46CF9E463}");
        if (spkExt == null) return null;

        var spkGroups = spkExt.GetFirstChild<X14.SparklineGroups>();
        if (spkGroups == null) return null;

        var groups = spkGroups.Elements<X14.SparklineGroup>().ToList();
        if (index < 1 || index > groups.Count) return null;
        return groups[index - 1];
    }

    /// <summary>
    /// Build a DocumentNode for a sparkline group.
    /// </summary>
    // Strip the parent-sheet prefix from a user-supplied sparkline location.
    // <xm:sqref> is ST_Sqref — a bare cell address, no sheet prefix allowed
    // (sheet is implied by the parent worksheet). Accept lenient input so the
    // canonical "Sheet1!G2" form many users naturally write still works; reject
    // cross-sheet references because OOXML doesn't allow them.
    internal static string NormalizeSparklineSqref(string spkCell, string parentSheetName)
    {
        var excl = spkCell.IndexOf('!');
        if (excl < 0) return spkCell;
        var sheetPart = spkCell[..excl].Trim('\'');
        if (!string.Equals(sheetPart, parentSheetName, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Sparkline location '{spkCell}' targets sheet '{sheetPart}' but sparkline lives on '{parentSheetName}'. " +
                "OOXML requires sparkline location to be on the same worksheet (sheet prefix is implicit).");
        return spkCell[(excl + 1)..];
    }

    private static IconSetValues ParseIconSetValues(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "3arrows" => IconSetValues.ThreeArrows,
            "3arrowsgray" => IconSetValues.ThreeArrowsGray,
            "3flags" => IconSetValues.ThreeFlags,
            "3trafficlights1" => IconSetValues.ThreeTrafficLights1,
            "3trafficlights2" => IconSetValues.ThreeTrafficLights2,
            "3signs" => IconSetValues.ThreeSigns,
            "3symbols" => IconSetValues.ThreeSymbols,
            "3symbols2" => IconSetValues.ThreeSymbols2,
            "4arrows" => IconSetValues.FourArrows,
            "4arrowsgray" => IconSetValues.FourArrowsGray,
            "4rating" => IconSetValues.FourRating,
            "4redtoblack" => IconSetValues.FourRedToBlack,
            "4trafficlights" => IconSetValues.FourTrafficLights,
            "5arrows" => IconSetValues.FiveArrows,
            "5arrowsgray" => IconSetValues.FiveArrowsGray,
            "5rating" => IconSetValues.FiveRating,
            "5quarters" => IconSetValues.FiveQuarters,
            _ => throw new ArgumentException($"Unknown icon set name: '{name}'. Valid names: 3Arrows, 3ArrowsGray, 3Flags, 3TrafficLights1, 3TrafficLights2, 3Signs, 3Symbols, 3Symbols2, 4Arrows, 4ArrowsGray, 4Rating, 4RedToBlack, 4TrafficLights, 5Arrows, 5ArrowsGray, 5Rating, 5Quarters")
        };
    }

    private static int GetIconCount(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.StartsWith("5")) return 5;
        if (lower.StartsWith("4")) return 4;
        return 3;
    }

    /// <summary>
    /// Build a <x:font> child for a dxf (differentialFormat) from font.* sub-props.
    /// Supports bold, italic, underline (single/double), strike, size, name, color.
    /// Returns null if no font sub-props were supplied.
    /// </summary>
    internal static Font? BuildFormulaCfFont(Dictionary<string, string> properties)
    {
        bool any = false;
        var font = new Font();
        if (properties.TryGetValue("font.bold", out var fBold) && ParseHelpers.IsTruthy(fBold))
        { font.Append(new Bold()); any = true; }
        if (properties.TryGetValue("font.italic", out var fItalic) && ParseHelpers.IsTruthy(fItalic))
        { font.Append(new Italic()); any = true; }
        if (properties.TryGetValue("font.strike", out var fStrike) && ParseHelpers.IsTruthy(fStrike))
        { font.Append(new Strike()); any = true; }
        if (properties.TryGetValue("font.underline", out var fUnder))
        {
            var ul = new Underline();
            var lv = fUnder.Trim().ToLowerInvariant();
            ul.Val = lv switch
            {
                "double" or "dbl" => UnderlineValues.Double,
                "singleaccounting" or "singleacct" => UnderlineValues.SingleAccounting,
                "doubleaccounting" or "doubleacct" => UnderlineValues.DoubleAccounting,
                "none" or "false" => UnderlineValues.None,
                _ => UnderlineValues.Single
            };
            font.Append(ul);
            any = true;
        }
        if (properties.TryGetValue("font.size", out var fSize))
        {
            // Accept "12", "12pt", "10.5pt" — strip trailing "pt" if present.
            var cleaned = fSize.Trim().TrimEnd('p', 't', 'P', 'T', ' ');
            if (double.TryParse(cleaned, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var sz))
            {
                font.Append(new FontSize { Val = sz });
                any = true;
            }
        }
        if (properties.TryGetValue("font.name", out var fName) && !string.IsNullOrWhiteSpace(fName))
        {
            font.Append(new FontName { Val = fName });
            any = true;
        }
        if (properties.TryGetValue("font.color", out var fColor))
        {
            var norm = ParseHelpers.NormalizeArgbColor(fColor);
            font.Append(new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = norm });
            any = true;
        }
        return any ? font : null;
    }
}
