# Design Traps and Anti-Patterns

## Common Default Traps

Before submitting, check whether any of the following slipped in without intention:

- A purple or blue gradient over white as the hero background
- A three-part hero: large headline, one-line subtext, two CTA buttons side by side
- A grid of cards with identical rounded corners, identical drop shadows, identical padding
- A top navigation bar with logo left, links center, primary action far right
- Sections that alternate between white and `#f9f9f9`
- A centered icon or illustration sitting above a heading above a paragraph
- A four-column footer with equal-weight columns

Any of these can appear if they serve the design intentionally. They cannot appear by default.

Final test: if you swapped in completely different content and the layout still made sense without changes, you built a template, not a design. Redo it.

## Absolute Bans (CSS-Pattern Level)

| Pattern | Why | Rewrite |
|---|---|---|
| `border-left` or `border-right` wider than 1px as a section accent | The single most overused design touch in admin UIs | Use a colored dot, a short horizontal rule, a background swatch, or a typographic weight shift |
| `background-clip: text` gradient text | Decorative and illegible in high-contrast mode | Use a solid brand color or typographic weight for emphasis |
| `backdrop-filter: blur` glassmorphism as default card surface | Expensive on low-power devices; overused | Use elevated surfaces via background color steps and `box-shadow` |
| Purple-to-blue gradients or cyan-on-dark accent systems | The canonical AI design palette; communicates nothing about the brand | Pick a palette from the brand words via OKLCH rules |
| Generic rounded-rect card with `box-shadow` as the default container | Template thinking | Default to cardless sections; only add card treatment when content type requires it |
| Modals as lazy escape for overflow UI | Interrupts flow and breaks browser back navigation | Inline expand, detail panel, or dedicated route; modals only when action truly requires focus-lock |
| `transition: all` or animating width/height/padding/margin | Forces browser into layout recalculation on every frame | List exact properties; use `grid-template-rows: 0fr to 1fr` for height reveals |

## AI Slop Test

Would a stranger glancing at the first viewport say "an AI made this" immediately? If yes, the committed direction was not committed enough. Usual culprits: reflex font, default purple accent, centered hero with generic card grid beneath. Fix the typography, color system, or layout until the answer flips.

## Content Authenticity

Placeholder copy that looks real but is not real breaks the illusion. Apply these rules before handoff.

**Sample data:** no generic names (John Doe, Jane Smith), no generic company names (Acme Corp, TechCorp), no Lorem Ipsum. Use organic numbers: `99.94%` not `99.99%`, `$99.00` not `$100.00`.

**UI copy:** sentence case on all headings, no exclamation marks on success states, never "Oops!" on errors. Banned words: Elevate, Seamless, Unleash, Delve, Tapestry, Game-changer, Next-Gen.

**Placeholders:** when a component is unavailable, use a labeled placeholder (grey rectangle, monogram wordmark, dashed border). Never draw illustrative imagery with inline SVG.
