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

    // ==================== Binary Extraction ====================
    //
    // Support for `officecli get --save <dest>`. The node's relId plus
    // the /slide[N]/ prefix in the path identifies the owning SlidePart;
    // the payload part is then looked up and its stream copied out.
    public bool TryExtractBinary(string path, string destPath, out string? contentType, out long byteCount)
    {
        contentType = null;
        byteCount = 0;
        var node = Get(path, 0);
        if (node == null) return false;
        if (!node.Format.TryGetValue("relId", out var relObj) || relObj is not string relId
            || string.IsNullOrEmpty(relId))
            return false;

        // Infer slide index from the path (/slide[N]/...).
        var m = System.Text.RegularExpressions.Regex.Match(path, @"^/slide\[(\d+)\]");
        if (!m.Success) return false;
        var slideIdx = int.Parse(m.Groups[1].Value);
        var slideParts = GetSlideParts().ToList();
        if (slideIdx < 1 || slideIdx > slideParts.Count) return false;

        var slidePart = slideParts[slideIdx - 1];
        DocumentFormat.OpenXml.Packaging.OpenXmlPart? part = null;
        try { part = slidePart.GetPartById(relId); } catch { /* not on slide */ }
        if (part == null) return false;

        // BUG-R10-04: create the destination directory if missing so
        // `get --save ./outdir/file.bin` works when outdir doesn't exist.
        var destDir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // CONSISTENCY(ole-cfb-wrap): unwrap CFB Ole10Native payload on read.
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

    // ==================== OLE Object Reading ====================
    //
    // Enumerate all OLE objects on a slide. PPTX wraps OLE in a
    // GraphicFrame whose GraphicData uri = "*/ole" contains a <p:oleObj>
    // element with progId + r:id. We walk descendants to catch both the
    // modern (p:oleObj as direct child) and alternate content fallback
    // forms. Orphan embedded parts (not referenced by any oleObj) are
    // surfaced the same way as the Excel reader, so nothing disappears.
    internal List<DocumentNode> CollectOleNodesForSlide(int slideNum, SlidePart slidePart)
    {
        var nodes = new List<DocumentNode>();
        var seenRelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var shapeTree = GetSlide(slidePart).CommonSlideData?.ShapeTree;
        if (shapeTree == null) return nodes;

        // 1. Walk GraphicFrames hosting p:oleObj (strong-typed via SDK).
        int oleIdx = 0;
        foreach (var gf in shapeTree.Descendants<GraphicFrame>())
        {
            // A GraphicFrame may carry table/chart/ole — filter on the
            // presence of a strong-typed OleObject descendant.
            var oleObj = gf.Descendants<DocumentFormat.OpenXml.Presentation.OleObject>().FirstOrDefault();
            if (oleObj == null) continue;

            oleIdx++;
            var node = new DocumentNode
            {
                Path = $"/slide[{slideNum}]/ole[{oleIdx}]",
                Type = "ole",
                Text = oleObj.ProgId?.Value ?? "",
            };
            node.Format["objectType"] = "ole";
            if (oleObj.ProgId?.Value != null) node.Format["progId"] = oleObj.ProgId.Value;
            if (oleObj.Name?.Value != null) node.Format["name"] = oleObj.Name.Value;
            // CONSISTENCY(ole-display): always emit display key so callers can
            // rely on it being present; mirrors Word OLE DrawAspect normalization.
            node.Format["display"] = (oleObj.ShowAsIcon?.Value == true) ? "icon" : "content";
            // CONSISTENCY(ole-width-units): imgW/imgH (raw EMU) used to be
            // surfaced here but duplicated the unit-qualified width/height
            // emitted from the graphicFrame xfrm below. Kept internal only.

            // Extents + offset from the frame's own xfrm.
            var xfrm = gf.Transform;
            if (xfrm?.Offset != null)
            {
                if (xfrm.Offset.X?.Value != null)
                    node.Format["x"] = OfficeCli.Core.EmuConverter.FormatEmu(xfrm.Offset.X.Value);
                if (xfrm.Offset.Y?.Value != null)
                    node.Format["y"] = OfficeCli.Core.EmuConverter.FormatEmu(xfrm.Offset.Y.Value);
            }
            if (xfrm?.Extents != null)
            {
                if (xfrm.Extents.Cx?.Value != null)
                    node.Format["width"] = OfficeCli.Core.EmuConverter.FormatEmu(xfrm.Extents.Cx.Value);
                if (xfrm.Extents.Cy?.Value != null)
                    node.Format["height"] = OfficeCli.Core.EmuConverter.FormatEmu(xfrm.Extents.Cy.Value);
            }

            var relId = oleObj.Id?.Value;
            if (!string.IsNullOrEmpty(relId))
            {
                node.Format["relId"] = relId;
                seenRelIds.Add(relId);
                try
                {
                    var part = slidePart.GetPartById(relId);
                    if (part != null)
                        OfficeCli.Core.OleHelper.PopulateFromPart(node, part, oleObj.ProgId?.Value);
                }
                catch
                {
                    // Ignore rel-join failures; keep whatever we got from XML.
                }
            }

            nodes.Add(node);
        }

        // CONSISTENCY(ole-orphan-indexing): orphan embedded parts are NOT
        // indexed under ole[N] to keep Get/Set/Remove in lockstep. Set/Remove
        // dispatch on schema-typed <p:oleObj> elements only; indexing orphans
        // here would produce Get-visible nodes that Set/Remove cannot
        // address. See ExcelHandler.Helpers.cs for the mirror comment.

        return nodes;
    }
}
