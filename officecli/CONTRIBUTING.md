# Contributing to OfficeCLI

> 中文版 / Chinese version: [CONTRIBUTING.zh.md](./CONTRIBUTING.zh.md)

> You must follow the two rules below. Code style, dependencies, tests, and
> docs are handled by the maintainer in post-merge cleanup — do not worry
> about them.

## Rule 1: One PR = one atomic change

A PR must contain exactly one feature or one bug fix that cannot be further
decomposed. If your change can be split into multiple pieces that each have
standalone value, submit each piece as a separate PR.

### Self-check

Before opening the PR, ask your AI tool:

> "Analyze this diff. Can it be decomposed into multiple PRs where each
> could be merged or reverted independently? If yes, list them."

If the answer is "yes, N PRs", split into N PRs before submitting.

### Examples

**✅ Single-PR bugs** — one root cause, one fix
- `Picture added with only 'width' specified gets wrong default height`
- `Body-level find: anchor throws ArgumentException`
- `AddParagraph --index N is off-by-one when the body contains a table`

**✅ Single-PR features** — one coherent capability
- `query ole: list embedded OLE objects with ProgID and dimensions`
- `set wrap/hposition/vposition on floating pictures`

**❌ Must split** — multiple independent changes bundled together
- `Fix picture index bug + add OLE detection + add HTML heading numbering`
  → 3 PRs, zero shared code
- `Add OLE object detection + add EMF→PNG conversion`
  → 2 PRs, two independent layers
- `Add auto aspect ratio + fix index off-by-one + fix line spacing clipping`
  → 3 PRs, three unrelated root causes

**🤔 Judgment calls** — default to splitting
- `Add helper function + its first consumer`
  → 1 or 2 PRs; split if the helper has standalone reuse potential
- `Add read support + add write support for the same property`
  → 1 or 2 PRs; split if you want read to land before write is vetted

## Rule 2: Every PR must include a verifiable validation method

State in the PR description (or a linked issue) how a reviewer can confirm
your change actually works.

### For bug-fix PRs — pick one (in order of preference)

1. **officecli command sequence** showing broken output before and fixed
   output after
2. **Shell or Python script** that reproduces the bug and runs clean after
   the fix
3. **Authoritative reference** showing what the correct behavior should be
   (OOXML spec, Microsoft / ECMA docs, etc.)
4. **Screenshot** — only when the bug is purely visual

### For feature PRs — include at minimum

- **A screenshot** of the feature in action (Word / Excel / PowerPoint
  window, HTML preview, or terminal output)
- Optionally a command sequence showing how to trigger it

### Examples

**Bug fix — command sequence (ideal):**

```bash
# Before my fix:
officecli blank test.docx
officecli add test.docx picture --prop "path=photo-2x1.png" --prop "width=10cm"
officecli query test.docx picture
# → height: "10.2cm"  ❌ WRONG (hardcoded 4-inch default)

# After my fix:
officecli blank test.docx
officecli add test.docx picture --prop "path=photo-2x1.png" --prop "width=10cm"
officecli query test.docx picture
# → height: "5.0cm"   ✓ CORRECT (auto-computed from 2:1 pixel ratio)
```

**Feature — screenshot (ideal):**

> **Heading auto-numbering from style chain**
>
> Before: ![heading-before.png] (plain "Chapter One" with no number)
> After:  ![heading-after.png]  ("1. Chapter One" with auto-numbering span)
>
> How to trigger:
> ```bash
> officecli blank demo.docx
> officecli add demo.docx paragraph --prop "style=Heading1" --prop "text=Chapter One"
> officecli watch demo.docx
> ```

## If you don't follow these rules

The maintainer reserves two options.

### Option A — Reject and ask for resubmission (preferred)

The maintainer closes the PR with a link to this guide and asks you to
resubmit as properly decomposed PRs with validation methods.

**Your credit:** the PR is entirely yours, including the **"Merged"** badge
after resubmission.

### Option B — Cherry-pick the valuable parts (last resort)

If part of your PR is clearly valuable and worth saving, the maintainer runs
`git cherry-pick` on those commits into `main` directly and closes the
original PR.

**Your credit:**
- `git cherry-pick` preserves the original author, so `git log` and
  `git blame` still show you as author of those lines.
- The maintainer's reconcile commit message carries a
  `Co-authored-by: <you> <your-email>` trailer, which counts toward your
  GitHub contribution graph.
- **However, the original PR shows as "Closed" instead of "Merged"**.
