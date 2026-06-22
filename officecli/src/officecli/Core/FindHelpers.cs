// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace OfficeCli.Core;

/// <summary>
/// Shared find-pattern parsing and matching for text find/replace across handlers.
/// Word and PowerPoint accept the same find syntax (plain text or r"..." regex) and
/// bound regex matching with the same catastrophic-backtracking timeout, so the
/// parse + match-range logic lives here rather than being duplicated per handler.
/// </summary>
internal static class FindHelpers
{
    // BUG-TESTER fuzz-2: bound regex match time on user-supplied find patterns to
    // prevent catastrophic-backtracking DoS (e.g. "(a+)+b" against long inputs).
    internal static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Parse a find pattern: plain text or regex (r"..." / r'...' prefix).
    /// Returns (pattern, isRegex).
    /// </summary>
    internal static (string Pattern, bool IsRegex) ParseFindPattern(string value)
    {
        // r"..." or r'...' → regex
        if (value.Length >= 3 && value[0] == 'r' && (value[1] == '"' || value[1] == '\''))
        {
            var quote = value[1];
            var endIdx = value.LastIndexOf(quote);
            if (endIdx > 1)
                return (value[2..endIdx], true);
        }
        return (value, false);
    }

    /// <summary>
    /// Find all match ranges in fullText using either plain text or regex.
    /// Returns list of (start, length) pairs, sorted by start ascending.
    /// Zero-length regex matches are skipped. Invalid patterns and
    /// catastrophic-backtracking timeouts surface as ArgumentException.
    /// </summary>
    internal static List<(int Start, int Length)> FindMatchRanges(string fullText, string pattern, bool isRegex)
    {
        var ranges = new List<(int Start, int Length)>();
        if (isRegex)
        {
            try
            {
                // Bound matching with a hard timeout so catastrophic-backtracking
                // patterns (e.g. "(a+)+b") fail fast instead of hanging the process.
                foreach (Match m in Regex.Matches(fullText, pattern, RegexOptions.None, RegexMatchTimeout))
                {
                    if (m.Length > 0) // skip zero-length matches
                        ranges.Add((m.Index, m.Length));
                }
            }
            catch (RegexParseException ex)
            {
                throw new ArgumentException($"Invalid regex pattern '{pattern}': {ex.Message}", ex);
            }
            catch (RegexMatchTimeoutException ex)
            {
                throw new ArgumentException(
                    $"Regex pattern '{pattern}' exceeded {RegexMatchTimeout.TotalSeconds}s match timeout (catastrophic backtracking?)",
                    ex);
            }
        }
        else
        {
            int idx = 0;
            while ((idx = fullText.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
            {
                ranges.Add((idx, pattern.Length));
                idx += pattern.Length;
            }
        }
        return ranges;
    }
}
