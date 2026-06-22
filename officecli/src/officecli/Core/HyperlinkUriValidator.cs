// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

namespace OfficeCli.Core;

/// <summary>
/// Cross-handler allowlist for user-supplied hyperlink URI schemes.
///
/// PowerPoint/Excel/Word would otherwise happily write any URI a caller
/// hands in (javascript:, file://, data:, vbscript:) into the document's
/// .rels Target. That round-trips cleanly but lets a malicious caller plant
/// click-bait that triggers script execution or local-file exfiltration on
/// recipients who follow the link. The OOXML format itself does not gate
/// scheme — every Office product applies its own runtime warning UI on top
/// — so we reject unsafe schemes at write time and keep the document clean.
///
/// Handler-internal targets (PowerPoint's ppaction://, slide://, named
/// actions like firstslide/nextslide, fragment anchors like #_ftn1,
/// in-workbook references like Sheet!A1, or any non-absolute URI) are
/// resolved by the handler before this validator is consulted; callers only
/// pass strings here once they have been classified as an external URI.
/// </summary>
public static class HyperlinkUriValidator
{
    // Schemes that survive an Office "is this link safe?" prompt without
    // user warnings. http/https/mailto are the everyday cases; ftp/sms/tel
    // /news are the standard PowerPoint "Action button" set; ppaction is
    // PowerPoint's internal navigation pseudo-scheme and is allowed so a
    // caller can paste a ppaction:// URI it read from another file.
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http",
        "https",
        "mailto",
        "ftp",
        "ftps",
        "sftp",
        "news",
        "tel",
        "sms",
        "ppaction",
        // BUG-R4B(BUG5): file: is a legitimate, low-risk hyperlink scheme that
        // real-world documents use to link local/network resources
        // (file:///C:/...). Unlike javascript:/data:/vbscript:, it does not
        // execute script or exfiltrate data — Office prompts on follow like any
        // external link. Allowing it lets dump→replay round-trip file-target
        // hyperlinks instead of emitting a command the batch rejects.
        // javascript:/data:/vbscript: stay rejected (omitted from this set).
        "file",
        // BUG-DUMP-ABOUTBLANK: `about:` (RFC 6694) is an inert scheme — Office
        // never navigates it and it executes no script. Word natively writes
        // about:blank as the placeholder Target for a hyperlink with no real
        // destination (a styled link whose URL was cleared). Rejecting it dropped
        // the <w:hyperlink> wrapper on dump→batch, so the link text lost its
        // hyperlink styling. Same low-risk rationale as file: above.
        "about",
    };

    /// <summary>
    /// Validate an external hyperlink URI's scheme. Throws ArgumentException
    /// with a deterministic, agent-readable message when the scheme is not
    /// in the allowlist. Empty / null input is a no-op so the caller's own
    /// "missing URL" diagnostic remains the surfaced error.
    /// </summary>
    /// <summary>
    /// Non-throwing predicate: true when <paramref name="url"/> is an absolute
    /// URI whose scheme is in the allowlist. Used by the HTML preview, which
    /// must not throw on an authored-in HYPERLINK() formula but also must not
    /// emit a javascript:/data:/file: href as an XSS sink.
    /// </summary>
    public static bool IsSafeScheme(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return !string.IsNullOrEmpty(uri.Scheme) && AllowedSchemes.Contains(uri.Scheme);
    }

    public static void RequireSafeScheme(string url, string contextKey = "link")
    {
        if (string.IsNullOrEmpty(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return; // not absolute → handler-internal path, not our concern
        var scheme = uri.Scheme;
        if (string.IsNullOrEmpty(scheme)) return;
        if (AllowedSchemes.Contains(scheme)) return;
        throw new ArgumentException(
            $"Invalid {contextKey} URL scheme '{scheme}:': only http, https, mailto, ftp, ftps, sftp, news, tel, sms, file, about, and ppaction targets are accepted. " +
            "javascript:, data:, vbscript:, and similar schemes are rejected to prevent click-bait redirection in shared documents.");
    }
}
