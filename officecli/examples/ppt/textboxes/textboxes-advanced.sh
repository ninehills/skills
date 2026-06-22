#!/bin/bash
# Advanced PPT textbox typography — per-paragraph and per-run overrides that the
# basic example didn't reach. Each slide demonstrates a different scope:
#   slide 1 — per-paragraph align / lineSpacing override (shape default vs paragraph)
#   slide 2 — paragraph indents (indent / marginLeft / marginRight) for hanging-indent style
#   slide 3 — per-paragraph styling (bold / italic / color / size / lang) without runs
#   slide 4 — per-run typography (font / size / spacing / kern / lang) inside one paragraph
#   slide 5 — subscript / superscript convenience aliases vs canonical baseline=

set -e

DIR="$(dirname "$0")"
PPTX="$DIR/textboxes-advanced.pptx"

rm -f "$PPTX"
officecli create "$PPTX"
officecli open "$PPTX"

LOREM='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus lacinia odio vitae vestibulum vestibulum.'

# ─────────────────────────────────────────────────────────────────────────────
# Slide 1 — Per-paragraph overrides (align / lineSpacing inside one textbox)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text="Per-paragraph overrides inside one textbox" \
    --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# One textbox; shape-level defaults are align=left, lineSpacing=1x.
# Each paragraph overrides one of them.
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop x=0.5in --prop y=1.2in --prop width=13in --prop height=5.5in \
    --prop fill=F1FAEE --prop size=14 \
    --prop text="[shape default: align=left, single-spaced]  $LOREM"

officecli add "$PPTX" '/slide[1]/shape[2]' --type paragraph \
    --prop text="[paragraph override: align=center]  $LOREM" --prop align=center

officecli add "$PPTX" '/slide[1]/shape[2]' --type paragraph \
    --prop text="[paragraph override: align=right]  $LOREM" --prop align=right

officecli add "$PPTX" '/slide[1]/shape[2]' --type paragraph \
    --prop text="[paragraph override: align=justify + lineSpacing=2x]  $LOREM $LOREM" \
    --prop align=justify --prop lineSpacing=2x

officecli add "$PPTX" '/slide[1]/shape[2]' --type paragraph \
    --prop text="[paragraph override: lineSpacing=18pt fixed]  $LOREM" \
    --prop lineSpacing=18pt

# ─────────────────────────────────────────────────────────────────────────────
# Slide 2 — Paragraph indents (indent / marginLeft / marginRight)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop text="Paragraph indents — indent / marginLeft / marginRight" \
    --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Reference (no indent)
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=1.3in --prop width=13in --prop height=1in \
    --prop fill=F1FAEE --prop size=14 \
    --prop text="[default: no indent]  $LOREM $LOREM"

# Left indent (whole paragraph shifted right)
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=2.5in --prop width=13in --prop height=1in \
    --prop fill=A8DADC --prop size=14 \
    --prop text="[marginLeft=1in]  $LOREM $LOREM" \
    --prop marginLeft=1in

# First-line indent (only first line is shifted; rest flush left)
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=3.7in --prop width=13in --prop height=1in \
    --prop fill=F4A261 --prop size=14 \
    --prop text="[indent=0.5in first-line]  $LOREM $LOREM" \
    --prop indent=0.5in

# Hanging indent (negative indent + positive marginLeft pulls first line back left)
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=4.9in --prop width=13in --prop height=1in \
    --prop fill=A8DADC --prop size=14 \
    --prop text="[hanging: marginLeft=0.6in + indent=-0.5in]  $LOREM $LOREM" \
    --prop marginLeft=0.6in --prop indent=-0.5in

# Right margin (text narrowed from the right)
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=6.1in --prop width=13in --prop height=1in \
    --prop fill=F4A261 --prop size=14 \
    --prop text="[marginRight=2in]  $LOREM $LOREM" \
    --prop marginRight=2in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 3 — Per-paragraph styling (bold / italic / color / size / lang)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text="Per-paragraph styling (no runs needed)" \
    --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# One textbox; each paragraph carries its own bold/italic/color/size/lang.
# This is cheaper than adding a run when the whole paragraph shares one style.
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop x=0.5in --prop y=1.2in --prop width=13in --prop height=5in \
    --prop fill=F1FAEE --prop size=14 \
    --prop text="[shape default: 14pt black]  Default paragraph styling."

officecli add "$PPTX" '/slide[3]/shape[2]' --type paragraph \
    --prop text="[bold=true at paragraph level]  Whole paragraph is bold." \
    --prop bold=true

officecli add "$PPTX" '/slide[3]/shape[2]' --type paragraph \
    --prop text="[italic=true at paragraph level]  Whole paragraph is italic." \
    --prop italic=true

