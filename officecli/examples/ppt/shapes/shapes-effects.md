# Shape Effects and Meta

This demo consists of three files that work together:

- **shapes-effects.sh** — Shell script that calls `officecli` commands to generate the deck. It generates a sample PNG inline via a Python heredoc — no external image file is needed.
- **shapes-effects.pptx** — The generated 5-slide deck (autoFit, flipH/V, image fill, 3D bevel/depth/lighting/material, softEdge/link/name/zorder).
- **shapes-effects.md** — This file. Covers shape properties not found in shapes-basic or shapes-connectors.

## Regenerate

```bash
cd examples/ppt
bash shapes/shapes-effects.sh
# → shapes/shapes-effects.pptx
```

## Slides

### Slide 1 — autoFit: Text Overflow Behavior

Three textboxes with the same long text and a fixed height, each using a different `autoFit=` mode.

```bash
officecli create shapes-effects.pptx
officecli open shapes-effects.pptx
officecli add shapes-effects.pptx / --type slide

LONGTEXT='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.'

# autoFit=none — text runs off the box boundary (no shrink, no clip)
officecli add shapes-effects.pptx '/slide[1]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=4in --prop height=1.5in \
  --prop fill=F1FAEE --prop size=18 --prop text="$LONGTEXT" \
  --prop autoFit=none

# autoFit=normal — font size shrinks until all text fits inside the fixed box
officecli add shapes-effects.pptx '/slide[1]' --type textbox \
  --prop x=5in --prop y=1.5in --prop width=4in --prop height=1.5in \
  --prop fill=A8DADC --prop size=18 --prop text="$LONGTEXT" \
  --prop autoFit=normal

# autoFit=shape — box grows taller to accommodate the text at the declared font size
officecli add shapes-effects.pptx '/slide[1]' --type textbox \
  --prop x=9.5in --prop y=1.5in --prop width=4in --prop height=1.5in \
  --prop fill=F4A261 --prop size=18 --prop text="$LONGTEXT" \
  --prop autoFit=shape
```

**Features:** `autoFit` (none — overflows, normal — text shrinks, shape — box grows)

---

### Slide 2 — flipH / flipV: Mirror

Four `rightArrow` shapes showing all combinations of horizontal and vertical flipping.

```bash
officecli add shapes-effects.pptx / --type slide

# Original — no flip flags
officecli add shapes-effects.pptx '/slide[2]' --type shape --prop geometry=rightArrow \
  --prop x=0.5in --prop y=2in --prop width=2.8in --prop height=1.5in \
  --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop text="original"

# flipH=true — mirror horizontally (arrow points left)
officecli add shapes-effects.pptx '/slide[2]' --type shape --prop geometry=rightArrow \
  --prop x=4in --prop y=2in --prop width=2.8in --prop height=1.5in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop text="flipH=true" \
  --prop flipH=true

# flipV=true — mirror vertically (arrow points down-right)
officecli add shapes-effects.pptx '/slide[2]' --type shape --prop geometry=rightArrow \
  --prop x=7.5in --prop y=2in --prop width=2.8in --prop height=1.5in \
  --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop text="flipV=true" \
  --prop flipV=true

# flipH + flipV together — visually 180° rotation, stored as flip flags not rotation
officecli add shapes-effects.pptx '/slide[2]' --type shape --prop geometry=rightArrow \
  --prop x=11in --prop y=2in --prop width=2.8in --prop height=1.5in \
  --prop fill=F4A261 --prop color=000000 --prop bold=true --prop text="flipH + flipV" \
  --prop flipH=true --prop flipV=true
```

**Features:** `flipH` (alias: flipHorizontal), `flipV` (alias: flipVertical). Flip flags are stored independently of `rotation=`, so `flipH=true` + `rotation=90` chains predictably.

---

### Slide 3 — image=: Picture as Shape Fill (blipFill)

Three shapes with different geometries all filled with an image. The geometry preset clips the bitmap to its outline.

