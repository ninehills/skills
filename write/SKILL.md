---
name: write
description: "Rewrites and polishes prose in Chinese or English, removing AI-like wording while preserving intent for drafts, docs, release notes, launch copy, and social posts. Use when users ask 帮我写/改稿/润色/去AI味/写一段/审稿/tweet/rewrite/proofread. Not for code comments, commit messages, or inline docs."
when_to_use: "帮我写, 改稿, 润色, 去AI味, 写一段, 审稿, 文档review, check this document, 推特, twitter, X推文, tweet, social post, 连贯性, 段落连贯, draft, edit text, proofread, sound natural, polish, rewrite"
dispatch_intent: "Writing, editing prose, polish, release notes, launch/social copy, remove AI tone"
---

# Write: Cut the AI Taste

Prefix your first line with 🥷 inline, not as its own paragraph.

Strip AI patterns from prose and rewrite it to sound human. Do not improve vocabulary; remove the performance of improvement.

## Outcome Contract

- Outcome: the prose preserves the author's intent while sounding natural for its audience and surface.
- Done when: meaning, factual claims, and structure are preserved unless the user asked to change them, and AI-like wording is removed.
- Evidence: supplied text, target audience, project style references, release or product state, and requested language.
- Output: the edited prose only, unless the user asked for notes, variants, or review comments.

## Pre-flight

1. **Text present?** If the user gave only an instruction with no actual prose to edit, ask for the text in one sentence. Do not proceed.
2. **Audience locked?** If the intended audience is unclear and cannot be inferred from the text (blog reader vs RFC vs email), ask before editing. Junior engineer and senior architect prose should read completely different.
3. **Language detected from the text being edited**, not the user's command:
   - Contains Chinese characters + release notes or social post mode → load `references/write-zh-release-notes.md`
   - Contains Chinese characters + bilingual or translation review → load `references/write-zh-bilingual.md`
   - Contains Chinese characters (default prose) → load `references/write-zh-prose.md` (quick rules); load `references/write-zh.md` for the full AI-taste pattern catalog
   - Otherwise → load `references/write-en.md`

Read the loaded reference file. Then edit. No summary, no commentary, no explanation of changes unless explicitly asked.

## Durable Context Preflight

See [rules/durable-context.md](../../rules/durable-context.md) for when to read durable context, the read-order budget, and the memory-type mapping.

For `/write`, voice and format constraints are `decision`, `preference`, and `principle` entries; editing checks are `pattern` and `learning`. The supplied text, audience, project docs, current release state, and source material override memory. Durable preferences can set brevity, tone, and social-post shape. They do not override the hard rule to edit in place, keep meaning intact, and avoid change lists unless the user explicitly asks.

## Hard Rules

- **Meaning first, style second.** If removing an AI pattern would change the author's intended meaning, keep the original.
- **No silent restructuring.** Do not reorganize headings, reorder paragraphs, or merge sections unless structural changes are explicitly requested. Edit in place.
- **Artifact-grounded claims.** For launch copy, release notes, social posts, product pages, and public replies, ground factual claims in real source material: current app behavior, screenshots, product page, release page, changelog, issue/PR, or user-provided draft. Do not turn concrete product evidence into generic marketing language.
- **No em-dash.** Never produce em-dash (U+2014 `—`) or en-dash (U+2013 `–`) in Chinese or English output. Em-dash is the strongest AI-tone fingerprint in this style of writing. Use commas, periods, colons, semicolons, or parentheses to break clauses. Hyphen-minus (`-`) inside compound words is allowed; replace it with a space or a period when possible. When editing a draft that contains em-dashes, replace every one before returning the text.
- **Stop after output.** Deliver the rewritten text. Do not append a list of changes, a justification, or a closer.

## Bilingual Review Mode

Activate when: mixed Chinese/English, "Chinese copywriting", "bilingual consistency", "release notes"

