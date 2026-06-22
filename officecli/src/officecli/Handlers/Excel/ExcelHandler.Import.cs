// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace OfficeCli.Handlers;

public partial class ExcelHandler
{
    /// <summary>
    /// Import CSV/TSV data into a worksheet starting at the given cell.
    /// </summary>
    /// <param name="parentPath">Sheet path, e.g. "/Sheet1"</param>
    /// <param name="csvContent">Raw CSV/TSV string content</param>
    /// <param name="delimiter">Field delimiter: ',' for CSV, '\t' for TSV</param>
    /// <param name="hasHeader">If true, set AutoFilter and freeze pane on first row</param>
    /// <param name="startCell">Starting cell reference, e.g. "A1"</param>
    /// <returns>Summary of rows/cols imported</returns>
    public string Import(string parentPath, string csvContent, char delimiter, bool hasHeader, string startCell)
    {
        parentPath = NormalizeExcelPath(parentPath);
        parentPath = ResolveSheetIndexInPath(parentPath);
        var sheetName = parentPath.TrimStart('/').Split('/', 2)[0];
        var worksheet = FindWorksheet(sheetName)
            ?? throw new ArgumentException($"Sheet not found: {sheetName}");

        var ws = GetSheet(worksheet);
        var sheetData = ws.GetFirstChild<SheetData>()
            ?? ws.AppendChild(new SheetData());

        // Parse start cell
        var (startCol, startRow) = ParseCellReference(startCell.ToUpperInvariant());
        var startColIdx = ColumnNameToIndex(startCol);

        // Parse CSV
        var rows = ParseCsv(csvContent, delimiter);
        if (rows.Count == 0)
            return "No data to import";

        int maxCols = 0;
        for (int r = 0; r < rows.Count; r++)
            if (rows[r].Count > maxCols) maxCols = rows[r].Count;

        // DOS-hardening: reject imports that exceed Excel's sheet dimensions
        // BEFORE writing anything. Without this an over-sized CSV (e.g. >XFD
        // columns or >1048576 rows) spun indefinitely instead of erroring.
        const int ExcelMaxRow = 1048576;
        const int ExcelMaxCol = 16384; // XFD (ColumnNameToIndex is 1-based)
        long endRowReq = (long)startRow + rows.Count - 1;
        if (endRowReq > ExcelMaxRow)
            throw new ArgumentException(
                $"Import exceeds Excel's row limit: data would reach row {endRowReq} " +
                $"(maximum {ExcelMaxRow}). Reduce the CSV or change the start cell.");
        long endColIdx = (long)startColIdx + maxCols - 1;
        if (endColIdx > ExcelMaxCol)
            throw new ArgumentException(
                $"Import exceeds Excel's column limit: data would reach column {endColIdx} " +
                $"(maximum {ExcelMaxCol} / XFD). Reduce the CSV width or change the start cell.");

        // BUG-R11-import-dup-row BUG-11: import previously always appended a
        // brand-new <row r="N">, producing duplicate row entries when the
        // target rows already existed (Excel auto-repaired by keeping the
        // first one, silently losing imported data). Upsert by RowIndex —
        // reuse an existing row, otherwise insert a new one in sorted position.
        //
        // PERF(dos-hardening): the previous implementation re-scanned the whole
        // SheetData (LINQ FirstOrDefault) for every imported row AND every cell,
        // making a bulk import O(rows*cells * existing) — a 100k-row CSV took
        // 9+ minutes. Pre-index existing rows once and walk them with an
        // ascending cursor for sorted insertion; build a per-row cell index
        // only when reusing a pre-existing row. Bulk-append into a fresh sheet
        // is now linear.
        var existingRows = sheetData.Elements<Row>()
            .Where(rr => rr.RowIndex?.Value != null)
            .OrderBy(rr => rr.RowIndex!.Value)
            .ToList();
        var rowByIndex = new Dictionary<uint, Row>();
        foreach (var er in existingRows)
            rowByIndex[er.RowIndex!.Value] = er;
        int exCursor = 0; // points at the first existing row with index > last processed

        for (int r = 0; r < rows.Count; r++)
        {
            var fields = rows[r];
            var rowIdx = (uint)(startRow + r);

            Dictionary<string, Cell>? cellByRef = null;
            if (rowByIndex.TryGetValue(rowIdx, out var row))
            {
                // Reused row may already hold cells — index them once for upsert.
                cellByRef = new Dictionary<string, Cell>(StringComparer.OrdinalIgnoreCase);
                foreach (var existingCell in row.Elements<Cell>())
                    if (existingCell.CellReference?.Value is { } cr)
                        cellByRef[cr] = existingCell;
            }
            else
            {
                row = new Row { RowIndex = rowIdx };
                // Advance the cursor past existing rows that sort before rowIdx,
                // then insert before the first existing row that sorts after it
                // (or append when none remain). O(existing) total across all rows.
                while (exCursor < existingRows.Count
                       && existingRows[exCursor].RowIndex!.Value < rowIdx)
                    exCursor++;
                if (exCursor < existingRows.Count)
                    sheetData.InsertBefore(row, existingRows[exCursor]);
                else
                    sheetData.Append(row);
            }

            for (int c = 0; c < fields.Count; c++)
            {
                var colIdx = startColIdx + c;
                var cellRef = $"{IndexToColumnName(colIdx)}{rowIdx}".ToUpperInvariant();
                Cell? cell = null;
                cellByRef?.TryGetValue(cellRef, out cell);
                if (cell == null)
                {
                    cell = new Cell { CellReference = cellRef };
                    row.Append(cell);
                }
                else
                {
                    cell.CellFormula = null;
                    cell.CellValue = null;
                    cell.DataType = null;
                }
                SetCellValueWithTypeDetection(cell, fields[c]);
            }
        }

        InvalidateRowIndex(sheetData);

        // --header: set AutoFilter on data range and freeze pane below first row
        if (hasHeader && rows.Count > 0)
        {
            var endCol = IndexToColumnName(startColIdx + maxCols - 1);
            var endRow = startRow + rows.Count - 1;
            var filterRange = $"{startCol}{startRow}:{endCol}{endRow}";

            // Set AutoFilter
            var autoFilter = ws.GetFirstChild<AutoFilter>();
            if (autoFilter == null)
            {
                autoFilter = new AutoFilter();
                var mergeCells = ws.GetFirstChild<MergeCells>();
                var sd = ws.GetFirstChild<SheetData>();
                if (mergeCells != null)
                    mergeCells.InsertAfterSelf(autoFilter);
                else if (sd != null)
                    sd.InsertAfterSelf(autoFilter);
                else
                    ws.AppendChild(autoFilter);
            }
            autoFilter.Reference = filterRange;

            // Set freeze pane below first row
            var sheetViews = ws.GetFirstChild<SheetViews>();
            if (sheetViews == null)
            {
                sheetViews = new SheetViews();
                ws.InsertAt(sheetViews, 0);
            }
            var sheetView = sheetViews.GetFirstChild<SheetView>();
            if (sheetView == null)
            {
                sheetView = new SheetView { WorkbookViewId = 0 };
                sheetViews.AppendChild(sheetView);
            }

            var existingPane = sheetView.GetFirstChild<Pane>();
            existingPane?.Remove();

            var freezeRow = startRow; // freeze after the header row
            var freezeCell = $"{startCol}{freezeRow + 1}";
            var pane = new Pane
            {
                VerticalSplit = freezeRow,
                TopLeftCell = freezeCell,
                State = PaneStateValues.Frozen,
                ActivePane = PaneValues.BottomLeft
            };
            sheetView.InsertAt(pane, 0);
        }

        SaveWorksheet(worksheet);
        return $"Imported {rows.Count} rows x {maxCols} cols into /{sheetName} starting at {startCell.ToUpperInvariant()}";
    }

