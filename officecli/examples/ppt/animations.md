# Animation Showcase

This demo consists of three files that work together:

- **animations.sh** — Shell script that calls `officecli add`, `officecli set`, and layout-slide commands to build the deck. Every animation command is shown as a copyable shell command.
- **animations.pptx** — The generated 6-slide deck covering entrance, exit, emphasis, transitions gallery, and timing.
- **animations.md** — This file. Maps each slide to the animation features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash animations.sh
# → animations.pptx
```

## Slides

### Slide 1 — Title

Radial gradient background via `layout=title` + `background=radial:`, title and subtitle placeholders, `fade` transition.

```bash
officecli add animations.pptx / --type slide --prop layout=title
officecli set animations.pptx /slide[1] \
  --prop background=radial:0D1B2A-1B4F72-bl
officecli set animations.pptx '/slide[1]/placeholder[centertitle]' \
  --prop text="Animation Showcase" --prop color=FFFFFF --prop size=48
officecli set animations.pptx '/slide[1]/placeholder[subtitle]' \
  --prop text="Every animation effect in officecli" \
  --prop color=85C1E9 --prop size=22
officecli set animations.pptx /slide[1] --prop transition=fade
```

**Features:** `layout=title`, `background=radial:color1-color2-corner`, `placeholder[centertitle]`/`[subtitle]`, `transition=fade`

### Slide 2 — Entrance Animations

Twelve entrance animations on rounded-rectangle shapes. Each shape is added then animated via a separate `set` call using the `animation=effect-entrance-duration` compound token.

```bash
officecli add animations.pptx / --type slide --prop title="Entrance Effects"
officecli set animations.pptx /slide[2] --prop background=1B2838

# appear — instantly visible, no motion
officecli add animations.pptx '/slide[2]' --type shape \
  --prop text="appear" --prop font=Consolas --prop size=14 --prop color=FFFFFF \
  --prop fill=2E86C1 --prop preset=roundRect \
  --prop x=1cm --prop y=4cm --prop width=5cm --prop height=2cm
officecli set animations.pptx '/slide[2]/shape[2]' --prop animation=appear-entrance-500

# fade — pixel cross-fade in
officecli add animations.pptx '/slide[2]' --type shape \
  --prop text="fade" --prop font=Consolas --prop size=14 --prop color=FFFFFF \
  --prop fill=27AE60 --prop preset=roundRect \
  --prop x=7cm --prop y=4cm --prop width=5cm --prop height=2cm
officecli set animations.pptx '/slide[2]/shape[3]' --prop animation=fade-entrance-800

# fly — flies in from off-screen
officecli add animations.pptx '/slide[2]' --type shape \
  --prop text="fly" --prop font=Consolas --prop size=14 --prop color=FFFFFF \
  --prop fill=E74C3C --prop preset=roundRect \
  --prop x=13cm --prop y=4cm --prop width=5cm --prop height=2cm
officecli set animations.pptx '/slide[2]/shape[4]' --prop animation=fly-entrance-600

# zoom — scales from invisible to full size
officecli set animations.pptx '/slide[2]/shape[5]' --prop animation=zoom-entrance-700

# wipe — reveals from one edge
officecli set animations.pptx '/slide[2]/shape[6]' --prop animation=wipe-entrance-600

# bounce — drops in with a bounce
officecli set animations.pptx '/slide[2]/shape[7]' --prop animation=bounce-entrance-800

# float — floats up gently
officecli set animations.pptx '/slide[2]/shape[8]' --prop animation=float-entrance-700

# split — reveals from center outward or edge inward
officecli set animations.pptx '/slide[2]/shape[9]' --prop animation=split-entrance-600

# wheel — spins in like a clock
officecli set animations.pptx '/slide[2]/shape[10]' --prop animation=wheel-entrance-800

# swivel — pivots in on a vertical axis
officecli set animations.pptx '/slide[2]/shape[11]' --prop animation=swivel-entrance-700

# checkerboard — reveals in checkerboard pattern
officecli set animations.pptx '/slide[2]/shape[12]' --prop animation=checkerboard-entrance-600

