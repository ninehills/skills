// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{

    /// <summary>
    /// Generate a minimal 1x1 light-gray PNG for use as a zoom placeholder.
    /// PowerPoint regenerates the actual slide thumbnail when the file is opened.
    /// </summary>
    private static byte[] GenerateZoomPlaceholderPng()
    {
        // Minimal valid 1x1 PNG (RGBA: light gray #D0D0D0, fully opaque)
        using var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        // PNG signature
        bw.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk: 1x1, 8-bit RGBA
        WriteChunk(bw, "IHDR", new byte[] {
            0, 0, 0, 1, // width = 1
            0, 0, 0, 1, // height = 1
            8,           // bit depth
            6,           // color type = RGBA
            0, 0, 0      // compression, filter, interlace
        });

        // IDAT chunk: zlib-compressed pixel data (filter=0, R=0xD0, G=0xD0, B=0xD0, A=0xFF)
        // Pre-computed deflate of [0x00, 0xD0, 0xD0, 0xD0, 0xFF]
        WriteChunk(bw, "IDAT", new byte[] {
            0x78, 0x01, 0x62, 0x60, 0x60, 0x28, 0x61, 0x28,
            0x61, 0x68, 0xF8, 0x0F, 0x00, 0x01, 0x45, 0x00, 0xC5
        });

        // IEND chunk
        WriteChunk(bw, "IEND", Array.Empty<byte>());

        return ms.ToArray();
    }

    private static void WriteChunk(BinaryWriter bw, string type, byte[] data)
    {
        // Length (big-endian)
        var lenBytes = BitConverter.GetBytes(data.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        bw.Write(lenBytes);

        // Type
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        bw.Write(typeBytes);

        // Data
        bw.Write(data);

        // CRC32 over type + data
        var crcData = new byte[4 + data.Length];
        Array.Copy(typeBytes, 0, crcData, 0, 4);
        Array.Copy(data, 0, crcData, 4, data.Length);
        var crc = Crc32(crcData);
        var crcBytes = BitConverter.GetBytes(crc);
        if (BitConverter.IsLittleEndian) Array.Reverse(crcBytes);
        bw.Write(crcBytes);
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                crc = (crc >> 1) ^ (crc & 1) * 0xEDB88320;
        }
        return ~crc;
    }

    /// <summary>
    /// Find all zoom AlternateContent elements in a shape tree.
    /// </summary>
    private static List<OpenXmlElement> GetZoomElements(ShapeTree shapeTree)
    {
        return shapeTree.ChildElements
            .Where(e => e.LocalName == "AlternateContent" &&
                   e.Descendants().Any(d => d.LocalName == "sldZm"))
            .ToList();
    }

    /// <summary>
    /// Find all 3D model AlternateContent elements in a shape tree.
    /// </summary>
    private static List<OpenXmlElement> GetModel3DElements(ShapeTree shapeTree)
    {
        return shapeTree.ChildElements
            .Where(e => e.LocalName == "AlternateContent" &&
                   e.Descendants().Any(d => d.LocalName == "model3d"))
            .ToList();
    }

    /// <summary>
    /// Build a DocumentNode from a 3D model AlternateContent element.
    /// </summary>
    private DocumentNode Model3DToNode(OpenXmlElement acElement, int slideNum, int modelIdx)
    {
        var node = new DocumentNode
        {
            Path = $"/slide[{slideNum}]/model3d[{modelIdx}]",
            Type = "model3d"
        };

        // Navigate: mc:Choice > p:graphicFrame (or p:sp for legacy)
        var choice = acElement.ChildElements.FirstOrDefault(e => e.LocalName == "Choice");
        var gf = choice?.ChildElements.FirstOrDefault(e => e.LocalName == "graphicFrame")
              ?? choice?.ChildElements.FirstOrDefault(e => e.LocalName == "sp");

        // Name from cNvPr
        var nvGfPr = gf?.ChildElements.FirstOrDefault(e => e.LocalName == "nvGraphicFramePr")
                  ?? gf?.ChildElements.FirstOrDefault(e => e.LocalName == "nvSpPr");
        var cNvPr = nvGfPr?.ChildElements.FirstOrDefault(e => e.LocalName == "cNvPr");
        if (cNvPr != null)
        {
            var nameAttr = cNvPr.GetAttribute("name", "");
            if (!string.IsNullOrEmpty(nameAttr.Value))
                node.Format["name"] = nameAttr.Value;
        }

        // Position/size from xfrm (graphicFrame level) or spPr > xfrm
        var xfrm = gf?.ChildElements.FirstOrDefault(e => e.LocalName == "xfrm");
        if (xfrm == null)
        {
            var spPr = gf?.ChildElements.FirstOrDefault(e => e.LocalName == "spPr");
            xfrm = spPr?.ChildElements.FirstOrDefault(e => e.LocalName == "xfrm");
        }
        if (xfrm != null)
        {
            var off = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "off");
            var ext = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "ext");
            if (off != null)
            {
                var xAttr = off.GetAttribute("x", "");
                var yAttr = off.GetAttribute("y", "");
                if (!string.IsNullOrEmpty(xAttr.Value) && long.TryParse(xAttr.Value, out var xVal))
                    node.Format["x"] = FormatEmu(xVal);
                if (!string.IsNullOrEmpty(yAttr.Value) && long.TryParse(yAttr.Value, out var yVal))
                    node.Format["y"] = FormatEmu(yVal);
            }
            if (ext != null)
            {
                var cxAttr = ext.GetAttribute("cx", "");
                var cyAttr = ext.GetAttribute("cy", "");
                if (!string.IsNullOrEmpty(cxAttr.Value) && long.TryParse(cxAttr.Value, out var cxVal))
                    node.Format["width"] = FormatEmu(cxVal);
                if (!string.IsNullOrEmpty(cyAttr.Value) && long.TryParse(cyAttr.Value, out var cyVal))
                    node.Format["height"] = FormatEmu(cyVal);
            }
        }

        // Model3D-specific properties
        var model3d = acElement.Descendants().FirstOrDefault(d => d.LocalName == "model3d");
        if (model3d != null)
        {
            // Model rotation
            var rot = model3d.Descendants().FirstOrDefault(d => d.LocalName == "rot");
            if (rot != null)
            {
                var ax = rot.GetAttribute("ax", "").Value ?? "";
                var ay = rot.GetAttribute("ay", "").Value ?? "";
                var az = rot.GetAttribute("az", "").Value ?? "";
                if (!string.IsNullOrEmpty(ax) || !string.IsNullOrEmpty(ay) || !string.IsNullOrEmpty(az))
                {
                    static string ToDeg(string val) =>
                        !string.IsNullOrEmpty(val) && int.TryParse(val, out var v) ? (v / 60000.0).ToString("0.##") : "0";
                    node.Format["rotation"] = $"{ToDeg(ax)},{ToDeg(ay)},{ToDeg(az)}";
                }
            }
        }

        return node;
    }

    /// <summary>
    /// Convert a SlideId value to 1-based slide number.
    /// </summary>
    private int SlideIdToNumber(uint sldId)
    {
        var slideIds = _doc.PresentationPart?.Presentation?.GetFirstChild<SlideIdList>()
            ?.Elements<SlideId>().ToList();
        if (slideIds == null) return -1;
        for (int i = 0; i < slideIds.Count; i++)
            if (slideIds[i].Id?.Value == sldId) return i + 1;
        return -1;
    }

    /// <summary>
    /// Build a DocumentNode from a zoom AlternateContent element.
    /// </summary>
    private DocumentNode ZoomToNode(OpenXmlElement acElement, int slideNum, int zoomIdx)
    {
        var node = new DocumentNode
        {
            Path = $"/slide[{slideNum}]/zoom[{zoomIdx}]",
            Type = "zoom"
        };

        // Navigate: mc:Choice > p:graphicFrame
        var choice = acElement.ChildElements.FirstOrDefault(e => e.LocalName == "Choice");
        var gf = choice?.ChildElements.FirstOrDefault(e => e.LocalName == "graphicFrame");

        // Name from cNvPr
        var nvGfPr = gf?.ChildElements.FirstOrDefault(e => e.LocalName == "nvGraphicFramePr");
        var cNvPr = nvGfPr?.ChildElements.FirstOrDefault(e => e.LocalName == "cNvPr");
        if (cNvPr != null)
        {
            var nameAttr = cNvPr.GetAttribute("name", "");
            if (!string.IsNullOrEmpty(nameAttr.Value))
                node.Format["name"] = nameAttr.Value;
        }

        // Position from xfrm
        var xfrm = gf?.ChildElements.FirstOrDefault(e => e.LocalName == "xfrm");
        if (xfrm != null)
        {
            var off = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "off");
            var ext = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "ext");
            if (off != null)
            {
                var xAttr = off.GetAttribute("x", "");
                var yAttr = off.GetAttribute("y", "");
                if (!string.IsNullOrEmpty(xAttr.Value) && long.TryParse(xAttr.Value, out var x))
                    node.Format["x"] = FormatEmu(x);
                if (!string.IsNullOrEmpty(yAttr.Value) && long.TryParse(yAttr.Value, out var y))
                    node.Format["y"] = FormatEmu(y);
            }
            if (ext != null)
            {
                var cxAttr = ext.GetAttribute("cx", "");
                var cyAttr = ext.GetAttribute("cy", "");
                if (!string.IsNullOrEmpty(cxAttr.Value) && long.TryParse(cxAttr.Value, out var cx))
                    node.Format["width"] = FormatEmu(cx);
                if (!string.IsNullOrEmpty(cyAttr.Value) && long.TryParse(cyAttr.Value, out var cy))
                    node.Format["height"] = FormatEmu(cy);
            }
        }

        // Zoom properties from sldZmObj / zmPr
        var sldZmObj = acElement.Descendants().FirstOrDefault(d => d.LocalName == "sldZmObj");
        if (sldZmObj != null)
        {
            var sldIdAttr = sldZmObj.GetAttribute("sldId", "");
            if (!string.IsNullOrEmpty(sldIdAttr.Value) && uint.TryParse(sldIdAttr.Value, out var sldId))
            {
                var targetNum = SlideIdToNumber(sldId);
                if (targetNum > 0) node.Format["target"] = targetNum;
            }
        }

        var zmPr = acElement.Descendants().FirstOrDefault(d => d.LocalName == "zmPr");
        if (zmPr != null)
        {
            var rtpAttr = zmPr.GetAttribute("returnToParent", "");
            if (!string.IsNullOrEmpty(rtpAttr.Value))
            {
                // Schema declares bool; normalize "1"/"0"/"true"/"false" → bool.
                node.Format["returnToParent"] = rtpAttr.Value is "1" or "true";
            }
            var tdAttr = zmPr.GetAttribute("transitionDur", "");
            if (!string.IsNullOrEmpty(tdAttr.Value))
                node.Format["transitionDur"] = tdAttr.Value;
        }

        return node;
    }
}
