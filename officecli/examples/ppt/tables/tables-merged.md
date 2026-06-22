# Merged Cells in PPT Tables

This demo consists of three files that work together:

- **tables-merged.sh** — Shell script that calls `officecli` commands to generate the deck.
- **tables-merged.pptx** — The generated 2-slide deck (two-level header with gridSpan, full-width section header rows).
- **tables-merged.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash tables/tables-merged.sh
# → tables/tables-merged.pptx
```

> `officecli` supports both horizontal merging (`gridSpan`) and vertical merging (`merge.down`). This file covers `gridSpan`; see `tables-rows-cols.md` slide 4 for `merge.down` examples.

## Slides

### Slide 1 — Two-Level Header (gridSpan on Row 1)

A 6-row × 5-column performance table with two header rows: the first row spans columns 2–3 and 4–5 via `gridSpan=2` to form super-headers.

```bash
officecli create tables-merged.pptx
officecli open tables-merged.pptx
officecli add tables-merged.pptx /presentation/slides --type slide

# Create the empty table grid
officecli add tables-merged.pptx '/slide[1]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=3.5in \
  --prop rows=6 --prop cols=5 --prop headerFill=2E75B6 --prop bodyFill=DEEAF6

# Row 1: super-headers — tc[2] spans 2 columns, tc[4] spans 2 columns
# tc[1]: label; tc[2]: "2024 Performance" spanning cols 2–3
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[1]/tc[1]' \
  --prop text="Department" --prop bold=true --prop color=FFFFFF --prop align=center
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[1]/tc[2]' \
  --prop text="2024 Performance" --prop bold=true --prop color=FFFFFF --prop align=center \
  --prop gridSpan=2

# tc[3] is now a continuation cell from gridSpan=2 — skip directly to tc[4]
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[1]/tc[4]' \
  --prop text="2025 Forecast" --prop bold=true --prop color=FFFFFF --prop align=center \
  --prop gridSpan=2

# Row 2: sub-headers (one per logical column, lighter shade)
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[2]/tc[1]' \
  --prop text="" --prop fill=5B9BD5
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[2]/tc[2]' \
  --prop text="Revenue" --prop bold=true --prop color=FFFFFF --prop align=center --prop fill=5B9BD5
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[2]/tc[3]' \
  --prop text="Margin" --prop bold=true --prop color=FFFFFF --prop align=center --prop fill=5B9BD5
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[2]/tc[4]' \
  --prop text="Revenue" --prop bold=true --prop color=FFFFFF --prop align=center --prop fill=5B9BD5
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[2]/tc[5]' \
  --prop text="Margin" --prop bold=true --prop color=FFFFFF --prop align=center --prop fill=5B9BD5

# Body rows 3–6
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[3]/tc[1]' \
  --prop text="Engineering" --prop bold=true
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[3]/tc[2]' --prop text="1.20M" --prop align=right
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[3]/tc[3]' --prop text="18%" --prop align=right
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[3]/tc[4]' --prop text="1.45M" --prop align=right
officecli set tables-merged.pptx '/slide[1]/table[1]/tr[3]/tc[5]' --prop text="22%" --prop align=right
# … repeat for Sales, Marketing, Operations rows
```

**Features:** `gridSpan` (horizontal span — `gridSpan=N` on the anchor cell; continuation cells are skipped in subsequent `set` calls), `align=center` (centers text in merged cell), `fill` (per-cell fill override)

> After setting `gridSpan=2` on `tc[2]`, the next physical cell `tc[3]` is consumed by the span. Do not set `tc[3]` — jump to `tc[4]` for the next visible header. Attempting to write `tc[3]` will overwrite the continuation marker and corrupt the merge.

---

### Slide 2 — Full-Width Section Headers (gridSpan=4)

A 9-row × 4-column project tracker where section header rows span all 4 columns.

```bash
officecli add tables-merged.pptx /presentation/slides --type slide

officecli add tables-merged.pptx '/slide[2]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=4.5in \
  --prop rows=9 --prop cols=4 --prop headerFill=1F3864