# blinds — venetian-blind reveal
officecli set animations.pptx '/slide[2]/shape[13]' --prop animation=blinds-entrance-600

officecli set animations.pptx /slide[2] --prop transition=wipe
```

**Features:** `animation=<effect>-entrance-<ms>` (compound token), entrance effects: `appear`, `fade`, `fly`, `zoom`, `wipe`, `bounce`, `float`, `split`, `wheel`, `swivel`, `checkerboard`, `blinds`; `preset=roundRect`, `font=`, `fill=`, `x=/y=/width=/height=` in cm

### Slide 3 — Exit Animations

Six exit animations — each shape is on-screen initially and animated to leave the slide.

```bash
officecli add animations.pptx / --type slide --prop title="Exit Effects"
officecli set animations.pptx /slide[3] --prop background=1B2838

# fade exit
officecli add animations.pptx '/slide[3]' --type shape \
  --prop text="fade out" --prop fill=E74C3C --prop preset=roundRect \
  --prop x=1cm --prop y=4cm --prop width=7cm --prop height=2.5cm
officecli set animations.pptx '/slide[3]/shape[2]' --prop animation=fade-exit-800

# fly exit
officecli set animations.pptx '/slide[3]/shape[3]' --prop animation=fly-exit-600

# zoom exit
officecli set animations.pptx '/slide[3]/shape[4]' --prop animation=zoom-exit-700

# dissolve exit — random pixel dissolve
officecli set animations.pptx '/slide[3]/shape[5]' --prop animation=dissolve-exit-600

# wipe exit
officecli set animations.pptx '/slide[3]/shape[6]' --prop animation=wipe-exit-600

# flash exit — white flash then disappears
officecli set animations.pptx '/slide[3]/shape[7]' --prop animation=flash-exit-500

officecli set animations.pptx /slide[3] --prop transition=push
```

**Features:** `animation=<effect>-exit-<ms>`, exit effects: `fade`, `fly`, `zoom`, `dissolve`, `wipe`, `flash`

### Slide 4 — Emphasis Animations

Four emphasis effects on ellipse shapes — the shape remains visible and the effect draws attention to it.

```bash
officecli add animations.pptx / --type slide --prop title="Emphasis Effects"
officecli set animations.pptx /slide[4] --prop background=1B2838

# spin — rotates 360°
officecli add animations.pptx '/slide[4]' --type shape \
  --prop text="spin" --prop font=Consolas --prop size=16 --prop color=FFFFFF \
  --prop fill=E74C3C --prop preset=ellipse \
  --prop x=2cm --prop y=4.5cm --prop width=4.5cm --prop height=4.5cm
officecli set animations.pptx '/slide[4]/shape[2]' --prop animation=spin-emphasis-1000

# grow — scales up then back to normal
officecli set animations.pptx '/slide[4]/shape[3]' --prop animation=grow-emphasis-800

# wave — wave motion
officecli set animations.pptx '/slide[4]/shape[4]' --prop animation=wave-emphasis-700

# bold — flash bold text
officecli set animations.pptx '/slide[4]/shape[5]' --prop animation=bold-emphasis-500

officecli set animations.pptx /slide[4] --prop transition=zoom
```

**Features:** `animation=<effect>-emphasis-<ms>`, emphasis effects: `spin`, `grow`, `wave`, `bold`; `preset=ellipse`

### Slide 5 — Transitions Gallery

Thirteen transition tokens displayed as labeled cards on a dark background. Demonstrates `transition=` applied to individual slides.

```bash
officecli add animations.pptx / --type slide --prop title="Slide Transitions"
officecli set animations.pptx /slide[5] --prop background=0D1B2A

# Add a labeled card for each transition token (loop)
TRANSITIONS="fade wipe push split zoom wheel cover reveal dissolve random blinds checker strips"
for TR in $TRANSITIONS; do
  officecli add animations.pptx '/slide[5]' --type shape \
    --prop text="$TR" --prop font=Consolas --prop size=12 --prop color=FFFFFF \
    --prop fill=2C3E50 --prop preset=roundRect \
    --prop line=5DADE2 --prop linewidth=0.5pt \
    --prop width=5cm --prop height=1.8cm
