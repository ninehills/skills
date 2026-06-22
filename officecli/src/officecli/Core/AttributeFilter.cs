// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text.RegularExpressions;

namespace OfficeCli.Core;

/// <summary>
/// Parses CSS-like attribute filters from query selectors and matches them against DocumentNode.
/// Supports operators: = (exact), != (not equal), ~= (contains), >= (greater or equal), <= (less or equal).
/// Example: "shape[fill=#FF0000][size>=24pt][text~=报告]"
/// </summary>
internal static class AttributeFilter
{
    public enum FilterOp { Equal, NotEqual, Contains, GreaterOrEqual, LessOrEqual, GreaterThan, LessThan, Exists }

    public record Condition(string Key, FilterOp Op, string Value);

    // Regex: [key op value] where op is ~=, >=, <=, !=, =, >, or <.
    // The leading '@' is an optional XPath-style attribute prefix accepted
    // for round-trip parity with Get/Add output (e.g. `/slide[1]/shape[@id=10000]`
    // pastes back into query unchanged). Stripped from the captured key
    // group via the non-capturing prefix.
    // Order matters: multi-char operators before single-char to avoid partial match
    private static readonly Regex AttrRegex = new(
        @"\[@?([\w.]+)\s*(~=|>=|<=|\\?!=|=|>|<)\s*([^\]]*)\]",
        RegexOptions.Compiled);

    // Regex: [key] (has-attribute, no operator). Optional '@' prefix mirrors AttrRegex.
    private static readonly Regex HasAttrRegex = new(
        @"\[@?([\w.]+)\]",
        RegexOptions.Compiled);

    // Regex to find any [...] block (for validation)
    private static readonly Regex BracketBlockRegex = new(
        @"\[([^\]]*)\]",
        RegexOptions.Compiled);

    // Regex: numeric positional index [N] only (used for reverse-doc-order keys).
    private static readonly Regex BracketIndexRegex = new(
        @"\[(\d+)\]",
        RegexOptions.Compiled);

    /// <summary>
    /// Parse all [key op value] conditions from a selector string.
    /// Throws CliException for malformed selectors.
    /// </summary>
    public static List<Condition> Parse(string selector)
    {
        // Check for unclosed brackets
        var openCount = selector.Count(c => c == '[');
        var closeCount = selector.Count(c => c == ']');
        if (openCount != closeCount)
            throw new CliException($"Malformed selector: unclosed bracket in \"{selector}\"")
            {
                Code = "invalid_selector",
                Suggestion = "Ensure every '[' has a matching ']'. Example: paragraph[style=Heading 1]"
            };

        var conditions = new List<Condition>();
        var matchedSpans = new HashSet<(int Start, int End)>();

        foreach (Match m in AttrRegex.Matches(selector))
        {
            var key = m.Groups[1].Value;
            var opStr = m.Groups[2].Value.Replace("\\", "");
            var rawVal = m.Groups[3].Value;
            // CONSISTENCY(find-regex): preserve quotes when the value is the
            // `r"..."` / `r'...'` regex form so MatchOne can detect it. Trim
            // would otherwise eat the surrounding quote that marks the prefix.
            var isRegexForm = rawVal.Length >= 3 && rawVal[0] == 'r'
                && (rawVal[1] == '"' || rawVal[1] == '\'');
            var val = isRegexForm ? rawVal : rawVal.Trim('\'', '"');

            // Detect corrupted values from mis-parsed operators (e.g. === parsed as = with value ==X)
            if (val.StartsWith("=") || val.StartsWith("~") || val.StartsWith("!"))
                throw new CliException($"Malformed selector: invalid operator in \"[{m.Groups[0].Value.Trim('[', ']')}]\". Supported operators: =, !=, ~=, >=, <=, >, <")
                {
                    Code = "invalid_selector",
                    Suggestion = $"Did you mean [{key}={val.TrimStart('=', '~', '!')}]?"
                };

            var op = opStr switch
            {
                "~=" => FilterOp.Contains,
                ">=" => FilterOp.GreaterOrEqual,
                "<=" => FilterOp.LessOrEqual,
                ">" => FilterOp.GreaterThan,
                "<" => FilterOp.LessThan,
                "!=" => FilterOp.NotEqual,
                _ => FilterOp.Equal
            };

            // BUG-R10-01: wildcard '*' in attribute value silently returned 0
            // matches. Users tried e.g. `ole[progId=Excel*]` expecting a
            // contains-like match. Fail fast with a clear error pointing to
            // the right operator rather than quietly mis-filtering.
            if (val.Contains('*'))
                throw new CliException(
                    $"Wildcards (*) are not supported in attribute filters. " +
                    $"Use ~= for contains, e.g. {key}~={val.Trim('*')}.")
                {
                    Code = "invalid_selector",
                    Suggestion = $"Did you mean [{key}~={val.Trim('*')}]?"
                };

            conditions.Add(new Condition(key, op, val));
            matchedSpans.Add((m.Index, m.Index + m.Length));
        }

        // Find [...] blocks that weren't matched by the key=value regex
        foreach (Match block in BracketBlockRegex.Matches(selector))
        {
            if (matchedSpans.Any(s => s.Start == block.Index)) continue;
            var content = block.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(content))
                throw new CliException($"Malformed selector: empty brackets \"[]\" in \"{selector}\"")
                {
                    Code = "invalid_selector",
                    Suggestion = "Use [key=value] or [key] syntax. Example: paragraph[style=Heading 1]"
                };
            // Index like [1] — valid path syntax, skip
            if (int.TryParse(content, out _)) continue;
            // [key] with no operator — "has attribute" filter (CSS [attr] syntax)
            var hasAttrMatch = HasAttrRegex.Match(block.Value);
            if (hasAttrMatch.Success)
            {
                conditions.Add(new Condition(hasAttrMatch.Groups[1].Value, FilterOp.Exists, ""));
                matchedSpans.Add((block.Index, block.Index + block.Length));
                continue;
            }
            // Unrecognized bracket content
            throw new CliException($"Malformed selector: cannot parse \"[{content}]\". Expected [key=value] with operator =, !=, ~=, >=, <=, >, or <")
            {
                Code = "invalid_selector",
                Suggestion = "Example: paragraph[style=Heading 1], shape[fill!=#FF0000], cell[formula]"
            };
        }