officecli add "$PPTX" '/slide[3]/shape[2]' --type paragraph \
    --prop text="[color=E63946 at paragraph level]  Whole paragraph is red." \
    --prop color=E63946

officecli add "$PPTX" '/slide[3]/shape[2]' --type paragraph \
    --prop text="[size=22 at paragraph level]  Whole paragraph is 22pt." \
    --prop size=22

officecli add "$PPTX" '/slide[3]/shape[2]' --type paragraph \
    --prop text="[lang=fr-FR at paragraph level]  Lorem ipsum dolor sit amet." \
    --prop lang=fr-FR --prop color=2A9D8F

# ─────────────────────────────────────────────────────────────────────────────
# Slide 4 — Per-run typography (font / size / spacing / kern / lang in one paragraph)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text="Per-run typography in one paragraph" \
    --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Empty textbox we'll build run-by-run
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=13in --prop height=1in \
    --prop text="" --prop size=20

officecli add "$PPTX" '/slide[4]/shape[1]/p[1]' --type run --prop text="Mix "
officecli add "$PPTX" '/slide[4]/shape[1]/p[1]' --type run \
    --prop text="Times " --prop font="Times New Roman" --prop size=24
officecli add "$PPTX" '/slide[4]/shape[1]/p[1]' --type run \
    --prop text="Courier " --prop font="Courier New" --prop size=18
officecli add "$PPTX" '/slide[4]/shape[1]/p[1]' --type run \
    --prop text="Georgia" --prop font="Georgia" --prop size=28 --prop bold=true

# Per-run character spacing
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=3in --prop width=13in --prop height=1in \
    --prop text="" --prop size=20 --prop bold=true

officecli add "$PPTX" '/slide[4]/shape[2]/p[1]' --type run --prop text="Normal "
officecli add "$PPTX" '/slide[4]/shape[2]/p[1]' --type run \
    --prop text="TIGHTENED " --prop spacing=-1 --prop color=E63946
officecli add "$PPTX" '/slide[4]/shape[2]/p[1]' --type run \
    --prop text="LOOSENED " --prop spacing=4 --prop color=2A9D8F
officecli add "$PPTX" '/slide[4]/shape[2]/p[1]' --type run \
    --prop text="EXPANDED" --prop spacing=8 --prop color=1D3557

# Per-run kerning threshold
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=4.3in --prop width=13in --prop height=1in \
    --prop text="" --prop size=20 --prop bold=true

officecli add "$PPTX" '/slide[4]/shape[3]/p[1]' --type run \
    --prop text="AV AT WA — kern=0  " --prop kern=0
officecli add "$PPTX" '/slide[4]/shape[3]/p[1]' --type run \
    --prop text="AV AT WA — kern=1" --prop kern=1 --prop color=E63946

# Per-run lang tag (drives spellcheck per-run)
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=5.6in --prop width=13in --prop height=1in \
    --prop text="" --prop size=20

officecli add "$PPTX" '/slide[4]/shape[4]/p[1]' --type run \
    --prop text="English: color  " --prop lang=en-US
officecli add "$PPTX" '/slide[4]/shape[4]/p[1]' --type run \
    --prop text="British: colour  " --prop lang=en-GB --prop color=2A9D8F
officecli add "$PPTX" '/slide[4]/shape[4]/p[1]' --type run \
    --prop text="Français: couleur" --prop lang=fr-FR --prop color=E63946

# ─────────────────────────────────────────────────────────────────────────────
# Slide 5 — subscript / superscript aliases vs canonical baseline=
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop text="subscript / superscript aliases" \
    --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Convenience form: subscript=true and superscript=true
officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=13in --prop height=1in \
    --prop text="" --prop size=24

officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run --prop text="H"
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run \
    --prop text="2" --prop subscript=true
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run --prop text="SO"
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run \
    --prop text="4" --prop subscript=true
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run --prop text="   x"
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run \
    --prop text="2" --prop superscript=true
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run --prop text=" + y"
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run \
    --prop text="2" --prop superscript=true
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run --prop text=" = r"
officecli add "$PPTX" '/slide[5]/shape[1]/p[1]' --type run \
    --prop text="2" --prop superscript=true

officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop text='subscript=true   ≡   baseline=sub      superscript=true   ≡   baseline=super' \
    --prop size=14 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=2.8in --prop width=13in --prop height=0.5in

# Custom baseline percent — neither alias gives you this
officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop x=0.5in --prop y=3.7in --prop width=13in --prop height=1in \
    --prop text="" --prop size=24

