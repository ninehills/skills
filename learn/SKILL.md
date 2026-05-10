---
name: learn
description: "Runs a six-phase research workflow to turn unfamiliar domains or collected sources into publish-ready output. Not for quick lookups or single-file reads."
when_to_use: "学习一下, 深入研究, 研究一下, 整理成文章, 把这批材料整理, 一站式参考, 一篇就够, 整理成长文, research, deep dive, help me understand, compile sources, unfamiliar domain"
metadata:
  version: "3.15.0"
---

# Learn: From Raw Materials to Published Output

Prefix your first line with 🥷 inline, not as its own paragraph.

Collect, organize, translate, explain, structure. Support the user's thinking; do not replace it.

**Boundary**: single URL that only needs fetching belongs in `/read`. A single URL that needs summary or analysis can use `/read` as the fetch step, but the final answer should satisfy the user's requested summary or analysis. `/learn` is for multi-source research that produces a new structured output.

## Pre-check

Check whether `/read` and `/write` skills are installed (look for their SKILL.md in the skills directories). Warn if missing, do not block:
- `/read` missing -- Phase 1 fetch falls back to native `WebFetch` / `curl`; coverage on paywalled, JS-heavy, and Chinese-platform pages degrades.
- `/write` missing -- Phase 5 AI-pattern stripping falls back to manual scan. Phases 1-4 are unaffected.

## Choose Mode

Ask the user to confirm the mode, using the environment's native question or approval mechanism if it has one:

| Mode | Goal | Entry | Exit |
|------|------|-------|------|
| **Deep Research** | Understand a domain well enough to write about it | Phase 1 | Phase 6: publish-ready draft |
| **Quick Reference** | Build a working mental model fast, no article planned | Phase 2 | Phase 2: notes only |
| **Write to Learn** | Already have materials, force understanding through writing | Phase 3 | Phase 6: publish-ready draft |
| **Canonical Article** | One article that covers a topic so thoroughly readers need nothing else | Phase 1 | Phase 6: single authoritative reference |

If unsure, suggest Quick Reference.

## Canonical Article Mode

Activate when: "一篇就够", "一站式参考", "整理成长文", "目的是大家只需要看这篇就好了", or the user wants a single authoritative reference on a topic.

Goal: after reading the article, no one should need to search for anything else on this topic.

Additional requirements on top of standard Deep Research:
- Every major sub-topic must have its own section; nothing left as a footnote
- Include worked examples, not just principles
- Cover common mistakes and how to avoid them
- Add a "Further Reading" section with the 3-5 sources that go deepest; flag which ones are the best starting points
- Phase 6 self-review must confirm: "Could a reader implement/understand this from this article alone?"

## Phase 1: Collect

Gather primary sources only: papers that introduced key ideas, official lab/product blogs, posts from builders, canonical "build it from scratch" repositories. Not summaries. Not explainers.

Three ordered steps per source -- no shortcuts, no merging:

1. **Discover** -- use an installed search plugin (e.g., PipeLLM) to map the landscape, then deep-search the 2-3 most promising sub-topics. No plugin: use the environment's native web search. Output is a URL list; do not fetch content here.
2. **Fetch** -- every URL goes through `/read`. `/read` already owns the proxy cascade, paywall detection, and platform routing (WeChat, Feishu, PDF, GitHub). `WebFetch` and raw `curl` silently fail on JS-heavy or paywalled sites and skip all of that. If `/read` is missing (Pre-check warned), fall back to native fetch and accept reduced coverage.
3. **File** -- `/read` saves to `~/Downloads/{title}.md` when called from `/learn`. Move each file into a sub-topic directory under the research project after the fetch returns. Move, don't refetch.

Target: 5-10 sources for a blog post, 15-20 for a deep technical survey.

## Phase 2: Digest

Work through the materials. For each piece: read it fully, keep what is good, cut what is not. At the end of this phase, cut roughly half of what was collected.

For key claims, ask before including in the outline:
- Does this idea appear in at least two different contexts from the same source?
- Can this framework predict what the source would say about a new problem?
- Is this specific to this source, or would any expert in the field say the same thing?

Generic wisdom is not worth distilling. Passes two or three: belongs in the outline. Passes one: background material. Passes zero: cut it.

When two sources contradict on a factual claim, note both positions and the evidence each gives. Do not silently pick one.

## Phase 3: Outline

Write the outline for the article. For each section: note the source materials it draws from. If a section has no sources, either it does not belong or a source needs to be found first.

Do not start Phase 4 until the outline is solid.

## Phase 4: Fill In

Work through the outline section by section. If a section is hard to write, the mental model is still weak there: return to Phase 2 for that sub-topic. The outline may change, and that is fine.

Stall signals (any one means the mental model is incomplete for this section):
- You have rewritten the opening sentence three or more times without settling
- The section relies on a single source and you cannot cross-check the claim
- You need a new source that was not collected in Phase 1
- The paragraph makes a claim you could not explain to someone out loud

When stalled: return to Phase 2 for that sub-topic, not for the whole article.

## Phase 5: Refine

Pass the draft with a specific brief:
- Remove redundant and verbose passages without changing meaning or voice
- Flag places where the argument does not flow
- Identify gaps: concepts used before they are explained, claims needing sources

Do not summarize sections the user has not written. Do not draft new sections from scratch. Edits only.

Then strip AI patterns from the draft. If `/write` is installed, invoke it. If not, do it manually: scan for filler phrases, binary contrasts, dramatic fragmentation, and overused adverbs. Cut them without changing meaning.

## Phase 6: Self-review and Publish Readiness

The user reads the entire article linearly before publishing. Not with AI. Mark everything that feels off, fix it, read again. Two passes minimum.

When it reads clean from start to finish, the draft is ready for the user to publish.

**After the user confirms the article is ready to publish, stop.** Do not upload, post, distribute, or perform any publish action unless explicitly asked.

## Gotchas

| What happened | Rule |
|---------------|------|
| Collected 30 secondary explainers instead of primary sources | Phase 1 targets papers, official blogs, and repos by builders. Summaries are not sources. |
| Used `WebFetch` or `curl` on URLs while `/read` was installed | Phase 1 fetch is not optional. `/read` owns the proxy cascade, paywall detection, and platform routing. Bypassing it silently loses coverage on paywalled, JS-heavy, or Chinese-platform pages. |
| Treated a convincing explainer as ground truth | Ask: does this appear in at least two different contexts from the same source? |
| Phase 2 wrote summaries instead of teaching the concept | Digest means building the mental model. Summarizing is not digesting. |
| AI offered to upload the article to a blog or social platform after the user said it was ready | Stop at confirmation. Publishing is the user's action, not yours. |
