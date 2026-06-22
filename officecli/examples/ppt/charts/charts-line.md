# Line Charts Showcase

This demo consists of three files that work together:

- **charts-line.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-line.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-line.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-line.py
# → charts-line.pptx
```

## Chart Slides

### Slide 1 — Variants

```bash
CATS="Mon,Tue,Wed,Thu,Fri"
D2="A:50,60,70,65,80;B:40,45,55,60,75"

officecli add charts-line.pptx /slide[1] --type chart \
  --prop chartType=line --prop title="line" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-line.pptx /slide[1] --type chart \
  --prop chartType=stackedLine --prop title="stackedLine" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-line.pptx /slide[1] --type chart \
  --prop chartType=percentStackedLine --prop title="percentStackedLine" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

officecli add charts-line.pptx /slide[1] --type chart \
  --prop chartType=line3d --prop title="line3d" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType` (line/stackedLine/percentStackedLine/line3d)

### Slide 2 — Markers

```bash
# Explicit marker with symbol, size, and color
officecli add charts-line.pptx /slide[2] --type chart \
  --prop chartType=line --prop title="marker=circle:8:FF0000" \
  --prop marker="circle:8:FF0000" --prop linewidth=2 --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

officecli add charts-line.pptx /slide[2] --type chart \
  --prop chartType=line --prop title="marker=square:6" \
  --prop marker="square:6" --prop linewidth=2 --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

officecli add charts-line.pptx /slide[2] --type chart \
  --prop chartType=line --prop title="marker=diamond:10:0070C0" \
  --prop marker="diamond:10:0070C0" --prop linewidth=2 --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

# showMarker=true: use default auto-markers
officecli add charts-line.pptx /slide[2] --type chart \
  --prop chartType=line --prop title="showMarker=true (default markers)" \
  --prop showMarker=true --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2"
```

**Features:** `marker` (symbol:size:color compound), symbols: circle/square/diamond/triangle/star/…; `showMarker=true` (auto markers), `linewidth`

### Slide 3 — Smoothing and Line Dash

```bash
# Smooth curve
officecli add charts-line.pptx /slide[3] --type chart \
  --prop chartType=line --prop title="smooth=true" \
  --prop smooth=true --prop linewidth=2.5 --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

# Dashed line styles
officecli add charts-line.pptx /slide[3] --type chart \
  --prop chartType=line --prop title="linedash=dash" \
  --prop linedash=dash --prop linewidth=2 --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

officecli add charts-line.pptx /slide[3] --type chart \
  --prop chartType=line --prop title="linedash=dot" \
  --prop linedash=dot --prop linewidth=2 --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

officecli add charts-line.pptx /slide[3] --type chart \
  --prop chartType=line --prop title="linedash=dashDot" \
  --prop linedash=dashDot --prop linewidth=2 --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"
```

**Features:** `smooth`, `linewidth` (pt float), `linedash` (solid/dash/dot/dashDot/longDash/longDashDot/longDashDotDot)

### Slide 4 — Title and Legend

```bash
officecli add charts-line.pptx /slide[4] --type chart \
  --prop chartType=line --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-line.pptx /slide[4] --type chart \
  --prop chartType=line --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-line.pptx /slide[4] --type chart \
  --prop chartType=line --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-line.pptx /slide[4] --type chart \
  --prop chartType=line --prop autotitledeleted=true --prop legend=none \
  --prop categories="$CATS" --prop data="$D2"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 5 — Data Labels

```bash
officecli add charts-line.pptx /slide[5] --type chart \
  --prop chartType=line --prop title="dataLabels=value @ top" \
  --prop dataLabels=value --prop labelPos=top \
  --prop labelfont="10:333333:Calibri" --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

officecli add charts-line.pptx /slide[5] --type chart \
  --prop chartType=line --prop title="value,category" \
  --prop dataLabels="value,category" --prop labelPos=top --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

officecli add charts-line.pptx /slide[5] --type chart \
  --prop chartType=line --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

officecli add charts-line.pptx /slide[5] --type chart \
  --prop chartType=line --prop title="labelfont styled" \
  --prop dataLabels=value --prop labelPos=top \
  --prop labelfont="12:C00000:Georgia" --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"
```

**Features:** `dataLabels` (value/category/none or combined), `labelPos` (top/center/insideEnd/outsideEnd/bestFit), `labelfont`

### Slide 6 — Axes

```bash
# Axis scaling, titles, number format
officecli add charts-line.pptx /slide[6] --type chart \
  --prop chartType=line --prop title="min/max + titles" --prop legend=none \
  --prop axismin=0 --prop axismax=100 --prop majorunit=25 \
  --prop axistitle="Visits" --prop cattitle="Day" \
  --prop axisfont="10:333333:Calibri" --prop axisline="666666:1" \
  --prop axisnumfmt="#,##0" \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

# Gridlines and tick marks
officecli add charts-line.pptx /slide[6] --type chart \
  --prop chartType=line --prop title="gridlines + ticks" --prop legend=none \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop majorTickMark=out --prop minorTickMark=in --prop tickLabelPos=nextTo \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

# Label rotation
officecli add charts-line.pptx /slide[6] --type chart \
  --prop chartType=line --prop title="labelrotation=-30" --prop legend=none \
  --prop labelrotation=-30 \
  --prop categories="January,February,March,April,May,June" \
  --prop data="A:60,90,140,180,160,210"

# Logarithmic scale on value axis
officecli add charts-line.pptx /slide[6] --type chart \
  --prop chartType=line --prop title="logbase=10" --prop legend=none \
  --prop logbase=10 --prop axismin=1 --prop axismax=10000 \
  --prop categories="$CATS" --prop data="Growth:5,50,500,5000,3000"
```

