#!/bin/bash
# Shape effects and meta — autoFit, flipH/V, image fill, 3D (bevel/depth/lighting/material),
# softEdge, hyperlinks on shape, name override, zorder.
# Covers the shape props NOT touched by shapes-basic / shapes-connectors / textboxes-basic.

set -e

DIR="$(dirname "$0")"
PPTX="$DIR/shapes-effects.pptx"

# Build a tiny PNG once (16×16 magenta+yellow checker) for the image-fill demo.
SAMPLE_PNG="$(mktemp -t ocli-shape-fill.XXXXXX).png"
python3 - "$SAMPLE_PNG" <<'PY'
import struct, zlib, sys
W = H = 64
rows = []
for y in range(H):
    row = b"\x00"
    for x in range(W):
        cell = (x // 16 + y // 16) & 1
        row += (b"\xE6\x39\x46" if cell else b"\xFF\xE6\x6D")
    rows.append(row)
raw = b"".join(rows)
def chunk(t, d):
    return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
png = b"\x89PNG\r\n\x1a\n"
png += chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 2, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(raw))
png += chunk(b"IEND", b"")
open(sys.argv[1], "wb").write(png)
PY

rm -f "$PPTX"
officecli create "$PPTX"
officecli open "$PPTX"

# ─────────────────────────────────────────────────────────────────────────────
# Slide 1 — autoFit (text overflow behavior)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text="autoFit — text overflow behavior" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

LONGTEXT='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.'

# 'none' — text just overflows the box
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=4in --prop height=1.5in \
    --prop fill=F1FAEE --prop size=18 --prop text="$LONGTEXT" \
    --prop autoFit=none

officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text='autoFit=none  (overflows)' --prop size=12 --prop italic=true \
    --prop x=0.5in --prop y=3.2in --prop width=4in --prop height=0.4in

# 'normal' — shrinks text to fit
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop x=5in --prop y=1.5in --prop width=4in --prop height=1.5in \
    --prop fill=A8DADC --prop size=18 --prop text="$LONGTEXT" \
    --prop autoFit=normal

officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text='autoFit=normal  (text shrinks)' --prop size=12 --prop italic=true \
    --prop x=5in --prop y=3.2in --prop width=4in --prop height=0.4in

# 'shape' — box resizes to fit text
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop x=9.5in --prop y=1.5in --prop width=4in --prop height=1.5in \
    --prop fill=F4A261 --prop size=18 --prop text="$LONGTEXT" \
    --prop autoFit=shape

officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text='autoFit=shape  (box grows)' --prop size=12 --prop italic=true \
    --prop x=9.5in --prop y=4.5in --prop width=4in --prop height=0.4in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 2 — flipH / flipV (mirror)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop text="flipH / flipV — mirror" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Original
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=rightArrow \
    --prop x=0.5in --prop y=2in --prop width=2.8in --prop height=1.5in \
    --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop text="original"

# flipH
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=rightArrow \
    --prop x=4in --prop y=2in --prop width=2.8in --prop height=1.5in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop text="flipH=true" \
    --prop flipH=true

# flipV
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=rightArrow \
    --prop x=7.5in --prop y=2in --prop width=2.8in --prop height=1.5in \
    --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop text="flipV=true" \
    --prop flipV=true

# flipH + flipV (= rotation 180° visually, but stored as flip flags not rotation)
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=rightArrow \
    --prop x=11in --prop y=2in --prop width=2.8in --prop height=1.5in \
    --prop fill=F4A261 --prop color=000000 --prop bold=true --prop text="flipH + flipV" \
    --prop flipH=true --prop flipV=true

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop text='Aliases: flipHorizontal, flipVertical. Flip flags are stored independently of rotation, so flipH + rotate=90 chains predictably.' \
    --prop size=14 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=4in --prop width=13in --prop height=0.6in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 3 — image fill on a shape (blipFill, NOT --type picture)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text="image= — picture as shape fill (blipFill)" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# The image fills the shape interior; the geometry preset clips the image.
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=ellipse \
    --prop x=0.5in --prop y=1.5in --prop width=3.5in --prop height=3.5in \
    --prop image="$SAMPLE_PNG" \
    --prop lineColor=1D3557 --prop lineWidth=3pt

officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=star5 \
    --prop x=4.5in --prop y=1.5in --prop width=3.5in --prop height=3.5in \
    --prop image="$SAMPLE_PNG"

officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=diamond \
    --prop x=8.5in --prop y=1.5in --prop width=3.5in --prop height=3.5in \
    --prop image="$SAMPLE_PNG" \
    --prop lineColor=1D3557 --prop lineWidth=3pt

officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text='image="/path/to/photo.png" turns the shape into a clipped picture — different element from --type picture, which embeds the bitmap with its native bounding box.' \
    --prop size=14 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=5.5in --prop width=13in --prop height=1in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 4 — 3D effects (bevel, bevelBottom, depth, lighting, material)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text="3D — bevel / depth / lighting / material" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Bevel top, default size
officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=roundRect \
    --prop x=0.5in --prop y=1.4in --prop width=3in --prop height=1.8in \
    --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop size=14 \
    --prop text='bevel=circle' \
    --prop bevel=circle

# Bevel top + bottom with explicit widths
officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=roundRect \
    --prop x=4in --prop y=1.4in --prop width=3in --prop height=1.8in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop size=14 \
    --prop text='bevel=angle-8-4 + bevelBottom=circle-4-4' \
    --prop bevel=angle-8-4 --prop bevelBottom=circle-4-4

# Extrusion depth
officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=roundRect \
    --prop x=7.5in --prop y=1.4in --prop width=3in --prop height=1.8in \
    --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=14 \
    --prop text='depth=14pt + bevel=softRound' \
    --prop depth=14pt --prop bevel=softRound

# Lighting + material combos
officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=ellipse \
    --prop x=0.5in --prop y=3.7in --prop width=3in --prop height=1.8in \
    --prop fill=F4A261 --prop color=000000 --prop bold=true --prop size=12 \
    --prop text='bevel=circle-8 depth=10 lighting=threePt material=metal' \
    --prop bevel=circle-8 --prop depth=10 --prop lighting=threePt --prop material=metal

officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=ellipse \
    --prop x=4in --prop y=3.7in --prop width=3in --prop height=1.8in \
    --prop fill=A8DADC --prop color=000000 --prop bold=true --prop size=12 \
    --prop text='lighting=balanced material=plastic' \
    --prop bevel=circle-6 --prop depth=8 --prop lighting=balanced --prop material=plastic

officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=ellipse \
    --prop x=7.5in --prop y=3.7in --prop width=3in --prop height=1.8in \
    --prop fill=FFD700 --prop color=000000 --prop bold=true --prop size=12 \
    --prop text='lighting=harsh material=warmMatte' \
    --prop bevel=circle-6 --prop depth=8 --prop lighting=harsh --prop material=warmMatte

# ─────────────────────────────────────────────────────────────────────────────
# Slide 5 — softEdge + link + tooltip + name + zorder
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop text="softEdge / link / name / zorder" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# softEdge — feathered/blurred edge in points
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=ellipse \
    --prop x=0.5in --prop y=1.5in --prop width=3in --prop height=2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
    --prop text='softEdge=0  (sharp)' --prop softEdge=0

officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=ellipse \
    --prop x=4in --prop y=1.5in --prop width=3in --prop height=2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
    --prop text='softEdge=8pt' --prop softEdge=8pt

officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=ellipse \
    --prop x=7.5in --prop y=1.5in --prop width=3in --prop height=2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
    --prop text='softEdge=20pt  (heavy feather)' --prop softEdge=20pt

# link + tooltip on a shape — entire shape becomes clickable
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=roundRect \
    --prop x=0.5in --prop y=4in --prop width=4in --prop height=1in \
    --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=16 \
    --prop text="Click me → example.com" \
    --prop link=https://example.com --prop tooltip="Open example.com" \
    --prop name="cta-button"

officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop text='link=https://example.com  tooltip="Open example.com"  name="cta-button"' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=5.1in --prop width=6in --prop height=0.4in

# zorder — three overlapping shapes with explicit stack order
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
    --prop x=8in --prop y=4in --prop width=2.5in --prop height=2.5in \
    --prop fill=4472C4 --prop name="back" --prop zorder=1 \
    --prop color=FFFFFF --prop bold=true --prop text="back (zorder=1)"

officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
    --prop x=9in --prop y=4.5in --prop width=2.5in --prop height=2.5in \
    --prop fill=E63946 --prop name="middle" --prop zorder=2 \
    --prop color=FFFFFF --prop bold=true --prop text="middle (zorder=2)"

officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
    --prop x=10in --prop y=5in --prop width=2.5in --prop height=2.5in \
    --prop fill=F4A261 --prop name="front" --prop zorder=3 \
    --prop color=000000 --prop bold=true --prop text="front (zorder=3)"

officecli close "$PPTX"
officecli validate "$PPTX"
rm -f "$SAMPLE_PNG"
echo "Created: $PPTX"
