# 3D Model Showcase — "The Sun"

This demo consists of three files that work together:

- **3d-model.sh** — Shell script that builds an 8-slide morph presentation embedding a GLB 3D model of the Sun on every slide, with different positions and rotations so PowerPoint's Morph transition animates it cinematically.
- **3d-model.pptx** — The generated 8-slide deck: each slide advances the 3D model's position and rotation, creating an orbit animation through morph transitions.
- **3d-model.md** — This file. Documents the `3dmodel` element properties and morph-based rotation technique.

## Regenerate

```bash
cd examples/ppt
bash 3d-model.sh
# → 3d-model.pptx
# Requires: models/sun.glb in the same directory
```

## Slides

### All 8 Slides — 3D Model Setup

Every slide shares the same structure: a jet-black background, a morph transition, and a `3dmodel` element. The GLB model has the same `name=sun` on every slide so PowerPoint pairs and tweens it across transitions.

```bash
# Create all 8 slides with matching dark background + morph transition
for i in $(seq 1 8); do
  officecli add 3d-model.pptx / --type slide \
    --prop background=0A0A0A \
    --prop transition=morph
done

# Slide 1 — model at right, slight downward tilt
officecli add 3d-model.pptx '/slide[1]' --type 3dmodel \
  --prop path="models/sun.glb" \
  --prop name=sun \
  --prop x=15cm --prop y=0.5cm --prop width=18cm --prop height=18cm \
  --prop rotx=10

# Slide 2 — model moves left, rotated 50° around Y
officecli add 3d-model.pptx '/slide[2]' --type 3dmodel \
  --prop path="models/sun.glb" \
  --prop name=sun \
  --prop x=0.5cm --prop y=0.5cm --prop width=16cm --prop height=16cm \
  --prop roty=50

# Slide 3 — model back right, 100° Y + 15° X tilt
officecli add 3d-model.pptx '/slide[3]' --type 3dmodel \
  --prop path="models/sun.glb" \
  --prop name=sun \
  --prop x=18cm --prop y=3cm --prop width=16cm --prop height=16cm \
  --prop roty=100 --prop rotx=15

# Slides 4–8 follow the same pattern, advancing roty by ~50° each slide
# and alternating left/right position to create an orbit path
officecli add 3d-model.pptx '/slide[4]' --type 3dmodel \
  --prop path="models/sun.glb" --prop name=sun \
  --prop x=0.5cm --prop y=1cm --prop width=18cm --prop height=18cm \
  --prop roty=150

officecli add 3d-model.pptx '/slide[5]' --type 3dmodel \
  --prop path="models/sun.glb" --prop name=sun \
  --prop x=17cm --prop y=0.5cm --prop width=18cm --prop height=18cm \
  --prop roty=200 --prop rotx=20

officecli add 3d-model.pptx '/slide[6]' --type 3dmodel \
  --prop path="models/sun.glb" --prop name=sun \
  --prop x=0.5cm --prop y=2cm --prop width=17cm --prop height=17cm \
  --prop roty=250

officecli add 3d-model.pptx '/slide[7]' --type 3dmodel \
  --prop path="models/sun.glb" --prop name=sun \
  --prop x=16cm --prop y=1cm --prop width=17cm --prop height=17cm \
  --prop roty=310 --prop rotx=10

officecli add 3d-model.pptx '/slide[8]' --type 3dmodel \
  --prop path="models/sun.glb" --prop name=sun \
  --prop x=15cm --prop y=0.5cm --prop width=18cm --prop height=18cm \
  --prop roty=360 --prop rotx=10
```

**Features:** `--type 3dmodel`, `path=` (GLB file to embed), `name=` (shape name — must match across slides for morph pairing), `x=/y=/width=/height=` in cm, `rotx=` (X-axis tilt degrees), `roty=` (Y-axis orbit degrees), `background=` hex, `transition=morph`

### Slide 1 — Title Text

Text labels overlapping the 3D model on the left side of the slide.

```bash
officecli add 3d-model.pptx '/slide[1]' --type shape \
  --prop text='THE SUN' \
  --prop x=1cm --prop y=2cm --prop w=13cm --prop h=3.5cm \
  --prop size=64 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop font='Arial Black'

officecli add 3d-model.pptx '/slide[1]' --type shape \
  --prop text='Our Star' \
  --prop x=1cm --prop y=6cm --prop w=13cm --prop h=2cm \
  --prop size=26 --prop color=FFB74D --prop fill=00000000 \
  --prop font=Calibri

officecli add 3d-model.pptx '/slide[1]' --type shape \
  --prop text='149.6 million km from Earth · Light takes 8 min 20 sec' \
  --prop x=1cm --prop y=8.5cm --prop w=13cm --prop h=2cm \
  --prop size=18 --prop color=9E9E9E --prop fill=00000000 \
  --prop font=Calibri
```

