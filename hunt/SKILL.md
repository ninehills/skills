---
name: hunt
description: "Finds root cause of errors, crashes, regressions, screenshot-reported defects, unexpected behavior, and failing tests before applying any fix. Not for code review or new features."
when_to_use: "排查, 查查, 报错, 崩溃, 不工作, 不对, 跑不通, 以前是好的, 回归, 截图回归, 继续优化, 反复修不好, debug, regression, used to work, broke after update, why broken, not working, what's wrong, fix error, stack trace"
metadata:
  version: "3.19.0"
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

## Hard Rules

- **Same symptom after a fix is a hard stop; so is "let me just try this."** Both mean the hypothesis is unfinished. Re-read the execution path from scratch before touching code again.
- **After three failed hypotheses, stop.** Use the Handoff format below to surface what was checked, what was ruled out, and what is unknown. Ask how to proceed.
- **Verify before claiming.** Never state versions, function names, or file locations from memory. Run `sw_vers` / `node --version` / grep first. No results = re-examine the path.
- **External tool failure: diagnose before switching.** When an MCP tool or API fails, determine why first (server running? API key valid? Config correct?) before trying an alternative.
- **Pay attention to deflection.** When someone says "that part doesn't matter," treat it as a signal. The area someone avoids examining is often where the problem lives.
- **Visual/rendering bugs: static analysis first.** Trace paint layers, stacking contexts, and layer order in DevTools before adding console.log or visual debug overlays. Logs cannot capture what the compositor does. Only add instrumentation after static analysis fails.
- **Fix the cause, not the symptom.** If the fix touches more than 5 files, pause and confirm scope with the user.

## Bisect Mode

Activate when the symptom is "used to work, now broken" or "broke after an update".

Find the last-known-good tag (`git tag --sort=-version:refname | head -5`), define a non-interactive pass/fail test command, run `git bisect start / bad / good <tag>`, let bisect drive without jumping ahead, read large files once and reference from notes rather than re-reading at each step, and when bisect names the culprit commit read only that diff to identify the specific line that introduced the regression.

## Repeated Regression / Screenshot Reference Mode

Activate when the user says the same issue is still wrong after a fix, provides a "good" screenshot/version/file, or describes a visual result as previously correct.

Treat the reference as evidence, not decoration:

1. List every reported and visible symptom, preserving the user's concrete words where useful ("still slow", "not clear", "尖刺", "先显示上一个内容").
2. Identify the reference oracle: last-good commit/tag, old build, fixture, screenshot, downloaded artifact, or expected state from the user's description.
3. Define the pass/fail check before editing. For visual bugs, this may be a narrow screenshot checklist plus the command that renders the view; for behavioral bugs, prefer an automated regression test or deterministic repro.
4. Compare current vs. reference and name the exact delta. Do not generalize a visual defect into "style polish" when the evidence points to a broken render, race, font pipeline, or state path.
5. If the same symptom remains after one attempted fix, stop and rebuild the hypothesis from the evidence. Do not stack more patches onto a disproven explanation.

If the issue is purely subjective UI taste, route to `/design`. If it is rendering, state, timing, build output, font generation, or a regression from a known-good version, stay in `/hunt`.

## Confirm or Discard

Add one targeted instrument: a log line, a failing assertion, or the smallest test that would fail if the hypothesis is correct. Run it. If the evidence contradicts the hypothesis, discard it completely and re-orient with what was just learned. Do not preserve a hypothesis the evidence disproves.

## Targeted Logging

Use logs as a scalpel, not as noise. Before adding a log, write the question it answers:

> "If this log prints X before Y, hypothesis A is still possible; if it does not, hypothesis A is wrong."

Good logging flow:

1. Place the first log at the boundary where the symptom should become explainable: request handler, state setter, cache read/write, render branch, async callback, build pass, or external tool boundary.
2. Log the minimum discriminating facts: timestamp or sequence number, input key, selected branch, old/new state, error code, and correlation id. Do not dump whole objects, secrets, tokens, personal data, or huge payloads.
3. If the expected log is missing, move upstream. If it appears with the wrong values, inspect that boundary. If it appears correctly, move downstream. This is binary search over the execution path.
4. For timing bugs, log ordering and identity, not just values: event id, source, start/end, stale/current version, and thread/task/queue when available.
5. Remove temporary logs before finishing, or gate useful diagnostics behind an existing debug flag/logger level. Do not leave `console.log`, `print`, or noisy tracing in shipped paths unless the project already keeps debug instrumentation there.

If adding logs changes the behavior, treat that as evidence of a timing, lifecycle, buffering, or concurrency problem. Do not dismiss it as "just logging side effects."

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

Activate when: "PDF looks wrong", "page break issue", "font not rendering", or broken PDF output

Diagnosis checklist:
- **WeasyPrint bugs**: `rgba()` causes double-rectangle bug (use solid hex), `page-break-inside: avoid` ignored (use explicit breaks)
- **Font loading**: Check @font-face paths, CORS headers, file format support
- **Page overflow**: Calculate content height vs page height, suggest line-height/padding reduction
- **Browser print CSS**: Confirm `@media print` rules, `@page` margins, orphan/widow control

Static analysis first (CSS review), then reproduce if needed.

## IME / Unicode Issues

For input method, character rendering, or text encoding bugs (IME state, cursor drift, emoji splitting, composition events), check `references/ime-unicode.md` first before forming a hypothesis.
