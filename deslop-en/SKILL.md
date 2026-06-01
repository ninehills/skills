---
name: deslop-en
description: "Remove AI writing patterns from English prose. Covers blog posts, technical docs, social copy, release notes, scientific writing. Trigger: deslop, de-AI, make it sound human, remove AI patterns, fix slop, polish, rewrite, sound natural"
metadata:
  trigger: deslop, de-AI, make it sound human, remove AI patterns, fix slop, sound natural, polish, rewrite, humanize, de-slop
  sources:
    - https://github.com/hardikpandya/stop-slop
    - https://github.com/stephenturner/skill-deslop
    - https://tropes.fyi
    - https://github.com/coderjatin/anti-slop-writing
---

# Deslop: Remove AI Writing Patterns from English Prose

## Core Principles

1. **Naturalness first.** Don't add slang or forced quirkiness to "sound human." If it's already clean, don't touch it.
2. **Subtraction over addition.** Delete before you tweak. Kill filler, adverbs, and throat-clearing before adjusting rhythm.
3. **Preserve meaning.** If removing a pattern alters the author's intent, keep the original.
4. **Match register to context.** A blog post and a grant proposal have different standards.

---

## Quick Checklist

- [ ] Any adverbs (-ly words)? Delete them.
- [ ] Any passive voice? Find the actor, make them the subject.
- [ ] Inanimate thing doing a human verb ("the decision emerges")? Name the person.
- [ ] Any "here's what/this/that" throat-clearing? Cut to the point.
- [ ] Any "not X, it's Y" contrasts? State Y directly.
- [ ] Any self-posed rhetorical question answered immediately? Fold into a statement.
- [ ] Three consecutive sentences match length? Break one.
- [ ] Paragraph ends with a punchy one-liner? Vary it.
- [ ] Em dash anywhere? Replace with comma, period, or parenthetical.
- [ ] Vague declarative ("The implications are significant")? Name the specific implication.
- [ ] Meta-joiners ("The rest of this essay...")? Delete.
- [ ] Any "Let's break this down" / "Let's dive in" pedagogical hand-holding? Delete.
- [ ] Tricolon stacking? Use two items or one.
- [ ] Every list item starts with bold keyword? Reformulate.

---

## AI Writing Patterns to Eliminate

### Throat-Clearing Openers
Remove announcement phrases. State content directly.

> ❌ Here's the thing: / The truth is, / Let me be clear / It turns out / I'm going to be honest / Can we talk about / Here's the kicker / Here's where it gets interesting
> ✅ State the point without preamble.

### Adverbs
**Kill all -ly words.** Softeners, intensifiers, hedges — all of them.

> ❌ really, just, literally, genuinely, honestly, simply, actually, deeply, truly, fundamentally, inherently, inevitably, interestingly, importantly, crucially, quietly, remarkably, arguably, certainly

Also cut these filler phrases:
> ❌ At its core / In today's [X] / It's worth noting / It bears mentioning / Notably / At the end of the day / When it comes to / In a world where / The reality is

### Passive Voice & False Agency
Every sentence needs a human subject doing something. Inanimate objects don't perform human verbs.

> ❌ "the complaint becomes a fix" → Someone fixed it.
> ❌ "a bet lives or dies in days" → Someone kills or ships the project.
> ❌ "the decision emerges" → Someone decides.
> ❌ "the culture shifts" → People change behavior.
> ❌ "the data tells us" → Someone reads the data and concludes.
> ❌ "the market rewards" → Buyers pay for things.

✅ Name the actor. "The team fixed it that week" beats "the complaint becomes a fix." If no specific person fits, use "you" or "we."

### Binary Contrasts (Negative Parallelism)
The most common AI tell. Creates false drama by framing everything as a surprising reframe.

> ❌ "It's not X — it's Y." / "Not because X. Because Y." / "The question isn't X. It's Y." / "It feels like X. It's actually Y." / "stops being X and starts being Y"
> ✅ State Y directly. "The problem is Y." Drop the negation entirely.

### Negative Listing
Listing what something is *not* before revealing what it *is*.

> ❌ "Not a X. Not a Y. A Z." / "It wasn't X. It wasn't Y. It was Z." / "Not ten. Not fifty. Five hundred."
> ✅ State Z directly.

### Self-Posed Rhetorical Questions
Asking a question nobody was asking, then answering it for dramatic effect.

> ❌ "The result? Devastating." / "The worst part? Nobody saw it coming." / "What if I told you..."
> ✅ State it directly: "The result was devastating."

### Tricolon Abuse
Overuse of the rule-of-three pattern.

> ❌ "Products impress people; platforms empower them. Products solve problems; platforms create worlds. Products scale linearly; platforms scale exponentially."
> ✅ Use two items or one. A single tricolon is fine. Back-to-back tricola are pattern recognition failure.

### Anaphora Abuse
Repeating the same sentence opener multiple times.

> ❌ "They assume that users will pay... They assume that developers will build... They assume that ecosystems will emerge..."
> ✅ Vary sentence openings. Combine related points.

