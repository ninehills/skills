# Advanced PPT Textbox Typography

This demo consists of three files that work together:

- **textboxes-advanced.sh** — Shell script that calls `officecli` commands to generate the deck.
- **textboxes-advanced.pptx** — The generated 6-slide deck (per-paragraph overrides, paragraph indents, per-paragraph styling, per-run typography, subscript/superscript aliases, textbox meta props).
- **textboxes-advanced.md** — This file. Covers per-paragraph and per-run overrides not shown in textboxes-basic.

## Regenerate

```bash
cd examples/ppt
bash textboxes/textboxes-advanced.sh
# → textboxes/textboxes-advanced.pptx
```

## Slides

### Slide 1 — Per-Paragraph Overrides (align / lineSpacing inside one textbox)

One textbox with five paragraphs; each paragraph overrides a different shape-level default.

```bash
officecli create textboxes-advanced.pptx
officecli open textboxes-advanced.pptx
officecli add textboxes-advanced.pptx / --type slide

LOREM='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus lacinia odio vitae vestibulum vestibulum.'

# Shape-level defaults: align=left, lineSpacing=1x
officecli add textboxes-advanced.pptx '/slide[1]' --type textbox \
  --prop x=0.5in --prop y=1.2in --prop width=13in --prop height=5.5in \
  --prop fill=F1FAEE --prop size=14 \
  --prop text="[shape default: align=left, single-spaced]  $LOREM"

# Each paragraph below overrides one property independently
officecli add textboxes-advanced.pptx '/slide[1]/shape[2]' --type paragraph \
  --prop text="[paragraph override: align=center]  $LOREM" --prop align=center

officecli add textboxes-advanced.pptx '/slide[1]/shape[2]' --type paragraph \
  --prop text="[paragraph override: align=right]  $LOREM" --prop align=right

officecli add textboxes-advanced.pptx '/slide[1]/shape[2]' --type paragraph \
  --prop text="[paragraph override: align=justify + lineSpacing=2x]  $LOREM $LOREM" \
  --prop align=justify --prop lineSpacing=2x

officecli add textboxes-advanced.pptx '/slide[1]/shape[2]' --type paragraph \
  --prop text="[paragraph override: lineSpacing=18pt fixed]  $LOREM" \
  --prop lineSpacing=18pt
```

**Features:** `--type paragraph` with `align` (center, right, justify) and `lineSpacing` (2x multiplier, 18pt fixed) override the shape defaults per-paragraph without affecting sibling paragraphs

---

### Slide 2 — Paragraph Indents (indent / marginLeft / marginRight)

Five textboxes showing every paragraph-level indent form: left-aligned indent, first-line indent, hanging indent, and right margin.

```bash
officecli add textboxes-advanced.pptx / --type slide

# Reference — no indent
officecli add textboxes-advanced.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=1.3in --prop width=13in --prop height=1in \
  --prop fill=F1FAEE --prop size=14 \
  --prop text="[default: no indent]  $LOREM $LOREM"

# marginLeft — whole paragraph body shifted right
officecli add textboxes-advanced.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=2.5in --prop width=13in --prop height=1in \
  --prop fill=A8DADC --prop size=14 \
  --prop text="[marginLeft=1in]  $LOREM $LOREM" \
  --prop marginLeft=1in

# indent — first-line indent only (subsequent lines flush left)
officecli add textboxes-advanced.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=3.7in --prop width=13in --prop height=1in \
  --prop fill=F4A261 --prop size=14 \
  --prop text="[indent=0.5in first-line]  $LOREM $LOREM" \
  --prop indent=0.5in

# Hanging indent — negative indent + positive marginLeft (first line extends further left)
officecli add textboxes-advanced.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=4.9in --prop width=13in --prop height=1in \
  --prop fill=A8DADC --prop size=14 \
  --prop text="[hanging: marginLeft=0.6in + indent=-0.5in]  $LOREM $LOREM" \
  --prop marginLeft=0.6in --prop indent=-0.5in

# marginRight — text column narrowed from the right
officecli add textboxes-advanced.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=6.1in --prop width=13in --prop height=1in \
  --prop fill=F4A261 --prop size=14 \
  --prop text="[marginRight=2in]  $LOREM $LOREM" \
  --prop marginRight=2in
```

**Features:** `marginLeft` (left paragraph indent; in, cm, pt), `indent` (first-line indent only; negative = hanging), `marginRight` (right margin; narrows from right edge), all at both textbox-creation time and as `--type paragraph` overrides

---

### Slide 3 — Per-Paragraph Styling (bold / italic / color / size / lang)

