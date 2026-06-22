#!/bin/bash
# Basic PowerPoint table — header row, body rows, fills, font sizing.
# Demonstrates: add table with inline `data=` CSV, headerFill/bodyFill,
# per-cell text override via set, table dimensions (x/y/width/height).

set -e

DIR="$(dirname "$0")"
PPTX="$DIR/tables-basic.pptx"

rm -f "$PPTX"
officecli create "$PPTX"
officecli open "$PPTX"

# --- Slide 1: minimal 3x3 table seeded inline ---
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[1]' --type shape \
    --prop text="Basic Table — Inline Data" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# 'data=' uses CSV (comma = cell sep, semicolon = row sep).
officecli add "$PPTX" '/slide[1]' --type table \
    --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=2in \
    --prop headerFill=4472C4 --prop bodyFill=DEEAF6 \
    --prop data="Region,Q1,Q2,Q3,Q4;North,120,135,142,168;South,98,110,121,140;East,165,178,190,205"

# --- Slide 2: explicit rows/cols then per-cell text ---
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[2]' --type shape \
    --prop text="Basic Table — Per-Cell Set" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

officecli add "$PPTX" '/slide[2]' --type table \
    --prop x=0.5in --prop y=1.2in --prop width=10in --prop height=2.5in \
    --prop rows=4 --prop cols=3 --prop headerFill=2E75B6

# Header
for entry in "1:Product" "2:Units" "3:Revenue"; do
    col="${entry%%:*}"; txt="${entry#*:}"
    officecli set "$PPTX" "/slide[2]/table[1]/tr[1]/tc[$col]" \
        --prop text="$txt" --prop bold=true --prop color=FFFFFF
done

# Body
officecli set "$PPTX" '/slide[2]/table[1]/tr[2]/tc[1]' --prop text="Widget"
officecli set "$PPTX" '/slide[2]/table[1]/tr[2]/tc[2]' --prop text="1,200"
officecli set "$PPTX" '/slide[2]/table[1]/tr[2]/tc[3]' --prop text="\$48,000"
officecli set "$PPTX" '/slide[2]/table[1]/tr[3]/tc[1]' --prop text="Gizmo"
officecli set "$PPTX" '/slide[2]/table[1]/tr[3]/tc[2]' --prop text="850"
officecli set "$PPTX" '/slide[2]/table[1]/tr[3]/tc[3]' --prop text="\$72,250"
officecli set "$PPTX" '/slide[2]/table[1]/tr[4]/tc[1]' --prop text="Sprocket"
officecli set "$PPTX" '/slide[2]/table[1]/tr[4]/tc[2]' --prop text="430"
officecli set "$PPTX" '/slide[2]/table[1]/tr[4]/tc[3]' --prop text="\$25,800"

# --- Slide 3: Cell fill variations (solid hex, theme color, gradient, none) ---
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[3]' --type shape \
    --prop text="Cell Fill Variations" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

officecli add "$PPTX" '/slide[3]' --type table \
    --prop x=0.5in --prop y=1.2in --prop width=12in --prop height=4in \
    --prop rows=5 --prop cols=2 --prop style=none --prop border.all="1pt solid 808080"

officecli set "$PPTX" '/slide[3]/table[1]/tr[1]/tc[1]' --prop text="fill spec" --prop bold=true --prop fill=404040 --prop color=FFFFFF
officecli set "$PPTX" '/slide[3]/table[1]/tr[1]/tc[2]' --prop text="rendered" --prop bold=true --prop fill=404040 --prop color=FFFFFF

# Solid hex
officecli set "$PPTX" '/slide[3]/table[1]/tr[2]/tc[1]' --prop text='fill=FF0000  (solid hex)'
officecli set "$PPTX" '/slide[3]/table[1]/tr[2]/tc[2]' --prop fill=FF0000

# Named color
officecli set "$PPTX" '/slide[3]/table[1]/tr[3]/tc[1]' --prop text='fill=red  /  fill=rgb(255,0,0)  (named / rgb forms)'
officecli set "$PPTX" '/slide[3]/table[1]/tr[3]/tc[2]' --prop fill=red

# Theme color — accent1 follows the deck theme
officecli set "$PPTX" '/slide[3]/table[1]/tr[4]/tc[1]' --prop text='fill=accent1  (theme color, follows deck theme)'
officecli set "$PPTX" '/slide[3]/table[1]/tr[4]/tc[2]' --prop fill=accent1

