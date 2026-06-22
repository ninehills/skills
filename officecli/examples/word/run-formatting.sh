#!/bin/bash
# run-formatting.sh — exercise the docx run (character) property surface.
#
# Each paragraph demonstrates one family of run-level formatting. Most lines set
# the run formatting on the paragraph's implicit run via `add ... --type paragraph`;
# the super/subscript line uses explicit `--type run` children for mixed runs.
#
# Families: weight/style, underline variants + color, strike/dstrike, case
# (caps/smallCaps), vertical align (super/subscript), color/size/highlight,
# per-script fonts (latin/eastAsia/cs), text effects (emboss/imprint/outline/
# shadow/vanish), character spacing/kerning/position, and language tagging.
set -e

DOCX="$(dirname "$0")/run-formatting.docx"
echo "Building $DOCX ..."
rm -f "$DOCX"
officecli create "$DOCX"

heading() { officecli add "$DOCX" /body --type paragraph --prop "text=$1" --prop bold=true --prop size=14 --prop color=1F4E79 --prop spaceBefore=8pt; }

officecli add "$DOCX" /body --type paragraph --prop "text=Run / Character Formatting Showcase" --prop align=center --prop bold=true --prop size=20

# --- weight & style ---
heading "Weight & style"
officecli add "$DOCX" /body --type paragraph --prop "text=Bold text" --prop bold=true
officecli add "$DOCX" /body --type paragraph --prop "text=Italic text" --prop italic=true
officecli add "$DOCX" /body --type paragraph --prop "text=Bold + italic" --prop bold=true --prop italic=true

# --- underline variants + color ---
heading "Underline"
officecli add "$DOCX" /body --type paragraph --prop "text=single" --prop underline=single
officecli add "$DOCX" /body --type paragraph --prop "text=double" --prop underline=double
officecli add "$DOCX" /body --type paragraph --prop "text=thick" --prop underline=thick
officecli add "$DOCX" /body --type paragraph --prop "text=dotted" --prop underline=dotted
officecli add "$DOCX" /body --type paragraph --prop "text=wave (red)" --prop underline=wave --prop underline.color=FF0000

# --- strikethrough ---
heading "Strikethrough"
officecli add "$DOCX" /body --type paragraph --prop "text=single strike" --prop strike=true
officecli add "$DOCX" /body --type paragraph --prop "text=double strike" --prop dstrike=true

# --- case ---
heading "Case"
officecli add "$DOCX" /body --type paragraph --prop "text=all caps rendering" --prop caps=true
officecli add "$DOCX" /body --type paragraph --prop "text=small caps rendering" --prop smallcaps=true

# --- vertical align: super / subscript (mixed runs) ---
heading "Super / subscript"
officecli add "$DOCX" /body --type paragraph --prop "text=E = mc"
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=2" --prop superscript=true
officecli add "$DOCX" /body --type paragraph --prop "text=H"
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=2" --prop subscript=true
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=O"

# --- color / size / highlight ---
heading "Color, size, highlight"
officecli add "$DOCX" /body --type paragraph --prop "text=Red 16pt" --prop color=C00000 --prop size=16
officecli add "$DOCX" /body --type paragraph --prop "text=Highlighted" --prop highlight=yellow

# --- per-script fonts ---
heading "Per-script fonts"
officecli add "$DOCX" /body --type paragraph --prop "text=Latin Georgia + CJK 宋体" --prop font.latin=Georgia --prop font.eastAsia=SimSun --prop size=14

# --- text effects ---
heading "Text effects"
officecli add "$DOCX" /body --type paragraph --prop "text=emboss" --prop emboss=true
officecli add "$DOCX" /body --type paragraph --prop "text=imprint" --prop imprint=true
officecli add "$DOCX" /body --type paragraph --prop "text=outline" --prop outline=true
officecli add "$DOCX" /body --type paragraph --prop "text=shadow" --prop shadow=true

# --- character spacing / position ---
heading "Character spacing & position"
officecli add "$DOCX" /body --type paragraph --prop "text=expanded spacing" --prop charSpacing=2pt
officecli add "$DOCX" /body --type paragraph --prop "text=raised 3pt" --prop position=3pt

# --- language ---
heading "Language tag"
officecli add "$DOCX" /body --type paragraph --prop "text=Tagged en-US for spellcheck" --prop lang=en-US

# --- complex-script (cs) variants ---
heading "Complex-script variants"
officecli add "$DOCX" /body --type paragraph --prop "text=cs bold + italic + 14pt" --prop bold.cs=true --prop italic.cs=true --prop size.cs=14pt
officecli add "$DOCX" /body --type paragraph --prop "text=Right-to-left run" --prop rtl=true --prop direction=rtl

