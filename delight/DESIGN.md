---
name: Impeccable
description: Warm-paper editorial sanctuary — committed serif display, one decisive magenta, flat surfaces at rest.

# Colors use OKLCH per `The OKLCH-Only Rule` in §2. Stitch's linter validates
# hex sRGB only, so it will warn on these entries — deliberate trade for one
# source of truth and full wide-gamut fidelity. Our own parser accepts strings.
colors:
  editorial-magenta: "oklch(60% 0.25 350)"
  editorial-magenta-deep: "oklch(52% 0.25 350)"
  warm-ash-cream: "oklch(96% 0.005 350)"
  crisp-paper-white: "oklch(98% 0 0)"
  deep-graphite: "oklch(10% 0 0)"
  soft-charcoal: "oklch(25% 0 0)"
  mid-ash: "oklch(55% 0 0)"
  paper-mist: "oklch(92% 0 0)"
  magenta-whisper: "oklch(60% 0.25 350 / 0.15)"
  magenta-veil: "oklch(60% 0.25 350 / 0.25)"

typography:
  display:
    fontFamily: "Cormorant Garamond, Georgia, serif"
    fontSize: "clamp(2.5rem, 7vw, 4.5rem)"
    fontWeight: 300
    lineHeight: 1
  headline:
    fontFamily: "Cormorant Garamond, Georgia, serif"
    fontSize: "clamp(1.75rem, 4vw, 2.5rem)"
    fontWeight: 400
    lineHeight: 1.2
  title:
    fontFamily: "Cormorant Garamond, Georgia, serif"
    fontSize: "clamp(1.125rem, 2.5vw, 1.75rem)"
    fontWeight: 400
    lineHeight: 1.3
  body:
    fontFamily: "Instrument Sans, system-ui, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.6
  body-lead:
    fontFamily: "Instrument Sans, system-ui, sans-serif"
    fontSize: "1.0625rem"
    fontWeight: 400
    lineHeight: 1.65
  supporting:
    fontFamily: "Instrument Sans, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.6
  label:
    fontFamily: "Instrument Sans, system-ui, sans-serif"
    fontSize: "0.9rem"
    fontWeight: 500
    letterSpacing: "0.05em"
  micro-label:
    fontFamily: "Instrument Sans, system-ui, sans-serif"
    fontSize: "0.6875rem"
    fontWeight: 500
    letterSpacing: "0.1em"
  mono:
    fontFamily: "Space Grotesk, monospace"
    fontSize: "0.75rem"
    fontWeight: 400

rounded:
  none: "0"
  sm: "4px"
  md: "8px"
  lg: "12px"
  xl: "16px"

spacing:
  xs: "8px"
  sm: "16px"
  md: "24px"
  lg: "32px"
  xl: "48px"
  "2xl": "80px"
  "3xl": "120px"

components:
  button-primary:
    backgroundColor: "{colors.deep-graphite}"
    textColor: "{colors.crisp-paper-white}"
    typography: "{typography.label}"
    rounded: "{rounded.none}"
    padding: "16px 48px"
  button-primary-hover:
    backgroundColor: "{colors.editorial-magenta}"
    textColor: "{colors.crisp-paper-white}"
  input-text:
    backgroundColor: "transparent"
    textColor: "{colors.deep-graphite}"
    rounded: "{rounded.sm}"
    padding: "8px 12px"
  card:
    backgroundColor: "{colors.warm-ash-cream}"
    textColor: "{colors.deep-graphite}"
    rounded: "{rounded.md}"
    padding: "24px"
  card-feature:
    backgroundColor: "{colors.crisp-paper-white}"
    textColor: "{colors.deep-graphite}"
    rounded: "{rounded.lg}"
    padding: "48px"
  nav-link:
    textColor: "{colors.deep-graphite}"
    typography: "{typography.body}"
  nav-link-hover:
    textColor: "{colors.editorial-magenta}"
---

# Design System: Impeccable

## 1. Overview: The Editorial Sanctuary

**Creative North Star: "The Editorial Sanctuary"**

The Impeccable site reads more like a printed design publication than a SaaS landing page. Committed typography, generous breathing room, and a single decisive accent that cuts through warm paper. The interface feels **considered, unhurried, and expert** — the work of someone who has made the calls a thousand times and has zero interest in chasing the current AI-tool aesthetic.

