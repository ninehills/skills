# Doughnut Charts Showcase

This demo consists of three files that work together:

- **charts-doughnut.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-doughnut.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-doughnut.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-doughnut.py
# → charts-doughnut.pptx
```

## Chart Slides

### Slide 1 — holeSize Variants

```bash
# holeSize controls the size of the center hole (10–90, % of radius)
for h in 10 30 55 75; do
  officecli add charts-doughnut.pptx /slide[1] --type chart \
    --prop chartType=doughnut --prop title="holeSize=$h" \
    --prop holeSize=$h --prop legend=right --prop varyColors=true \
    --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
done
```

**Features:** `chartType=doughnut`, `holeSize` (10–90), `varyColors`

### Slide 2 — Multi-Ring (Concentric Series)

```bash
# Single ring
officecli add charts-doughnut.pptx /slide[2] --type chart \
  --prop chartType=doughnut --prop title="single ring" \
  --prop holeSize=50 --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

# Two concentric rings
officecli add charts-doughnut.pptx /slide[2] --type chart \
  --prop chartType=doughnut --prop title="two rings" \
  --prop holeSize=40 --prop legend=right \
  --prop categories="North,South,East,West" \
  --prop data="Last:25,30,25,20;This:30,25,28,17"

# Three concentric rings
officecli add charts-doughnut.pptx /slide[2] --type chart \
  --prop chartType=doughnut --prop title="three rings" \
  --prop holeSize=30 --prop legend=right \
  --prop categories="North,South,East,West" \
  --prop data="Region1:30,25,28,17;Region2:25,30,20,25;Region3:20,25,30,25"

# Two rings + percent labels
officecli add charts-doughnut.pptx /slide[2] --type chart \
  --prop chartType=doughnut --prop title="two rings + dataLabels=percent" \
  --prop holeSize=40 --prop dataLabels=percent --prop legend=right \
  --prop categories="North,South,East,West" \
  --prop data="Last:25,30,25,20;This:30,25,28,17"
```

**Features:** Multi-series doughnut = concentric rings (outer = first series), `holeSize` interacts with ring width

### Slide 3 — First Slice Angle

```bash
for ang in 0 90 180 270; do
  officecli add charts-doughnut.pptx /slide[3] --type chart \
    --prop chartType=doughnut --prop title="firstSliceAngle=$ang" \
    --prop firstSliceAngle=$ang --prop holeSize=50 --prop legend=right \
    --prop varyColors=true \
    --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
done
```

**Features:** `firstSliceAngle` (0–360°)

### Slide 4 — Data Labels

```bash
officecli add charts-doughnut.pptx /slide[4] --type chart \
  --prop chartType=doughnut --prop title="dataLabels=percent" \
  --prop dataLabels=percent --prop holeSize=50 --prop legend=right \
  --prop labelfont="10:333333:Calibri" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[4] --type chart \
  --prop chartType=doughnut --prop title="percent,category + leaderlines" \
  --prop dataLabels="percent,category" --prop holeSize=50 \
  --prop leaderlines=true --prop legend=none \
  --prop labelfont="10:333333:Calibri" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[4] --type chart \
  --prop chartType=doughnut --prop title="all flags (value,percent,category)" \
  --prop dataLabels="value,percent,category" --prop holeSize=50 \
  --prop leaderlines=true --prop legend=none \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[4] --type chart \
  --prop chartType=doughnut --prop title="dataLabels=none" \
  --prop dataLabels=none --prop holeSize=50 --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `dataLabels` (percent/category/value/none or combined), `leaderlines`, `labelfont`

### Slide 5 — Series Styling

