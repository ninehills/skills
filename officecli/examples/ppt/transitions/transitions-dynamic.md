# Dynamic / 3D Transitions (Office 2010+ "Exciting")

This demo consists of three files that work together:

- **transitions-dynamic.sh** — Shell script that generates a 24-slide deck covering all Office 2010+ "Exciting" 3D transition tokens across their direction families.
- **transitions-dynamic.pptx** — The generated 24-slide deck.
- **transitions-dynamic.md** — This file. Documents the direction groupings and combined-token syntax.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-dynamic.sh
# → transitions-dynamic.pptx
```

These transitions require PowerPoint 2010 or later. officecli writes each one with an inline fade fallback baked in, so pre-2010 PowerPoint plays a plain fade instead of failing.

## Slides

### Slide 1 — Cover (no transition)

```bash
officecli add transitions-dynamic.pptx / --type slide
officecli add transitions-dynamic.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864
officecli add transitions-dynamic.pptx /slide[1] --type shape \
  --prop text="Dynamic Transitions" --prop size=40 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
```

### Slides 2–7 — LeftRight family (switch, flip, ferris, gallery, conveyor, reveal)

```bash
for t in switch flip ferris gallery conveyor reveal; do
  officecli add transitions-dynamic.pptx / --type slide
  officecli add transitions-dynamic.pptx "/slide[N]" --type shape \
    --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
    --prop fill=2E5C8A
  officecli add transitions-dynamic.pptx "/slide[N]" --type shape \
    --prop text="$t-right" --prop size=40 --prop bold=true \
    --prop color=FFFFFF --prop align=center \
    --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
  officecli set transitions-dynamic.pptx "/slide[N]" --prop transition="$t-right"
done
```

**Features:** `transition=switch-right`, `flip-right`, `ferris-right`, `gallery-right`, `conveyor-right`, `reveal-right` (LeftRight family: `left`/`right` only)

### Slides 8–10 — InOut family (shred, flythrough, warp)

```bash
for t in shred flythrough warp; do
  officecli set transitions-dynamic.pptx "/slide[N]" --prop transition="$t-out"
done
```

**Features:** `transition=shred-out`, `flythrough-out`, `warp-out` (InOut family: `in`/`out` only)

### Slides 11–16 — SlideDir family (vortex, glitter, pan — 4 cardinal)

```bash
for t in vortex glitter pan; do
  for d in up right; do
    officecli set transitions-dynamic.pptx "/slide[N]" --prop transition="$t-$d"
  done
done
```

**Features:** `transition=vortex-up`, `vortex-right`, `glitter-up`, `glitter-right`, `pan-up`, `pan-right` (up/down/left/right)

### Slides 17–19 — Prism family (prism, rotate, orbit)

These three map to the same `<p14:prism>` OOXML element but with different flags.

```bash
officecli set transitions-dynamic.pptx "/slide[17]" --prop transition=prism
# prism (= "Cube" in PowerPoint UI)

officecli set transitions-dynamic.pptx "/slide[18]" --prop transition=rotate
# rotate (= "Rotate" Dynamic Content tile, isContent=1)

officecli set transitions-dynamic.pptx "/slide[19]" --prop transition=orbit
# orbit (= "Orbit" Dynamic Content tile, isContent=1 isInverted=1)
```

**Features:** `transition=prism`, `rotate`, `orbit`; alias `cube` → `prism`

### Slides 20–23 — Horizontal/Vertical orientation (doors, window)

```bash
for t in doors window; do
  for d in horizontal vertical; do
    officecli set transitions-dynamic.pptx "/slide[N]" --prop transition="$t-$d"
  done
done
```

**Features:** `transition=doors-horizontal`, `doors-vertical`, `window-horizontal`, `window-vertical`

### Slides 24–25 — Direction-less (ripple, honeycomb)

```bash
officecli set transitions-dynamic.pptx "/slide[24]" --prop transition=ripple
officecli set transitions-dynamic.pptx "/slide[25]" --prop transition=honeycomb
```

**Features:** `transition=ripple`, `transition=honeycomb`

## Complete Feature Coverage

| Family | Direction set | Tokens |
|--------|---------------|--------|
| LeftRight | `left` / `right` | switch, flip, ferris, gallery, conveyor, reveal |
| InOut | `in` / `out` | shred, flythrough, warp |
| SlideDir (4 cardinal) | `up` / `down` / `left` / `right` | vortex, glitter, pan |
| Prism sub-family | (direction-less) | prism (=cube), rotate, orbit |
| Orientation | `horizontal` / `vertical` | doors, window |
| Direction-less | — | ripple, honeycomb |

## Inspect the Generated File

```bash
officecli query transitions-dynamic.pptx slide
officecli get transitions-dynamic.pptx /slide[2]
officecli get transitions-dynamic.pptx /slide[17]
officecli get transitions-dynamic.pptx /slide[24]
```

## Related

- [transitions-basic.md](transitions-basic.md) — Office 97-era cut/fade/dissolve
- [transitions-morph.md](transitions-morph.md) — Office 2016+ Morph (separate code path)