### Dramatic Fragmentation
Sentence fragments for manufactured emphasis.

> ❌ "[Noun]. That's it. That's the [thing]." / "He published this. Openly. In a book. As a priest." / "Platforms do."
> ✅ Use complete sentences. Trust content over presentation.

### Meta-Commentary
Self-referential asides that announce structure instead of moving forward.

> ❌ "In this section, we'll explore..." / "As we'll see..." / "Let me walk you through..." / "The rest of this essay explains..." / "And so we return to where we began."
> ✅ Delete. Let the essay move without announcing itself.

### The "Serves As" Dodge
Replace pompous alternatives to "is" or "are" with the simple copula.

> ❌ serves as / stands as / marks / represents / constitutes / boasts (as 'has') / features (as 'has')
> ✅ is / are / has

### Emphasis Crutches
These add no meaning.

> ❌ Full stop. / Period. / Let that sink in. / This matters because. / Make no mistake. / Here's why that matters.
> ✅ Delete.

### Pedagogical Hand-Holding
Phrases that assume the reader needs a teacher.

> ❌ Let's break this down. / Let's unpack this. / Let's dive in. / Think of it as... / Imagine a world where...
> ✅ State the information directly.

### Superficial Participle Analyses
Tacking "-ing" phrases onto sentences for shallow analysis.

> ❌ "contributing to the region's rich cultural heritage" / "highlighting its importance" / "underscoring the need"
> ✅ If the analysis applies to literally any subject, it adds nothing. Delete it or replace with specific evidence.

### False Ranges
Using "from X to Y" where no real spectrum exists between X and Y.

> ❌ "From innovation to implementation to cultural transformation."
> ✅ Only use "from X to Y" when there's a real, identifiable midpoint on a real scale.

### Listicle in a Trench Coat
Numbered points disguised as continuous prose.

> ❌ "The first wall is the absence of a free API... The second wall is the lack of delegated access..."
> ✅ If a list is genuinely needed, use a list. Don't disguise it as prose paragraphs.

### Notability Overemphasis
Listing every source that covered the topic to prove it deserves to be written about.

> ❌ "Her views have been featured in Forbes, TechCrunch, Wired, and The New York Times..."
> ✅ Mention sources only where they naturally belong in the narrative. Don't recite a press list.

### Vague Attribution
Hiding behind unnamed authorities.

> ❌ "Experts argue..." / "Many believe..." / "Studies show..." / "It is widely regarded..." / "Observers note..."
> ✅ Name the source, or don't invoke one at all.

### The "Despite Its... Faces Challenges" Formula
A formulaic challenges-and-future-prospects ending.

> ❌ "Despite its industrial prosperity, Korattur faces challenges typical of urban areas... With its strategic location and ongoing initiatives, Korattur continues to thrive."
> ✅ Name specific problems with specific evidence. Don't follow with vague optimism about "ongoing initiatives."

### False Vulnerability / Performative Emphasis
Simulated self-awareness or manufactured sincerity.

> ❌ "And yes, I'm openly..." / "This is not a rant; it's a diagnosis" / "I promise" / "They exist, I promise" / "creeps in"
> ✅ Delete.

### Elegant Variation
AI cycles through synonyms to avoid repeating words, even when repetition is clearer.

> ❌ "Soviet artistic constraints... non-conformist artists... their creativity... state-imposed artistic norms... the artistic aspirations..."
> ✅ Repeat words when clarity demands it. "The Soviet government told artists what they could and couldn't paint. Yankilevsky painted what he wanted anyway."

### Patronizing Analogies
Assuming the reader needs a dumbed-down metaphor.

> ❌ "Think of it as a Swiss Army knife." / "Think of it like a highway system for data."
> ✅ Explain the concept directly unless the analogy carries real explanatory weight.

### Bold-First Bullets
Every list item starting with a bolded keyword followed by a colon.

> ❌ `- **SEO:** Traditional methods for improving visibility...`
> ✅ Write as prose. If a list is genuinely needed, keep it simple without bold headers and colons.

### Em Dash Overuse
AI uses em dashes (—) where humans use commas, parentheses, or periods.

> ❌ More than one em dash per 500 words is a tell.
> ✅ Use periods and start new sentences. Use commas for asides.

---

## Genre-Specific Guidelines

### Blog Posts & Newsletters
- Put the reader in the room. "You" beats "People." Specifics beat abstractions.
- No narrator-from-a-distance voice ("Nobody designed this" → "You inherit a system that grew by accretion").
- One point per section. Don't restate the same argument ten ways.

### Scientific Writing
- Maintain appropriate formality while cutting hollow filler.
- Use "we" for your own work (no passive evasion).
- Cite specific authors, not "researchers have shown."
- Give concrete numbers: "Each additional week added roughly 15% to the mean WIS."
- No: "In today's rapidly evolving landscape" / "This paradigm shift has far-reaching implications" / "It has long been recognized that" / "It was observed that."

