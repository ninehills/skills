# Waterfall Charts Showcase

This demo consists of three files that work together:

- **charts-waterfall.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-waterfall.pptx** — The generated 8-slide deck (4 charts per slide, with one hero full-slide chart on slide 7).
- **charts-waterfall.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-waterfall.py
# → charts-waterfall.pptx
```

In a waterfall chart, positive values are "increase" bars, negative values are "decrease" bars, and the first/last values are typically "total" bars. The `increaseColor`, `decreaseColor`, and `totalColor` properties control each segment type.

## Chart Slides

### Slide 1 — Basic Waterfall (Default Colors)

```bash
CATS="Start,Q1,Q2,Q3,Q4,End"
D="Cashflow:100,30,-15,40,-10,145"

officecli add charts-waterfall.pptx /slide[1] --type chart \
  --prop chartType=waterfall --prop title="Default colors" --prop legend=none \
  --prop categories="$CATS" --prop data="$D" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-waterfall.pptx /slide[1] --type chart \
  --prop chartType=waterfall --prop title="Default + dataTable" \
  --prop dataTable=true --prop legend=none \
  --prop categories="$CATS" --prop data="$D" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-waterfall.pptx /slide[1] --type chart \
  --prop chartType=waterfall --prop title="With legend" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

# 7-step P&L walk
officecli add charts-waterfall.pptx /slide[1] --type chart \
  --prop chartType=waterfall --prop title="7-step P&L" --prop legend=none \
  --prop categories="Open,Revenue,COGS,Opex,R&D,Tax,Net" \
  --prop data="P&L:100,80,-30,-25,-15,-10,100" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType=waterfall`, `legend`, `dataTable`, multi-step P&L data

### Slide 2 — Color Schemes

```bash
# green/red/blue (standard traffic-light colors)
officecli add charts-waterfall.pptx /slide[2] --type chart \
  --prop chartType=waterfall --prop title="green/red/blue (default-ish)" \
  --prop increaseColor=00AA00 --prop decreaseColor=FF0000 \
  --prop totalColor=4472C4 --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

# Corporate teal/orange/navy
officecli add charts-waterfall.pptx /slide[2] --type chart \
  --prop chartType=waterfall --prop title="corporate (teal/orange/navy)" \
  --prop increaseColor=008080 --prop decreaseColor=D86600 \
  --prop totalColor=1F3864 --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

# Monochrome grey scale
officecli add charts-waterfall.pptx /slide[2] --type chart \
  --prop chartType=waterfall --prop title="monochrome" \
  --prop increaseColor=606060 --prop decreaseColor=A0A0A0 \
  --prop totalColor=303030 --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

# Vivid green/red/blue
officecli add charts-waterfall.pptx /slide[2] --type chart \
  --prop chartType=waterfall --prop title="vivid" \
  --prop increaseColor=00C853 --prop decreaseColor=D50000 \
  --prop totalColor=2962FF --prop legend=none \
  --prop categories="$CATS" --prop data="$D"
```

**Features:** `increaseColor` (positive bars), `decreaseColor` (negative bars), `totalColor` (first/last total bars)

### Slide 3 — Title and Legend

```bash
officecli add charts-waterfall.pptx /slide[3] --type chart \
  --prop chartType=waterfall --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[3] --type chart \
  --prop chartType=waterfall --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[3] --type chart \
  --prop chartType=waterfall --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[3] --type chart \
  --prop chartType=waterfall --prop autotitledeleted=true --prop legend=none \
  --prop categories="$CATS" --prop data="$D"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 4 — Data Labels

```bash
officecli add charts-waterfall.pptx /slide[4] --type chart \
  --prop chartType=waterfall --prop title="value" \
  --prop dataLabels=value --prop labelfont="10:333333:Calibri" --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[4] --type chart \
  --prop chartType=waterfall --prop title="value,category" \
  --prop dataLabels="value,category" --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[4] --type chart \
  --prop chartType=waterfall --prop title="value @ outsideEnd" \
  --prop dataLabels=value --prop labelPos=outsideEnd --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[4] --type chart \
  --prop chartType=waterfall --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=none \
  --prop categories="$CATS" --prop data="$D"
```

**Features:** `dataLabels` (value/category/none or combined), `labelPos` (outsideEnd), `labelfont`

### Slide 5 — Axes

