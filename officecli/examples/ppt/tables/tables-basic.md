# Basic PPT Tables

This demo consists of three files that work together:

- **tables-basic.sh** — Shell script that calls `officecli` commands to generate the deck.
- **tables-basic.pptx** — The generated 5-slide deck (inline data, per-cell set, fill variations, cell typography, cell layout).
- **tables-basic.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash tables/tables-basic.sh
# → tables/tables-basic.pptx
```

## Slides

### Slide 1 — Inline Data (CSV via data=)

A single command creates a fully populated table using `data=` CSV syntax.

```bash
officecli create tables-basic.pptx
officecli open tables-basic.pptx
officecli add tables-basic.pptx / --type slide

# data= uses CSV: comma = cell separator, semicolon = row separator.
# First row becomes the header row when headerFill= is set.
officecli add tables-basic.pptx '/slide[1]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=2in \
  --prop headerFill=4472C4 --prop bodyFill=DEEAF6 \
  --prop data="Region,Q1,Q2,Q3,Q4;North,120,135,142,168;South,98,110,121,140;East,165,178,190,205"
```

**Features:** `--type table`, `data` (CSV; `,` = cell, `;` = row), `headerFill` (header row background hex color), `bodyFill` (body rows background hex color), `x`/`y`/`width`/`height` (position and size)

---

### Slide 2 — Per-Cell Set

Create an empty grid with `rows=` and `cols=`, then populate each cell individually via `set`.

```bash
officecli add tables-basic.pptx / --type slide

# Empty table skeleton — no data, just dimensions
officecli add tables-basic.pptx '/slide[2]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=10in --prop height=2.5in \
  --prop rows=4 --prop cols=3 --prop headerFill=2E75B6

# Header cells: tr[1] = first row, tc[N] = Nth column (1-based)
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[1]/tc[1]' \
  --prop text="Product" --prop bold=true --prop color=FFFFFF
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[1]/tc[2]' \
  --prop text="Units" --prop bold=true --prop color=FFFFFF
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[1]/tc[3]' \
  --prop text="Revenue" --prop bold=true --prop color=FFFFFF

# Body cells — row 2..4
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[2]/tc[1]' --prop text="Widget"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[2]/tc[2]' --prop text="1,200"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[2]/tc[3]' --prop text="\$48,000"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[3]/tc[1]' --prop text="Gizmo"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[3]/tc[2]' --prop text="850"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[3]/tc[3]' --prop text="\$72,250"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[4]/tc[1]' --prop text="Sprocket"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[4]/tc[2]' --prop text="430"
officecli set tables-basic.pptx '/slide[2]/table[1]/tr[4]/tc[3]' --prop text="\$25,800"
```

**Features:** `rows` / `cols` (create empty grid), path syntax `/table[N]/tr[R]/tc[C]` (all 1-based), `text`, `bold`, `color` (text color hex)

---

### Slide 3 — Cell Fill Variations

Every fill syntax demonstrated in a two-column reference table.

```bash
officecli add tables-basic.pptx / --type slide

# style=none disables the built-in theme; border.all applies a uniform border
officecli add tables-basic.pptx '/slide[3]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=4in \
  --prop rows=5 --prop cols=2 --prop style=none --prop border.all="1pt solid 808080"

# Solid hex fill
officecli set tables-basic.pptx '/slide[3]/table[1]/tr[2]/tc[2]' --prop fill=FF0000

# Named color (also accepts rgb(255,0,0) form)
officecli set tables-basic.pptx '/slide[3]/table[1]/tr[3]/tc[2]' --prop fill=red

# Theme color — follows deck theme; changes when theme changes
officecli set tables-basic.pptx '/slide[3]/table[1]/tr[4]/tc[2]' --prop fill=accent1

# Gradient — "COLOR1-COLOR2-ANGLE"
officecli set tables-basic.pptx '/slide[3]/table[1]/tr[5]/tc[2]' \
  --prop fill="FF0000-0000FF-90"

# fill=none — explicit transparent (cell becomes slide-background color)
officecli add tables-basic.pptx '/slide[3]' --type table \
  --prop x=0.5in --prop y=5.9in --prop width=4in --prop height=0.8in \
  --prop rows=1 --prop cols=2 --prop style=none --prop border.all="1pt solid 000000"
officecli set tables-basic.pptx '/slide[3]/table[2]/tr[1]/tc[1]' \
  --prop text="solid" --prop fill=FFE699
officecli set tables-basic.pptx '/slide[3]/table[2]/tr[1]/tc[2]' \
  --prop text="none" --prop fill=none
```

**Features:** `fill` (hex, named color, rgb(), accent1..6 theme colors, `C1-C2-ANGLE` gradient, none), `style=none` (disable built-in theme), `border.all` (compound `Npt solid HEX` shorthand)

---

### Slide 4 — Cell Typography

Seven-row reference table demonstrating every text property available on table cells.

```bash
officecli add tables-basic.pptx / --type slide

officecli add tables-basic.pptx '/slide[4]' --type table \
  --prop x=0.5in --prop y=1.1in --prop width=13in --prop height=5in \
  --prop rows=7 --prop cols=2 --prop style=none --prop border.all="1pt solid 808080"

# italic
officecli set tables-basic.pptx '/slide[4]/table[1]/tr[2]/tc[2]' \
  --prop text="This cell text is italic." --prop italic=true

# underline
officecli set tables-basic.pptx '/slide[4]/table[1]/tr[3]/tc[2]' \
  --prop text="This cell text is underlined." --prop underline=single

# strike
officecli set tables-basic.pptx '/slide[4]/table[1]/tr[4]/tc[2]' \
  --prop text="This cell text has strikethrough." --prop strike=single

