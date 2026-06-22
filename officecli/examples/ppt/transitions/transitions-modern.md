# Modern Transitions (PowerPoint 2013+ "Exciting" Gallery)

This demo consists of three files that work together:

- **transitions-modern.sh** — Shell script that generates a 19-slide deck covering all 12 p15 preset tokens plus their `-out` directional variants.
- **transitions-modern.pptx** — The generated 19-slide deck.
- **transitions-modern.md** — This file. Documents each preset and the -in/-out direction modifier.

## Regenerate

```bash
cd examples/ppt/transitions
bash transitions-modern.sh
# → transitions-modern.pptx
```

These transitions require PowerPoint 2013 or later. officecli writes each one inside an `mc:AlternateContent` wrapper with an inline fade fallback, so pre-2013 PowerPoint plays a graceful fade instead of nothing.

## Slides

### Slide 1 — Cover (no transition)

```bash
officecli add transitions-modern.pptx / --type slide
officecli add transitions-modern.pptx /slide[1] --type shape \
  --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
  --prop fill=1F3864
officecli add transitions-modern.pptx /slide[1] --type shape \
  --prop text="Modern (p15) Transitions" --prop size=40 --prop bold=true \
  --prop color=FFFFFF --prop align=center \
  --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
```

### Slides 2–13 — All 12 presets in default (-in) form

```bash
for t in fallOver drape curtains wind prestige fracture crush peelOff \
         pageCurlDouble pageCurlSingle airplane origami; do
  officecli add transitions-modern.pptx / --type slide
  officecli add transitions-modern.pptx "/slide[N]" --type shape \
    --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
    --prop fill=2E5C8A
  officecli add transitions-modern.pptx "/slide[N]" --type shape \
    --prop text="$t" --prop size=40 --prop bold=true \
    --prop color=FFFFFF --prop align=center \
    --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
  officecli set transitions-modern.pptx "/slide[N]" --prop transition="$t"
done
```

**Features:** `transition=fallOver`, `drape`, `curtains`, `wind`, `prestige`, `fracture`, `crush`, `peelOff`, `pageCurlDouble`, `pageCurlSingle`, `airplane`, `origami`

### Slides 14–19 — Direction-sensitive presets with -out

```bash
for t in wind peelOff pageCurlDouble airplane origami fallOver; do
  officecli add transitions-modern.pptx / --type slide
  officecli add transitions-modern.pptx "/slide[N]" --type shape \
    --prop x=0 --prop y=0 --prop width=33.87cm --prop height=19.05cm \
    --prop fill=8A5A2B
  officecli add transitions-modern.pptx "/slide[N]" --type shape \
    --prop text="$t-out" --prop size=40 --prop bold=true \
    --prop color=FFFFFF --prop align=center \
    --prop x=2cm --prop y=7cm --prop width=29.87cm --prop height=4cm
  officecli set transitions-modern.pptx "/slide[N]" --prop transition="$t-out"
done
```

`-out` sets `invX="1" invY="1"` in the OOXML, visually flipping the direction on direction-sensitive presets. Symmetric presets (`curtains`, `fracture`, `crush`, `prestige`) accept the suffix but render unchanged.

**Features:** `transition=wind-out`, `peelOff-out`, `pageCurlDouble-out`, `airplane-out`, `origami-out`, `fallOver-out`

## Complete Feature Coverage

| CLI token | UI name | Direction-sensitive? |
|-----------|---------|---------------------|
| `fallOver` | Fall Over | Yes |
| `drape` | Drape | Yes |
| `curtains` | Curtains | Symmetric |
| `wind` | Wind | Yes |
| `prestige` | Prestige | Symmetric |
| `fracture` | Fracture | Symmetric |
| `crush` | Crush | Symmetric |
| `peelOff` | Peel Off | Yes |
| `pageCurlDouble` | Page Curl (double) | Yes |
| `pageCurlSingle` | Page Curl (single) | Yes |
| `airplane` | Airplane | Yes |
| `origami` | Origami | Yes |

**Direction modifier:** `-in` (default, no invX/invY written) / `-out` (sets `invX="1" invY="1"`). Any other direction suffix is rejected:

```
Error: Transition 'fallOver' only accepts -in or -out (got '-up').
```

**Token case:** lowerCamelCase. Input is case-insensitive (`PageCurlDouble` and `pagecurldouble` both work); `get` returns the canonical lowerCamelCase form.

## PowerPoint UI Tiles Backed by Other CLI Tokens

| PowerPoint UI tile | CLI token |
|---|---|
| Cube (Exciting) | `prism` or `cube` (→ transitions-dynamic) |
| Rotate (Dynamic Content) | `rotate` (→ transitions-dynamic) |
| Orbit (Dynamic Content) | `orbit` (→ transitions-dynamic) |
| Clock (Exciting) | `wheel-1` or `clock` (→ transitions-shapes) |
| Box (Exciting) | `box-in` / `box-out` (→ transitions-shapes) |

## Inspect the Generated File

```bash
officecli query transitions-modern.pptx slide
officecli get transitions-modern.pptx /slide[2]
officecli get transitions-modern.pptx /slide[14]
```

## Related

- [transitions-shapes.md](transitions-shapes.md) — Box alongside circle/diamond/zoom
- [transitions-dynamic.md](transitions-dynamic.md) — 2010 "Exciting" gallery (vortex/switch/flip/ferris/…/prism/rotate/orbit)
- [transitions-morph.md](transitions-morph.md) — Morph (2016+)