One textbox where each paragraph carries its own style without needing explicit runs.

```bash
officecli add textboxes-advanced.pptx / --type slide

# Shape default: 14pt black
officecli add textboxes-advanced.pptx '/slide[3]' --type textbox \
  --prop x=0.5in --prop y=1.2in --prop width=13in --prop height=5in \
  --prop fill=F1FAEE --prop size=14 \
  --prop text="[shape default: 14pt black]  Default paragraph styling."

# Each appended paragraph has a single style override — no runs needed
officecli add textboxes-advanced.pptx '/slide[3]/shape[2]' --type paragraph \
  --prop text="[bold=true at paragraph level]  Whole paragraph is bold." \
  --prop bold=true

officecli add textboxes-advanced.pptx '/slide[3]/shape[2]' --type paragraph \
  --prop text="[italic=true at paragraph level]  Whole paragraph is italic." \
  --prop italic=true

officecli add textboxes-advanced.pptx '/slide[3]/shape[2]' --type paragraph \
  --prop text="[color=E63946 at paragraph level]  Whole paragraph is red." \
  --prop color=E63946

officecli add textboxes-advanced.pptx '/slide[3]/shape[2]' --type paragraph \
  --prop text="[size=22 at paragraph level]  Whole paragraph is 22pt." \
  --prop size=22

officecli add textboxes-advanced.pptx '/slide[3]/shape[2]' --type paragraph \
  --prop text="[lang=fr-FR at paragraph level]  Lorem ipsum dolor sit amet." \
  --prop lang=fr-FR --prop color=2A9D8F
```

**Features:** `--type paragraph` with `bold`, `italic`, `color`, `size`, `lang` (BCP-47) — all applied at the paragraph level without explicit runs, cheaper when the whole paragraph shares one style

---

### Slide 4 — Per-Run Typography (font / size / spacing / kern / lang)

Four textboxes built run-by-run, demonstrating font mixing, character spacing, kerning, and per-run language tagging inside one paragraph.

```bash
officecli add textboxes-advanced.pptx / --type slide

# Font mixing — four fonts at different sizes in one paragraph
officecli add textboxes-advanced.pptx '/slide[4]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=13in --prop height=1in \
  --prop text="" --prop size=20

officecli add textboxes-advanced.pptx '/slide[4]/shape[1]/p[1]' --type run \
  --prop text="Mix "
officecli add textboxes-advanced.pptx '/slide[4]/shape[1]/p[1]' --type run \
  --prop text="Times " --prop font="Times New Roman" --prop size=24
officecli add textboxes-advanced.pptx '/slide[4]/shape[1]/p[1]' --type run \
  --prop text="Courier " --prop font="Courier New" --prop size=18
officecli add textboxes-advanced.pptx '/slide[4]/shape[1]/p[1]' --type run \
  --prop text="Georgia" --prop font="Georgia" --prop size=28 --prop bold=true

# Per-run character spacing (1/100 pt; negative = tighter, positive = looser)
officecli add textboxes-advanced.pptx '/slide[4]' --type textbox \
  --prop x=0.5in --prop y=3in --prop width=13in --prop height=1in \
  --prop text="" --prop size=20 --prop bold=true

officecli add textboxes-advanced.pptx '/slide[4]/shape[2]/p[1]' --type run \
  --prop text="Normal "
officecli add textboxes-advanced.pptx '/slide[4]/shape[2]/p[1]' --type run \
  --prop text="TIGHTENED " --prop spacing=-1 --prop color=E63946
officecli add textboxes-advanced.pptx '/slide[4]/shape[2]/p[1]' --type run \
  --prop text="LOOSENED " --prop spacing=4 --prop color=2A9D8F
officecli add textboxes-advanced.pptx '/slide[4]/shape[2]/p[1]' --type run \
  --prop text="EXPANDED" --prop spacing=8 --prop color=1D3557

# Per-run kerning threshold
officecli add textboxes-advanced.pptx '/slide[4]' --type textbox \
  --prop x=0.5in --prop y=4.3in --prop width=13in --prop height=1in \
  --prop text="" --prop size=20 --prop bold=true

officecli add textboxes-advanced.pptx '/slide[4]/shape[3]/p[1]' --type run \
  --prop text="AV AT WA — kern=0  " --prop kern=0
officecli add textboxes-advanced.pptx '/slide[4]/shape[3]/p[1]' --type run \
  --prop text="AV AT WA — kern=1" --prop kern=1 --prop color=E63946

# Per-run lang tag — drives spellcheck for each run independently
officecli add textboxes-advanced.pptx '/slide[4]' --type textbox \
  --prop x=0.5in --prop y=5.6in --prop width=13in --prop height=1in \
  --prop text="" --prop size=20

officecli add textboxes-advanced.pptx '/slide[4]/shape[4]/p[1]' --type run \
  --prop text="English: color  " --prop lang=en-US
officecli add textboxes-advanced.pptx '/slide[4]/shape[4]/p[1]' --type run \
  --prop text="British: colour  " --prop lang=en-GB --prop color=2A9D8F
officecli add textboxes-advanced.pptx '/slide[4]/shape[4]/p[1]' --type run \
  --prop text="Français: couleur" --prop lang=fr-FR --prop color=E63946
```

