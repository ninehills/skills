#!/bin/bash
# tables-nested.sh — BUILD, NAVIGATE, and FULLY EXERCISE a nested pptx element:
# the table tree (slide → table → tr → tc). 4 slides, so the full property
# surface of each level fits without cramming one table:
#
#   Slide 1  Structure & ownership   — the teaching table: levels, path tokens,
#                                       property ownership, colspan, navigation.
#   Slide 2  Table-level surface     — every `table` property (banding, fills,
#                                       per-side borders, sizing, name/zorder/id, data).
#   Slide 3  Cell box surface        — every `tc` box property (all borders incl.
#                                       diagonals, padding, valign, wrap, textdir,
#                                       direction, bevel, opacity, image fill, merge).
#   Slide 4  Cell text surface       — every `tc` text property (font, size, weight,
#                                       underline/strike, color, align, line/para spacing).
#
# Coverage target: 100% of the settable props on pptx table / table-row /
# table-cell (verify with `help pptx <element> --json`). The two lessons a flat
# example can't teach are still front-and-centre on slide 1:
#   1. path token ≠ element name:  table-row → tr,  table-cell → tc
#      → a cell is /slide[N]/table[M]/tr[R]/tc[C]
#   2. property ownership: table owns style/banding/structure; tr owns height;
#      tc owns the cell box AND the cell text (pptx flattens text onto the cell).
#
# SDK twin: tables-nested.py.
set -e

FILE="$(dirname "$0")/tables-nested.pptx"
echo "Building $FILE ..."
rm -f "$FILE"
officecli create "$FILE"
officecli open "$FILE"

# A 1x1 PNG for the cell image-fill demo (slide 3). image= needs a real file, not
# a data-URI; generate one in a temp path so the example stays self-contained.
IMG="$(dirname "$0")/.cell-dot.png"
python3 -c "import base64,sys; open(sys.argv[1],'wb').write(base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=='))" "$IMG"

title() {  # title <slide> <text>
  officecli add "$FILE" "/slide[$1]" --type shape --prop geometry=rect \
    --prop x=1.2cm --prop y=0.6cm --prop width=30cm --prop height=1.3cm \
    --prop fill=none --prop line=none --prop text="$2" --prop size=20 --prop bold=true --prop color=1F4E79
}
# cell <slide> <tr/tc path> <--prop ...>  — full --prop tokens forwarded verbatim.
cell() { local sl="$1" p="$2"; shift 2; officecli set "$FILE" "/slide[$sl]/table[1]/$p" "$@"; }

# ════════════════════════════ SLIDE 1 — Structure & ownership ════════════════════════════
officecli add "$FILE" / --type slide
title 1 "1 · Structure & ownership  (slide → table → tr → tc)"
# Level 1: the table (returns /slide[1]/table[1]).
# rows/cols/colWidths are add-time structure; style/banding are settable later.
officecli add "$FILE" "/slide[1]" --type table --prop rows=5 --prop cols=3 \
  --prop x=2.5cm --prop y=2.4cm --prop width=28cm --prop height=9cm --prop colWidths=12cm,8cm,8cm
officecli set "$FILE" "/slide[1]/table[1]" --prop style=medium2-accent1 \
  --prop firstRow=true --prop bandedRows=true   # ← table owns style + banding
# Level 2: a row owns only its height.
officecli set "$FILE" "/slide[1]/table[1]/tr[1]" --prop height=2cm
# Level 3: a cell owns box + text together.
cell 1 "tr[1]/tc[1]" --prop text="Region"  --prop bold=true --prop color=FFFFFF --prop align=center --prop valign=middle --prop fill=1F6FEB
cell 1 "tr[1]/tc[2]" --prop text="Units"   --prop bold=true --prop color=FFFFFF --prop align=center --prop valign=middle --prop fill=1F6FEB
cell 1 "tr[1]/tc[3]" --prop text="Revenue" --prop bold=true --prop color=FFFFFF --prop align=center --prop valign=middle --prop fill=1F6FEB
d1() { cell 1 "tr[$1]/tc[1]" --prop text="$2" --prop align=left  --prop valign=middle
       cell 1 "tr[$1]/tc[2]" --prop text="$3" --prop align=right --prop valign=middle
       cell 1 "tr[$1]/tc[3]" --prop text="$4" --prop align=right --prop valign=middle; }
