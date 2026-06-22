# Advanced Charts Showcase

This demo consists of three files that work together:

- **charts-advanced.py** — Python script that calls `officecli` commands to generate the deck. It covers the "long-tail" chart properties not shown in the per-type decks.
- **charts-advanced.pptx** — The generated 8-slide deck.
- **charts-advanced.md** — This file. Maps each slide to the niche/advanced features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-advanced.py
# → charts-advanced.pptx
```

## Chart Slides

### Slide 1 — RTL and Anchor Variants

```bash
CATS="Q1,Q2,Q3,Q4"; D="A:60,90,140,180"

# Default LTR chart
officecli add charts-advanced.pptx /slide[1] --type chart \
  --prop chartType=column --prop title="default (LTR)" --prop legend=bottom \
  --prop categories="$CATS" --prop data="A:60,90,140,180;B:50,75,110,150" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

# RTL: Add first, then Set direction=rtl
officecli add charts-advanced.pptx /slide[1] --type chart \
  --prop chartType=column --prop title="direction=rtl (Set after Add)" \
  --prop legend=bottom \
  --prop categories="$CATS" --prop data="A:60,90,140,180;B:50,75,110,150" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in
officecli set charts-advanced.pptx "/slide[1]/chart[2]" --prop direction=rtl

# anchor= cm-form alternative to x/y/width/height
officecli add charts-advanced.pptx /slide[1] --type chart \
  --prop chartType=column --prop title="anchor=0.3cm,11cm,15.5cm,7cm" \
  --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D" \
  --prop anchor="0.3cm,11cm,15.5cm,7cm"
```

**Features:** `direction=rtl` (Set-only, reverses chart reading order), `anchor=x,y,w,h` (cm-form alternative to x=/y=/width=/height=)

### Slide 2 — Axis Visibility Shortcuts

```bash
# Hide both axes
officecli add charts-advanced.pptx /slide[2] --type chart \
  --prop chartType=column --prop title="axisvisible=false (both axes hidden)" \
  --prop legend=none --prop axisvisible=false \
  --prop categories="$CATS" --prop data="$D"

# Hide value (Y) axis only
officecli add charts-advanced.pptx /slide[2] --type chart \
  --prop chartType=column --prop title="valaxisvisible=false (Y hidden, X shown)" \
  --prop legend=none --prop valaxisvisible=false \
  --prop categories="$CATS" --prop data="$D"

# Hide category (X) axis only
officecli add charts-advanced.pptx /slide[2] --type chart \
  --prop chartType=column --prop title="catAxisVisible=false (X hidden)" \
  --prop legend=none --prop catAxisVisible=false \
  --prop categories="$CATS" --prop data="$D"

# Axis orientation reversal + axis position at top
officecli add charts-advanced.pptx /slide[2] --type chart \
  --prop chartType=column \
  --prop title="axisorientation=true (reversed) + axisposition=top" \
  --prop legend=none --prop axisorientation=true --prop axisposition=top \
  --prop cataxisline="333333:1" --prop valaxisline="333333:1" \
  --prop categories="$CATS" --prop data="$D"
```

**Features:** `axisvisible` (false = hide both), `valaxisvisible` (hide value/Y axis), `catAxisVisible` (hide category/X axis), `axisorientation` (true = reverse), `axisposition` (top/bottom/left/right), `cataxisline`/`valaxisline` (color:width)

### Slide 3 — Crossings

```bash
# Default crossBetween (bars straddle gridlines)
officecli add charts-advanced.pptx /slide[3] --type chart \
  --prop chartType=column --prop title="crossBetween=between (default)" \
  --prop legend=none --prop crossBetween=between \
  --prop categories="$CATS" --prop data="$D"

# midCat: bars centered on gridlines
officecli add charts-advanced.pptx /slide[3] --type chart \
  --prop chartType=column --prop title="crossBetween=midCat" \
  --prop legend=none --prop crossBetween=midCat \
  --prop categories="$CATS" --prop data="$D"

# Y axis crosses at the maximum value (bars grow down)
officecli add charts-advanced.pptx /slide[3] --type chart \
  --prop chartType=column --prop title="crosses=max (Y crosses at top)" \
  --prop legend=none --prop crosses=max \
  --prop categories="$CATS" --prop data="$D"

# X axis crosses at a specific Y value
officecli add charts-advanced.pptx /slide[3] --type chart \
  --prop chartType=column \
  --prop title="crossesAt=100 + crosses=autoZero" \
  --prop legend=none --prop crosses=autoZero --prop crossesAt=100 \
  --prop categories="$CATS" --prop data="A:60,-30,140,180"
```

**Features:** `crossBetween` (between/midCat), `crosses` (autoZero/max/min), `crossesAt` (numeric Y value where X axis crosses)

### Slide 4 — Category Axis Layout

```bash
# labeloffset controls label distance from axis (100 = default, 300 = farther)
officecli add charts-advanced.pptx /slide[4] --type chart \
  --prop chartType=column --prop title="labeloffset=100 (default)" \
  --prop labeloffset=100 --prop legend=none \
  --prop categories="January,February,March,April,May,June" \
  --prop data="A:60,90,140,180,160,210"

