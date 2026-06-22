# Basic PPT Textboxes

This demo consists of three files that work together:

- **textboxes-basic.sh** — Shell script that calls `officecli` commands to generate the deck.
- **textboxes-basic.pptx** — The generated 4-slide deck (alignment, multi-paragraph lists, styled runs, multilingual fonts + layout).
- **textboxes-basic.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash textboxes/textboxes-basic.sh
# → textboxes/textboxes-basic.pptx
```

## Slides

### Slide 1 — Horizontal Alignment

Four textboxes with identical long text, one per `align=` value.

```bash
officecli create textboxes-basic.pptx
officecli open textboxes-basic.pptx
officecli add textboxes-basic.pptx / --type slide

LOREM='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus lacinia odio vitae vestibulum vestibulum.'

# align=left (default)
officecli add textboxes-basic.pptx '/slide[1]' --type textbox \
  --prop x=0.5in --prop y=1.3in --prop width=12in --prop height=1.3in \
  --prop fill=F1FAEE --prop text="[align=left] $LOREM" --prop size=14 \
  --prop align=left

# align=center
officecli add textboxes-basic.pptx '/slide[1]' --type textbox \
  --prop x=0.5in --prop y=2.8in --prop width=12in --prop height=1.3in \
  --prop fill=F1FAEE --prop text="[align=center] $LOREM" --prop size=14 \
  --prop align=center

# align=right
officecli add textboxes-basic.pptx '/slide[1]' --type textbox \
  --prop x=0.5in --prop y=4.3in --prop width=12in --prop height=1.3in \
  --prop fill=F1FAEE --prop text="[align=right] $LOREM" --prop size=14 \
  --prop align=right

# align=justify
officecli add textboxes-basic.pptx '/slide[1]' --type textbox \
  --prop x=0.5in --prop y=5.8in --prop width=12in --prop height=1.3in \
  --prop fill=F1FAEE --prop text="[align=justify] $LOREM" --prop size=14 \
  --prop align=justify
```

**Features:** `--type textbox` (alias for `--type shape` — both are textboxes in OOXML), `align` (left, center, right, justify), `fill`, `text`, `size`, `x`/`y`/`width`/`height`

---

### Slide 2 — Multi-Paragraph + Bulleted / Numbered Lists

Two textboxes: a bulleted list on the left and a numbered list with a sub-item on the right.

```bash
officecli add textboxes-basic.pptx / --type slide

# Bulleted list — add title paragraph first, then append body paragraphs,
# then apply list=bullet via Set to turn all paragraphs into bullets.
officecli add textboxes-basic.pptx '/slide[2]' --type textbox \
  --prop x=0.5in --prop y=1.2in --prop width=6in --prop height=4in \
  --prop text="Coffee preparation steps" \
  --prop bold=true --prop size=18 --prop color=1D3557

officecli add textboxes-basic.pptx '/slide[2]/shape[1]' --type paragraph \
  --prop text="Grind beans to medium-fine"
officecli add textboxes-basic.pptx '/slide[2]/shape[1]' --type paragraph \
  --prop text="Heat water to 93°C"
officecli add textboxes-basic.pptx '/slide[2]/shape[1]' --type paragraph \
  --prop text="Bloom 30s with 2× coffee weight"
officecli add textboxes-basic.pptx '/slide[2]/shape[1]' --type paragraph \
  --prop text="Pour remaining water in spirals"
officecli add textboxes-basic.pptx '/slide[2]/shape[1]' --type paragraph \
  --prop text="Total brew time: 3-4 minutes"

# Turn all paragraphs into bullets at shape level
officecli set textboxes-basic.pptx '/slide[2]/shape[1]' --prop list=bullet

# Numbered list — similar pattern
officecli add textboxes-basic.pptx '/slide[2]' --type textbox \
  --prop x=7in --prop y=1.2in --prop width=6in --prop height=4in \
  --prop text="Release checklist" \
  --prop bold=true --prop size=18 --prop color=1D3557

officecli add textboxes-basic.pptx '/slide[2]/shape[2]' --type paragraph \
  --prop text="Run tests"
officecli add textboxes-basic.pptx '/slide[2]/shape[2]' --type paragraph \
  --prop text="Tag the release"
officecli add textboxes-basic.pptx '/slide[2]/shape[2]' --type paragraph \
  --prop text="Push to registry"
officecli add textboxes-basic.pptx '/slide[2]/shape[2]' --type paragraph \
  --prop text="Announce in #releases"

officecli set textboxes-basic.pptx '/slide[2]/shape[2]' --prop list=numbered

# Sub-item: level=1 nests one step deeper
officecli add textboxes-basic.pptx '/slide[2]/shape[2]' --type paragraph \
  --prop text="(verify checksum)" --prop level=1
officecli set textboxes-basic.pptx '/slide[2]/shape[2]' --prop list=numbered
```

**Features:** `--type paragraph` (appends a paragraph to the parent shape), `list` (bullet, numbered — applied via `set` at shape level), `level` (indent depth; 0 = top-level, 1 = one indent, …)

---

### Slide 3 — Styled Runs (Rich Text)

Three textboxes demonstrating per-run styling within a single paragraph.

```bash
officecli add textboxes-basic.pptx / --type slide

# Empty textbox — filled run-by-run via --type run
officecli add textboxes-basic.pptx '/slide[3]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=12in --prop height=1in \
  --prop text="" --prop size=20

# Append runs with different styles to paragraph [1]
officecli add textboxes-basic.pptx '/slide[3]/shape[1]/p[1]' --type run \
  --prop text="The "
officecli add textboxes-basic.pptx '/slide[3]/shape[1]/p[1]' --type run \
  --prop text="quick " --prop bold=true --prop color=E63946
