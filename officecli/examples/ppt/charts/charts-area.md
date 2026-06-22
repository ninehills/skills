# Area Charts Showcase

This demo consists of three files that work together:

- **charts-area.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-area.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-area.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-area.py
# → charts-area.pptx
```

## Chart Slides

### Slide 1 — Variants

```bash
officecli add charts-area.pptx /slide[1] --type chart \
  --prop chartType=area --prop title="area" --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-area.pptx /slide[1] --type chart \
  --prop chartType=stackedArea --prop title="stackedArea" --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-area.pptx /slide[1] --type chart \
  --prop chartType=percentStackedArea --prop title="percentStackedArea" \
  --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

officecli add charts-area.pptx /slide[1] --type chart \
  --prop chartType=area3d --prop title="area3d" --prop view3d="15,20,30" \
  --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType` (area/stackedArea/percentStackedArea/area3d), `view3d`

### Slide 2 — Title and Legend

```bash
officecli add charts-area.pptx /slide[2] --type chart \
  --prop chartType=area --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[2] --type chart \
  --prop chartType=area --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[2] --type chart \
  --prop chartType=area --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[2] --type chart \
  --prop chartType=area --prop autotitledeleted=true --prop legend=none \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 3 — Data Labels

```bash
officecli add charts-area.pptx /slide[3] --type chart \
  --prop chartType=area --prop title="dataLabels=value" \
  --prop dataLabels=value --prop labelfont="10:333333:Calibri" --prop legend=none \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[3] --type chart \
  --prop chartType=stackedArea --prop title="stacked + center labels" \
  --prop dataLabels=value --prop labelPos=center --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[3] --type chart \
  --prop chartType=area --prop title="value,category" \
  --prop dataLabels="value,category" --prop labelfont="9:333333:Calibri" \
  --prop legend=none \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[3] --type chart \
  --prop chartType=area --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=none \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"
```

**Features:** `dataLabels` (value/category/none or combined), `labelPos` (center), `labelfont`

### Slide 4 — Axes

```bash
officecli add charts-area.pptx /slide[4] --type chart \
  --prop chartType=area --prop title="min/max + titles" --prop legend=none \
  --prop axismin=0 --prop axismax=100 --prop majorunit=25 \
  --prop axistitle="Value" --prop cattitle="Day" \
  --prop axisfont="10:333333:Calibri" --prop axisline="666666:1" \
  --prop axisnumfmt="#,##0" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[4] --type chart \
  --prop chartType=area --prop title="gridlines + ticks" --prop legend=none \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop majorTickMark=out --prop minorTickMark=in --prop tickLabelPos=nextTo \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[4] --type chart \
  --prop chartType=area --prop title="labelrotation=-30" --prop legend=none \
  --prop labelrotation=-30 \
  --prop categories="January,February,March,April,May,June" \
  --prop data="A:60,90,140,180,160,210"

officecli add charts-area.pptx /slide[4] --type chart \
  --prop chartType=area --prop title="dispunits=thousands" --prop legend=none \
  --prop dispunits=thousands \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Rev:120000,135000,148000,162000,180000"
```

**Features:** `axismin/max`, `majorunit`, `axistitle/cattitle`, `axisfont/axisline/axisnumfmt`, `gridlines/minorGridlines`, `majorTickMark/minorTickMark/tickLabelPos`, `labelrotation`, `dispunits`

### Slide 5 — Series Styling

```bash
officecli add charts-area.pptx /slide[5] --type chart \
  --prop chartType=area --prop title="colors + seriesoutline" --prop legend=bottom \
  --prop colors="4472C4,ED7D31" --prop seriesoutline="000000:0.5" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[5] --type chart \
  --prop chartType=area --prop title="gradient + seriesshadow" --prop legend=none \
  --prop gradient="FF6600-FFCC00:90" --prop seriesshadow="000000-5-45-3-50" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[5] --type chart \
  --prop chartType=area --prop title="per-series gradients + transparency=30" \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" --prop transparency=30 \
  --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[5] --type chart \
  --prop chartType=area --prop title="single + transparency=50" \
  --prop transparency=50 --prop colors="4472C4" --prop legend=none \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"
```

**Features:** `colors`, `seriesoutline`, `gradient`, `seriesshadow`, `gradients`, `transparency`

### Slide 6 — Overlays

