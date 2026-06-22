#!/bin/bash
# Shape typography — paragraph spacing, character spacing, kerning, case, BCP-47 lang,
# RTL direction, complex-script (Arabic) font slot.
# Covers the typography props NOT touched by textboxes-basic.

set -e

DIR="$(dirname "$0")"
PPTX="$DIR/shapes-typography.pptx"

rm -f "$PPTX"
officecli create "$PPTX"
officecli open "$PPTX"

LOREM='Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt.'

# ─────────────────────────────────────────────────────────────────────────────
# Slide 1 — Paragraph spacing (lineSpacing / spaceBefore / spaceAfter)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text="lineSpacing / spaceBefore / spaceAfter" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Reference (tight spacing) — default
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop x=0.5in --prop y=1.2in --prop width=4in --prop height=3.5in \
    --prop fill=F1FAEE --prop size=14 --prop text="$LOREM"
officecli add "$PPTX" '/slide[1]/shape[2]' --type paragraph --prop text="$LOREM"
officecli add "$PPTX" '/slide[1]/shape[2]' --type paragraph --prop text="$LOREM"

officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text='default (no spacing props set)' --prop size=12 --prop italic=true \
    --prop x=0.5in --prop y=4.8in --prop width=4in --prop height=0.4in

# lineSpacing=1.5x
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop x=5in --prop y=1.2in --prop width=4in --prop height=3.5in \
    --prop fill=A8DADC --prop size=14 --prop text="$LOREM" --prop lineSpacing=1.5x
officecli add "$PPTX" '/slide[1]/shape[4]' --type paragraph --prop text="$LOREM" --prop lineSpacing=1.5x
officecli add "$PPTX" '/slide[1]/shape[4]' --type paragraph --prop text="$LOREM" --prop lineSpacing=1.5x

officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text='lineSpacing=1.5x  (multiplier; also accepts 150% / 18pt)' \
    --prop size=12 --prop italic=true \
    --prop x=5in --prop y=4.8in --prop width=4in --prop height=0.4in

# spaceBefore + spaceAfter on each paragraph
officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop x=9.5in --prop y=1.2in --prop width=4in --prop height=3.5in \
    --prop fill=F4A261 --prop size=14 --prop text="$LOREM" \
    --prop spaceBefore=12pt --prop spaceAfter=12pt
officecli add "$PPTX" '/slide[1]/shape[6]' --type paragraph --prop text="$LOREM" \
    --prop spaceBefore=12pt --prop spaceAfter=12pt
officecli add "$PPTX" '/slide[1]/shape[6]' --type paragraph --prop text="$LOREM" \
    --prop spaceBefore=12pt --prop spaceAfter=12pt

officecli add "$PPTX" '/slide[1]' --type textbox \
    --prop text='spaceBefore=12pt  spaceAfter=12pt' --prop size=12 --prop italic=true \
    --prop x=9.5in --prop y=4.8in --prop width=4in --prop height=0.4in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 2 — Character spacing, kerning, all/small caps
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop text="spacing / kern / cap" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Character spacing (1/100 pt; positive = looser, negative = tighter)
SAMPLE='Tight Loose Spacing TYPOGRAPHY'

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=13in --prop height=0.8in \
    --prop size=22 --prop bold=true --prop text="$SAMPLE  (default)"

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=2.3in --prop width=13in --prop height=0.8in \
    --prop size=22 --prop bold=true --prop text="$SAMPLE  (spacing=-50)" \
    --prop spacing=-50

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=3.1in --prop width=13in --prop height=0.8in \
    --prop size=22 --prop bold=true --prop text="$SAMPLE  (spacing=200)" \
    --prop spacing=200

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=3.9in --prop width=13in --prop height=0.8in \
    --prop size=22 --prop bold=true --prop text="$SAMPLE  (spacing=500)" \
    --prop spacing=500

# Kerning threshold — minimum font size (in 1/100 pt) at which kerning kicks in
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=5in --prop width=6in --prop height=0.8in \
    --prop size=18 --prop text="AVATAR  Yawning  (kern=1)  — kern on from 0.01pt" \
    --prop kern=1

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=7in --prop y=5in --prop width=6in --prop height=0.8in \
    --prop size=18 --prop text="AVATAR  Yawning  (kern=0)  — kern OFF" \
    --prop kern=0

