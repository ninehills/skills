# Transition Timing — Speed, Duration, and Advance Knobs

This demo consists of three files that work together:

- **transitions-timing.sh** — Shell script that generates a 9-slide deck demonstrating legacy speed tokens, ms duration, auto-advance, and click-disable.
- **transitions-timing.pptx** — The generated 9-slide deck.
- **transitions-timing.md** — This file. Documents all four timing knobs.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-timing.sh
# → transitions-timing.pptx
```

## Slides

### Slide 1 — Cover (no transition)

```bash
officecli add transitions-timing.pptx / --type slide
officecli add transitions-timing.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864
officecli add transitions-timing.pptx /slide[1] --type shape \
  --prop text="Transition Timing" --prop size=40 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
```

### Slides 2–4 — Legacy speed tokens (PowerPoint 97+)

```bash
# fast — snappiest legacy speed
officecli add transitions-timing.pptx / --type slide
officecli add transitions-timing.pptx /slide[2] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=C00000
officecli add transitions-timing.pptx /slide[2] --type shape \
  --prop text="fade-fast (legacy @spd)" --prop size=40 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
officecli set transitions-timing.pptx /slide[2] --prop transition=fade-fast

# medium
officecli set transitions-timing.pptx /slide[3] --prop transition=fade-med

# slow
officecli set transitions-timing.pptx /slide[4] --prop transition=fade-slow
```

`get` surfaces the value as the read-only `transitionSpeed` format key.

**Features:** `transition=fade-fast`, `fade-med` (or `fade-medium`), `fade-slow`

### Slides 5–7 — Office 2010+ duration in milliseconds

```bash
officecli set transitions-timing.pptx /slide[5] --prop transition=fade-500    # 0.5 s
officecli set transitions-timing.pptx /slide[6] --prop transition=fade-1500   # 1.5 s
officecli set transitions-timing.pptx /slide[7] --prop transition=fade-3000   # 3.0 s
```

`get` surfaces the value as the read-only `transitionDuration` key (ms integer). Specifying both speed and duration is allowed — newer PowerPoint honors `@dur`, older falls back to `@spd`.

**Features:** `transition=fade-500`, `fade-1500`, `fade-3000` (any integer ms)

### Slide 8 — Auto-advance (advanceTime=2000)

The slide advances automatically after 2 seconds in Slide Show mode — no click required.

```bash
officecli add transitions-timing.pptx / --type slide
officecli add transitions-timing.pptx /slide[8] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=BF8F00
officecli add transitions-timing.pptx /slide[8] --type shape \
  --prop text="advanceTime=2000  (auto-advance after 2s)" --prop size=36 \
  --prop bold=true --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
officecli set transitions-timing.pptx /slide[8] \
  --prop transition=fade --prop advanceTime=2000
```

To clear later: `officecli set ... --prop advanceTime=none`

**Features:** `advanceTime=<ms>`, `advanceTime=none` (clear)

### Slide 9 — Disable click-to-advance (advanceClick=false)

This slide only advances via auto-time or arrow keys — clicks are ignored.

```bash
officecli add transitions-timing.pptx / --type slide
officecli add transitions-timing.pptx /slide[9] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=2E5C8A
officecli add transitions-timing.pptx /slide[9] --type shape \
  --prop text="advanceClick=false  (no click advance)" --prop size=36 \
  --prop bold=true --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
officecli set transitions-timing.pptx /slide[9] \
  --prop transition=fade --prop advanceClick=false
```

**Features:** `advanceClick=false` (disable click-to-advance)

## Complete Feature Coverage

| Knob | Syntax | Notes |
|------|--------|-------|
| Legacy speed | `fade-fast` / `fade-med` / `fade-slow` | Sets OOXML `@spd`; readback: `transitionSpeed` |
| Duration ms | `fade-500` / `fade-1500` / `fade-3000` | Sets OOXML `@dur`; readback: `transitionDuration` |
| Auto-advance | `advanceTime=2000` | Sets `advTm`; clear with `advanceTime=none` |
| Click-to-advance | `advanceClick=false` | Default true (attribute stripped); false stored |

## Round-Trip Semantics

- `advanceClick=true` → XML attribute stripped → readback emits no `advanceClick` key (default is true).
- `advanceClick=false` → XML keeps `advClick="0"` → readback emits `advanceClick=false`.
- `advanceTime=none` → XML attribute removed → readback emits no `advanceTime` key.

## Inspect the Generated File

```bash
officecli query transitions-timing.pptx slide
officecli get transitions-timing.pptx /slide[2]
officecli get transitions-timing.pptx /slide[8]
officecli get transitions-timing.pptx /slide[9]
```

## Related

- [transitions-basic.md](transitions-basic.md) — `transition=none` to clear the entire transition element
