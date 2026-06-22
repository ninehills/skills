# Styled PPT Tables — Built-in Styles and Banding

This demo consists of three files that work together:

- **tables-styled.sh** — Shell script that calls `officecli` commands to generate the deck.
- **tables-styled.pptx** — The generated 11-slide deck (9 style presets + banding flag combinations + rowHeight/name addressing).
- **tables-styled.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash tables/tables-styled.sh
# → tables/tables-styled.pptx
```

## Slides

### Slides 1–9 — One Style per Slide (medium1..4, light1..3, dark1..2)

Each slide adds a single table with one of the 9 built-in style presets and `firstRow=true + bandedRows=true`.

```bash
officecli create tables-styled.pptx
officecli open tables-styled.pptx

DATA="Region,Q1,Q2,Q3,Q4;North,120,135,142,168;South,98,110,121,140;East,165,178,190,205;West,140,155,168,182"

# Slide 1: style=medium1
officecli add tables-styled.pptx /presentation/slides --type slide
officecli add tables-styled.pptx '/slide[1]' --type shape \
  --prop text="style=medium1" --prop size=28 --prop bold=true \
  --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in
officecli add tables-styled.pptx '/slide[1]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=3in \
  --prop style=medium1 \
  --prop firstRow=true --prop bandedRows=true \
  --prop data="$DATA"

# Slide 2: style=medium2  (repeat pattern for medium3, medium4)
officecli add tables-styled.pptx /presentation/slides --type slide
officecli add tables-styled.pptx '/slide[2]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=3in \
  --prop style=medium2 --prop firstRow=true --prop bandedRows=true \
  --prop data="$DATA"

# Slide 5: style=light1  (repeat pattern for light2, light3)
officecli add tables-styled.pptx /presentation/slides --type slide
officecli add tables-styled.pptx '/slide[5]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=3in \
  --prop style=light1 --prop firstRow=true --prop bandedRows=true \
  --prop data="$DATA"

# Slide 8: style=dark1  (repeat for dark2)
officecli add tables-styled.pptx /presentation/slides --type slide
officecli add tables-styled.pptx '/slide[8]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=3in \
  --prop style=dark1 --prop firstRow=true --prop bandedRows=true \
  --prop data="$DATA"
```

**Features:** `style` (medium1, medium2, medium3, medium4, light1, light2, light3, dark1, dark2, none), `firstRow` (highlight first row), `bandedRows` (alternating row shading), `data` (CSV)

---

### Slide 10 — Banding Flag Combinations

Four tables on one slide demonstrating every banding flag combination using `style=medium2`.

```bash
officecli add tables-styled.pptx /presentation/slides --type slide

# firstRow + bandedRows — typical: highlighted header + alternating body
officecli add tables-styled.pptx '/slide[10]' --type table \
  --prop x=0.5in --prop y=1.4in --prop width=6in --prop height=2.5in \
  --prop style=medium2 --prop firstRow=true --prop bandedRows=true \
  --prop data="$DATA"

# firstCol + bandedCols — vertical emphasis with alternating column shading
officecli add tables-styled.pptx '/slide[10]' --type table \
  --prop x=7in --prop y=1.4in --prop width=6in --prop height=2.5in \
  --prop style=medium2 --prop firstCol=true --prop bandedCols=true \
  --prop data="$DATA"

# firstRow + lastRow — header + total row emphasis
officecli add tables-styled.pptx '/slide[10]' --type table \
  --prop x=0.5in --prop y=4.7in --prop width=6in --prop height=2.5in \
  --prop style=medium2 --prop firstRow=true --prop lastRow=true \
  --prop data="$DATA;Total,523,578,621,695"

# style=none with manual border — fallback for unstyled tables
officecli add tables-styled.pptx '/slide[10]' --type table \
  --prop x=7in --prop y=4.7in --prop width=6in --prop height=2.5in \
  --prop style=none --prop border.all="1pt solid 808080" \
  --prop data="$DATA"
```

**Features:** `firstRow` (highlight first row), `lastRow` (highlight last/total row), `firstCol` (highlight first column), `lastCol` (highlight last column), `bandedRows` (alternating row shading), `bandedCols` (alternating column shading)

---

### Slide 11 — rowHeight + name= Addressing

Demonstrates uniform `rowHeight=` at table creation time and stable `@name` addressing for later cell updates.

```bash
officecli add tables-styled.pptx /presentation/slides --type slide

# rowHeight= stamps every row at creation — no need to set each row individually
officecli add tables-styled.pptx '/slide[11]' --type table \
  --prop x=0.5in --prop y=2in --prop width=12in \
  --prop rows=5 --prop cols=4 --prop rowHeight=1cm \
  --prop name=SalesData --prop style=medium2 --prop firstRow=true \
  --prop data="Region,Q1,Q2,Q3;North,120,135,142;South,98,110,121;East,165,178,190;West,140,155,168"

# name= enables stable @name addressing — use instead of positional table[N]
# Handy when slides are reordered or tables inserted/removed later.
officecli set tables-styled.pptx '/slide[11]/table[@name=SalesData]/tr[2]/tc[2]' \
  --prop text="120 ▲" --prop bold=true --prop fill=C6E0B4

officecli close tables-styled.pptx
officecli validate tables-styled.pptx
```

**Features:** `rowHeight` (uniform row height at table-creation time; accepts in, cm, pt), `name` (stable table identifier; addressable as `/table[@name=...]` instead of positional `/table[N]`)

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **style presets:** medium1..4, light1..3, dark1..2, none | 1–9, 10 |
| **firstRow:** highlight header row | 1–10 |
| **lastRow:** highlight total/footer row | 10 |
| **firstCol:** highlight first column | 10 |
| **lastCol:** highlight last column | 10 |
| **bandedRows:** alternating row shading | 1–10 |
| **bandedCols:** alternating column shading | 10 |
| **style=none + border.all:** manual unstyled table | 10 |
| **rowHeight=:** uniform row height (in/cm/pt) | 11 |
| **name=:** stable @name addressing for tables | 11 |

## Inspect the Generated File

```bash
# List all tables across slides
officecli query tables-styled.pptx '/slide[1]' table
officecli query tables-styled.pptx '/slide[10]' table

# Get style properties on a specific table
officecli get tables-styled.pptx '/slide[1]/table[1]'
officecli get tables-styled.pptx '/slide[8]/table[1]'

# Inspect banding flags on slide 10
officecli get tables-styled.pptx '/slide[10]/table[1]'
officecli get tables-styled.pptx '/slide[10]/table[2]'

# Read back the named table and the modified cell
officecli get tables-styled.pptx '/slide[11]/table[@name=SalesData]'
officecli get tables-styled.pptx '/slide[11]/table[@name=SalesData]/tr[2]/tc[2]'
```
