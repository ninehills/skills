# Stock Charts Showcase

This demo consists of three files that work together:

- **charts-stock.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-stock.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-stock.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-stock.py
# → charts-stock.pptx
```

Stock charts require series in a fixed order: for HLC provide three series (High, Low, Close); for OHLC provide four (Open, High, Low, Close).

## Chart Slides

### Slide 1 — Basic Stock (HLC and OHLC)

```bash
HLC="High:130,135,140,138,145;Low:118,122,128,125,132;Close:125,130,135,132,140"
OHLC="Open:120,128,130,135,138;High:130,135,140,138,145;Low:118,122,128,125,132;Close:125,130,135,132,140"
CATS="Mon,Tue,Wed,Thu,Fri"

# High-Low-Close (3-series)
officecli add charts-stock.pptx /slide[1] --type chart \
  --prop chartType=stock --prop title="HLC" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

# Open-High-Low-Close (4-series)
officecli add charts-stock.pptx /slide[1] --type chart \
  --prop chartType=stock --prop title="OHLC" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$OHLC" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

# HLC with data table
officecli add charts-stock.pptx /slide[1] --type chart \
  --prop chartType=stock --prop title="HLC + dataTable=true" \
  --prop dataTable=true --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

# OHLC with data table
officecli add charts-stock.pptx /slide[1] --type chart \
  --prop chartType=stock --prop title="OHLC + dataTable=true" \
  --prop dataTable=true --prop legend=bottom \
  --prop categories="$CATS" --prop data="$OHLC" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType=stock`, HLC (3-series) vs OHLC (4-series), `dataTable`

### Slide 2 — Hi-Low Lines and Up-Down Bars

```bash
# hilowlines — vertical lines connecting high and low of each period
officecli add charts-stock.pptx /slide[2] --type chart \
  --prop chartType=stock --prop title="hilowlines=true" \
  --prop hilowlines=true --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

# hilowlines with custom color and width
officecli add charts-stock.pptx /slide[2] --type chart \
  --prop chartType=stock --prop title="hilowlines=808080:0.5" \
  --prop hilowlines="808080:0.5" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

# updownbars — candlestick-like up/down fill bars (OHLC only)
officecli add charts-stock.pptx /slide[2] --type chart \
  --prop chartType=stock --prop title="updownbars=true (OHLC)" \
  --prop updownbars=true --prop legend=bottom \
  --prop categories="$CATS" --prop data="$OHLC"

# updownbars with custom width and colors
officecli add charts-stock.pptx /slide[2] --type chart \
  --prop chartType=stock --prop title="updownbars=150:00AA00:FF0000" \
  --prop updownbars="150:00AA00:FF0000" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$OHLC"
```

**Features:** `hilowlines` (true or color:width), `updownbars` (true or gapWidth:upColor:downColor — OHLC only)

### Slide 3 — Title and Legend

```bash
officecli add charts-stock.pptx /slide[3] --type chart \
  --prop chartType=stock --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[3] --type chart \
  --prop chartType=stock --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[3] --type chart \
  --prop chartType=stock --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[3] --type chart \
  --prop chartType=stock --prop autotitledeleted=true --prop legend=none \
  --prop categories="$CATS" --prop data="$HLC"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 4 — Data Labels

```bash
officecli add charts-stock.pptx /slide[4] --type chart \
  --prop chartType=stock --prop title="dataLabels=value" \
  --prop dataLabels=value --prop labelfont="9:333333:Calibri" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[4] --type chart \
  --prop chartType=stock --prop title="value,series" \
  --prop dataLabels="value,series" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[4] --type chart \
  --prop chartType=stock --prop title="value,category" \
  --prop dataLabels="value,category" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[4] --type chart \
  --prop chartType=stock --prop title="dataLabels=none" \
  --prop dataLabels=none --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"
```

**Features:** `dataLabels` (value/series/category/none or combined), `labelfont`

### Slide 5 — Axes

```bash
# axis min/max + currency number format
officecli add charts-stock.pptx /slide[5] --type chart \
  --prop chartType=stock --prop title="min/max + currency format" \
  --prop axismin=100 --prop axismax=160 --prop majorunit=10 \
  --prop axistitle="Price (USD)" --prop cattitle="Day" \
  --prop axisfont="10:333333:Calibri" --prop axisnumfmt='$#,##0.00' \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[5] --type chart \
  --prop chartType=stock --prop title="gridlines + minorGridlines" \
  --prop gridlines="E0E0E0:0.3" --prop minorGridlines="F0F0F0:0.25" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[5] --type chart \
  --prop chartType=stock --prop title="labelrotation=-30" \
  --prop labelrotation=-30 --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[5] --type chart \
  --prop chartType=stock --prop title="dispunits=hundreds" \
  --prop dispunits=hundreds --prop legend=bottom \
  --prop categories="$CATS" \
  --prop data="High:13000,13500,14000,13800,14500;Low:11800,12200,12800,12500,13200;Close:12500,13000,13500,13200,14000"
```

