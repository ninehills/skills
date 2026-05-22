# Project Review Context Template

Use this template to compress repository context before running Waza `/check`. The context must come from public project files, the diff, CI configuration, or explicit user instructions. Do not depend on private machine paths or unpublished project instructions.

## What Belongs In Waza `/check`

- Diff depth classification.
- Scope drift detection.
- Hard stops such as destructive automation, missing release artifacts, generated artifact drift, version skew, unknown identifiers, injection risks, credential leakage, and dependency surprises.
- Release Gate 2.0 matrix for release readiness.
- Safety sink review for destructive operations, command construction, path boundaries, signing/appcast, sandbox/approval, and auth prompts.
- Security and architecture specialist routing.
- Autofix policy.
- Sign-off format.
- Verification expectations.

## What Belongs In Project Context

- Verification commands discovered from public docs, manifests, Makefiles, scripts, or CI workflows.
- Protected files and directories.
- Generated or bundled artifacts that must stay in sync with source changes.
- Packaging source of truth: whether archives are built from `git ls-files`, explicit allowlists, generated manifests, or source directories.
- Runtime dependencies introduced by the diff: Python packages, CLIs, network services, package managers, or platform tools that are not already declared in CI/docs.
- Domain-specific safety rules.
- Release artifacts that must exist.
- GitHub release reactions or other public release follow-through expected by the project.
- Public issue or PR reply conventions.
- Known CI or test flakes documented by the project and how to distinguish them from real failures.
- Release, publish, push, or issue-closure prerequisites documented by the project.

## What Does Not Belong In Public Context

- Credential paths, private key filenames, passwords, tokens, or secret values.
- Maintainer-only machine paths.
- One-off personal preferences that do not affect project behavior.
- One-off review reports, scorecards, or diagnostic snapshots copied as guidance instead of distilled into stable project rules.
- Full copies of Waza `/check` sections.

## Recommended Context Shape

```markdown
## Project Commands

- Format: `<command>`
- Fast check: `<command>`
- Full verification: `<command>`

## Project Hard Stops

- Do not modify `<protected path>` unless explicitly requested.
- If `<artifact>` is generated from `<source>`, verify it was regenerated.
- If `<package script>` builds from tracked files or an allowlist, verify newly introduced helpers, references, templates, and scripts are included in `<archive>`.
- If an installer fetches remote content, verify the default ref is pinned to a release tag or checksum-protected; floating `main` must be an explicit override.
- If a helper introduces a non-stdlib package or external CLI, verify CI installs it or the helper fails with a clear setup path.
- If `<artifact>` is listed in release notes, verify it exists before sign-off.

## Project-Specific Risks

- `<risk>`: `<how to inspect it>`

## Public Replies

- Draft replies in the same language as the thread.
- Do not post comments, close issues, or merge PRs without maintainer approval.
- For accepted PRs, prefer updating the contributor branch and merging the PR; close without merge only when the direction is rejected, unsafe, out of scope, or the branch cannot be updated and a maintainer commit is explicitly needed.
- Default reply shape: `@<user>` + thanks, brief reason/action, then update command, release/version, or next step.
- Keep shipped-fix replies to 1-2 natural sentences unless the project explicitly uses a longer template.

## Release Follow-through

- Version fields to check: `<manifest>`, `<app config>`, `<lockfile>`.
- Generated artifacts to check: `<artifact>` from `<source>`.
- Dry-run command before publishing: `<command>`.
- GitHub release reactions to add after asset verification: `<+1/laugh/heart/hooray/rocket/eyes or none>`.
- Public state to re-read after publishing or closing: `<registry/release/issue URL or command>`.
```

Keep this context brief. It should guide the review, not replace the review method.

## Release Gate 2.0 Matrix

Fill this before claiming a change is release-ready. Use "n/a" only when the project clearly has no such surface.

| Surface | Evidence |
|---|---|
| Review base | Base branch, latest tag, and commit range reviewed |
| Worktree state | Dirty, staged, and untracked files accounted for |
| Remote state | `origin/main` or release branch sync checked |
| Version fields | Manifest, app config, changelog, appcast, and lockfile versions aligned |
| Runtime dependencies | Newly introduced Python packages, CLIs, package managers, and network tools declared and available in CI |
| Generated artifacts | Bundled/minified/archive outputs regenerated or proven not needed |
| Package/archive contents | Built package inspected for required files, newly introduced helpers/references, and missing extras |
| Release assets | GitHub release, appcast, download archive, checksum, or installer assets verified |
| Registry/appcast | npm/crates/Homebrew/appcast/App Store or equivalent state re-read after publish |
| CI status | Latest required checks passed or blocker named |
| Issue/PR state | Target issue or PR re-read before commenting, closing, merging, or saying shipped |

## Safety Sink Review

Any diff that touches one of these sinks needs explicit validation and rollback thinking:

- Deleting, moving, or overwriting user files, caches, history, preferences, or generated outputs.
- Building shell, AppleScript, SQL, URL, or filesystem paths from user input.
- Changing cwd handling, symlink resolution, path traversal guards, sandbox permissions, approval checks, or auth prompts.
- Changing signing, notarization, appcast, update, license, payment, or release asset generation.

Review the smallest entry point that reaches the sink, then the downstream call. If validation is missing or rollback is unclear, treat it as a hard stop.
