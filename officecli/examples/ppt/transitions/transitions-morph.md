# Morph Transition (Office 2016+)

This demo consists of three files that work together:

- **transitions-morph.sh** — Shell script that builds a 4-slide deck demonstrating all three morph granularities (byobject, byword, bychar) using a named `morphBall` ellipse that tweens position and size across slides.
- **transitions-morph.pptx** — The generated 4-slide deck.
- **transitions-morph.md** — This file. Documents morph pairing, the three granularity options, and backwards compatibility.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-morph.sh
# → transitions-morph.pptx
```

## Slides

### Slide 1 — Starting state (no transition)

The entry point. A named yellow `morphBall` ellipse is placed at bottom-left, small.

```bash
officecli add transitions-morph.pptx / --type slide

# Full-bleed dark background
officecli add transitions-morph.pptx '/slide[1]' --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864

# Title label
officecli add transitions-morph.pptx '/slide[1]' --type shape \
  --prop text="Morph" --prop size=72 --prop bold=true --prop color=FFFFFF \
  --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm

# Named morph target — small circle at bottom-left
# name= is the pairing key: same name on slides 1-4 → PowerPoint tweens it
officecli add transitions-morph.pptx '/slide[1]' --type shape \
  --prop shape=ellipse --prop fill=FFC000 --prop name=morphBall \
  --prop x=2cm --prop y=14cm --prop width=3cm --prop height=3cm
```

**Features:** `name=morphBall` (morph pairing key), `shape=ellipse`, `fill=FFC000`

### Slide 2 — morph (default = byobject)

Ball grows and moves to center. The shape `name=morphBall` on both slides triggers Morph to tween the geometry.

```bash
officecli add transitions-morph.pptx / --type slide
officecli set transitions-morph.pptx '/slide[2]' --prop transition=morph

officecli add transitions-morph.pptx '/slide[2]' --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=2E5C8A
officecli add transitions-morph.pptx '/slide[2]' --type shape \
  --prop text="morph (byobject — default)" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=2cm --prop width=29.87cm --prop height=3cm

# Same name, larger and centered → ball grows + moves via morph
officecli add transitions-morph.pptx '/slide[2]' --type shape \
  --prop shape=ellipse --prop fill=FFC000 --prop name=morphBall \
  --prop x=15cm --prop y=10cm --prop width=6cm --prop height=6cm
```

**Features:** `transition=morph` (default = byobject)

### Slide 3 — morph-byword

Text bodies are recomposed word-by-word.

```bash
officecli add transitions-morph.pptx / --type slide
officecli set transitions-morph.pptx '/slide[3]' --prop transition=morph-byword

officecli add transitions-morph.pptx '/slide[3]' --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=4F7C3A
officecli add transitions-morph.pptx '/slide[3]' --type shape \
  --prop text="morph byword tweens words" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=2cm --prop width=29.87cm --prop height=3cm

# Ball continues moving — now at right, small again
officecli add transitions-morph.pptx '/slide[3]' --type shape \
  --prop shape=ellipse --prop fill=FFC000 --prop name=morphBall \
  --prop x=27cm --prop y=14cm --prop width=3cm --prop height=3cm
```

**Features:** `transition=morph-byword`

### Slide 4 — morph-bychar

Text bodies are recomposed character-by-character.

```bash
officecli add transitions-morph.pptx / --type slide
officecli set transitions-morph.pptx '/slide[4]' --prop transition=morph-bychar

officecli add transitions-morph.pptx '/slide[4]' --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=8A5A2B
officecli add transitions-morph.pptx '/slide[4]' --type shape \
  --prop text="bychar tweens letters" --prop size=44 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=2cm --prop width=29.87cm --prop height=3cm

# Ball at center, medium size
officecli add transitions-morph.pptx '/slide[4]' --type shape \
  --prop shape=ellipse --prop fill=FFC000 --prop name=morphBall \
  --prop x=14cm --prop y=14cm --prop width=4cm --prop height=4cm
```

**Features:** `transition=morph-bychar`

## Complete Feature Coverage

| Option | Syntax | Tweens at the level of |
|--------|--------|------------------------|
| byobject (default) | `transition=morph` | Whole shape pairs (matched by name) |
| byword | `transition=morph-byword` | Whole words within text bodies |
| bychar | `transition=morph-bychar` | Individual characters within text bodies |

Input aliases accepted: `object`/`word`/`char`/`character`. `get` returns canonical `byObject`/`byWord`/`byChar`.

## How Shape Pairing Works

Same `name=` on adjacent slides → PowerPoint pairs the shapes and tweens geometry. Without matching names, shapes fade in/out independently.

```bash
# Shape on slide N: small, bottom-left
officecli add deck.pptx /slide[1] --type shape \
  --prop shape=ellipse --prop name=morphBall \
  --prop x=2cm --prop y=14cm --prop width=3cm --prop height=3cm

# Shape on slide N+1: larger, centered → morph animates the change
officecli set deck.pptx /slide[2] --prop transition=morph
officecli add deck.pptx /slide[2] --type shape \
  --prop shape=ellipse --prop name=morphBall \
  --prop x=15cm --prop y=10cm --prop width=6cm --prop height=6cm
```

## Backwards Compatibility

officecli writes morph with an inline fade fallback baked in. Pre-2016 PowerPoint plays the fallback fade — the deck remains openable everywhere.

## Inspect the Generated File

```bash
officecli query transitions-morph.pptx slide
officecli get transitions-morph.pptx /slide[1]
officecli get transitions-morph.pptx /slide[2]
officecli get transitions-morph.pptx "/slide[2]/shape[3]"
```

## Related

- `examples/product_launch_morph.pptx` — a full product-launch deck built with morph as the primary motion
- [transitions-dynamic.md](transitions-dynamic.md) — Office 2010+ "Exciting" gallery
