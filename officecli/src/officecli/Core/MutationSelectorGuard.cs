// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace OfficeCli.Core;

/// <summary>
/// Agent-safety guard for the MUTATING CLI/agent verbs (set, remove): reject a
/// bare, unscoped selector. A bare type selector — `cell`, `run`, `shape`,
/// `cell[value>5]`, `run[bold=true]` — matches across the ENTIRE document with
/// no scope, so one mistaken `set "cell"` rewrites every cell in every sheet and
/// `remove "run"` deletes every run. A mutation must name WHERE it applies:
///
///   • a `/`-scoped path  — `/Sheet1/cell[...]`, `/slide[1]/shape[...]`, `/body/p[1]/r[...]`
///   • Excel `Sheet!Ref`  — `Sheet1!A1`, `Sheet1!A1:B5`, `Sheet1!row[Amount>5000]`
///
/// `query` is intentionally NOT guarded: it is read-only and the bare type
/// selector is its primary, day-one (v1.0.0) discovery form. The guard lives at
/// the agent-facing layer (CLI / MCP / resident / batch) only — the handler
/// `Set`/`Remove` API stays permissive for internal recursion and programmatic
/// callers.
/// </summary>
public static class MutationSelectorGuard
{
    // Excel `Sheet!Ref` notation: a sheet name (no '/', '[' or ']') then '!'.
    // The char class stops at the first '[', so a bare filter whose VALUE happens
    // to contain '!' (e.g. `cell[value=foo!]`) does not match — only a real
    // sheet-separator '!' before any bracket counts as scoped.
    private static readonly Regex ExcelNotation = new(@"^[^/\[\]]+!", RegexOptions.Compiled);

    /// <summary>
    /// Throw a CliException when <paramref name="path"/> is a bare unscoped
    /// selector on a mutating verb. No-op for `/`-scoped paths, Excel `Sheet!Ref`
    /// notation, and null/empty (handled downstream).
    /// </summary>
    public static void EnsureScoped(string? path, string verb)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (path.StartsWith("/")) return;
        if (ExcelNotation.IsMatch(path)) return;

        throw new CliException(
            $"Bare selector '{path}' is not allowed for '{verb}' — it would match across the whole document.")
        {
            Code = "bare_selector_rejected",
            Suggestion =
                $"Scope the {verb} to a path: '/Sheet1/{path}' / '/slide[1]/{path}' / '/body/p[1]/{path}', " +
                "or use Excel notation 'Sheet1!A1'. Bare selectors stay available on read-only 'query'.",
        };
    }
}
