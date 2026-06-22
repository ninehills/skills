# Column Charts Showcase

This demo consists of three files that work together:

- **charts-column.py** — Python script that calls `officecli` commands to generate the deck. Each chart command is shown as a copyable shell command below.
- **charts-column.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-column.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-column.py
# → charts-column.pptx
```

## Chart Slides

### Slide 1 — Basic Variants

Four column chart type variants covering the basic options.

```bash
# Standard clustered column
officecli add charts-column.pptx /slide[1] --type chart \
  --prop chartType=column --prop title="column" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

# Stacked column — values accumulate per category
officecli add charts-column.pptx /slide[1] --type chart \
  --prop chartType=stackedColumn --prop title="stackedColumn" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

# 100% stacked column — proportional
officecli add charts-column.pptx /slide[1] --type chart \
  --prop chartType=percentStackedColumn --prop title="percentStackedColumn" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

# 3D column with perspective and depth
officecli add charts-column.pptx /slide[1] --type chart \
  --prop chartType=column3d --prop view3d="15,20,30" --prop gapdepth=150 \
  --prop title="column3d (view3d=15,20,30)" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType` (column/stackedColumn/percentStackedColumn/column3d), `categories`, `data` (Name:v1,v2,… semicolon-separated), `legend` (bottom/top/right/none), `view3d` (rotX,rotY,perspective), `gapdepth`

### Slide 2 — Title and Legend

```bash
# Styled chart title
officecli add charts-column.pptx /slide[2] --type chart \
  --prop chartType=column --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

# Legend at top with custom font
officecli add charts-column.pptx /slide[2] --type chart \
  --prop chartType=column --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

# Legend overlay (drawn over chart area, not reserving space)
officecli add charts-column.pptx /slide[2] --type chart \
  --prop chartType=column --prop title="legend=topRight overlay" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"

# No title, no legend
officecli add charts-column.pptx /slide[2] --type chart \
  --prop chartType=column --prop autotitledeleted=true --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;West:95,108,115,128"
```

**Features:** `title.font`, `title.size`, `title.color`, `title.bold`, `legend` (bottom/top/topRight/none), `legendFont` (size:color:face), `legend.overlay`, `autotitledeleted`

### Slide 3 — Data Labels

```bash
# Value labels above bars (outsideEnd)
officecli add charts-column.pptx /slide[3] --type chart \
  --prop chartType=column --prop title="value @ outsideEnd" \
  --prop dataLabels=value --prop labelPos=outsideEnd \
  --prop labelfont="10:333333:Calibri" --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Value + category labels inside top of bars
officecli add charts-column.pptx /slide[3] --type chart \
  --prop chartType=column --prop title="value,category @ insideEnd" \
  --prop dataLabels="value,category" --prop labelPos=insideEnd \
  --prop labelfont="9:FFFFFF:Calibri" --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Stacked column with center labels
officecli add charts-column.pptx /slide[3] --type chart \
  --prop chartType=stackedColumn --prop title="stacked + center labels" \
  --prop dataLabels=value --prop labelPos=center \
  --prop labelfont="9:FFFFFF:Calibri" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110"

# Suppress labels
officecli add charts-column.pptx /slide[3] --type chart \
  --prop chartType=column --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=none \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"
```

**Features:** `dataLabels` (value/category/percent/none or comma-combined), `labelPos` (outsideEnd/insideEnd/insideBase/center), `labelfont` (size:color:face)

### Slide 4 — Axes

```bash
# Axis scaling, titles, number format, axis line
officecli add charts-column.pptx /slide[4] --type chart \
  --prop chartType=column --prop title="axis min/max + titles + numfmt" \
  --prop legend=none \
  --prop axismin=0 --prop axismax=200 --prop majorunit=50 --prop minorunit=10 \
  --prop axistitle="Revenue (USD)" --prop cattitle="Quarter" \
  --prop axisfont="10:333333:Calibri" --prop axisline="666666:1" \
  --prop axisnumfmt="#,##0" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="Rev:60,90,140,180"

# Gridlines and tick marks
officecli add charts-column.pptx /slide[4] --type chart \
  --prop chartType=column --prop title="gridlines + minorGridlines + ticks" \
  --prop legend=none \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop majorTickMark=out --prop minorTickMark=in --prop tickLabelPos=nextTo \
  --prop labelrotation=-30 \
  --prop categories="January,February,March,April" \
  --prop data="A:60,90,140,180"

# Display units (thousands)
officecli add charts-column.pptx /slide[4] --type chart \
  --prop chartType=column --prop title="dispunits=thousands" --prop legend=none \
  --prop dispunits=thousands \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="Rev:120000,135000,148000,162000"

