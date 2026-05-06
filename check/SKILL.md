---
name: check
description: "Reviews code diffs and release-ready changes after implementation, extracts project-specific constraints from repository context, auto-fixes safe issues, and drives approved release, publish, push, release-reaction, and issue/PR follow-through. Also triages issues and PRs when the user mentions them. Not for exploring ideas or debugging."
when_to_use: "review, 看看代码, 检查一下, 有没有问题, 是否需要优化, 合并前, 看看issue, 看看PR, release, publish, push, release reaction, GitHub reaction, 发布, 提交, 关闭issue, 发布表情, release表情, close issue, issue close, review my code, check changes, before merge, before release, code review, code-review"
metadata:
  version: "3.21.0"
---

# Check: Review Before You Ship

Prefix your first line with 🥷 inline, not as its own paragraph.

Read the diff, find the problems, fix what can be fixed safely, ask about the rest. Done means verification ran in this session and passed.

## Project Context Extraction

This is Waza's public, standalone code-review capability. It should not depend on private machine paths or unpublished project instructions.

Before reviewing, extract project constraints from repository context:

1. Read the diff and identify changed languages, frameworks, manifests, generated outputs, release files, and CI workflows.
2. Inspect public project files only as needed: README, AGENTS/CLAUDE instructions when present, package manifests, lockfiles, build configs, test configs, workflow files, and release notes.
3. Compress the findings into review context: verification commands, protected or generated files, release artifacts, domain risks, and public reply rules.
4. Apply the stricter rule when project context and this skill overlap.
5. If project docs or CI name a verification command, prefer that over auto-detection.

For the context shape, see `references/project-context.md`.

## Get the Diff

Get the full diff between the current branch and the base branch. If unclear, ask. If already on the base branch, ask which commits to review.

## Triage Mode

Activate when the user mentions: issue, PR, "review all", triage, "batch", or "批量处理". Skip the diff flow and run this instead.

**Action-first rule:** Items with a clear disposition (already fixed, duplicate, already released) get acted on immediately without analysis paragraphs. When analyzing screenshots or images, state what you see and the suggested action in one message. Only ask the user when the disposition is genuinely ambiguous.

**Flow:** Pull open items with `gh issue list -R <repo> --state open --limit 20` and `gh pr list -R <repo> --state open`. For each item, check if a fix already shipped: `git log --oneline <latest-tag>..HEAD | grep -i "<keyword>"`. If shipped: close with note. If merged but unreleased: reply "已修复，等下一个版本 release" and close. If no fix: analyze and act. Fix now if possible (`fix: closes #N` commit); for Mole nightly-fixed items reply `@<user>, this is already fixed in the latest nightly. Upgrade: mo update --nightly` and close; for valid-but-unreleased items acknowledge and leave open; for invalid items give one-two sentence reason and close.

**PR handling:** If the PR direction is accepted but the patch needs changes, prefer pushing the maintainer's fixes to the contributor's PR branch and then merging the PR. Check `maintainerCanModify` first. If branch edits are not allowed, ask the contributor to enable maintainer edits or push the needed revision; only fall back to a separate maintainer commit when timing or release safety requires it, and say so in the PR. Close without merging only when the direction is rejected, unsafe, no longer needed, or explicitly not part of the project's scope. Do not silently absorb an accepted PR into `main` and close it.

**Public reply shape (maintainer, issue or PR):**

1. Resolve `@<login>` from `gh issue view` / `gh pr view --json author`.
2. **Language:** Match the **opener's** language when it is Chinese or English. If the opener used Japanese or Korean, use English for the maintainer reply unless project docs override.
3. Open with `@<login>` and **at most one** short thanks (`感谢反馈`, `thank you for the report`, etc.). Do **not** add closing thanks stacks (`再次感谢`, `Thanks again`, long courtesy endings).
4. One or two short paragraphs: factual reason, what shipped or what is blocked, no ceremony.
5. Always give a **next step tied to releases or verification**: next App Store or GitHub release, nightly upgrade command, cache path to clear once, or exactly what info is still needed.
6. Prefer **editing** an existing maintainer comment (`PATCH /repos/{owner}/{repo}/issues/comments/{comment_id}`) when updating wording; avoid delete plus repost unless the old text must disappear from history.

