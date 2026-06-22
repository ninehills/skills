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

    // ==================== Binary Extraction ====================
    //
    // Support for `officecli get --save <dest>`. Parses the path to find
    // the owning worksheet and queries the node's relId. Both DrawingsPart
    // (pictures) and WorksheetPart (embedded ole/package) are consulted
    // because pictures live on DrawingsPart while OLE payloads live on
    // WorksheetPart directly.
    public bool TryExtractBinary(string path, string destPath, out string? contentType, out long byteCount)
    {
        contentType = null;
        byteCount = 0;
        var node = Get(path, 0);
        if (node == null) return false;
        if (!node.Format.TryGetValue("relId", out var relObj) || relObj is not string relId
            || string.IsNullOrEmpty(relId))
            return false;

        // Path looks like /SheetName/... — find the worksheet.
        var normalized = NormalizeExcelPath(path);
        normalized = ResolveSheetIndexInPath(normalized);
        var segments = normalized.TrimStart('/').Split('/', 2);
        var sheetName = segments[0];
        var worksheetPart = FindWorksheet(sheetName);
        if (worksheetPart == null) return false;

        DocumentFormat.OpenXml.Packaging.OpenXmlPart? part = null;
        try { part = worksheetPart.GetPartById(relId); } catch { /* try drawing */ }
        if (part == null && worksheetPart.DrawingsPart != null)
        {
            try { part = worksheetPart.DrawingsPart.GetPartById(relId); } catch { /* fall through */ }
        }
        if (part == null) return false;

        // BUG-R10-04: create the destination directory if missing so
        // `get --save ./outdir/file.bin` works when outdir doesn't exist.
        var destDir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // CONSISTENCY(ole-cfb-wrap): non-Office OLE payloads are stored as
        // CFB containers with \x01Ole10Native; unwrap on read so the caller
        // gets back the bytes they fed in via `add ole src=...`.
        byte[] rawBytes;
        using (var src = part.GetStream())
        using (var ms = new MemoryStream())
        {
            src.CopyTo(ms);
            rawBytes = ms.ToArray();
        }
        var payload = OfficeCli.Core.OleHelper.UnwrapOle10NativeIfCfb(rawBytes);
        File.WriteAllBytes(destPath, payload);
        byteCount = payload.Length;
        contentType = part.ContentType;
        return true;
    }

    // ==================== OLE Object Writing Helpers ====================

    /// <summary>
    /// Ensure the given VmlDrawingPart contains a minimal v:shape with the
    /// specified shapeId so the schema-required <c>oleObject/@shapeId</c>
    /// attribute has a valid target. Modern Excel (2010+) renders OLE from
    /// the companion <c>objectPr/anchor</c>, but the shape itself still
    /// has to exist for a round-trip — otherwise opening the workbook in
    /// older Excel versions tends to drop the object silently.
    /// </summary>
    internal static void EnsureExcelVmlShapeForOle(VmlDrawingPart vmlPart, uint shapeId,
        int fromCol, int fromRow, int toCol, int toRow)
    {
        // Load the existing VML (may be absent on a freshly-created part).
        string existing;
        try
        {
            using var readStream = vmlPart.GetStream(FileMode.OpenOrCreate, FileAccess.Read);
            using var reader = new StreamReader(readStream);
            existing = reader.ReadToEnd();
        }
        catch
        {
            existing = string.Empty;
        }

        // VML clientData carries the anchor (16 coordinates: from/to col/row + offsets).
        // Coordinates are in the legacy "left, top, right, bottom" pixel order.
        var anchorValue = $"{fromCol}, 0, {fromRow}, 0, {toCol}, 0, {toRow}, 0";
        var newShape = $"""
<v:shape id="_x0000_s{shapeId}" type="#_x0000_t75" style='position:absolute;margin-left:0;margin-top:0;width:100pt;height:40pt;visibility:hidden' o:oleicon="t" o:ole="" filled="f" stroked="f">
 <v:imagedata chromakey="white"/>
 <o:lock v:ext="edit" aspectratio="t"/>
 <x:ClientData ObjectType="Pict">
  <x:Anchor>{anchorValue}</x:Anchor>
  <x:CF>Pict</x:CF>
  <x:AutoPict/>
 </x:ClientData>
</v:shape>
""";

        string merged;
        if (string.IsNullOrWhiteSpace(existing))
        {
            // Build a minimal xml with shapetype + our shape.
            merged = $"""
<xml xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel">
 <o:shapelayout v:ext="edit"><o:idmap v:ext="edit" data="1"/></o:shapelayout>
 <v:shapetype id="_x0000_t75" coordsize="21600,21600" o:spt="75" o:preferrelative="t" path="m@4@5l@4@11@9@11@9@5xe" filled="f" stroked="f">
  <v:stroke joinstyle="miter"/>
  <v:formulas>
   <v:f eqn="if lineDrawn pixelLineWidth 0"/>
   <v:f eqn="sum @0 1 0"/>
   <v:f eqn="sum 0 0 @1"/>
   <v:f eqn="prod @2 1 2"/>
   <v:f eqn="prod @3 21600 pixelWidth"/>
   <v:f eqn="prod @3 21600 pixelHeight"/>
   <v:f eqn="sum @0 0 1"/>
   <v:f eqn="prod @6 1 2"/>
   <v:f eqn="prod @7 21600 pixelWidth"/>
   <v:f eqn="sum @8 21600 0"/>
   <v:f eqn="prod @7 21600 pixelHeight"/>
   <v:f eqn="sum @10 21600 0"/>
  </v:formulas>
  <v:path o:extrusionok="f" gradientshapeok="t" o:connecttype="rect"/>
  <o:lock v:ext="edit" aspectratio="t"/>
 </v:shapetype>
{newShape}
</xml>
""";
        }
        else
        {
            // Append our shape before the closing </xml> tag.
            var closeIdx = existing.LastIndexOf("</xml>", StringComparison.OrdinalIgnoreCase);
            if (closeIdx < 0) closeIdx = existing.Length;
            merged = existing.Substring(0, closeIdx) + newShape + "\n</xml>";
        }

        using var writeStream = vmlPart.GetStream(FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(writeStream);
        writer.Write(merged);
    }

    // ==================== OLE Object Reading ====================
    //
    // Enumerate all OLE objects attached to a worksheet. Excel stores these
    // as <x:oleObjects> inside the worksheet (each <x:oleObject> has
    // progId + shapeId + r:id), plus matching EmbeddedObjectPart /
    // EmbeddedPackagePart parts joined by rel id.
    //
    // CONSISTENCY(ole-orphan-indexing): orphan embedded parts (backing parts
    // with no matching x:oleObject XML element) are intentionally NOT
    // surfaced under the ole[N] index. Set/Remove dispatch on
    // ws.Descendants<OleObject>() which only yields schema-typed elements;
    // indexing orphans here would cause Get to return nodes that Set/Remove
    // cannot address. Orphans can still be audited via Validate() or raw
    // package inspection.
    internal List<DocumentNode> CollectOleNodesForSheet(string sheetName, WorksheetPart worksheetPart)
    {
        var nodes = new List<DocumentNode>();

        // Walk schema-typed <x:oleObject> elements (may live inside
        // <oleObjects>, directly under <worksheet>, or wrapped in an
        // <mc:AlternateContent><mc:Choice>...</mc:Choice></mc:AlternateContent>).
        // Descendants<OleObject> picks all of those up.
        var oleElements = GetSheet(worksheetPart).Descendants<OleObject>().ToList();
        for (int i = 0; i < oleElements.Count; i++)
        {
            var ole = oleElements[i];
            var node = new DocumentNode
            {
                Path = $"/{sheetName}/ole[{i + 1}]",
                Type = "ole",
                Text = ole.ProgId?.Value ?? "",
            };
            node.Format["objectType"] = "ole";
            // CONSISTENCY(ole-display): PPT and Word OLE Get both expose
            // Format["display"]. Excel worksheet OLE objects have no
            // DrawAspect concept — they always render as icons — so emit
            // a fixed "icon" value for schema symmetry.
            node.Format["display"] = "icon";
            if (ole.ProgId?.Value != null) node.Format["progId"] = ole.ProgId.Value;
            if (ole.ShapeId?.Value != null) node.Format["shapeId"] = (long)ole.ShapeId.Value;
            if (ole.Link?.Value != null) node.Format["link"] = ole.Link.Value;

            var relId = ole.Id?.Value;
            if (!string.IsNullOrEmpty(relId))
            {
                node.Format["relId"] = relId;
                try
                {
                    var part = worksheetPart.GetPartById(relId);
                    if (part != null)
                        OfficeCli.Core.OleHelper.PopulateFromPart(node, part, ole.ProgId?.Value);
                }
                catch
                {
                    // Relationship may be missing; leave part-sourced fields absent.
                }
            }

            // Expose anchor rectangle as unit-qualified width/height (cm).
            // CONSISTENCY(ole-width-units): mirrors PPTX/Word OLE which emit
            // EmuConverter.FormatEmu strings. Internally the anchor stores
            // only cell markers (col/row), so convert via the same rough
            // default-column/row → EMU constants used by ParseAnchorDimension
            // (Add-side). Known limitation: Excel's actual column widths are
            // ignored; this is a symmetric round-trip of the Add inputs.
            var objectPr = ole.GetFirstChild<EmbeddedObjectProperties>();
            var objAnchor = objectPr?.GetFirstChild<ObjectAnchor>();
            if (objAnchor != null)
            {
                var fromM = objAnchor.GetFirstChild<FromMarker>();
                var toM = objAnchor.GetFirstChild<ToMarker>();
                if (fromM != null && toM != null)
                {
                    int fromCol = 0, fromRow = 0, toCol = 0, toRow = 0;
                    long fromColOff = 0, fromRowOff = 0, toColOff = 0, toRowOff = 0;
                    int.TryParse(fromM.GetFirstChild<XDR.ColumnId>()?.Text ?? "0", out fromCol);
                    int.TryParse(fromM.GetFirstChild<XDR.RowId>()?.Text ?? "0", out fromRow);
                    int.TryParse(toM.GetFirstChild<XDR.ColumnId>()?.Text ?? "0", out toCol);
                    int.TryParse(toM.GetFirstChild<XDR.RowId>()?.Text ?? "0", out toRow);
                    long.TryParse(fromM.GetFirstChild<XDR.ColumnOffset>()?.Text ?? "0", out fromColOff);
                    long.TryParse(fromM.GetFirstChild<XDR.RowOffset>()?.Text ?? "0", out fromRowOff);
                    long.TryParse(toM.GetFirstChild<XDR.ColumnOffset>()?.Text ?? "0", out toColOff);
                    long.TryParse(toM.GetFirstChild<XDR.RowOffset>()?.Text ?? "0", out toRowOff);
                    // CONSISTENCY(ole-width-units): rebuild EMU extent from
                    // (cell-count * approx-per-cell) + (to-offset - from-offset)
                    // so sub-cell precision set on Add survives Get.
                    long widthEmu = Math.Max(0, (long)(toCol - fromCol)) * EmuPerColApprox
                        + (toColOff - fromColOff);
                    long heightEmu = Math.Max(0, (long)(toRow - fromRow)) * EmuPerRowApprox
                        + (toRowOff - fromRowOff);
                    if (widthEmu < 0) widthEmu = 0;
                    if (heightEmu < 0) heightEmu = 0;
                    node.Format["width"] = OfficeCli.Core.EmuConverter.FormatEmu(widthEmu);
                    node.Format["height"] = OfficeCli.Core.EmuConverter.FormatEmu(heightEmu);
                    // CONSISTENCY(ole-anchor-roundtrip): expose the cell-range
                    // form so `add ... anchor=B2:D4` survives Get/Query. XDR
                    // markers are 0-based; A1-style needs +1 on both axes.
                    node.Format["anchor"] =
                        $"{IndexToColumnName(fromCol + 1)}{fromRow + 1}:{IndexToColumnName(toCol + 1)}{toRow + 1}";
                }
            }

            nodes.Add(node);
        }

        return nodes;
    }
}
