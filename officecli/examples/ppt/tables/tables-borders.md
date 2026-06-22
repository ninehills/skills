# PPT Table Borders

This demo consists of three files that work together:

- **tables-borders.sh** — Shell script that calls `officecli` commands to generate the deck.
- **tables-borders.pptx** — The generated 3-slide deck (shorthand + per-edge borders, inside dividers + dash patterns, diagonal borders).
- **tables-borders.md** — This file. Maps each slide to the features it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash tables/tables-borders.sh
# → tables/tables-borders.pptx
```

## Slides

### Slide 1 — Border Shorthand and Per-Edge Borders

Seven small tables demonstrating `border.all` shorthand and each of the four per-edge properties.

```bash
officecli create tables-borders.pptx
officecli open tables-borders.pptx
officecli add tables-borders.pptx / --type slide

DATA="A,B,C;1,2,3;4,5,6;7,8,9"

# border.all shorthand — applies to every cell edge in the table
# Format: "Npt dash HEX"  (width, dash pattern, color)
officecli add tables-borders.pptx '/slide[1]' --type table \
  --prop x=0.5in --prop y=1.35in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.all="1pt solid 808080"

officecli add tables-borders.pptx '/slide[1]' --type table \
  --prop x=5in --prop y=1.35in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.all="2pt solid FF0000"

# border.all=none — explicitly removes all borders
officecli add tables-borders.pptx '/slide[1]' --type table \
  --prop x=9.5in --prop y=1.35in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.all=none

# Per-edge borders — each targets one side of every cell in the table
officecli add tables-borders.pptx '/slide[1]' --type table \
  --prop x=0.5in --prop y=3.85in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.top="3pt solid 000000"

officecli add tables-borders.pptx '/slide[1]' --type table \
  --prop x=5in --prop y=3.85in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.bottom="3pt solid 0070C0"

officecli add tables-borders.pptx '/slide[1]' --type table \
  --prop x=9.5in --prop y=3.85in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.left="3pt solid 00B050"

officecli add tables-borders.pptx '/slide[1]' --type table \
  --prop x=0.5in --prop y=6.15in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.right="3pt solid C00000"
```

**Features:** `border.all` (compound `Npt dash HEX` shorthand — applies to all cell edges), `border.all=none` (remove all borders), `border.top`, `border.bottom`, `border.left`, `border.right` (per-side outer border, same compound syntax)

---

### Slide 2 — Inside Dividers and Dash Patterns

Three tables showing inner horizontal/vertical dividers, and three tables cycling through dash pattern names.

```bash
officecli add tables-borders.pptx / --type slide

# border.horizontal — divider lines between rows (inside the table body)
officecli add tables-borders.pptx '/slide[2]' --type table \
  --prop x=0.5in --prop y=1.35in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.horizontal="1pt solid CCCCCC" --prop border.all="1pt solid 404040"

# border.vertical — divider lines between columns
officecli add tables-borders.pptx '/slide[2]' --type table \
  --prop x=5in --prop y=1.35in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.vertical="1pt dash 0070C0" --prop border.all="1pt solid 404040"

# horizontal + vertical together (both inside dividers as dots)
officecli add tables-borders.pptx '/slide[2]' --type table \
  --prop x=9.5in --prop y=1.35in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.horizontal="1pt dot 808080" --prop border.vertical="1pt dot 808080" \
  --prop border.all="2pt solid 000000"

# Dash pattern showcase (border.all)
officecli add tables-borders.pptx '/slide[2]' --type table \
  --prop x=0.5in --prop y=3.85in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.all="1.5pt lgDash FF0000"

officecli add tables-borders.pptx '/slide[2]' --type table \
  --prop x=5in --prop y=3.85in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.all="1.5pt dashDot 0070C0"

officecli add tables-borders.pptx '/slide[2]' --type table \
  --prop x=9.5in --prop y=3.85in --prop width=3.5in --prop height=1.8in \
  --prop style=none --prop data="$DATA" \
  --prop border.all="1.5pt sysDash 00B050"
```

**Features:** `border.horizontal` (inside row dividers), `border.vertical` (inside column dividers), dash patterns: solid, dot, dash, lgDash, dashDot, sysDot, sysDash

---

### Slide 3 — Diagonal Borders (per-cell tl2br / tr2bl)

Diagonal borders are set on individual cells, not on the table. The classic use is a "crossed-out" corner cell that labels both the row and column axes.

```bash
officecli add tables-borders.pptx / --type slide

