# Design Aesthetic Quality and Production Structure

## App Shell Rules

When building a sidebar + main workspace layout (Slack, Linear, Notion class):
- Decorative backgrounds default to off
- Surface hierarchy uses background-color steps and shadow only
- All interactive elements get `active:scale-95`
- Button radius is consistent within each component type (pick one: pill, square, or one fixed value, do not mix)
- Commit to a named radius scale before the first component

## Options Guide

When asked for design options, give at least 3 variations spread across genuinely different dimensions:

- **Dimensions to vary**: visual density, typographic personality, color temperature, layout structure, motion character, amount of decoration, level of abstraction
- **Mix approaches**: one option follows existing conventions closely, one remixes the brand DNA, one is deliberately unexpected
- **Progress from basic to bold**: the first option is safe and understandable; later options push further
- Three options that differ only by accent color are not three variations. Vary layout, typeface, motion, surface treatment.

## DESIGN.md Scaffold (Optional, Production UIs)

For a multi-page or production UI, emit a short `DESIGN.md`-style summary before writing the first component. The nine sections:

1. **Visual Theme and Atmosphere**: mood, density, design philosophy in 2-3 sentences
2. **Color Palette and Roles**: semantic name + value + functional role for each color token
3. **Typography Rules**: font family, size scale, weight scale, line-height, letter-spacing
4. **Component Stylings**: buttons (all states), cards (if used), inputs, navigation
5. **Layout Principles**: spacing scale, grid columns, whitespace philosophy
6. **Depth and Elevation**: shadow system or background-color-step system; describe each level
7. **Do's and Don'ts**: 5 to 10 guardrails specific to this project
8. **Responsive Behavior**: breakpoints, how navigation collapses, touch target minimums
9. **Agent Prompt Guide**: color reference (name: value pairs) + 3-5 example component prompts with all values inlined

For a single component or quick prototype, skip this. The three-line thesis is sufficient.

## Pre-Handoff Checklist: Strategic Omissions

Items most frequently missing from AI-generated UIs:

- [ ] **Custom 404 page**: a branded page with a clear path back
- [ ] **Back navigation**: every page reachable by user action must have a path back
- [ ] **Form client-side validation**: inline errors adjacent to each field
- [ ] **Skip-to-content link**: visually hidden `<a href="#main-content">Skip to main content</a>` as first focusable element
- [ ] **Cookie consent**: for EU or California jurisdictions
- [ ] **Footer Privacy and Terms links**

These are not polish items. They are the difference between a demo and a shippable product.

## Reference Material Priority

When source code and a screenshot are both available: read the code. Source files contain exact token values; screenshots require guessing.

When only a URL is provided: fetching returns extracted text only, with no layout information. For visual references, ask for a screenshot rather than inferring from stripped HTML.

## Adding to Existing UI

When extending an existing interface, first understand its visual vocabulary. Match all of the following before writing the first line of new code:
- Copywriting tone and reading level
- Color palette and semantic color roles
- Hover and click states: scale, color shift, underline, background fill
- Animation style: duration, easing, whether interactions bounce or are ease-out
- Shadow and card treatment
- Layout density and whitespace rhythm
- Border radius choices

If swapping in different content would make the new component look out of place, the vocabulary was not matched closely enough.