**Features:** `font` (per-run typeface override), `size` (per-run pt), `spacing` (character spacing in 1/100 pt per run), `kern` (kerning threshold in 1/100 pt per run), `lang` (BCP-47 per-run spellcheck tag)

---

### Slide 5 — subscript / superscript Aliases vs canonical baseline=

`subscript=true` and `superscript=true` are convenience aliases. `baseline=` accepts any signed integer percent for custom vertical offset.

```bash
officecli add textboxes-advanced.pptx / --type slide

# Convenience aliases: subscript=true / superscript=true
officecli add textboxes-advanced.pptx '/slide[5]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=13in --prop height=1in \
  --prop text="" --prop size=24

officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="H"
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="2" --prop subscript=true
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="SO"
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="4" --prop subscript=true
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="   x"
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="2" --prop superscript=true
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text=" + y"
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="2" --prop superscript=true
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text=" = r"
officecli add textboxes-advanced.pptx '/slide[5]/shape[1]/p[1]' --type run \
  --prop text="2" --prop superscript=true

# Custom baseline percent — neither alias supports this
officecli add textboxes-advanced.pptx '/slide[5]' --type textbox \
  --prop x=0.5in --prop y=3.7in --prop width=13in --prop height=1in \
  --prop text="" --prop size=24

officecli add textboxes-advanced.pptx '/slide[5]/shape[3]/p[1]' --type run \
  --prop text="Custom: "
officecli add textboxes-advanced.pptx '/slide[5]/shape[3]/p[1]' --type run \
  --prop text="50%" --prop baseline=50 --prop color=E63946
officecli add textboxes-advanced.pptx '/slide[5]/shape[3]/p[1]' --type run \
  --prop text=" higher  /  "
officecli add textboxes-advanced.pptx '/slide[5]/shape[3]/p[1]' --type run \
  --prop text="-40%" --prop baseline=-40 --prop color=2A9D8F
officecli add textboxes-advanced.pptx '/slide[5]/shape[3]/p[1]' --type run \
  --prop text=" lower"

# Per-run case: cap=small / cap=all / allCaps boolean alias
officecli add textboxes-advanced.pptx '/slide[5]' --type textbox \
  --prop x=0.5in --prop y=5.9in --prop width=13in --prop height=0.8in \
  --prop text="" --prop size=20 --prop bold=true

officecli add textboxes-advanced.pptx '/slide[5]/shape[4]/p[1]' --type run \
  --prop text="default  "
officecli add textboxes-advanced.pptx '/slide[5]/shape[4]/p[1]' --type run \
  --prop text="small caps  " --prop cap=small --prop color=2A9D8F
officecli add textboxes-advanced.pptx '/slide[5]/shape[4]/p[1]' --type run \
  --prop text="ALL CAPS" --prop allCaps=true --prop color=E63946
```

**Features:** `subscript` (true — alias for baseline=sub, approx -25%), `superscript` (true — alias for baseline=super, approx +30%), `baseline` (signed integer %; arbitrary vertical offset), `cap` (small, all, none — per-run), `allCaps` / `smallCaps` (boolean aliases for cap=all / cap=small)

---

### Slide 6 — name / zorder / autoFit / direction / font.cs (Textbox-Specific)

Textbox identity, stacking, overflow, and complex-script typography.