**Features:** `fill=00000000` (fully transparent fill = no background behind text), `font=Arial Black`, `bold=`, `color=` (amber/orange palette), `w=/h=` as aliases for `width=/height=`

### Slides 2–7 — Content Slides (Alternating Left/Right)

Each content slide adds two shapes: a right-aligned or left-aligned headline plus a multi-line body with `lineSpacing=2x`.

```bash
# Slide 2 — right-aligned (model on left)
officecli add 3d-model.pptx '/slide[2]' --type shape \
  --prop text='Star Profile' \
  --prop x=18cm --prop y=1cm --prop w=15cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop font=Calibri --prop align=right

officecli add 3d-model.pptx '/slide[2]' --type shape \
  --prop text='Spectral type  G2V yellow dwarf\nDiameter  1.392 million km\nMass  330,000x Earth\nSurface temp  5,778 K\nCore temp  15 million K\nAge  4.6 billion years' \
  --prop x=18cm --prop y=4cm --prop w=15cm --prop h=14cm \
  --prop size=22 --prop color=E0E0E0 --prop fill=00000000 \
  --prop font=Calibri --prop align=right --prop lineSpacing=2x

# Slide 3 — left-aligned (model on right)
officecli add 3d-model.pptx '/slide[3]' --type shape \
  --prop text='Internal Structure' \
  --prop x=1cm --prop y=1cm --prop w=15cm --prop h=2.5cm \
  --prop size=40 --prop bold=true --prop color=FF6F00 --prop fill=00000000 \
  --prop font=Calibri

# ... slides 4–7 follow the same pattern, alternating align=right/left
```

**Features:** `align=right` / `align=left` paragraph alignment, `lineSpacing=2x` (2× line height multiplier), `\n` literal newline in `text=`, multi-line factoid layout

### Slide 8 — Closing Latin Quote

Bold italic quote in Georgia, grey translation beneath.

```bash
officecli add 3d-model.pptx '/slide[8]' --type shape \
  --prop text='Per Aspera Ad Astra' \
  --prop x=1cm --prop y=7cm --prop w=13cm --prop h=3cm \
  --prop size=48 --prop bold=true --prop italic=true \
  --prop color=FF6F00 --prop fill=00000000 \
  --prop font=Georgia

officecli add 3d-model.pptx '/slide[8]' --type shape \
  --prop text='Through hardships to the stars' \
  --prop x=1cm --prop y=11cm --prop w=13cm --prop h=2cm \
  --prop size=24 --prop color=9E9E9E --prop fill=00000000 \
  --prop font=Calibri
```

**Features:** `italic=true`, `font=Georgia`, contrasting size pair (48pt / 24pt), `color=9E9E9E` (muted grey)

## Complete Feature Coverage

| Feature | Slides |
|---------|--------|
| **--type 3dmodel** GLB embedding | 1–8 |
| **path=** (GLB file path) | 1–8 |
| **name=** (morph pairing key) | 1–8 |
| **rotx=** (X-axis tilt degrees) | 1, 3, 5, 7, 8 |
| **roty=** (Y-axis orbit degrees) | 2–8 |
| **x=/y=/width=/height=** in cm (model placement) | 1–8 |
| **transition=morph** | 1–8 |
| **background=** hex | 1–8 |
| **fill=00000000** (transparent — no shape background) | 1–8 |
| **font=Arial Black / Georgia / Calibri** | 1, 8 |
| **bold=** / **italic=** | 1, 8 |
| **align=right / align=left** | 2, 4, 6 |
| **lineSpacing=2x** (line height multiplier) | 2–7 |
| **text= with \n** multi-line | 2–7 |
| **w=/h= aliases** | 1 |
| **officecli validate** post-generation check | final |

## Morph Technique

PowerPoint's Morph transition tweens shapes that share the same name between adjacent slides. The key rule: `name=sun` must appear on both the outgoing and incoming slide for morph to pair them.

The rotation values advance ~50° per slide around the Y axis (`roty=0→50→100→150→200→250→310→360`), producing a smooth orbital animation. The X-axis tilt (`rotx=`) varies slightly to add dimensional interest.

Position also shifts left/right each slide (`x=15cm→0.5cm→18cm→0.5cm→17cm→...`), so the model appears to orbit across the slide while rotating — all interpolated smoothly by Morph.

## Inspect the Generated File

```bash
officecli query 3d-model.pptx slide
officecli get 3d-model.pptx /slide[1]
officecli get 3d-model.pptx "/slide[1]/3dmodel[1]"
officecli get 3d-model.pptx "/slide[2]/3dmodel[1]"
```
