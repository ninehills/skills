# Directional Transitions

This demo consists of three files that work together:

- **transitions-directional.sh** — Shell script that generates a 25-slide deck covering all push, wipe, cover, and uncover direction combinations.
- **transitions-directional.pptx** — The generated deck (1 cover + 4 push + 4 wipe + 8 cover + 8 uncover = 25 slides).
- **transitions-directional.md** — This file. Documents the family/direction matrix and combined-token syntax.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-directional.sh
# → transitions-directional.pptx
```

## Slides

### Slide 1 — Cover (no transition)

```bash
officecli add transitions-directional.pptx / --type slide
officecli add transitions-directional.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864
officecli add transitions-directional.pptx /slide[1] --type shape \
  --prop text="Directional Transitions" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
```

### Slides 2–5 — push (4 cardinal directions)

New slide pushes old slide off screen in the specified direction.

```bash
for d in up down left right; do
  officecli add transitions-directional.pptx / --type slide
  # (background + label shapes added identically)
  officecli set transitions-directional.pptx "/slide[N]" --prop transition="push-$d"
done
```

**Features:** `transition=push-up`, `push-down`, `push-left`, `push-right`

### Slides 6–9 — wipe (4 cardinal directions)

A revealing edge wipes across the slide in the specified direction.

```bash
for d in up down left right; do
  officecli set transitions-directional.pptx "/slide[N]" --prop transition="wipe-$d"
done
```

**Features:** `transition=wipe-up`, `wipe-down`, `wipe-left`, `wipe-right`

### Slides 10–17 — cover (8 directions: 4 cardinal + 4 diagonal)

The new slide covers the old slide by sliding in from the given direction or corner.

```bash
for d in up down left right leftup rightup leftdown rightdown; do
  officecli set transitions-directional.pptx "/slide[N]" --prop transition="cover-$d"
done
```

**Features:** `transition=cover-up/down/left/right`, `cover-leftup`, `cover-rightup`, `cover-leftdown`, `cover-rightdown`

### Slides 18–25 — uncover (8 directions, same as cover)

The old slide uncovers (slides off) to reveal the new slide beneath.

```bash
for d in up down left right leftup rightup leftdown rightdown; do
  officecli set transitions-directional.pptx "/slide[N]" --prop transition="uncover-$d"
done
```

**Features:** `transition=uncover-up/down/left/right/leftup/rightup/leftdown/rightdown`

## Complete Feature Coverage

| Family | Directions supported | Total slides |
|--------|---------------------|--------------|
| `push` | up / down / left / right | 4 |
| `wipe` | up / down / left / right | 4 |
| `cover` | 4 cardinal + 4 diagonal corners | 8 |
| `uncover` | 4 cardinal + 4 diagonal corners | 8 |

## Family / Direction Matrix

Direction support is **not uniform**. Mismatching the family triggers an error:

```
Error: Invalid slide direction: 'leftup'. Valid values: left, right, up, down.
```

| Family | Direction set |
|---|---|
| `push` | `up` / `down` / `left` / `right` (4 cardinal only) |
| `wipe` | `up` / `down` / `left` / `right` (4 cardinal only) |
| `cover` | 4 cardinal + `leftup` / `rightup` / `leftdown` / `rightdown` (8 total) |
| `uncover` | same 8 as `cover` |
| `pull` | alias for `uncover` — same XML, canonical readback: `uncover` |

## Combined-Token Syntax

The compound input syntax is `TYPE[-DIR][-SPEED|DUR]`:

```bash
officecli set deck.pptx /slide[2] --prop transition=push-right
officecli set deck.pptx /slide[2] --prop transition=cover-leftdown
officecli set deck.pptx /slide[2] --prop transition=wipe-up-slow
officecli set deck.pptx /slide[2] --prop transition=push-right-1500
```

`Get` returns the canonical full-word form: `push-right`, not `push-r`. Single-letter direction abbreviations (`l`/`r`/`u`/`d`) are accepted on input but always expand on readback.

## Aliases (input only, canonicalize on readback)

- `pull` → reads back as `uncover`
- `l`/`r`/`u`/`d` → expand to `left`/`right`/`up`/`down`

## Inspect the Generated File

```bash
officecli query transitions-directional.pptx slide
officecli get transitions-directional.pptx /slide[2]
officecli get transitions-directional.pptx /slide[10]
officecli get transitions-directional.pptx /slide[18]
```
