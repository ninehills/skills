# Rendering Bug Debug Reference

## Rendering Bug Mode

Activate when: "PDF looks wrong", "page break issue", "font not rendering", broken PDF output, print layout wrong.

Static analysis first (CSS review), then reproduce if needed.

### WeasyPrint

- `rgba()` causes double-rectangle bug: use solid hex colors instead
- `page-break-inside: avoid` is often ignored: use explicit breaks
- Float-based layouts often break at page boundaries: prefer flexbox or block
- External font URLs blocked at render time: embed fonts as base64 or host locally

### Font Loading

- Check `@font-face` src paths (relative vs. absolute)
- CORS headers must allow the render origin for external fonts
- Format support: WeasyPrint prefers WOFF/TTF; WOFF2 support depends on version
- Missing font fallback = invisible text or system fallback glyph

### Page Overflow

- Calculate content height vs. page height before debugging line-by-line
- Reduce `line-height`, `padding`, or `margin` to reclaim space
- Orphan/widow control: `orphans: 3; widows: 3` in `@page` body rule

### Browser Print CSS

- Confirm `@media print` rules are present and not overridden
- `@page` margin must account for printer unprintable area (~6mm minimum)
- `break-before: page` / `break-after: page` on section dividers
- Test with `window.print()` in browser DevTools, not just visual preview