officecli add charts-advanced.pptx /slide[4] --type chart \
  --prop chartType=column --prop title="labeloffset=300 (push labels down)" \
  --prop labeloffset=300 --prop legend=none \
  --prop categories="January,February,March,April,May,June" \
  --prop data="A:60,90,140,180,160,210"

# ticklabelskip: show every Nth label (reduce clutter on dense axes)
officecli add charts-advanced.pptx /slide[4] --type chart \
  --prop chartType=column --prop title="ticklabelskip=2 (every other label)" \
  --prop ticklabelskip=2 --prop legend=none \
  --prop categories="Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sep,Oct,Nov,Dec" \
  --prop data="A:60,90,140,180,160,210,200,190,170,150,130,170"

officecli add charts-advanced.pptx /slide[4] --type chart \
  --prop chartType=column --prop title="ticklabelskip=3" \
  --prop ticklabelskip=3 --prop legend=none \
  --prop categories="Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sep,Oct,Nov,Dec" \
  --prop data="A:60,90,140,180,160,210,200,190,170,150,130,170"
```

**Features:** `labeloffset` (100–1000, label distance from axis), `ticklabelskip` (show every Nth label)

### Slide 5 — Marker Size, Area Fill, chartFill, plotvisonly

```bash
# markersize as standalone key (independent of marker= compound)
officecli add charts-advanced.pptx /slide[5] --type chart \
  --prop chartType=line --prop title="markersize=12 (standalone key)" \
  --prop showMarker=true --prop markersize=12 --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

# areafill: gradient fill applied to every series shape
officecli add charts-advanced.pptx /slide[5] --type chart \
  --prop chartType=column --prop title="areafill (gradient on series)" \
  --prop areafill="4472C4-A5C8FF:90" --prop legend=none \
  --prop categories="$CATS" --prop data="A:60,90,140,180;B:50,75,110,150"

# chartFill: chart-level background fill (vs chartareafill which targets the plot frame)
officecli add charts-advanced.pptx /slide[5] --type chart \
  --prop chartType=column --prop title="chartFill=#FFF8E7 (chart-level fill)" \
  --prop chartFill="#FFF8E7" --prop legend=none \
  --prop categories="$CATS" --prop data="$D"

# plotvisonly: skip hidden rows when bound to a sheet
officecli add charts-advanced.pptx /slide[5] --type chart \
  --prop chartType=column --prop title="plotvisonly=true" \
  --prop plotvisonly=true --prop legend=none \
  --prop categories="$CATS" --prop data="$D"
```

**Features:** `markersize` (standalone, independent of `marker=`), `areafill` (gradient fill on all series), `chartFill` (chart-level fill), `plotvisonly` (skip hidden rows)

### Slide 6 — Style, dispBlanksAs, dataRange

```bash
# Built-in chart style presets (1–48)
officecli add charts-advanced.pptx /slide[6] --type chart \
  --prop chartType=column --prop style=2 --prop title="style=2" \
  --prop legend=bottom --prop categories="$CATS" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

officecli add charts-advanced.pptx /slide[6] --type chart \
  --prop chartType=column --prop style=42 --prop title="style=42" \
  --prop legend=bottom --prop categories="$CATS" \
  --prop data="A:60,90,140,180;B:50,75,110,150"

# dispBlanksAs: how to handle blank/null data points
# Set-only (Add first, then Set)
officecli add charts-advanced.pptx /slide[6] --type chart \
  --prop chartType=line --prop title="dispBlanksAs=gap (Set after Add)" \
  --prop showMarker=true --prop legend=bottom \
  --prop categories="$CATS" --prop data="A:60,90,140,180"
officecli set charts-advanced.pptx "/slide[6]/chart[3]" --prop dispBlanksAs=gap

# dataRange: sheet-range alternative to data= for workbook-backed sources
officecli add charts-advanced.pptx /slide[6] --type chart \
  --prop chartType=column --prop title="dataRange syntax demo (fallback inline)" \
  --prop dataRange="Sheet1!A1:D5" --prop legend=bottom --prop catTitle="Quarter" \
  --prop categories="$CATS" --prop data="A:60,90,140,180;B:50,75,110,150"
```

**Features:** `style` (1–48 built-in style presets), `dispBlanksAs` (gap/zero/span — Set-only), `dataRange` (sheet range syntax), `catTitle`

### Slide 7 — chart-axis Set (per-axis post-Add mutations)

```bash
# Set dispUnits + format + minorUnit + labelRotation on value axis
officecli add charts-advanced.pptx /slide[7] --type chart \
  --prop chartType=column --prop title="after: dispUnits=thousands" \
  --prop legend=none --prop categories="$CATS" \
  --prop data="Rev:120000,135000,148000,162000"
officecli set charts-advanced.pptx "/slide[7]/chart[1]/axis[@role=value]" \
  --prop dispUnits=thousands --prop format="#,##0" \
  --prop minorUnit=10000 --prop labelRotation=0 --prop visible=true

