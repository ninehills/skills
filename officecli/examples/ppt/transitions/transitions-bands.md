# Band-Pattern Transitions

This demo consists of three files that work together:

- **transitions-bands.sh** — Shell script that generates a 21-slide deck covering all band/strip orientation and split in/out combinations, plus alias demonstrations.
- **transitions-bands.pptx** — The generated 21-slide deck.
- **transitions-bands.md** — This file. Documents the orientation, corner-direction, and split orient×in/out matrix.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-bands.sh
# → transitions-bands.pptx
```

## Slides

### Slide 1 — Cover (no transition)

```bash
officecli add transitions-bands.pptx / --type slide
officecli add transitions-bands.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864
officecli add transitions-bands.pptx /slide[1] --type shape \
  --prop text="Band Transitions" --prop size=40 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
```

### Slides 2–9 — Orientation modifier (-horizontal / -vertical)

Eight slides: `blinds`, `checker`, `comb`, `bars` — each in both horizontal and vertical orientation.

```bash
for combo in blinds-horizontal blinds-vertical \
             checker-horizontal checker-vertical \
             comb-horizontal comb-vertical \
             bars-horizontal bars-vertical; do
  officecli add transitions-bands.pptx / --type slide
  officecli add transitions-bands.pptx "/slide[N]" --type shape \
    --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
    --prop fill=2E5C8A
  officecli add transitions-bands.pptx "/slide[N]" --type shape \
    --prop text="$combo" --prop size=40 --prop bold=true \
    --prop color=FFFFFF --prop align=center \
    --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
  officecli set transitions-bands.pptx "/slide[N]" --prop transition="$combo"
done
```

The default orientation is `horizontal`; explicit writes round-trip preserving the explicit form.

**Features:** `transition=blinds-horizontal`, `blinds-vertical`, `checker-horizontal`, `checker-vertical`, `comb-horizontal`, `comb-vertical`, `bars-horizontal`, `bars-vertical`

### Slides 10–13 — Corner direction for strips (-leftup / -rightup / -leftdown / -rightdown)

Diagonal strip reveal from a specified corner.

```bash
for d in leftup rightup leftdown rightdown; do
  officecli set transitions-bands.pptx "/slide[N]" --prop transition="strips-$d"
done
```

**Features:** `transition=strips-leftup`, `strips-rightup`, `strips-leftdown`, `strips-rightdown`

### Slides 14–17 — Split orient × in/out matrix

`split` requires both orientation and direction to be specified.

```bash
for orient in horizontal vertical; do
  for io in in out; do
    officecli set transitions-bands.pptx "/slide[N]" --prop transition="split-$orient-$io"
  done
done
```

`split-vertical` (orient without in/out) defaults `dir=in` and round-trips as `split-vertical-in`. Bare `split` reads back as plain `split`.

**Features:** `transition=split-horizontal-in`, `split-horizontal-out`, `split-vertical-in`, `split-vertical-out`

### Slides 18–21 — Alias demonstrations

These four slides show that alias tokens produce identical OOXML as their canonical counterparts.

```bash
officecli set transitions-bands.pptx "/slide[18]" --prop transition=venetian-vertical
# → canonical readback: blinds-vertical

officecli set transitions-bands.pptx "/slide[19]" --prop transition=checkerboard-vertical
# → canonical readback: checker-vertical

officecli set transitions-bands.pptx "/slide[20]" --prop transition=randombar-vertical
# → canonical readback: bars-vertical

officecli set transitions-bands.pptx "/slide[21]" --prop transition=diagonal-leftdown
# → canonical readback: strips-leftdown
```

**Features:** Aliases: `venetian` → `blinds`, `checkerboard` → `checker`, `randombar` → `bars`, `diagonal` → `strips`

## Complete Feature Coverage

| Token family | Orientation/direction modifier | Total |
|--------------|-------------------------------|-------|
| `blinds`, `checker`, `comb`, `bars` | `-horizontal` / `-vertical` | 8 |
| `strips` | `-leftup`, `-rightup`, `-leftdown`, `-rightdown` | 4 |
| `split` | `-horizontal-in/out`, `-vertical-in/out` | 4 |
| Aliases | `venetian`, `checkerboard`, `randombar`, `diagonal` | 4 demo |

## Aliases (input only, canonicalize on readback)

| Input alias | Canonical readback |
|---|---|
| `venetian` | `blinds` |
| `checkerboard` | `checker` |
| `randombar` | `bars` |
| `diagonal` | `strips` |

## Inspect the Generated File

```bash
officecli query transitions-bands.pptx slide
officecli get transitions-bands.pptx /slide[2]
officecli get transitions-bands.pptx /slide[14]
officecli get transitions-bands.pptx /slide[18]
```

## Related

- [transitions-shapes.md](transitions-shapes.md) — circle/diamond/wheel (non-banded geometric family)
- [transitions-directional.md](transitions-directional.md) — push/cover/wipe (cardinal-direction transitions)