Default to this shape unless `AGENTS.md` or `CLAUDE.md` in the repo contradicts it.

**Sign-off line (append to standard sign-off):**
```
triage:           N reviewed, N closed, N deferred
```

## Ship / Release Follow-through

Activate when the user asks to commit, tag, release, publish, push, reply on an issue/PR, or close an issue after a change is ready.

This mode extends review; it does not skip review. Before any public or irreversible action:

1. Extract release rules from public project context: README, manifests, CI workflows, release notes, package scripts, changelogs, and explicit user instructions in the current thread.
2. Verify generated or bundled outputs, version fields, release notes, package contents, and required artifacts are in sync. Prefer dry-run commands when the ecosystem provides them.
3. Commit only intended files. Preserve unrelated dirty work, and serialize git operations so index locks or overlapping adds do not corrupt the workflow.
4. Push, publish, tag, or create a release only when the user has explicitly approved that action. If auth, OTP, CI, registry, or network state blocks the operation, pause and report the exact blocker.
5. For issue/PR follow-through, confirm the item identity with `gh issue view` or `gh pr view` before posting. Use the **Public reply shape** from Triage Mode (mention, single thanks, facts, explicit next release or verification step). Close only when the fix is shipped, already available, invalid, duplicate, or the maintainer explicitly asked for closure.
6. For GitHub release reaction follow-through, only do it when project context or the current thread asks for it. After the release exists and required assets are verified, resolve the release id from the tag, POST every positive release reaction to `repos/<owner>/<repo>/releases/<id>/reactions` with `gh api`, and re-read reactions to confirm. Positive release reactions are `+1`, `laugh`, `heart`, `hooray`, `rocket`, and `eyes`.
7. After network or API failures, re-read the end state instead of assuming success or failure.

End with the concrete shipped state: commit hash, tag, release URL, registry/version result, pushed branch, release asset state, release reaction state, issue/PR state, and any remaining blockers. Omit fields that do not apply.

## Scope

Measure the diff and classify depth:

| Depth | Criteria | Reviewers |
|-------|----------|-----------|
| **Quick** | Under 100 lines, 1-5 files | Base review only |
| **Standard** | 100-500 lines, or 6-10 files | Base + conditional specialists |
| **Deep** | 500+ lines, 10+ files, or touches auth/payments/data mutation | Base + all specialists + adversarial pass |

State the depth before proceeding.

## Did We Build What Was Asked?

Before reading code, check scope drift: do the diff and the stated goal match? Label: **on target** / **drift** / **incomplete**.

Drift signals (examples, not exhaustive -- any one is enough to label drift):
- A changed file has no connection to the stated goal
- The diff includes pure refactoring (renames, formatting, restructuring) when the goal was a bug fix or feature
- A new dependency appears that the goal did not mention
- Code unrelated to the goal was deleted or commented out
- A new abstraction or helper was introduced that is not required by the goal

## Hard Stops (fix before merging)

Examples, not exhaustive -- flag any diff that could cause irreversible harm if merged unreviewed.

- **Destructive auto-execution**: any task marked "safe" or "auto-run" that modifies user-visible state (history files, config, preferences, installed software) must require explicit confirmation.
- **Release artifacts missing**: verify every artifact listed in release notes, release templates, or project workflows exists and has been uploaded before declaring done.
- **Generated artifact drift**: if source changes require generated or bundled outputs, verify the output was regenerated and included.
- **Version skew**: release version fields across manifests, package metadata, app configs, changelogs, tags, or lockfiles must stay synchronized.
- **Unknown identifiers in diff**: any function, variable, or type introduced in the diff that does not exist in the codebase is a hard stop. Grep before writing or approving any reference: `grep -r "name" .` -- no results outside the diff = does not exist.
- **Injection and validation**: SQL, command, path injection at system entry points. Credentials hardcoded, logged, committed, or copied into public docs.
- **Dependency changes**: unexpected additions or version bumps in package.json, Cargo.toml, go.mod, requirements.txt. Flag any new dependency not obviously required by the diff.