**Features:** `axismin/max`, `majorunit`, `axistitle/cattitle`, `axisfont/axisline/axisnumfmt`, `gridlines/minorGridlines`, `majorTickMark/minorTickMark/tickLabelPos`, `labelrotation`, `logbase`

### Slide 7 — Overlays

```bash
# Drop lines + hi-low lines
officecli add charts-line.pptx /slide[7] --type chart \
  --prop chartType=line --prop title="droplines + hilowlines" \
  --prop droplines="808080:0.5" --prop hilowlines=true \
  --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="High:130,135,140,138,145;Low:118,122,128,125,132"

# Up-down bars with custom colors
officecli add charts-line.pptx /slide[7] --type chart \
  --prop chartType=line --prop title="updownbars=150:00AA00:FF0000" \
  --prop updownbars="150:00AA00:FF0000" --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="Open:120,128,130,135,138;Close:128,125,135,138,142"

# Trendline + error bars
officecli add charts-line.pptx /slide[7] --type chart \
  --prop chartType=line --prop title="trendline=linear + errbars=stdDev:1" \
  --prop trendline=linear --prop errbars="stdDev:1" --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"

# Reference line (horizontal threshold)
officecli add charts-line.pptx /slide[7] --type chart \
  --prop chartType=line --prop title="referenceline=70:FF0000:Target" \
  --prop referenceline="70:FF0000:Target" --prop legend=none \
  --prop categories="$CATS" --prop data="A:50,60,70,65,80"
```

**Features:** `droplines` (color:width), `hilowlines` (true or color:width), `updownbars` (gapWidth:upColor:downColor), `trendline` (linear/…), `errbars`, `referenceline` (value:color:label)

### Slide 8 — Per-Series Set and Presets

```bash
for p in minimal dark corporate; do
  officecli add charts-line.pptx /slide[8] --type chart \
    --prop chartType=line --prop preset=$p --prop title="preset=$p" \
    --prop legend=bottom --prop categories="$CATS" --prop data="$D2"
done

officecli add charts-line.pptx /slide[8] --type chart \
  --prop chartType=line --prop title="chart-series Set per line" \
  --prop showMarker=true --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2"

# Per-series mutation: lineWidth, lineDash, marker, markerSize, smooth
officecli set charts-line.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Alpha" --prop color=C00000 --prop lineWidth=2.5 \
  --prop lineDash=solid --prop marker=circle --prop markerSize=9 \
  --prop smooth=true
officecli set charts-line.pptx "/slide[8]/chart[4]/series[2]" \
  --prop name="Beta" --prop color=2E75B6 --prop lineWidth=1.5 \
  --prop lineDash=dash --prop marker=diamond --prop markerSize=8
```

**Features:** `preset`, `chart-series Set`: `name`, `color`, `lineWidth`, `lineDash`, `marker`, `markerSize`, `smooth`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Chart types:** line, stackedLine, percentStackedLine, line3d | 1 |
| **marker** (symbol:size:color compound) | 2 |
| **showMarker** (auto markers) | 2 |
| **linewidth** | 2, 3 |
| **smooth** | 3 |
| **linedash** (solid/dash/dot/dashDot/longDash/…) | 3 |
| **title.font/size/color/bold** | 4 |
| **legend** positions, legendFont, legend.overlay | 4 |
| **autotitledeleted** | 4 |
| **dataLabels:** value/category/none + combined | 5 |
| **labelPos** (top/center/insideEnd/outsideEnd) | 5 |
| **labelfont** | 5 |
| **axismin/max**, majorunit, axistitle/cattitle | 6 |
| **axisfont/axisline/axisnumfmt** | 6 |
| **gridlines/minorGridlines**, tickmarks | 6 |
| **labelrotation**, **logbase** | 6 |
| **droplines** | 7 |
| **hilowlines** | 7 |
| **updownbars** (gapWidth:upColor:downColor) | 7 |
| **trendline**, errbars | 7 |
| **referenceline** | 7 |
| **preset** | 8 |
| **chart-series Set:** lineWidth/lineDash/marker/markerSize/smooth | 8 |

## Inspect the Generated File

```bash
officecli query charts-line.pptx chart
officecli get charts-line.pptx "/slide[1]/chart[1]"
officecli get charts-line.pptx "/slide[7]/chart[1]"
officecli get charts-line.pptx "/slide[8]/chart[4]/series[1]"
```
