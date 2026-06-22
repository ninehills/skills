// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeCli.Core;
using Vml = DocumentFormat.OpenXml.Vml;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using M = DocumentFormat.OpenXml.Math;

namespace OfficeCli.Handlers;

public partial class WordHandler
{

    private string? FindWatermark()
    {
        var headerParts = _doc.MainDocumentPart?.HeaderParts;
        if (headerParts == null) return null;

        foreach (var headerPart in headerParts)
        {
            var header = headerPart.Header;
            if (header == null) continue;

            // Search for VML shapes with watermark
            foreach (var pict in header.Descendants<DocumentFormat.OpenXml.Wordprocessing.Picture>())
            {
                foreach (var shape in pict.Descendants<Vml.Shape>())
                {
                    var id = shape.GetAttribute("id", "");
                    if (id.Value?.Contains("WaterMark", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var textPath = shape.Descendants<Vml.TextPath>().FirstOrDefault();
                        return textPath?.String?.Value ?? "(image watermark)";
                    }
                }
            }

            // Also check for DrawingML watermarks
            foreach (var drawing in header.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>())
            {
                // Simple detection: check if it looks like a watermark by inline/anchor properties
                var docProps = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().FirstOrDefault();
                if (docProps?.Name?.Value?.Contains("WaterMark", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return "(image watermark)";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Remove all header parts that contain watermark SDT elements.
    /// </summary>
    private void RemoveWatermarkHeaders()
    {
        var mainPart = _doc.MainDocumentPart;
        if (mainPart == null) return;

        var toRemove = new List<HeaderPart>();
        foreach (var hp in mainPart.HeaderParts)
        {
            if (hp.Header == null) continue;
            // Check for watermark: SDT with docPartGallery="Watermarks" or VML shape with "WaterMark" in id
            var hasSdt = hp.Header.Descendants<SdtProperties>()
                .Any(sp => sp.Descendants<DocPartGallery>().Any(g =>
                    g.Val?.Value?.Equals("Watermarks", StringComparison.OrdinalIgnoreCase) == true));
            if (hasSdt)
            {
                toRemove.Add(hp);
                continue;
            }
            foreach (var pict in hp.Header.Descendants<DocumentFormat.OpenXml.Wordprocessing.Picture>())
            {
                var hasWm = pict.InnerXml.Contains("WaterMark", StringComparison.OrdinalIgnoreCase);
                if (hasWm) { toRemove.Add(hp); break; }
            }
        }

        foreach (var hp in toRemove)
        {
            // Remove header references from section properties
            var relId = mainPart.GetIdOfPart(hp);
            foreach (var sectPr in mainPart.Document?.Body?.Descendants<SectionProperties>() ?? Enumerable.Empty<SectionProperties>())
            {
                var refs = sectPr.Elements<HeaderReference>().Where(r => r.Id?.Value == relId).ToList();
                foreach (var r in refs) r.Remove();
            }
            mainPart.DeletePart(hp);
        }
    }

    private List<string> GetHeaderTexts()
    {
        var results = new List<string>();
        var headerParts = _doc.MainDocumentPart?.HeaderParts;
        if (headerParts == null) return results;

        foreach (var headerPart in headerParts)
        {
            var header = headerPart.Header;
            if (header == null) continue;
            var text = string.Concat(header.Descendants<Text>().Select(t => t.Text)).Trim();
            if (!string.IsNullOrEmpty(text))
                results.Add(text);
        }

        return results;
    }

    private List<string> GetFooterTexts()
    {
        var results = new List<string>();
        var footerParts = _doc.MainDocumentPart?.FooterParts;
        if (footerParts == null) return results;

        foreach (var footerPart in footerParts)
        {
            var footer = footerPart.Footer;
            if (footer == null) continue;

            // Build footer text by processing paragraphs, resolving field codes
            var footerLines = new List<string>();
            foreach (var para in footer.Descendants<Paragraph>())
            {
                var sb = new System.Text.StringBuilder();
                bool inField = false;
                bool pastSeparator = false;

                foreach (var run in para.Elements<Run>())
                {
                    var fldChar = run.GetFirstChild<FieldChar>();
                    if (fldChar != null)
                    {
                        if (fldChar.FieldCharType! == FieldCharValues.Begin)
                        {
                            inField = true;
                            pastSeparator = false;
                        }
                        else if (fldChar.FieldCharType! == FieldCharValues.Separate)
                        {
                            pastSeparator = true;
                        }
                        else if (fldChar.FieldCharType! == FieldCharValues.End)
                        {
                            inField = false;
                            pastSeparator = false;
                        }
                        continue;
                    }

                    var fieldCode = run.GetFirstChild<FieldCode>();
                    if (fieldCode != null)
                    {
                        // Extract field type from instruction (e.g., " PAGE " -> "PAGE")
                        var instr = fieldCode.Text?.Trim() ?? "";
                        var fieldType = instr.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? instr;
                        sb.Append($"[{fieldType.ToUpperInvariant()}]");
                        continue;
                    }

                    // Skip result runs inside a field (they contain stale/literal values)
                    if (inField && pastSeparator)
                        continue;

                    var text = run.GetFirstChild<Text>();
                    if (text != null)
                        sb.Append(text.Text);
                }

                var line = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(line))
                    footerLines.Add(line);
            }

            if (footerLines.Count > 0)
                results.Add(string.Join(" ", footerLines));
        }

        return results;
    }
}
