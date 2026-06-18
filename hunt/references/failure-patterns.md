# Failure Pattern Reference

Use this when a bug has repeated, a first fix did not hold, or the symptom smells like runtime state rather than local code syntax.

## Stale Verifier Or Tool Cache

Signals: verifier output points at deleted temp worktrees, old generated files, or paths outside the current repo; rerunning after a clean checkout changes the file path but not the current code.

Checks:
- Confirm the reported path exists.
- Clear the tool cache only after proving the path is stale.
- Re-run the same verifier from the current repo root.

## Worker Queue Or DB Boundary

Signals: UI says work is running but no worker processes it; logs show scheduler activity but no queued row; retry fixes one item but not the pipeline.

Checks:
- Trace request -> enqueue -> worker pickup -> persistence -> UI refresh.
- Inspect queue rows or job state directly.
- Add a regression test around the enqueue boundary, not only the worker body.

## Generated Rebuild Boundary

Signals: source changed but generated output, app bundle, CLI artifact, archive, checksum, or release package still contains old behavior.

Checks:
- Identify the source-to-artifact rule.
- Verify the build system watches the source path.
- Inspect the generated artifact contents, not just the source diff.

## Guard Lifetime Race

Signals: permission, auth, or state guard looks correct locally but a delayed callback, app relaunch, or alternate entry point bypasses it.

Checks:
- Trace guard creation, retention, invalidation, and every alternate entry point.
- Verify cold launch, warm launch, deep link/file open, and retry paths when applicable.
- Prefer explicit durable state over transient flags when the guard must survive relaunch.

## Atomic Temp Filename

Signals: concurrent runs collide, cleanup removes the wrong file, or a partially written output is observed.

Checks:
- Use unique temp directories or atomic rename.
- Keep cleanup scoped to files created by the current run.
- Test two concurrent or back-to-back runs when the tool supports it.

## Path, Cwd, Or Symlink Escape

Signals: an operation intended for one root touches a sibling directory, follows a symlink unexpectedly, or behaves differently from another working directory.

Checks:
- Resolve and compare canonical roots before writing or deleting.
- Reject paths outside the allowed root after symlink resolution.
- Reproduce from a non-default cwd and through any UI entry point that supplies paths.

## CLI Effect Scope Drift

Signals: preview, dry-run, size, count, or report output is computed from one predicate, but execution mutates a broader or different set.

Checks:
- Trace display, dry-run, and mutation predicates to the same source of truth.
- Compare planned paths or records with executor input in a regression test.
- Assert partial failures report the exact skipped and completed items.

## CLI Wrapper Or PATH Drift

Signals: source-tree invocation works, but the installed command, package wrapper, PATH shim, completion, or package-manager install path runs old code or a different binary.

Checks:
- Inspect built package contents, shebang, executable bit, and wrapper target.
- Reproduce through a temp prefix or package-manager install path, not only from source.
- Check PATH order and use absolute system-tool paths where wrappers should not intercept.

## Interactive Stdin Or TTY Hang

Signals: CI stalls, spinner never finishes, a subprocess reads from the script body, or an auth prompt appears in non-interactive mode.

Checks:
- Reproduce with stdin redirected and with TTY/non-TTY paths separated.
- Add test-mode or no-auth guards around real prompts and system changes.
- Stub external prompt tools through PATH when timeout wrappers exec real binaries.

## Subprocess Pipe Backpressure

Signals: a long-running child process hangs only on large output, small fixtures pass, or the parent waits for exit before reading stdout/stderr. The child may be blocked on a full pipe buffer while the parent is blocked on `wait`.

Checks:
- Drain stdout and stderr while the process runs, or explicitly inherit/redirect streams when output is not needed.
- Test with output larger than a typical pipe buffer, not only tiny fixtures.
- Preserve stderr tails or structured error output for diagnostics without holding the whole stream in memory.

## Signal Or Partial-Failure Mapping

Signals: cancel, timeout, SIGINT, or SIGTERM is reported as success or as a normal business failure; temp files, locks, or operation logs make retries look complete.

Checks:
- Classify interrupted execution separately from success and expected validation failures.
- Assert temp cleanup, lock release, and operation-log state after interruption.
- Test retry and idempotency after a partial write.

## CLI Stream Contract Regression

Signals: automation breaks after human logs, progress output, JSON shape, stdout/stderr routing, or exit-code behavior changes.

Checks:
- Assert exit code, stdout, and stderr separately in CLI tests.
- Keep human diagnostics off stdout for machine-readable modes.
- Snapshot or parse JSON/schema output and include non-interactive coverage.

## Snapshot Rebuild Drops Carried Field

Signals: live data shows up at the data source and on the wire but a downstream view sees it empty; the field has a default value (`var x: [T] = []`, `var y: Int? = nil`) that lets memberwise init compile without it; the symptom appears only on the path where the snapshot is rebuilt (icon resolution, decoration, redaction), not on a fresh fetch.

Checks:
- Trace whether every code path that constructs the snapshot type passes the field. The Swift compiler does not warn on default-value omission in memberwise init.
- Add a unit test that fetches the snapshot, runs the rebuild path, and asserts the carried field equals the input.
- Prefer `with(...)` mutating helpers or `inout` mutation over fresh memberwise init when only one field is changing.

## Multi-Sample Command Cold Start

Signals: a CLI tool that takes `-l N` / `--samples N` / `--repeat N` returns one block of zeros and one block of real data; aggregating all blocks yields zeros; only the second sample carries real measurements.

Checks:
- Read the tool's man page for cold-start semantics. `top -l 2`, `iostat -d 2`, `vm_stat 1 2`, etc. all share this shape.
- Slice the output to the latest sample (`.suffix(perSampleSize)` on parsed lines, or look for the second instance of the header row).
- When in doubt, raise `-l` to 3 and confirm sample 2 and 3 agree; sample 1 stays zero.

## Aggregation Key Variant

Signals: a count, log roll-up, event tally, or per-category breakdown is short by some entries; the missing items share a trait (a system-derived path, a localized string, a prefixed command name); the base-form key matches but a derived variant (`<base>-system`, a suffix, a prefix) is silently dropped.

Checks:
- Before adding a category, grep every write site that produces this class of key and enumerate the real variants, not just the base form.
- Match with `hasPrefix` / a regex / an explicit variant list rather than exact equality on the base key.
- Add a fixture row for each known variant so a future key shape that escapes the matcher fails the test instead of the aggregate.