        return conditions;
    }

    /// <summary>
    /// Filter a list of DocumentNodes by the given conditions.
    /// All operators (=, !=, ~=, >=, <=) are applied as a post-filter.
    /// This is safe even when handler selectors already pre-filter = and !=,
    /// since filtering is idempotent.
    /// </summary>
    public static List<DocumentNode> Apply(List<DocumentNode> nodes, List<Condition> conditions, bool applyAll = true)
    {
        if (conditions.Count == 0) return nodes;

        var toApply = applyAll
            ? conditions
            : conditions.Where(c => c.Op is FilterOp.Contains or FilterOp.GreaterOrEqual or FilterOp.LessOrEqual or FilterOp.GreaterThan or FilterOp.LessThan or FilterOp.Exists).ToList();

        if (toApply.Count == 0) return nodes;

        return nodes.Where(n => MatchAll(n, toApply)).ToList();
    }

    /// <summary>
    /// Filter nodes and collect diagnostic warnings.
    /// Warns when: a filter key doesn't exist in ANY node's Format,
    /// or when >= / <= / > / < is used on a non-numeric value.
    /// </summary>
    /// <summary>
    /// Rewrite conditions' keys through <paramref name="keyResolver"/>. Used so
    /// handler-level alias maps (e.g. Excel cell: bold -> font.bold) also apply
    /// when AttributeFilter post-filters against DocumentNode.Format in the CLI
    /// query pipeline.
    /// </summary>
    public static List<Condition> NormalizeKeys(List<Condition> conditions, Func<string, string> keyResolver)
    {
        if (conditions.Count == 0) return conditions;
        return conditions.Select(c => new Condition(keyResolver(c.Key), c.Op, c.Value)).ToList();
    }

    public static (List<DocumentNode> Results, List<string> Warnings) ApplyWithWarnings(
        List<DocumentNode> nodes, List<Condition> conditions, bool applyAll = true)
    {
        var warnings = new List<string>();
        if (conditions.Count == 0) return (nodes, warnings);

        var toApply = applyAll
            ? conditions
            : conditions.Where(c => c.Op is FilterOp.Contains or FilterOp.GreaterOrEqual or FilterOp.LessOrEqual or FilterOp.GreaterThan or FilterOp.LessThan or FilterOp.Exists).ToList();

        if (toApply.Count == 0) return (nodes, warnings);

        // Check for missing keys: if a filter key doesn't exist in ANY node, warn
        foreach (var cond in toApply)
        {
            if (cond.Op == FilterOp.NotEqual) continue; // missing key is valid for !=
            bool anyHasKey = nodes.Any(n => ResolveValue(n, cond.Key).HasKey);
            if (!anyHasKey && nodes.Count > 0)
            {
                warnings.Add($"Warning: filter key '{cond.Key}' not found in any result's Format. " +
                    $"Available keys: {string.Join(", ", GetAllFormatKeys(nodes))}");
            }
        }

        // Check for non-numeric values on >= / <= / > / <
        foreach (var cond in toApply.Where(c => c.Op is FilterOp.GreaterOrEqual or FilterOp.LessOrEqual or FilterOp.GreaterThan or FilterOp.LessThan))
        {
            if (ExtractNumber(cond.Value) == null && !EmuConverter.TryParseEmu(cond.Value, out _))
            {
                warnings.Add($"Warning: '{cond.Value}' in [{cond.Key}{OpToString(cond.Op)}{cond.Value}] " +
                    $"is not numeric — comparison may produce unexpected results");
            }
            // Also check actual values in nodes
            foreach (var node in nodes)
            {
                var (hasKey, actual) = ResolveValue(node, cond.Key);
                if (hasKey && ExtractNumber(actual) == null && !EmuConverter.TryParseEmu(actual, out _))
                {
                    warnings.Add($"Warning: value '{actual}' for key '{cond.Key}' at {node.Path} " +
                        $"is not numeric — {OpToString(cond.Op)} comparison may be unreliable");
                    break; // one warning per condition is enough
                }
            }
        }

        var results = nodes.Where(n => MatchAll(n, toApply)).ToList();
        return (results, warnings);
    }

    private static string OpToString(FilterOp op) => op switch
    {
        FilterOp.Equal => "=",
        FilterOp.NotEqual => "!=",
        FilterOp.Contains => "~=",
        FilterOp.GreaterOrEqual => ">=",
        FilterOp.LessOrEqual => "<=",
        FilterOp.GreaterThan => ">",
        FilterOp.LessThan => "<",
        FilterOp.Exists => "(exists)",
        _ => "?"
    };

    private static HashSet<string> GetAllFormatKeys(List<DocumentNode> nodes)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            foreach (var key in node.Format.Keys)
                keys.Add(key);
            if (node.Text != null) keys.Add("text");
            if (!string.IsNullOrEmpty(node.Type)) keys.Add("type");
        }
        return keys;
    }

    /// <summary>
    /// Check if a DocumentNode matches all conditions.
    /// </summary>
    public static bool MatchAll(DocumentNode node, List<Condition> conditions)
    {
        foreach (var cond in conditions)
        {
            if (!MatchOne(node, cond)) return false;
        }
        return true;
    }

    /// <summary>
    /// CONSISTENCY(find-regex): shared text-match used by both the `~=` Contains
    /// operator and the CLI `--find` post-filter. Mirrors Word/Pptx Set's
    /// `r"..."` / `r'...'` regex prefix — without it, `--find r"Bullet"`
    /// literally looked for the string `r"Bullet"` (quotes included) and always
    /// returned 0. A plain (non-prefixed) value still does a case-insensitive
    /// contains; a malformed regex falls back to literal contains.
    /// </summary>
    public static bool MatchesTextFilter(string text, string find)
    {
        if (TryParseRegexPrefix(find, out var pattern))
        {
            try
            {
                // CONSISTENCY(find-regex): bound matching with a hard timeout so
                // catastrophic-backtracking patterns (e.g. r"(a+)+$") against
                // attacker-influenced document text fail fast instead of hanging
                // the process. Mirrors FindHelpers.FindMatchRanges.
                return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase, DocumentLimits.RegexMatchTimeout);
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
            {
                // Pathological pattern — treat as no-match rather than hanging.
                return false;
            }
            catch (System.ArgumentException)
            {
                // Malformed regex — fall through to literal contains so the
                // user still gets usable behavior, never an opaque exception.
            }
        }
        return text.Contains(find, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// CONSISTENCY(find-regex): canonical parser for the `r"..."` / `r'...'`
    /// raw-string regex prefix shared by every find vocabulary (query `~=`,
    /// CLI `--find`, Word/Pptx/Excel Set find/replace). Returns true and the
    /// inner pattern when <paramref name="find"/> is r-prefixed; false for a
    /// plain literal. Centralizing the parse here keeps one source of truth —
    /// do not re-implement the prefix scan anywhere else.
    /// </summary>
    public static bool TryParseRegexPrefix(string find, out string pattern)
    {
        pattern = "";
        if (find.Length >= 3 && find[0] == 'r'
            && (find[1] == '"' || find[1] == '\''))
        {
            var quote = find[1];
            var endIdx = find.LastIndexOf(quote);
            if (endIdx > 1)
            {
                pattern = find[2..endIdx];
                return true;
            }
        }
        return false;
    }

    private static bool MatchOne(DocumentNode node, Condition cond)
    {
        // Resolve actual value from node
        var (hasKey, actualStr) = ResolveValue(node, cond.Key);

        // CONSISTENCY(style-dual-key): paragraph `style` has two surfacings —
        // OOXML styleId (Format["style"]/["styleId"], e.g. "H5") and the
        // user-facing display name (node.Style/Format["styleName"], e.g.
        // "H正文"). The Word handler-level selector matches either; the CLI
        // post-filter must mirror that, otherwise `[style=H正文]` returns the
        // 3 handler-matched paragraphs only to have the post-filter drop them
        // because Format["style"] holds the styleId. styleId= / styleName=
        // are precise keys with no fallback.
        if ((cond.Op == FilterOp.Equal || cond.Op == FilterOp.NotEqual)
            && string.Equals(cond.Key, "style", StringComparison.OrdinalIgnoreCase))
        {
            bool dualHit = StringEquals(node.Style ?? "", cond.Value)
                || (node.Format.TryGetValue("style", out var sid) && StringEquals(sid?.ToString() ?? "", cond.Value))
                || (node.Format.TryGetValue("styleName", out var sname) && StringEquals(sname?.ToString() ?? "", cond.Value));
            return cond.Op == FilterOp.Equal ? dualHit : !dualHit;
        }

        switch (cond.Op)
        {
            case FilterOp.Exists:
                return hasKey && !string.IsNullOrEmpty(actualStr);

            case FilterOp.Equal:
                if (!hasKey) return false;
                return StringEquals(actualStr, cond.Value)
                    || DimensionEquals(actualStr, cond.Value);

            case FilterOp.NotEqual:
                if (!hasKey) return true; // key absent → not equal
                return !StringEquals(actualStr, cond.Value)
                    && !DimensionEquals(actualStr, cond.Value);

            case FilterOp.Contains:
                if (!hasKey) return false;
                return MatchesTextFilter(actualStr, cond.Value);

            case FilterOp.GreaterOrEqual:
                if (!hasKey) return false;
                return CompareNumeric(actualStr, cond.Value) is int ge && ge >= 0;

            case FilterOp.LessOrEqual:
                if (!hasKey) return false;
                return CompareNumeric(actualStr, cond.Value) is int le && le <= 0;

            case FilterOp.GreaterThan:
                if (!hasKey) return false;
                return CompareNumeric(actualStr, cond.Value) is int gt && gt > 0;

            case FilterOp.LessThan:
                if (!hasKey) return false;
                return CompareNumeric(actualStr, cond.Value) is int lt && lt < 0;

            default:
                return true;
        }
    }

    private static (bool HasKey, string Value) ResolveValue(DocumentNode node, string key)
    {
        // Case-insensitive Format key lookup (highest priority)
        var matchedKey = node.Format.Keys.FirstOrDefault(k =>
            string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

        if (matchedKey != null)
        {
            var val = node.Format[matchedKey];
            return (true, val?.ToString() ?? "");
        }

        // "text" falls back to node.Text if not in Format
        if (string.Equals(key, "text", StringComparison.OrdinalIgnoreCase))
        {
            return (node.Text != null, node.Text ?? "");
        }

        // "type" falls back to node.Type if not in Format
        if (string.Equals(key, "type", StringComparison.OrdinalIgnoreCase))
        {
            return (!string.IsNullOrEmpty(node.Type), node.Type ?? "");
        }

        // Excel cells expose their displayed value as node.Text, not
        // Format["value"]. Map the user-facing `value` filter key onto Text so
        // numeric/equality post-filters (e.g. cell[value>100]) resolve. Gated on
        // the cell node type so a future `value` Format key on other node kinds
        // is not shadowed.
        if (string.Equals(key, "value", StringComparison.OrdinalIgnoreCase)
            && string.Equals(node.Type, "cell", StringComparison.OrdinalIgnoreCase))
        {
            return (node.Text != null, node.Text ?? "");
        }

        // BUG-BT-R6-01: "style" falls back to node.Style if not in Format.
        // Word/PPT handlers populate the top-level DocumentNode.Style property
        // (serialized as the top-level "style" key in JSON output) but do NOT
        // duplicate it into Format. Without this fallback, query selectors
        // like `paragraph[style=Normal]` returned 0 results even though every
        // paragraph in the document literally had style="Normal".
        if (string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
        {
            return (!string.IsNullOrEmpty(node.Style), node.Style ?? "");
        }

        return (false, "");
    }

    private static bool StringEquals(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;
        // Normalize color hex: "#FF0000" matches "FF0000" and vice versa
        var aNorm = a.TrimStart('#');
        var bNorm = b.TrimStart('#');
        if (aNorm != a || bNorm != b)
            return string.Equals(aNorm, bNorm, StringComparison.OrdinalIgnoreCase);
        return false;
    }

    private static bool DimensionEquals(string actual, string expected)
    {
        // Fuzzy EMU equality (±500) is for unit-qualified dimensions so that
        // e.g. "2cm" matches a stored "2.0cm". It must NOT fire for unitless
        // numbers: EmuConverter parses a bare "50" as 50 EMU, so "50" and "150"
        // fall within the ±500 tolerance and would be judged equal — wrong for
        // cell values (cell[value!=150] then drops every cell). Require an
        // explicit unit on at least one side; bare numbers compare exactly via
        // StringEquals instead.
        if (string.IsNullOrEmpty(ExtractUnit(actual)) && string.IsNullOrEmpty(ExtractUnit(expected)))
            return false;
        if (EmuConverter.TryParseEmu(actual, out var a) && EmuConverter.TryParseEmu(expected, out var b))
            return Math.Abs(a - b) <= 500;
        return false;
    }

    /// <summary>
    /// Compare two values numerically. Supports:
    /// - Plain numbers: "24", "1.5"
    /// - pt-suffixed: "24pt", "10.5pt"
    /// - EMU/dimension values: "2cm", "1in"
    /// Returns negative if actual &lt; expected, 0 if equal, positive if actual &gt; expected.
    /// Returns <c>null</c> when the values are not both numerically/dimensionally
    /// comparable. The &gt;/&lt;/&gt;=/&lt;= operators treat null as "no match" so a
    /// numeric filter never matches non-numeric text via a string comparison —
    /// e.g. cell[value&gt;5000] must NOT match the text cell "张三" (whose code
    /// points would otherwise sort above "5000").
    /// </summary>
    private static int? CompareNumeric(string actual, string expected)
    {
        // Try plain decimal comparison (handles "24", "1.5", "24pt" vs "20pt", etc.)
        var actualNum = ExtractNumber(actual);
        var expectedNum = ExtractNumber(expected);

        if (actualNum.HasValue && expectedNum.HasValue)
        {
            // If both have the same unit suffix (or none), compare directly
            var actualUnit = ExtractUnit(actual);
            var expectedUnit = ExtractUnit(expected);
            if (string.Equals(actualUnit, expectedUnit, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(actualUnit) || string.IsNullOrEmpty(expectedUnit))
            {
                return actualNum.Value.CompareTo(expectedNum.Value);
            }
        }

        // Try EMU-based dimension comparison (handles mixed units: "2cm" vs "1in")
        if (EmuConverter.TryParseEmu(actual, out var actualEmu) && EmuConverter.TryParseEmu(expected, out var expectedEmu))
        {
            return actualEmu.CompareTo(expectedEmu);
        }

        // Fallback: plain number comparison (mixed units, both unitless numbers)
        if (actualNum.HasValue && expectedNum.HasValue)
            return actualNum.Value.CompareTo(expectedNum.Value);

        // Not numerically comparable — no string fallback. A numeric operator on
        // a non-numeric value is "no match", not a lexical comparison.
        return null;
    }

    private static decimal? ExtractNumber(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        // Strip known unit suffixes
        var trimmed = value.TrimEnd();
        foreach (var suffix in new[] { "pt", "px", "cm", "in", "em", "%" })
        {
            if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^suffix.Length];
                break;
            }
        }

        return decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static string ExtractUnit(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        foreach (var suffix in new[] { "pt", "px", "cm", "in", "em", "%" })
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return suffix;
        }
        return "";
    }

    // ==================== Boolean expression layer (and / or) ====================
    //
    // XPath-predicate-style booleans inside a bracket:
    //   cell[value>5000 and value<8000]
    //   cell[type=number or type=date]
    //   cell[(type=number or type=date) and value>0]
    // Precedence: and > or; parens override. Only `and` / `or` are reserved —
    // `not` is intentionally left free (no negation yet). Atoms are the existing
    // `key op value` predicates (same operators, same value grammar incl. r"...").
    // Inside ONE bracket, AND must be explicit (`and`); implicit AND stays the
    // stacked-bracket form [a][b]. A value containing a reserved word
    // (and/or/not), whitespace, or parens must be quoted: [text~="salt and pepper"].
    //
    // The flat List<Condition> path above is unchanged. ParseExpr is additive:
    // consumers parse to a FilterExpr, then TryFlatten lets a pure AND-of-
    // predicates fall back to the legacy List path byte-for-byte; only or/not/
    // grouped selectors use the tree evaluator.

    public abstract record FilterExpr;
    public sealed record PredicateExpr(Condition Cond) : FilterExpr;
    public sealed record AndExpr(IReadOnlyList<FilterExpr> Parts) : FilterExpr;
    public sealed record OrExpr(IReadOnlyList<FilterExpr> Parts) : FilterExpr;

    /// <summary>
    /// Parse a selector's bracket filters into one expression tree. Multiple
    /// top-level brackets are ANDed (stacking). A pure-numeric bracket ([2]) is a
    /// positional index, skipped here. Returns null when there is no filter
    /// bracket (match-all). Throws CliException on malformed input, mirroring Parse.
    /// </summary>
    public static FilterExpr? ParseExpr(string selector)
    {
        var openCount = selector.Count(c => c == '[');
        var closeCount = selector.Count(c => c == ']');
        if (openCount != closeCount)
            throw new CliException($"Malformed selector: unclosed bracket in \"{selector}\"")
            {
                Code = "invalid_selector",
                Suggestion = "Ensure every '[' has a matching ']'. Example: cell[value>5000 and value<8000]"
            };

        var parts = new List<FilterExpr>();
        foreach (Match block in BracketBlockRegex.Matches(selector))
        {
            var content = block.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(content))
                throw new CliException($"Malformed selector: empty brackets \"[]\" in \"{selector}\"")
                {
                    Code = "invalid_selector",
                    Suggestion = "Use [key=value] or a boolean expression. Example: cell[a and b]"
                };
            if (int.TryParse(content.Trim(), out _)) continue;   // [2] positional index
            parts.Add(new ExprParser(content).ParseTop());
        }
        if (parts.Count == 0) return null;
        return parts.Count == 1 ? parts[0] : new AndExpr(parts);
    }

    /// <summary>
    /// When the expression is a pure AND of predicates (i.e. equivalent to the
    /// legacy flat List&lt;Condition&gt;), return that list so callers can take the
    /// existing code path unchanged. Returns null when the tree contains or/not,
    /// which the flat list cannot represent.
    /// </summary>
    public static List<Condition>? TryFlatten(FilterExpr? expr)
    {
        if (expr == null) return new List<Condition>();
        var acc = new List<Condition>();
        return Flatten(expr, acc) ? acc : null;

        static bool Flatten(FilterExpr e, List<Condition> into)
        {
            switch (e)
            {
                case PredicateExpr p: into.Add(p.Cond); return true;
                case AndExpr a: return a.Parts.All(part => Flatten(part, into));
                default: return false;   // Or / Not are not flattenable
            }
        }
    }

    /// <summary>Rewrite every predicate key through <paramref name="keyResolver"/> (alias map).</summary>
    public static FilterExpr NormalizeKeysExpr(FilterExpr expr, Func<string, string> keyResolver) => expr switch
    {
        PredicateExpr p => new PredicateExpr(new Condition(keyResolver(p.Cond.Key), p.Cond.Op, p.Cond.Value)),
        AndExpr a => new AndExpr(a.Parts.Select(x => NormalizeKeysExpr(x, keyResolver)).ToList()),
        OrExpr o => new OrExpr(o.Parts.Select(x => NormalizeKeysExpr(x, keyResolver)).ToList()),
        _ => expr
    };

    /// <summary>Evaluate the expression tree against a node.</summary>
    public static bool MatchesExpr(DocumentNode node, FilterExpr? expr) => expr switch
    {
        null => true,
        PredicateExpr p => MatchOne(node, p.Cond),
        AndExpr a => a.Parts.All(x => MatchesExpr(node, x)),
        OrExpr o => o.Parts.Any(x => MatchesExpr(node, x)),
        _ => true
    };

    public static List<DocumentNode> ApplyExpr(List<DocumentNode> nodes, FilterExpr? expr)
        => expr == null ? nodes : nodes.Where(n => MatchesExpr(n, expr)).ToList();

    /// <summary>
    /// Like ApplyExpr but also collects the same diagnostic warnings
    /// ApplyWithWarnings emits (missing key, non-numeric comparison value) by
    /// walking the predicate leaves.
    /// </summary>
    public static (List<DocumentNode> Results, List<string> Warnings) ApplyExprWithWarnings(
        List<DocumentNode> nodes, FilterExpr? expr)
    {
        var warnings = new List<string>();
        if (expr == null) return (nodes, warnings);

        foreach (var cond in LeafConditions(expr))
        {
            if (cond.Op != FilterOp.NotEqual)
            {
                bool anyHasKey = nodes.Any(n => ResolveValue(n, cond.Key).HasKey);
                if (!anyHasKey && nodes.Count > 0)
                    warnings.Add($"Warning: filter key '{cond.Key}' not found in any result's Format. " +
                        $"Available keys: {string.Join(", ", GetAllFormatKeys(nodes))}");
            }
            if (cond.Op is FilterOp.GreaterOrEqual or FilterOp.LessOrEqual or FilterOp.GreaterThan or FilterOp.LessThan
                && ExtractNumber(cond.Value) == null && !EmuConverter.TryParseEmu(cond.Value, out _))
                warnings.Add($"Warning: '{cond.Value}' in [{cond.Key}{OpToString(cond.Op)}{cond.Value}] " +
                    $"is not numeric — comparison may produce unexpected results");
        }

        var results = nodes.Where(n => MatchesExpr(n, expr)).ToList();
        return (results, warnings);
    }

    /// <summary>
    /// Remove filter brackets so a boolean selector can be queried bare (the
    /// handler returns the full element set) and the expression applied to the
    /// result. A pure-numeric bracket ([2]) is a positional index, kept.
    /// </summary>
    public static string StripFilterBrackets(string selector)
        => BracketBlockRegex.Replace(selector, m =>
            int.TryParse(m.Groups[1].Value.Trim(), out _) ? m.Value : "");

    /// <summary>
    /// Unified selector filtering for query / set / remove. A pure-AND (flat)
    /// selector takes the exact legacy path: the handler pre-filters and the flat
    /// conditions are re-applied (idempotent). A selector containing `or` is
    /// queried with its filter brackets stripped — so the handler returns the full
    /// element set — and then narrowed by the expression tree, since the handler's
    /// own pre-filter cannot understand booleans. <paramref name="keyResolver"/>
    /// (when non-null) rewrites alias keys, e.g. cell bold → font.bold.
    /// </summary>
    /// <param name="applyAll">
    /// Governs the FLAT path only. true (query, Excel set) re-applies every
    /// condition. false (Word/Pptx set) re-applies only the comparison/contains/
    /// exists ops the handler selector drops, leaving = / != to the handler — so
    /// the handler's looser equality matching is preserved. The boolean path
    /// always applies all conditions: stripping the bracket means the handler did
    /// not pre-filter, so the tree must evaluate every predicate itself.
    /// </param>
    public static (List<DocumentNode> Results, List<string> Warnings) FilterSelector(
        string selector, Func<string, List<DocumentNode>> query, Func<string, string>? keyResolver = null,
        bool applyAll = true)
    {
        var expr = ParseExpr(selector);
        if (expr != null && keyResolver != null)
            expr = NormalizeKeysExpr(expr, keyResolver);

        if (TryFlatten(expr) is { } flat)
            return ApplyWithWarnings(query(selector), flat, applyAll);

        // Boolean path. Most elements are filtered by the generic engine on the
        // broad result, so strip the brackets and query bare. Elements that
        // resolve their OWN virtual attributes — Excel `row` table-column values
        // are attached by the handler's row-where, NOT present on a bare row —
        // must receive the full selector so the handler can resolve them; the tree
        // then re-confirms on the carried column values.
        var queryStr = ElementResolvesOwnBoolean(selector) ? selector : StripFilterBrackets(selector);
        return ApplyExprWithWarnings(query(queryStr), expr);
    }

    /// <summary>
    /// True when a selector PATH carries a content-filter predicate (attribute
    /// comparison / equality on a bare key / contains / exists, or an and·or
    /// expression) rather than pure structural addressing. Set dispatchers use
    /// this to route a `/`-prefixed path that filters by content (e.g.
    /// `/Sheet1/cell[value>5000 or value<300]`, `/body/p[1]/r[bold=true]`)
    /// through FilterSelector — the same engine query uses — instead of the
    /// positional-index path navigator that rejects non-index predicates.
    ///
    /// Structural addressing stays false: positional `[N]` / `[last()]`, and a
    /// single `[@attr=value]` equality (the locator form — `@paraId`, `@role`,
    /// `@id`, `@name`, `@author`, `@type`, …). Per-bracket rule:
    ///   `[N]` / `[last()]`                       → structural
    ///   and·or expression inside one bracket     → content
    ///   bare-token exists `[A]` / `[key]` (no op)→ structural (protects the
    ///                                              Excel `col[A]` column letter)
    ///   bare (non-`@`) key with an operator      → content
    ///   `@key` with a comparison op (&gt; &lt; &gt;= &lt;= ~= !=) → content
    ///   `@key=value` equality                    → structural (locator)
    /// CONSISTENCY(filter-path): `@key=value` equality is structural, so a
    /// forced-attr equality like `/Sheet1/row[@height=5]` is not hijacked — use
    /// the bare `row[@height=5]` form to equality-filter. Mirrors the project's
    /// "consistency &gt; robustness" precedent.
    /// </summary>
    public static bool IsContentFilterPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        foreach (Match block in BracketBlockRegex.Matches(path))
        {
            var content = block.Groups[1].Value.Trim();
            if (content.Length == 0) continue;
            if (int.TryParse(content, out _)) continue;                      // [N]
            if (content.Equals("last()", StringComparison.OrdinalIgnoreCase)) continue;

            FilterExpr expr;
            try { expr = new ExprParser(content).ParseTop(); }
            catch { continue; }   // malformed → let the structural navigator report it

            switch (expr)
            {
                case AndExpr:
                case OrExpr:
                    return true;                                             // boolean expression
                case PredicateExpr p:
                    if (p.Cond.Op == FilterOp.Exists) break;                // `[A]` / `[key]` bare token → structural (Excel col[A])
                    var atPrefixed = content.StartsWith("@", StringComparison.Ordinal);
                    if (!atPrefixed) return true;                            // bare-key operator filter
                    if (p.Cond.Op != FilterOp.Equal) return true;           // @key with comparison op
                    break;                                                   // @key=value equality → structural
            }
        }
        return false;
    }

    /// <summary>
    /// Sort key for index-shift-safe batch removal. A path's numeric `[N]`
    /// indices, zero-padded and joined left-to-right, so that
    /// <c>OrderByDescending(ReverseDocOrderKey)</c> yields reverse document order:
    /// the latest element is removed first, keeping every earlier index valid for
    /// the not-yet-removed targets (deleting `r[2]` before `r[3]` would otherwise
    /// renumber `r[3]`→`r[2]`). `@attr`/non-indexed segments contribute nothing —
    /// stable locators (`@id`, `@paraId`) don't shift. Used by the Word/Pptx
    /// selector-remove branches.
    /// </summary>
    public static string ReverseDocOrderKey(string? path) =>
        string.Join(".", BracketIndexRegex.Matches(path ?? "")
            .Select(m => m.Groups[1].Value.PadLeft(8, '0')));

    // True when the selector's element resolves its own virtual attributes that a
    // bare query would not carry (Excel row/col table-column predicates). Such a
    // selector must reach the handler with its brackets intact.
    private static bool ElementResolvesOwnBoolean(string selector)
    {
        var s = Regex.Replace((selector ?? "").TrimStart(), @"^(?:[^/!\[]+!|/[^/]+/)", "");
        return Regex.IsMatch(s, @"^(?:row|col|column)\[", RegexOptions.IgnoreCase);
    }

    /// <summary>Wrap a flat condition list as an expression (single predicate or AND).</summary>
    public static FilterExpr FromConditions(IReadOnlyList<Condition> conds)
        => conds.Count == 1
            ? new PredicateExpr(conds[0])
            : new AndExpr(conds.Select(c => (FilterExpr)new PredicateExpr(c)).ToList());

    public static IEnumerable<Condition> LeafConditions(FilterExpr expr) => expr switch
    {
        PredicateExpr p => new[] { p.Cond },
        AndExpr a => a.Parts.SelectMany(LeafConditions),
        OrExpr o => o.Parts.SelectMany(LeafConditions),
        _ => Enumerable.Empty<Condition>()
    };

    // Recursive-descent parser for one bracket's content. Grammar:
    //   expr   := or
    //   or     := and ( 'or' and )*
    //   and    := factor ( 'and' factor )*
    //   factor := '(' or ')' | predicate
    //   pred   := key op value
    private sealed class ExprParser
    {
        private readonly string _s;
        private int _i;
        public ExprParser(string content) { _s = content; _i = 0; }

        public FilterExpr ParseTop()
        {
            var e = ParseOr();
            SkipWs();
            if (_i < _s.Length) throw Err($"unexpected '{_s[_i..]}'");
            return e;
        }

        private FilterExpr ParseOr()
        {
            var parts = new List<FilterExpr> { ParseAnd() };
            while (TryKeyword("or")) parts.Add(ParseAnd());
            return parts.Count == 1 ? parts[0] : new OrExpr(parts);
        }

        private FilterExpr ParseAnd()
        {
            var parts = new List<FilterExpr> { ParseFactor() };
            while (TryKeyword("and")) parts.Add(ParseFactor());
            return parts.Count == 1 ? parts[0] : new AndExpr(parts);
        }

        private FilterExpr ParseFactor()
        {
            SkipWs();
            // Only `and` / `or` are reserved. `not` is intentionally NOT a keyword
            // — it parses as an ordinary value/identifier, leaving the word free
            // for a future negation design.
            if (Peek() == '(')
            {
                _i++;
                var inner = ParseOr();
                SkipWs();
                Expect(')');
                return inner;
            }
            return new PredicateExpr(ParsePredicate());
        }

        private Condition ParsePredicate()
        {
            SkipWs();
            int keyStart = _i;
            if (_i < _s.Length && _s[_i] == '@') _i++;
            while (_i < _s.Length && (char.IsLetterOrDigit(_s[_i]) || _s[_i] == '.' || _s[_i] == '_')) _i++;
            var key = _s[keyStart.._i];
            if (key.Length == 0 || key == "@")
                throw Err($"expected a predicate (key op value) at '{_s[_i..]}'");
            SkipWs();
            // Has-attribute form [key] with no operator → Exists, matching the flat
            // parser's CSS [attr] behavior. Detected when no operator char follows
            // (end of input, a ')', or the start of an and/or connective).
            if (_i >= _s.Length || _s[_i] == ')' || !IsOpStart(_s[_i]))
                return new Condition(key.TrimStart('@'), FilterOp.Exists, "");
            var op = ReadOp();
            var rawVal = ReadValue();
            return BuildCondition(key, op, rawVal);
        }

        private static bool IsOpStart(char c) => c is '>' or '<' or '=' or '!' or '~' or '\\';

        private string ReadOp()
        {
            // zsh-escaped != (the shell may pass \!= through); mirrors the flat
            // parser's \\?!= handling.
            if (_i + 3 <= _s.Length && _s[_i] == '\\' && _s[_i + 1] == '!' && _s[_i + 2] == '=')
            { _i += 3; return "!="; }
            foreach (var op in new[] { ">=", "<=", "!=", "~=", "=", ">", "<" })
                if (_i + op.Length <= _s.Length && _s.Substring(_i, op.Length) == op)
                {
                    _i += op.Length;
                    return op;
                }
            throw Err($"expected an operator (=, !=, ~=, >, <, >=, <=) at '{(_i < _s.Length ? _s[_i..] : "end")}'");
        }

        private string ReadValue()
        {
            SkipWs();
            if (_i < _s.Length)
            {
                char c = _s[_i];
                bool rPrefixed = c == 'r' && _i + 1 < _s.Length && (_s[_i + 1] == '"' || _s[_i + 1] == '\'');
                if (c == '"' || c == '\'' || rPrefixed)
                    return ReadQuoted(rPrefixed);
            }
            int start = _i;
            while (_i < _s.Length)
            {
                char c = _s[_i];
                if (c == ')') break;
                if (char.IsWhiteSpace(c) && ConnectiveAhead(_i)) break;
                _i++;
            }
            var v = _s[start.._i].TrimEnd();
            if (v.Length == 0) throw Err("expected a value after the operator");
            return v;
        }

        // Returns the raw token INCLUDING quotes (and any r-prefix) so
        // BuildCondition applies the same regex-form / trim logic as Parse.
        private string ReadQuoted(bool rPrefixed)
        {
            int start = _i;
            if (rPrefixed) _i++;                 // consume 'r'
            char quote = _s[_i];
            _i++;                                // consume opening quote
            while (_i < _s.Length && _s[_i] != quote) _i++;
            if (_i >= _s.Length) throw Err($"unterminated quoted value in '{_s[start..]}'");
            _i++;                                // consume closing quote
            return _s[start.._i];
        }

        // True when the whitespace at wsPos is followed by an `and`/`or` keyword
        // at a word boundary — the point where an unquoted value ends.
        private bool ConnectiveAhead(int wsPos)
        {
            int j = wsPos;
            while (j < _s.Length && char.IsWhiteSpace(_s[j])) j++;
            foreach (var kw in new[] { "and", "or" })
            {
                if (j + kw.Length <= _s.Length
                    && string.Compare(_s, j, kw, 0, kw.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    int after = j + kw.Length;
                    if (after >= _s.Length || char.IsWhiteSpace(_s[after]) || _s[after] == '(') return true;
                }
            }
            return false;
        }

        // Consume a keyword (and/or/not) at the cursor if present, word-bounded.
        private bool TryKeyword(string kw)
        {
            SkipWs();
            if (_i + kw.Length <= _s.Length
                && string.Compare(_s, _i, kw, 0, kw.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                int after = _i + kw.Length;
                // word boundary: next is end, whitespace, or '(' (for not()).
                if (after >= _s.Length || char.IsWhiteSpace(_s[after]) || _s[after] == '(')
                {
                    _i = after;
                    return true;
                }
            }
            return false;
        }

        private void SkipWs() { while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++; }
        private char Peek() { SkipWs(); return _i < _s.Length ? _s[_i] : '\0'; }
        private void Expect(char c)
        {
            SkipWs();
            if (_i >= _s.Length || _s[_i] != c) throw Err($"expected '{c}'");
            _i++;
        }
        private CliException Err(string what) => new($"Malformed filter expression: {what} in \"{_s}\"")
        {
            Code = "invalid_selector",
            Suggestion = "Use: key op value joined by and/or/not(...). Quote values with spaces or reserved words: [text~=\"a and b\"]."
        };
    }

    // Build a Condition from raw key/op/value with the SAME validation and value
    // handling as Parse (regex r"..." preservation, mis-parsed-operator guard,
    // wildcard rejection). Shared so the expression parser and the flat parser
    // agree on predicate semantics.
    private static Condition BuildCondition(string key, string opStr, string rawVal)
    {
        var isRegexForm = rawVal.Length >= 3 && rawVal[0] == 'r' && (rawVal[1] == '"' || rawVal[1] == '\'');
        var val = isRegexForm ? rawVal : rawVal.Trim('\'', '"');

        if (val.StartsWith("=") || val.StartsWith("~") || val.StartsWith("!"))
            throw new CliException($"Malformed selector: invalid operator near \"{key}{opStr}{val}\". Supported operators: =, !=, ~=, >=, <=, >, <")
            {
                Code = "invalid_selector",
                Suggestion = $"Did you mean [{key}={val.TrimStart('=', '~', '!')}]?"
            };
        if (val.Contains('*'))
            throw new CliException($"Wildcards (*) are not supported in attribute filters. Use ~= for contains, e.g. {key}~={val.Trim('*')}.")
            {
                Code = "invalid_selector",
                Suggestion = $"Did you mean [{key}~={val.Trim('*')}]?"
            };

        var op = opStr switch
        {
            "~=" => FilterOp.Contains,
            ">=" => FilterOp.GreaterOrEqual,
            "<=" => FilterOp.LessOrEqual,
            ">" => FilterOp.GreaterThan,
            "<" => FilterOp.LessThan,
            "!=" => FilterOp.NotEqual,
            _ => FilterOp.Equal
        };
        return new Condition(key.TrimStart('@'), op, val);
    }
}