The aesthetic philosophy is **restraint in service of craft**. Every element earns its place. Nothing is decorative without function. The palette is dominated by warm paper tones with one vibrant voice. The typography pairs a stately italic serif with a clean neutral sans. Motion is reserved for moments that actually communicate state. The site is the demo — it must pass the same anti-pattern audit it asks its users to run on their own work.

This system explicitly rejects the AI-tool visual vocabulary that surrounds the product: dark mode with purple gradients, neon accents, glassmorphism, glowing cyan-on-black, SaaS hero-metric layouts, and identical-card feature grids. When in doubt, do less than a marketing site would, more than a portfolio would.

**Key Characteristics:**
- Warm off-white paper tones with an almost-imperceptible magenta tint for subliminal palette cohesion.
- A single decisive magenta accent used on no more than 10% of any screen. Its rarity is the point.
- Italic serif for display type; clean neutral sans for body at 1.6+ line-height.
- Sharp, uppercase, letter-tracked primary CTAs — no rounded-rectangle-with-drop-shadow defaults.
- Flat surfaces at rest. Shadows appear only as a response to state (hover, elevation, focus).
- Asymmetric magazine-scale spacing; intentionally skips the 4px step.

## 2. Colors: The Warm-Paper Palette

A two-chord palette: warm paper neutrals carrying a near-invisible magenta tint, plus one decisive accent in the same hue family. No secondary or tertiary accents in the core system — the restraint is doctrinal.

### Primary
- **Editorial Magenta** (oklch(60% 0.25 350)): The one vibrant voice. Primary CTAs, active navigation states, live-state indicators, rare editorial emphasis. Never used as a gradient, never as a background wash, never as text fill. Rarity is the design choice.

### Neutral
- **Warm Ash Cream** (oklch(96% 0.005 350)): Primary page background. Near-white with a near-imperceptible magenta tint that creates subconscious cohesion with Editorial Magenta. Used on `body` and standard surfaces.
- **Crisp Paper White** (oklch(98% 0 0)): Pure background. Used for inverted text moments (white-on-dark CTAs) and surfaces needing maximum contrast. Almost never the page background — too cold alone.
- **Deep Graphite** (oklch(10% 0 0)): Primary text for body copy and headlines. Softer than pure black, reads as confident-but-not-aggressive on warm paper. Background of the primary CTA.
- **Soft Charcoal** (oklch(25% 0 0)): Secondary text — taglines, hook paragraphs, supporting copy. Clearly subordinate to Deep Graphite without being washed out.
- **Mid Ash** (oklch(55% 0 0)): Tertiary text — micro-labels, captions, meta lines, "works with" labels. At small sizes reads as intentionally recessed metadata.
- **Paper Mist** (oklch(92% 0 0)): Hairline borders, section dividers, the barely-visible structural seams.

### Accent Alpha Variants
- **Editorial Magenta Deep** (oklch(52% 0.25 350)): Hover/active state for Editorial Magenta. Small darkening, confirms interaction without shouting.
- **Magenta Whisper** (oklch(60% 0.25 350 / 0.15)): Glow backdrop under accent elements on hover (diffuse shadows only), subtle selection highlights.
- **Magenta Veil** (oklch(60% 0.25 350 / 0.25)): Slightly stronger translucent tint for focus rings and emphasis shells.

### Command Category Tints (fenced — do not extend)
A separate six-tint vocabulary used exclusively to color-code the periodic-table visualization of impeccable's 23 commands. These tints predate the OKLCH system and live in one component. **Do not extend this vocabulary elsewhere.**

- **Create** (bg `#fdf2f8` / border `#ec4899` / text `#be185d`)
- **Evaluate** (bg `#fdf4ff` / border `#d946ef` / text `#a21caf`)
- **Refine** (bg `#eff6ff` / border `#3b82f6` / text `#1d4ed8`)
- **Simplify** (bg `#fffbeb` / border `#f59e0b` / text `#b45309`)
- **Harden** (bg `#f0fdf4` / border `#22c55e` / text `#15803d`)
- **System** (bg `#f5f5f4` / border `#78716c` / text `#44403c`)

### Named Rules