### Release Notes
- Structure: Breaking Changes → New Features → Fixes & Improvements → Deprecations
- Match the target project's most recent release style.
- 5-8 items, one sentence per item, describing user impact.
- No emoji unless the project is explicitly celebratory.

### Social Copy
- Lead with community context (stars, user thanks, whose feedback drove the fix).
- Pick 2-4 highlights, not the full changelog.
- Write from user experience: "When you use it..." not "This tool does..."
- At least one sentence stating a decision rationale.
- End with an invitation, not a CTA.

---

## Vocabulary Replacement Table

| Avoid | Use |
|-------|-----|
| serve as / stand as / marks / represents | is / are |
| boasts / features (as 'has') | has |
| leverage / utilize | use |
| harness | use / apply |
| streamline | simplify |
| navigate (challenges) | handle / address |
| unpack | explain / examine |
| deep dive | analysis / examination |
| delve | examine / look at / explore |
| robust | strong / solid / reliable |
| paradigm | model / system / approach |
| synergy | cooperation / combined effect |
| ecosystem | system / field / community |
| landscape | situation / field / area |
| tapestry | mix / combination / range |
| crucial / pivotal / vital | important / key |
| profound / indelible / paramount | significant (or delete) |
| showcase | show / display |
| underscore / highlight | point out / show |
| foster / cultivate | encourage / build |
| garner | get / receive |
| bolster | support / boost |
| facilitate | help / enable |
| consequently / thus / hence | so |
| furthermore / moreover / additionally | also / and |
| nonetheless / nevertheless | still / but |

**Hard ban (never use):**
- delve, tapestry, nuanced, certainly, realm, nexus, interplay, cornerstone, beacon, pillar, catalyst, crucible, confluence, odyssey, multifaceted, holistic, compelling
- quietly orchestrating / fundamentally reshape / navigate the landscape / rich cultural heritage / diverse array
- groundbreaking, transformative, indelible, paramount, indispensable, quintessential
- showcases, fosters, garners, bolsters, spearheads, galvanizes
- In today's fast-paced world / In the ever-evolving landscape / It's worth noting / Without further ado

---

## Execution Flow

1. **Identify the genre.** Blog? Scientific? Release notes? Lock in the register.
2. **Preserve meaning.** Don't sacrifice accuracy for style.
3. **Subtract first:**
   - Kill throat-clearing openers, meta-commentary, emphasis crutches
   - Kill all adverbs
   - Fix binary contrasts (just state Y)
   - Fix passive voice / false agency (name the actor)
   - Kill "serves as" and other pompous copulas
   - Kill vague declaratives
4. **Then smooth:** Vary sentence length, break tricola, remove em dashes.
5. **Final readthrough:** Fix anything unnatural. Don't force changes on already-clean sentences.
6. **Deliver:** Output the rewritten text only. No change lists, no commentary, no closers — unless explicitly asked.

---

## Examples

### Example 1: Blog Post

**Before:**
> Here's the thing: building reliable distributed systems is genuinely hard. It's not about making individual components bulletproof — it's about designing for partial failure from the ground up. Let that sink in. The implications are significant.

**After:**
> Distributed systems break in predictable ways. Network partitions, clock drift, and GC pauses each create failure modes that unit tests can't catch. We found three patterns that helped: timeouts with jitter, circuit breakers, and idempotent writes.

### Example 2: Scientific Abstract

**Before:**
> The FluSight initiative serves as a foundational framework for influenza forecasting in the United States, contributing to public health preparedness and underscoring the importance of collaborative forecasting efforts.

**After:**
> The FluSight initiative coordinates influenza forecasting across dozens of modeling groups in the United States. Since 2013, it has standardized targets, submission formats, and evaluation metrics.

### Example 3: Cover Letter

**Before:**
> It's worth noting that these findings have important implications for how we navigate the challenges of forecast ensembling moving forward. Despite these challenges, this work contributes meaningfully to the growing body of literature, highlighting the need for continued evaluation and underscoring the importance of robust benchmarking.

**After:**
> If individual model rankings are unstable across geography and time, ensemble methods that weight models by past performance may not improve on equal-weight approaches.

### Example 4: Vague → Specific

**Before:**
> In today's rapidly evolving genomic landscape, single-cell RNA sequencing has fundamentally reshaped how we think about cellular heterogeneity. This paradigm shift has far-reaching implications for our understanding of disease.

**After:**
> Single-cell RNA sequencing reveals cell-type-specific expression patterns that bulk methods average out. In tumor samples, this distinction matters: rare resistant subpopulations visible in single-cell data disappear in bulk profiles.

---

## Scoring (Optional)

Five dimensions, rate 1-10 each. Revise if below 35/50.

| Dimension | Question |
|-----------|----------|
| Directness | Statements or announcements? |
| Rhythm | Varied or metronomic? |
| Trust | Respects reader intelligence? |
| Authenticity | Sounds human or AI-generated? |
| Density | Anything cuttable remaining? |

---

## Related Skills

- `deslop-zh`：Chinese de-AI writing (Chinese-specific patterns: summary tags, contrast formulas, jargon, punctuation, spacing rules)
