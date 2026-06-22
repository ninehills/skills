# Combo Charts Showcase

This demo consists of three files that work together:

- **charts-combo.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-combo.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-combo.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-combo.py
# → charts-combo.pptx
```

## Chart Slides

### Slide 1 — combotypes Mixes

```bash
CATS="Q1,Q2,Q3,Q4"
D2="Sales:120,135,148,162;Growth %:5,12,18,22"

# Column bars + line overlay
officecli add charts-combo.pptx /slide[1] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="column + line" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

# Column bars + area fill
officecli add charts-combo.pptx /slide[1] --type chart \
  --prop chartType=combo --prop combotypes="column,area" \
  --prop title="column + area" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

# Line + area
officecli add charts-combo.pptx /slide[1] --type chart \
  --prop chartType=combo --prop combotypes="line,area" \
  --prop title="line + area" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

# Horizontal bar + line (bar direction)
officecli add charts-combo.pptx /slide[1] --type chart \
  --prop chartType=combo --prop combotypes="bar,line" \
  --prop title="bar + line" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType=combo`, `combotypes` (comma-separated chart type per series: column/line/area/bar)

### Slide 2 — combosplit

```bash
D3="Sales:120,135,148,162;Cost:80,90,95,105;Growth %:5,12,18,22"

# combosplit=2: first 2 series use primary type (column), rest use secondary type (line)
officecli add charts-combo.pptx /slide[2] --type chart \
  --prop chartType=combo --prop combotypes="column,column,line" \
  --prop combosplit=2 --prop title="combosplit=2 (2 columns + 1 line)" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D3"

# combosplit=1: first 1 series uses primary type (column), rest use secondary type (line)
officecli add charts-combo.pptx /slide[2] --type chart \
  --prop chartType=combo --prop combotypes="column,line,line" \
  --prop combosplit=1 --prop title="combosplit=1 (1 column + 2 lines)" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D3"

# combosplit=2 with reversed order
officecli add charts-combo.pptx /slide[2] --type chart \
  --prop chartType=combo --prop combotypes="line,line,column" \
  --prop combosplit=2 --prop title="combosplit=2 (2 lines + 1 column)" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D3"

# Three distinct types: area + column + line
officecli add charts-combo.pptx /slide[2] --type chart \
  --prop chartType=combo --prop combotypes="area,column,line" \
  --prop combosplit=1 --prop title="area + column + line" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D3"
```

**Features:** `combosplit` (N = number of series using the first combotype)

### Slide 3 — secondaryaxis

```bash
# Line series on right (secondary) axis
officecli add charts-combo.pptx /slide[3] --type chart \
  --prop chartType=combo --prop combotypes="column,line" --prop secondaryaxis=2 \
  --prop title="secondaryaxis=2" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2"

# Growth % (3rd series) on right axis
officecli add charts-combo.pptx /slide[3] --type chart \
  --prop chartType=combo --prop combotypes="column,column,line" \
  --prop secondaryaxis=3 --prop combosplit=2 \
  --prop title="secondaryaxis=3 (Growth on right)" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D3"

# Two series on secondary axis
officecli add charts-combo.pptx /slide[3] --type chart \
  --prop chartType=combo --prop combotypes="column,line,line" \
  --prop secondaryaxis="2,3" --prop combosplit=1 \
  --prop title="secondaryaxis=2,3" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D3"

# Secondary axis with gridlines and axis font
officecli add charts-combo.pptx /slide[3] --type chart \
  --prop chartType=combo --prop combotypes="column,line" --prop secondaryaxis=2 \
  --prop title="with grid + tick fonts" \
  --prop gridlines="E0E0E0:0.3" --prop axisfont="9:333333:Calibri" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"
```

**Features:** `secondaryaxis` (1-based series index, or comma-separated list for multiple)

### Slide 4 — Title and Legend

```bash
officecli add charts-combo.pptx /slide[4] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="Styled title" --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[4] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="legend=top + legendFont" --prop legend=top \
  --prop legendFont="10:333333:Calibri" \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[4] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="legend.overlay=true" --prop legend=topRight \
  --prop legend.overlay=true \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[4] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop autotitledeleted=true --prop legend=none \
  --prop categories="$CATS" --prop data="$D2"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 5 — Data Labels