**The One Voice Rule.** Editorial Magenta is the only vibrant color in the system. No supporting accent is added, ever, no matter how much a layout "wants" a second color. If a second emphasis point is needed, use scale or weight, never a second hue.

**The Paper-Not-White Rule.** The page background is Warm Ash Cream, never Crisp Paper White. Pure white is reserved for specific inverted surfaces. Warmth is load-bearing — without it, the site reads as generic and the decisive magenta reads as abrasive rather than decisive.

**The OKLCH-Only Rule.** All new colors must be declared in OKLCH. Legacy hex values exist only in the fenced Command Category Tints. Do not introduce new hex-declared colors into the system.

## 3. Typography: The Italic-and-Ink Voice

**Display Font:** Cormorant Garamond (with Georgia fallback)
**Body Font:** Instrument Sans (with system-ui fallback)
**Label/Mono Font:** Space Grotesk (used as a geometric mono, not for code blocks)

**Character:** The display face is a refined transitional serif used in its **italic** cut — stately without being stuffy, drawing on long-form editorial headline traditions. The body face is a clean neutral sans with subtle geometric warmth, chosen to set long paragraphs without visual overhead. The "mono" is a contemporary grotesque reserved for small labels and metadata where a machine-adjacent feel reinforces the command-line product story.

### Hierarchy

- **Display** (display family, weight 300, italic, clamp(2.5rem, 7vw, 4.5rem), line-height 1): Hero title only. The light weight + italic cursive reads as an author signature rather than a marketing headline.
- **Headline** (display family, weight 400, clamp(1.75rem, 4vw, 2.5rem), line-height 1.2): Section headings. Larger editorial moments.
- **Title** (display family, weight 400, italic, clamp(1.125rem, 2.5vw, 1.75rem), line-height 1.3): Hero tagline / section leads. A quieter second display voice.
- **Body** (body family, weight 400, 1rem, line-height 1.6): Paragraph copy. Capped at 65–75ch for readability.
- **Body Lead** (body family, weight 400, 1rem–1.0625rem, line-height 1.6–1.65): The one or two "lead" paragraphs on each page. Slightly relaxed leading.
- **Supporting** (body family, weight 400, 0.875rem, line-height 1.6): Captions, footnotes, supporting context.
- **Label** (body family, weight 500, 0.9rem, `text-transform: uppercase`, `letter-spacing: 0.05em`): CTA labels. Short, declarative.
- **Micro-Label** (body family, weight 500, 0.625–0.6875rem, `text-transform: uppercase`, `letter-spacing: 0.1em`): "Works with", "What's Included", "v3.0 Changelog".
- **Monospace Meta** (mono family, weight 400–500, 0.6875–0.8125rem): Command names in inline prose, periodic-table tile labels.

### Named Rules

**The Italic-Is-Voice Rule.** Italic is used as a voice choice for display type, not as emphasis within body copy. Body emphasis is carried by weight or by swapping to the mono family (see `<em>` in command menus). Treating italic as emphasis inside paragraphs dilutes the display voice.

**The 1.6 Leading Rule.** Body line-height is 1.6 everywhere. Not 1.5, not 1.7, not "relaxed". This is the load-bearing readability decision — when the site reads as calm and editorial, it's 1.6 doing the work.

**The Fluid-Headlines-Only Rule.** Headings use `clamp()` fluid sizing. Body copy uses fixed `rem` values. Fluid body sizes look clever and feel wrong — they make line-lengths wander off spec.

## 4. Elevation

Flat by default. Depth is conveyed through **state response**, not structural shadow. Surfaces rest on a single tonal layer (Warm Ash Cream); shadows appear only when an element is hovered, deliberately lifted, or requires ambient separation from a busy area.

### Shadow Vocabulary

- **Soft Hover Lift** (`0 4px 24px -4px rgba(0,0,0,0.12), 0 1px 3px rgba(0,0,0,0.06)`): The default hover response on cards and interactive surfaces. Diffuse, offset downward.
- **Lifted Card** (`0 20px 40px rgba(0,0,0,0.08)`): Deliberately elevated content (featured cards, install blocks). Low alpha — never reads as dark.
- **Accent Glow** (`0 20px 60px var(--color-accent-dim)`): Magenta-tinted ambient shadow under the one or two moments that should feel magnetic. Used sparingly — this is the "rare ingredient" of the shadow vocabulary.
- **Tooltip / Popover** (`0 0 20px rgba(0,0,0,0.15)` or `0 2px 8px rgba(0,0,0,0.1)`): Tight shadow for small floating UI.

