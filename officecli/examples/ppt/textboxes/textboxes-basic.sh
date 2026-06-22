#!/bin/bash
# Basic PowerPoint textboxes — alignment, multi-paragraph, bulleted lists,
# styled runs, per-script fonts (Latin/EastAsian), vertical alignment, padding.
# Demonstrates: --type textbox vs --type shape (textbox is an alias for shape),
# --type paragraph with align/lineSpacing/indent, --type run with bold/color/baseline,
# shape-level list=bullet|ordered, font.latin/font.ea, valign and margin/autoFit.

set -e

DIR="$(dirname "$0")"
PPTX="$DIR/textboxes-basic.pptx"

rm -f "$PPTX"
officecli create "$PPTX"
officecli open "$PPTX"

# ─────────────────────────────────────────────────────────────────────────────
# Slide 1 — Horizontal alignment (4 textboxes, one per align value)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text="Horizontal Alignment" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

LOREM='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus lacinia odio vitae vestibulum vestibulum.'

Y=1.3
for a in left center right justify; do
    officecli add "$PPTX" '/slide[1]' --type textbox \
        --prop x=0.5in --prop y="${Y}in" --prop width=12in --prop height=1.3in \
        --prop fill=F1FAEE --prop text="[align=$a] $LOREM" --prop size=14 \
        --prop align="$a"
    Y=$(echo "$Y + 1.5" | bc -l)
done

# ─────────────────────────────────────────────────────────────────────────────
# Slide 2 — Multi-paragraph + bulleted / numbered lists
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop text="Lists and Multi-Paragraph" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Bulleted list — start with one initial paragraph, append the rest, then turn on bullets.
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=1.2in --prop width=6in --prop height=4in \
    --prop text="Coffee preparation steps" \
    --prop bold=true --prop size=18 --prop color=1D3557

officecli add "$PPTX" '/slide[2]/shape[1]' --type paragraph --prop text="Grind beans to medium-fine"
officecli add "$PPTX" '/slide[2]/shape[1]' --type paragraph --prop text="Heat water to 93°C"
officecli add "$PPTX" '/slide[2]/shape[1]' --type paragraph --prop text="Bloom 30s with 2× coffee weight"
officecli add "$PPTX" '/slide[2]/shape[1]' --type paragraph --prop text="Pour remaining water in spirals"
officecli add "$PPTX" '/slide[2]/shape[1]' --type paragraph --prop text="Total brew time: 3-4 minutes"

# Turn paragraphs 2-6 into bullets (level 0). Paragraph 1 is the title — leave unbulleted.
officecli set "$PPTX" '/slide[2]/shape[1]' --prop list=bullet

# Numbered list (ordered)
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=7in --prop y=1.2in --prop width=6in --prop height=4in \
    --prop text="Release checklist" \
    --prop bold=true --prop size=18 --prop color=1D3557

officecli add "$PPTX" '/slide[2]/shape[2]' --type paragraph --prop text="Run tests"
officecli add "$PPTX" '/slide[2]/shape[2]' --type paragraph --prop text="Tag the release"
officecli add "$PPTX" '/slide[2]/shape[2]' --type paragraph --prop text="Push to registry"
officecli add "$PPTX" '/slide[2]/shape[2]' --type paragraph --prop text="Announce in #releases"

officecli set "$PPTX" '/slide[2]/shape[2]' --prop list=numbered

# Indented sub-bullet — level=1 on a paragraph nests it one step in.
officecli add "$PPTX" '/slide[2]/shape[2]' --type paragraph \
    --prop text="(verify checksum)" --prop level=1
officecli set "$PPTX" '/slide[2]/shape[2]' --prop list=numbered

# ─────────────────────────────────────────────────────────────────────────────
# Slide 3 — Styled runs (rich text within one paragraph)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text="Rich Text — Runs" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Empty paragraph that we'll fill with multiple runs of different styles.
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=12in --prop height=1in \
    --prop text="" --prop size=20

officecli add "$PPTX" '/slide[3]/shape[1]/p[1]' --type run --prop text="The "
officecli add "$PPTX" '/slide[3]/shape[1]/p[1]' --type run \
    --prop text="quick " --prop bold=true --prop color=E63946
officecli add "$PPTX" '/slide[3]/shape[1]/p[1]' --type run \
    --prop text="brown " --prop italic=true --prop color=A0522D
officecli add "$PPTX" '/slide[3]/shape[1]/p[1]' --type run --prop text="fox jumps over the "
officecli add "$PPTX" '/slide[3]/shape[1]/p[1]' --type run \
    --prop text="lazy " --prop underline=single --prop color=2A9D8F
officecli add "$PPTX" '/slide[3]/shape[1]/p[1]' --type run --prop text="dog."

# Superscript / subscript via baseline
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop x=0.5in --prop y=3in --prop width=12in --prop height=0.8in \
    --prop text="" --prop size=24

officecli add "$PPTX" '/slide[3]/shape[2]/p[1]' --type run --prop text="E = mc"
officecli add "$PPTX" '/slide[3]/shape[2]/p[1]' --type run --prop text="2" --prop baseline=super
officecli add "$PPTX" '/slide[3]/shape[2]/p[1]' --type run --prop text="    and H"
officecli add "$PPTX" '/slide[3]/shape[2]/p[1]' --type run --prop text="2" --prop baseline=sub
officecli add "$PPTX" '/slide[3]/shape[2]/p[1]' --type run --prop text="O"

# Strikethrough + small caps + colored
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop x=0.5in --prop y=4.2in --prop width=12in --prop height=0.8in \
    --prop text="" --prop size=20

officecli add "$PPTX" '/slide[3]/shape[3]/p[1]' --type run \
    --prop text="OLD PRICE: \$99   " --prop strike=single --prop color=999999
officecli add "$PPTX" '/slide[3]/shape[3]/p[1]' --type run \
    --prop text="NOW \$49!" --prop bold=true --prop color=E63946 --prop size=24

# ─────────────────────────────────────────────────────────────────────────────
# Slide 4 — Per-script fonts (Latin + East Asian) + vertical alignment + padding
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text="Multilingual Fonts + Layout" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Mixed-script box: separate fonts for Latin and EastAsian text.
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=6in --prop height=2in \
    --prop fill=F1FAEE --prop margin=0.2in \
    --prop text="Hello, 世界! こんにちは、世界。" \
    --prop size=24 --prop bold=true \
    --prop font.latin="Georgia" --prop font.ea="Yu Mincho"

officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=3.7in --prop width=6in --prop height=0.5in \
    --prop text='font.latin=Georgia, font.ea="Yu Mincho"' \
    --prop size=12 --prop italic=true --prop color=666666

# Vertical alignment within a tall box
for va in top middle bottom; do
    case $va in
        top) X=7 ;;
        middle) X=9.5 ;;
        bottom) X=12 ;; # off-slide — squeeze
    esac
done

X=7
for va in top middle bottom; do
    officecli add "$PPTX" '/slide[4]' --type textbox \
        --prop x="${X}in" --prop y=1.5in --prop width=2in --prop height=3in \
        --prop fill=A8DADC --prop margin=0.15in \
        --prop text="valign=$va" --prop size=16 --prop bold=true \
        --prop valign="$va" --prop align=center
    X=$(echo "$X + 2.2" | bc -l)
done

officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=7in --prop y=4.8in --prop width=6in --prop height=0.5in \
    --prop text='valign + per-box margin + align=center' \
    --prop size=12 --prop italic=true --prop color=666666

officecli close "$PPTX"
officecli validate "$PPTX"
echo "Created: $PPTX"