officecli add textboxes-basic.pptx '/slide[3]/shape[1]/p[1]' --type run \
  --prop text="brown " --prop italic=true --prop color=A0522D
officecli add textboxes-basic.pptx '/slide[3]/shape[1]/p[1]' --type run \
  --prop text="fox jumps over the "
officecli add textboxes-basic.pptx '/slide[3]/shape[1]/p[1]' --type run \
  --prop text="lazy " --prop underline=single --prop color=2A9D8F
officecli add textboxes-basic.pptx '/slide[3]/shape[1]/p[1]' --type run \
  --prop text="dog."

# Superscript / subscript via baseline= (also accepts baseline=super/sub)
officecli add textboxes-basic.pptx '/slide[3]' --type textbox \
  --prop x=0.5in --prop y=3in --prop width=12in --prop height=0.8in \
  --prop text="" --prop size=24

officecli add textboxes-basic.pptx '/slide[3]/shape[2]/p[1]' --type run \
  --prop text="E = mc"
officecli add textboxes-basic.pptx '/slide[3]/shape[2]/p[1]' --type run \
  --prop text="2" --prop baseline=super
officecli add textboxes-basic.pptx '/slide[3]/shape[2]/p[1]' --type run \
  --prop text="    and H"
officecli add textboxes-basic.pptx '/slide[3]/shape[2]/p[1]' --type run \
  --prop text="2" --prop baseline=sub
officecli add textboxes-basic.pptx '/slide[3]/shape[2]/p[1]' --type run \
  --prop text="O"

# Strikethrough + size override in one paragraph
officecli add textboxes-basic.pptx '/slide[3]' --type textbox \
  --prop x=0.5in --prop y=4.2in --prop width=12in --prop height=0.8in \
  --prop text="" --prop size=20

officecli add textboxes-basic.pptx '/slide[3]/shape[3]/p[1]' --type run \
  --prop text="OLD PRICE: \$99   " --prop strike=single --prop color=999999
officecli add textboxes-basic.pptx '/slide[3]/shape[3]/p[1]' --type run \
  --prop text="NOW \$49!" --prop bold=true --prop color=E63946 --prop size=24
```

**Features:** `--type run` (appends a run to the parent `p[N]` paragraph), `bold`, `italic`, `underline` (single, double, …), `strike` (single, double), `color` (hex), `size` (pt), `baseline` (super, sub, or signed integer %)

---

### Slide 4 — Multilingual Fonts + Vertical Alignment + Padding

Per-script font selection, vertical text alignment within a tall box, and inner margin padding.

```bash
officecli add textboxes-basic.pptx / --type slide

# Mixed-script textbox: font.latin for Latin text, font.ea for East Asian characters
officecli add textboxes-basic.pptx '/slide[4]' --type textbox \
  --prop x=0.5in --prop y=1.5in --prop width=6in --prop height=2in \
  --prop fill=F1FAEE --prop margin=0.2in \
  --prop text="Hello, 世界! こんにちは、世界。" \
  --prop size=24 --prop bold=true \
  --prop font.latin="Georgia" --prop font.ea="Yu Mincho"

# valign — vertical text position inside a tall fixed-height box
# top (default), middle, bottom
X=7
for va in top middle bottom; do
  officecli add textboxes-basic.pptx '/slide[4]' --type textbox \
    --prop x="${X}in" --prop y=1.5in --prop width=2in --prop height=3in \
    --prop fill=A8DADC --prop margin=0.15in \
    --prop text="valign=$va" --prop size=16 --prop bold=true \
    --prop valign="$va" --prop align=center
  X=$(echo "$X + 2.2" | bc -l)
done

officecli close textboxes-basic.pptx
officecli validate textboxes-basic.pptx
```

**Features:** `font.latin` (Latin script font slot), `font.ea` (East Asian script font slot), `valign` (top, middle, bottom), `margin` (uniform inner padding; also `marginLeft`, `marginRight`, `marginTop`, `marginBottom`), `align=center`

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **align:** left, center, right, justify | 1 |
| **fill:** hex color background | 1–4 |
| **--type paragraph:** append paragraph to existing shape | 2 |
| **list=bullet:** bulleted list at shape level | 2 |
| **list=numbered:** numbered list at shape level | 2 |
| **level:** indent depth for sub-items (0=top, 1=nested, …) | 2 |
| **--type run:** append styled run to a paragraph | 3 |
| **bold / italic:** per-run weight and style | 3 |
| **underline:** single, double, heavy, dotted, dash | 3 |
| **strike:** single, double | 3 |
| **color:** per-run text color (hex) | 3 |
| **size:** per-run font size (pt) | 3 |
| **baseline:** super, sub, or signed integer % offset | 3 |
| **font.latin / font.ea:** per-script font slots | 4 |
| **valign:** top, middle, bottom | 4 |
| **margin:** uniform inner text padding | 4 |
| **align=center:** horizontal center within valign demo | 4 |

## Inspect the Generated File

```bash
# List shapes on slide 1
officecli query textboxes-basic.pptx '/slide[1]' shape

# Get alignment on each textbox
officecli get textboxes-basic.pptx '/slide[1]/shape[2]'

# Check list type on slide 2 shapes
officecli get textboxes-basic.pptx '/slide[2]/shape[1]'
officecli get textboxes-basic.pptx '/slide[2]/shape[2]'

# Inspect paragraph [1] runs on slide 3
officecli query textboxes-basic.pptx '/slide[3]/shape[1]/p[1]' run

# Get valign and font on slide 4
officecli get textboxes-basic.pptx '/slide[4]/shape[1]'
officecli get textboxes-basic.pptx '/slide[4]/shape[3]'
```
