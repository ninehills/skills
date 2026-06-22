# Shape Typography

This demo consists of three files that work together:

- **shapes-typography.sh** — Shell script that calls `officecli` commands to generate the deck.
- **shapes-typography.pptx** — The generated 5-slide deck (paragraph spacing, character spacing/kerning/caps, RTL/complex-script, bare font + BCP-47 lang, decorations + valign + list + lineOpacity + animation).
- **shapes-typography.md** — This file. Covers typography properties not touched by textboxes-basic.

## Regenerate

```bash
cd examples/ppt
bash shapes/shapes-typography.sh
# → shapes/shapes-typography.pptx
```

## Slides

### Slide 1 — Paragraph Spacing (lineSpacing / spaceBefore / spaceAfter)

Three textboxes with identical Latin text and three paragraphs each, demonstrating the three paragraph-spacing props.

```bash
officecli create shapes-typography.pptx
officecli open shapes-typography.pptx
officecli add shapes-typography.pptx / --type slide

LOREM='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt.'

# Reference — no spacing props set (tight default)
officecli add shapes-typography.pptx '/slide[1]' --type textbox \
  --prop x=0.5in --prop y=1.2in --prop width=4in --prop height=3.5in \
  --prop fill=F1FAEE --prop size=14 --prop text="$LOREM"
officecli add shapes-typography.pptx '/slide[1]/shape[2]' --type paragraph --prop text="$LOREM"
officecli add shapes-typography.pptx '/slide[1]/shape[2]' --type paragraph --prop text="$LOREM"

# lineSpacing=1.5x — line height multiplier applied to every paragraph
# Also accepts: 150% / 18pt (fixed) / bare number (hundredths of a point)
officecli add shapes-typography.pptx '/slide[1]' --type textbox \
  --prop x=5in --prop y=1.2in --prop width=4in --prop height=3.5in \
  --prop fill=A8DADC --prop size=14 --prop text="$LOREM" --prop lineSpacing=1.5x
officecli add shapes-typography.pptx '/slide[1]/shape[4]' --type paragraph \
  --prop text="$LOREM" --prop lineSpacing=1.5x
officecli add shapes-typography.pptx '/slide[1]/shape[4]' --type paragraph \
  --prop text="$LOREM" --prop lineSpacing=1.5x

# spaceBefore + spaceAfter — gap above/below each paragraph
# Accepts pt-suffixed values, cm, in, or bare numbers (hundredths of a point)
officecli add shapes-typography.pptx '/slide[1]' --type textbox \
  --prop x=9.5in --prop y=1.2in --prop width=4in --prop height=3.5in \
  --prop fill=F4A261 --prop size=14 --prop text="$LOREM" \
  --prop spaceBefore=12pt --prop spaceAfter=12pt
officecli add shapes-typography.pptx '/slide[1]/shape[6]' --type paragraph \
  --prop text="$LOREM" --prop spaceBefore=12pt --prop spaceAfter=12pt
officecli add shapes-typography.pptx '/slide[1]/shape[6]' --type paragraph \
  --prop text="$LOREM" --prop spaceBefore=12pt --prop spaceAfter=12pt
```

**Features:** `lineSpacing` (multiplier: `1.5x`, `150%`; fixed: `18pt`; bare hundredths), `spaceBefore` (pt, cm, in, bare), `spaceAfter` (same units); all three work at both shape level (default for all paragraphs) and per-paragraph override via `--type paragraph`

---

### Slide 2 — Character Spacing, Kerning, and Case

Four textboxes demonstrating `spacing=`, two `kern=` comparisons, and three `cap=` modes.