```bash
officecli add charts-waterfall.pptx /slide[5] --type chart \
  --prop chartType=waterfall --prop title="min/max + titles" \
  --prop axismin=0 --prop axismax=200 --prop majorunit=50 \
  --prop axistitle="USD" --prop cattitle="Phase" \
  --prop axisfont="10:333333:Calibri" --prop axisnumfmt='$#,##0' \
  --prop legend=none --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[5] --type chart \
  --prop chartType=waterfall --prop title="gridlines + minorGridlines" \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop legend=none --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[5] --type chart \
  --prop chartType=waterfall --prop title="labelrotation=-30" \
  --prop labelrotation=-30 --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[5] --type chart \
  --prop chartType=waterfall --prop title="dispunits=thousands" \
  --prop dispunits=thousands --prop legend=none \
  --prop categories="$CATS" \
  --prop data="USD:100000,30000,-15000,40000,-10000,145000"
```

**Features:** `axismin/max`, `majorunit`, `axistitle/cattitle`, `axisfont`, `axisnumfmt` (currency), `gridlines/minorGridlines`, `labelrotation`, `dispunits`

### Slide 6 — Backgrounds

```bash
officecli add charts-waterfall.pptx /slide[6] --type chart \
  --prop chartType=waterfall --prop title="chartareafill + chartborder" \
  --prop chartareafill=FFF8E7 --prop chartborder="000000:1" \
  --prop plotFill=FAFAFA --prop plotborder="CCCCCC:0.5" \
  --prop legend=none --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[6] --type chart \
  --prop chartType=waterfall --prop title="roundedcorners=true" \
  --prop roundedcorners=true --prop chartborder="4472C4:2" \
  --prop legend=none --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[6] --type chart \
  --prop chartType=waterfall --prop title="plotFill=none" \
  --prop plotFill=none --prop gridlines=none \
  --prop legend=none --prop categories="$CATS" --prop data="$D"

officecli add charts-waterfall.pptx /slide[6] --type chart \
  --prop chartType=waterfall --prop title="chartareafill=none" \
  --prop chartareafill=none --prop legend=none \
  --prop categories="$CATS" --prop data="$D"
```

**Features:** `chartareafill`, `plotFill`, `chartborder`, `plotborder`, `roundedcorners`, `gridlines=none`

### Slide 7 — Hero Cashflow Waterfall (Full Slide)

A real-world FY24 P&L walk on a full-slide canvas with styled title, custom colors, and labeled data points.

```bash
officecli add charts-waterfall.pptx /slide[7] --type chart \
  --prop chartType=waterfall --prop title="FY24 P&L Walk" \
  --prop title.font=Helvetica --prop title.size=22 \
  --prop title.bold=true --prop title.color=1F3864 \
  --prop increaseColor=00C853 --prop decreaseColor=D50000 \
  --prop totalColor=2962FF \
  --prop dataLabels="value,category" --prop labelPos=outsideEnd \
  --prop labelfont="11:333333:Helvetica" \
  --prop axistitle="USD" --prop axisnumfmt='$#,##0' \
  --prop gridlines="E0E0E0:0.3" --prop legend=none \
  --prop categories="Open,Revenue,COGS,Opex,R&D,Tax,Net" \
  --prop data="P&L:100,80,-30,-25,-15,-10,100" \
  --prop x=1in --prop y=1.05in --prop width=11.3in --prop height=6.2in
```

**Features:** Full-slide hero layout, combined `increaseColor/decreaseColor/totalColor`, full label suite, custom `title.font/size/bold/color`

### Slide 8 — Presets

```bash
for p in minimal dark corporate colorful; do
  officecli add charts-waterfall.pptx /slide[8] --type chart \
    --prop chartType=waterfall --prop preset=$p --prop title="preset=$p" \
    --prop legend=none --prop categories="$CATS" --prop data="$D"
done
```

**Features:** `preset` (minimal/dark/corporate/colorful)

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **chartType=waterfall** | 1 |
| **dataTable**, **legend** | 1 |
| **increaseColor** (positive bars) | 2 |
| **decreaseColor** (negative bars) | 2 |
| **totalColor** (total/subtotal bars) | 2 |
| **title.font/size/color/bold** | 3 |
| **legend** positions, legendFont, legend.overlay | 3 |
| **autotitledeleted** | 3 |
| **dataLabels:** value/category/none + combined | 4 |
| **labelPos** (outsideEnd), **labelfont** | 4 |
| **axismin/max**, majorunit, axistitle/cattitle | 5 |
| **axisfont**, axisnumfmt (currency), gridlines | 5 |
| **labelrotation**, dispunits | 5 |
| **chartareafill**, plotFill, chartborder, plotborder | 6 |
| **roundedcorners**, gridlines=none | 6 |
| **Hero layout** (full-slide, combined features) | 7 |
| **preset** (minimal/dark/corporate/colorful) | 8 |

## Inspect the Generated File

```bash
officecli query charts-waterfall.pptx chart
officecli get charts-waterfall.pptx "/slide[1]/chart[1]"
officecli get charts-waterfall.pptx "/slide[2]/chart[1]"
officecli get charts-waterfall.pptx "/slide[7]/chart[1]"
```
