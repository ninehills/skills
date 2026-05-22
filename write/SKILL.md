---
name: write
description: "Strips AI writing patterns and rewrites prose to sound natural in Chinese or English, including artifact-grounded release, launch, and social copy. Not for code comments, commit messages, or inline docs."
when_to_use: "帮我写, 改稿, 润色, 去AI味, 写一段, 审稿, 文档review, check this document, 推特, twitter, X推文, tweet, social post, 连贯性, 段落连贯, draft, edit text, proofread, sound natural, polish, rewrite"
dispatch_intent: "Writing, editing prose, polish, release notes, launch/social copy, remove AI tone"
---

# Write: Cut the AI Taste

Prefix your first line with 🥷 inline, not as its own paragraph.

Strip AI patterns from prose and rewrite it to sound human. Do not improve vocabulary; remove the performance of improvement.

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
2. Run `gh release view --json body -R <owner>/<repo>` to read the most recent release as a style, length, and density reference.
3. If the user mentions comparing with a sibling project's release style, ask for the `owner/repo` to fetch it: `gh release view --json body -R <owner>/<sibling>`.
4. Match the reference release's item count, sentence length, and tone. Do not invent a new format.
5. Keep each release-note item to one sentence unless the reference project clearly does otherwise. Do not add emoji to release prose unless the target surface is explicitly a reaction or celebratory social surface.

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
