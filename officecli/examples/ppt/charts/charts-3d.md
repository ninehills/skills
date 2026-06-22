# 3D Charts Showcase

This demo consists of three files that work together:

- **charts-3d.py** — Python script that calls `officecli` commands to generate the deck.
- **charts-3d.pptx** — The generated 8-slide deck (4 charts per slide, 32 charts total).
- **charts-3d.md** — This file. Maps each slide to the 3D chart features it demonstrates.

## Regenerate

```bash
cd examples/ppt/charts
python3 charts-3d.py
# → charts-3d.pptx
```

## Chart Slides

### Slide 1 — 3D Families

```bash
CATS="Q1,Q2,Q3,Q4"
D2="East:120,135,148,162;West:95,108,115,128"

officecli add charts-3d.pptx /slide[1] --type chart \
  --prop chartType=column3d --prop title="column3d" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=0.3in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-3d.pptx /slide[1] --type chart \
  --prop chartType=bar3d --prop title="bar3d" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=6.95in --prop y=1.05in --prop width=6.1in --prop height=3in

officecli add charts-3d.pptx /slide[1] --type chart \
  --prop chartType=pie3d --prop title="pie3d" --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17" \
  --prop x=0.3in --prop y=4.25in --prop width=6.1in --prop height=3in

officecli add charts-3d.pptx /slide[1] --type chart \
  --prop chartType=line3d --prop title="line3d" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2" \
  --prop x=6.95in --prop y=4.25in --prop width=6.1in --prop height=3in
```

**Features:** `chartType` (column3d/bar3d/pie3d/line3d)

### Slide 2 — area3d and Stacked 3D

```bash
D3="East:120,135,148,162;South:95,108,115,128;West:80,90,98,110"

officecli add charts-3d.pptx /slide[2] --type chart \
  --prop chartType=area3d --prop title="area3d" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-3d.pptx /slide[2] --type chart \
  --prop chartType=stackedColumn3d --prop title="stackedColumn3d" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D3"

officecli add charts-3d.pptx /slide[2] --type chart \
  --prop chartType=percentStackedColumn3d --prop title="percentStackedColumn3d" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D3"

officecli add charts-3d.pptx /slide[2] --type chart \
  --prop chartType=stackedBar3d --prop title="stackedBar3d" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D3"
```

**Features:** `chartType` (area3d/stackedColumn3d/percentStackedColumn3d/stackedBar3d)

### Slide 3 — view3d Angles

`view3d` accepts up to three comma-separated values: `rotX,rotY,perspective`.

```bash
# Low elevation, slight rotation
officecli add charts-3d.pptx /slide[3] --type chart \
  --prop chartType=column3d --prop title="view3d=15,20,30" \
  --prop view3d="15,20,30" --prop legend=none \
  --prop categories="$CATS" --prop data="$D2"

# Higher elevation + wider rotation
officecli add charts-3d.pptx /slide[3] --type chart \
  --prop chartType=column3d --prop title="view3d=30,40,15" \
  --prop view3d="30,40,15" --prop legend=none \
  --prop categories="$CATS" --prop data="$D2"

# Single value: sets rotX only
officecli add charts-3d.pptx /slide[3] --type chart \
  --prop chartType=column3d --prop title="view3d=20 (rotX only)" \
  --prop view3d="20" --prop legend=none \
  --prop categories="$CATS" --prop data="$D2"

# Pie 3D with all three parameters
officecli add charts-3d.pptx /slide[3] --type chart \
  --prop chartType=pie3d --prop title="pie3d view3d=40,30,30" \
  --prop view3d="40,30,30" --prop legend=right \
  --prop categories="North,South,East,West" --prop data="Share:30,25,28,17"
```

**Features:** `view3d` (rotX,rotY,perspective — one, two, or three values)

### Slide 4 — gapdepth

Controls the depth spacing between bars/columns in the Z direction (0–500).

```bash
for g in 0 50 150 300; do
  officecli add charts-3d.pptx /slide[4] --type chart \
    --prop chartType=column3d --prop title="gapdepth=$g" \
    --prop gapdepth=$g --prop legend=none \
    --prop categories="$CATS" --prop data="$D2"
done
```

