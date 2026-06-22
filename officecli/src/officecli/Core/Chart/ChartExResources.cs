// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace OfficeCli.Core;

/// <summary>
/// Resource provider for the three chartEx sidecar parts that PowerPoint
/// and Word require alongside an ExtendedChartPart:
///
///   1. EmbeddedPackagePart  (.xlsx) — referenced by &lt;cx:externalData r:id="rId1"/&gt;
///   2. ChartStylePart       (style1.xml,  cs:chartStyle id="419")
///   3. ChartColorStylePart  (colors1.xml, cs:colorStyle method="cycle" id="10")
///
/// Without these sidecars Excel/PowerPoint silently "repairs" the file by
/// dropping the chart (or the entire drawing it lives in). The chartStyle
/// and colorStyle XML are layout-/data-independent and reused verbatim from
/// a canonical funnel reference; the embedded xlsx is built programmatically
/// per-chart so its Sheet1!$A:$Z cells match the cx:f formulas emitted by
/// ChartExBuilder.
///
/// CONSISTENCY(chartex-sidecars): Excel's path uses ChartExStyleBuilder for
/// a per-type style; PPT/Word use the canonical funnel template here. Both
/// produce schema-valid sidecars that satisfy Office's "must have these
/// rels" check.
/// </summary>
internal static class ChartExResources
{
    /// <summary>
    /// Build a minimal embedded .xlsx as a byte stream. Sheet1 contains:
    ///   row 1: ["", seriesName1, seriesName2, ...]
    ///   row 2..N+1: [category, value1, value2, ...]
    /// Categories may be null (histogram) — in that case row 1's A column
    /// is still empty and only numeric data fills column B onward.
    /// </summary>
    internal static byte[] BuildMinimalEmbeddedXlsx(
        string[]? categories,
        List<(string name, double[] values)> seriesData)
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();

            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();

            int rowCount = categories?.Length ?? (seriesData.Count > 0 ? seriesData[0].values.Length : 0);

            // Row 1 — headers: A1 is empty, B1..K1 are series names.
            var headerRow = new Row { RowIndex = 1U };
            headerRow.Append(new Cell
            {
                CellReference = "A1",
                DataType = CellValues.String,
                CellValue = new CellValue(""),
            });
            for (int s = 0; s < seriesData.Count; s++)
            {
                headerRow.Append(new Cell
                {
                    CellReference = $"{ColumnLetter(s + 2)}1",
                    DataType = CellValues.String,
                    CellValue = new CellValue(seriesData[s].name ?? $"Series{s + 1}"),
                });
            }
            sheetData.AppendChild(headerRow);

            // Data rows
            for (int r = 0; r < rowCount; r++)
            {
                var row = new Row { RowIndex = (uint)(r + 2) };
                if (categories != null && r < categories.Length)
                {
                    row.Append(new Cell
                    {
                        CellReference = $"A{r + 2}",
                        DataType = CellValues.String,
                        CellValue = new CellValue(categories[r] ?? string.Empty),
                    });
                }
                for (int s = 0; s < seriesData.Count; s++)
                {
                    var values = seriesData[s].values;
                    if (r >= values.Length) continue;
                    row.Append(new Cell
                    {
                        CellReference = $"{ColumnLetter(s + 2)}{r + 2}",
                        DataType = CellValues.Number,
                        CellValue = new CellValue(values[r].ToString("G", CultureInfo.InvariantCulture)),
                    });
                }
                sheetData.AppendChild(row);
            }

            wsPart.Worksheet = new Worksheet(sheetData);

            var sheets = wbPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = wbPart.GetIdOfPart(wsPart),
                SheetId = 1U,
                Name = "Sheet1",
            });

            wbPart.Workbook.Save();
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Return the canonical chartStyle XML (cs:chartStyle id="419") used by
    /// PowerPoint/Word ExtendedChartPart sidecars. Loaded once from the
    /// embedded resource Resources/chartex-style.xml.
    /// </summary>
    internal static Stream OpenChartStyleXml() => OpenResource("chartex-style.xml");

    /// <summary>
    /// Return the canonical colorStyle XML (cs:colorStyle method="cycle"
    /// id="10"). Same content as Excel's chart palette.
    /// </summary>
    internal static Stream OpenChartColorStyleXml() => OpenResource("chartex-colors.xml");

    private static Stream OpenResource(string fileName)
    {
        var assembly = typeof(ChartExResources).Assembly;
        var name = $"OfficeCli.Resources.{fileName}";
        return assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Embedded resource not found: {name}. Ensure it is declared in officecli.csproj.");
    }

    /// <summary>
    /// Convert a 1-based column index to its Excel column letter (1=A, 2=B,
    /// 27=AA, ...). Used for both embedded-xlsx cell refs and cx:f formulas.
    /// </summary>
    internal static string ColumnLetter(int index1Based)
    {
        if (index1Based <= 0) throw new ArgumentOutOfRangeException(nameof(index1Based));
        var sb = new System.Text.StringBuilder();
        int n = index1Based;
        while (n > 0)
        {
            int rem = (n - 1) % 26;
            sb.Insert(0, (char)('A' + rem));
            n = (n - 1) / 26;
        }
        return sb.ToString();
    }
}
