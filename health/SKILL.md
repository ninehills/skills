---
name: health
description: "Runs a budget-aware Agent Health audit for Codex, Claude Code, agent instructions, verifier surfaces, and AI maintainability when agents ignore instructions, hooks/MCP fail, validation is missing, or AI-written code is hard to maintain. Flags issues by severity. Not for debugging code or reviewing PRs."
when_to_use: "检查claude, 检查codex, Codex 配置, AGENTS.md, config.toml, agent instructions, 健康度, 配置检查, 配置对不对, AI coding 腐化, 代码变烂, 维护性, 上下文混乱, 验证缺失, 验证命令失真, Claude ignoring instructions, check config, settings not working, audit config"
dispatch_intent: "Codex/Claude ignoring instructions, agent config audit, hooks/MCP broken, health token usage, AI coding code rot, hotspot ownership, unclear context, missing verification, stale verifier output"
---

# Health: Agent Config and AI Maintainability

Prefix your first line with 🥷 inline, not as its own paragraph.

Audit the current project's agent setup and AI coding maintainability against this framework:
`agent config → instruction surfaces → tools/runtime → verifiers → maintainability`

Find violations. Identify the misaligned layer. Calibrate to project complexity only.

**Output language:** Check in order: (1) project agent instructions (`AGENTS.md` before runtime-specific files); (2) global agent instructions; (3) user's recent language; (4) English.

**Budget posture:** Start with the summary audit. Escalate automatically when the user asks for a deep, full, complete, thorough, "深入", "完整", "彻底", or "继续跑完" audit, when the user explicitly mentions AI coding code rot, Codex/Claude config drift, unclear context, missing verification, verifier output that points at stale paths, or "代码变烂", when current project instructions or remembered user preference says to run deep health checks by default, when the project is Complex, or when the summary pass exposes a critical ambiguity that cannot be resolved locally. Otherwise do not read full conversation extracts or launch inspector subagents. Tell the user before escalating because deep health audits can consume significant token quota.

## Durable Context Preflight

See [rules/durable-context.md](../../rules/durable-context.md) for when to read durable context, the read-order budget, and the memory-type mapping.

For `/health`, audit expectations are `decision`, `preference`, and `principle` entries; checks for repeated failures are `pattern` and `learning`. Current CLAUDE.md, installed skills, hooks, MCP config, command output, and live probes override memory. Also flag durable memory problems when they affect behavior: oversized injected summaries, stale or contradictory entries, missing project entrypoint references, or private paths copied into public instructions. Keep these as context findings, not code-review findings.

## Step 0: Assess project tier

Pick one. Apply only that tier's requirements.


| Tier         | Signal                                  | What's expected                                |
| ------------ | --------------------------------------- | ---------------------------------------------- |
| **Simple**   | <500 files, 1 contributor, no CI        | CLAUDE.md only; 0-1 skills; hooks optional     |
| **Standard** | 500-5K files, small team or CI          | CLAUDE.md + 1-2 rules; 2-4 skills; basic hooks |
| **Complex**  | >5K files, multi-contributor, active CI | Full six-layer setup required                  |


## Step 1: Collect data

Run the collection script in summary mode first. Do not interpret yet.

```bash
# Resolve collect-data.sh from canonical locations (no personal home-dir paths).
HEALTH_SCRIPT="${CLAUDE_SKILL_DIR:+$CLAUDE_SKILL_DIR/scripts/collect-data.sh}"
if [ ! -f "${HEALTH_SCRIPT:-}" ]; then
  for candidate in \
    "./skills/health/scripts/collect-data.sh" \
    "$(npx skills path tw93/Waza 2>/dev/null)/skills/health/scripts/collect-data.sh"; do
    [ -f "$candidate" ] && HEALTH_SCRIPT="$candidate" && break
  done
fi
if [ ! -f "${HEALTH_SCRIPT:-}" ]; then
  echo "health collect-data.sh not found; set CLAUDE_SKILL_DIR or reinstall: npx skills add tw93/Waza -a claude-code -g -y"
  exit 1
fi
bash "$HEALTH_SCRIPT"
```

Sections may show `(unavailable)` when tools are missing:

- `jq` missing → conversation sections unavailable
- `python3` missing → MCP/hooks/allowedTools sections unavailable
- `settings.local.json` absent → hooks/MCP may be unavailable (normal for global-only setups)

Treat `(unavailable)` as insufficient data, not a finding. Do not flag those areas.

The collector includes both runtime-specific and agent-agnostic surfaces:

- `AGENT CONFIG SUMMARY` / `AGENT CONFIG DETAIL` for Codex, Claude, and project instruction files.
- `AI MAINTAINABILITY SUMMARY` / `AI MAINTAINABILITY DETAIL` for project shape, verification surface, hotspot ownership, wrappers, and doc links.

