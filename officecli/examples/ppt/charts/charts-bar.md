# Bar Charts Showcase

This demo consists of three files that work together:

- **charts-bar.py** — Python script that calls `officecli` commands to generate the deck. Each chart command is shown as a copyable shell command below.
- **charts-bar.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-bar.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-bar.py
# → charts-bar.pptx
```

## Chart Slides

### Slide 1 — Basic Variants

```bash
officecli add charts-bar.pptx /slide[1] --type chart \
  --prop chartType=bar --prop title="bar" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-bar.pptx /slide[1] --type chart \
  --prop chartType=stackedBar --prop title="stackedBar" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-bar.pptx /slide[1] --type chart \
  --prop chartType=percentStackedBar --prop title="percentStackedBar" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

officecli add charts-bar.pptx /slide[1] --type chart \
  --prop chartType=bar3d --prop title="bar3d" --prop legend=bottom \
  --prop view3d="15,20,30" \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType` (bar/stackedBar/percentStackedBar/bar3d), `categories`, `data`, `legend`, `view3d`

### Slide 2 — 3D Bar Shapes

```bash
# shape= controls the 3D bar geometry (bar3d only)
officecli add charts-bar.pptx /slide[2] --type chart \
  --prop chartType=bar3d --prop shape=box --prop title="shape=box" \
  --prop legend=none --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

officecli add charts-bar.pptx /slide[2] --type chart \
  --prop chartType=bar3d --prop shape=cylinder --prop title="shape=cylinder" \
  --prop legend=none --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

officecli add charts-bar.pptx /slide[2] --type chart \
  --prop chartType=bar3d --prop shape=cone --prop title="shape=cone" \
  --prop legend=none --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

officecli add charts-bar.pptx /slide[2] --type chart \
  --prop chartType=bar3d --prop shape=pyramid --prop title="shape=pyramid" \
  --prop legend=none --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"
```

**Features:** `shape` (box/cylinder/cone/pyramid) for `bar3d`

### Slide 3 — Title and Legend

```bash
officecli add charts-bar.pptx /slide[3] --type chart \
  --prop chartType=bar --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

officecli add charts-bar.pptx /slide[3] --type chart \
  --prop chartType=bar --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

officecli add charts-bar.pptx /slide[3] --type chart \
  --prop chartType=bar --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

officecli add charts-bar.pptx /slide[3] --type chart \
  --prop chartType=bar --prop autotitledeleted=true --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"
```

**Features:** `title.font`, `title.size`, `title.color`, `title.bold`, `legend` (bottom/top/topRight/none), `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 4 — Data Labels

```bash
officecli add charts-bar.pptx /slide[4] --type chart \
  --prop chartType=bar --prop title="value @ outsideEnd" \
  --prop dataLabels=value --prop labelPos=outsideEnd \
  --prop labelfont="10:333333:Calibri" --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

officecli add charts-bar.pptx /slide[4] --type chart \
  --prop chartType=bar --prop title="value,category @ insideEnd" \
  --prop dataLabels="value,category" --prop labelPos=insideEnd \
  --prop labelfont="9:FFFFFF:Calibri" --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

officecli add charts-bar.pptx /slide[4] --type chart \
  --prop chartType=stackedBar --prop title="stacked + center labels" \
  --prop dataLabels=value --prop labelPos=center \
  --prop labelfont="9:FFFFFF:Calibri" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110"

officecli add charts-bar.pptx /slide[4] --type chart \
  --prop chartType=bar --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"
```

**Features:** `dataLabels` (value/category/percent/none or combined), `labelPos` (outsideEnd/insideEnd/insideBase/center), `labelfont`

### Slide 5 — Axes

```bash
officecli add charts-bar.pptx /slide[5] --type chart \
  --prop chartType=bar --prop title="min/max + titles + numfmt" --prop legend=none \
  --prop axismin=0 --prop axismax=200 --prop majorunit=50 --prop minorunit=10 \
  --prop axistitle="Revenue" --prop cattitle="Quarter" \
  --prop axisfont="10:333333:Calibri" --prop axisline="666666:1" \
  --prop axisnumfmt="#,##0" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="Rev:60,90,140,180"

officecli add charts-bar.pptx /slide[5] --type chart \
  --prop chartType=bar --prop title="gridlines + ticks" --prop legend=none \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop majorTickMark=out --prop minorTickMark=in --prop tickLabelPos=nextTo \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

officecli add charts-bar.pptx /slide[5] --type chart \
  --prop chartType=bar --prop title="labelrotation=-30" --prop legend=none \
  --prop labelrotation=-30 \
  --prop categories="January,February,March,April" \
  --prop data="A:60,90,140,180"

officecli add charts-bar.pptx /slide[5] --type chart \
  --prop chartType=bar --prop title="dispunits=thousands" --prop legend=none \
  --prop dispunits=thousands \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="Rev:120000,135000,148000,162000"

# chart-axis Set: mutate axis after creation
officecli set charts-bar.pptx "/slide[5]/chart[1]/axis[@role=value]" \
  --prop title="Revenue" --prop format='$#,##0' \
  --prop majorGridlines=true --prop max=200 --prop min=0
```