```bash
officecli add charts-doughnut.pptx /slide[5] --type chart \
  --prop chartType=doughnut --prop title="colors=" --prop holeSize=50 --prop legend=right \
  --prop colors="4472C4,ED7D31,A5A5A5,70AD47" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[5] --type chart \
  --prop chartType=doughnut --prop title="gradient + seriesshadow" \
  --prop holeSize=50 --prop gradient="FF6600-FFCC00" \
  --prop seriesshadow="000000-5-45-3-50" --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[5] --type chart \
  --prop chartType=doughnut --prop title="seriesoutline white" \
  --prop holeSize=50 --prop seriesoutline="FFFFFF:2" --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[5] --type chart \
  --prop chartType=doughnut --prop title="transparency=30" \
  --prop holeSize=50 --prop transparency=30 --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `colors`, `gradient`, `seriesshadow`, `seriesoutline`, `transparency`

### Slide 6 — Title and Legend

```bash
officecli add charts-doughnut.pptx /slide[6] --type chart \
  --prop chartType=doughnut --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop holeSize=50 --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[6] --type chart \
  --prop chartType=doughnut --prop title="legend=bottom + legendFont" \
  --prop holeSize=50 --prop legend=bottom --prop legendFont="10:333333:Calibri" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[6] --type chart \
  --prop chartType=doughnut --prop title="legend.overlay=true" \
  --prop holeSize=50 --prop legend=topRight --prop legend.overlay=true \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[6] --type chart \
  --prop chartType=doughnut --prop autotitledeleted=true \
  --prop holeSize=50 --prop legend=none \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 7 — Backgrounds

```bash
officecli add charts-doughnut.pptx /slide[7] --type chart \
  --prop chartType=doughnut --prop title="chartareafill + chartborder" \
  --prop holeSize=50 --prop chartareafill=FFF8E7 --prop chartborder="000000:1" \
  --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[7] --type chart \
  --prop chartType=doughnut --prop title="roundedcorners=true" \
  --prop holeSize=50 --prop roundedcorners=true --prop chartborder="4472C4:2" \
  --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[7] --type chart \
  --prop chartType=doughnut --prop title="plotFill=none" \
  --prop holeSize=50 --prop plotFill=none --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-doughnut.pptx /slide[7] --type chart \
  --prop chartType=doughnut --prop title="chartareafill=none" \
  --prop holeSize=50 --prop chartareafill=none --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `chartareafill`, `plotFill`, `chartborder`, `roundedcorners`

### Slide 8 — Presets and Per-Series Set

```bash
for p in minimal dark corporate; do
  officecli add charts-doughnut.pptx /slide[8] --type chart \
    --prop chartType=doughnut --prop preset=$p --prop title="preset=$p" \
    --prop holeSize=50 --prop legend=right \
    --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
done

officecli add charts-doughnut.pptx /slide[8] --type chart \
  --prop chartType=doughnut --prop title="chart-series Set name+color" \
  --prop holeSize=50 --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli set charts-doughnut.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed Share" --prop color=C00000
```

**Features:** `preset` (minimal/dark/corporate), `chart-series Set`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **holeSize** (10–90%) | 1 |
| **varyColors** | 1 |
| **Multi-ring** (concentric series) | 2 |
| **firstSliceAngle** (0–360) | 3 |
| **dataLabels:** percent/category/value/none + combined | 4 |
| **leaderlines**, **labelfont** | 4 |
| **colors, gradient, seriesshadow, seriesoutline, transparency** | 5 |
| **title.font/size/color/bold** | 6 |
| **legend** positions, **legendFont**, **legend.overlay** | 6 |
| **autotitledeleted** | 6 |
| **chartareafill, plotFill, chartborder, roundedcorners** | 7 |
| **preset** (minimal/dark/corporate) | 8 |
| **chart-series Set** | 8 |

## Inspect the Generated File

```bash
officecli query charts-doughnut.pptx chart
officecli get charts-doughnut.pptx "/slide[1]/chart[1]"
officecli get charts-doughnut.pptx "/slide[2]/chart[3]"
officecli get charts-doughnut.pptx "/slide[8]/chart[4]/series[1]"
```
