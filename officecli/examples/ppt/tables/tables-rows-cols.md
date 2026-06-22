# PPT Table Rows and Columns

This demo consists of three files that work together:

- **tables-rows-cols.sh** — Shell script that calls `officecli` commands to generate the deck.
- **tables-rows-cols.pptx** — The generated 4-slide deck (grow a table, per-row/col sizing, uniform rowHeight, cell merging).
- **tables-rows-cols.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash tables/tables-rows-cols.sh
# → tables/tables-rows-cols.pptx
```

## Slides

### Slide 1 — Grow a Table (add row / add column)

Two side-by-side tables start small and grow via `--type row` and `--type column`. The left table uses `style=medium2` (theme auto-inherits new cells); the right uses `headerFill/bodyFill` (per-cell stamp — new cells need manual fill).

```bash
officecli create tables-rows-cols.pptx
officecli open tables-rows-cols.pptx
officecli add tables-rows-cols.pptx /presentation/slides --type slide

# === LEFT: Table A — style=medium2 (auto-inherits new rows/columns) ===
officecli add tables-rows-cols.pptx '/slide[1]' --type table \
  --prop x=0.5in --prop y=1.5in --prop width=2in --prop height=1.5in \
  --prop style=medium2 --prop firstRow=true --prop bandedRows=true --prop lastCol=true \
  --prop data="Name,H1;Alice,220"

# Append rows — theme styling inherits automatically
officecli add tables-rows-cols.pptx '/slide[1]/table[1]' --type row \
  --prop c1=Bob --prop c2=205
officecli add tables-rows-cols.pptx '/slide[1]/table[1]' --type row \
  --prop c1=Carol --prop c2=275

# Append columns — text= seeds the header cell
officecli add tables-rows-cols.pptx '/slide[1]/table[1]' --type column \
  --prop width=1in --prop text="H2"
officecli add tables-rows-cols.pptx '/slide[1]/table[1]' --type column \
  --prop width=1in --prop text="Total"

# Fill in body cells for the new columns
officecli set tables-rows-cols.pptx '/slide[1]/table[1]/tr[2]/tc[3]' --prop text="245"
officecli set tables-rows-cols.pptx '/slide[1]/table[1]/tr[3]/tc[3]' --prop text="225"
officecli set tables-rows-cols.pptx '/slide[1]/table[1]/tr[4]/tc[3]' --prop text="335"
officecli set tables-rows-cols.pptx '/slide[1]/table[1]/tr[2]/tc[4]' --prop text="465" --prop bold=true
officecli set tables-rows-cols.pptx '/slide[1]/table[1]/tr[3]/tc[4]' --prop text="430" --prop bold=true
officecli set tables-rows-cols.pptx '/slide[1]/table[1]/tr[4]/tc[4]' --prop text="610" --prop bold=true

# === RIGHT: Table B — headerFill/bodyFill (per-cell stamp; must top-up manually) ===
officecli add tables-rows-cols.pptx '/slide[1]' --type table \
  --prop x=7in --prop y=1.5in --prop width=2in --prop height=1.5in \
  --prop headerFill=4472C4 --prop bodyFill=DEEAF6 \
  --prop data="Name,H1;Alice,220"

officecli add tables-rows-cols.pptx '/slide[1]/table[2]' --type row \
  --prop c1=Bob --prop c2=205
officecli add tables-rows-cols.pptx '/slide[1]/table[2]' --type row \
  --prop c1=Carol --prop c2=275
officecli add tables-rows-cols.pptx '/slide[1]/table[2]' --type column \
  --prop width=1in --prop text="H2"
officecli add tables-rows-cols.pptx '/slide[1]/table[2]' --type column \
  --prop width=1in --prop text="Total"

# Must manually stamp fill on all new cells — headerFill/bodyFill is a one-shot
HDR=4472C4; BODY=DEEAF6; SUM=B4C7E7
officecli set tables-rows-cols.pptx '/slide[1]/table[2]/tr[1]/tc[3]' \
  --prop fill=$HDR --prop color=FFFFFF --prop bold=true
for r in 2 3 4; do
  officecli set tables-rows-cols.pptx "/slide[1]/table[2]/tr[$r]/tc[3]" --prop fill=$BODY
done
officecli set tables-rows-cols.pptx '/slide[1]/table[2]/tr[1]/tc[4]' \
  --prop fill=$HDR --prop color=FFFFFF --prop bold=true
for r in 2 3 4; do
  officecli set tables-rows-cols.pptx "/slide[1]/table[2]/tr[$r]/tc[4]" \
    --prop fill=$SUM --prop bold=true
done
```

**Features:** `--type row` (appends a row; `c1=`, `c2=`, … seed cell text), `--type column` (appends a column; `text=` seeds the header cell, `width=` sets column width), style=medium2 auto-inherits new cells vs manual per-cell fill top-up

---

### Slide 2 — Per-Row Heights and Per-Column Widths

Individual rows and columns resized after table creation using `set /tr[N]` and `set /col[N]`.

```bash
officecli add tables-rows-cols.pptx /presentation/slides --type slide

officecli add tables-rows-cols.pptx '/slide[2]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=4in \
  --prop rows=4 --prop cols=4 --prop headerFill=2E75B6

# Set per-column widths (total ~12in)
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/col[1]' --prop width=2in
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/col[2]' --prop width=1.5in
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/col[3]' --prop width=7in
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/col[4]' --prop width=1.5in

# Set per-row heights
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[1]' --prop height=0.5in
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[2]' --prop height=0.6in
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[3]' --prop height=1in
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[4]' --prop height=1.5in

