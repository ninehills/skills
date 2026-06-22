# Scatter Charts Showcase

This demo consists of three files that work together:

- **charts-scatter.py** — Python script that calls `officecli` commands to generate the deck (9 slides).
- **charts-scatter.pptx** — The generated 9-slide deck.
- **charts-scatter.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-scatter.py
# → charts-scatter.pptx
```

## Chart Slides

### Slide 1 — scatterstyle Variants

Five scatter style modes demonstrated with the same dataset.

```bash
officecli add charts-scatter.pptx /slide[1] --type chart \
  --prop chartType=scatter --prop scatterstyle=line --prop title="scatterstyle=line" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-scatter.pptx /slide[1] --type chart \
  --prop chartType=scatter --prop scatterstyle=lineMarker \
  --prop title="scatterstyle=lineMarker" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-scatter.pptx /slide[1] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="scatterstyle=marker" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

officecli add charts-scatter.pptx /slide[1] --type chart \
  --prop chartType=scatter --prop scatterstyle=smoothMarker \
  --prop title="scatterstyle=smoothMarker" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `scatterstyle` (line/lineMarker/marker/smooth/smoothMarker)

### Slide 2 — Markers

```bash
# circle with explicit size and color
officecli add charts-scatter.pptx /slide[2] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="circle:10:FF0000" --prop marker="circle:10:FF0000" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65"

officecli add charts-scatter.pptx /slide[2] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="diamond:12:0070C0" --prop marker="diamond:12:0070C0" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65"

officecli add charts-scatter.pptx /slide[2] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="square:8:70AD47" --prop marker="square:8:70AD47" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65"

# markercolor — fill color independent of marker= compound form
officecli add charts-scatter.pptx /slide[2] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="markercolor=E63946" --prop marker="circle:10" \
  --prop markercolor=E63946 \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55,52,65"
```

**Features:** `marker` (symbol:size:color compound), symbols: circle/diamond/square/triangle/star/…; `markercolor` (standalone fill color)

### Slide 3 — Title and Legend

```bash
officecli add charts-scatter.pptx /slide[3] --type chart \
  --prop chartType=scatter --prop scatterstyle=smoothMarker \
  --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop data="A:10,20,18,30;B:5,12,15,22"

officecli add charts-scatter.pptx /slide[3] --type chart \
  --prop chartType=scatter --prop scatterstyle=lineMarker \
  --prop title="legend=top + legendFont" --prop legend=top \
  --prop legendFont="10:333333:Calibri" \
  --prop data="A:10,20,18,30;B:5,12,15,22"

# title.overlay — title drawn over plot area (saves vertical space)
officecli add charts-scatter.pptx /slide[3] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="title.overlay=true" --prop title.overlay=true \
  --prop legend=none --prop data="A:10,20,18,30;B:5,12,15,22"
```

**Features:** `title.font/size/color/bold`, `title.overlay`, `legend` positions, `legendFont`, `legend.overlay`

### Slide 4 — Data Labels

```bash
officecli add charts-scatter.pptx /slide[4] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="value" --prop dataLabels=value \
  --prop labelfont="9:333333:Calibri" --prop legend=none \
  --prop data="A:10,20,18,30,28,40,42,55"

officecli add charts-scatter.pptx /slide[4] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="value,series" --prop dataLabels="value,series" \
  --prop legend=none --prop data="A:10,20,18,30;B:5,12,15,22"

officecli add charts-scatter.pptx /slide[4] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="labelPos=top" --prop dataLabels=value --prop labelPos=top \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55"

officecli add charts-scatter.pptx /slide[4] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="dataLabels=none" --prop dataLabels=none \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55"
```

**Features:** `dataLabels` (value/series/none or combined), `labelPos` (top), `labelfont`

### Slide 5 — Axes

```bash
officecli add charts-scatter.pptx /slide[5] --type chart \
  --prop chartType=scatter --prop scatterstyle=lineMarker \
  --prop title="min/max + titles" --prop legend=none \
  --prop axismin=0 --prop axismax=80 --prop majorunit=20 \
  --prop axistitle="Y" --prop cattitle="X" \
  --prop axisfont="10:333333:Calibri" --prop axisline="666666:1" \
  --prop axisnumfmt="#,##0" --prop data="A:10,20,18,30,28,40,42,55"

officecli add charts-scatter.pptx /slide[5] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="gridlines + minorGridlines" --prop legend=none \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop data="A:10,20,18,30,28,40,42,55"

# Log scale on Y axis
officecli add charts-scatter.pptx /slide[5] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="logbase=10 (Y)" --prop logbase=10 \
  --prop axismin=1 --prop axismax=100 --prop legend=none \
  --prop data="A:2,5,8,12,20,40,80"
```

**Features:** `axismin/max`, `majorunit`, `axistitle/cattitle`, `axisfont/axisline/axisnumfmt`, `gridlines/minorGridlines`, `labelrotation`, `logbase`

### Slide 6 — Series Styling

