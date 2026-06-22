// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace OfficeCli.Core;

/// <summary>
/// Detects when the user's path argument was rewritten by Git Bash / MSYS2 /
/// Cygwin's POSIX-to-Windows path conversion before reaching the CLI. The
/// shell turns a leading '/' into its install root (e.g. '/' becomes
/// 'C:/Program Files/Git/'), which then fails downstream path validation.
/// </summary>
internal static class MsysPathHint
{
    // Canonical prefix that opens every hint we emit. AugmentMessage uses it
    // as the idempotency sentinel — keeping prefix and sentinel as ONE constant
    // means a future copy-edit can't desync them without the compiler noticing.
    private const string HintPrefix = "Git Bash rewrote";

    // Mangled paths look like: lowercase drive letter, forward slashes,
    // optional trailing slash.  e.g. "d:/git/git/", "c:/program files/git/body".
    private static readonly Regex MangledShape = new(
        @"^[a-z]:/", RegexOptions.Compiled);

    // Scan for mangled-shape tokens embedded in a longer error message.
    // The token stops at whitespace, quotes, brackets, or sentence punctuation
    // so we don't slurp trailing words.
    private static readonly Regex MangledTokenInMessage = new(
        @"[a-z]:/[^\s'""<>\)\]]+", RegexOptions.Compiled);

    // Marker files that identify an install as a real MSYS/Git/Cygwin root.
    // Each entry is a relative path from the candidate root directory.
    private static readonly string[] InstallMarkers =
    {
        "usr/bin/msys-2.0.dll",   // MSYS2 / Git Bash
        "git-bash.exe",           // Git for Windows install root
        "cygwin1.dll",            // Cygwin install root
    };

    /// <summary>
    /// Return a hint string when <paramref name="offendingPath"/> looks like a
    /// shell-rewritten argument that fell on disk inside a real MSYS / Git Bash
    /// / Cygwin install. Returns null when nothing matches — call site should
    /// then emit its normal error message unchanged.
    /// </summary>
    public static string? TryDescribeRewrite(string? offendingPath)
    {
        if (string.IsNullOrEmpty(offendingPath)) return null;
        if (!OperatingSystem.IsWindows()) return null;
        if (!MangledShape.IsMatch(offendingPath)) return null;

        // Walk parent directories upward to find an install root that contains
        // the offending path. The portion after the install root is what the
        // user most likely typed.
        var probe = offendingPath.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar);
        if (probe.Length == 0) return null;

        while (true)
        {
            if (LooksLikeShellInstallRoot(probe))
            {
                var probeForward = probe.Replace(Path.DirectorySeparatorChar, '/');
                var tail = offendingPath.TrimEnd('/');
                string original;
                if (tail.Length <= probeForward.Length ||
                    !tail.StartsWith(probeForward, StringComparison.OrdinalIgnoreCase))
                {
                    original = "/";
                }
                else
                {
                    original = tail.Substring(probeForward.Length);
                    if (!original.StartsWith('/')) original = "/" + original;
                }

                var doubled = "/" + original;
                return $"{HintPrefix} '{original}' before officecli received it. " +
                       $"Use '{doubled}' or set MSYS_NO_PATHCONV=1.";
            }

            var parent = Path.GetDirectoryName(probe);
            if (string.IsNullOrEmpty(parent) || parent == probe) return null;
            probe = parent;
        }
    }

    /// <summary>
    /// Single chokepoint used at the error-formatter layer: scan an arbitrary
    /// error message for any embedded MSYS-mangled path token, probe it, and
    /// append a hint if one matches. Idempotent — checked via the shared
    /// <see cref="HintPrefix"/> sentinel so a second pass over an already-
    /// augmented message returns it unchanged.
    /// </summary>
    public static string AugmentMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return message ?? string.Empty;
        if (!OperatingSystem.IsWindows()) return message;
        if (message.Contains(HintPrefix, StringComparison.Ordinal)) return message;
        foreach (Match m in MangledTokenInMessage.Matches(message))
        {
            var hint = TryDescribeRewrite(m.Value);
            if (hint != null)
            {
                // Some upstream errors ("Path not found: <path>") don't end in
                // sentence punctuation. Add a period so the appended hint
                // reads as a separate sentence.
                var sep = message[^1] is '.' or '!' or '?' ? " " : ". ";
                return $"{message}{sep}{hint}";
            }
        }
        return message;
    }

    /// <summary>
    /// Returns true when <paramref name="dir"/> exists on disk and contains a
    /// known MSYS / Git for Windows / Cygwin marker file.
    /// </summary>
    private static bool LooksLikeShellInstallRoot(string dir)
    {
        if (!Directory.Exists(dir)) return false;
        foreach (var marker in InstallMarkers)
        {
            var rel = marker.Replace('/', Path.DirectorySeparatorChar);
            if (File.Exists(Path.Combine(dir, rel))) return true;
        }
        return false;
    }
}
