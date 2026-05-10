---
name: write
description: "Strips AI writing patterns and rewrites prose to sound natural in Chinese or English. Only activates on explicit writing or editing requests. Not for code comments, commit messages, or inline docs."
when_to_use: "帮我写, 改稿, 润色, 去AI味, 写一段, 审稿, 文档review, check this document, 推特, twitter, X推文, tweet, social post, 连贯性, 段落连贯, draft, edit text, proofread, sound natural, polish, rewrite"
metadata:
  version: "3.20.0"
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

Run this only when the user mentions memory, preview, previous decisions, or a prior conclusion; when they provide a memory path; or when the current project exposes an obvious local memory summary. Do not hard-code machine-specific memory roots or read raw transcripts.

Read durable context in this order: user-provided path, current project scope, then global preferences. List titles first, then open at most 1-2 relevant summaries. Treat cross-project entries as transferable patterns only.

Map memory types before using them: `decision`, `preference`, and `principle` are voice and format constraints; `pattern` and `learning` are editing checks; `fact` must be verified against current state before it affects factual wording. The supplied text, audience, project docs, current release state, and source material override memory.

For `/write`, durable preferences can set brevity, tone, and social-post shape. They do not override the hard rule to edit in place, keep meaning intact, and avoid change lists unless the user explicitly asks.

## Hard Rules

- **Meaning first, style second.** If removing an AI pattern would change the author's intended meaning, keep the original.
- **No silent restructuring.** Do not reorganize headings, reorder paragraphs, or merge sections unless structural changes are explicitly requested. Edit in place.
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

Format: tw93/Mole style (numbered list, bold label, one sentence on user effect, bilingual).

### Release Notes Pre-flight

Before drafting, gather style references:

1. Read the target project's `CLAUDE.md` for its Release Convention / Release Flow section.
2. Run `gh release view --json body -R <owner>/<repo>` to read the most recent release as a style, length, and density reference.
3. If the user mentions comparing with a sibling project's release style, ask for the `owner/repo` to fetch it: `gh release view --json body -R <owner>/<sibling>`.
4. Match the reference release's item count, sentence length, and tone. Do not invent a new format.

## Document Review Mode

Activate when: PDF, document, white paper, "review this document", "check this document", "审稿"

Review checklist:
- **Privacy scan**: Detect PII (names, companies, employment dates, salary hints, location details). Hard stop if any text implies job seeking, competitor info, or personal data leakage.
- **Tone consistency**: Flag voice shifts, register mismatches, formulaic phrasing. Check for AI patterns using the loaded `write-zh.md` or `write-en.md` rules.
- **Bilingual validation**: For CN/EN pairs, confirm translation accuracy and terminology consistency. Apply Bilingual Review Mode rules.
- **Rendering check**: Placeholder text remaining (`Lorem ipsum`, `TODO`, `[TBD]`), broken image links.

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

Apply the five announcement rules for Tang's projects (Pake, MiaoYan, Mole, Kaku, Waza):
1. **Lead with community**: open with the social anchor (star count, user thanks, whose feedback drove the fix). Changes follow, not lead.
2. **Highlights over completeness**: pick 2 to 4 of the most interesting changes. Dropping whole items is fine.
3. **UX framing**: phrase each point as "你用它的时候..." or "有一种...的感觉", not "这个工具做了...".
4. **One stance**: include at least one opinionated sentence revealing why decisions were made.
5. **Native Chinese rhythm**: use idiomatic phrasing. Avoid translation-sounding terms.

Close casually with an invitation, not a CTA. End with one short sentence inviting readers to try, not "立即升级".

For non-Tang projects or English posts, apply the same structure (community lead, highlights, UX framing, one stance, casual close) adapted to the project's voice.

## Gotchas

| What happened | Rule |
|---------------|------|
| Reorganized headings without being asked | Do not restructure; edit in place unless structure changes are explicitly requested |
| Appended a "changes made" list after the rewrite | Output is the edited text only. No changelog, no commentary. |
| Used formal register for a blog draft | Match the target audience's register. Blog is conversational, not academic. |
| Applied Chinese/English spacing rules to a pure-English text | Bilingual spacing rules (半角/全角) only apply when the text mixes Chinese and English |

## Output

Return only the edited prose. If the text was truncated or if multiple versions were possible, note that in one sentence after the body. Otherwise, no wrapper, no preamble, no postscript.
