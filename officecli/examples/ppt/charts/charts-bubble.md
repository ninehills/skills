# Bubble Charts Showcase

This demo consists of three files that work together:

- **charts-bubble.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-bubble.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-bubble.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-bubble.py
# → charts-bubble.pptx
```

## Chart Slides

### Slide 1 — bubbleScale Variants

```bash
# bubbleScale controls relative size of all bubbles (% of default)
for s in 50 100 150 200; do
  officecli add charts-bubble.pptx /slide[1] --type chart \
    --prop chartType=bubble --prop title="bubbleScale=$s" \
    --prop bubbleScale=$s --prop legend=none \
    --prop data="A:5,12,8,18,22,9,15,11"
done
```

**Features:** `chartType=bubble`, `bubbleScale` (50–200, % of default)

### Slide 2 — sizerepresents (area vs width)

```bash
officecli add charts-bubble.pptx /slide[2] --type chart \
  --prop chartType=bubble --prop title="sizerepresents=area" \
  --prop sizerepresents=area --prop legend=none \
  --prop data="A:5,12,8,18,22,9,15,11"

officecli add charts-bubble.pptx /slide[2] --type chart \
  --prop chartType=bubble --prop title="sizerepresents=width" \
  --prop sizerepresents=width --prop legend=none \
  --prop data="A:5,12,8,18,22,9,15,11"

# Two series with area
officecli add charts-bubble.pptx /slide[2] --type chart \
  --prop chartType=bubble --prop title="area + 2 series" \
  --prop sizerepresents=area --prop legend=bottom \
  --prop data="A:5,12,8,18,22,9;B:7,11,15,9,20,14"

officecli add charts-bubble.pptx /slide[2] --type chart \
  --prop chartType=bubble --prop title="width + 2 series" \
  --prop sizerepresents=width --prop legend=bottom \
  --prop data="A:5,12,8,18,22,9;B:7,11,15,9,20,14"
```

**Features:** `sizerepresents` (area/width) — controls whether the data value maps to bubble area or diameter

### Slide 3 — shownegbubbles

```bash
# With negative values: shownegbubbles controls visibility
officecli add charts-bubble.pptx /slide[3] --type chart \
  --prop chartType=bubble --prop title="shownegbubbles=false" \
  --prop shownegbubbles=false --prop legend=none \
  --prop data="A:5,-8,12,-15,18,22"

officecli add charts-bubble.pptx /slide[3] --type chart \
  --prop chartType=bubble --prop title="shownegbubbles=true" \
  --prop shownegbubbles=true --prop legend=none \
  --prop data="A:5,-8,12,-15,18,22"
```

**Features:** `shownegbubbles` (true/false) — when false, negative-size bubbles are hidden; when true, they render with inverted color

### Slide 4 — Title and Legend

```bash
officecli add charts-bubble.pptx /slide[4] --type chart \
  --prop chartType=bubble --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop data="A:5,12,8,18;B:7,11,15,9"

officecli add charts-bubble.pptx /slide[4] --type chart \
  --prop chartType=bubble --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop data="A:5,12,8,18;B:7,11,15,9"

officecli add charts-bubble.pptx /slide[4] --type chart \
  --prop chartType=bubble --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop data="A:5,12,8,18;B:7,11,15,9"

officecli add charts-bubble.pptx /slide[4] --type chart \
  --prop chartType=bubble --prop autotitledeleted=true --prop legend=none \
  --prop data="A:5,12,8,18;B:7,11,15,9"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 5 — Data Labels

```bash
officecli add charts-bubble.pptx /slide[5] --type chart \
  --prop chartType=bubble --prop title="value" --prop dataLabels=value \
  --prop labelfont="9:333333:Calibri" --prop legend=none \
  --prop data="A:5,12,8,18,22,9,15,11"

officecli add charts-bubble.pptx /slide[5] --type chart \
  --prop chartType=bubble --prop title="value,series" \
  --prop dataLabels="value,series" --prop legend=none \
  --prop data="A:5,12,8,18;B:7,11,15,9"

officecli add charts-bubble.pptx /slide[5] --type chart \
  --prop chartType=bubble --prop title="labelPos=top" \
  --prop dataLabels=value --prop labelPos=top --prop legend=none \
  --prop data="A:5,12,8,18,22,9,15,11"

officecli add charts-bubble.pptx /slide[5] --type chart \
  --prop chartType=bubble --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=none \
  --prop data="A:5,12,8,18,22,9,15,11"
```

**Features:** `dataLabels` (value/series/none or combined), `labelPos`, `labelfont`