```bash
officecli add shapes-effects.pptx / --type slide

# Ellipse with image fill + outline border — image clipped to ellipse shape
officecli add shapes-effects.pptx '/slide[3]' --type shape --prop geometry=ellipse \
  --prop x=0.5in --prop y=1.5in --prop width=3.5in --prop height=3.5in \
  --prop image="$SAMPLE_PNG" \
  --prop lineColor=1D3557 --prop lineWidth=3pt

# Star5 — image clipped to a 5-pointed star
officecli add shapes-effects.pptx '/slide[3]' --type shape --prop geometry=star5 \
  --prop x=4.5in --prop y=1.5in --prop width=3.5in --prop height=3.5in \
  --prop image="$SAMPLE_PNG"

# Diamond with image fill + outline
officecli add shapes-effects.pptx '/slide[3]' --type shape --prop geometry=diamond \
  --prop x=8.5in --prop y=1.5in --prop width=3.5in --prop height=3.5in \
  --prop image="$SAMPLE_PNG" \
  --prop lineColor=1D3557 --prop lineWidth=3pt
```

**Features:** `image` (file path to PNG/JPG — embeds the image as a blipFill inside the shape geometry; the geometry clips the image). This is distinct from `--type picture`, which embeds the bitmap with its native bounding box.

---

### Slide 4 — 3D Effects: Bevel / Depth / Lighting / Material

Six shapes demonstrating bevel types, extrusion depth, and lighting/material combinations.

```bash
officecli add shapes-effects.pptx / --type slide

# bevel=circle — top bevel only, default size
officecli add shapes-effects.pptx '/slide[4]' --type shape --prop geometry=roundRect \
  --prop x=0.5in --prop y=1.4in --prop width=3in --prop height=1.8in \
  --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop size=14 \
  --prop text='bevel=circle' \
  --prop bevel=circle

# bevel with explicit dimensions + separate bottom bevel
# bevel=TYPE-W-H: bevel type, width in pt, height in pt
officecli add shapes-effects.pptx '/slide[4]' --type shape --prop geometry=roundRect \
  --prop x=4in --prop y=1.4in --prop width=3in --prop height=1.8in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop size=14 \
  --prop text='bevel=angle-8-4 + bevelBottom=circle-4-4' \
  --prop bevel=angle-8-4 --prop bevelBottom=circle-4-4

# depth= — extrusion thickness (pt-suffixed)
officecli add shapes-effects.pptx '/slide[4]' --type shape --prop geometry=roundRect \
  --prop x=7.5in --prop y=1.4in --prop width=3in --prop height=1.8in \
  --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=14 \
  --prop text='depth=14pt + bevel=softRound' \
  --prop depth=14pt --prop bevel=softRound

# lighting=threePt material=metal
officecli add shapes-effects.pptx '/slide[4]' --type shape --prop geometry=ellipse \
  --prop x=0.5in --prop y=3.7in --prop width=3in --prop height=1.8in \
  --prop fill=F4A261 --prop color=000000 --prop bold=true --prop size=12 \
  --prop text='lighting=threePt material=metal' \
  --prop bevel=circle-8 --prop depth=10 --prop lighting=threePt --prop material=metal

# lighting=balanced material=plastic
officecli add shapes-effects.pptx '/slide[4]' --type shape --prop geometry=ellipse \
  --prop x=4in --prop y=3.7in --prop width=3in --prop height=1.8in \
  --prop fill=A8DADC --prop color=000000 --prop bold=true --prop size=12 \
  --prop text='lighting=balanced material=plastic' \
  --prop bevel=circle-6 --prop depth=8 --prop lighting=balanced --prop material=plastic

# lighting=harsh material=warmMatte
officecli add shapes-effects.pptx '/slide[4]' --type shape --prop geometry=ellipse \
  --prop x=7.5in --prop y=3.7in --prop width=3in --prop height=1.8in \
  --prop fill=FFD700 --prop color=000000 --prop bold=true --prop size=12 \
  --prop text='lighting=harsh material=warmMatte' \
  --prop bevel=circle-6 --prop depth=8 --prop lighting=harsh --prop material=warmMatte
```

**Features:** `bevel` (TYPE or `TYPE-W` or `TYPE-W-H`; preset types: circle, angle, softRound, convex, coolSlant, cross, divot, hardEdge, relaxedInset, riblet, slope), `bevelBottom` (same syntax, sets the bottom face bevel), `depth` (extrusion in pt), `lighting` (threePt, balanced, harsh, flat, softAmbient, …), `material` (metal, plastic, warmMatte, matte, powder, translucentPowder, …)

---

### Slide 5 — softEdge / link / name / zorder

Feathered edges, clickable hyperlinks with tooltip, named addressable shapes, and explicit z-order stacking.