# Set logBase + min/max + majorGridlines on value axis
officecli add charts-advanced.pptx /slide[7] --type chart \
  --prop chartType=line --prop title="after: logBase=10" \
  --prop legend=none --prop categories="$CATS" \
  --prop data="A:5,50,500,5000"
officecli set charts-advanced.pptx "/slide[7]/chart[2]/axis[@role=value]" \
  --prop logBase=10 --prop min=1 --prop max=10000 --prop majorGridlines=true

# Set visible=false on value axis
officecli add charts-advanced.pptx /slide[7] --type chart \
  --prop chartType=column --prop title="after: visible=false on value axis" \
  --prop legend=none --prop categories="$CATS" --prop data="$D"
officecli set charts-advanced.pptx "/slide[7]/chart[3]/axis[@role=value]" \
  --prop visible=false

# Set labelRotation=-45 + title on category axis
officecli add charts-advanced.pptx /slide[7] --type chart \
  --prop chartType=column --prop title="after: labelRotation=-45 on category axis" \
  --prop legend=none --prop categories="January,February,March,April" --prop data="$D"
officecli set charts-advanced.pptx "/slide[7]/chart[4]/axis[@role=category]" \
  --prop labelRotation=-45 --prop title="Month" --prop visible=true
```

**Features (chart-axis Set):** `dispUnits`, `format`, `minorUnit`, `labelRotation`, `visible`, `logBase`, `min`, `max`, `majorGridlines`, `title`; axis selector: `axis[@role=value]` / `axis[@role=category]`

### Slide 8 — chart-series and chart-axis Get Readback

```bash
# Add a chart, mutate a series, then get --json readback
officecli add charts-advanced.pptx /slide[8] --type chart \
  --prop chartType=column --prop title="before: A=60,90,140,180" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D"

# Mutate values after add
officecli set charts-advanced.pptx "/slide[8]/chart[1]/series[1]" \
  --prop values="200,150,100,80"

# Rename + recolor, then get JSON readback
officecli set charts-advanced.pptx "/slide[8]/chart[1]/series[1]" \
  --prop name="Readback Demo" --prop color=C00000

# Get series JSON (readback fields: alpha, outlineColor, scatterStyle, etc.)
officecli get charts-advanced.pptx "/slide[8]/chart[1]/series[1]" --json

# Set axis properties, then get axis JSON readback
officecli set charts-advanced.pptx "/slide[8]/chart[1]/axis[@role=value]" \
  --prop title="Readback Y" --prop format='$#,##0' \
  --prop min=0 --prop max=300 --prop majorUnit=75

# Get axis JSON (readback fields: axisFont, axisMax, axisMin, axisNumFmt, etc.)
officecli get charts-advanced.pptx "/slide[8]/chart[1]/axis[@role=value]" --json
```

**Features:** `chart-series Set values=` (mutate data after creation), `get --json` for series + axis readback fields

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **direction=rtl** (Set-only) | 1 |
| **anchor=x,y,w,h** (cm-form) | 1 |
| **axisvisible, valaxisvisible, catAxisVisible** | 2 |
| **axisorientation** (reversed), **axisposition** | 2 |
| **cataxisline, valaxisline** | 2 |
| **crossBetween** (between/midCat) | 3 |
| **crosses** (autoZero/max/min), **crossesAt** | 3 |
| **labeloffset** | 4 |
| **ticklabelskip** | 4 |
| **markersize** (standalone key) | 5 |
| **areafill** (gradient on series) | 5 |
| **chartFill** (chart-level) | 5 |
| **plotvisonly** | 5 |
| **style** (1–48 preset) | 6 |
| **dispBlanksAs** (gap/zero/span — Set-only) | 6 |
| **dataRange** (sheet-range syntax) | 6 |
| **catTitle** | 6 |
| **chart-axis Set:** dispUnits, logBase, minorUnit, labelRotation, visible, min, max, majorGridlines, title | 7 |
| **chart-series Set values=** (mutate data) | 8 |
| **get --json** series + axis readback fields | 8 |

## Get-Only Readback Fields (surface in `get --json`)

| Element | Get-only properties |
|---|---|
| chart | `id` |
| chart-axis | `axisFont`, `axisMax`, `axisMin`, `axisNumFmt`, `axisOrientation`, `axisTitle`, `labelOffset`, `tickLabelSkip` |
| chart-series | `alpha`, `categoriesRef`, `dataLabels.numFmt`, `dataLabels.separator`, `errBars`, `invertIfNeg`, `nameRef`, `outlineColor`, `scatterStyle`, `secondaryAxis` |

These fields cannot be Set as input — they surface in the JSON readback shapes on slide 8.

## Inspect the Generated File

```bash
officecli query charts-advanced.pptx chart
officecli get charts-advanced.pptx "/slide[1]/chart[2]"
officecli get charts-advanced.pptx "/slide[7]/chart[1]/axis[@role=value]"
officecli get charts-advanced.pptx "/slide[8]/chart[1]/series[1]" --json
```
