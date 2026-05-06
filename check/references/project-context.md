# Project Review Context Template

Use this template to compress repository context before running Waza `/check`. The context must come from public project files, the diff, CI configuration, or explicit user instructions. Do not depend on private machine paths or unpublished project instructions.

## What Belongs In Waza `/check`

- Diff depth classification.
- Scope drift detection.
- Hard stops such as destructive automation, missing release artifacts, generated artifact drift, version skew, unknown identifiers, injection risks, credential leakage, and dependency surprises.
- Security and architecture specialist routing.
- Autofix policy.
- Sign-off format.
- Verification expectations.

## What Belongs In Project Context

- Verification commands discovered from public docs, manifests, Makefiles, scripts, or CI workflows.
- Protected files and directories.
- Generated or bundled artifacts that must stay in sync with source changes.
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