```bash
officecli add shapes-effects.pptx / --type slide

# softEdge — feathered/blurred edge in points; 0 = sharp
officecli add shapes-effects.pptx '/slide[5]' --type shape --prop geometry=ellipse \
  --prop x=0.5in --prop y=1.5in --prop width=3in --prop height=2in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
  --prop text='softEdge=0  (sharp)' --prop softEdge=0

officecli add shapes-effects.pptx '/slide[5]' --type shape --prop geometry=ellipse \
  --prop x=4in --prop y=1.5in --prop width=3in --prop height=2in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
  --prop text='softEdge=8pt' --prop softEdge=8pt

officecli add shapes-effects.pptx '/slide[5]' --type shape --prop geometry=ellipse \
  --prop x=7.5in --prop y=1.5in --prop width=3in --prop height=2in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
  --prop text='softEdge=20pt  (heavy feather)' --prop softEdge=20pt

# link + tooltip — makes the entire shape a clickable hyperlink
officecli add shapes-effects.pptx '/slide[5]' --type shape --prop geometry=roundRect \
  --prop x=0.5in --prop y=4in --prop width=4in --prop height=1in \
  --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=16 \
  --prop text="Click me → example.com" \
  --prop link=https://example.com --prop tooltip="Open example.com" \
  --prop name="cta-button"

# zorder — three overlapping rects with explicit stack depth
officecli add shapes-effects.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=8in --prop y=4in --prop width=2.5in --prop height=2.5in \
  --prop fill=4472C4 --prop name="back" --prop zorder=1 \
  --prop color=FFFFFF --prop bold=true --prop text="back (zorder=1)"

officecli add shapes-effects.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=9in --prop y=4.5in --prop width=2.5in --prop height=2.5in \
  --prop fill=E63946 --prop name="middle" --prop zorder=2 \
  --prop color=FFFFFF --prop bold=true --prop text="middle (zorder=2)"

officecli add shapes-effects.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=10in --prop y=5in --prop width=2.5in --prop height=2.5in \
  --prop fill=F4A261 --prop name="front" --prop zorder=3 \
  --prop color=000000 --prop bold=true --prop text="front (zorder=3)"

officecli close shapes-effects.pptx
officecli validate shapes-effects.pptx
```

**Features:** `softEdge` (pt-suffixed radius; 0 = sharp), `link` (URL, `slide[N]` in-deck jump, or named action like `nextslide`), `tooltip` (hover text on hyperlinked shapes), `name` (stable identifier; addressable as `/slide[N]/shape[@name=...]`), `zorder` (integer stack depth; aliases: z-order, order)

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **autoFit:** none (overflow), normal (shrink text), shape (grow box) | 1 |
| **flipH / flipV:** mirror horizontal/vertical, independent of rotation | 2 |
| **image=:** blipFill — image clipped to any geometry preset | 3 |
| **bevel:** circle, angle, softRound, convex, and more (`TYPE-W-H` form) | 4 |
| **bevelBottom:** separate bottom-face bevel | 4 |
| **depth=:** 3D extrusion thickness in pt | 4 |
| **lighting:** threePt, balanced, harsh, flat, softAmbient | 4 |
| **material:** metal, plastic, warmMatte, matte, powder | 4 |
| **softEdge=:** feathered boundary radius in pt | 5 |
| **link=:** URL / `slide[N]` jump / named action | 5 |
| **tooltip=:** hover text for hyperlinked shapes | 5 |
| **name=:** stable @name addressing | 5 |
| **zorder=:** explicit stack depth (aliases: z-order, order) | 5 |

## Inspect the Generated File

```bash
# Check autoFit mode on each textbox (slide 1)
officecli get shapes-effects.pptx '/slide[1]/shape[1]'
officecli get shapes-effects.pptx '/slide[1]/shape[3]'

# Read back flip flags on slide 2
officecli get shapes-effects.pptx '/slide[2]/shape[2]'
officecli get shapes-effects.pptx '/slide[2]/shape[4]'

# Get bevel/depth/lighting details on slide 4
officecli get shapes-effects.pptx '/slide[4]/shape[1]'
officecli get shapes-effects.pptx '/slide[4]/shape[4]'

# Verify link, tooltip, name on the CTA button
officecli get shapes-effects.pptx '/slide[5]/shape[@name=cta-button]'

# Check zorder stack on slide 5
officecli get shapes-effects.pptx '/slide[5]/shape[@name=back]'
officecli get shapes-effects.pptx '/slide[5]/shape[@name=front]'
```
