// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml.Packaging;

namespace OfficeCli.Core;

/// <summary>
/// Shared helpers for HTML preview rendering across PowerPoint, Word, and Excel handlers.
/// </summary>
internal static class HtmlPreviewHelper
{
    /// <summary>
    /// HTML-encode text for safe insertion into element content or double-quoted
    /// attribute values: escapes &amp;, &lt;, &gt;, double-quote, and single-quote.
    /// This is the plain entity-encoding shared by the PowerPoint, Excel, and chart
    /// SVG renderers. (Word's preview uses a variant that additionally preserves
    /// consecutive spaces as non-breaking spaces and does not escape the apostrophe —
    /// see WordHandler.HtmlPreview.Css.HtmlEncode, kept separate by design.)
    /// </summary>
    public static string HtmlEncode(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Load an OpenXML part by its relationship ID and return the content as a base64 data URI.
    /// Returns null if the part cannot be found or read.
    /// </summary>
    public static string? PartToDataUri(OpenXmlPart parentPart, string relId)
    {
        try
        {
            var part = parentPart.GetPartById(relId);
            using var stream = part.GetStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var contentType = part.ContentType ?? "image/png";
            return $"data:{contentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        }
        catch
        {
            return null;
        }
    }
}
