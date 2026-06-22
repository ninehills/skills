# Real-World PPT Tables — Financial Review Deck

This demo consists of three files that work together:

- **tables-financial.sh** — Shell script that calls `officecli` commands to generate the deck.
- **tables-financial.pptx** — The generated 4-slide deck (title, Quarterly P&L with section spans, Risk Register with traffic-light fills, KPI summary).
- **tables-financial.md** — This file. Shows how to combine style presets, gridSpan section headers, per-cell fill, alignment, and totals rows in a real presentation.

## Regenerate

```bash
cd examples/ppt
bash tables/tables-financial.sh
# → tables/tables-financial.pptx
```

## Slides

### Slide 1 — Title

A plain title slide with no table — just two text shapes.

```bash
officecli create tables-financial.pptx
officecli open tables-financial.pptx

NAVY=1F3864; STEEL=2E75B6; PALE=DEEAF6
GREEN=00B050; AMBER=FFC000; RED=C00000

officecli add tables-financial.pptx /presentation/slides --type slide

officecli add tables-financial.pptx '/slide[1]' --type shape \
  --prop text="Q4 2025 Financial Review" \
  --prop size=44 --prop bold=true --prop color="$NAVY" \
  --prop x=1in --prop y=2.5in --prop width=11in --prop height=1.2in \
  --prop align=center

officecli add tables-financial.pptx '/slide[1]' --type shape \
  --prop text="Revenue · Expenses · Margin · Forecast" \
  --prop size=22 --prop color=595959 \
  --prop x=1in --prop y=4in --prop width=11in --prop height=0.8in \
  --prop align=center
```

---

### Slide 2 — Quarterly P&L (gridSpan Section Headers)

An 11-row × 6-column P&L with two section header rows (REVENUE, EXPENSES) each spanning all 6 columns via `gridSpan=6`, a subtotal row with highlighted fill, and a NET INCOME row with green fill.

```bash
officecli add tables-financial.pptx /presentation/slides --type slide

officecli add tables-financial.pptx '/slide[2]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=5.5in \
  --prop rows=11 --prop cols=6

# Column header row
for entry in "1:Line Item" "2:Q1" "3:Q2" "4:Q3" "5:Q4" "6:FY Total"; do
  c="${entry%%:*}"; t="${entry#*:}"
  officecli set tables-financial.pptx "/slide[2]/table[1]/tr[1]/tc[$c]" \
    --prop text="$t" --prop bold=true --prop color=FFFFFF \
    --prop fill="$NAVY" --prop align=center
done

# REVENUE section header — spans all 6 columns
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[2]/tc[1]' \
  --prop text="REVENUE" --prop bold=true \
  --prop fill="$STEEL" --prop color=FFFFFF \
  --prop gridSpan=6

# Revenue body rows (right-aligned numbers, bold FY Total)
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[3]/tc[1]' \
  --prop text="  Product Sales"
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[3]/tc[2]' \
  --prop text="1,200" --prop align=right
# … fill tc[3]..tc[5] similarly
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[3]/tc[6]' \
  --prop text="5,750" --prop align=right --prop bold=true

# Subtotal row with pale highlight fill
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[6]/tc[1]' \
  --prop text="  Subtotal" --prop fill="$PALE"
for c in 2 3 4 5 6; do
  officecli set tables-financial.pptx "/slide[2]/table[1]/tr[6]/tc[$c]" \
    --prop align=right --prop fill="$PALE"
done
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[6]/tc[6]' \
  --prop text="8,600" --prop align=right --prop bold=true --prop fill="$PALE"

# EXPENSES section header — same pattern as REVENUE
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[7]/tc[1]' \
  --prop text="EXPENSES" --prop bold=true \
  --prop fill="$STEEL" --prop color=FFFFFF \
  --prop gridSpan=6

# NET INCOME row — all cells green
officecli set tables-financial.pptx '/slide[2]/table[1]/tr[11]/tc[1]' \
  --prop text="NET INCOME" --prop bold=true \
  --prop fill="$GREEN" --prop color=FFFFFF
for c in 2 3 4 5 6; do
  officecli set tables-financial.pptx "/slide[2]/table[1]/tr[11]/tc[$c]" \
    --prop align=right --prop bold=true \
    --prop fill="$GREEN" --prop color=FFFFFF
done
```

**Features:** `gridSpan=6` (full-width section header rows), `fill` (per-cell — STEEL for section, PALE for subtotal, GREEN for net income), `color=FFFFFF` (white text on dark fill), `align=right` (number alignment), `bold=true` on totals

---

### Slide 3 — Risk Register (Traffic-Light Fills)

