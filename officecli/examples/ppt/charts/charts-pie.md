# Pie Charts Showcase

This demo consists of three files that work together:

- **charts-pie.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-pie.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-pie.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-pie.py
# → charts-pie.pptx
```

## Chart Slides

### Slide 1 — Variants (pie, pie3d, firstSliceAngle, varyColors)

```bash
# Standard pie with auto per-slice colors
officecli add charts-pie.pptx /slide[1] --type chart \
  --prop chartType=pie --prop title="pie" --prop legend=right \
  --prop varyColors=true \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

# 3D pie with view3d perspective
officecli add charts-pie.pptx /slide[1] --type chart \
  --prop chartType=pie3d --prop title="pie3d (view3d=20,20,30)" \
  --prop view3d="20,20,30" --prop legend=right --prop varyColors=true \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

# First slice starts at 90° instead of 0°
officecli add charts-pie.pptx /slide[1] --type chart \
  --prop chartType=pie --prop title="firstSliceAngle=90" \
  --prop firstSliceAngle=90 --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

# Single solid color (varyColors=false)
officecli add charts-pie.pptx /slide[1] --type chart \
  --prop chartType=pie --prop title="varyColors=false" \
  --prop varyColors=false --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType` (pie/pie3d), `varyColors`, `firstSliceAngle` (0–360°), `view3d`

### Slide 2 — Explosion

```bash
# explosion= pushes all slices outward by N% of the radius
for angle in 0 10 20 30; do
  officecli add charts-pie.pptx /slide[2] --type chart \
    --prop chartType=pie --prop title="explosion=$angle" \
    --prop explosion=$angle --prop legend=right \
    --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
done
```

**Features:** `explosion` (0–100, % of pie radius)

### Slide 3 — Title and Legend

```bash
officecli add charts-pie.pptx /slide[3] --type chart \
  --prop chartType=pie --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=right --prop categories="North,South,East,West" \
  --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[3] --type chart \
  --prop chartType=pie --prop title="legend=bottom + legendFont" \
  --prop legend=bottom --prop legendFont="10:333333:Calibri" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[3] --type chart \
  --prop chartType=pie --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[3] --type chart \
  --prop chartType=pie --prop autotitledeleted=true --prop legend=none \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `title.font`, `title.size`, `title.color`, `title.bold`, `legend` (right/bottom/topRight/none), `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 4 — Data Labels

```bash
officecli add charts-pie.pptx /slide[4] --type chart \
  --prop chartType=pie --prop title="dataLabels=percent" \
  --prop dataLabels=percent --prop legend=right \
  --prop labelfont="10:333333:Calibri" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

# percent + category with leader lines to each slice
officecli add charts-pie.pptx /slide[4] --type chart \
  --prop chartType=pie --prop title="percent,category + leaderlines" \
  --prop dataLabels="percent,category" --prop leaderlines=true \
  --prop legend=none --prop labelfont="10:333333:Calibri" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[4] --type chart \
  --prop chartType=pie --prop title="all flags (value,percent,category)" \
  --prop dataLabels="value,percent,category" --prop leaderlines=true \
  --prop legend=none \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[4] --type chart \
  --prop chartType=pie --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `dataLabels` (percent/category/value/none or combined), `leaderlines`, `labelfont`

### Slide 5 — Series Styling

```bash
officecli add charts-pie.pptx /slide[5] --type chart \
  --prop chartType=pie --prop title="colors= explicit palette" --prop legend=right \
  --prop colors="4472C4,ED7D31,A5A5A5,70AD47" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[5] --type chart \
  --prop chartType=pie --prop title="gradient + seriesshadow" --prop legend=right \
  --prop gradient="FF6600-FFCC00" --prop seriesshadow="000000-5-45-3-50" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[5] --type chart \
  --prop chartType=pie --prop title="seriesoutline white" --prop legend=right \
  --prop seriesoutline="FFFFFF:2" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[5] --type chart \
  --prop chartType=pie --prop title="transparency=30" --prop legend=right \
  --prop transparency=30 \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `colors`, `gradient`, `seriesshadow`, `seriesoutline`, `transparency`

### Slide 6 — First Slice Angle Variations

```bash
for ang in 0 90 180 270; do
  officecli add charts-pie.pptx /slide[6] --type chart \
    --prop chartType=pie --prop title="firstSliceAngle=$ang" \
    --prop firstSliceAngle=$ang --prop legend=right --prop varyColors=true \
    --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
done
```

**Features:** `firstSliceAngle` (0/90/180/270 degrees — full range survey)

### Slide 7 — Backgrounds

```bash
officecli add charts-pie.pptx /slide[7] --type chart \
  --prop chartType=pie --prop title="chartareafill + chartborder" --prop legend=right \
  --prop chartareafill=FFF8E7 --prop chartborder="000000:1" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[7] --type chart \
  --prop chartType=pie --prop title="roundedcorners=true" --prop legend=right \
  --prop roundedcorners=true --prop chartborder="4472C4:2" \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[7] --type chart \
  --prop chartType=pie --prop title="plotFill=none" --prop legend=right \
  --prop plotFill=none \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

officecli add charts-pie.pptx /slide[7] --type chart \
  --prop chartType=pie --prop title="chartareafill=none" --prop legend=right \
  --prop chartareafill=none \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `chartareafill` (hex or none), `plotFill` (hex or none), `chartborder`, `roundedcorners`

### Slide 8 — Presets and Per-Series Set

```bash
for p in minimal dark corporate; do
  officecli add charts-pie.pptx /slide[8] --type chart \
    --prop chartType=pie --prop preset=$p --prop title="preset=$p" \
    --prop legend=right \
    --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
done

officecli add charts-pie.pptx /slide[8] --type chart \
  --prop chartType=pie --prop title="chart-series Set name+color" --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"

# Post-Add mutation of series name and color
officecli set charts-pie.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed Share" --prop color=C00000
```

**Features:** `preset` (minimal/dark/corporate), `chart-series Set name=/color=`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Chart types:** pie, pie3d | 1 |
| **varyColors** | 1 |
| **firstSliceAngle** (0–360) | 1, 6 |
| **view3d** (pie3d) | 1 |
| **explosion** (0–100%) | 2 |
| **Title styling:** title.font/size/color/bold | 3 |
| **Legend:** right/bottom/topRight/none, legendFont, legend.overlay | 3 |
| **autotitledeleted** | 3 |
| **dataLabels:** percent/category/value/none + combined | 4 |
| **leaderlines** | 4 |
| **labelfont** | 4 |
| **colors** (palette) | 5 |
| **gradient, seriesshadow, seriesoutline, transparency** | 5 |
| **chartareafill**, **plotFill**, **chartborder**, **roundedcorners** | 7 |
| **preset** (minimal/dark/corporate) | 8 |
| **chart-series Set** | 8 |

## Inspect the Generated File

```bash
officecli query charts-pie.pptx chart
officecli get charts-pie.pptx "/slide[1]/chart[1]"
officecli get charts-pie.pptx "/slide[2]/chart[2]"
officecli get charts-pie.pptx "/slide[8]/chart[4]/series[1]"
```
