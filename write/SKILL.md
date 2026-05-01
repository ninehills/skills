---
name: write
description: "Strips AI writing patterns and rewrites prose to sound natural in Chinese or English. Only activates on explicit writing or editing requests. Not for code comments, commit messages, or inline docs."
when_to_use: "帮我写, 改稿, 润色, 去AI味, 写一段, draft, edit text, proofread, sound natural, polish, rewrite"
metadata:
  version: "3.18.0"
---

# Write: Cut the AI Taste

Prefix your first line with 🥷 inline, not as its own paragraph.

Strip AI patterns from prose and rewrite it to sound human. Do not improve vocabulary; remove the performance of improvement.

## Pre-flight

1. **Text present?** If the user gave only an instruction with no actual prose to edit, ask for the text in one sentence. Do not proceed.
2. **Audience locked?** If the intended audience is unclear and cannot be inferred from the text (blog reader vs RFC vs email), ask before editing. Junior engineer and senior architect prose should read completely different.
3. **Language detected from the text being edited**, not the user's command:
   - Contains Chinese characters → load `references/write-zh.md`
   - Otherwise → load `references/write-en.md`

Read the loaded reference file. Then edit. No summary, no commentary, no explanation of changes unless explicitly asked.

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
2. Run `gh release view --json body -R <repo>` to read the most recent release as a style, length, and density reference.
3. For tw93 projects, also read one sibling project's latest release (`gh release view --json body -R tw93/<sibling>`) to calibrate cross-project consistency.
4. Match the reference release's item count, sentence length, and tone. Do not invent a new format.

## Gotchas

| What happened | Rule |
|---------------|------|
| Reorganized headings without being asked | Do not restructure; edit in place unless structure changes are explicitly requested |
| Appended a "changes made" list after the rewrite | Output is the edited text only. No changelog, no commentary. |
| Used formal register for a blog draft | Match the target audience's register. Blog is conversational, not academic. |
| Applied Chinese/English spacing rules to a pure-English text | Bilingual spacing rules (半角/全角) only apply when the text mixes Chinese and English |

## Output

Return only the edited prose. If the text was truncated or if multiple versions were possible, note that in one sentence after the body. Otherwise, no wrapper, no preamble, no postscript.