# font + size
officecli set tables-basic.pptx '/slide[4]/table[1]/tr[5]/tc[2]' \
  --prop text="This cell uses Georgia." --prop font="Georgia" --prop size=16

# wrap=false — text overflows/clips rather than wrapping to next line
officecli set tables-basic.pptx '/slide[4]/table[1]/tr[6]/tc[2]' \
  --prop text="This is a long sentence that will not wrap because wrap is disabled — it just runs off the edge." \
  --prop wrap=false

# linespacing + spacebefore + spaceafter — paragraph spacing inside cell
officecli set tables-basic.pptx '/slide[4]/table[1]/tr[7]/tc[2]' \
  --prop text="Paragraph A" \
  --prop linespacing=1.5x --prop spacebefore=4pt --prop spaceafter=4pt
```

**Features:** `italic`, `underline` (single, double, heavy, dotted, dash), `strike` (single, double), `font` (typeface name), `size` (pt), `wrap` (false — disables text wrap), `linespacing` (multiplier or pt), `spacebefore` / `spaceafter` (pt)

---

### Slide 5 — Cell Layout

Eight-row reference table demonstrating padding, opacity, image fill, text direction, merge, and per-edge border.

```bash
officecli add tables-basic.pptx / --type slide

officecli add tables-basic.pptx '/slide[5]' --type table \
  --prop x=0.5in --prop y=1.1in --prop width=13in --prop height=6.2in \
  --prop rows=8 --prop cols=2 --prop style=none --prop border.all="1pt solid 808080"

# padding — uniform inner text margin
officecli set tables-basic.pptx '/slide[5]/table[1]/tr[2]/tc[2]' \
  --prop text="Large inner margin." --prop fill=F1FAEE --prop padding=0.25in

# padding.bottom — single-edge padding override
officecli set tables-basic.pptx '/slide[5]/table[1]/tr[3]/tc[2]' \
  --prop text="Extra space below this text." --prop fill=F1FAEE \
  --prop "padding.bottom=0.3in"

# opacity — fill transparency (0=opaque, 1=invisible); requires fill
officecli set tables-basic.pptx '/slide[5]/table[1]/tr[4]/tc[2]' \
  --prop text="40% transparent fill." --prop fill=4472C4 --prop opacity=0.4

# image — blipFill (picture fill on the cell background)
officecli set tables-basic.pptx '/slide[5]/table[1]/tr[5]/tc[2]' \
  --prop image="/path/to/img.png"

# textdirection=vert — vertical text rendering inside a cell
officecli set tables-basic.pptx '/slide[5]/table[1]/tr[6]/tc[2]' \
  --prop text="Vertical text" --prop textdirection=vert --prop fill=FFE699

# direction=rtl — RTL paragraph direction within a cell
officecli set tables-basic.pptx '/slide[5]/table[1]/tr[7]/tc[2]' \
  --prop text="مرحبا" --prop direction=rtl --prop size=18 --prop fill=A8DADC

# merge.right + bevel + border.right — combined cell properties
officecli set tables-basic.pptx '/slide[5]/table[1]/tr[8]/tc[2]' \
  --prop text="Merged, beveled, custom right border." \
  --prop fill=F4A261 --prop bevel=circle \
  --prop "border.right=2pt solid E63946"

officecli close tables-basic.pptx
officecli validate tables-basic.pptx
```

**Features:** `padding` (uniform; also `padding.top`, `padding.right`, `padding.bottom`, `padding.left`), `opacity` (0.0–1.0; requires fill), `image` (file path — blipFill on cell background), `textdirection` (vert — vertical text), `direction` (rtl — RTL paragraph), `bevel` (3D bevel preset), `border.right` (per-edge: `Npt solid HEX`)

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **data=:** CSV inline population (`,` = cell, `;` = row) | 1 |
| **headerFill / bodyFill:** header + body background colors | 1, 2 |
| **rows / cols:** empty grid creation | 2 |
| **Per-cell set:** `/table[N]/tr[R]/tc[C]` path targeting (1-based) | 2–5 |
| **fill:** hex, named, rgb(), accent1..6, gradient, none | 3 |
| **style=none:** disable built-in table theme | 3–5 |
| **border.all:** shorthand uniform border (`Npt solid HEX`) | 3–5 |
| **italic / underline / strike:** cell text decoration | 4 |
| **font / size:** cell typeface and point size | 4 |
| **wrap=false:** no text wrapping | 4 |
| **linespacing / spacebefore / spaceafter:** paragraph spacing in cell | 4 |
| **padding:** uniform inner margin (also per-edge variants) | 5 |
| **opacity:** fill transparency | 5 |
| **image=:** picture fill on cell background | 5 |
| **textdirection=vert:** vertical text in cell | 5 |
| **direction=rtl:** RTL paragraph in cell | 5 |
| **bevel:** 3D bevel on cell | 5 |
| **border.right / border.left / …:** per-edge borders | 5 |

## Inspect the Generated File

```bash
# List tables on each slide
officecli query tables-basic.pptx '/slide[1]' table
officecli query tables-basic.pptx '/slide[3]' table

# Get fill properties on slide 3
officecli get tables-basic.pptx '/slide[3]/table[1]/tr[2]/tc[2]'
officecli get tables-basic.pptx '/slide[3]/table[1]/tr[5]/tc[2]'

# Check typography on slide 4
officecli get tables-basic.pptx '/slide[4]/table[1]/tr[5]/tc[2]'
officecli get tables-basic.pptx '/slide[4]/table[1]/tr[7]/tc[2]'

# Inspect layout properties on slide 5
officecli get tables-basic.pptx '/slide[5]/table[1]/tr[4]/tc[2]'
officecli get tables-basic.pptx '/slide[5]/table[1]/tr[8]/tc[2]'
```