```bash
officecli add textboxes-advanced.pptx / --type slide

# name= — stable @name address for later targeting
officecli add textboxes-advanced.pptx '/slide[6]' --type textbox \
  --prop x=0.5in --prop y=1.2in --prop width=5in --prop height=1.5in \
  --prop fill=F1FAEE --prop size=16 --prop bold=true \
  --prop text="This is intro-box." \
  --prop name="intro-box"

# zorder= — three overlapping textboxes with explicit stack depth
officecli add textboxes-advanced.pptx '/slide[6]' --type textbox \
  --prop x=6in --prop y=1.2in --prop width=3in --prop height=2in \
  --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop size=16 \
  --prop text="back (zorder=1)" \
  --prop name="tb-back" --prop zorder=1

officecli add textboxes-advanced.pptx '/slide[6]' --type textbox \
  --prop x=7in --prop y=1.6in --prop width=3in --prop height=2in \
  --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop size=16 \
  --prop text="mid (zorder=2)" \
  --prop name="tb-mid" --prop zorder=2

officecli add textboxes-advanced.pptx '/slide[6]' --type textbox \
  --prop x=8in --prop y=2.0in --prop width=3in --prop height=2in \
  --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=16 \
  --prop text="front (zorder=3)" \
  --prop name="tb-front" --prop zorder=3

# autoFit=normal — text shrinks to fit a fixed-height box
LONGTEXT='Vivamus lacinia odio vitae vestibulum vestibulum. Sed molestie augue sit amet leo consequat posuere.'
officecli add textboxes-advanced.pptx '/slide[6]' --type textbox \
  --prop x=0.5in --prop y=3.6in --prop width=3in --prop height=1.2in \
  --prop fill=FFE66D --prop size=16 --prop text="$LONGTEXT" \
  --prop autoFit=normal

# direction=rtl + font.cs — RTL textbox with complex-script font
officecli add textboxes-advanced.pptx '/slide[6]' --type textbox \
  --prop x=0.5in --prop y=5.6in --prop width=5in --prop height=1.2in \
  --prop fill=A8DADC --prop size=20 --prop bold=true \
  --prop text="مرحبا بالعالم — 2026" \
  --prop direction=rtl --prop align=right \
  --prop font.cs="Arabic Typesetting"

officecli close textboxes-advanced.pptx
officecli validate textboxes-advanced.pptx
```

**Features:** `name` (stable identifier; addressable as `/slide[N]/shape[@name=...]`), `zorder` (integer stack depth; aliases: z-order, order), `autoFit` (normal — text shrinks), `direction` (rtl; aliases: dir, rtl), `font.cs` (complex-script font slot), `align=right` (combined with rtl for correct Arabic/Hebrew layout)

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Per-paragraph align override:** center, right, justify inside one textbox | 1 |
| **Per-paragraph lineSpacing override:** multiplier (2x) + fixed (18pt) | 1 |
| **marginLeft:** left indent (whole paragraph) | 2 |
| **indent:** first-line indent only (negative = hanging) | 2 |
| **marginRight:** narrow text from right edge | 2 |
| **Per-paragraph bold / italic:** whole paragraph style, no run needed | 3 |
| **Per-paragraph color / size:** whole paragraph style | 3 |
| **Per-paragraph lang:** BCP-47 spellcheck tag at paragraph scope | 3 |
| **Per-run font:** typeface override | 4 |
| **Per-run size:** point size override | 4 |
| **Per-run spacing:** character spacing in 1/100 pt | 4 |
| **Per-run kern:** kerning threshold in 1/100 pt | 4 |
| **Per-run lang:** BCP-47 spellcheck tag at run scope | 4 |
| **subscript / superscript:** boolean aliases for baseline=sub/super | 5 |
| **baseline:** signed integer % — arbitrary vertical offset | 5 |
| **Per-run cap:** small, all, none | 5 |
| **allCaps / smallCaps:** boolean aliases for cap=all/small | 5 |
| **name=:** stable @name addressing | 6 |
| **zorder=:** explicit stack depth | 6 |
| **autoFit=normal:** text shrinks to fit fixed box | 6 |
| **direction=rtl:** RTL paragraph direction in textbox | 6 |
| **font.cs:** complex-script font slot | 6 |

## Inspect the Generated File

```bash
# Check per-paragraph overrides in the slide 1 textbox
officecli get textboxes-advanced.pptx '/slide[1]/shape[2]'
officecli query textboxes-advanced.pptx '/slide[1]/shape[2]' paragraph

# Inspect indent values on slide 2
officecli get textboxes-advanced.pptx '/slide[2]/shape[3]'
officecli get textboxes-advanced.pptx '/slide[2]/shape[4]'

# Get per-run font data on slide 4 shape 1
officecli query textboxes-advanced.pptx '/slide[4]/shape[1]/p[1]' run

# Check baseline values on slide 5
officecli get textboxes-advanced.pptx '/slide[5]/shape[3]'

# Verify name + zorder on slide 6
officecli get textboxes-advanced.pptx '/slide[6]/shape[@name=tb-back]'
officecli get textboxes-advanced.pptx '/slide[6]/shape[@name=tb-front]'
```