```bash
officecli add charts-scatter.pptx /slide[6] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="colors + seriesoutline" --prop legend=bottom \
  --prop colors="4472C4,ED7D31" --prop seriesoutline="000000:0.5" \
  --prop data="A:10,20,18,30;B:5,12,15,22"

officecli add charts-scatter.pptx /slide[6] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="gradient + seriesshadow" --prop legend=none \
  --prop gradient="FF6600-FFCC00" --prop seriesshadow="000000-5-45-3-50" \
  --prop data="A:10,20,18,30,28,40,42,55"

officecli add charts-scatter.pptx /slide[6] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="transparency=30" --prop legend=bottom \
  --prop transparency=30 --prop data="A:10,20,18,30;B:5,12,15,22"

officecli add charts-scatter.pptx /slide[6] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="per-series gradients" --prop legend=bottom \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" \
  --prop data="A:10,20,18,30;B:5,12,15,22"
```

**Features:** `colors`, `seriesoutline`, `gradient`, `seriesshadow`, `transparency`, `gradients`

### Slide 7 — Overlays (trendlines, error bars)

```bash
officecli add charts-scatter.pptx /slide[7] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="trendline=linear" --prop trendline=linear \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55"

officecli add charts-scatter.pptx /slide[7] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="trendline=poly:3" --prop trendline="poly:3" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55"

officecli add charts-scatter.pptx /slide[7] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="trendline=movingAvg:3" --prop trendline="movingAvg:3" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55"

officecli add charts-scatter.pptx /slide[7] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="errbars=stdDev:1" --prop errbars="stdDev:1" \
  --prop legend=none --prop data="A:10,20,18,30,28,40,42,55"
```

**Features:** `trendline` (linear/poly:N/exp/log/power/movingAvg:N), `errbars`

### Slide 8 — Per-Series Set and Presets

```bash
for p in minimal dark corporate; do
  officecli add charts-scatter.pptx /slide[8] --type chart \
    --prop chartType=scatter --prop scatterstyle=smoothMarker \
    --prop preset=$p --prop title="preset=$p" --prop legend=bottom \
    --prop data="A:10,20,18,30;B:5,12,15,22"
done

officecli add charts-scatter.pptx /slide[8] --type chart \
  --prop chartType=scatter --prop scatterstyle=lineMarker \
  --prop title="chart-series Set per series" --prop legend=bottom \
  --prop data="A:10,20,18,30;B:5,12,15,22"

# Per-series Set: lineWidth, lineDash, marker, markerSize, smooth
officecli set charts-scatter.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Alpha" --prop color=C00000 --prop lineWidth=2.5 \
  --prop lineDash=solid --prop marker=circle --prop markerSize=10 \
  --prop smooth=true
officecli set charts-scatter.pptx "/slide[8]/chart[4]/series[2]" \
  --prop name="Beta" --prop color=2E75B6 --prop lineWidth=1.5 \
  --prop lineDash=dash --prop marker=diamond --prop markerSize=8
```

**Features:** `preset`, `chart-series Set`: `name`, `color`, `lineWidth`, `lineDash` (solid/dash/dot/…), `marker`, `markerSize`, `smooth`

### Slide 9 — Named Series Shorthand

```bash
# series{N}= is an alternative to data= that names each series at Add time
officecli add charts-scatter.pptx /slide[9] --type chart \
  --prop chartType=scatter --prop scatterstyle=lineMarker \
  --prop title="series1= + series2=" \
  --prop series1="Alpha:10,25,18,40" --prop series2="Beta:5,15,12,30" \
  --prop legend=bottom

officecli add charts-scatter.pptx /slide[9] --type chart \
  --prop chartType=scatter --prop scatterstyle=marker \
  --prop title="series1.* per-series naming + colors=" \
  --prop series1.name="Alpha" --prop series1.values="10,25,18,40" \
  --prop series2.name="Beta" --prop series2.values="5,15,12,30" \
  --prop colors="4472C4,E63946" --prop legend=bottom
```

**Features:** `series{N}=Name:v1,v2,…` (named series shorthand), `series{N}.name`/`series{N}.values` per-series at Add time, mixing with `colors=`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **scatterstyle:** line/lineMarker/marker/smooth/smoothMarker | 1 |
| **marker** (symbol:size:color compound) | 2 |
| **markercolor** (standalone) | 2 |
| **title.font/size/color/bold**, **title.overlay** | 3 |
| **legend** positions, legendFont, legend.overlay | 3 |
| **dataLabels:** value/series/none + combined | 4 |
| **labelPos**, **labelfont** | 4 |
| **axismin/max**, majorunit, axistitle/cattitle | 5 |
| **axisfont/axisline/axisnumfmt**, gridlines | 5 |
| **logbase** | 5 |
| **colors**, seriesoutline, gradient, seriesshadow | 6 |
| **gradients**, transparency | 6 |
| **trendline** (linear/poly/exp/log/power/movingAvg) | 7 |
| **errbars** | 7 |
| **preset** | 8 |
| **chart-series Set:** lineWidth/lineDash/marker/markerSize/smooth | 8 |
| **series{N}=** shorthand | 9 |
| **series{N}.name/values** per-series Add | 9 |

## Inspect the Generated File

```bash
officecli query charts-scatter.pptx chart
officecli get charts-scatter.pptx "/slide[1]/chart[1]"
officecli get charts-scatter.pptx "/slide[8]/chart[4]/series[1]"
officecli get charts-scatter.pptx "/slide[5]/chart[1]/axis[@role=value]"
```