A 6-row risk register using `style=medium2` + `bandedRows=true`, with the Status column cells overridden with traffic-light fills (green/amber/red).

```bash
officecli add tables-financial.pptx /presentation/slides --type slide

officecli add tables-financial.pptx '/slide[3]' --type table \
  --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=4in \
  --prop style=medium2 --prop firstRow=true --prop bandedRows=true \
  --prop data="Risk,Impact,Likelihood,Owner,Status;FX volatility,High,Medium,CFO,At risk;Supply chain,Medium,Low,COO,On track;Talent attrition,High,High,CPO,Critical;Reg compliance,Medium,Medium,GC,On track;Cybersecurity,High,Low,CTO,On track"

# Override Status column (col 5) per-cell with traffic-light fills
officecli set tables-financial.pptx '/slide[3]/table[1]/tr[2]/tc[5]' \
  --prop text="At risk" --prop fill="$AMBER" --prop bold=true --prop align=center
officecli set tables-financial.pptx '/slide[3]/table[1]/tr[3]/tc[5]' \
  --prop text="On track" --prop fill="$GREEN" --prop color=FFFFFF --prop bold=true --prop align=center
officecli set tables-financial.pptx '/slide[3]/table[1]/tr[4]/tc[5]' \
  --prop text="Critical" --prop fill="$RED" --prop color=FFFFFF --prop bold=true --prop align=center
officecli set tables-financial.pptx '/slide[3]/table[1]/tr[5]/tc[5]' \
  --prop text="On track" --prop fill="$GREEN" --prop color=FFFFFF --prop bold=true --prop align=center
officecli set tables-financial.pptx '/slide[3]/table[1]/tr[6]/tc[5]' \
  --prop text="On track" --prop fill="$GREEN" --prop color=FFFFFF --prop bold=true --prop align=center
```

**Features:** `data=` (CSV inline population), `style=medium2 + bandedRows`, per-cell `fill` override (traffic-light: GREEN=00B050, AMBER=FFC000, RED=C00000), `color=FFFFFF` on dark cells, `align=center` on status cells

---

### Slide 4 — KPI Summary

A 5-row × 4-column KPI table using `style=medium4` with `firstRow + firstCol + lastRow` flags for maximum header/totals emphasis.

```bash
officecli add tables-financial.pptx /presentation/slides --type slide

officecli add tables-financial.pptx '/slide[4]' --type table \
  --prop x=2in --prop y=1.5in --prop width=9in --prop height=3.5in \
  --prop style=medium4 \
  --prop firstRow=true --prop firstCol=true --prop lastRow=true \
  --prop data="Metric,Target,Actual,Variance;Revenue (\$M),8.0,8.6,+7.5%;Gross Margin,38%,40.1%,+2.1pp;Op Margin,18%,19.8%,+1.8pp;CAC Payback,14 mo,12 mo,-2 mo;Total,—,—,Beat"

officecli close tables-financial.pptx
officecli validate tables-financial.pptx
```

**Features:** `style=medium4`, `firstRow=true` (header highlight), `firstCol=true` (row label highlight), `lastRow=true` (total row highlight), `data=` CSV with all content inline

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Title shape:** large text, align=center, navy/grey colors | 1 |
| **gridSpan=6:** full-width section header (REVENUE, EXPENSES) | 2 |
| **Per-cell fill + color:** section (STEEL), subtotal (PALE), net (GREEN) | 2 |
| **align=right:** right-align numeric columns | 2 |
| **Bold totals:** FY Total column and NET INCOME row | 2 |
| **style=medium2 + bandedRows:** themed risk register | 3 |
| **Traffic-light fills:** GREEN / AMBER / RED per-cell status | 3 |
| **color=FFFFFF:** white text on dark fill | 3 |
| **data= inline:** full table in one command | 3, 4 |
| **style=medium4:** KPI table style | 4 |
| **firstRow + firstCol + lastRow:** multi-flag header emphasis | 4 |

## Inspect the Generated File

```bash
# List tables on each slide
officecli query tables-financial.pptx '/slide[2]' table
officecli query tables-financial.pptx '/slide[3]' table

# Inspect the REVENUE section header (gridSpan)
officecli get tables-financial.pptx '/slide[2]/table[1]/tr[2]/tc[1]'

# Check subtotal row fill
officecli get tables-financial.pptx '/slide[2]/table[1]/tr[6]/tc[1]'

# Inspect traffic-light cells on slide 3
officecli get tables-financial.pptx '/slide[3]/table[1]/tr[2]/tc[5]'
officecli get tables-financial.pptx '/slide[3]/table[1]/tr[4]/tc[5]'

# Get KPI table properties on slide 4
officecli get tables-financial.pptx '/slide[4]/table[1]'
```