# --- theme fonts (resolve against the document theme) ---
heading "Theme fonts"
officecli add "$DOCX" /body --type paragraph --prop "text=Latin/CS/EA theme fonts" --prop font.asciiTheme=minorHAnsi --prop font.hAnsiTheme=minorHAnsi --prop font.csTheme=minorBidi --prop font.eaTheme=minorEastAsia

# --- explicit per-script fonts + the `font` shorthand ---
heading "Per-script font keys"
officecli add "$DOCX" /body --type paragraph --prop "text=font shorthand (all scripts)" --prop font=Calibri
officecli add "$DOCX" /body --type paragraph --prop "text=cs + ea explicit fonts" --prop font.cs="Arial" --prop font.ea="SimSun"

# --- per-script language tags ---
heading "Per-script language"
officecli add "$DOCX" /body --type paragraph --prop "text=lang per script (latin/ea/cs)" --prop lang.latin=en-US --prop lang.ea=zh-CN --prop lang.cs=ar-SA

# --- run shading & hidden text ---
heading "Run shading & hidden text"
officecli add "$DOCX" /body --type paragraph --prop "text=Yellow run shading" --prop shading=FFFF00
officecli add "$DOCX" /body --type paragraph --prop "text=Hidden (vanish) text" --prop vanish=true
officecli add "$DOCX" /body --type paragraph --prop "text=No-proof (spellcheck off)" --prop noproof=true

# --- vertical alignment (vertAlign enum alias) ---
heading "vertAlign enum"
officecli add "$DOCX" /body --type paragraph --prop "text=vertAlign=superscript" --prop vertAlign=superscript

# --- WordprocessingML 2010 (w14) text effects ---
heading "w14 text effects"
officecli add "$DOCX" /body --type paragraph --prop "text=Text fill color" --prop textFill=FF0000 --prop size=16
officecli add "$DOCX" /body --type paragraph --prop "text=Text outline" --prop textOutline=1pt-FF0000 --prop size=16
officecli add "$DOCX" /body --type paragraph --prop "text=w14 glow" --prop w14glow=FF0000 --prop size=16
officecli add "$DOCX" /body --type paragraph --prop "text=w14 reflection" --prop w14reflection=true --prop size=16
officecli add "$DOCX" /body --type paragraph --prop "text=w14 shadow" --prop w14shadow=FF0000 --prop size=16

# --- character border, kerning, EastAsian layout, run style ---
# kern / eastAsianLayout route to the paragraph's implicit run; bdr and a
# run-level rStyle must be set on explicit `--type run` children (on a
# paragraph, `bdr`/`rStyle` bind the paragraph border / paragraph-mark style).
heading "Border, kerning, EastAsian layout, run style"
officecli add "$DOCX" /body --type paragraph --prop "text=Kerning on (28 = 14pt threshold)" --prop kern=28
officecli add "$DOCX" /body --type paragraph --prop "text=EastAsian layout 縦中横 (vert + combine)" --prop eastAsianLayout.vert=true --prop eastAsianLayout.combine=true
officecli add "$DOCX" /body --type paragraph --prop "text=Boxed run: "
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=single border" --prop bdr=single
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=  red 0.5pt" --prop "bdr=single;4;FF0000;0"
officecli add "$DOCX" /body --type paragraph --prop "text=Run character style: "
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=Emphasis" --prop rStyle=Emphasis

# --- emphasis mark + legacy / visibility effects ---
# These run keys are handled by the generic typed-attribute fallback (no
# curated case in the handler) but still round-trip through add/set/get.
# em = 着重号 (East-Asian emphasis dots): dot=above, underDot=below, circle.
heading "Emphasis mark & visibility effects"
officecli add "$DOCX" /body --type paragraph --prop "text=着重号 dots above (em=dot)" --prop em=dot
officecli add "$DOCX" /body --type paragraph --prop "text=着重号 dots below (em=underDot)" --prop em=underDot
officecli add "$DOCX" /body --type paragraph --prop "text=Circle emphasis (em=circle)" --prop em=circle
officecli add "$DOCX" /body --type paragraph --prop "text=Legacy text animation (effect=blinkBackground)" --prop effect=blinkBackground
officecli add "$DOCX" /body --type paragraph --prop "text=Hidden in web layout (webHidden)" --prop webHidden=true
officecli add "$DOCX" /body --type paragraph --prop "text=Fit run to 1 inch (fitText=1440 twips)" --prop fitText=1440
# snapToGrid is also a paragraph property, so set it on an explicit run child to
# demonstrate the run-level flag unambiguously; specVanish is run-only.
officecli add "$DOCX" /body --type paragraph --prop "text=Layout grid + special vanish: "
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=snapToGrid=false" --prop snapToGrid=false
officecli add "$DOCX" "/body/p[last()]" --type run --prop "text=  specVanish" --prop specVanish=true

officecli validate "$DOCX"
echo "Created: $DOCX"
