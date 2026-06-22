# Basic Transitions

This demo consists of three files that work together:

- **transitions-basic.sh** — Shell script that calls `officecli set --prop transition=` to build a 6-slide deck showing cut, fade, dissolve, flash, and the `none` clear.
- **transitions-basic.pptx** — The generated 6-slide deck (1 cover + 5 transition demos).
- **transitions-basic.md** — This file. Documents each basic transition token.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-basic.sh
# → transitions-basic.pptx
```

Open in PowerPoint and step through Slide Show mode to experience the transitions — the differences only show during playback.

## Slides

### Slide 1 — Cover (no transition)

The entry slide has no transition. It establishes the dark navy baseline background.

```bash
officecli add transitions-basic.pptx / --type slide

# Full-bleed background rectangle
officecli add transitions-basic.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864

# Title label
officecli add transitions-basic.pptx /slide[1] --type shape \
  --prop text="Basic Transitions" --prop size=54 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm

# No transition set on slide 1
```

### Slide 2 — cut

Instant swap. No animation frames — the slide replaces the previous one on the next render cycle.

```bash
officecli add transitions-basic.pptx / --type slide
officecli add transitions-basic.pptx /slide[2] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=C00000
officecli add transitions-basic.pptx /slide[2] --type shape \
  --prop text="cut — instant swap" --prop size=54 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm

officecli set transitions-basic.pptx /slide[2] --prop transition=cut
```

### Slide 3 — fade

Pixel cross-fade. Both slides are blended with linearly increasing opacity until the new slide is fully opaque.

```bash
officecli add transitions-basic.pptx / --type slide
officecli add transitions-basic.pptx /slide[3] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=2E75B6
officecli add transitions-basic.pptx /slide[3] --type shape \
  --prop text="fade — pixel cross-fade" --prop size=54 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm

officecli set transitions-basic.pptx /slide[3] --prop transition=fade
```

### Slide 4 — dissolve

Speckle/pixel-noise blend. Random pixels flip from the old slide to the new until the transition is complete. Visually similar to fade but uses noise rather than linear opacity.

```bash
officecli set transitions-basic.pptx /slide[4] --prop transition=dissolve
```

### Slide 5 — flash

The screen flashes white, then the new slide fades in from white. Useful for dramatic or energetic reveals.

```bash
officecli set transitions-basic.pptx /slide[5] --prop transition=flash
```

### Slide 6 — none (clear an existing transition)

Slide 6 is first set to `fade`, then immediately cleared with `transition=none`. The final state has no transition XML element — `none` is a clear verb, not a stored value.

```bash
# Set a transition first (to demonstrate clearing)
officecli set transitions-basic.pptx /slide[6] --prop transition=fade

# Now clear it — slide 6 ends up with NO transition element
officecli set transitions-basic.pptx /slide[6] --prop transition=none
# After this call, get /slide[6] will NOT return a transition key
```

**Features:** `transition=cut`, `transition=fade`, `transition=dissolve`, `transition=flash`, `transition=none` (remove/clear); `fill=` hex on full-bleed rect, `bold=`, `align=center`, `size=`, `x=/y=/width=/height=` in cm

## Complete Feature Coverage

| Token | Effect in playback | OOXML element |
|-------|-------------------|---------------|
| `cut` | Instant swap, zero animation | `<p:cut/>` |
| `fade` | Pixel cross-fade | `<p:fade/>` |
| `dissolve` | Random pixel dissolve | `<p:dissolve/>` |
| `flash` | White flash-through | `<p:flash/>` |
| `none` | Remove transition (clear verb, not stored) | *(removes element)* |

## How `transition=none` clears

```bash
officecli set deck.pptx /slide[6] --prop transition=fade
officecli set deck.pptx /slide[6] --prop transition=none
# Get /slide[6] → no "transition" key returned
```

After the second call, `get` returns no `transition` key — the element is fully removed. Use this to clear any stale transition, including Morph and other wrapped types.

## Related Trios

- [transitions-directional.md](transitions-directional.md) — push / cover / wipe with direction
- [transitions-shapes.md](transitions-shapes.md) — circle / diamond / wheel / zoom
- [transitions-bands.md](transitions-bands.md) — blinds / strips / split / checker
- [transitions-dynamic.md](transitions-dynamic.md) — Office 2010+ "Exciting" gallery
- [transitions-random.md](transitions-random.md) — newsflash / random
- [transitions-timing.md](transitions-timing.md) — speed, duration, advance
- [transitions-morph.md](transitions-morph.md) — Morph (2016+)

## Inspect the Generated File

```bash
officecli query transitions-basic.pptx slide
officecli get transitions-basic.pptx /slide[2]
officecli get transitions-basic.pptx /slide[6]
```