**Chinese rules** (from https://github.com/mzlogin/chinese-copywriting-guidelines):
- Space between Chinese and English characters (CN文字EN → CN 文字 EN)
- No mixing of punctuation (Chinese uses 、。？！；：, not commas/periods)
- Consistent terminology across all instances

**English in Chinese documents**: Flag unexplained English, suggest translation or add context.

**Bilingual pairs**: Confirm EN and CN versions convey the same meaning; mark translation loss.

## Release Note Template Mode

Activate when: "release", "changelog", "version", "release notes"

Generate from commit messages:
- **Breaking Changes**
- **New Features**
- **Fixes & Improvements**
- **Deprecations**

Format: target-project style by default. If no project style is available, use numbered items with bold labels, one sentence on user effect, and bilingual output only when the project already uses bilingual release notes.

### Release Notes Pre-flight

Before drafting, gather style references:

1. Read the target project's `CLAUDE.md` for its Release Convention / Release Flow section.
2. Read the target project's existing release source as a style, length, and density reference: changelog, release notes, registry page, appcast, or platform release page.
3. For GitHub projects, `gh release view --json body -R <owner>/<repo>` is the preferred way to read the most recent release when `gh` is available. If the project is not on GitHub, use the release source named by the project docs or user request.
4. If the user mentions comparing with a sibling project's release style, ask for the target identifier or release URL before fetching it.
5. Match the reference release's item count, sentence length, and tone. Do not invent a new format.
6. Keep each release-note item to one sentence unless the reference project clearly does otherwise. Do not add emoji to release prose unless the target surface is explicitly a reaction or celebratory social surface.

### Release Notes Content Rules

- **Group by user-perceivable feature**, not by internal taxonomy. "Polish", "细节打磨", "Misc improvements", "Chores" are not categories users can act on. Group by product surface (Clean / Uninstall / Status / Settings) or by user-visible verb (Faster startup / New keyboard shortcut / Fixed crash on M3).
- **Extract from `git log <last-tag>..HEAD`** rather than from memory. Read every `feat:` and `fix:` commit; do not omit small items just because they look minor in commit form (iOS wrapper support, Dock cleanup, AV-vendor protection boundary are not "minor" from a user point of view).
- **One sentence per item, naming the user-visible change**, not the implementation. "Use `CKDownloadQueue` observer for App Store updates" is not a release note; "App Store updates now run inside the app instead of opening App Store" is.
- **Bilingual structure**: when the project ships bilingual release notes, put the English block and the Chinese block as two parallel sections inside the same release item; do not interleave per bullet. For Sparkle appcast CDATA, separate with `<h4>Changelog</h4>` and `<h4>更新日志</h4>` so the rendered update window shows both.
- **No em-dash** in release prose (covered by the Hard Rule). Use Chinese full-width punctuation in Chinese blocks, ASCII in English blocks.

## Public Reply Mode (GitHub issue / PR)

Activate when: "回复 issue", "reply to PR", "comment on #N", "回 issue", or the user asks for the text of a GitHub issue / PR comment.

Five hard rules for the reply body:

1. **Open with `@<reporter>` + one thanks line.** Match the reporter's language (Chinese → "感谢反馈" / English → "thanks for the detailed report"). No exclamation mark. No emoji. No "🙏".
2. **Then state the cause in one sentence, the impact in one sentence.** No multi-paragraph background, no internal symbol names, no walk-through of the fix.
3. **Then state the ship state**, exactly one of: already shipped in v<X.Y.Z>, fixed on `main` and going out in the next release, planned for v<X.Y.Z>, not planned (with one-line reason and an alternative path). Do not write "already shipped" without release evidence in the current turn.
4. **Two paragraphs maximum**, separated by one blank line. No bullet lists, no section headers, no code blocks except a one-line command when actually needed.
5. **No em-dash.** Use commas, periods, colons. (Covered by the Hard Rule, surfaced again because issue replies attract this pattern.)

The reply is the final user-facing text, not an agent log. Do not write "刚才我判断错了", "前面回复有误", "I re-read it and changed the comment", or any meta narration about your own process. If editing an existing maintainer comment, replace it with the clean final wording as if it were the only comment the user will read.

Before posting, re-read the live issue / PR with `gh issue view <num>` or `gh pr view <num>`. Do not reply from memory; titles, states, and author languages change between sessions.

For paid / subscribed users, acknowledge the purchase relationship and the inconvenience in one phrase, then state the boundary. Do not over-explain. When the current product cannot support their setup, suggest the safest practical path (upgrade macOS, wait for the next release, provide logs, refund route) without arguing.

Closing rule: when closing as `completed`, the comment must independently explain what was fixed and the expected release. When closing as `not planned`, the comment must independently explain the current boundary and an alternative path. Do not rely on prior thread context as the explanation.

## Document Review Mode

Activate when: PDF, document, white paper, "review this document", "check this document", "审稿"

Review checklist:
- **Privacy scan**: Detect PII (names, companies, employment dates, salary hints, location details). Hard stop if any text implies job seeking, competitor info, or personal data leakage.
- **Tone consistency**: Flag voice shifts, register mismatches, formulaic phrasing. Check for AI patterns using the loaded `write-zh.md` or `write-en.md` rules.
- **Bilingual validation**: For CN/EN pairs, confirm translation accuracy and terminology consistency. Apply Bilingual Review Mode rules.
- **Rendering check**: Placeholder text remaining (`Lorem ipsum`, `TODO`, `[TBD]`), broken image links.
- **Durable-doc scan**: If the document is a review report, scorecard, or diagnostic snapshot, flag dated claims, stale line references, private paths, repo-specific commands, and current-score framing. Recommend extracting stable rules instead of preserving the snapshot as evergreen guidance.

Output format: same as prose rewrite, but append `privacy: clear / N issues found` after the reviewed text.

## Paragraph Coherence Mode

Activate when: "连贯性", "段落连贯", "可读性", "coherence", "flow check", "段落顺不顺"

Do not rewrite. Instead, work through each paragraph in sequence:
1. Flag transitions that abruptly shift topic without a signal.
2. Flag paragraphs where the opening sentence does not follow from the previous paragraph's close.
3. Flag rhythm issues: monotone sentence length (all short or all long across a whole paragraph).
4. Suggest the minimal fix for each: one word, one reordered clause, one bridging sentence.

Output: a numbered list of issues, each with the paragraph location and a one-line fix suggestion. Then ask if the user wants any applied.

## Tweet / Social Post Mode

Activate when: "推特", "twitter", "X推文", "tweet", "social post", "折叠长度", "长文推特", "发文"

Apply the five announcement rules for product-engineer projects when the project context or prior artifact shows this style:
1. **Lead with community**: open with the social anchor (star count, user thanks, whose feedback drove the fix). Changes follow, not lead.
2. **Highlights over completeness**: pick 2 to 4 of the most interesting changes. Dropping whole items is fine.
3. **UX framing**: phrase each point as "你用它的时候..." or "有一种...的感觉", not "这个工具做了...".
4. **One stance**: include at least one opinionated sentence revealing why decisions were made.
5. **Native Chinese rhythm**: use idiomatic phrasing. Avoid translation-sounding terms.

Close casually with an invitation, not a CTA. End with one short sentence inviting readers to try, not "立即升级".

For other engineering projects or English posts, apply the same structure (community lead, highlights, UX framing, one stance, casual close) adapted to the project's voice.

## Gotchas

| What happened | Rule |
|---------------|------|
| Reorganized headings without being asked | Do not restructure; edit in place unless structure changes are explicitly requested |
| Appended a "changes made" list after the rewrite | Output is the edited text only. No changelog, no commentary. |
| Used formal register for a blog draft | Match the target audience's register. Blog is conversational, not academic. |
| Applied Chinese/English spacing rules to a pure-English text | Bilingual spacing rules (半角/全角) only apply when the text mixes Chinese and English |
| Polished the user's voice into generic launch copy | Preserve the author's cadence and stance. Use real product artifacts to sharpen facts, not to replace the voice. |
| Drafted release or social copy from memory | Read the current release page, changelog, issue/PR, product page, screenshot, or supplied source before making factual claims. |
| Wrote launch copy in one pass without checking the live screenshots | Iterate: draft, compare against the real product screenshot or page, tighten wording to match what ships, repeat until copy and artifact agree |
| Polished a review report until it sounded timeless | Keep snapshots labeled as snapshots, or distill them into stable rules. Do not make dated claims sound evergreen |

## Output

Return only the edited prose. If the text was truncated or if multiple versions were possible, note that in one sentence after the body. Otherwise, no wrapper, no preamble, no postscript.