# Fill in cells (abbreviated)
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[1]/tc[1]' \
  --prop text="Field" --prop bold=true --prop color=FFFFFF
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[2]/tc[3]' \
  --prop text="Standard row height (0.6in)"
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[3]/tc[3]' \
  --prop text="Taller row (1in) for emphasis"
officecli set tables-rows-cols.pptx '/slide[2]/table[1]/tr[4]/tc[3]' \
  --prop text="Tallest row (1.5in) — multi-line content"
```

**Features:** `set /table[N]/col[C]` with `width=` (per-column width resize, in/cm/pt), `set /table[N]/tr[R]` with `height=` (per-row height resize, in/cm/pt)

---

### Slide 3 — Uniform rowHeight (table-level)

Setting `rowHeight=` at table creation time stamps every row at once — no per-row set commands needed.

```bash
officecli add tables-rows-cols.pptx /presentation/slides --type slide

# rowHeight= at add-table time → every row gets this height
officecli add tables-rows-cols.pptx '/slide[3]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in \
  --prop rows=5 --prop cols=3 --prop rowHeight=0.8in \
  --prop headerFill=1F4E79 --prop bodyFill=F2F2F2 \
  --prop data="Step,Action,Result;1,Init,OK;2,Process,OK;3,Verify,OK;4,Commit,OK"
```

**Features:** `rowHeight` (uniform; in/cm/pt — stamps all rows at creation time; equivalent to calling `set /tr[N] --prop height=` on every row)

---

### Slide 4 — Cell Merging (gridSpan horizontal + merge.down vertical)

Two tables demonstrating both axes of merging in OOXML tables.

```bash
officecli add tables-rows-cols.pptx /presentation/slides --type slide

# === TOP TABLE: gridSpan=N — full-width footnote row ===
officecli add tables-rows-cols.pptx '/slide[4]' --type table \
  --prop x=0.5in --prop y=1.5in --prop width=12in --prop height=1.5in \
  --prop headerFill=2E75B6 \
  --prop data="Q1,Q2,Q3,Q4;100,120,135,150"

# Append a normal row, then merge all 4 cells via gridSpan on tc[1]
officecli add tables-rows-cols.pptx '/slide[4]/table[1]' --type row \
  --prop c1="Footnote: figures in thousands USD, unaudited."
officecli set tables-rows-cols.pptx '/slide[4]/table[1]/tr[3]/tc[1]' \
  --prop gridSpan=4 --prop fill=F2F2F2 --prop bold=true

# === BOTTOM TABLE: merge.down=N — grouped row labels ===
# merge.down=N makes a cell span N+1 rows total (anchor + N continuation rows).
officecli add tables-rows-cols.pptx '/slide[4]' --type table \
  --prop x=0.5in --prop y=3.8in --prop width=12in --prop height=3in \
  --prop headerFill=2E75B6 --prop rowHeight=0.5in \
  --prop data="Region,Month,Sales,Notes;North,Jan,120,;North,Feb,135,;North,Mar,142,;South,Jan,98,;South,Feb,110,"

# "North" label spans rows 2..4 (merge.down=2 = anchor row + 2 continuations)
officecli set tables-rows-cols.pptx '/slide[4]/table[2]/tr[2]/tc[1]' \
  --prop merge.down=2 --prop bold=true --prop fill=DEEAF6 --prop valign=middle

# "South" label spans rows 5..6 (merge.down=1 = anchor + 1 continuation)
officecli set tables-rows-cols.pptx '/slide[4]/table[2]/tr[5]/tc[1]' \
  --prop merge.down=1 --prop bold=true --prop fill=DEEAF6 --prop valign=middle

officecli close tables-rows-cols.pptx
officecli validate tables-rows-cols.pptx
```

**Features:** `gridSpan=N` (horizontal merge — anchor spans N columns; continuation cells skipped), `merge.down=N` (vertical merge — anchor spans N+1 rows total via OOXML `rowSpan` + `vMerge`), `valign=middle` (center text vertically in merged cell)

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **--type row:** append a row to an existing table | 1 |
| **c1= c2= … cN=:** seed text for appended row cells | 1 |
| **--type column:** append a column to an existing table | 1 |
| **column text=:** seed header cell of appended column | 1 |
| **column width=:** set width of appended column | 1 |
| **style=medium2 auto-inherit:** theme applies to new rows/cols | 1 |
| **Manual fill top-up:** headerFill/bodyFill is one-shot at creation | 1 |
| **set /col[C] width=:** resize individual column width | 2 |
| **set /tr[R] height=:** resize individual row height | 2 |
| **rowHeight=:** uniform height for all rows at creation time | 3 |
| **gridSpan=N:** horizontal cell merge | 4 |
| **merge.down=N:** vertical cell merge (rowSpan + vMerge) | 4 |
| **valign=middle:** center text in merged cell | 4 |

## Inspect the Generated File

```bash
# List tables on each slide
officecli query tables-rows-cols.pptx '/slide[1]' table
officecli query tables-rows-cols.pptx '/slide[4]' table

# Compare the two table styles after grow on slide 1
officecli get tables-rows-cols.pptx '/slide[1]/table[1]'
officecli get tables-rows-cols.pptx '/slide[1]/table[2]'

# Check per-column widths on slide 2
officecli get tables-rows-cols.pptx '/slide[2]/table[1]/col[3]'

# Check per-row heights on slide 2
officecli get tables-rows-cols.pptx '/slide[2]/table[1]/tr[4]'

# Verify gridSpan on the footnote cell (slide 4 top table)
officecli get tables-rows-cols.pptx '/slide[4]/table[1]/tr[3]/tc[1]'

# Verify merge.down on "North" grouped label (slide 4 bottom table)
officecli get tables-rows-cols.pptx '/slide[4]/table[2]/tr[2]/tc[1]'
```