done

officecli set animations.pptx /slide[5] --prop transition=split
```

**Features:** Transition token gallery: `fade`, `wipe`, `push`, `split`, `zoom`, `wheel`, `cover`, `reveal`, `dissolve`, `random`, `blinds`, `checker`, `strips`; `line=` color, `linewidth=` (pt units)

### Slide 6 — Timing and Triggers

Demonstrates the three animation trigger modes (`click`/`after`/`with`) and duration extremes (`200ms` vs `2000ms`). Slide auto-advances with `advanceTime`.

```bash
officecli add animations.pptx / --type slide --prop title="Timing & Triggers"
officecli set animations.pptx /slide[6] --prop background=1B2838

# Click trigger (default) — animation fires on mouse click
officecli add animations.pptx '/slide[6]' --type shape \
  --prop text="Click to animate\n(default trigger)" \
  --prop fill=2E86C1 --prop preset=roundRect \
  --prop x=1cm --prop y=4cm --prop width=7cm --prop height=3cm
officecli set animations.pptx '/slide[6]/shape[2]' --prop animation=fade-entrance-500

# After previous — fires automatically after the preceding animation ends
officecli add animations.pptx '/slide[6]' --type shape \
  --prop text="After previous\n(auto-follows)" \
  --prop fill=27AE60 --prop preset=roundRect \
  --prop x=9cm --prop y=4cm --prop width=7cm --prop height=3cm
officecli set animations.pptx '/slide[6]/shape[3]' --prop animation=fly-entrance-500-after

# With previous — fires simultaneously with the preceding animation
officecli add animations.pptx '/slide[6]' --type shape \
  --prop text="With previous\n(simultaneous)" \
  --prop fill=E74C3C --prop preset=roundRect \
  --prop x=17cm --prop y=4cm --prop width=7cm --prop height=3cm
officecli set animations.pptx '/slide[6]/shape[4]' --prop animation=zoom-entrance-500-with

# Duration extremes
officecli set animations.pptx '/slide[6]/shape[5]' --prop animation=wipe-entrance-2000  # slow: 2000ms
officecli set animations.pptx '/slide[6]/shape[6]' --prop animation=wipe-entrance-200   # fast: 200ms

# Slide auto-advances after 5 seconds (no click required)
officecli set animations.pptx /slide[6] --prop transition=reveal
officecli set animations.pptx /slide[6] --prop advanceTime=5000
```

**Features:** Animation trigger suffix `-after` (after previous), `-with` (with previous); default = click; `advanceTime=<ms>` auto-advance, `transition=reveal`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **layout=title** slide layout | 1 |
| **background=radial:** gradient | 1 |
| **background=** solid hex | 2–6 |
| **placeholder[centertitle]** / **[subtitle]** | 1 |
| **transition=** (fade, wipe, push, zoom, split, reveal) | 1–6 |
| **advanceTime=** auto-advance ms | 6 |
| **animation=effect-entrance-ms** | 2 |
| **Entrance effects:** appear, fade, fly, zoom, wipe, bounce, float, split, wheel, swivel, checkerboard, blinds | 2 |
| **animation=effect-exit-ms** | 3 |
| **Exit effects:** fade, fly, zoom, dissolve, wipe, flash | 3 |
| **animation=effect-emphasis-ms** | 4 |
| **Emphasis effects:** spin, grow, wave, bold | 4 |
| **Trigger suffix:** (default click), -after, -with | 6 |
| **Duration range:** 200ms–2000ms | 6 |
| **preset=roundRect / ellipse** | 2, 3, 4, 5 |
| **font=, fill=, color=, size=** on shape | 2, 3, 4, 5 |
| **line= / linewidth=** | 5 |
| **x=/y=/width=/height=** in cm/pt | 2–6 |
| **text= with \\n newlines** | 6 |

## Inspect the Generated File

```bash
officecli query animations.pptx slide
officecli get animations.pptx /slide[1]
officecli get animations.pptx "/slide[2]/shape[2]"
officecli get animations.pptx "/slide[6]/shape[3]"
```
