---
name: health
description: "Runs a budget-aware audit of the Claude Code config stack when Claude ignores instructions, behaves inconsistently, hooks malfunction, MCP servers need auditing, or users ask why /health used many tokens. Flags issues by severity. Not for debugging code or reviewing PRs."
when_to_use: "检查claude, 健康度, 配置检查, 配置对不对, Claude ignoring instructions, check config, settings not working, audit config"
metadata:
  version: "3.18.0"
---

# Health: Audit the Six-Layer Stack

Prefix your first line with 🥷 inline, not as its own paragraph.

Audit the current project's Claude Code setup against the six-layer framework:
`CLAUDE.md → rules → skills → hooks → subagents → verifiers`

Find violations. Identify the misaligned layer. Calibrate to project complexity only.

**Output language:** Check in order: (1) CLAUDE.md `## Communication` rule (global over local); (2) user's recent language; (3) English.

**Budget posture:** Start with the summary audit. Escalate automatically when the user asks for a deep, full, complete, thorough, "深入", "完整", "彻底", or "继续跑完" audit, when current project instructions or remembered user preference says to run deep health checks by default, when the project is Complex, or when the summary pass exposes a critical ambiguity that cannot be resolved locally. Otherwise do not read full conversation extracts or launch inspector subagents. Tell the user before escalating because deep health audits can consume significant token quota.

## Durable Context Preflight

Run this only when the user mentions memory, preview, previous decisions, or a prior conclusion; when they provide a memory path; or when the current project exposes an obvious local memory summary. Do not hard-code machine-specific memory roots or read raw transcripts.

Read durable context in this order: user-provided path, current project scope, then global preferences. List titles first, then open at most 1-2 relevant summaries. Treat cross-project entries as transferable patterns only.

Map memory types before using them: `decision`, `preference`, and `principle` are audit expectations; `pattern` and `learning` are checks for repeated failures; `fact` must be verified against current state before it becomes a finding. CLAUDE.md, installed skills, hooks, MCP config, command output, and live probes override memory.

For `/health`, also flag durable memory problems when they affect behavior: oversized injected summaries, stale or contradictory entries, missing project entrypoint references, or private paths copied into public instructions. Keep these as context findings, not code-review findings.

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

## Step 1b: MCP Live Check

Test every MCP server: call one harmless tool per server. Record `live=yes/no` with error detail. Respect `enabled: false` (skip without flagging). For API keys, only check if the env var is set (`echo $VAR | head -c 5`), never print full keys.

## Step 2: Analyze

Confirm the tier. Then route:

- **Simple:** Analyze locally. No subagents.
- **Standard:** Analyze locally from the summary output. Do not launch subagents by default. If the user asks for a deep/full/thorough audit, or if local analysis cannot classify a security/control issue, escalate to deep mode and explain the likely token cost.
- **Complex, remembered deep preference, or explicit deep audit:** Re-run collection with `bash "$HEALTH_SCRIPT" auto deep`, then launch two subagents in parallel. Redact credentials to `[REDACTED]`.
  - **Agent 1** (Context + Security): Read `agents/inspector-context.md`. Feed `CONVERSATION SIGNALS` section.
  - **Agent 2** (Control + Behavior): Read `agents/inspector-control.md`. Feed detected tier.
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

CLAUDE.md content in wrong layer, missing hooks, oversized descriptions, verifier gaps.

**Broken doc references.** Scan `AGENTS.md`, `CLAUDE.md`, `.claude/rules/*.md`, and every `.claude/skills/*/SKILL.md` for references shaped like `@<path>`, `~/.claude/rules/<name>.md`, `~/.claude/skills/<name>/`, `docs/<name>.md`, or `references/<name>.md`. For each match, check that the target exists on disk. Report every "referenced but missing" pointer with the source file and line.

Common offenders:
- A project-level rule references a global rule file that was never created (e.g. `~/.claude/rules/swift.md`).
- A `CLAUDE.md` uses an `@AGENTS.md` placeholder but the actual `AGENTS.md` is missing or empty.
- A skill body references `references/<name>.md` but only `references/<name>-v2.md` exists.
- A rule file references a deleted skill path.

Quick check:

```bash
# Replace <root> with the project root being audited.
for f in <root>/AGENTS.md <root>/CLAUDE.md <root>/.claude/rules/*.md <root>/.claude/skills/*/SKILL.md; do
  [ -f "$f" ] || continue
  grep -nE '@[A-Za-z][A-Za-z0-9_/.-]+\.md|~/\.claude/[A-Za-z0-9_/.-]+\.md|(docs|references)/[A-Za-z0-9_/.-]+\.md' "$f" \
    | while IFS=: read -r line ref; do
        target=$(echo "$ref" | grep -oE '@?[A-Za-z0-9_~/.-]+\.md' | head -1 | sed 's|^@||; s|^~|'"$HOME"'|')
        case "$target" in
          /*) full="$target" ;;
          *)  full="$(dirname "$f")/$target" ;;
        esac
        [ -f "$full" ] || echo "MISSING: $f:$line -> $target"
      done
done
```

Report missing references as Structural findings, not Critical, unless the missing file is named as a hard dependency (e.g. `release.md` for the project's release skill).

### [-] Incremental -- nice to have

Outdated items, global vs local placement, context hygiene, stale allowedTools entries.

---

If no issues: `All relevant checks passed. Nothing to fix.`

## Non-goals

- Never auto-apply fixes without confirmation.
- Never apply complex-tier checks to simple projects.

## Gotchas


| What happened                                                               | Rule                                                                                                                                                                                                                                                                                           |
| --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Missed the local override                                                   | Always read `settings.local.json` too; it shadows the committed file                                                                                                                                                                                                                           |
| Subagent timeout reported as MCP failure                                    | MCP failures come from the live probe, not data collection                                                                                                                                                                                                                                     |
| Reported issues in wrong language                                           | Honor CLAUDE.md Communication rule first                                                                                                                                                                                                                                                       |
| Flagged intentionally noisy hook as broken                                  | Ask before calling a hook "broken"                                                                                                                                                                                                                                                             |
| Hook seemed not to fire, but it did -- a later UI element rendered above it | Hook firing order is not visual order. Before re-editing the hook config: (a) confirm with `--debug` or by piping output, (b) check whether a diff dialog, permission prompt, or other UI element rendered on top and pushed the hook output offscreen, (c) only then suspect the hook itself. |
| `/health` burned too much quota on first run                                | Stay in summary mode first. Full conversation extracts and inspector subagents are deep-audit tools, not the default path for Standard projects.                                                                                                                                                 |
