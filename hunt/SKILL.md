---
name: hunt
description: "Finds root cause of errors, crashes, regressions, screenshot-reported defects, unexpected behavior, and failing tests before applying any fix. Not for code review or new features."
when_to_use: "排查, 查查, 报错, 崩溃, 不工作, 不对, 跑不通, 以前是好的, 回归, 截图回归, 判断错误原因, 判断为什么报错, 反复修不好, debug, regression, used to work, broke after update, why broken, not working, what's wrong, fix error, stack trace"
metadata:
  version: "3.20.0"
---

# Hunt: Diagnose Before You Fix

Prefix your first line with 🥷 inline, not as its own paragraph.

A patch applied to a symptom creates a new bug somewhere else.

**Do not touch code until you can state the root cause in one sentence:**
> "I believe the root cause is [X] because [evidence]."

Name a specific file, function, line, or condition. "A state management issue" is not testable. "Stale cache in `useUser` at `src/hooks/user.ts:42` because the dependency array is missing `userId`" is testable. If you cannot be that specific, you do not have a hypothesis yet.

## Diagnosis Signals

Good progress: a log line matches the hypothesis, you can predict the next error before running it, you understand the propagation path from root cause to symptom, you can write a test that fails on the old code. At each of these signals, find one more independent piece of evidence before committing.

Hypothesis quality gate: before acting on a hypothesis, list all observable symptoms (not just the one the user reported first). The hypothesis must explain every symptom; if it only covers some, it is a symptom-level guess, not a root cause. For timing-dependent issues (flicker, intermittent failure, race condition), reproduce reliably before diagnosing.

Rationalization warning: "I'll just try this" means no hypothesis, write it first. "I'm confident" means run an instrument that proves it. "Probably the same issue" means re-read the execution path from scratch. "It works on my machine" means enumerate every env difference before dismissing. "One more restart" means read the last error verbatim; never restart more than twice without new evidence.

## Durable Context Preflight

Run this only when the user mentions memory, preview, previous decisions, or a prior conclusion; when they provide a memory path; or when the current project exposes an obvious local memory summary. Do not hard-code machine-specific memory roots or read raw transcripts.

Read durable context in this order: user-provided path, current project scope, then global preferences. List titles first, then open at most 1-2 relevant summaries. Treat cross-project entries as transferable patterns only.

Map memory types before using them: `decision`, `preference`, and `principle` describe diagnostic constraints; `pattern` and `learning` can seed hypotheses; `fact` must be verified against current state before it affects diagnosis. Current code, logs, repro steps, tests, environment versions, and remote state override memory.

For `/hunt`, durable context is hypothesis fuel only. It never replaces a fresh root-cause sentence, a reproducible symptom list, or evidence from the current state.

## Hard Rules

- **Same symptom after a fix is a hard stop; so is "let me just try this."** Both mean the hypothesis is unfinished. Re-read the execution path from scratch before touching code again.
- **After three failed hypotheses, stop.** Use the Handoff format below to surface what was checked, what was ruled out, and what is unknown. Ask how to proceed.
- **Verify before claiming.** Never state versions, function names, or file locations from memory. Run `sw_vers` / `node --version` / grep first. No results = re-examine the path.
- **External tool failure: diagnose before switching.** When an MCP tool or API fails, determine why first (server running? API key valid? Config correct?) before trying an alternative.
- **Pay attention to deflection.** When someone says "that part doesn't matter," treat it as a signal. The area someone avoids examining is often where the problem lives.
- **Visual/rendering bugs: static analysis first.** Trace paint layers, stacking contexts, and layer order in DevTools before adding console.log or visual debug overlays. Logs cannot capture what the compositor does. Only add instrumentation after static analysis fails.
- **Fix the cause, not the symptom.** If the fix touches more than 5 files, pause and confirm scope with the user.

## Bisect Mode

Activate when: "以前是好的", "之前是好的", "used to work", "上一次提交还是对的", "broke after update", or the user remembers a specific good commit or version.

1. Find candidate good tag: `git tag --sort=-version:refname | head -10` or ask the user for the last known-good commit.
2. Define a non-interactive pass/fail test command before starting bisect. Bisect is worthless without a reproducible check.
3. Run: `git bisect start && git bisect bad HEAD && git bisect good <tag-or-hash>`
4. At each step bisect checks out a commit. Run the test command. Mark: `git bisect good` or `git bisect bad`.
5. Let bisect drive. Do not jump ahead or skip commits unless explicitly asked.
6. When bisect names the culprit commit, read only that diff. Identify the specific line that introduced the regression.
7. Run `git bisect reset` when done.

Read large files once and reference from notes rather than re-reading at each bisect step.

## Repeated Regression / Screenshot Reference Mode

Activate when the user says the same issue is still wrong after a fix, provides a "good" screenshot/version/file, or describes a visual result as previously correct.

Treat the reference as evidence, not decoration:

1. List every reported and visible symptom, preserving the user's concrete words where useful ("still slow", "not clear", "尖刺", "先显示上一个内容").
2. Identify the reference oracle: last-good commit/tag, old build, fixture, screenshot, downloaded artifact, or expected state from the user's description.
3. Define the pass/fail check before editing. For visual bugs, this may be a narrow screenshot checklist plus the command that renders the view; for behavioral bugs, prefer an automated regression test or deterministic repro.
4. Compare current vs. reference and name the exact delta. Do not generalize a visual defect into "style polish" when the evidence points to a broken render, race, font pipeline, or state path.
5. If the same symptom remains after one attempted fix, stop and rebuild the hypothesis from the evidence. Do not stack more patches onto a disproven explanation.

