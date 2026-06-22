// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace OfficeCli.Core;

/// <summary>
/// Resolves a trailing positional index in a SELECTOR-form path for the Word and
/// PowerPoint handlers, where element indices are dense 1-based ordinals that
/// match their Get-path position (unlike Excel's sparse RowIndex, which the
/// Excel handler resolves by Path suffix instead).
///
/// A selector like <c>p[2]</c>, <c>table[2]</c>, <c>shape[2]</c> or the
/// slide-scoped <c>slide[1]&gt;shape[2]</c> carries a trailing <c>[N]</c> that is
/// a positional index, not an attribute filter. The handler-level selector
/// parsers only capture <c>[key op value]</c> brackets, so a bare <c>[N]</c> was
/// silently dropped and EVERY element of that type matched. Harmless for a
/// read-only query, but a footgun once <c>Set</c>/<c>Remove</c> began routing
/// non-slash selectors through <c>Query</c>: <c>set "shape[2]"</c> then mutated
/// every shape while reporting success.
///
/// The dispatched result list is already in document/slide order (and already
/// scoped by any <c>slide[N]</c> prefix), so the Nth element is exactly what the
/// slash form <c>/body/p[N]</c> / <c>/slide[1]/shape[N]</c> denotes for a
/// top-level element. Callers must pre-exclude selectors where a trailing
/// numeric bracket is NOT a positional index — leading '/' scoped paths and
/// top-level comma unions.
/// </summary>
internal static class SelectorPositionalIndex
{
    // A trailing `[<digits>]` (optionally followed by whitespace). Pure digits
    // only, so `[fill=red]` / `[hidden]` (filters) never match.
    private static readonly Regex TrailingNumericIndex =
        new(@"\[(\d+)\]\s*$", RegexOptions.Compiled);

    /// <summary>
    /// If <paramref name="selector"/> ends with a positional <c>[N]</c>, return a
    /// single-element list holding the Nth (1-based, document order) result — or
    /// an empty list when N is out of range. A selector with no trailing numeric
    /// index is returned unchanged.
    /// </summary>
    public static List<DocumentNode> TakeNth(string selector, List<DocumentNode> results)
    {
        if (results.Count == 0 || string.IsNullOrEmpty(selector)) return results;
        var m = TrailingNumericIndex.Match(selector);
        if (!m.Success) return results;
        var n = int.Parse(m.Groups[1].Value);
        if (n < 1 || n > results.Count) return new List<DocumentNode>();
        return new List<DocumentNode> { results[n - 1] };
    }
}
