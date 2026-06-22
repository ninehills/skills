#!/bin/bash
# Basic PowerPoint shapes — geometries, fills, outlines, effects, rotation, opacity.
# Demonstrates: --type shape with geometry preset, solid/gradient/pattern/image fills,
# line styling (color/width/dash/arrowheads), rotation, opacity, shadow effects.

set -e

DIR="$(dirname "$0")"
PPTX="$DIR/shapes-basic.pptx"

rm -f "$PPTX"
officecli create "$PPTX"
officecli open "$PPTX"

# ─────────────────────────────────────────────────────────────────────────────
# Slide 1 — Geometry preset gallery
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[1]' --type shape \
    --prop text="Geometry Presets" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Row of 8 shapes, one per supported preset.
# Schema-declared presets: rect, roundRect, ellipse, triangle, diamond,
# parallelogram, rightArrow, star5
COL=0
for preset in rect roundRect ellipse triangle diamond parallelogram rightArrow star5; do
    X=$(echo "0.5 + $COL * 1.55" | bc -l)
    officecli add "$PPTX" '/slide[1]' --type shape \
        --prop geometry="$preset" \
        --prop x="${X}in" --prop y=1.5in --prop width=1.3in --prop height=1.3in \
        --prop fill=4472C4 --prop color=FFFFFF \
        --prop text="$preset" --prop size=11 --prop bold=true
    COL=$((COL + 1))
done

# ─────────────────────────────────────────────────────────────────────────────
# Slide 2 — Fill variations on the same geometry
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[2]' --type shape \
    --prop text="Fill Variations" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Solid hex
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=0.5in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true \
    --prop text='fill=E63946'

# Theme color (follows deck theme)
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=3.3in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
    --prop fill=accent2 --prop color=FFFFFF --prop bold=true \
    --prop text='fill=accent2'

# Linear gradient (color1-color2-angle)
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=6.1in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
    --prop gradient="FF6B6B-4ECDC4-45" --prop color=FFFFFF --prop bold=true \
    --prop text='gradient linear 45°'

# Radial gradient
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=8.9in --prop y=1.3in --prop width=2.5in --prop height=1.5in \
    --prop gradient="radial:FFE66D-FF6B35-center" --prop color=000000 --prop bold=true \
    --prop text='gradient radial'

# Pattern (preset:fg:bg)
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=0.5in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
    --prop pattern="diagBrick:1D3557:F1FAEE" --prop color=FFFFFF --prop bold=true \
    --prop text='pattern diagBrick'

# Opacity (requires a fill to attach to)
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=3.3in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
    --prop fill=2A9D8F --prop opacity=0.4 --prop color=000000 --prop bold=true \
    --prop text='fill + opacity=0.4'

# No fill (outline only)
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=6.1in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
    --prop fill=none --prop line="264653:2.5:solid" --prop color=264653 --prop bold=true \
    --prop text='fill=none + outline'

# Per-stop gradient positions
officecli add "$PPTX" '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=8.9in --prop y=3.1in --prop width=2.5in --prop height=1.5in \
    --prop gradient="FF0000@0-FFD700@40-0000FF@100" --prop color=FFFFFF --prop bold=true \
    --prop text='gradient per-stop'

# ─────────────────────────────────────────────────────────────────────────────
# Slide 3 — Outline styling (line color / width / dash / caps / arrowheads)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[3]' --type shape \
    --prop text="Outline Styling" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Compound line form: color:width:dash
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=0.5in --prop y=1.3in --prop width=3in --prop height=1.2in \
    --prop fill=none --prop line="E63946:3:solid" \
    --prop text='line="E63946:3:solid"' --prop size=12

officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=4in --prop y=1.3in --prop width=3in --prop height=1.2in \
    --prop fill=none --prop line="1D3557:2:dash" \
    --prop text='line="1D3557:2:dash"' --prop size=12

officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=7.5in --prop y=1.3in --prop width=3in --prop height=1.2in \
    --prop fill=none --prop line="2A9D8F:2.5:dashDot" \
    --prop text='line="2A9D8F:2.5:dashDot"' --prop size=12

# Per-attribute form: lineColor + lineWidth + lineDash
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=ellipse \
    --prop x=0.5in --prop y=2.9in --prop width=3in --prop height=1.4in \
    --prop fill=FFE66D --prop lineColor=E63946 --prop lineWidth=4pt --prop lineDash=solid \
    --prop text='separate lineColor/lineWidth/lineDash' --prop size=11

# Compound stroke (cmpd=dbl → double line)
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=ellipse \
    --prop x=4in --prop y=2.9in --prop width=3in --prop height=1.4in \
    --prop fill=A8DADC --prop lineColor=1D3557 --prop lineWidth=6pt --prop cmpd=dbl \
    --prop text='cmpd=dbl (double stroke)' --prop size=11

# Triple stroke
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=ellipse \
    --prop x=7.5in --prop y=2.9in --prop width=3in --prop height=1.4in \
    --prop fill=A8DADC --prop lineColor=1D3557 --prop lineWidth=8pt --prop cmpd=tri \
    --prop text='cmpd=tri (triple stroke)' --prop size=11

# Arrowheads on shape outlines (rightArrow already arrow-shaped — demo headEnd/tailEnd here)
officecli add "$PPTX" '/slide[3]' --type shape \
    --prop text="headEnd / tailEnd work on any outline (not just connectors):" \
    --prop size=12 \
    --prop x=0.5in --prop y=4.7in --prop width=12in --prop height=0.4in

officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=0.5in --prop y=5.2in --prop width=4in --prop height=0.05in \
    --prop fill=none --prop lineColor=000000 --prop lineWidth=2pt \
    --prop headEnd=triangle --prop tailEnd=arrow

officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=5in --prop y=5.2in --prop width=4in --prop height=0.05in \
    --prop fill=none --prop lineColor=000000 --prop lineWidth=2pt \
    --prop headEnd=diamond --prop tailEnd=oval

# ─────────────────────────────────────────────────────────────────────────────
# Slide 4 — Rotation, shadow effect, z-order via add order
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[4]' --type shape \
    --prop text="Rotation + Effects" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Rotation in degrees (0..360)
COL=0
for r in 0 30 60 90 135 180 225 270; do
    X=$(echo "0.5 + $COL * 1.55" | bc -l)
    officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=rightArrow \
        --prop x="${X}in" --prop y=1.3in --prop width=1.4in --prop height=0.8in \
        --prop fill=4472C4 --prop color=FFFFFF --prop bold=true \
        --prop rotation="$r" --prop text="${r}°" --prop size=11
    COL=$((COL + 1))
done

# Shadow effect: shadow=color:blur:offset:direction (handler's compound effect)
officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=roundRect \
    --prop x=1in --prop y=3in --prop width=3.5in --prop height=1.8in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop size=14 \
    --prop text='shadow=000000' \
    --prop shadow=000000

officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=roundRect \
    --prop x=5.5in --prop y=3in --prop width=3.5in --prop height=1.8in \
    --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=14 \
    --prop text='glow=FFD700' \
    --prop glow=FFD700

officecli add "$PPTX" '/slide[4]' --type shape --prop geometry=roundRect \
    --prop x=10in --prop y=3in --prop width=3in --prop height=1.8in \
    --prop fill=F4A261 --prop color=000000 --prop bold=true --prop size=14 \
    --prop text='reflection=tight' \
    --prop reflection=tight

# ─────────────────────────────────────────────────────────────────────────────
# Slide 5 — Stroke geometry details (lineCap / lineJoin / lineAlign)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[5]' --type shape \
    --prop text="Stroke Geometry — lineCap / lineJoin / lineAlign" \
    --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# lineCap — how the stroke terminates at line endpoints / dash gaps.
# Most visible with a thick dashed stroke.
X=0.5
for cap in flat round square; do
    officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
        --prop x="${X}in" --prop y=1.5in --prop width=4in --prop height=0.05in \
        --prop fill=none --prop lineColor=1D3557 --prop lineWidth=10pt \
        --prop lineDash=dash --prop lineCap="$cap"
    officecli add "$PPTX" '/slide[5]' --type shape \
        --prop text="lineCap=$cap" --prop size=12 \
        --prop x="${X}in" --prop y=1.8in --prop width=4in --prop height=0.4in \
        --prop fill=none --prop line="000000:0:solid"
    X=$(echo "$X + 4.3" | bc -l)
done

# lineJoin — corner style on a stroked shape.
# Most visible on a triangle outline with thick lines.
X=0.5
for join in round bevel miter; do
    officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=triangle \
        --prop x="${X}in" --prop y=2.8in --prop width=2.5in --prop height=2in \
        --prop fill=A8DADC --prop lineColor=E63946 --prop lineWidth=12pt \
        --prop lineJoin="$join"
    officecli add "$PPTX" '/slide[5]' --type shape \
        --prop text="lineJoin=$join" --prop size=12 \
        --prop x="${X}in" --prop y=4.9in --prop width=2.5in --prop height=0.4in \
        --prop fill=none --prop line="000000:0:solid"
    X=$(echo "$X + 3" | bc -l)
done

# miterLimit — caps how far a miter join's spike extends before it's clipped.
# Expressed in 1/1000ths of a percent; 800000 = 800%. Supplied as the compound
# lineJoin=miter:<lim> form which sets both join style and limit in one prop.
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=triangle \
    --prop x=0.5in --prop y=5.1in --prop width=2.5in --prop height=1.6in \
    --prop fill=A8DADC --prop lineColor=E63946 --prop lineWidth=8pt \
    --prop lineJoin="miter:800000"
officecli add "$PPTX" '/slide[5]' --type shape \
    --prop text='lineJoin="miter:800000"  (limit 800%)' --prop size=12 \
    --prop x=0.5in --prop y=6.9in --prop width=4in --prop height=0.4in \
    --prop fill=none --prop line="000000:0:solid"

# lineAlign — stroke alignment relative to the path: ctr (centered) vs in (inset).
# Same shape, same border width, only the alignment of the stroke differs.
X=8.9
for algn in ctr in; do
    officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
        --prop x="${X}in" --prop y=2.8in --prop width=1.9in --prop height=2in \
        --prop fill=F4A261 --prop lineColor=1D3557 --prop lineWidth=12pt \
        --prop lineAlign="$algn"
    officecli add "$PPTX" '/slide[5]' --type shape \
        --prop text="lineAlign=$algn" --prop size=12 \
        --prop x="${X}in" --prop y=4.9in --prop width=2in --prop height=0.4in \
        --prop fill=none --prop line="000000:0:solid"
    X=$(echo "$X + 2.1" | bc -l)
done

officecli close "$PPTX"
officecli validate "$PPTX"
echo "Created: $PPTX"