### Slide 6 — Axes

```bash
officecli add charts-bubble.pptx /slide[6] --type chart \
  --prop chartType=bubble --prop title="min/max + titles" \
  --prop axismin=0 --prop axismax=30 --prop majorunit=10 \
  --prop axistitle="Y" --prop cattitle="X" \
  --prop axisfont="10:333333:Calibri" --prop axisline="666666:1" \
  --prop legend=none --prop data="A:5,12,8,18,22,9,15,11"

officecli add charts-bubble.pptx /slide[6] --type chart \
  --prop chartType=bubble --prop title="gridlines + minorGridlines" \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop legend=none --prop data="A:5,12,8,18,22,9,15,11"

officecli add charts-bubble.pptx /slide[6] --type chart \
  --prop chartType=bubble --prop title="labelrotation=-30" \
  --prop labelrotation=-30 --prop legend=none \
  --prop data="A:5,12,8,18,22,9,15,11"

officecli add charts-bubble.pptx /slide[6] --type chart \
  --prop chartType=bubble --prop title="dispunits=hundreds" \
  --prop dispunits=hundreds --prop legend=none \
  --prop data="A:500,1200,800,1800,2200,900"
```

**Features:** `axismin/max`, `majorunit`, `axistitle/cattitle`, `axisfont/axisline`, `gridlines/minorGridlines`, `labelrotation`, `dispunits`

### Slide 7 — Series Styling

```bash
officecli add charts-bubble.pptx /slide[7] --type chart \
  --prop chartType=bubble --prop title="colors + seriesoutline" \
  --prop colors="4472C4,ED7D31" --prop seriesoutline="000000:0.5" \
  --prop legend=bottom --prop data="A:5,12,8,18;B:7,11,15,9"

officecli add charts-bubble.pptx /slide[7] --type chart \
  --prop chartType=bubble --prop title="gradient + seriesshadow" \
  --prop gradient="FF6600-FFCC00" --prop seriesshadow="000000-5-45-3-50" \
  --prop legend=none --prop data="A:5,12,8,18,22,9,15,11"

officecli add charts-bubble.pptx /slide[7] --type chart \
  --prop chartType=bubble --prop title="transparency=30" \
  --prop transparency=30 --prop legend=bottom \
  --prop data="A:5,12,8,18;B:7,11,15,9"

officecli add charts-bubble.pptx /slide[7] --type chart \
  --prop chartType=bubble --prop title="per-series gradients" \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" --prop legend=bottom \
  --prop data="A:5,12,8,18;B:7,11,15,9"
```

**Features:** `colors`, `seriesoutline`, `gradient`, `seriesshadow`, `transparency`, `gradients`

### Slide 8 — Presets and Per-Series Set

```bash
for p in minimal dark corporate; do
  officecli add charts-bubble.pptx /slide[8] --type chart \
    --prop chartType=bubble --prop preset=$p --prop title="preset=$p" \
    --prop legend=bottom --prop data="A:5,12,8,18;B:7,11,15,9"
done

officecli add charts-bubble.pptx /slide[8] --type chart \
  --prop chartType=bubble --prop title="chart-series Set name+color" \
  --prop legend=bottom --prop data="A:5,12,8,18;B:7,11,15,9"

officecli set charts-bubble.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed A" --prop color=C00000
officecli set charts-bubble.pptx "/slide[8]/chart[4]/series[2]" \
  --prop name="Renamed B" --prop color=2E75B6
```

**Features:** `preset` (minimal/dark/corporate), `chart-series Set`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **bubbleScale** (50–200) | 1 |
| **sizerepresents** (area/width) | 2 |
| **shownegbubbles** | 3 |
| **title.font/size/color/bold** | 4 |
| **legend** positions, legendFont, legend.overlay | 4 |
| **autotitledeleted** | 4 |
| **dataLabels:** value/series/none | 5 |
| **labelPos, labelfont** | 5 |
| **axismin/max**, majorunit, axistitle/cattitle | 6 |
| **axisfont, axisline, gridlines** | 6 |
| **labelrotation, dispunits** | 6 |
| **colors, seriesoutline, gradient, seriesshadow** | 7 |
| **transparency, gradients** | 7 |
| **preset** | 8 |
| **chart-series Set** | 8 |

## Inspect the Generated File

```bash
officecli query charts-bubble.pptx chart
officecli get charts-bubble.pptx "/slide[1]/chart[1]"
officecli get charts-bubble.pptx "/slide[3]/chart[1]"
officecli get charts-bubble.pptx "/slide[8]/chart[4]/series[1]"
```
