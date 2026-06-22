# Basic PPT Shapes — Geometries, Fills, Outlines, Effects, Rotation

This demo consists of three files that work together:

- **shapes-basic.sh** — Shell script that calls `officecli` commands to generate the deck. Each slide's commands are shown as copyable blocks below.
- **shapes-basic.pptx** — The generated 5-slide deck (8 geometry presets, 8 fill types, 6 outline variants, 8 rotation angles, 3 effects, 6 stroke-geometry variants).
- **shapes-basic.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash shapes/shapes-basic.sh
# → shapes/shapes-basic.pptx
```

## Slides

### Slide 1 — Geometry Preset Gallery

One row of 8 shapes, each rendered with a different `geometry=` preset.
Every supported schema-declared preset appears exactly once.

```bash
# Create the file and open a resident session
officecli create shapes-basic.pptx
officecli open shapes-basic.pptx

# Add the slide
officecli add shapes-basic.pptx / --type slide

# Title label
officecli add shapes-basic.pptx '/slide[1]' --type shape \
  --prop text="Geometry Presets" --prop size=28 --prop bold=true \
  --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# One shape per preset — loop adds all 8 at columns 0..7
officecli add shapes-basic.pptx '/slide[1]' --type shape \
  --prop geometry=rect \
  --prop x=0.5in --prop y=1.5in --prop width=1.3in --prop height=1.3in \
  --prop fill=4472C4 --prop color=FFFFFF \
  --prop text="rect" --prop size=11 --prop bold=true

officecli add shapes-basic.pptx '/slide[1]' --type shape \
  --prop geometry=roundRect \
  --prop x=2.05in --prop y=1.5in --prop width=1.3in --prop height=1.3in \
  --prop fill=4472C4 --prop color=FFFFFF \
  --prop text="roundRect" --prop size=11 --prop bold=true

officecli add shapes-basic.pptx '/slide[1]' --type shape \
  --prop geometry=ellipse \
  --prop x=3.6in --prop y=1.5in --prop width=1.3in --prop height=1.3in \
  --prop fill=4472C4 --prop color=FFFFFF \
  --prop text="ellipse" --prop size=11 --prop bold=true

# … triangle, diamond, parallelogram, rightArrow, star5 follow the same pattern
officecli add shapes-basic.pptx '/slide[1]' --type shape \
  --prop geometry=star5 \
  --prop x=11.35in --prop y=1.5in --prop width=1.3in --prop height=1.3in \
  --prop fill=4472C4 --prop color=FFFFFF \
  --prop text="star5" --prop size=11 --prop bold=true
```

**Features:** `geometry` (rect, roundRect, ellipse, triangle, diamond, parallelogram, rightArrow, star5), `fill` (hex color), `color` (text color), `text`, `size`, `bold`, `x`/`y`/`width`/`height` (inch-suffixed dimensions)

---

### Slide 2 — Fill Variations

Eight `roundRect` shapes on one slide — every fill syntax demonstrated side by side.

```bash
officecli add shapes-basic.pptx / --type slide

# Solid hex fill
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=0.5in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
  --prop text='fill=E63946'

# Theme color (accent2 follows the deck's color theme)
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=3.3in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
  --prop fill=accent2 --prop color=FFFFFF --prop bold=true \
  --prop text='fill=accent2'

# Linear gradient: COLOR1-COLOR2-ANGLE
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=6.1in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
  --prop gradient="FF6B6B-4ECDC4-45" --prop color=FFFFFF --prop bold=true \
  --prop text='gradient linear 45°'

# Radial gradient: radial:CENTER-COLOR1-COLOR2
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=8.9in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
  --prop gradient="radial:FFE66D-FF6B35-center" --prop color=000000 --prop bold=true \
  --prop text='gradient radial'

# Pattern fill: preset:foreground:background
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=0.5in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
  --prop pattern="diagBrick:1D3557:F1FAEE" --prop color=FFFFFF --prop bold=true \
  --prop text='pattern diagBrick'

# Solid + opacity (opacity requires fill)
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=3.3in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
  --prop fill=2A9D8F --prop opacity=0.4 --prop color=000000 --prop bold=true \
  --prop text='fill + opacity=0.4'