# Secondary axis (combo: column + line on right axis)
officecli add charts-column.pptx /slide[4] --type chart \
  --prop chartType=combo --prop combotypes="column,line" --prop secondaryaxis=2 \
  --prop title="secondaryaxis=2 (line on right)" --prop legend=bottom \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="Sales:120,135,148,162;Growth %:5,12,18,22"

# chart-axis Set: modify axis properties after chart creation
officecli set charts-column.pptx "/slide[4]/chart[1]/axis[@role=value]" \
  --prop title="Revenue (USD)" --prop format='$#,##0' \
  --prop majorGridlines=true --prop minorGridlines=false \
  --prop max=200 --prop min=0 --prop majorUnit=50
```

**Features:** `axismin`, `axismax`, `majorunit`, `minorunit`, `axistitle`, `cattitle`, `axisfont`, `axisline` (color:width), `axisnumfmt`, `gridlines` (color:width), `minorGridlines`, `majorTickMark` (out/in/cross/none), `minorTickMark`, `tickLabelPos` (nextTo/high/low/none), `labelrotation`, `dispunits` (hundreds/thousands/millions/…), `secondaryaxis`, `chart-axis Set`

### Slide 5 — Series Styling

```bash
# Explicit palette + series outline
officecli add charts-column.pptx /slide[5] --type chart \
  --prop chartType=column --prop title="colors + seriesoutline" --prop legend=bottom \
  --prop colors="4472C4,ED7D31,A5A5A5" --prop seriesoutline="000000:0.5" \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110"

# Gradient fill + drop shadow on series
officecli add charts-column.pptx /slide[5] --type chart \
  --prop chartType=column --prop title="gradient + seriesshadow" --prop legend=bottom \
  --prop gradient="FF6600-FFCC00:90" --prop seriesshadow="000000-5-45-3-50" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Per-series gradients + transparency
officecli add charts-column.pptx /slide[5] --type chart \
  --prop chartType=column --prop title="per-series gradients + transparency=30" \
  --prop legend=bottom \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" --prop transparency=30 \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:40,70,100,130"

# Invert negative bars + conditional color rule
officecli add charts-column.pptx /slide[5] --type chart \
  --prop chartType=column --prop title="invertifneg + colorrule" --prop legend=none \
  --prop invertifneg=true --prop colorrule="0:FF0000:00AA00" \
  --prop categories="Q1,Q2,Q3,Q4,Q5" \
  --prop data="Net:60,-30,40,-50,80"

# chart-series Set: recolor series 1 after creation
officecli set charts-column.pptx "/slide[5]/chart[1]/series[1]" \
  --prop color=2E75B6
```

**Features:** `colors` (comma palette), `seriesoutline` (color:width), `gradient` (color1-color2:angle), `seriesshadow` (color-blur-angle-dist-opacity), `gradients` (semicolon-separated per-series), `transparency` (0–100), `invertifneg`, `colorrule` (threshold:belowColor:aboveColor), `chart-series Set color=`

### Slide 6 — Layout and Overlays

```bash
# Gap width + overlap (clustered spacing)
officecli add charts-column.pptx /slide[6] --type chart \
  --prop chartType=column --prop title="gapwidth=50 + overlap=20" \
  --prop legend=bottom --prop gapwidth=50 --prop overlap=20 \
  --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

# Reference line (horizontal threshold marker)
officecli add charts-column.pptx /slide[6] --type chart \
  --prop chartType=column --prop title="referenceline=100" --prop legend=none \
  --prop referenceline="100:FF0000:Target" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Error bars
officecli add charts-column.pptx /slide[6] --type chart \
  --prop chartType=column --prop title="errbars=percentage:10" --prop legend=none \
  --prop errbars="percentage:10" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Data table + trend line
officecli add charts-column.pptx /slide[6] --type chart \
  --prop chartType=column --prop title="dataTable=true + trendline=linear" \
  --prop legend=bottom --prop dataTable=true --prop trendline=linear \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"
```

**Features:** `gapwidth` (0–500), `overlap` (-100–100), `referenceline` (value:color:label), `errbars` (fixedVal/percentage/stdDev/stdError:value), `trendline` (linear/poly/exp/log/power/movingAvg), `dataTable`

### Slide 7 — Backgrounds

```bash
# Chart area + plot area fills with borders
officecli add charts-column.pptx /slide[7] --type chart \
  --prop chartType=column --prop title="chartareafill + plotFill + borders" \
  --prop legend=bottom \
  --prop chartareafill=FFF8E7 --prop plotFill=FAFAFA \
  --prop chartborder="000000:1" --prop plotborder="CCCCCC:0.5" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Rounded corners on chart area
