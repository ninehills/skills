# Public Reply Shape (maintainer, issue or PR)

Reusable by both Triage Mode and Ship / Release Follow-through. Default to this shape unless `AGENTS.md` or `CLAUDE.md` in the target repo contradicts it.

1. Resolve `@<login>` from `gh issue view` / `gh pr view --json author` before posting.
2. **Language:** Match the **opener's** language when it is Chinese or English. If the opener used Japanese or Korean, use English for the maintainer reply unless project docs override.
3. Open with `@<login>` and **at most one** short thanks (`感谢反馈`, `thank you for the report`, etc.). Do **not** add closing thanks stacks (`再次感谢`, `Thanks again`, long courtesy endings).
4. One or two short paragraphs: factual reason, what shipped or what is blocked, no ceremony.
5. Name the exact boundary: already released, fixed on `main` but unreleased, available in nightly/beta/preview, next release, not planned, duplicate, or still needs evidence. Do not write "shipped", "released", or "verified" unless that state was checked in the current turn.
6. Always give a **next step tied to releases or verification**: next App Store or GitHub release, nightly upgrade command, cache path to clear once, or exactly what info is still needed.
7. Prefer **editing** an existing maintainer comment (`PATCH /repos/{owner}/{repo}/issues/comments/{comment_id}`) when updating wording; avoid delete plus repost unless the old text must disappear from history.

## When closing

Close only when the fix is shipped, already available in the latest release, the report is invalid, the report is a duplicate, or the maintainer explicitly asked for closure. Otherwise leave open with the next-release acknowledgement.
