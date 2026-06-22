#!/bin/bash
# Connectors and groups — flowchart-style shapes with attached arrows, then grouped.
# Demonstrates: --type connector with from=/to= shape references, straight/elbow/curve
# presets, arrowheads, --type group with comma-separated shape indices.

set -e

DIR="$(dirname "$0")"
PPTX="$DIR/shapes-connectors.pptx"

# Helper — Add a shape and echo the returned @id path on stdout.
# (officecli prints "Added shape at /slide[N]/shape[@id=M]" — last whitespace-delimited token is the path.)
add_shape_get_path() {
    officecli add "$PPTX" "$@" | awk '/Added/ {print $NF; exit}'
}

rm -f "$PPTX"
officecli create "$PPTX"
officecli open "$PPTX"

# ─────────────────────────────────────────────────────────────────────────────
# Slide 1 — Connector geometry presets (straight / elbow / curve)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[1]' --type shape \
    --prop text="Connector Presets" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

A1=$(add_shape_get_path '/slide[1]' --type shape --prop geometry=ellipse \
    --prop x=0.5in --prop y=1.5in --prop width=2in --prop height=1.2in \
    --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop text="A")
B1=$(add_shape_get_path '/slide[1]' --type shape --prop geometry=ellipse \
    --prop x=4.5in --prop y=1.5in --prop width=2in --prop height=1.2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop text="B")
officecli add "$PPTX" '/slide[1]' --type connector \
    --prop shape=straight --prop from="$A1" --prop to="$B1" \
    --prop color=1D3557 --prop lineWidth=2pt --prop tailEnd=triangle

officecli add "$PPTX" '/slide[1]' --type shape \
    --prop text='straight (default)' --prop size=12 \
    --prop x=0.5in --prop y=2.8in --prop width=6in --prop height=0.4in

A2=$(add_shape_get_path '/slide[1]' --type shape --prop geometry=ellipse \
    --prop x=0.5in --prop y=3.6in --prop width=2in --prop height=1.2in \
    --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop text="A")
B2=$(add_shape_get_path '/slide[1]' --type shape --prop geometry=ellipse \
    --prop x=4.5in --prop y=5in --prop width=2in --prop height=1.2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop text="B")
officecli add "$PPTX" '/slide[1]' --type connector \
    --prop shape=elbow --prop from="$A2" --prop to="$B2" \
    --prop color=1D3557 --prop lineWidth=2pt --prop tailEnd=triangle

officecli add "$PPTX" '/slide[1]' --type shape \
    --prop text='elbow (bent, 90° turns)' --prop size=12 \
    --prop x=0.5in --prop y=6.3in --prop width=6in --prop height=0.4in

A3=$(add_shape_get_path '/slide[1]' --type shape --prop geometry=ellipse \
    --prop x=8in --prop y=1.5in --prop width=2in --prop height=1.2in \
    --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop text="A")
B3=$(add_shape_get_path '/slide[1]' --type shape --prop geometry=ellipse \
    --prop x=11.5in --prop y=4.5in --prop width=2in --prop height=1.2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop text="B")
officecli add "$PPTX" '/slide[1]' --type connector \
    --prop shape=curve --prop from="$A3" --prop to="$B3" \
    --prop color=2A9D8F --prop lineWidth=3pt --prop tailEnd=arrow

officecli add "$PPTX" '/slide[1]' --type shape \
    --prop text='curve (smooth Bezier)' --prop size=12 \
    --prop x=7.5in --prop y=6in --prop width=6in --prop height=0.4in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 2 — Mini flowchart with attached connectors
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[2]' --type shape \
    --prop text="Flowchart with Attached Connectors" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

