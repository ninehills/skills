// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{
    private record ShapeSelector(string? ElementType, int? SlideNum, string? TextContains,
        string? FontEquals, string? FontNotEquals, bool? IsTitle, bool? HasAlt,
        Dictionary<string, (string Value, bool Negate)>? Attributes = null);

    private static bool ContainsTopLevelComma(string selector)
    {
        int depthBracket = 0, depthParen = 0;
        char? quote = null;
        foreach (var c in selector)
        {
            if (quote.HasValue) { if (c == quote.Value) quote = null; continue; }
            if (c == '"' || c == '\'') { quote = c; continue; }
            if (c == '[') depthBracket++;
            else if (c == ']') depthBracket = Math.Max(0, depthBracket - 1);
            else if (c == '(') depthParen++;
            else if (c == ')') depthParen = Math.Max(0, depthParen - 1);
            else if (c == ',' && depthBracket == 0 && depthParen == 0) return true;
        }
        return false;
    }

    private static List<string> SplitTopLevelCommas(string selector)
    {
        var parts = new List<string>();
        int depthBracket = 0, depthParen = 0;
        char? quote = null;
        int start = 0;
        for (int i = 0; i < selector.Length; i++)
        {
            var c = selector[i];
            if (quote.HasValue) { if (c == quote.Value) quote = null; continue; }
            if (c == '"' || c == '\'') { quote = c; continue; }
            if (c == '[') depthBracket++;
            else if (c == ']') depthBracket = Math.Max(0, depthBracket - 1);
            else if (c == '(') depthParen++;
            else if (c == ')') depthParen = Math.Max(0, depthParen - 1);
            else if (c == ',' && depthBracket == 0 && depthParen == 0)
            {
                parts.Add(selector.Substring(start, i - start));
                start = i + 1;
            }
        }
        parts.Add(selector.Substring(start));
        return parts;
    }

    private static string? FindUnsupportedCombinator(string selector)
    {
        int depthBracket = 0, depthParen = 0;
        char? quote = null;
        for (int i = 0; i < selector.Length; i++)
        {
            var c = selector[i];
            if (quote.HasValue) { if (c == quote.Value) quote = null; continue; }
            if (c == '"' || c == '\'') { quote = c; continue; }
            if (c == '[') depthBracket++;
            else if (c == ']') depthBracket = Math.Max(0, depthBracket - 1);
            else if (c == '(') depthParen++;
            else if (c == ')') depthParen = Math.Max(0, depthParen - 1);
            else if (depthBracket == 0 && depthParen == 0)
            {
                // `~=` is an attribute operator (must be inside [], but guard
                // anyway). At top level a bare `~` or `+` is the sibling
                // combinator.
                if (c == '+') return "+";
                if (c == '~' && (i + 1 >= selector.Length || selector[i + 1] != '='))
                    return "~";
            }
        }
        return null;
    }

    private static string? FindUnsupportedDescendant(string selector)
    {
        // Allowed descendant forms: leading `slide ...` (ancestor scoping
        // already handled by ParseShapeSelector); also `slide[N] ...` and
        // `slide > X`. Anything else with whitespace between two top-level
        // tokens is rejected.
        var trimmed = selector.TrimStart();
        if (trimmed.Length == 0) return null;
        if (Regex.IsMatch(trimmed, @"^slide(\s|>|\[)", RegexOptions.IgnoreCase))
            return null;
        // Look for whitespace between two top-level word-starting tokens.
        int depthBracket = 0, depthParen = 0;
        char? quote = null;
        bool sawToken = false;
        for (int i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (quote.HasValue) { if (c == quote.Value) quote = null; continue; }
            if (c == '"' || c == '\'') { quote = c; continue; }
            if (c == '[') { depthBracket++; sawToken = true; continue; }
            if (c == ']') { depthBracket = Math.Max(0, depthBracket - 1); continue; }
            if (c == '(') { depthParen++; sawToken = true; continue; }
            if (c == ')') { depthParen = Math.Max(0, depthParen - 1); continue; }
            if (depthBracket > 0 || depthParen > 0) continue;
            if (char.IsWhiteSpace(c))
            {
                if (!sawToken) continue;
                // Find the next non-space character — if it's a `>` it's a
                // child combinator (handled elsewhere); if it starts another
                // word token, it's an unsupported descendant combinator.
                int j = i + 1;
                while (j < trimmed.Length && char.IsWhiteSpace(trimmed[j])) j++;
                if (j >= trimmed.Length) return null;
                var next = trimmed[j];
                if (next == '>' || next == '+' || next == '~' || next == ',') return null;
                if (char.IsLetter(next) || next == '[' || next == '#' || next == '.' || next == ':')
                {
                    return trimmed.Substring(0, j) + "…";
                }
            }
            else
            {
                sawToken = true;
            }
        }
        return null;
    }

    private static ShapeSelector ParseShapeSelector(string selector)
    {
        string? elementType = null;
        int? slideNum = null;
        string? textContains = null;
        string? fontEquals = null;
        string? fontNotEquals = null;
        bool? isTitle = null;
        bool? hasAlt = null;

        // Check for slide prefix
        var slideMatch = Regex.Match(selector, @"slide\[(\d+)\]\s*(.*)");
        if (slideMatch.Success)
        {
            slideNum = int.Parse(slideMatch.Groups[1].Value);
            // CONSISTENCY(query-slide-prefix): strip '>', '/', or ' ' separators
            // so both "slide[1]>ole" and "/slide[1]/ole" resolve the element type.
            selector = slideMatch.Groups[2].Value.TrimStart('>', '/', ' ');
        }
        else
        {
            // CONSISTENCY(query-slide-prefix): also accept unindexed `slide > shape`
            // as "match this child type across all slides" — Word supports child
            // combinators without a specific parent index, so PPTX should too.
            var unindexedSlideMatch = Regex.Match(selector, @"^\s*slide\s*>\s*(.+)$", RegexOptions.IgnoreCase);
            if (unindexedSlideMatch.Success)
                selector = unindexedSlideMatch.Groups[1].Value;
            else
            {
                // CSS descendant combinator `slide chart` — subject is the
                // right-hand element, ancestor on the left is advisory.
                // Without this, the type-match loop below ate the leading
                // "slide" and returned slide nodes, silently swallowing the
                // chart subject.
                var unindexedDescendantMatch = Regex.Match(selector, @"^\s*slide\s+(\w[\w\[\]=@'""]*.*)$", RegexOptions.IgnoreCase);
                if (unindexedDescendantMatch.Success)
                    selector = unindexedDescendantMatch.Groups[1].Value;
            }
        }

        // Strip any remaining combinator prefixes like "table > " so that
        // "slide > table > tr" (after slide> is stripped above) resolves to "tr".
        // PPTX has at most two nesting levels relevant to query (slide > X > Y),
        // and the engine always queries globally — the ancestor prefix is advisory.
        var remainingCombinator = Regex.Match(selector, @"^\s*\w[\w\[\]=@'""\s]*\s*>\s*(.+)$");
        if (remainingCombinator.Success)
            selector = remainingCombinator.Groups[1].Value.Trim();

        // Element type
        var typeMatch = Regex.Match(selector, @"^(\w+)");
        if (typeMatch.Success)
        {
            var t = typeMatch.Groups[1].Value.ToLowerInvariant();
            if (t is "shape" or "textbox" or "title" or "picture" or "pic"
                or "video" or "audio"
                or "equation" or "math" or "formula"
                or "table" or "chart" or "placeholder"
                or "connector" or "connection"
                or "group" or "notes"
                or "zoom" or "slidezoom"
                or "tr" or "row" or "tc" or "cell")
                elementType = t;
        }

        // Attributes
        Dictionary<string, (string Value, bool Negate)>? genericAttrs = null;
        foreach (Match attrMatch in Regex.Matches(selector, @"\[(\w+)(~=|\\?!?=)([^\]]*)\]"))
        {
            var key = attrMatch.Groups[1].Value.ToLowerInvariant();
            var op = attrMatch.Groups[2].Value.Replace("\\", "");
            var rawVal = attrMatch.Groups[3].Value;
            // CONSISTENCY(find-regex): preserve the surrounding quotes when the
            // value is the `r"..."` / `r'...'` regex form so MatchesGenericAttributes
            // can detect it. Otherwise Trim eats the quotes and the regex prefix
            // is indistinguishable from a literal beginning with 'r'.
            var isRegexForm = rawVal.Length >= 3 && rawVal[0] == 'r'
                && (rawVal[1] == '"' || rawVal[1] == '\'');
            var val = isRegexForm ? rawVal : rawVal.Trim('\'', '"');

            switch (key)
            {
                case "font" when op == "=": fontEquals = val; break;
                case "font" when op == "!=": fontNotEquals = val; break;
                case "title":
                case "istitle":
                    isTitle = val.ToLowerInvariant() != "false"; break;
                case "alt": hasAlt = !string.IsNullOrEmpty(val) && val.ToLowerInvariant() != "false"; break;
                default:
                    genericAttrs ??= new Dictionary<string, (string, bool)>();
                    if (op == "~=")
                    {
                        // ~= is a "contains" match — store with special prefix
                        // Also handled by AttributeFilter post-filter (idempotent)
                        genericAttrs[key] = ($"\x01~={val}", false);
                    }
                    else
                    {
                        genericAttrs[key] = (val, op == "!=");
                    }
                    break;
            }
        }

        // :contains()
        var containsMatch = Regex.Match(selector, @":contains\(['""]?(.+?)['""]?\)");
        if (containsMatch.Success) textContains = containsMatch.Groups[1].Value;

        // Shorthand: "shape:text" → treat as :contains(text)
        if (textContains == null)
        {
            var shorthandMatch = Regex.Match(selector, @"^(?:\w+)?:(?!contains|empty|no-alt|has)(.+)$");
            if (shorthandMatch.Success) textContains = shorthandMatch.Groups[1].Value;
        }

        // Element type shortcuts
        if (elementType == "title") isTitle = true;

        // :no-alt
        if (selector.Contains(":no-alt")) hasAlt = false;

        return new ShapeSelector(elementType, slideNum, textContains, fontEquals, fontNotEquals, isTitle, hasAlt, genericAttrs);
    }

    private static bool MatchesShapeSelector(Shape shape, ShapeSelector selector)
    {
        // Element type filter
        if (selector.ElementType is "picture" or "pic" or "video" or "audio" or "table" or "chart"
            or "placeholder" or "connector" or "connection" or "group" or "notes" or "zoom"
            or "tr" or "row" or "tc" or "cell")
            return false;

        // BUG-BT-R33-1: `query textbox` previously matched every shape including
        // title placeholders. Title shapes are surfaced via the dedicated
        // `query title` selector (IsTitle=true); textbox should only match
        // non-title shapes for symmetry.
        if (selector.ElementType == "textbox" && IsTitle(shape)) return false;

        // Title filter
        if (selector.IsTitle.HasValue)
        {
            if (selector.IsTitle.Value != IsTitle(shape)) return false;
        }

        // Text contains
        if (selector.TextContains != null)
        {
            var text = GetShapeText(shape);
            if (!text.Contains(selector.TextContains, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Font filter
        var runs = shape.Descendants<Drawing.Run>().ToList();
        if (selector.FontEquals != null)
        {
            bool found = runs.Any(r =>
            {
                var font = r.RunProperties?.GetFirstChild<Drawing.LatinFont>()?.Typeface?.Value
                    ?? r.RunProperties?.GetFirstChild<Drawing.EastAsianFont>()?.Typeface?.Value;
                return font != null && string.Equals(font, selector.FontEquals, StringComparison.OrdinalIgnoreCase);
            });
            if (!found) return false;
        }

        if (selector.FontNotEquals != null)
        {
            bool hasWrongFont = runs.Any(r =>
            {
                var font = r.RunProperties?.GetFirstChild<Drawing.LatinFont>()?.Typeface?.Value
                    ?? r.RunProperties?.GetFirstChild<Drawing.EastAsianFont>()?.Typeface?.Value;
                return font != null && !string.Equals(font, selector.FontNotEquals, StringComparison.OrdinalIgnoreCase);
            });
            if (!hasWrongFont) return false;
        }

        return true;
    }

    private static bool MatchesGenericAttributes(DocumentNode node, Dictionary<string, (string Value, bool Negate)>? attributes)
    {
        if (attributes == null || attributes.Count == 0) return true;

        foreach (var (key, (expected, negate)) in attributes)
        {
            // Special case: "text" attribute matches node.Text, not Format["text"]
            var isTextKey = string.Equals(key, "text", StringComparison.OrdinalIgnoreCase);
            var matchedKey = node.Format.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            var hasKey = matchedKey != null || (isTextKey && node.Text != null);
            object? actual = matchedKey != null ? node.Format[matchedKey!] : (isTextKey ? node.Text : null);
            var actualStr = actual?.ToString() ?? "";

            // Handle ~= (contains) operator
            if (expected.StartsWith("\x01~="))
            {
                var pattern = expected[3..]; // strip "\x01~="
                if (!hasKey) return false;
                // CONSISTENCY(find-regex): mirror Set's `r"..."` regex form so
                // `query run[text~=r"Bold"]` matches the same elements as plain
                // `~=Bold` for literals, and supports anchors/wildcards beyond
                // that. Falls back to literal contains on bad regex so callers
                // never see an opaque parse exception from deep in query.
                if (pattern.Length >= 3 && pattern[0] == 'r'
                    && (pattern[1] == '"' || pattern[1] == '\''))
                {
                    var quote = pattern[1];
                    var endIdx = pattern.LastIndexOf(quote);
                    if (endIdx > 1)
                    {
                        var rx = pattern[2..endIdx];
                        try
                        {
                            if (!System.Text.RegularExpressions.Regex.IsMatch(actualStr, rx,
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                return false;
                            continue;
                        }
                        catch (ArgumentException)
                        {
                            // Malformed regex — fall back to literal contains below.
                        }
                    }
                }
                if (!actualStr.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return false;
                continue;
            }

            var isNameKey = string.Equals(key, "name", StringComparison.OrdinalIgnoreCase);

            if (negate)
            {
                // [attr!=value]: must not equal
                var matches = isNameKey ? MatchesShapeName(actualStr, expected) : NormalizedEquals(actualStr, expected);
                if (hasKey && matches)
                    return false;
            }
            else
            {
                // [attr=value]: must exist and equal.
                // CONSISTENCY(attr-miss-warn): when the key is absent, return
                // true here (pass-through) so AttributeFilter.ApplyWithWarnings
                // sees nodes.Count > 0 and emits the "filter key not found"
                // warning. The CLI/Resident/MCP all post-filter through
                // AttributeFilter which then rejects the node via MatchOne
                // (!hasKey → false), so the final result set is identical.
                // Direct Query() consumers don't use attribute filters; the
                // pre-filter here was an optimisation, not a correctness gate.
                // Without this pass-through, [foo=bar] silently returned empty
                // with no warning, while the symmetric [foo] presence form
                // warned because it parsed via the AttributeFilter path only.
                if (!hasKey) continue;
                var matches = isNameKey ? MatchesShapeName(actualStr, expected) : NormalizedEquals(actualStr, expected);
                if (!matches)
                {
                    // Special case: boolean properties stored as `true`/`True` matching "true"
                    if (actual is bool b && string.Equals(expected, b.ToString(), StringComparison.OrdinalIgnoreCase))
                        continue;
                    // Special case: dimension values with different units (e.g., "0.07cm" vs "2pt").
                    // CONSISTENCY(dim-only): only apply when BOTH sides look like a
                    // unit-qualified dimension (carry a non-digit unit suffix like
                    // cm/in/pt/mm/px/emu). Without this guard, bare integers like
                    // "2" / "3" got parsed as raw EMU and then matched within the
                    // 500-EMU tolerance — which silently made `[zorder=2]` match
                    // `zorder=3` (and similar for any small-integer attribute).
                    if (LooksLikeDimension(actualStr) && LooksLikeDimension(expected)
                        && Core.EmuConverter.TryParseEmu(actualStr, out var actualEmu)
                        && Core.EmuConverter.TryParseEmu(expected, out var expectedEmu)
                        && Math.Abs(actualEmu - expectedEmu) <= 500)
                        continue;
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// True when the string carries an explicit OOXML/CSS-style length unit suffix.
    /// Used to gate the dimension-tolerance fallback in MatchesGenericAttributes so
    /// bare integer attributes (zorder, id, count, ...) don't get cross-matched
    /// through EMU-with-tolerance comparison.
    /// </summary>
    private static bool LooksLikeDimension(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        return s.EndsWith("cm", StringComparison.OrdinalIgnoreCase)
            || s.EndsWith("mm", StringComparison.OrdinalIgnoreCase)
            || s.EndsWith("in", StringComparison.OrdinalIgnoreCase)
            || s.EndsWith("pt", StringComparison.OrdinalIgnoreCase)
            || s.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            || s.EndsWith("emu", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Case-insensitive comparison that also normalizes '#' prefix for color hex values.
    /// "#FF0000" equals "FF0000" and vice versa.
    /// </summary>
    private static bool NormalizedEquals(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;
        var aNorm = a.TrimStart('#');
        var bNorm = b.TrimStart('#');
        if (aNorm != a || bNorm != b)
            return string.Equals(aNorm, bNorm, StringComparison.OrdinalIgnoreCase);
        return false;
    }

    /// <summary>
    /// Match shape name with !! morph prefix awareness.
    /// "my-box" matches both "my-box" and "!!my-box".
    /// "!!my-box" matches both "!!my-box" and "my-box".
    /// </summary>
    private static bool MatchesShapeName(string? actual, string expected)
    {
        if (actual == null) return false;
        if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            return true;
        // Strip !! prefix from actual name and compare
        if (actual.StartsWith("!!") && string.Equals(actual[2..], expected, StringComparison.OrdinalIgnoreCase))
            return true;
        // Strip !! prefix from expected and compare
        if (expected.StartsWith("!!") && string.Equals(actual, expected[2..], StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static bool MatchesPictureSelector(Picture pic, ShapeSelector selector)
    {
        // Only match if looking for pictures/video/audio or no type specified
        if (selector.ElementType != null &&
            selector.ElementType is not ("picture" or "pic" or "video" or "audio"))
            return false;

        if (selector.IsTitle.HasValue) return false; // Pictures can't be titles

        // Alt text filter
        if (selector.HasAlt.HasValue)
        {
            var alt = pic.NonVisualPictureProperties?.NonVisualDrawingProperties?.Description?.Value;
            bool hasAlt = !string.IsNullOrEmpty(alt);
            if (selector.HasAlt.Value != hasAlt) return false;
        }

        return true;
    }
}
