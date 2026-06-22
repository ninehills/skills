# Radar Charts Showcase

This demo consists of three files that work together:

- **charts-radar.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-radar.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-radar.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-radar.py
# → charts-radar.pptx
```

## Chart Slides

### Slide 1 — radarstyle Variants

```bash
CATS="Speed,Power,Range,Style,Tech,Price"

officecli add charts-radar.pptx /slide[1] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="radarstyle=standard" --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-radar.pptx /slide[1] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="radarstyle=marker" --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-radar.pptx /slide[1] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="radarstyle=filled" --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

officecli add charts-radar.pptx /slide[1] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="single series" --prop legend=bottom \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType=radar`, `radarstyle` (standard/marker/filled)

### Slide 2 — Title and Legend

```bash
officecli add charts-radar.pptx /slide[2] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="Styled title" --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[2] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="legend=top + legendFont" --prop legend=top \
  --prop legendFont="10:333333:Calibri" \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[2] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="legend.overlay=true" --prop legend=topRight \
  --prop legend.overlay=true \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[2] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop autotitledeleted=true --prop legend=none \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 3 — Data Labels

```bash
officecli add charts-radar.pptx /slide[3] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="value" --prop dataLabels=value \
  --prop labelfont="9:333333:Calibri" --prop legend=none \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[3] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="value,series" --prop dataLabels="value,series" \
  --prop legend=bottom --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[3] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="value,category" --prop dataLabels="value,category" \
  --prop legend=none --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[3] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="dataLabels=none" --prop dataLabels=none \
  --prop legend=bottom --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"
```

**Features:** `dataLabels` (value/series/category/none or combined), `labelfont`

### Slide 4 — Axes

```bash
officecli add charts-radar.pptx /slide[4] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="min/max + titles" --prop legend=none \
  --prop axismin=0 --prop axismax=10 --prop majorunit=2 \
  --prop axisfont="10:333333:Calibri" \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[4] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="gridlines + minorGridlines" --prop legend=none \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[4] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="labelrotation=30" --prop labelrotation=30 --prop legend=none \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[4] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="axisnumfmt=0.0" --prop axisnumfmt="0.0" --prop legend=none \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"
```

**Features:** `axismin/max`, `majorunit`, `axisfont`, `gridlines/minorGridlines`, `labelrotation`, `axisnumfmt`

### Slide 5 — Series Styling

```bash
officecli add charts-radar.pptx /slide[5] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="colors + seriesoutline" --prop legend=bottom \
  --prop colors="4472C4,ED7D31" --prop seriesoutline="000000:0.5" \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[5] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="gradient + seriesshadow" --prop legend=none \
  --prop gradient="FF6600-FFCC00" --prop seriesshadow="000000-5-45-3-50" \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[5] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="transparency=40" --prop legend=bottom \
  --prop transparency=40 \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[5] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="per-series gradients" --prop legend=bottom \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"
```

**Features:** `colors`, `seriesoutline`, `gradient`, `seriesshadow`, `transparency`, `gradients`

### Slide 6 — Markers (radarstyle=marker only)

```bash
officecli add charts-radar.pptx /slide[6] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="circle:10:FF0000" --prop marker="circle:10:FF0000" \
  --prop legend=none --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[6] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="square:8:0070C0" --prop marker="square:8:0070C0" \
  --prop legend=none --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[6] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="diamond:12" --prop marker="diamond:12" \
  --prop legend=none --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[6] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="triangle:10:70AD47" --prop marker="triangle:10:70AD47" \
  --prop legend=none --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"
```

**Features:** `marker` (symbol:size:color compound), symbols: circle/square/diamond/triangle

### Slide 7 — Backgrounds

```bash
officecli add charts-radar.pptx /slide[7] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="chartareafill + plotFill + borders" --prop legend=bottom \
  --prop chartareafill=FFF8E7 --prop plotFill=FAFAFA \
  --prop chartborder="000000:1" --prop plotborder="CCCCCC:0.5" \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[7] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="roundedcorners=true" --prop legend=bottom \
  --prop roundedcorners=true --prop chartborder="4472C4:2" \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli add charts-radar.pptx /slide[7] --type chart \
  --prop chartType=radar --prop radarstyle=standard \
  --prop title="plotFill=none" --prop plotFill=none --prop legend=none \
  --prop categories="$CATS" --prop data="A:8,7,9,6,8,7"

officecli add charts-radar.pptx /slide[7] --type chart \
  --prop chartType=radar --prop radarstyle=filled \
  --prop title="chartareafill=none" --prop chartareafill=none --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"
```

**Features:** `chartareafill`, `plotFill`, `chartborder`, `plotborder`, `roundedcorners`

### Slide 8 — Presets and Per-Series Set

```bash
for p in minimal dark corporate; do
  officecli add charts-radar.pptx /slide[8] --type chart \
    --prop chartType=radar --prop radarstyle=filled --prop preset=$p \
    --prop title="preset=$p" --prop legend=bottom \
    --prop categories="$CATS" \
    --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"
done

officecli add charts-radar.pptx /slide[8] --type chart \
  --prop chartType=radar --prop radarstyle=marker \
  --prop title="chart-series Set" --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="Model A:8,7,9,6,8,7;Model B:6,9,7,8,9,6"

officecli set charts-radar.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed A" --prop color=C00000 \
  --prop marker=circle --prop markerSize=9
```

**Features:** `preset`, `chart-series Set`: `name`, `color`, `marker`, `markerSize`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **radarstyle:** standard/marker/filled | 1 |
| **title.font/size/color/bold** | 2 |
| **legend** positions, legendFont, legend.overlay | 2 |
| **autotitledeleted** | 2 |
| **dataLabels:** value/series/category/none | 3 |
| **labelfont** | 3 |
| **axismin/max**, majorunit, axisfont | 4 |
| **gridlines/minorGridlines, labelrotation, axisnumfmt** | 4 |
| **colors, seriesoutline, gradient, seriesshadow** | 5 |
| **transparency, gradients** | 5 |
| **marker** (symbol:size:color) — radarstyle=marker | 6 |
| **chartareafill, plotFill, chartborder, plotborder, roundedcorners** | 7 |
| **preset** | 8 |
| **chart-series Set:** name/color/marker/markerSize | 8 |

## Inspect the Generated File

```bash
officecli query charts-radar.pptx chart
officecli get charts-radar.pptx "/slide[1]/chart[1]"
officecli get charts-radar.pptx "/slide[6]/chart[1]"
officecli get charts-radar.pptx "/slide[8]/chart[4]/series[1]"
```
