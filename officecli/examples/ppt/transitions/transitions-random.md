# Random Transitions

This demo consists of three files that work together:

- **transitions-random.sh** — Shell script that generates a 4-slide deck demonstrating `newsflash` (fixed legacy) and `random` (randomized each play).
- **transitions-random.pptx** — The generated 4-slide deck.
- **transitions-random.md** — This file. Documents the behavior difference between these two tokens.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-random.sh
# → transitions-random.pptx
```

## Slides

### Slide 1 — Cover (no transition)

```bash
officecli add transitions-random.pptx / --type slide
officecli add transitions-random.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864
officecli add transitions-random.pptx /slide[1] --type shape \
  --prop text="Random Transitions" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
```

### Slide 2 — newsflash

A fixed legacy animation: the new slide spins inward newspaper-style. Pre-2010 OOXML element — no compatibility wrapper needed.

```bash
officecli add transitions-random.pptx / --type slide
officecli add transitions-random.pptx /slide[2] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=C00000
officecli add transitions-random.pptx /slide[2] --type shape \
  --prop text="newsflash" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
officecli set transitions-random.pptx /slide[2] --prop transition=newsflash
```

**Features:** `transition=newsflash` — spin-and-zoom newspaper reveal, fixed animation

### Slides 3–4 — random (re-rolls each play)

PowerPoint picks a random transition at render time. The `.pptx` captures the intent only — the motion you see in Slide Show mode will differ each time you enter presentation mode, even for the same slide.

```bash
# Run Slide Show twice — slides 3 and 4 animate differently each pass
officecli add transitions-random.pptx / --type slide
officecli add transitions-random.pptx /slide[3] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=2E75B6
officecli add transitions-random.pptx /slide[3] --type shape \
  --prop text="random (re-rolls each play)" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
officecli set transitions-random.pptx /slide[3] --prop transition=random

officecli add transitions-random.pptx / --type slide
officecli add transitions-random.pptx /slide[4] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=7030A0
officecli add transitions-random.pptx /slide[4] --type shape \
  --prop text="random (different again)" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
officecli set transitions-random.pptx /slide[4] --prop transition=random
```

**Features:** `transition=random` — intent stored in PPTX, animation chosen by PowerPoint at runtime (different each Slide Show pass)

## Complete Feature Coverage

| Token | Behavior |
|-------|----------|
| `newsflash` | Fixed newspaper spin-zoom (OOXML `<p:newsflash/>`) |
| `random` | PowerPoint picks randomly at play time (`<p:random/>`) |

To experience the randomness: open `transitions-random.pptx`, run Slide Show, exit, run Slide Show again — slides 3 and 4 animate differently each pass.

## Inspect the Generated File

```bash
officecli query transitions-random.pptx slide
officecli get transitions-random.pptx /slide[2]
officecli get transitions-random.pptx /slide[3]
```

## Related

- [transitions-basic.md](transitions-basic.md) — deterministic counterparts (cut/fade/dissolve/flash)