P1=$(add_shape_get_path '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=0.8in --prop y=2.5in --prop width=2.2in --prop height=1.2in \
    --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=16 \
    --prop text="Start")

P2=$(add_shape_get_path '/slide[2]' --type shape --prop geometry=diamond \
    --prop x=4.5in --prop y=2.3in --prop width=2.8in --prop height=1.6in \
    --prop fill=F4A261 --prop color=000000 --prop bold=true --prop size=14 \
    --prop text="Valid?")

P3=$(add_shape_get_path '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=9in --prop y=2.5in --prop width=2.2in --prop height=1.2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop size=16 \
    --prop text="End")

P4=$(add_shape_get_path '/slide[2]' --type shape --prop geometry=roundRect \
    --prop x=4.7in --prop y=5in --prop width=2.4in --prop height=1in \
    --prop fill=A8DADC --prop color=000000 --prop bold=true --prop size=14 \
    --prop text="Retry")

# Connect Start → Valid? → End, plus the loopback Valid? → Retry → back to Start
officecli add "$PPTX" '/slide[2]' --type connector \
    --prop shape=straight --prop from="$P1" --prop to="$P2" \
    --prop color=1D3557 --prop lineWidth=2pt --prop tailEnd=triangle

officecli add "$PPTX" '/slide[2]' --type connector \
    --prop shape=straight --prop from="$P2" --prop to="$P3" \
    --prop color=1D3557 --prop lineWidth=2pt --prop tailEnd=triangle

officecli add "$PPTX" '/slide[2]' --type connector \
    --prop shape=elbow --prop from="$P2" --prop to="$P4" \
    --prop color=E63946 --prop lineWidth=2pt --prop lineDash=dash --prop tailEnd=triangle

officecli add "$PPTX" '/slide[2]' --type connector \
    --prop shape=elbow --prop from="$P4" --prop to="$P1" \
    --prop color=E63946 --prop lineWidth=2pt --prop lineDash=dash --prop tailEnd=triangle

# Branch labels (textbox-style; no fill, transparent outline)
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=7.4in --prop y=2.7in --prop width=1.3in --prop height=0.5in \
    --prop text="yes" --prop size=12 --prop bold=true --prop color=2A9D8F

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=6in --prop y=4in --prop width=1.3in --prop height=0.5in \
    --prop text="no" --prop size=12 --prop bold=true --prop color=E63946

# ─────────────────────────────────────────────────────────────────────────────
# Slide 3 — Grouping shapes
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[3]' --type shape \
    --prop text="Grouping Shapes" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Three logo-like shapes that we'll group together.
G1=$(add_shape_get_path '/slide[3]' --type shape --prop geometry=ellipse \
    --prop x=1.5in --prop y=2in --prop width=1.4in --prop height=1.4in \
    --prop fill=E63946)
G2=$(add_shape_get_path '/slide[3]' --type shape --prop geometry=ellipse \
    --prop x=2.4in --prop y=2in --prop width=1.4in --prop height=1.4in \
    --prop fill=F4A261 --prop opacity=0.75)
G3=$(add_shape_get_path '/slide[3]' --type shape --prop geometry=ellipse \
    --prop x=3.3in --prop y=2in --prop width=1.4in --prop height=1.4in \
    --prop fill=2A9D8F --prop opacity=0.75)

# Group them by passing the captured shape paths (comma-separated)
officecli add "$PPTX" '/slide[3]' --type group \
    --prop shapes="$G1,$G2,$G3" --prop name="Logo"

officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text='Three ellipses grouped (shapes='"$G1,$G2,$G3"').' \
    --prop size=12 \
    --prop x=0.5in --prop y=4in --prop width=12in --prop height=0.5in

# Three independent boxes for comparison
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=8in --prop y=2in --prop width=1.4in --prop height=1.4in \
    --prop fill=4472C4
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=9.5in --prop y=2in --prop width=1.4in --prop height=1.4in \
    --prop fill=4472C4
officecli add "$PPTX" '/slide[3]' --type shape --prop geometry=rect \
    --prop x=11in --prop y=2in --prop width=1.4in --prop height=1.4in \
    --prop fill=4472C4

officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text='Three independent boxes (no group — each addressed separately).' \
    --prop size=12 \
    --prop x=7in --prop y=4in --prop width=6in --prop height=0.5in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 4 — headEnd / lineJoin / miterLimit on connectors
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[4]' --type shape \
    --prop text="headEnd / lineJoin / miterLimit on Connectors" \
    --prop size=24 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# headEnd — arrowhead at the START of the connector (the 'head' = from-side)
# tailEnd — arrowhead at the END of the connector (the 'tail' = to-side)
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text="headEnd + tailEnd arrowhead combinations:" \
    --prop size=14 --prop bold=true \
    --prop x=0.5in --prop y=1.1in --prop width=12in --prop height=0.4in

Y=1.8
for combo in "headEnd=triangle tailEnd=oval" "headEnd=diamond tailEnd=arrow" "headEnd=arrow tailEnd=arrow"; do
    read -r h t <<< "$combo"
    hv="${h#headEnd=}"; tv="${t#tailEnd=}"
    officecli add "$PPTX" '/slide[4]' --type connector \
        --prop shape=straight \
        --prop x=0.5in --prop "y=${Y}in" --prop width=5in --prop height=0in \
        --prop color=1D3557 --prop lineWidth=2pt \
        --prop headEnd="$hv" --prop tailEnd="$tv"
    officecli add "$PPTX" '/slide[4]' --type textbox \
        --prop text="headEnd=$hv  tailEnd=$tv" --prop size=12 \
        --prop x=5.8in --prop "y=${Y}in" --prop width=6in --prop height=0.4in
    Y=$(echo "$Y + 0.8" | bc -l)
done

# lineJoin — connector joins: round / bevel / miter
# lineJoin on connectors affects the joint where the connector bends (elbow)
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text="lineJoin on elbow connectors:" \
    --prop size=14 --prop bold=true \
    --prop x=0.5in --prop y=4.6in --prop width=12in --prop height=0.4in

# Three elbow connectors, one per column, so each join style at the bend is
# clearly visible. The third uses the compound lineJoin=miter:<lim> form
# (limit in 1/1000ths of a percent) to also exercise miterLimit.
add_elbow() { # $1=x(in)  $2=color  $3=lineJoin  $4=label
    officecli add "$PPTX" '/slide[4]' --type connector \
        --prop shape=elbow \
        --prop x="${1}in" --prop y=5.2in --prop width=3.4in --prop height=1.6in \
        --prop color="$2" --prop lineWidth=5pt \
        --prop lineJoin="$3"
    officecli add "$PPTX" '/slide[4]' --type textbox \
        --prop text="$4" --prop size=12 \
        --prop x="${1}in" --prop y=7.0in --prop width=4in --prop height=0.4in
}
add_elbow 0.5 E63946 round          "lineJoin=round"
add_elbow 4.7 E63946 bevel          "lineJoin=bevel"
add_elbow 8.9 2A9D8F "miter:800000" "lineJoin=miter:800000 (800% limit)"

officecli close "$PPTX"
officecli validate "$PPTX"
echo "Created: $PPTX"
