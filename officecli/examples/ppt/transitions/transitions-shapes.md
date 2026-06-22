# Shape-Mask Transitions

This demo consists of three files that work together:

- **transitions-shapes.sh** — Shell script that generates a 12-slide deck covering all shape-mask transition tokens.
- **transitions-shapes.pptx** — The generated deck (1 cover + 4 direction-less + 4 in/out + 5 wheel spokes = 14 slides).
- **transitions-shapes.md** — This file. Documents the three sub-families and their constraints.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-shapes.sh
# → transitions-shapes.pptx
```

## Slides

### Slide 1 — Cover (no transition)

```bash
officecli add transitions-shapes.pptx / --type slide
officecli add transitions-shapes.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864
officecli add transitions-shapes.pptx /slide[1] --type shape \
  --prop text="Shape Transitions" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
```

### Slides 2–5 — Direction-less geometric masks

The new slide reveals through a growing geometric shape. No in/out modifier is accepted.

```bash
for t in circle diamond plus wedge; do
  officecli add transitions-shapes.pptx / --type slide
  officecli add transitions-shapes.pptx "/slide[N]" --type shape \
    --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
    --prop fill=C00000
  officecli add transitions-shapes.pptx "/slide[N]" --type shape \
    --prop text="$t" --prop size=44 --prop bold=true \
    --prop color=FFFFFF --prop align=center \
    --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
  officecli set transitions-shapes.pptx "/slide[N]" --prop transition="$t"
done
```

Passing `-in`/`-out` on a direction-less token is rejected:

```
Error: Transition 'circle' does not accept a direction modifier (got '-in').
Use plain 'transition=circle'.
```

**Features:** `transition=circle`, `transition=diamond`, `transition=plus`, `transition=wedge`

### Slides 6–9 — In/Out direction masks

```bash
for combo in zoom-in zoom-out box-in box-out; do
  officecli add transitions-shapes.pptx / --type slide
  officecli add transitions-shapes.pptx "/slide[N]" --type shape \
    --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
    --prop fill=2E75B6
  officecli add transitions-shapes.pptx "/slide[N]" --type shape \
    --prop text="$combo" --prop size=44 --prop bold=true \
    --prop color=FFFFFF --prop align=center \
    --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
  officecli set transitions-shapes.pptx "/slide[N]" --prop transition="$combo"
done
```

The default is `-in`; bare `zoom`/`box` round-trip as `zoom`/`box`. `zoom-out`/`box-out` round-trip with the suffix intact.

**Note:** `box` is a PowerPoint 2013+ "modern" transition. officecli writes it with an inline fade fallback so pre-2013 PowerPoint plays a graceful fade instead of nothing.

**Features:** `transition=zoom-in`, `zoom-out`, `box-in`, `box-out`

### Slides 10–14 — Wheel spoke counts

The same rotating-spoke wipe animation with different spoke counts.

```bash
for n_spokes in 1 2 3 4 8; do
  officecli add transitions-shapes.pptx / --type slide
  officecli add transitions-shapes.pptx "/slide[N]" --type shape \
    --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
    --prop fill=7030A0
  officecli add transitions-shapes.pptx "/slide[N]" --type shape \
    --prop text="wheel-$n_spokes ($n_spokes spokes)" --prop size=44 --prop bold=true \
    --prop color=FFFFFF --prop align=center \
    --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
  officecli set transitions-shapes.pptx "/slide[N]" --prop transition="wheel-$n_spokes"
done
```

The integer suffix (1–32) is the spoke count, not a duration. To set both: `wheel-8-1500` (8 spokes + 1500 ms duration). `wheel-4` collapses to bare `wheel` on readback.

**Features:** `transition=wheel-1`, `wheel-2`, `wheel-3`, `wheel-4` (=bare `wheel`), `wheel-8`

## Complete Feature Coverage

| Sub-family | Tokens | Direction modifier |
|------------|--------|-------------------|
| Direction-less geometric | `circle`, `diamond`, `plus`, `wedge` | None (error if provided) |
| In/Out direction | `zoom-in`, `zoom-out`, `box-in`, `box-out` | `-in` (default) / `-out` |
| Spoke count | `wheel-1` .. `wheel-8` (any 1–32) | Integer suffix = spoke count |

## Inspect the Generated File

```bash
officecli query transitions-shapes.pptx slide
officecli get transitions-shapes.pptx /slide[2]
officecli get transitions-shapes.pptx /slide[7]
officecli get transitions-shapes.pptx /slide[12]
```

## Related

- [transitions-bands.md](transitions-bands.md) — split-vertical-in / split-horizontal-out (similar in/out modifier)
- [transitions-basic.md](transitions-basic.md) — cut/fade/dissolve baseline