## Step 1b: MCP Live Check

Test every MCP server: call one harmless tool per server. Record `live=yes/no` with error detail. Respect `enabled: false` (skip without flagging). For API keys, only check if the env var is set (`echo $VAR | head -c 5`), never print full keys.

## Step 2: Analyze

Confirm the tier. Then route:

- **Simple:** Analyze locally. No subagents.
- **Standard:** Analyze locally from the summary output. Do not launch subagents by default. If the user asks for a deep/full/thorough audit, or if local analysis cannot classify a security/control issue, escalate to deep mode and explain the likely token cost.
- **Complex, remembered deep preference, explicit deep audit, or explicit AI maintainability audit:** Re-run collection with `bash "$HEALTH_SCRIPT" auto deep`, then launch the relevant subagents in parallel. Redact credentials to `[REDACTED]`.
  - **Agent 1** (Context + Security): Read `agents/inspector-context.md`. Feed `CONVERSATION SIGNALS` section.
  - **Agent 2** (Control + Behavior): Read `agents/inspector-control.md`. Feed detected tier.
  - **Agent 3** (AI Maintainability): Read `agents/inspector-maintainability.md`. Feed only `TIER METRICS`, `AI MAINTAINABILITY SUMMARY` or `AI MAINTAINABILITY DETAIL`, and the script hotspot lists. Launch this agent only for deep health audits, Complex projects, or explicit code-rot/AI-maintainability requests.
- **Fallback:** If a subagent fails, analyze that layer locally and note "(analyzed locally)".

## Step 3: Report

**Health Report: {project} ({tier} tier, {file_count} files)**

### [PASS] Passing checks (table, max 5 rows)

### Finding format

```
- [severity] <symptom> ({file}:{line} if known)
  Why: <one-line reason>
  Action: <exact command or edit to fix>
```

`Action:` must be copy-pasteable. Never write "investigate X" or "consider Y". If the fix is unknown, name the diagnostic command.

### [!] Critical -- fix now

Rules violated, dangerous allowedTools, MCP overhead >12.5%, security findings, leaked credentials.

Example:

- [!] `settings.local.json` committed to git (exposes MCP tokens)
Why: leaked token enables remote code execution via installed MCP servers
Action: `git rm --cached .claude/settings.local.json && echo '.claude/settings.local.json' >> .gitignore`

### [~] Structural -- fix soon

Agent instructions in the wrong layer, missing hooks, oversized descriptions, verifier gaps.

**Codex/Claude instruction drift.** Use `AGENT CONFIG SUMMARY` first. Report a Structural finding when `AGENTS.md` and runtime-specific files both contain substantial guidance without delegation, when Codex `config.toml` lacks trust for the current project, when project agent instructions are missing, or when runtime-specific instructions contradict the shared project source of truth. Also report when important rules live only in ignored or private local instruction overlays but the tracked/public docs lack them; those overlays are private context, not durable project source of truth. Do not print raw config values. Secrets, tokens, keys, and passwords must appear only as `[REDACTED]`.

Quick check from the project root:

```bash
bash skills/health/scripts/check-agent-context.sh . summary
```

**AI-maintainability gaps.** Use `AI MAINTAINABILITY SUMMARY` in summary mode and `AI MAINTAINABILITY DETAIL` in deep mode. Report `FAIL` when the project has no executable verification command, no agent instruction surface for a non-trivial repo, or broken doc references. Report `WARN` when instructions exist but lack a project map, verification guidance, boundary/non-goal language, when TODO/HACK markers are concentrated, when large source hotspots lack ownership/boundary and verification guidance, or when durable docs contain raw one-off review reports, scorecards, dated line references, or diagnostic dumps instead of stable invariants. Treat missing `docs/`, `specs/`, `.specify/`, `HANDOFF.md`, `CHANGELOG`, issue templates, and PR templates as informational unless project complexity makes them necessary for handoff. The action for stale reports is to extract stable rules into public instructions, rules, references, or verifier scripts, then remove or archive the transient report.

**Hotspot ownership gaps.** In deep mode, read `HOTSPOT OWNERSHIP SURFACE`. If a largest source file exceeds the hotspot threshold and `AGENTS.md` / `CLAUDE.md` / shared instruction files do not name who owns the hotspot, what boundary should stay stable, and which verification command covers it, report a Structural `WARN`. Do not treat documented large files as code rot by size alone; some modules are intentionally large.

**Missing stable verifier wrapper.** If the repo exposes multiple verification commands through CI, scripts, or manifests but `Makefile` has no `check`, `test`, or `verify` target, report a Structural `WARN`. This is an AI-maintainability gap because agents need one stable default entrypoint, not because the project is broken.