# No fill — outline only
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=6.1in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
  --prop fill=none --prop line="264653:2.5:solid" --prop color=264653 --prop bold=true \
  --prop text='fill=none + outline'

# Per-stop gradient: COLOR@POSITION (0..100%)
officecli add shapes-basic.pptx '/slide[2]' --type shape --prop geometry=roundRect \
  --prop x=8.9in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
  --prop gradient="FF0000@0-FFD700@40-0000FF@100" --prop color=FFFFFF --prop bold=true \
  --prop text='gradient per-stop'
```

**Features:** `fill` (hex, theme color, none), `gradient` (linear `C1-C2-ANGLE`, radial `radial:C1-C2-center`, per-stop `C@POS-C@POS-C@POS`), `pattern` (`preset:fg:bg` — e.g. diagBrick, wave, dotGrid), `opacity` (0.0–1.0, requires fill), `line` (compound `color:widthPt:dash`)

---

### Slide 3 — Outline Styling

Six shapes demonstrating both the compound and per-attribute line forms, compound strokes, and arrowhead endpoints.

```bash
officecli add shapes-basic.pptx / --type slide

# Compound line form: "color:widthPt:dash"
officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=rect \
  --prop x=0.5in --prop y=1.3in --prop width=3in --prop height=1.2in \
  --prop fill=none --prop line="E63946:3:solid" \
  --prop text='line="E63946:3:solid"' --prop size=12

officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=rect \
  --prop x=4in --prop y=1.3in --prop width=3in --prop height=1.2in \
  --prop fill=none --prop line="1D3557:2:dash" \
  --prop text='line="1D3557:2:dash"' --prop size=12

officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=rect \
  --prop x=7.5in --prop y=1.3in --prop width=3in --prop height=1.2in \
  --prop fill=none --prop line="2A9D8F:2.5:dashDot" \
  --prop text='line="2A9D8F:2.5:dashDot"' --prop size=12

# Per-attribute form: lineColor + lineWidth + lineDash separately
officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=ellipse \
  --prop x=0.5in --prop y=2.9in --prop width=3in --prop height=1.4in \
  --prop fill=FFE66D --prop lineColor=E63946 --prop lineWidth=4pt --prop lineDash=solid \
  --prop text='separate lineColor/lineWidth/lineDash' --prop size=11

# Compound stroke: cmpd=dbl (double line)
officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=ellipse \
  --prop x=4in --prop y=2.9in --prop width=3in --prop height=1.4in \
  --prop fill=A8DADC --prop lineColor=1D3557 --prop lineWidth=6pt --prop cmpd=dbl \
  --prop text='cmpd=dbl (double stroke)' --prop size=11

# Compound stroke: cmpd=tri (triple line)
officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=ellipse \
  --prop x=7.5in --prop y=2.9in --prop width=3in --prop height=1.4in \
  --prop fill=A8DADC --prop lineColor=1D3557 --prop lineWidth=8pt --prop cmpd=tri \
  --prop text='cmpd=tri (triple stroke)' --prop size=11

# Arrowheads on shape outlines (headEnd / tailEnd)
officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=rect \
  --prop x=0.5in --prop y=5.2in --prop width=4in --prop height=0.05in \
  --prop fill=none --prop lineColor=000000 --prop lineWidth=2pt \
  --prop headEnd=triangle --prop tailEnd=arrow

officecli add shapes-basic.pptx '/slide[3]' --type shape --prop geometry=rect \
  --prop x=5in --prop y=5.2in --prop width=4in --prop height=0.05in \
  --prop fill=none --prop lineColor=000000 --prop lineWidth=2pt \
  --prop headEnd=diamond --prop tailEnd=oval
```

**Features:** `line` (compound `color:widthPt:dash`), `lineColor`, `lineWidth` (pt-suffixed), `lineDash` (solid, dash, dashDot, dot, lgDash, sysDash, sysDot), `cmpd` (dbl, tri), `headEnd` / `tailEnd` (none, triangle, arrow, diamond, oval, stealth)

---

### Slide 4 — Rotation + Effects

Eight right-arrow shapes show `rotation=0..270`. Three `roundRect` shapes demonstrate shadow, glow, and reflection effects.

```bash
officecli add shapes-basic.pptx / --type slide