**Features:** `axismin/max`, `majorunit`, `axistitle/cattitle`, `axisfont`, `axisnumfmt` (currency), `gridlines/minorGridlines`, `labelrotation`, `dispunits`

### Slide 6 — Series Styling

```bash
officecli add charts-stock.pptx /slide[6] --type chart \
  --prop chartType=stock --prop title="colors" \
  --prop colors="4472C4,ED7D31,70AD47" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[6] --type chart \
  --prop chartType=stock --prop title="seriesoutline" \
  --prop seriesoutline="000000:1" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[6] --type chart \
  --prop chartType=stock --prop title="transparency=30" \
  --prop transparency=30 --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[6] --type chart \
  --prop chartType=stock --prop title="seriesshadow" \
  --prop seriesshadow="000000-5-45-3-50" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"
```

**Features:** `colors`, `seriesoutline`, `transparency`, `seriesshadow`

### Slide 7 — Backgrounds

```bash
officecli add charts-stock.pptx /slide[7] --type chart \
  --prop chartType=stock --prop title="chartareafill + plotFill + borders" \
  --prop chartareafill=FFF8E7 --prop plotFill=FAFAFA \
  --prop chartborder="000000:1" --prop plotborder="CCCCCC:0.5" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[7] --type chart \
  --prop chartType=stock --prop title="roundedcorners=true" \
  --prop roundedcorners=true --prop chartborder="4472C4:2" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[7] --type chart \
  --prop chartType=stock --prop title="plotFill=none" \
  --prop plotFill=none --prop gridlines=none \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli add charts-stock.pptx /slide[7] --type chart \
  --prop chartType=stock --prop title="chartareafill=none" \
  --prop chartareafill=none --prop legend=bottom \
  --prop categories="$CATS" --prop data="$HLC"
```

**Features:** `chartareafill`, `plotFill`, `chartborder`, `plotborder`, `roundedcorners`

### Slide 8 — Presets and Per-Series Set

```bash
for p in minimal dark corporate; do
  officecli add charts-stock.pptx /slide[8] --type chart \
    --prop chartType=stock --prop preset=$p --prop title="preset=$p" \
    --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"
done

officecli add charts-stock.pptx /slide[8] --type chart \
  --prop chartType=stock --prop title="chart-series Set name+color" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$HLC"

officecli set charts-stock.pptx "/slide[8]/chart[4]/series[1]" \
  --prop name="H" --prop color=00AA00
officecli set charts-stock.pptx "/slide[8]/chart[4]/series[2]" \
  --prop name="L" --prop color=C00000
officecli set charts-stock.pptx "/slide[8]/chart[4]/series[3]" \
  --prop name="C" --prop color=4472C4
```

**Features:** `preset` (minimal/dark/corporate), `chart-series Set` (name/color per series)

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **chartType=stock**, HLC vs OHLC series order | 1 |
| **dataTable** | 1 |
| **hilowlines** (true or color:width) | 2 |
| **updownbars** (true or gapWidth:upColor:downColor) | 2 |
| **title.font/size/color/bold** | 3 |
| **legend** positions, legendFont, legend.overlay | 3 |
| **autotitledeleted** | 3 |
| **dataLabels:** value/series/category/none | 4 |
| **labelfont** | 4 |
| **axismin/max**, majorunit, axistitle/cattitle | 5 |
| **axisfont**, axisnumfmt (currency), gridlines | 5 |
| **labelrotation**, dispunits | 5 |
| **colors**, seriesoutline, transparency, seriesshadow | 6 |
| **chartareafill, plotFill, chartborder, plotborder, roundedcorners** | 7 |
| **preset** | 8 |
| **chart-series Set** | 8 |

## Inspect the Generated File

```bash
officecli query charts-stock.pptx chart
officecli get charts-stock.pptx "/slide[1]/chart[1]"
officecli get charts-stock.pptx "/slide[2]/chart[3]"
officecli get charts-stock.pptx "/slide[8]/chart[4]/series[1]"
```