Quick check from the project root:

```bash
bash skills/health/scripts/check-maintainability.sh . summary
```

For deep audits:

```bash
bash skills/health/scripts/check-maintainability.sh . deep
```

Keep actions concrete and non-invasive: add or fix the smallest useful instruction surface, add one executable validation command, document hotspot ownership and tests, split only when the boundary is already clear, or repair the broken reference. Do not propose broad rewrites from the script output alone.

**Broken doc references.** Scan `AGENTS.md`, `CLAUDE.md`, `.claude/rules/*.md`, and every `.claude/skills/*/SKILL.md` for references shaped like `@<path>`, `~/.claude/rules/<name>.md`, `~/.claude/skills/<name>/`, `docs/<name>.md`, or `references/<name>.md`. For each match, check that the target exists on disk. Report every "referenced but missing" pointer with the source file and line.

Common offenders:
- A project-level rule references a global rule file that was never created (e.g. `~/.claude/rules/swift.md`).
- A `CLAUDE.md` uses an `@AGENTS.md` placeholder but the actual `AGENTS.md` is missing or empty.
- A skill body references `references/<name>.md` but only `references/<name>-v2.md` exists.
- A rule file references a deleted skill path.

Quick check from the project root:

```bash
bash skills/health/scripts/check-doc-refs.sh .
```

The checker resolves `@...` and `docs/...` from the project root, expands `~`, resolves `references/...` from each `.claude/skills/<name>/SKILL.md` directory, checks every reference on a line, skips fenced code examples, and exits non-zero when any target is missing.

Report missing references as Structural findings, not Critical, unless the missing file is named as a hard dependency (e.g. `release.md` for the project's release skill).

**Broken Markdown references.** In deep mode, `check-maintainability.sh` also scans repository Markdown links. Report these as Structural findings when they point to missing local files, especially design, security, release, or handoff docs that agents may follow during future work.

**Stale verifier cache output.** If validation output points at a deleted temp worktree or non-existent `/tmp` / `/private/tmp` file, parse the captured log with:

```bash
bash skills/health/scripts/check-verifier-output.sh . <log-file>
```

Only use this script for existing command output supplied by the user or generated during the current audit. Do not run project tests just to feed this checker. Known actions include `golangci-lint cache clean`, `go clean -cache -testcache`, and `npm cache verify`; unknown tools get a diagnostic rerun action.

### [-] Incremental -- nice to have

Outdated items, global vs local placement, context hygiene, stale allowedTools entries.

---

If no issues: `All relevant checks passed. Nothing to fix.`

## Non-goals

- Never auto-apply fixes without confirmation.
- Never apply complex-tier checks to simple projects.
- Never act as a heavy lint, typecheck, duplication, or architecture-rewrite substitute; `/health` reports maintainability guardrails and concrete next actions only.

## Gotchas


| What happened                                                               | Rule                                                                                                                                                                                                                                                                                           |
| --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Missed the local override                                                   | Always read `settings.local.json` too; it shadows the committed file                                                                                                                                                                                                                           |
| Subagent timeout reported as MCP failure                                    | MCP failures come from the live probe, not data collection                                                                                                                                                                                                                                     |
| Reported issues in wrong language                                           | Honor CLAUDE.md Communication rule first                                                                                                                                                                                                                                                       |
| Flagged intentionally noisy hook as broken                                  | Ask before calling a hook "broken"                                                                                                                                                                                                                                                             |
| Hook seemed not to fire, but it did -- a later UI element rendered above it | Hook firing order is not visual order. Before re-editing the hook config: (a) confirm with `--debug` or by piping output, (b) check whether a diff dialog, permission prompt, or other UI element rendered on top and pushed the hook output offscreen, (c) only then suspect the hook itself. |
| `/health` burned too much quota on first run                                | Stay in summary mode first. Full conversation extracts and inspector subagents are deep-audit tools, not the default path for Standard projects.                                                                                                                                                 |
| Treated missing specs/docs as a failure                                     | Decision artifacts are optional by default. Escalate missing docs/specs only when the tier, active handoff risk, or user request makes them necessary.                                                                                                                                           |
| Treated an ignored AGENTS/CLAUDE file as durable project truth              | Report whether the rule is tracked and distributed. Local overlays can inform the audit, but durable fixes belong in public repo docs or shipped skill/rule files.                                                                                                                               |
| Treated a review scorecard as maintainability documentation                 | Scorecards are snapshots. Extract the invariant and verification path, then remove or archive the report instead of calling the score itself a durable rule.                                                                                                                                     |