```bash
officecli add shapes-typography.pptx / --type slide

SAMPLE='Tight Loose Spacing TYPOGRAPHY'

# spacing — character spacing in 1/100 pt; positive = looser, negative = tighter
officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=13in --prop height=0.8in \
  --prop size=22 --prop bold=true --prop text="$SAMPLE  (default)"

officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=2.3in --prop width=13in --prop height=0.8in \
  --prop size=22 --prop bold=true --prop text="$SAMPLE  (spacing=-50)" \
  --prop spacing=-50

officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=3.1in --prop width=13in --prop height=0.8in \
  --prop size=22 --prop bold=true --prop text="$SAMPLE  (spacing=200)" \
  --prop spacing=200

officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=3.9in --prop width=13in --prop height=0.8in \
  --prop size=22 --prop bold=true --prop text="$SAMPLE  (spacing=500)" \
  --prop spacing=500

# kern — minimum font size (in 1/100 pt) at which auto-kerning activates
# kern=1 = kern from 0.01pt upward (always on); kern=0 = completely off
officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=5in --prop width=6in --prop height=0.8in \
  --prop size=18 --prop text="AVATAR  Yawning  (kern=1)  — kern on from 0.01pt" \
  --prop kern=1

officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=7in --prop y=5in --prop width=6in --prop height=0.8in \
  --prop size=18 --prop text="AVATAR  Yawning  (kern=0)  — kern OFF" \
  --prop kern=0

# cap — text case rendering (does not change stored characters)
officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=6in --prop width=4in --prop height=0.8in \
  --prop size=18 --prop text="cap=none — Default case" --prop cap=none

officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=4.7in --prop y=6in --prop width=4in --prop height=0.8in \
  --prop size=18 --prop text="cap=small — Small caps" --prop cap=small

officecli add shapes-typography.pptx '/slide[2]' --type textbox \
  --prop x=8.9in --prop y=6in --prop width=4in --prop height=0.8in \
  --prop size=18 --prop text="cap=all — All caps" --prop cap=all
```

**Features:** `spacing` (character spacing in 1/100 pt; negative = tighter, positive = looser), `kern` (kerning threshold in 1/100 pt; 0 = off, 1 = always on), `cap` (none, small, all)

---

### Slide 3 — direction=rtl + font.cs (Complex Script / Arabic / Hebrew)

Demonstrates RTL paragraph direction with the complex-script font slot for Arabic and Hebrew text.

```bash
officecli add shapes-typography.pptx / --type slide

# LTR with Arabic text + complex-script font (default direction; digits flow LTR)
officecli add shapes-typography.pptx '/slide[3]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=6in --prop height=1.2in \
  --prop fill=F1FAEE --prop size=24 --prop bold=true \
  --prop text="مرحبا بالعالم — 2026" \
  --prop font.cs="Arabic Typesetting"

# RTL Arabic — paragraph flows right-to-left; align=right for correct visual placement
officecli add shapes-typography.pptx '/slide[3]' --type textbox \
  --prop x=7in --prop y=1.5in --prop width=6in --prop height=1.2in \
  --prop fill=A8DADC --prop size=24 --prop bold=true \
  --prop text="مرحبا بالعالم — 2026" \
  --prop direction=rtl --prop font.cs="Arabic Typesetting" --prop align=right

# Hebrew RTL with appropriate complex-script font
officecli add shapes-typography.pptx '/slide[3]' --type textbox \
  --prop x=0.5in --prop y=3.7in --prop width=12.5in --prop height=1.5in \
  --prop fill=F4A261 --prop size=24 --prop bold=true \
  --prop text="שלום עולם — Hebrew demo" \
  --prop direction=rtl --prop font.cs="Arial Hebrew" --prop align=right
```

**Features:** `direction` (rtl; aliases: dir, rtl; default is ltr), `font.cs` (complex-script font slot — used for Arabic, Hebrew, Urdu, Persian, Thai, etc.), `align` (left, center, right, justify)

---

### Slide 4 — Bare font= + BCP-47 lang Tags

Bare `font=` targets both the Latin and EastAsian slots at once. Per-script `font.latin` / `font.ea` give finer control. `lang=` drives spellcheck, hyphenation, and font fallback.