### Named Rules

**The Flat-By-Default Rule.** Surfaces are flat at rest. If you find yourself adding a shadow to a non-interactive, non-elevated element, stop — you're reaching for Material Design muscle memory. Use a hairline Paper Mist border instead, or no articulation at all.

**The Low-Alpha Rule.** Every shadow in the system uses ≤0.15 alpha on its strongest blur. Higher alphas read as 2014 Material Design drop shadows — an immediate tell that the design wasn't considered.

**The Tinted-Shadow-Only-For-Accent Rule.** Neutral shadows (black alpha) for structure. Colored (magenta-dim) shadows only for the deliberate accent-glow moments. Never tint shadows for decorative effect.

## 5. Components

### Buttons

- **Shape:** Flat and squared by default (`border-radius: 0`). Sharp corners are an explicit editorial choice — the site rejects the rounded-rectangle-with-drop-shadow default that marks most AI-adjacent marketing pages.
- **Primary (hero-cta-combined):** Deep Graphite background, Crisp Paper White text. Padding 16px / 48px (`--spacing-sm` / `--spacing-xl`). Uppercase, `letter-spacing: 0.05em`, weight 500. No border, no shadow at rest.
- **Hover:** `transform: translateY(-2px)` and background shifts to Editorial Magenta. Transition 200ms linear ease. A small confident step up, never a bounce.
- **Focus:** Browser-default focus ring combined with the hover treatment. Visible keyboard focus is required.
- **Secondary:** Inline text link in body copy, weight 500, hover shifts to Editorial Magenta. **No boxed secondary button exists in the system** — the site avoids the "stack of equal-weight CTAs" pattern entirely.
- **Chip (picker overlay):** Radius 3–5px, small padding, mono-family label. Used in the live-mode action selector.

### Cards & Containers

- **Corner Style:** Controlled vocabulary — 4px (chips / inline callouts), 8px (standard cards and card-CTAs), 12px (feature cards, install blocks), 16px (large content frames). No single "rounded-lg" default. Radius is picked per component weight.
- **Background:** Warm Ash Cream or Crisp Paper White depending on layering. Deeper nested surfaces may lift to Paper Mist as a near-imperceptible tone shift.
- **Shadow:** Flat at rest — see Elevation for the shadow vocabulary that applies on hover/lift.
- **Border:** Hairline 1px in Paper Mist when a surface needs articulation without shadow.
- **Internal Padding:** 16–32px for typical cards; large editorial frames 48px+. Padding matches visual weight, not applied uniformly.

### Inputs / Fields

The site is primarily editorial, so inputs are minimal:

- **Email / text field:** Radius 4–6px, hairline Paper Mist border, transparent background. Focus state shifts border to Editorial Magenta with a Magenta Whisper backdrop glow.
- **Combobox / select (filter controls):** Same stroke vocabulary, smaller padding, chevron glyph in Mid Ash.
- **No custom checkbox/radio styling** beyond what the live-mode command picker needs.

### Navigation

- **Site Header:** 62px compact bar, left-aligned brand lockup (monochrome mark + wordmark), right-aligned link cluster.
- **Typography:** Body family, weight 500, 0.9–1rem. Normal case — the header is readable prose, not a set of signals.
- **Default State:** Deep Graphite on Warm Ash Cream.
- **Hover / Active:** Smooth color transition to Editorial Magenta, 200ms. No underline bar at rest; if an active indicator is needed, a thin accent-colored underline appears.
- **Mobile:** Collapses to an icon-triggered drawer when horizontal space is insufficient.

### Periodic Table of Commands (signature component)

A distinctive custom element worth documenting: the 23 commands are laid out as a periodic-table grid of 56×64px tiles, each with a category tint background, category-colored border, atomic number in the top-left (mono family, 7px), a symbol in the center (display family, weight 500, 20px), and a command label in mono below. Hover lifts the tile 2px with a category-colored shadow. Tiles are the one place where the Category Tint vocabulary (see Colors) is used on a colored surface rather than as a text accent.