## Specialist Review (Standard and Deep only)

Load `references/persona-catalog.md` to determine which specialists activate. Launch all activated specialists in parallel via the environment's agent or sub-agent facility when available, passing the full diff. If no parallel reviewer facility exists, run the specialist passes sequentially in the same session.

Merge findings: when two specialists flag the same code location, keep the higher severity and note cross-reviewer agreement. Findings on different code locations are never duplicates even if they share a theme.

## Autofix Routing

| Class | Definition | Action |
|-------|------------|--------|
| `safe_auto` | Unambiguous, risk-free: typos, missing imports, style inconsistencies | Apply immediately |
| `gated_auto` | Likely correct but changes behavior: null checks, error handling additions | Batch into one user confirmation block |
| `manual` | Requires judgment: architecture, behavior changes, security tradeoffs | Present in sign-off |
| `advisory` | Informational only | Note in sign-off |

Apply all `safe_auto` fixes first. Batch all `gated_auto` into one confirmation block. Never ask separately about each one.

## Adversarial Pass (Deep only)

"If I were trying to break this system through this specific diff, what would I exploit?" Four angles (see `references/persona-catalog.md`): assumption violation, composition failures, cascade construction, abuse cases. Suppress findings below 0.60 confidence.

## GitHub Operations

Use `gh` CLI for all GitHub interactions, not MCP or raw API. Confirm CI passes before merging.

## Verification

Run `bash scripts/run-tests.sh` from this skill directory, or the project's known verification command from the target repository. Paste the full output.

If the script exits non-zero or prints `(no test command detected)`: halt. Do not claim done. Ask the user for the verification command before proceeding. If the user also cannot provide one, document this explicitly in the sign-off as `verification: none -- no command available` and flag it as a structural gap, not a pass.

For bug fixes: a regression test that fails on the old code must exist before the fix is done.

## Gotchas

| What happened | Rule |
|---------------|------|
| Commented on #249 when discussing #255 | Run `gh issue view N` to confirm title before acting |
| PR comment sounded like a report | 1-2 sentences, natural, like a colleague. Not structured, not AI-sounding. |
| PR comment used bullet points | Write as short paragraphs, one thought per paragraph; thank the contributor first |
| article.en.md inside _posts_en/ doubled the suffix | Check naming convention of existing files in the target directory first |
| Deployed without env vars set | Run `vercel env ls` before deploying; diff against local keys |
| Push failed from auth mismatch | Run `git remote -v` before the first push in a new project |

## Document Review Mode

Activate when: PDF, document, release notes, white paper, final version, or "check this document"

Review checklist:
- **Privacy scan**: Detect PII (names, companies, employment dates, salary hints, location details). Hard stop if any text implies job seeking, competitor info, or personal data leakage.
- **Tone consistency**: Flag voice shifts, register mismatches, formulaic phrasing. Check for AI patterns (see `/write` skill for detection rules).
- **Bilingual validation**: For CN/EN pairs, confirm translation accuracy and terminology consistency. Use `/write` skill's bilingual rules.
- **Rendering check**: Placeholder text remaining (`Lorem ipsum`, `TODO`, `[TBD]`), style violations, font fallbacks, broken image links.

Output format: same as code review sign-off, but replace `verification:` with `privacy: clear / N issues found`.

## Sign-off

```
files changed:    N (+X -Y)
scope:            on target / drift: [what]
review depth:     quick / standard / deep
hard stops:       N found, N fixed, N deferred
specialists:      [security, architecture] or none
new tests:        N
verification:     [command] -> pass / fail
```