**Features:** `gapdepth` (0–500, 3D depth spacing between series groups)

### Slide 5 — 3D Bar Shapes

```bash
for s in box cylinder cone pyramid; do
  officecli add charts-3d.pptx /slide[5] --type chart \
    --prop chartType=bar3d --prop shape=$s --prop title="shape=$s" \
    --prop legend=none --prop categories="$CATS" --prop data="$D2"
done
```

**Features:** `shape` (box/cylinder/cone/pyramid) for bar3d and column3d

### Slide 6 — Title and Legend

```bash
officecli add charts-3d.pptx /slide[6] --type chart \
  --prop chartType=column3d --prop title="Styled title" \
  --prop title.font=Georgia --prop title.size=20 \
  --prop title.color=4472C4 --prop title.bold=true \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-3d.pptx /slide[6] --type chart \
  --prop chartType=column3d --prop title="legend=top + legendFont" \
  --prop legend=top --prop legendFont="10:333333:Calibri" \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-3d.pptx /slide[6] --type chart \
  --prop chartType=column3d --prop title="legend.overlay=true" \
  --prop legend=topRight --prop legend.overlay=true \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-3d.pptx /slide[6] --type chart \
  --prop chartType=column3d --prop autotitledeleted=true --prop legend=none \
  --prop categories="$CATS" --prop data="$D2"
```

**Features:** `title.font/size/color/bold`, `legend` positions, `legendFont`, `legend.overlay`, `autotitledeleted`

### Slide 7 — Series Styling

```bash
officecli add charts-3d.pptx /slide[7] --type chart \
  --prop chartType=column3d --prop title="colors + seriesoutline" \
  --prop colors="4472C4,ED7D31" --prop seriesoutline="000000:0.5" \
  --prop legend=bottom --prop categories="$CATS" --prop data="$D2"

officecli add charts-3d.pptx /slide[7] --type chart \
  --prop chartType=column3d --prop title="gradient + seriesshadow" \
  --prop gradient="FF6600-FFCC00" --prop seriesshadow="000000-5-45-3-50" \
  --prop legend=none --prop categories="$CATS" --prop data="$D2"

officecli add charts-3d.pptx /slide[7] --type chart \
  --prop chartType=column3d --prop title="transparency=30" \
  --prop transparency=30 --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2"

officecli add charts-3d.pptx /slide[7] --type chart \
  --prop chartType=column3d --prop title="per-series gradients" \
  --prop gradients="FF0000-0000FF;00FF00-FFFF00" --prop legend=bottom \
  --prop categories="$CATS" --prop data="$D2"
```

**Features:** `colors`, `seriesoutline`, `gradient`, `seriesshadow`, `transparency`, `gradients`

### Slide 8 — Presets

```bash
for p in minimal dark corporate colorful; do
  officecli add charts-3d.pptx /slide[8] --type chart \
    --prop chartType=column3d --prop preset=$p --prop title="preset=$p" \
    --prop view3d="15,20,30" --prop legend=bottom \
    --prop categories="$CATS" --prop data="$D2"
done
```

**Features:** `preset` (minimal/dark/corporate/colorful) applied to 3D chart type

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **3D chart types:** column3d, bar3d, pie3d, line3d | 1 |
| **3D chart types:** area3d, stackedColumn3d, percentStackedColumn3d, stackedBar3d | 2 |
| **view3d** (rotX,rotY,perspective) | 1, 3 |
| **gapdepth** (0–500) | 4 |
| **shape** (box/cylinder/cone/pyramid) — bar3d/column3d | 5 |
| **title.font/size/color/bold** | 6 |
| **legend** positions, legendFont, legend.overlay | 6 |
| **autotitledeleted** | 6 |
| **colors**, seriesoutline, gradient, seriesshadow | 7 |
| **transparency**, gradients | 7 |
| **preset** (minimal/dark/corporate/colorful) | 8 |

## Inspect the Generated File

```bash
officecli query charts-3d.pptx chart
officecli get charts-3d.pptx "/slide[1]/chart[1]"
officecli get charts-3d.pptx "/slide[3]/chart[1]"
officecli get charts-3d.pptx "/slide[4]/chart[2]"
```