```bash
officecli add charts-area.pptx /slide[6] --type chart \
  --prop chartType=area --prop title="referenceline=60" --prop legend=none \
  --prop referenceline="60:FF0000:Target" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[6] --type chart \
  --prop chartType=area --prop title="errbars=percentage:10" --prop legend=none \
  --prop errbars="percentage:10" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[6] --type chart \
  --prop chartType=area --prop title="trendline=linear" --prop legend=none \
  --prop trendline=linear \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[6] --type chart \
  --prop chartType=area --prop title="trendline=movingAvg:3" --prop legend=none \
  --prop trendline="movingAvg:3" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"
```

**Features:** `referenceline`, `errbars`, `trendline` (linear/poly/exp/log/power/movingAvg)

### Slide 7 — Backgrounds

```bash
officecli add charts-area.pptx /slide[7] --type chart \
  --prop chartType=area --prop title="chartareafill + plotFill + borders" \
  --prop legend=bottom \
  --prop chartareafill=FFF8E7 --prop plotFill=FAFAFA \
  --prop chartborder="000000:1" --prop plotborder="CCCCCC:0.5" \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[7] --type chart \
  --prop chartType=area --prop title="roundedcorners=true" \
  --prop roundedcorners=true --prop chartborder="4472C4:2" --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"

officecli add charts-area.pptx /slide[7] --type chart \
  --prop chartType=area --prop title="plotFill=none" \
  --prop plotFill=none --prop gridlines=none --prop legend=none \
  --prop categories="Mon,Tue,Wed,Thu,Fri" --prop data="A:50,60,70,65,80"

officecli add charts-area.pptx /slide[7] --type chart \
  --prop chartType=area --prop title="dataTable=true" \
  --prop dataTable=true --prop legend=bottom \
  --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"
```

**Features:** `chartareafill`, `plotFill`, `chartborder`, `plotborder`, `roundedcorners`, `gridlines=none`, `dataTable`

### Slide 8 — Presets and Per-Series Control

```bash
for p in minimal dark corporate; do
  officecli add charts-area.pptx /slide[8] --type chart \
    --prop chartType=area --prop preset=$p --prop title="preset=$p" \
    --prop legend=bottom --prop categories="Mon,Tue,Wed,Thu,Fri" \
    --prop data="Web:50,60,70,65,80;Mobile:30,35,42,48,55"
done

officecli add charts-area.pptx /slide[8] --type chart \
  --prop chartType=area --prop title="seriesN.* + chart-series Set" \
  --prop legend=bottom --prop categories="Mon,Tue,Wed,Thu,Fri" \
  --prop series1.name="Web" --prop series1.values="50,60,70,65,80" \
  --prop series1.color=4472C4 \
  --prop series2.name="Mobile" --prop series2.values="30,35,42,48,55" \
  --prop series2.color=ED7D31

officecli set charts-area.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="Renamed Web" --prop color=C00000
```

**Features:** `preset`, `series1.name/values/color`, `chart-series Set`

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **Chart types:** area, stackedArea, percentStackedArea, area3d | 1 |
| **view3d** | 1 |
| **title.font/size/color/bold** | 2 |
| **legend** positions, legendFont, legend.overlay | 2 |
| **autotitledeleted** | 2 |
| **dataLabels:** value/category/none + combined | 3 |
| **labelPos** (center) | 3 |
| **labelfont** | 3 |
| **axismin/max**, majorunit, axistitle/cattitle | 4 |
| **axisfont**, axisline, axisnumfmt | 4 |
| **gridlines**, minorGridlines, tickmarks | 4 |
| **labelrotation**, dispunits | 4 |
| **colors**, seriesoutline, gradient, seriesshadow | 5 |
| **gradients** (per-series), transparency | 5 |
| **referenceline**, errbars | 6 |
| **trendline** (linear/movingAvg) | 6 |
| **chartareafill**, plotFill, chartborder, plotborder | 7 |
| **roundedcorners**, gridlines=none, dataTable | 7 |
| **preset**, seriesN.*, chart-series Set | 8 |

## Inspect the Generated File

```bash
officecli query charts-area.pptx chart
officecli get charts-area.pptx "/slide[1]/chart[1]"
officecli get charts-area.pptx "/slide[5]/chart[1]"
officecli get charts-area.pptx "/slide[8]/chart[4]/series[1]"
```