d1 2 "North" "1,240" "\$11,780"; d1 3 "South" "980" "\$9,310"; d1 4 "East" "1,520" "\$14,440"
# Nesting-only op: colspan (alias gridspan). Total row spans all 3 columns.
cell 1 "tr[5]/tc[1]" --prop colspan=3 --prop valign=middle --prop bold=true --prop align=center \
  --prop text="TOTAL    3,740 units    \$35,530" --prop fill=DDEBF7
# Navigate: address a deep node AFTER building — same path that built it reaches it.
echo "--- deep readback ---"; officecli get "$FILE" "/slide[1]/table[1]/tr[4]/tc[3]"
cell 1 "tr[4]/tc[3]" --prop fill=FFF2CC --prop bold=true

# ════════════════════════════ SLIDE 2 — Table-level full surface ════════════════════════════
officecli add "$FILE" / --type slide
title 2 "2 · Table level — banding · fills · per-side borders · sizing"
# Add-time props (data defines the grid; zorder/rowHeight/colWidths/header+body
# fills are add-only) go on the ADD; banding flags + name are settable later.
# NOTE: `id` is intentionally NOT set — table ids are auto-assigned and must stay
# unique, so hardcoding one risks collisions. (It's settable for round-trip
# fidelity, but never something to set by hand.)
officecli add "$FILE" "/slide[2]" --type table \
  --prop x=2cm --prop y=2.4cm --prop width=29cm --prop height=9cm \
  --prop data="Q,FY24,FY25,Growth;Q1,120,138,+15%;Q2,95,121,+27%;Q3,140,162,+16%" \
  --prop zorder=2 --prop rowHeight=1.8cm --prop colWidths=8cm,7cm,7cm,7cm \
  --prop headerFill=1F6FEB --prop bodyFill=EEF3FB
officecli set "$FILE" "/slide[2]/table[1]" \
  --prop name=QuarterlySales \
  --prop firstRow=true --prop lastRow=true --prop firstCol=true --prop lastCol=false \
  --prop bandedRows=true --prop bandedCols=false
# Every table-level border edge (outer 4 + inner horizontal/vertical):
officecli set "$FILE" "/slide[2]/table[1]" \
  --prop border.all="1pt solid B7C7E0" \
  --prop border.top="3pt solid 1F4E79" --prop border.bottom="3pt solid 1F4E79" \
  --prop border.left="1.5pt solid 1F6FEB" --prop border.right="1.5pt solid 1F6FEB" \
  --prop border.horizontal="1pt solid CCD8EC" --prop border.vertical="1pt solid CCD8EC"

# ════════════════════════════ SLIDE 3 — Cell box full surface ════════════════════════════
officecli add "$FILE" / --type slide
title 3 "3 · Cell box — borders · padding · valign · direction · bevel · opacity · image · merge"
officecli add "$FILE" "/slide[3]" --type table --prop rows=5 --prop cols=4 \
  --prop x=2cm --prop y=2.4cm --prop width=29cm --prop height=12cm --prop style=none