officecli add "$PPTX" '/slide[5]/shape[3]/p[1]' --type run --prop text="Custom: "
officecli add "$PPTX" '/slide[5]/shape[3]/p[1]' --type run \
    --prop text="50%" --prop baseline=50 --prop color=E63946
officecli add "$PPTX" '/slide[5]/shape[3]/p[1]' --type run --prop text=" higher  /  "
officecli add "$PPTX" '/slide[5]/shape[3]/p[1]' --type run \
    --prop text="-40%" --prop baseline=-40 --prop color=2A9D8F
officecli add "$PPTX" '/slide[5]/shape[3]/p[1]' --type run --prop text=" lower"

officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop text='baseline= accepts signed integer percent (super≡+30, sub≡-25 by convention). Custom values give arbitrary vertical offset.' \
    --prop size=14 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=5in --prop width=13in --prop height=0.6in

# Per-run case rendering — cap on run Add accepts cap / allCaps / smallCaps
# directly (canonical + aliases). Same enum surface as the shape-level Set.
officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop x=0.5in --prop y=5.9in --prop width=13in --prop height=0.8in \
    --prop text="" --prop size=20 --prop bold=true

officecli add "$PPTX" '/slide[5]/shape[4]/p[1]' --type run --prop text="default  "
officecli add "$PPTX" '/slide[5]/shape[4]/p[1]' --type run \
    --prop text="small caps  " --prop cap=small --prop color=2A9D8F
officecli add "$PPTX" '/slide[5]/shape[4]/p[1]' --type run \
    --prop text="ALL CAPS" --prop allCaps=true --prop color=E63946

officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop text='Per-run cap=small / cap=all / cap=none, plus allCaps / smallCaps boolean aliases.' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=6.8in --prop width=13in --prop height=0.5in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 6 — name / zorder / autoFit / direction / font.cs (textbox-specific)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop text="name / zorder / autoFit / direction / font.cs" \
    --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=13in --prop height=0.6in

# name= — label the textbox so it can be re-addressed by @name later
officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop x=0.5in --prop y=1.2in --prop width=5in --prop height=1.5in \
    --prop fill=F1FAEE --prop size=16 --prop bold=true \
    --prop text="This is intro-box." \
    --prop name="intro-box"

officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop text='name="intro-box"  → addressable as /slide[6]/shape[@name=intro-box]' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=2.8in --prop width=5in --prop height=0.5in

# zorder= — three overlapping textboxes with explicit stack order
officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop x=6in --prop y=1.2in --prop width=3in --prop height=2in \
    --prop fill=4472C4 --prop color=FFFFFF --prop bold=true --prop size=16 \
    --prop text="back (zorder=1)" \
    --prop name="tb-back" --prop zorder=1

officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop x=7in --prop y=1.6in --prop width=3in --prop height=2in \
    --prop fill=E63946 --prop color=FFFFFF --prop bold=true --prop size=16 \
    --prop text="mid (zorder=2)" \
    --prop name="tb-mid" --prop zorder=2

officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop x=8in --prop y=2.0in --prop width=3in --prop height=2in \
    --prop fill=2A9D8F --prop color=FFFFFF --prop bold=true --prop size=16 \
    --prop text="front (zorder=3)" \
    --prop name="tb-front" --prop zorder=3

officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop text='zorder=  controls stack depth; aliases: z-order, order.' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=6in --prop y=4.2in --prop width=5in --prop height=0.5in

# autoFit= — overflow behavior for textbox (same as shape)
LONGTEXT='Vivamus lacinia odio vitae vestibulum vestibulum. Sed molestie augue sit amet leo consequat posuere.'
officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop x=0.5in --prop y=3.6in --prop width=3in --prop height=1.2in \
    --prop fill=FFE66D --prop size=16 --prop text="$LONGTEXT" \
    --prop autoFit=normal

officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop text='autoFit=normal  (shrinks text to fit)' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=4.9in --prop width=3in --prop height=0.5in

# direction=rtl — paragraph flows right-to-left inside a textbox
officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop x=0.5in --prop y=5.6in --prop width=5in --prop height=1.2in \
    --prop fill=A8DADC --prop size=20 --prop bold=true \
    --prop text="مرحبا بالعالم — 2026" \
    --prop direction=rtl --prop align=right \
    --prop font.cs="Arabic Typesetting"

officecli add "$PPTX" '/slide[6]' --type textbox \
    --prop text='direction=rtl + font.cs="Arabic Typesetting"  (complex-script slot)' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=6.9in --prop width=5in --prop height=0.5in

officecli close "$PPTX"
officecli validate "$PPTX"
echo "Created: $PPTX"