# Gradient — "COLOR1-COLOR2[-ANGLE]"
officecli set "$PPTX" '/slide[3]/table[1]/tr[5]/tc[1]' --prop text='fill="FF0000-0000FF-90"  (gradient, 90° angle)'
officecli set "$PPTX" '/slide[3]/table[1]/tr[5]/tc[2]' --prop fill="FF0000-0000FF-90"

# fill=none demo (separate small table so 'none' is visible against page bg)
officecli add "$PPTX" '/slide[3]' --type shape \
    --prop text='fill=none  (explicit no-fill; cell becomes transparent):' --prop size=14 \
    --prop x=0.5in --prop y=5.4in --prop width=12in --prop height=0.4in
officecli add "$PPTX" '/slide[3]' --type table \
    --prop x=0.5in --prop y=5.9in --prop width=4in --prop height=0.8in \
    --prop rows=1 --prop cols=2 --prop style=none --prop border.all="1pt solid 000000"
officecli set "$PPTX" '/slide[3]/table[2]/tr[1]/tc[1]' --prop text="solid" --prop fill=FFE699
officecli set "$PPTX" '/slide[3]/table[2]/tr[1]/tc[2]' --prop text="none" --prop fill=none

# --- Slide 4: Cell typography — italic / underline / strike / font / wrap ---
# --- and paragraph spacing — linespacing / spacebefore / spaceafter        ---
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[4]' --type shape \
    --prop text="Cell Typography — italic / underline / strike / font / wrap / spacing" \
    --prop size=24 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=13in --prop height=0.6in

officecli add "$PPTX" '/slide[4]' --type table \
    --prop x=0.5in --prop y=1.1in --prop width=13in --prop height=5in \
    --prop rows=7 --prop cols=2 --prop style=none --prop border.all="1pt solid 808080"

# Header
officecli set "$PPTX" '/slide[4]/table[1]/tr[1]/tc[1]' \
    --prop text="Property" --prop bold=true --prop fill=2E75B6 --prop color=FFFFFF
officecli set "$PPTX" '/slide[4]/table[1]/tr[1]/tc[2]' \
    --prop text="Example" --prop bold=true --prop fill=2E75B6 --prop color=FFFFFF

# italic
officecli set "$PPTX" '/slide[4]/table[1]/tr[2]/tc[1]' --prop text="italic=true"
officecli set "$PPTX" '/slide[4]/table[1]/tr[2]/tc[2]' \
    --prop text="This cell text is italic." --prop italic=true

# underline
officecli set "$PPTX" '/slide[4]/table[1]/tr[3]/tc[1]' --prop text="underline=single"
officecli set "$PPTX" '/slide[4]/table[1]/tr[3]/tc[2]' \
    --prop text="This cell text is underlined." --prop underline=single

# strike
officecli set "$PPTX" '/slide[4]/table[1]/tr[4]/tc[1]' --prop text="strike=single"
officecli set "$PPTX" '/slide[4]/table[1]/tr[4]/tc[2]' \
    --prop text="This cell text has strikethrough." --prop strike=single

# font
officecli set "$PPTX" '/slide[4]/table[1]/tr[5]/tc[1]' --prop text="font=Georgia"
officecli set "$PPTX" '/slide[4]/table[1]/tr[5]/tc[2]' \
    --prop text="This cell uses Georgia." --prop font="Georgia" --prop size=16

# wrap=false (text doesn't wrap; overflow is clipped)
officecli set "$PPTX" '/slide[4]/table[1]/tr[6]/tc[1]' --prop text="wrap=false"
officecli set "$PPTX" '/slide[4]/table[1]/tr[6]/tc[2]' \
    --prop text="This is a long sentence that will not wrap because wrap is disabled — it just runs off the edge." \
    --prop wrap=false

# linespacing / spacebefore / spaceafter
officecli set "$PPTX" '/slide[4]/table[1]/tr[7]/tc[1]' \
    --prop text="linespacing=1.5x + spacebefore=4pt + spaceafter=4pt"
officecli set "$PPTX" '/slide[4]/table[1]/tr[7]/tc[2]' \
    --prop text="Paragraph A" \
    --prop linespacing=1.5x --prop spacebefore=4pt --prop spaceafter=4pt