```bash
officecli add charts-combo.pptx /slide[5] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="dataLabels=value" --prop dataLabels=value \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[5] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="value,series" --prop dataLabels="value,series" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[5] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="dataLabels=none" --prop dataLabels=none \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[5] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="labelfont styled" --prop dataLabels=value \
  --prop labelfont="10:C00000:Georgia" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"
```

**Note:** `labelPos` is chart-type conditional — combo charts skip it since column and line have different valid positions.

**Features:** `dataLabels` (value/series/none or combined), `labelfont`

### Slide 6 — Axes

```bash
# Axis min/max + titles + number format
officecli add charts-combo.pptx /slide[6] --type chart \
  --prop chartType=combo --prop combotypes="column,line" --prop secondaryaxis=2 \
  --prop title="both axes min/max" --prop axismin=0 --prop axismax=200 \
  --prop axistitle="Sales" --prop cattitle="Quarter" \
  --prop axisfont="10:333333:Calibri" --prop axisnumfmt="#,##0" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[6] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="gridlines + minorGridlines" \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[6] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="labelrotation=-30" --prop labelrotation=-30 \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

# chart-axis Set: mutate axis after creation
officecli add charts-combo.pptx /slide[6] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="chart-axis Set after add" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"
officecli set charts-combo.pptx "/slide[6]/chart[4]/axis[@role=value]" \
  --prop title="Sales (USD)" --prop format='$#,##0' \
  --prop majorGridlines=true --prop min=0 --prop max=200
```

**Features:** `axismin/max`, `axistitle/cattitle`, `axisfont`, `axisnumfmt`, `gridlines/minorGridlines`, `labelrotation`, `chart-axis Set`

### Slide 7 — Series Styling

```bash
officecli add charts-combo.pptx /slide[7] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="colors + seriesoutline" --prop legend=bottom \
  --prop colors="4472C4,ED7D31" --prop seriesoutline="000000:0.5" \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[7] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="gradient + seriesshadow" --prop legend=bottom \
  --prop gradient="FF6600-FFCC00" --prop seriesshadow="000000-5-45-3-50" \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[7] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="transparency=30" --prop legend=bottom \
  --prop transparency=30 \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-combo.pptx /slide[7] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="per-series gradients" --prop legend=bottom \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" \
  --prop categories="$CATS" --prop data="$D2"
```

**Features:** `colors`, `seriesoutline`, `gradient`, `seriesshadow`, `transparency`, `gradients`

### Slide 8 — Presets and Per-Series Set

```bash
for p in minimal dark corporate; do
  officecli add charts-combo.pptx /slide[8] --type chart \
    --prop chartType=combo --prop combotypes="column,line" \
    --prop preset=$p --prop title="preset=$p" --prop legend=bottom \
    --prop categories="$CATS" --prop data="$D2"
done

officecli add charts-combo.pptx /slide[8] --type chart \
  --prop chartType=combo --prop combotypes="column,line" \
  --prop title="chart-series Set" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2"

officecli set charts-combo.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed Sales" --prop color=C00000
officecli set charts-combo.pptx "/slide[8]/chart[4]/series[2]" \
  --prop name="Renamed Growth" --prop color=2E75B6 \
  --prop lineWidth=2.5 --prop marker=circle --prop markerSize=8
```

**Features:** `preset`, `chart-series Set`: `name`, `color`, `lineWidth`, `marker`, `markerSize`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **combotypes** (column/line/area/bar combinations) | 1 |
| **combosplit** (first N series in primary type) | 2 |
| **secondaryaxis** (1-based index, or comma list) | 3 |
| **title.font/size/color/bold** | 4 |
| **legend** positions, legendFont, legend.overlay | 4 |
| **autotitledeleted** | 4 |
| **dataLabels** (value/series/none), **labelfont** | 5 |
| **axismin/max**, axistitle/cattitle, axisfont, axisnumfmt | 6 |
| **gridlines/minorGridlines**, labelrotation | 6 |
| **chart-axis Set** | 6 |
| **colors**, seriesoutline, gradient, seriesshadow | 7 |
| **transparency**, gradients | 7 |
| **preset** | 8 |
| **chart-series Set:** name/color/lineWidth/marker/markerSize | 8 |

## Inspect the Generated File

```bash
officecli query charts-combo.pptx chart
officecli get charts-combo.pptx "/slide[1]/chart[1]"
officecli get charts-combo.pptx "/slide[3]/chart[1]"
officecli get charts-combo.pptx "/slide[8]/chart[4]/series[2]"
```