If the issue is purely subjective UI taste, route to `/design`. If it is rendering, state, timing, build output, font generation, or a regression from a known-good version, stay in `/hunt`.

## Scope Blast Mode

Activate after fixing a root-cause pattern, before declaring the bug done. The same shape often hides in N other places; one local fix that ignores the blast leaves N - 1 bugs in the tree.

1. Extract the pattern signature: the specific function name, regex, API call, CSS selector, lock acquisition, validation skip, or input boundary that produced the bug.
2. `grep -rn <pattern>` across the repo (exclude generated dirs, build output, vendored deps). For class-of-bug patterns (e.g. "any handler missing the lock"), grep for the surrounding shape, not just the literal text.
3. List every match. For each one, answer in writing: same bug here? Pick fix / leave (explain why it is safe) / unsure (ask the user). Do not silently skip a match.
4. Do not claim "fixed" until the blast report is in the Outcome block.

Common triggers:
- Visual bug fixed on one page: check every other page using the same component, layout, or media-query breakpoint.
- One race fixed in one handler: check every handler acquiring the same lock or touching the same shared state.
- One validation skip patched at one entry point: check every entry point that reaches the same downstream sink.
- One regex / parser fix for one input shape: check every caller of the same regex / parser.

If the blast surfaces unrelated bugs, list them but do not fix in this PR unless the user agrees; scope creep is its own anti-pattern.

## Confirm or Discard

Add one targeted instrument: a log line, a failing assertion, or the smallest test that would fail if the hypothesis is correct. Run it. If the evidence contradicts the hypothesis, discard it completely and re-orient with what was just learned. Do not preserve a hypothesis the evidence disproves.

## Targeted Logging

Use logs as a scalpel, not as noise. Before adding a log, write the question it answers:

> "If this log prints X before Y, hypothesis A is still possible; if it does not, hypothesis A is wrong."

Load `references/logging-techniques.md` for the full logging playbook: binary-search instrumentation, discriminating log content, boundary-first placement, timing bug logging, and removal discipline.

Quick rules:
1. Place the first log at the midpoint of the execution path, not at the symptom. Binary search from there.
2. Log discriminating facts only: sequence number, input key, branch taken, old/new state, error code.
3. Remove temporary logs before finishing. Gate persistent diagnostics behind the project's debug flag.

If adding logs changes the behavior, treat that as evidence of a timing, lifecycle, or concurrency problem.

## Gotchas

| What happened | Rule |
|---------------|------|
| Patched client pane instead of local pane | Trace the execution path backward before touching any file |
| MCP not loading, switched tools instead of diagnosing | Check server status, API key, config before switching methods |
| Orchestrator said RUNNING but TTS vendor was misconfigured | In multi-stage pipelines, test each stage in isolation |
| Race condition diagnosed as a stale-state bug | For timing-sensitive issues, inspect event timestamps and ordering before state |
| Added logs everywhere and still could not explain the bug | Rewrite each log as a yes/no question. Delete logs that do not rule a hypothesis in or out |
| Reproduced locally but failed in CI | Align the environment first (runtime version, env vars, timezone), then chase the code |
| Stack trace points deep into a library | Walk back 3 frames into your own code; the bug is almost always there, not in the dependency |
| Worked when launched from app, broke when opened via file association / drag-drop / deep link / external proxy | Reproduce using the exact entry point the user described. App-internal init differs from cold-launch-with-file init; state may not be ready when the document arrives. |

## Outcome

### Success Format

```
Root cause:        [what was wrong, file:line]
Fix:               [what changed, file:line]
Confirmed:         [evidence or test that proves the fix]
Tests:             [pass/fail count, regression test location]
Regression guard:  [test file:line] or [none, reason]
```

Status: **resolved**, **resolved with caveats** (state them), or **blocked** (state what is unknown).

**Regression guard rule**: for any bug that recurred or was previously "fixed", the fix is not done until:
1. A regression test exists that fails on the unfixed code and passes on the fixed code.
2. The test lives in the project's test suite, not a temporary file.
3. The commit message states why the bug recurred and why this fix prevents it.

### Handoff Format (after 3 failed hypotheses)

```
Symptom:
[Original error description, one sentence]

Hypotheses Tested:
1. [Hypothesis 1] → [Test method] → [Result: ruled out because...]
2. [Hypothesis 2] → [Test method] → [Result: ruled out because...]
3. [Hypothesis 3] → [Test method] → [Result: ruled out because...]

Evidence Collected:
- [Log snippets / stack traces / file content]
- [Reproduction steps]
- [Environment info: versions, config, runtime]

Ruled Out:
- [Root causes that have been eliminated]

Unknowns:
- [What is still unclear]
- [What information is missing]

Suggested Next Steps:
1. [Next investigation direction]
2. [External tools or permissions that may be needed]
3. [Additional context the user should provide]
```

Status: **blocked**

## Rendering Bug Mode

Activate when: "PDF looks wrong", "page break issue", "font not rendering", broken PDF output, or print layout wrong.

Load `references/rendering-debug.md` for the full diagnosis checklist (WeasyPrint quirks, font loading, page overflow, browser print CSS). Static analysis first, then reproduce if needed.

## IME / Unicode Issues

For input method, character rendering, or text encoding bugs (IME state, cursor drift, emoji splitting, composition events), check `references/ime-unicode.md` first before forming a hypothesis.