# --- Slide 5: Cell layout — padding / padding.bottom / opacity / image / ---
# ---           textdirection / direction / merge.right / bevel / border.right ---
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[5]' --type shape \
    --prop text="Cell Layout — padding / opacity / image / textdirection / merge.right / bevel" \
    --prop size=22 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=13in --prop height=0.6in

officecli add "$PPTX" '/slide[5]' --type table \
    --prop x=0.5in --prop y=1.1in --prop width=13in --prop height=6.2in \
    --prop rows=8 --prop cols=2 --prop style=none --prop border.all="1pt solid 808080"

# Header
officecli set "$PPTX" '/slide[5]/table[1]/tr[1]/tc[1]' \
    --prop text="Property" --prop bold=true --prop fill=1F4E79 --prop color=FFFFFF
officecli set "$PPTX" '/slide[5]/table[1]/tr[1]/tc[2]' \
    --prop text="Example" --prop bold=true --prop fill=1F4E79 --prop color=FFFFFF

# padding — uniform inner margin
officecli set "$PPTX" '/slide[5]/table[1]/tr[2]/tc[1]' --prop text="padding=0.25in"
officecli set "$PPTX" '/slide[5]/table[1]/tr[2]/tc[2]' \
    --prop text="Large inner margin." --prop fill=F1FAEE --prop padding=0.25in

# padding.bottom — single-edge padding override
officecli set "$PPTX" '/slide[5]/table[1]/tr[3]/tc[1]' --prop text="padding.bottom=0.3in"
officecli set "$PPTX" '/slide[5]/table[1]/tr[3]/tc[2]' \
    --prop text="Extra space below this text." --prop fill=F1FAEE --prop "padding.bottom=0.3in"

# opacity — fill transparency (0=opaque, 1=invisible)
officecli set "$PPTX" '/slide[5]/table[1]/tr[4]/tc[1]' --prop text="opacity=0.4  (requires fill)"
officecli set "$PPTX" '/slide[5]/table[1]/tr[4]/tc[2]' \
    --prop text="40% transparent fill." --prop fill=4472C4 --prop opacity=0.4

# image — picture fill (blipFill on the cell)
IMGFILE="$(python3 - <<'PY'
import struct, zlib, tempfile, os
W = H = 32
rows = []
for y in range(H):
    row = b'\x00'
    for x in range(W):
        cell = (x // 8 + y // 8) & 1
        row += (b'\x4a\x72\xc4' if cell else b'\xa8\xda\xdc')
    rows.append(row)
raw = b''.join(rows)
def chunk(t, d):
    return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
png = b'\x89PNG\r\n\x1a\n'
png += chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 2, 0, 0, 0))
png += chunk(b'IDAT', zlib.compress(raw))
png += chunk(b'IEND', b'')
p = tempfile.mktemp(suffix='.png')
open(p, 'wb').write(png)
print(p)
PY
)"
officecli set "$PPTX" '/slide[5]/table[1]/tr[5]/tc[1]' --prop text="image=/path/to/img.png"
officecli set "$PPTX" '/slide[5]/table[1]/tr[5]/tc[2]' \
    --prop image="$IMGFILE"
rm -f "$IMGFILE"

# textdirection — vertical text rendering in a cell
officecli set "$PPTX" '/slide[5]/table[1]/tr[6]/tc[1]' --prop text="textdirection=vert"
officecli set "$PPTX" '/slide[5]/table[1]/tr[6]/tc[2]' \
    --prop text="Vertical text" --prop textdirection=vert --prop fill=FFE699

# direction — RTL paragraph direction within a cell
officecli set "$PPTX" '/slide[5]/table[1]/tr[7]/tc[1]' --prop text="direction=rtl"
officecli set "$PPTX" '/slide[5]/table[1]/tr[7]/tc[2]' \
    --prop text="مرحبا" --prop direction=rtl --prop size=18 --prop fill=A8DADC

# merge.right + bevel + border.right (per-cell border)
officecli set "$PPTX" '/slide[5]/table[1]/tr[8]/tc[1]' \
    --prop text="merge.right=1  bevel=circle  border.right=2pt solid E63946" \
    --prop fill=F4A261 --prop size=11
officecli set "$PPTX" '/slide[5]/table[1]/tr[8]/tc[2]' \
    --prop text="Merged, beveled, custom right border." \
    --prop fill=F4A261 --prop bevel=circle \
    --prop "border.right=2pt solid E63946"

officecli close "$PPTX"
officecli validate "$PPTX"
echo "Created: $PPTX"
