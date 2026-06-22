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

    // CONSISTENCY(style-dual-key): resolve a style display name to its
    // OOXML styleId by scanning the styles part. Returns null when no
    // matching style is found, letting callers fall back to using the
    // value verbatim (lenient input). Used by paragraph-level Set on
    // styleName so users can write back the canonical readback key.
    private string? ResolveStyleIdFromName(string displayName)
    {
        var stylesPart = _doc.MainDocumentPart?.StyleDefinitionsPart;
        if (stylesPart?.Styles == null || string.IsNullOrEmpty(displayName)) return null;
        var match = stylesPart.Styles.Elements<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleName?.Val?.Value, displayName, StringComparison.Ordinal));
        return match?.StyleId?.Value;
    }

    /// <summary>
    /// Returns true if a style with the given styleId exists in the Styles part.
    /// "Normal" is implicit in OOXML and considered to exist even when the
    /// blank-document StyleDefinitionsPart is empty/absent — matches Word's
    /// own behaviour where every doc has Normal as the default paragraph style.
    /// </summary>
    internal bool StyleIdExists(string? styleId)
    {
        if (string.IsNullOrEmpty(styleId)) return false;
        if (string.Equals(styleId, "Normal", StringComparison.Ordinal)) return true;
        var stylesPart = _doc.MainDocumentPart?.StyleDefinitionsPart;
        if (stylesPart?.Styles == null) return false;
        return stylesPart.Styles.Elements<Style>()
            .Any(s => string.Equals(s.StyleId?.Value, styleId, StringComparison.Ordinal));
    }

    private string GetStyleName(Paragraph para)
    {
        var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (styleId == null) return "Normal";

        // Try to resolve display name from styles part
        var stylesPart = _doc.MainDocumentPart?.StyleDefinitionsPart;
        if (stylesPart?.Styles != null)
        {
            var style = stylesPart.Styles.Elements<Style>()
                .FirstOrDefault(s => s.StyleId?.Value == styleId);
            if (style?.StyleName?.Val?.Value != null)
                return style.StyleName.Val.Value;
        }

        return styleId;
    }

    private static int GetHeadingLevel(string styleName)
    {
        // Heading 1, Heading 2, heading1, 标题 1, etc.
        foreach (var ch in styleName)
        {
            if (char.IsDigit(ch))
                return ch - '0';
        }
        if (styleName == "Title") return 0;
        if (styleName == "Subtitle") return 1;
        return 1;
    }

    private static bool IsNormalStyle(string styleName)
    {
        return styleName is "Normal" or "正文" or "Body Text" or "Body" or "a"
            || styleName.StartsWith("Normal");
    }
}