    /// <summary>
    /// Set a cell's value with automatic type detection.
    /// Order: number -> date (ISO) -> boolean -> formula -> string
    /// </summary>
    private static void SetCellValueWithTypeDetection(Cell cell, string value)
    {
        // Empty
        if (string.IsNullOrEmpty(value))
        {
            cell.CellValue = null;
            cell.DataType = null;
            return;
        }

        // R13-1: enforce Excel's 32767-char per-cell limit at the CSV/TSV
        // import path too, so bulk imports fail fast instead of producing a
        // file Excel refuses to open.
        EnsureCellValueLength(value, cell.CellReference?.Value);

        // Formula: starts with =
        if (value.StartsWith('='))
        {
            cell.CellFormula = new CellFormula(OfficeCli.Core.PivotTableHelper.SanitizeXmlText(OfficeCli.Core.ModernFunctionQualifier.Qualify(value[1..])));
            cell.CellValue = null;
            cell.DataType = null;
            return;
        }

        // Number (integer or decimal)
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numVal))
        {
            cell.CellValue = new CellValue(numVal.ToString(CultureInfo.InvariantCulture));
            cell.DataType = null; // numeric is default
            return;
        }

        // Date: ISO 8601 formats (yyyy-MM-dd, yyyy-MM-ddTHH:mm:ss, etc.)
        if (TryParseIsoDate(value, out var dateVal))
        {
            // Excel stores dates as OLE Automation date numbers
            cell.CellValue = new CellValue(dateVal.ToOADate().ToString(CultureInfo.InvariantCulture));
            cell.DataType = null; // numeric
            return;
        }

        // Boolean: TRUE/FALSE (case-insensitive)
        if (value.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
        {
            cell.CellValue = new CellValue("1");
            cell.DataType = new EnumValue<CellValues>(CellValues.Boolean);
            return;
        }
        if (value.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
        {
            cell.CellValue = new CellValue("0");
            cell.DataType = new EnumValue<CellValues>(CellValues.Boolean);
            return;
        }

        // String (fallback)
        cell.CellValue = new CellValue(value);
        cell.DataType = new EnumValue<CellValues>(CellValues.String);
    }

    private static bool TryParseIsoDate(string value, out DateTime result)
    {
        // Try common ISO date formats
        string[] formats =
        [
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-dd HH:mm:ss"
        ];
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Parse CSV/TSV content into a list of rows, each containing field values.
    /// Handles quoted fields, embedded delimiters, escaped quotes (""), and newlines within quotes.
    /// UTF-8 with optional BOM.
    /// </summary>
    internal static List<List<string>> ParseCsv(string content, char delimiter)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(content))
            return rows;

        // Strip BOM if present
        if (content.Length > 0 && content[0] == '\uFEFF')
            content = content[1..];

        var currentRow = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < content.Length)
        {
            char c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ""
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                    }
                    else
                    {
                        // End of quoted field
                        inQuotes = false;
                        i++;
                    }
                }
                else
                {
                    field.Append(c);
                    i++;
                }
            }
            else
            {
                if (c == '"' && field.Length == 0)
                {
                    // Start of quoted field
                    inQuotes = true;
                    i++;
                }
                else if (c == delimiter)
                {
                    currentRow.Add(field.ToString());
                    field.Clear();
                    i++;
                }
                else if (c == '\r')
                {
                    // End of row
                    currentRow.Add(field.ToString());
                    field.Clear();
                    if (currentRow.Count > 0 && !(currentRow.Count == 1 && currentRow[0] == ""))
                        rows.Add(currentRow);
                    currentRow = new List<string>();
                    i++;
                    if (i < content.Length && content[i] == '\n')
                        i++; // skip \n after \r
                }
                else if (c == '\n')
                {
                    // End of row
                    currentRow.Add(field.ToString());
                    field.Clear();
                    if (currentRow.Count > 0 && !(currentRow.Count == 1 && currentRow[0] == ""))
                        rows.Add(currentRow);
                    currentRow = new List<string>();
                    i++;
                }
                else
                {
                    field.Append(c);
                    i++;
                }
            }
        }

        // Last field/row
        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            if (currentRow.Count > 0 && !(currentRow.Count == 1 && currentRow[0] == ""))
                rows.Add(currentRow);
        }

        return rows;
    }
}