# Case rendering
officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=0.5in --prop y=6in --prop width=4in --prop height=0.8in \
    --prop size=18 --prop text="cap=none — Default case" --prop cap=none

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=4.7in --prop y=6in --prop width=4in --prop height=0.8in \
    --prop size=18 --prop text="cap=small — Small caps" --prop cap=small

officecli add "$PPTX" '/slide[2]' --type textbox \
    --prop x=8.9in --prop y=6in --prop width=4in --prop height=0.8in \
    --prop size=18 --prop text="cap=all — All caps" --prop cap=all

# ─────────────────────────────────────────────────────────────────────────────
# Slide 3 — direction=rtl + font.cs (Arabic / complex-script)
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text="direction=rtl + font.cs (complex script)" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# LTR Arabic — punctuation/digits end up in left-to-right order
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=6in --prop height=1.2in \
    --prop fill=F1FAEE --prop size=24 --prop bold=true \
    --prop text="مرحبا بالعالم — 2026" \
    --prop font.cs="Arabic Typesetting"

officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text='direction=ltr (default)  +  font.cs="Arabic Typesetting"' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=2.8in --prop width=6in --prop height=0.4in

# RTL Arabic — paragraph flows right-to-left
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop x=7in --prop y=1.5in --prop width=6in --prop height=1.2in \
    --prop fill=A8DADC --prop size=24 --prop bold=true \
    --prop text="مرحبا بالعالم — 2026" \
    --prop direction=rtl --prop font.cs="Arabic Typesetting" --prop align=right

officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text='direction=rtl  +  align=right  (aliases: dir, rtl)' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=7in --prop y=2.8in --prop width=6in --prop height=0.4in

# Hebrew
officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop x=0.5in --prop y=3.7in --prop width=12.5in --prop height=1.5in \
    --prop fill=F4A261 --prop size=24 --prop bold=true \
    --prop text="שלום עולם — Hebrew demo" \
    --prop direction=rtl --prop font.cs="Arial Hebrew" --prop align=right

officecli add "$PPTX" '/slide[3]' --type textbox \
    --prop text='Same RTL machinery covers Hebrew, Urdu, Persian etc. — pick the appropriate font.cs face.' \
    --prop size=12 --prop italic=true --prop color=666666 \
    --prop x=0.5in --prop y=5.3in --prop width=12.5in --prop height=0.4in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 4 — Bare 'font' + BCP-47 lang tag
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text="font (bare) + lang BCP-47" --prop size=28 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=12in --prop height=0.6in

# Bare 'font' targets BOTH Latin and EastAsian slots in one shot
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=1.5in --prop width=6in --prop height=1.5in \
    --prop fill=F1FAEE --prop size=22 \
    --prop text="Bare font sets Latin + EastAsian" \
    --prop font="Times New Roman"

officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text='font="Times New Roman"  (sets a:latin AND a:ea)' \
    --prop size=12 --prop italic=true \
    --prop x=0.5in --prop y=3.1in --prop width=6in --prop height=0.4in

officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=7in --prop y=1.5in --prop width=6in --prop height=1.5in \
    --prop fill=A8DADC --prop size=22 \
    --prop text="Per-script gives finer control" \
    --prop font.latin="Georgia" --prop font.ea="Yu Mincho"

officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text='font.latin=Georgia + font.ea="Yu Mincho"  (a:latin / a:ea)' \
    --prop size=12 --prop italic=true \
    --prop x=7in --prop y=3.1in --prop width=6in --prop height=0.4in

# BCP-47 language tags — affects spellcheck, hyphenation, font fallback
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=0.5in --prop y=3.8in --prop width=4in --prop height=1in \
    --prop fill=F4A261 --prop size=18 --prop text="Color or colour?" --prop lang=en-GB
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text='lang=en-GB  (British English spellcheck)' --prop size=12 --prop italic=true \
    --prop x=0.5in --prop y=4.9in --prop width=4in --prop height=0.4in

officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=4.7in --prop y=3.8in --prop width=4in --prop height=1in \
    --prop fill=F4A261 --prop size=18 --prop text="Couleur en français" --prop lang=fr-FR
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text='lang=fr-FR' --prop size=12 --prop italic=true \
    --prop x=4.7in --prop y=4.9in --prop width=4in --prop height=0.4in

officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop x=8.9in --prop y=3.8in --prop width=4in --prop height=1in \
    --prop fill=F4A261 --prop size=18 --prop text="日本語のテスト" --prop lang=ja-JP \
    --prop font.ea="Yu Mincho"
officecli add "$PPTX" '/slide[4]' --type textbox \
    --prop text='lang=ja-JP + font.ea="Yu Mincho"' --prop size=12 --prop italic=true \
    --prop x=8.9in --prop y=4.9in --prop width=4in --prop height=0.4in

# ─────────────────────────────────────────────────────────────────────────────
# Slide 5 — strike / underline / valign / margin / list / lineOpacity / animation
# ─────────────────────────────────────────────────────────────────────────────
officecli add "$PPTX" / --type slide
officecli add "$PPTX" '/slide[5]' --type textbox \
    --prop text="strike / underline / valign / margin / list / lineOpacity / animation" \
    --prop size=22 --prop bold=true \
    --prop x=0.5in --prop y=0.3in --prop width=13in --prop height=0.6in

# strike + underline — set at shape level (applied to all runs as a default)
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=roundRect \
    --prop x=0.5in --prop y=1.2in --prop width=3.5in --prop height=1.2in \
    --prop fill=F4A261 --prop color=000000 --prop size=18 \
    --prop text="strike=single" \
    --prop strike=single

officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=roundRect \
    --prop x=4.3in --prop y=1.2in --prop width=3.5in --prop height=1.2in \
    --prop fill=A8DADC --prop color=000000 --prop size=18 \
    --prop text="underline=single" \
    --prop underline=single

# valign — vertical text position inside the shape (top / middle / bottom)
for va in top middle bottom; do
    case $va in top) X=0.5 ;; middle) X=4.3 ;; bottom) X=8.1 ;; esac
    officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
        --prop x="${X}in" --prop y=2.6in --prop width=3.5in --prop height=2in \
        --prop fill=DEEAF6 --prop lineColor=4472C4 --prop lineWidth=2pt \
        --prop text="valign=$va" --prop size=16 --prop bold=true \
        --prop valign="$va"
done

# Bottom area laid out as a 2x2 grid (left column x=0.5, right column x=5.0)
# so no boxes overlap.

# margin — inner text padding (uniform; also accepts per-edge via marginLeft etc.)
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=roundRect \
    --prop x=0.5in --prop y=4.8in --prop width=4in --prop height=1.3in \
    --prop fill=F1FAEE --prop lineColor=2A9D8F --prop lineWidth=2pt \
    --prop text="margin=0.4in  — large inner padding" --prop size=16 \
    --prop margin=0.4in

# list — shape-level bullet/numbered list. Pass every item as ONE multiline
# text block so the list style applies to all paragraphs; paragraphs added
# after creation do NOT inherit the shape's list style.
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
    --prop x=5in --prop y=4.8in --prop width=4.2in --prop height=1.5in \
    --prop fill=F4A261 --prop color=000000 --prop size=14 \
    --prop list=bullet \
    --prop text="First item
Second item
Third item"

# lineOpacity — outline transparency (0=opaque … 1=invisible); needs a non-none line
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=rect \
    --prop x=0.5in --prop y=6.4in --prop width=4in --prop height=0.95in \
    --prop fill=4472C4 --prop lineColor=E63946 --prop lineWidth=6pt \
    --prop lineOpacity=0.35 \
    --prop text="lineOpacity=0.35" --prop color=FFFFFF --prop size=14

# animation — shape entrance animation (see animations.sh for full coverage)
officecli add "$PPTX" '/slide[5]' --type shape --prop geometry=roundRect \
    --prop x=5in --prop y=6.4in --prop width=4.2in --prop height=0.95in \
    --prop fill=E63946 --prop color=FFFFFF --prop size=14 --prop bold=true \
    --prop text="animation=fadeIn" \
    --prop animation=fadeIn

officecli close "$PPTX"
officecli validate "$PPTX"
echo "Created: $PPTX"