```bash
officecli add shapes-typography.pptx / --type slide

# Bare font= — sets a:latin AND a:ea simultaneously in one prop
officecli add shapes-typography.pptx '/slide[4]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=6in --prop height=1.5in \
  --prop fill=F1FAEE --prop size=22 \
  --prop text="Bare font sets Latin + EastAsian" \
  --prop font="Times New Roman"

# Per-script slots — finer control over mixed Latin + East Asian text
officecli add shapes-typography.pptx '/slide[4]' --type textbox \
  --prop x=7in --prop y=1.5in --prop width=6in --prop height=1.5in \
  --prop fill=A8DADC --prop size=22 \
  --prop text="Per-script gives finer control" \
  --prop font.latin="Georgia" --prop font.ea="Yu Mincho"

# BCP-47 language tags — drives spellcheck, hyphenation, font fallback per locale
officecli add shapes-typography.pptx '/slide[4]' --type textbox \
  --prop x=0.5in --prop y=3.8in --prop width=4in --prop height=1in \
  --prop fill=F4A261 --prop size=18 --prop text="Color or colour?" --prop lang=en-GB

officecli add shapes-typography.pptx '/slide[4]' --type textbox \
  --prop x=4.7in --prop y=3.8in --prop width=4in --prop height=1in \
  --prop fill=F4A261 --prop size=18 --prop text="Couleur en français" --prop lang=fr-FR

officecli add shapes-typography.pptx '/slide[4]' --type textbox \
  --prop x=8.9in --prop y=3.8in --prop width=4in --prop height=1in \
  --prop fill=F4A261 --prop size=18 --prop text="日本語のテスト" --prop lang=ja-JP \
  --prop font.ea="Yu Mincho"
```

**Features:** `font` (bare — sets a:latin and a:ea simultaneously), `font.latin` (Latin script slot only), `font.ea` (East Asian script slot only), `font.cs` (complex-script slot), `lang` (BCP-47 tag: en-US, en-GB, fr-FR, ja-JP, ar-SA, he-IL, …)

---

### Slide 5 — strike / underline / valign / margin / list / lineOpacity / animation

Miscellaneous shape-level properties that complete the typography surface.

```bash
officecli add shapes-typography.pptx / --type slide

# strike + underline — decoration applied to all runs in the shape
officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=roundRect \
  --prop x=0.5in --prop y=1.2in --prop width=3.5in --prop height=1.2in \
  --prop fill=F4A261 --prop color=000000 --prop size=18 \
  --prop text="strike=single" \
  --prop strike=single

officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=roundRect \
  --prop x=4.3in --prop y=1.2in --prop width=3.5in --prop height=1.2in \
  --prop fill=A8DADC --prop color=000000 --prop size=18 \
  --prop text="underline=single" \
  --prop underline=single

# valign — vertical text position within the shape bounding box
officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=0.5in --prop y=2.6in --prop width=3.5in --prop height=2in \
  --prop fill=DEEAF6 --prop lineColor=4472C4 --prop lineWidth=2pt \
  --prop text="valign=top" --prop size=16 --prop bold=true \
  --prop valign=top

officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=4.3in --prop y=2.6in --prop width=3.5in --prop height=2in \
  --prop fill=DEEAF6 --prop lineColor=4472C4 --prop lineWidth=2pt \
  --prop text="valign=middle" --prop size=16 --prop bold=true \
  --prop valign=middle

officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=8.1in --prop y=2.6in --prop width=3.5in --prop height=2in \
  --prop fill=DEEAF6 --prop lineColor=4472C4 --prop lineWidth=2pt \
  --prop text="valign=bottom" --prop size=16 --prop bold=true \
  --prop valign=bottom

# margin — uniform inner text padding
# Also accepts per-edge: marginLeft, marginRight, marginTop, marginBottom
officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=roundRect \
  --prop x=0.5in --prop y=4.9in --prop width=5in --prop height=1.4in \
  --prop fill=F1FAEE --prop lineColor=2A9D8F --prop lineWidth=2pt \
  --prop text="margin=0.4in  — large inner padding" --prop size=16 \
  --prop margin=0.4in

# list=bullet — shape-level bullet applied to all paragraphs. Pass every item
# as ONE multiline text block so the list style covers all paragraphs;
# paragraphs added after creation do NOT inherit the shape's list style.
officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=5in --prop y=4.8in --prop width=4.2in --prop height=1.5in \
  --prop fill=F4A261 --prop color=000000 --prop size=14 \
  --prop list=bullet \
  --prop text="First item
Second item
Third item"

# lineOpacity — outline transparency (0=opaque, 1=invisible); requires a non-none line
officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=rect \
  --prop x=0.5in --prop y=6.4in --prop width=4in --prop height=0.95in \
  --prop fill=4472C4 --prop lineColor=E63946 --prop lineWidth=6pt \
  --prop lineOpacity=0.35 \
  --prop text="lineOpacity=0.35" --prop color=FFFFFF --prop size=14

# animation — entrance animation preset applied at shape add time
officecli add shapes-typography.pptx '/slide[5]' --type shape --prop geometry=roundRect \
  --prop x=4.3in --prop y=6.5in --prop width=3.5in --prop height=1in \
  --prop fill=E63946 --prop color=FFFFFF --prop size=14 --prop bold=true \
  --prop text="animation=fadeIn" \
  --prop animation=fadeIn

officecli close shapes-typography.pptx
officecli validate shapes-typography.pptx
```