# Rotation — degrees clockwise (0..360); each shape shows its angle as text
officecli add shapes-basic.pptx '/slide[4]' --type shape --prop geometry=rightArrow \
  --prop x=0.5in --prop y=1.3in --prop width=1.4in --prop height=0.8in \
  --prop fill=4472C4 --prop color=FFFFFF --prop bold=true \
  --prop rotation=0 --prop text="0°" --prop size=11

officecli add shapes-basic.pptx '/slide[4]' --type shape --prop geometry=rightArrow \
  --prop x=2.05in --prop y=1.3in --prop width=1.4in --prop height=0.8in \
  --prop fill=4472C4 --prop color=FFFFFF --prop bold=true \
  --prop rotation=30 --prop text="30°" --prop size=11

# … continues for 60, 90, 135, 180, 225, 270

# Shadow effect — color only; handler fills in blur/offset/direction defaults
officecli add shapes-basic.pptx '/slide[4]' --type shape --prop geometry=roundRect \
  --prop x=1in --prop y=3in --prop width=3.5in --prop height=1.8in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop size=14 \
  --prop text='shadow=000000' \
  --prop shadow=000000

# Glow effect — color; handler applies default radius
officecli add shapes-basic.pptx '/slide[4]' --type shape --prop geometry=roundRect \
  --prop x=5.5in --prop y=3in --prop width=3.5in --prop height=1.8in \
  --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=14 \
  --prop text='glow=FFD700' \
  --prop glow=FFD700

# Reflection effect — preset name
officecli add shapes-basic.pptx '/slide[4]' --type shape --prop geometry=roundRect \
  --prop x=10in --prop y=3in --prop width=3in --prop height=1.8in \
  --prop fill=F4A261 --prop color=000000 --prop bold=true --prop size=14 \
  --prop text='reflection=tight' \
  --prop reflection=tight
```

**Features:** `rotation` (degrees 0–360), `shadow` (color or `true` for defaults; Get returns `#RRGGBB-blur-angle-dist-opacity`), `glow` (color or `color-radius-opacity`), `reflection` (tight, half, full)

---

### Slide 5 — Stroke Geometry Details

Six shapes demonstrating `lineCap`, `lineJoin` (incl. miterLimit), and `lineAlign`.

```bash
officecli add shapes-basic.pptx / --type slide

# lineCap — how stroke terminates at endpoints / dash gaps
# Thick dashed stroke makes the difference visible: round/square extend past
# the endpoint, flat cuts exactly at it.
officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=0.5in --prop y=1.5in --prop width=4in --prop height=0.05in \
  --prop fill=none --prop lineColor=1D3557 --prop lineWidth=10pt \
  --prop lineDash=dash --prop lineCap=flat

officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=4.8in --prop y=1.5in --prop width=4in --prop height=0.05in \
  --prop fill=none --prop lineColor=1D3557 --prop lineWidth=10pt \
  --prop lineDash=dash --prop lineCap=round

officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=9.1in --prop y=1.5in --prop width=4in --prop height=0.05in \
  --prop fill=none --prop lineColor=1D3557 --prop lineWidth=10pt \
  --prop lineDash=dash --prop lineCap=square

# lineJoin — corner style on a stroked shape (thick triangle makes it obvious)
officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=triangle \
  --prop x=0.5in --prop y=2.8in --prop width=2.5in --prop height=2in \
  --prop fill=A8DADC --prop lineColor=E63946 --prop lineWidth=12pt \
  --prop lineJoin=round

officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=triangle \
  --prop x=3.5in --prop y=2.8in --prop width=2.5in --prop height=2in \
  --prop fill=A8DADC --prop lineColor=E63946 --prop lineWidth=12pt \
  --prop lineJoin=bevel

officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=triangle \
  --prop x=6.5in --prop y=2.8in --prop width=2.5in --prop height=2in \
  --prop fill=A8DADC --prop lineColor=E63946 --prop lineWidth=12pt \
  --prop lineJoin=miter

# miterLimit — compound form sets join style + limit together
# Expressed in 1/1000ths of a percent; 800000 = 800%
officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=triangle \
  --prop x=0.5in --prop y=5.5in --prop width=2.5in --prop height=1.8in \
  --prop fill=A8DADC --prop lineColor=E63946 --prop lineWidth=8pt \
  --prop lineJoin="miter:800000"

# lineAlign — stroke alignment: ctr (centered on path) vs in (fully inset)
officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=9in --prop y=2.8in --prop width=2in --prop height=2in \
  --prop fill=F4A261 --prop lineColor=1D3557 --prop lineWidth=12pt \
  --prop lineAlign=ctr

officecli add shapes-basic.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=11.5in --prop y=2.8in --prop width=2in --prop height=2in \
  --prop fill=F4A261 --prop lineColor=1D3557 --prop lineWidth=12pt \
  --prop lineAlign=in

officecli close shapes-basic.pptx
officecli validate shapes-basic.pptx
```