### Layout & Spacing (fold from spec-absent Layout section)

- **Max width:** Content blocks cap at 900px (`--width-content`); page-level containers at 1400px (`--width-max`). Prose further constrained to 65–75ch.
- **Spacing scale:** 8 / 16 / 24 / 32 / 48 / 80 / 120px (`--spacing-xs` through `--spacing-3xl`). The 4px step is deliberately omitted — this is an editorial scale, not an app-UI scale.
- **Rhythm:** 80–120px between top-level sections, 24–48px between content groups within a section, 6–16px inside tight clusters.
- **Grid:** No traditional column grid. Hero layouts are asymmetric two-column splits. Feature sections use `repeat(auto-fit, minmax(280px, 1fr))` rather than breakpoint-driven columns.
- **Motion:** 150ms for color/opacity, 300–400ms for transforms, 600–1200ms for orchestrated entrances. All use `--ease-out` (`cubic-bezier(0.16, 1, 0.3, 1)`) or `--ease-out-quint`. `prefers-reduced-motion` collapses every non-essential transition.

## 6. Do's and Don'ts

### Do:

- **Do** treat Warm Ash Cream (not Crisp Paper White) as the default page background. Warmth is load-bearing — see The Paper-Not-White Rule.
- **Do** use Editorial Magenta on ≤10% of any given screen. Scarcity is what makes it read as decisive rather than noisy — see The One Voice Rule.
- **Do** set all new colors in OKLCH. Hex is for the fenced Command Category Tints only.
- **Do** use italic display type as a voice, not as emphasis inside paragraphs. Body emphasis is carried by weight.
- **Do** use `clamp()` fluid sizing for headings; use fixed `rem` for body — see The Fluid-Headlines-Only Rule.
- **Do** keep the primary CTA sharp and squared. `border-radius: 0`, uppercase, letter-tracked. This is the editorial signature.
- **Do** use `--ease-out` (`cubic-bezier(0.16, 1, 0.3, 1)`) or `--ease-out-quint` on transitions. Expo-out only.
- **Do** leave surfaces flat at rest. Reach for shadows only on hover or for deliberate elevation — see The Flat-By-Default Rule.
- **Do** respect `prefers-reduced-motion` on every animation.
- **Do** cap body line length at 65–75ch via `max-width`.

### Don't:

- **Don't** use pure black (#000) or pure white (#fff). Always the tinted neutrals (Deep Graphite / Warm Ash Cream / Crisp Paper White).
- **Don't** use `border-left` or `border-right` greater than 1px as a colored stripe on cards, list items, callouts, or alerts. Ever. This is the single most recognizable AI-dashboard tell.
- **Don't** use `background-clip: text` with a gradient. Gradient text is banned across the site. If you want emphasis, use weight or size, never gradient fill.
- **Don't** default to dark mode. The site is light mode because editorial reading is a light-mode activity. Dark mode with glowing accents is the AI-tool aesthetic Impeccable exists to replace.
- **Don't** use glassmorphism (blurred translucent cards, glass borders, glow backgrounds as decoration). It is on PRODUCT.md's explicit anti-reference list.
- **Don't** add a second accent color. If a layout "needs" a second emphasis point, use scale or weight, not hue.
- **Don't** use rounded rectangles with generic drop shadows. That's the "could be any AI output" fingerprint.
- **Don't** use bounce or elastic easing. Real objects decelerate smoothly — expo-out is the signature.
- **Don't** animate layout properties (`width`, `height`, `padding`, `margin`). Use `transform` and `opacity` only.
- **Don't** nest cards inside cards. Flatten the hierarchy.
- **Don't** use identical card grids (same-sized cards with icon + heading + text, repeated endlessly).
- **Don't** use the hero-metric layout template (big number + small label + supporting stats + gradient accent). SaaS cliché.
- **Don't** extend the Command Category Tints vocabulary. Those hex tints are scoped to the periodic-table viz.
- **Don't** hedge in UI copy. "Maybe consider" and "could be helpful" are banned in-product — match PRODUCT.md's expert-decisive voice.
- **Don't** introduce a new spacing token outside the 8/16/24/32/48/80/120 scale. If you need a specific pixel gap, use a literal value rather than polluting the token scale.