# Row 1 — per-side, full, and diagonal borders (one kind per cell so each renders distinctly)
cell 3 "tr[1]/tc[1]" --prop text="border.all"  --prop border.all="1.5pt solid 1F6FEB"
cell 3 "tr[1]/tc[2]" --prop text="top+bottom"  --prop border.top="3pt solid C00000" --prop border.bottom="3pt solid C00000"
cell 3 "tr[1]/tc[3]" --prop text="left+right"  --prop border.left="3pt solid 2DA44E" --prop border.right="3pt solid 2DA44E"
cell 3 "tr[1]/tc[4]" --prop text="diagonals"   --prop border.tl2br="1.5pt solid BF8700" --prop border.tr2bl="1.5pt solid BF8700"
# Row 2 — fill, opacity, bevel, image fill
cell 3 "tr[2]/tc[1]" --prop text="fill"        --prop fill=FFE699
cell 3 "tr[2]/tc[2]" --prop text="opacity=0.5" --prop fill=1F6FEB --prop opacity=0.5
cell 3 "tr[2]/tc[3]" --prop text="bevel=circle" --prop fill=DDEBF7 --prop bevel=circle
cell 3 "tr[2]/tc[4]" --prop text="image fill"  --prop image="$IMG"
# Row 3 — padding, padding.bottom, valign (top + bottom)
cell 3 "tr[3]/tc[1]" --prop text="padding=0.4cm" --prop padding=0.4cm --prop fill=F2F2F2
cell 3 "tr[3]/tc[2]" --prop text="padding.bottom=0.5cm" --prop padding.bottom=0.5cm --prop fill=F2F2F2
cell 3 "tr[3]/tc[3]" --prop text="valign=top" --prop valign=top --prop fill=F2F2F2
cell 3 "tr[3]/tc[4]" --prop text="valign=bottom" --prop valign=bottom --prop fill=F2F2F2
# Row 4 — wrap, vertical text, RTL direction, and merge.down (eats the cell below it)
cell 3 "tr[4]/tc[1]" --prop text="wrap=false: this long line will not wrap inside the cell" --prop wrap=false --prop fill=E2EFDA
cell 3 "tr[4]/tc[2]" --prop text="textdir=vertical270" --prop textdirection=vertical270 --prop fill=E2EFDA
cell 3 "tr[4]/tc[3]" --prop text="direction=rtl العربية" --prop direction=rtl --prop fill=E2EFDA
cell 3 "tr[4]/tc[4]" --prop text="merge.down=1 ↓" --prop merge.down=1 --prop align=center --prop fill=FCE4D6
# Row 5 — merge.right (eats the cell to its right). tc[4] is swallowed by the merge.down above.
cell 3 "tr[5]/tc[1]" --prop text="merge.right=2 →" --prop merge.right=2 --prop align=center --prop fill=FCE4D6

# ════════════════════════════ SLIDE 4 — Cell text full surface ════════════════════════════
officecli add "$FILE" / --type slide
title 4 "4 · Cell text — font · size · weight · underline/strike · color · align · spacing"
officecli add "$FILE" "/slide[4]" --type table --prop rows=4 --prop cols=3 \
  --prop x=2cm --prop y=2.4cm --prop width=29cm --prop height=10cm --prop style=light1
cell 4 "tr[1]/tc[1]" --prop text="font=Georgia" --prop font=Georgia
cell 4 "tr[1]/tc[2]" --prop text="size=20pt"    --prop size=20pt
cell 4 "tr[1]/tc[3]" --prop text="color"        --prop color=C00000
cell 4 "tr[2]/tc[1]" --prop text="bold"         --prop bold=true
cell 4 "tr[2]/tc[2]" --prop text="italic"       --prop italic=true
cell 4 "tr[2]/tc[3]" --prop text="underline"    --prop underline=double
cell 4 "tr[3]/tc[1]" --prop text="strike"       --prop strike=single
cell 4 "tr[3]/tc[2]" --prop text="align=center" --prop align=center
cell 4 "tr[3]/tc[3]" --prop text="align=right"  --prop align=right
cell 4 "tr[4]/tc[1]" --prop text="linespacing=1.5x — line one is followed by line two in this cell" --prop linespacing=1.5x
cell 4 "tr[4]/tc[2]" --prop text="spacebefore=10pt" --prop spacebefore=10pt
cell 4 "tr[4]/tc[3]" --prop text="spaceafter=10pt"  --prop spaceafter=10pt
# table-row also owns height (set on slide 1 too); set one here to keep row 4 roomy.
officecli set "$FILE" "/slide[4]/table[1]/tr[4]" --prop height=2.4cm

rm -f "$IMG"
officecli close "$FILE"
officecli validate "$FILE"
echo "Created: $FILE"