**Features:** `strike` (single, double), `underline` (single, double, heavy, dotted, dash, …), `valign` (top, middle, bottom), `margin` (uniform; also `marginLeft`, `marginRight`, `marginTop`, `marginBottom` per-edge), `list` (bullet, numbered), `lineOpacity` (0.0–1.0; requires a non-none line), `animation` (entrance preset: fadeIn, flyIn, appear, …)

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **lineSpacing:** multiplier (1.5x / 150%), fixed (18pt), bare | 1 |
| **spaceBefore / spaceAfter:** gap above/below paragraph (pt, cm, in) | 1 |
| **Per-paragraph spacing override:** via `--type paragraph` | 1 |
| **spacing:** character spacing in 1/100 pt (positive/negative) | 2 |
| **kern:** kerning threshold in 1/100 pt (0=off, 1=always on) | 2 |
| **cap:** none, small (small-caps), all (all-caps) | 2 |
| **direction=rtl:** right-to-left paragraph flow | 3 |
| **font.cs:** complex-script font slot (Arabic, Hebrew, Thai, …) | 3, 4 |
| **font=:** bare — sets Latin + EastAsian simultaneously | 4 |
| **font.latin / font.ea:** per-script slot overrides | 4 |
| **lang=:** BCP-47 tag (en-GB, fr-FR, ja-JP, …) | 4 |
| **strike:** single, double | 5 |
| **underline:** single, double, heavy, dotted, dash | 5 |
| **valign:** top, middle, bottom | 5 |
| **margin:** uniform inner padding (also per-edge variants) | 5 |
| **list=bullet:** shape-level bullet (all paragraphs) | 5 |
| **lineOpacity:** outline transparency 0.0–1.0 | 5 |
| **animation:** entrance preset (fadeIn, flyIn, appear, …) | 5 |

## Inspect the Generated File

```bash
# Check paragraph spacing on slide 1 textboxes
officecli get shapes-typography.pptx '/slide[1]/shape[2]'
officecli get shapes-typography.pptx '/slide[1]/shape[4]'

# Inspect character spacing and cap on slide 2
officecli get shapes-typography.pptx '/slide[2]/shape[2]'
officecli get shapes-typography.pptx '/slide[2]/shape[7]'

# Check direction + font.cs on slide 3
officecli get shapes-typography.pptx '/slide[3]/shape[2]'

# Read back font.latin / font.ea / lang on slide 4
officecli get shapes-typography.pptx '/slide[4]/shape[2]'
officecli get shapes-typography.pptx '/slide[4]/shape[4]'

# Verify valign, list, lineOpacity on slide 5
officecli get shapes-typography.pptx '/slide[5]/shape[4]'
officecli get shapes-typography.pptx '/slide[5]/shape[8]'
```