**Features:** `axismin`, `axismax`, `majorunit`, `minorunit`, `axistitle`, `cattitle`, `axisfont`, `axisline`, `axisnumfmt`, `gridlines`, `minorGridlines`, `majorTickMark`, `minorTickMark`, `tickLabelPos`, `labelrotation`, `dispunits`, `chart-axis Set`

### Slide 6 — Series Styling

```bash
officecli add charts-bar.pptx /slide[6] --type chart \
  --prop chartType=bar --prop title="colors + seriesoutline" --prop legend=bottom \
  --prop colors="4472C4,ED7D31,A5A5A5" --prop seriesoutline="000000:0.5" \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110"

officecli add charts-bar.pptx /slide[6] --type chart \
  --prop chartType=bar --prop title="gradient + seriesshadow" --prop legend=bottom \
  --prop gradient="FF6600-FFCC00:90" --prop seriesshadow="000000-5-45-3-50" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

officecli add charts-bar.pptx /slide[6] --type chart \
  --prop chartType=bar --prop title="transparency=30 + gradients" --prop legend=bottom \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" --prop transparency=30 \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:40,70,100,130"

# serlines — leader lines from stacked bar to legend (stackedBar only)
officecli add charts-bar.pptx /slide[6] --type chart \
  --prop chartType=stackedBar --prop title="stacked + serlines=true" \
  --prop serlines=true --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"
```

**Features:** `colors`, `seriesoutline`, `gradient`, `seriesshadow`, `gradients`, `transparency`, `serlines` (stackedBar series connector lines)

### Slide 7 — Overlays

```bash
officecli add charts-bar.pptx /slide[7] --type chart \
  --prop chartType=bar --prop title="referenceline=100" --prop legend=none \
  --prop referenceline="100:FF0000:Target" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

officecli add charts-bar.pptx /slide[7] --type chart \
  --prop chartType=bar --prop title="errbars=fixedVal:10" --prop legend=none \
  --prop errbars="fixedVal:10" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

officecli add charts-bar.pptx /slide[7] --type chart \
  --prop chartType=bar --prop title="gapwidth=50 + overlap=20" --prop legend=bottom \
  --prop gapwidth=50 --prop overlap=20 \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

officecli add charts-bar.pptx /slide[7] --type chart \
  --prop chartType=bar --prop title="dataTable=true" --prop legend=bottom \
  --prop dataTable=true \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"
```

**Features:** `referenceline`, `errbars`, `gapwidth`, `overlap`, `dataTable`

### Slide 8 — Presets and Per-Series Control

```bash
officecli add charts-bar.pptx /slide[8] --type chart \
  --prop chartType=bar --prop preset=minimal --prop title="preset=minimal" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

officecli add charts-bar.pptx /slide[8] --type chart \
  --prop chartType=bar --prop preset=dark --prop title="preset=dark" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

officecli add charts-bar.pptx /slide[8] --type chart \
  --prop chartType=bar --prop preset=corporate --prop title="preset=corporate" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

officecli add charts-bar.pptx /slide[8] --type chart \
  --prop chartType=bar --prop title="seriesN.* Add + chart-series Set" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop series1.name="Product A" --prop series1.values="60,90,140,180" \
  --prop series1.color=4472C4 \
  --prop series2.name="Product B" --prop series2.values="50,75,110,150" \
  --prop series2.color=ED7D31

officecli set charts-bar.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed" --prop color=C00000
```

**Features:** `preset` (minimal/dark/corporate), `series1.name`/`series1.values`/`series1.color`, `chart-series Set`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Chart types:** bar, stackedBar, percentStackedBar, bar3d | 1 |
| **3D bar shape:** box/cylinder/cone/pyramid | 2 |
| **view3d** | 1 |
| **Title styling:** title.font/size/color/bold | 3 |
| **Legend:** positions, legendFont, legend.overlay | 3 |
| **autotitledeleted** | 3 |
| **dataLabels:** value/category/percent/none + combined | 4 |
| **labelPos:** outsideEnd/insideEnd/insideBase/center | 4 |
| **labelfont** | 4 |
| **Axis scaling:** axismin/max, majorunit, minorunit | 5 |
| **Axis titles/font/line/numfmt** | 5 |
| **Gridlines, tick marks** | 5 |
| **labelrotation, dispunits** | 5 |
| **chart-axis Set** | 5 |
| **colors, seriesoutline, seriesshadow** | 6 |
| **gradient, gradients, transparency** | 6 |
| **serlines** (stackedBar connector lines) | 6 |
| **referenceline, errbars** | 7 |
| **gapwidth, overlap, dataTable** | 7 |
| **preset** (minimal/dark/corporate) | 8 |
| **seriesN.*** per-series at Add time | 8 |
| **chart-series Set** | 8 |

## Inspect the Generated File

```bash
officecli query charts-bar.pptx chart
officecli get charts-bar.pptx "/slide[1]/chart[1]"
officecli get charts-bar.pptx "/slide[2]/chart[1]"
officecli get charts-bar.pptx "/slide[5]/chart[1]/axis[@role=value]"
```