# Main cross-reference table — top-left corner cell gets a diagonal split
officecli add tables-borders.pptx '/slide[3]' --type table \
  --prop x=2in --prop y=1.6in --prop width=9in --prop height=3in \
  --prop rows=4 --prop cols=4 --prop border.all="1pt solid 808080"

# tl2br — diagonal line from top-left to bottom-right (splits the corner cell)
officecli set tables-borders.pptx '/slide[3]/table[1]/tr[1]/tc[1]' \
  --prop text="" --prop fill=F2F2F2 \
  --prop border.tl2br="1pt solid 808080"

# Column and row headers
officecli set tables-borders.pptx '/slide[3]/table[1]/tr[1]/tc[2]' \
  --prop text="Jan" --prop bold=true --prop align=center --prop fill=DEEAF6
officecli set tables-borders.pptx '/slide[3]/table[1]/tr[1]/tc[3]' \
  --prop text="Feb" --prop bold=true --prop align=center --prop fill=DEEAF6
officecli set tables-borders.pptx '/slide[3]/table[1]/tr[1]/tc[4]' \
  --prop text="Mar" --prop bold=true --prop align=center --prop fill=DEEAF6
officecli set tables-borders.pptx '/slide[3]/table[1]/tr[2]/tc[1]' \
  --prop text="North" --prop bold=true --prop fill=DEEAF6
officecli set tables-borders.pptx '/slide[3]/table[1]/tr[2]/tc[2]' --prop text="120"
# … fill remaining body cells

# Standalone cell with both diagonals (X / crossed-out pattern)
officecli add tables-borders.pptx '/slide[3]' --type table \
  --prop x=5in --prop y=5.7in --prop width=3in --prop height=1.2in \
  --prop rows=1 --prop cols=1 --prop border.all="1pt solid 000000"
officecli set tables-borders.pptx '/slide[3]/table[2]/tr[1]/tc[1]' \
  --prop text="N/A" --prop align=center --prop fill=F2F2F2 \
  --prop border.tl2br="1pt solid C00000" \
  --prop border.tr2bl="1pt solid C00000"

officecli close tables-borders.pptx
officecli validate tables-borders.pptx
```

**Features:** `border.tl2br` (diagonal from top-left to bottom-right; per-cell), `border.tr2bl` (diagonal from top-right to bottom-left; per-cell; combine with tl2br for X pattern)

> Diagonal borders are cell-level properties set via `officecli set /table[N]/tr[R]/tc[C]`, not table-level properties. They are independent of the table's `border.all` shorthand.

---

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **border.all:** shorthand — applies to all cell edges (`Npt dash HEX`) | 1, 2 |
| **border.all=none:** remove all borders | 1 |
| **border.top:** outer top border | 1 |
| **border.bottom:** outer bottom border | 1 |
| **border.left:** outer left border | 1 |
| **border.right:** outer right border | 1 |
| **border.horizontal:** inner row dividers | 2 |
| **border.vertical:** inner column dividers | 2 |
| **Dash patterns:** solid, dot, dash, lgDash, dashDot, sysDot, sysDash | 2 |
| **border.tl2br:** diagonal top-left to bottom-right (per-cell) | 3 |
| **border.tr2bl:** diagonal top-right to bottom-left (per-cell) | 3 |
| **Crossed-out cell:** tl2br + tr2bl combined for N/A pattern | 3 |

## Inspect the Generated File

```bash
# List all tables on slide 1
officecli query tables-borders.pptx '/slide[1]' table

# Get border properties on the first table
officecli get tables-borders.pptx '/slide[1]/table[1]'

# Inspect inner divider table on slide 2
officecli get tables-borders.pptx '/slide[2]/table[1]'
officecli get tables-borders.pptx '/slide[2]/table[2]'

# Check diagonal border on the corner cell (slide 3)
officecli get tables-borders.pptx '/slide[3]/table[1]/tr[1]/tc[1]'

# Inspect the X-pattern cell on slide 3
officecli get tables-borders.pptx '/slide[3]/table[2]/tr[1]/tc[1]'
```