**Features:** `lineCap` (flat, round, square), `lineJoin` (round, bevel, miter), `lineJoin="miter:N"` (compound form sets join + miterLimit in one prop), `lineAlign` (ctr, in)

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Geometry presets:** rect, roundRect, ellipse, triangle, diamond, parallelogram, rightArrow, star5 | 1 |
| **Solid fill:** hex color | 1, 2 |
| **Theme fill:** accent1..6, dark1/2, light1/2 | 2 |
| **Linear gradient:** `C1-C2-ANGLE` | 2 |
| **Radial gradient:** `radial:C1-C2-center` | 2 |
| **Per-stop gradient:** `C@POS-C@POS-C@POS` | 2 |
| **Pattern fill:** `preset:fg:bg` | 2 |
| **Opacity:** `opacity=0.0–1.0` (requires fill) | 2 |
| **No fill:** `fill=none` | 2 |
| **Compound line:** `line="color:widthPt:dash"` | 2, 3 |
| **Per-attribute line:** `lineColor`, `lineWidth`, `lineDash` | 3 |
| **Dash patterns:** solid, dash, dashDot, dot, lgDash, sysDash, sysDot | 3 |
| **Compound stroke:** `cmpd=dbl|tri` | 3 |
| **Arrowheads:** `headEnd` / `tailEnd` (triangle, arrow, diamond, oval, none) | 3 |
| **Rotation:** `rotation=0..360` | 4 |
| **Shadow effect:** `shadow=color` or `shadow=color-blur-angle-dist-opacity` | 4 |
| **Glow effect:** `glow=color` or `glow=color-radius-opacity` | 4 |
| **Reflection effect:** `reflection=tight|half|full` | 4 |
| **Line cap:** `lineCap=flat|round|square` | 5 |
| **Line join:** `lineJoin=round|bevel|miter` | 5 |
| **Miter limit:** `lineJoin="miter:N"` (compound) | 5 |
| **Line alignment:** `lineAlign=ctr|in` | 5 |
| **Text in shape:** `text`, `size`, `bold`, `color` | 1–5 |
| **Position/size:** `x`, `y`, `width`, `height` (in/cm/pt/emu) | 1–5 |

## Inspect the Generated File

```bash
# List all shapes on slide 1
officecli query shapes-basic.pptx '/slide[1]' shape

# Get the full property set for the ellipse (shape[3])
officecli get shapes-basic.pptx '/slide[1]/shape[3]'

# Check the fill and gradient on slide 2 shape[3] (linear gradient)
officecli get shapes-basic.pptx '/slide[2]/shape[3]'

# Inspect outline properties on slide 3 shape[2]
officecli get shapes-basic.pptx '/slide[3]/shape[2]'

# Check effects on the shadow shape (slide 4 shape[9])
officecli get shapes-basic.pptx '/slide[4]/shape[9]'

# Inspect stroke geometry on slide 5
officecli get shapes-basic.pptx '/slide[5]/shape[1]'
officecli get shapes-basic.pptx '/slide[5]/shape[4]'
```