officecli add charts-column.pptx /slide[7] --type chart \
  --prop chartType=column --prop title="roundedcorners=true" \
  --prop legend=bottom --prop roundedcorners=true \
  --prop chartborder="4472C4:2" \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Transparent plot area, no gridlines
officecli add charts-column.pptx /slide[7] --type chart \
  --prop chartType=column --prop title="plotFill=none, gridlines=none" \
  --prop legend=none --prop plotFill=none --prop gridlines=none \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"

# Per-bar color variation (varyColors)
officecli add charts-column.pptx /slide[7] --type chart \
  --prop chartType=column --prop title="varyColors=true (single series)" \
  --prop legend=none --prop varyColors=true \
  --prop categories="Q1,Q2,Q3,Q4" --prop data="A:60,90,140,180"
```

**Features:** `chartareafill` (hex or none), `plotFill` (hex or none), `chartborder` (color:width), `plotborder` (color:width), `roundedcorners`, `gridlines=none`, `varyColors`

### Slide 8 — Presets and Per-Series Control

```bash
# Built-in preset bundles
officecli add charts-column.pptx /slide[8] --type chart \
  --prop chartType=column --prop preset=minimal --prop title="preset=minimal" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

officecli add charts-column.pptx /slide[8] --type chart \
  --prop chartType=column --prop preset=corporate --prop title="preset=corporate" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

officecli add charts-column.pptx /slide[8] --type chart \
  --prop chartType=column --prop preset=dark --prop title="preset=dark" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

# Per-series control via seriesN.* compound properties
officecli add charts-column.pptx /slide[8] --type chart \
  --prop chartType=column --prop title="seriesN.* Add + chart-series Set" \
  --prop legend=bottom --prop categories="Q1,Q2,Q3,Q4" \
  --prop series1.name="Product A" --prop series1.values="60,90,140,180" \
  --prop series1.color=4472C4 \
  --prop series2.name="Product B" --prop series2.values="50,75,110,150" \
  --prop series2.color=ED7D31 \
  --prop series3.name="Product C" --prop series3.values="40,65,90,120" \
  --prop series3.color=70AD47

# chart-series Set: rename + recolor after creation
officecli set charts-column.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed Alpha" --prop color=C00000
```

**Features:** `preset` (minimal/corporate/dark/colorful), `series1.name`/`series1.values`/`series1.color` (per-series at Add time), `chart-series Set name=/color=` (post-Add mutation)

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Chart types:** column, stackedColumn, percentStackedColumn, column3d | 1 |
| **3D:** view3d (rotX,rotY,perspective), gapdepth | 1 |
| **Data input:** data=, categories=, seriesN.name/values/color | 1, 8 |
| **Title styling:** title.font/size/color/bold | 2 |
| **Legend:** bottom/top/topRight/none, legendFont, legend.overlay | 2 |
| **autotitledeleted** | 2 |
| **dataLabels:** value/category/percent/none + combo | 3 |
| **labelPos:** outsideEnd/insideEnd/insideBase/center | 3 |
| **labelfont** | 3 |
| **Axis scaling:** axismin/max, majorunit, minorunit | 4 |
| **Axis titles:** axistitle, cattitle | 4 |
| **Axis font/line/numfmt:** axisfont, axisline, axisnumfmt | 4 |
| **Gridlines:** gridlines, minorGridlines, gridlines=none | 4, 7 |
| **Tick marks:** majorTickMark, minorTickMark, tickLabelPos | 4 |
| **labelrotation** | 4 |
| **dispunits** (hundreds/thousands/millions/…) | 4 |
| **secondaryaxis** | 4 |
| **chart-axis Set** | 4 |
| **colors** (palette), **seriesoutline**, **seriesshadow** | 5 |
| **gradient**, **gradients** (per-series) | 5 |
| **transparency** (0–100) | 5 |
| **invertifneg**, **colorrule** | 5 |
| **chart-series Set** (color, name) | 5, 8 |
| **gapwidth**, **overlap** | 6 |
| **referenceline** | 6 |
| **errbars** (fixedVal/percentage/stdDev/stdError) | 6 |
| **trendline** (linear/poly/exp/…) | 6 |
| **dataTable** | 6 |
| **chartareafill**, **plotFill**, **chartborder**, **plotborder** | 7 |
| **roundedcorners** | 7 |
| **varyColors** | 7 |
| **preset** (minimal/corporate/dark/colorful) | 8 |
| **seriesN.*** per-series at Add time | 8 |

## Inspect the Generated File

```bash
officecli query charts-column.pptx chart
officecli get charts-column.pptx "/slide[1]/chart[1]"
officecli get charts-column.pptx "/slide[5]/chart[1]/series[1]"
officecli get charts-column.pptx "/slide[4]/chart[1]/axis[@role=value]"
```