# Column headers (row 1)
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[1]/tc[1]' \
  --prop text="Item" --prop bold=true --prop color=FFFFFF
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[1]/tc[2]' \
  --prop text="Owner" --prop bold=true --prop color=FFFFFF
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[1]/tc[3]' \
  --prop text="Due" --prop bold=true --prop color=FFFFFF
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[1]/tc[4]' \
  --prop text="Status" --prop bold=true --prop color=FFFFFF

# Section header row: spans all 4 columns (gridSpan=4 on tc[1])
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[2]/tc[1]' \
  --prop text="◆ Phase 1 — Discovery" --prop bold=true --prop fill=FFE699 \
  --prop gridSpan=4

# Phase 1 body rows
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[3]/tc[1]' \
  --prop text="Stakeholder interviews"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[3]/tc[2]' --prop text="Alice"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[3]/tc[3]' --prop text="Mar 15"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[3]/tc[4]' \
  --prop text="✓ Done" --prop color=00B050
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[4]/tc[1]' --prop text="Market research"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[4]/tc[2]' --prop text="Bob"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[4]/tc[3]' --prop text="Mar 30"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[4]/tc[4]' \
  --prop text="✓ Done" --prop color=00B050

# Phase 2 section header
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[5]/tc[1]' \
  --prop text="◆ Phase 2 — Design" --prop bold=true --prop fill=C6E0B4 \
  --prop gridSpan=4

officecli set tables-merged.pptx '/slide[2]/table[1]/tr[6]/tc[1]' --prop text="Architecture spec"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[6]/tc[2]' --prop text="Carol"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[6]/tc[3]' --prop text="Apr 20"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[6]/tc[4]' \
  --prop text="◐ WIP" --prop color=FFC000

# Phase 3 section header
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[7]/tc[1]' \
  --prop text="◆ Phase 3 — Build" --prop bold=true --prop fill=F4B084 \
  --prop gridSpan=4

officecli set tables-merged.pptx '/slide[2]/table[1]/tr[8]/tc[1]' --prop text="Backend services"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[8]/tc[2]' --prop text="Dave"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[8]/tc[3]' --prop text="Jun 15"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[8]/tc[4]' --prop text="◯ Not started"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[9]/tc[1]' --prop text="Frontend UI"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[9]/tc[2]' --prop text="Eve"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[9]/tc[3]' --prop text="Jul 01"
officecli set tables-merged.pptx '/slide[2]/table[1]/tr[9]/tc[4]' --prop text="◯ Not started"

officecli close tables-merged.pptx
officecli validate tables-merged.pptx
```

**Features:** `gridSpan=N` (merge the anchor cell across N columns — sets the OOXML `gridSpan` attribute; continuation cells must be skipped), `fill` (per-cell fill for visual section grouping), `color` (text color for status indicators), `bold` + `align=center` on merged headers

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **gridSpan=N:** horizontal cell merge (anchor spans N columns) | 1, 2 |
| **Two-level header:** super-header spans multiple column groups | 1 |
| **Full-width section row:** single cell spans all columns | 2 |
| **Continuation cell skip:** after gridSpan, skip consumed tc[N] indices | 1, 2 |
| **Per-cell fill:** section row color bands | 2 |
| **Per-cell color:** status text colors (green, amber, grey) | 2 |
| **align=center:** center text in merged header cells | 1 |
| **headerFill / bodyFill:** base table coloring | 1 |

## Inspect the Generated File

```bash
# List tables on each slide
officecli query tables-merged.pptx '/slide[1]' table
officecli query tables-merged.pptx '/slide[2]' table

# Inspect the merged header cells on slide 1
officecli get tables-merged.pptx '/slide[1]/table[1]/tr[1]/tc[2]'

# Check the full-width section header on slide 2
officecli get tables-merged.pptx '/slide[2]/table[1]/tr[2]/tc[1]'
officecli get tables-merged.pptx '/slide[2]/table[1]/tr[5]/tc[1]'

# Read a body cell with status color
officecli get tables-merged.pptx '/slide[2]/table[1]/tr[3]/tc[4]'
```
