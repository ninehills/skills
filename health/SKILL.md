---
name: health
description: "Audits the full six-layer Claude Code config stack when Claude ignores instructions, behaves inconsistently, hooks malfunction, or MCP servers need auditing. Flags issues by severity. Not for debugging code or reviewing PRs."
when_to_use: "检查claude, 健康度, 配置检查, 配置对不对, Claude ignoring instructions, check config, settings not working, audit config"
metadata:
  version: "3.16.0"
---

# Health: Audit the Six-Layer Stack

Prefix your first line with 🥷 inline, not as its own paragraph.

Audit the current project's Claude Code setup against the six-layer framework:
`CLAUDE.md → rules → skills → hooks → subagents → verifiers`

Find violations. Identify the misaligned layer. Calibrate to project complexity only.

**Output language:** Check in order: (1) CLAUDE.md `## Communication` rule (global over local); (2) user's recent language; (3) English.

## Step 0: Assess project tier

Pick one. Apply only that tier's requirements.

| Tier | Signal | What's expected |
|------|--------|-----------------|
| **Simple** | <500 files, 1 contributor, no CI | CLAUDE.md only; 0-1 skills; hooks optional |
| **Standard** | 500-5K files, small team or CI | CLAUDE.md + 1-2 rules; 2-4 skills; basic hooks |
| **Complex** | >5K files, multi-contributor, active CI | Full six-layer setup required |

## Step 1: Collect data

Run the collection script. Do not interpret yet.

```bash
bash "${CLAUDE_SKILL_DIR:-$HOME/.agents/skills/health}/scripts/collect-data.sh"
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
- **Standard/Complex:** Launch two subagents in parallel. Redact credentials to `[REDACTED]`.
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

### [-] Incremental -- nice to have

Outdated items, global vs local placement, context hygiene, stale allowedTools entries.

---

If no issues: `All relevant checks passed. Nothing to fix.`

## Non-goals

- Never auto-apply fixes without confirmation.
- Never apply complex-tier checks to simple projects.

## Gotchas

| What happened | Rule |
|---------------|------|
| Missed the local override | Always read `settings.local.json` too; it shadows the committed file |
| Subagent timeout reported as MCP failure | MCP failures come from the live probe, not data collection |
| Reported issues in wrong language | Honor CLAUDE.md Communication rule first |
| Flagged intentionally noisy hook as broken | Ask before calling a hook "broken" |
| Hook seemed not to fire, but it did -- a later UI element rendered above it | Hook firing order is not visual order. Before re-editing the hook config: (a) confirm with `--debug` or by piping output, (b) check whether a diff dialog, permission prompt, or other UI element rendered on top and pushed the hook output offscreen, (c) only then suspect the hook itself. |
