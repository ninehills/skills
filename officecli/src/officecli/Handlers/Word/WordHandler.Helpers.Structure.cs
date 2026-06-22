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

    /// <summary>
    /// Extract the root path segment (e.g. "/body", "/header[1]", "/footer[2]",
    /// "/styles") from a full parent path. Used by Add helpers that need to
    /// return a path rooted at the actual OOXML part — header/footer parents
    /// must not claim a /body-rooted path since that path won't resolve.
    /// Defaults to "/body" when the input is empty or doesn't start with a
    /// recognized root.
    /// </summary>
    private static string ExtractRootSegment(string? parentPath)
    {
        if (string.IsNullOrEmpty(parentPath)) return "/body";
        var trimmed = parentPath.TrimEnd('/');
        if (trimmed.Length == 0 || trimmed == "/") return "/body";
        // Take the first segment (between leading '/' and the next '/').
        var start = trimmed.StartsWith("/") ? 1 : 0;
        var nextSlash = trimmed.IndexOf('/', start);
        var firstSeg = nextSlash < 0 ? trimmed[start..] : trimmed[start..nextSlash];
        return "/" + firstSeg;
    }

    /// <summary>
    /// Append a child element to parent, but if parent is Body, insert before
    /// the final SectionProperties to maintain valid OOXML structure.
    /// </summary>
    private static void AppendToParent(OpenXmlElement parent, OpenXmlElement child)
    {
        if (parent is Body body)
        {
            // The body-level sectPr is ALWAYS the last child, so check LastChild
            // (O(1)) before falling back to GetFirstChild<SectionProperties>
            // (which scans the whole child list — O(N) per append). Batch replay
            // appends thousands of paragraphs to /body; the O(N) scan made that
            // O(N²) (a 13k-paragraph doc spent tens of seconds just locating the
            // trailing sectPr over and over).
            var lastSectPr = body.LastChild as SectionProperties
                             ?? body.GetFirstChild<SectionProperties>();
            if (lastSectPr != null)
            {
                body.InsertBefore(child, lastSectPr);
                return;
            }
        }
        parent.AppendChild(child);
    }

    /// <summary>
    /// Insert <paramref name="child"/> into <paramref name="parent"/> at the
    /// ChildElements index specified by <paramref name="index"/>. If the
    /// index is null or out of range, falls back to <see cref="AppendToParent"/>
    /// (which respects Body's trailing sectPr).
    /// </summary>
    private static void InsertAtIndexOrAppend(OpenXmlElement parent, OpenXmlElement child, int? index)
    {
        if (index.HasValue && index.Value >= 0 && index.Value < parent.ChildElements.Count)
        {
            parent.InsertBefore(child, parent.ChildElements[index.Value]);
            return;
        }
        AppendToParent(parent, child);
    }

    /// <summary>
    /// Insert <paramref name="newElem"/> into <paramref name="para"/> at the
    /// ChildElements index specified by <paramref name="index"/>, clamping
    /// forward past any leading ParagraphProperties so pPr stays first child.
    /// Null/out-of-range index appends.
    /// </summary>
    private static void InsertIntoParagraph(Paragraph para, OpenXmlElement newElem, int? index)
    {
        var children = para.ChildElements.ToList();
        if (index.HasValue && index.Value >= 0 && index.Value < children.Count)
        {
            var refElem = children[index.Value];
            if (refElem is ParagraphProperties)
            {
                if (index.Value + 1 < children.Count)
                    para.InsertBefore(newElem, children[index.Value + 1]);
                else
                    para.AppendChild(newElem);
            }
            else
            {
                para.InsertBefore(newElem, refElem);
            }
            return;
        }
        para.AppendChild(newElem);
    }

    /// <summary>
    /// Insert multiple elements consecutively into a paragraph, starting at
    /// the ChildElements index (clamped forward past pPr). Later elements go
    /// after earlier ones in order.
    /// </summary>
    private static void InsertIntoParagraph(Paragraph para, IList<OpenXmlElement> newElems, int? index)
    {
        if (newElems == null || newElems.Count == 0) return;
        InsertIntoParagraph(para, newElems[0], index);
        for (int i = 1; i < newElems.Count; i++)
        {
            para.InsertAfter(newElems[i], newElems[i - 1]);
        }
    }

    // CONSISTENCY(para-path-canonical): replace the last `/p[...]` segment
    // in <paramref name="path"/> with paraId-form (`/p[@paraId=X]`) when the
    // paragraph carries a w14:paraId. Used by Add helpers whose `parentPath`
    // already targets the paragraph itself (so re-appending /p[N] would
    // double the segment) — the result mirrors what Get later surfaces, so
    // the returned path round-trips through subsequent Get/Set calls
    // without rewriting.
    private static string ReplaceTrailingParaSegment(string path, Paragraph para)
    {
        if (para.ParagraphId?.Value == null) return path;
        var idx = path.LastIndexOf("/p[", StringComparison.Ordinal);
        if (idx < 0) return path;
        var endIdx = path.IndexOf(']', idx);
        if (endIdx < 0) return path;
        return path[..idx] + $"/p[@paraId={para.ParagraphId.Value}]" + path[(endIdx + 1)..];
    }
}
