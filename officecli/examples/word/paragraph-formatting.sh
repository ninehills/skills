#!/bin/bash
# paragraph-formatting.sh — exercise the docx paragraph property surface.
#
# Sections: alignment, indentation, spacing, pagination flags, paragraph-level
# run formatting (applied to every run), shading, the paragraph-mark run
# properties (markRPr.* — formatting of the ¶ glyph itself), and outline level.
#
# Note: paragraph-level `bold` etc. apply to all runs in the paragraph and read
# back on the paragraph; `markRPr.bold` formats only the paragraph mark and is
# distinct. Both are settable + gettable.
set -e

DOCX="$(dirname "$0")/paragraph-formatting.docx"
echo "Building $DOCX ..."
rm -f "$DOCX"
officecli create "$DOCX"

heading() { officecli add "$DOCX" /body --type paragraph --prop "text=$1" --prop bold=true --prop size=14 --prop color=1F4E79 --prop spaceBefore=10pt; }

officecli add "$DOCX" /body --type paragraph --prop "text=Paragraph Formatting Showcase" --prop align=center --prop bold=true --prop size=20

# --- alignment ---
heading "Alignment"
officecli add "$DOCX" /body --type paragraph --prop "text=Left aligned (default)" --prop align=left
officecli add "$DOCX" /body --type paragraph --prop "text=Center aligned" --prop align=center
officecli add "$DOCX" /body --type paragraph --prop "text=Right aligned" --prop align=right
officecli add "$DOCX" /body --type paragraph --prop "text=Justified text stretched edge to edge across the full measure of the line so both margins align." --prop align=both

# --- indentation ---
heading "Indentation"
officecli add "$DOCX" /body --type paragraph --prop "text=Left indent 1cm" --prop indent=1cm
officecli add "$DOCX" /body --type paragraph --prop "text=Right indent 2cm so the right edge pulls in." --prop rightIndent=2cm
officecli add "$DOCX" /body --type paragraph --prop "text=First-line indent — only the first line is pushed in from the left margin." --prop firstLineIndent=1cm
officecli add "$DOCX" /body --type paragraph --prop "text=Hanging indent — the first line hangs left while the rest of the paragraph is indented." --prop indent=1cm --prop hangingIndent=1cm

# --- spacing ---
heading "Spacing"
officecli add "$DOCX" /body --type paragraph --prop "text=Space before 18pt, after 6pt" --prop spaceBefore=18pt --prop spaceAfter=6pt
officecli add "$DOCX" /body --type paragraph --prop "text=Line spacing 1.5x across a longer paragraph that wraps so the extra leading between wrapped lines is visible." --prop lineSpacing=1.5x

# --- pagination flags ---
heading "Pagination flags"
officecli add "$DOCX" /body --type paragraph --prop "text=keepNext — stays with the following paragraph" --prop keepNext=true
officecli add "$DOCX" /body --type paragraph --prop "text=keepLines — lines stay together, never split across pages" --prop keepLines=true
officecli add "$DOCX" /body --type paragraph --prop "text=widowControl on" --prop widowControl=true

# --- paragraph-level run formatting (applies to all runs) ---
heading "Paragraph-level run formatting"
officecli add "$DOCX" /body --type paragraph --prop "text=Whole paragraph bold + red + 13pt" --prop bold=true --prop color=C00000 --prop size=13
officecli add "$DOCX" /body --type paragraph --prop "text=Whole paragraph italic + highlighted" --prop italic=true --prop highlight=yellow

# --- shading ---
heading "Shading"
officecli add "$DOCX" /body --type paragraph --prop "text=Light gray paragraph shading" --prop shading.fill=D9D9D9
officecli add "$DOCX" /body --type paragraph --prop "text=Pale blue shading" --prop shading.fill=DDEBF7

# --- paragraph-mark run props (the pilcrow itself) ---
heading "Paragraph-mark formatting (markRPr)"
officecli add "$DOCX" /body --type paragraph --prop "text=The mark glyph is bold+red (distinct from run text)" --prop markRPr.bold=true --prop markRPr.color=C00000

# --- outline level ---
heading "Outline level"
officecli add "$DOCX" /body --type paragraph --prop "text=Outline level 1 (shows in document map)" --prop outlineLvl=1

# --- paragraph-level run formatting: strike / underline ---
heading "Paragraph strike & underline"
officecli add "$DOCX" /body --type paragraph --prop "text=Whole paragraph struck out" --prop strike=true
officecli add "$DOCX" /body --type paragraph --prop "text=Underlined paragraph (red wave)" --prop underline=wave --prop underline.color=#FF0000

# --- complex-script run props on the paragraph ---
heading "Complex-script (cs)"
officecli add "$DOCX" /body --type paragraph --prop "text=cs bold/italic/14pt + RTL" --prop bold.cs=true --prop italic.cs=true --prop size.cs=14pt --prop direction=rtl

# --- spacing & pagination extras ---
heading "Spacing & pagination extras"
officecli add "$DOCX" /body --type paragraph --prop "text=contextualSpacing (collapse between same-style paras)" --prop contextualSpacing=true
officecli add "$DOCX" /body --type paragraph --prop "text=lineSpacing 14pt, lineRule=atLeast" --prop lineSpacing=14pt --prop lineRule=atLeast
officecli add "$DOCX" /body --type paragraph --prop "text=pageBreakBefore" --prop pageBreakBefore=true
officecli add "$DOCX" /body --type paragraph --prop "text=wordWrap off (break long URLs anywhere)" --prop wordWrap=false

# --- chars-based indentation (CJK 1/100-char units) ---
heading "Chars-based indent"
officecli add "$DOCX" /body --type paragraph --prop "text=first-line 200 chars, hanging 100 chars" --prop firstLineChars=200 --prop hangingChars=100

# --- fonts: explicit per-script + theme references ---
heading "Fonts (explicit & theme)"
officecli add "$DOCX" /body --type paragraph --prop "text=font shorthand Times New Roman" --prop font="Times New Roman"
officecli add "$DOCX" /body --type paragraph --prop "text=per-script latin/ea/cs" --prop font.latin=Calibri --prop font.ea=SimSun --prop font.cs=Arial
officecli add "$DOCX" /body --type paragraph --prop "text=theme fonts" --prop font.asciiTheme=minorHAnsi --prop font.hAnsiTheme=minorHAnsi --prop font.eaTheme=minorEastAsia --prop font.csTheme=minorBidi

# --- styles ---
heading "Styles"
officecli add "$DOCX" /body --type paragraph --prop "text=Paragraph style Heading1" --prop style=Heading1
officecli add "$DOCX" /body --type paragraph --prop "text=Character style on the run" --prop rStyle=Emphasis

# --- shading variants (shd shorthand + decomposed val/color) ---
heading "Shading variants"
officecli add "$DOCX" /body --type paragraph --prop "text=shd shorthand (yellow)" --prop shd=FFFF00
officecli add "$DOCX" /body --type paragraph --prop "text=pct15 pattern, blue fill, red pattern color" --prop shading.val=pct15 --prop shading.fill=DDEBF7 --prop shading.color=C00000

# --- tab stops ---
heading "Tab stops"
officecli add "$DOCX" /body --type paragraph --prop "text=Tabs at 720 and 1440 twips" --prop tabs=720,1440

# --- paragraph-mark run props (full markRPr set) ---
heading "Paragraph-mark formatting (full markRPr)"
officecli add "$DOCX" /body --type paragraph --prop "text=mark: italic/strike/underline/size/highlight/fonts" \
    --prop markRPr.italic=true --prop markRPr.strike=true --prop markRPr.underline=single \
    --prop markRPr.size=14pt --prop markRPr.highlight=yellow \
    --prop markRPr.font.latin=Georgia --prop markRPr.font.ea=SimSun --prop markRPr.font.cs=Arial

# --- text frame / drop-cap frame (framePr) ---
# A framed paragraph floats in its own box (twips for w/h/hSpace/vSpace);
# wrap=around lets body text flow around it, anchored to the margin.
heading "Text frame (framePr)"
officecli add "$DOCX" /body --type paragraph --prop "text=Framed paragraph — floats in a 3-inch box with text wrapping around it, anchored to the margin." \
    --prop framePr.w=4320 --prop framePr.h=720 --prop framePr.wrap=around \
    --prop framePr.hAnchor=margin --prop framePr.vAnchor=text \
    --prop framePr.hSpace=180 --prop framePr.vSpace=180

# --- paragraph borders (pBdr) ---
# Whole-box forms only (`border=...`); per-side `border.top=` works but the
# handler reports it as an unsupported prop, so the trio sticks to the box form.
heading "Paragraph borders (pBdr)"
officecli add "$DOCX" /body --type paragraph --prop "text=Box border, all sides (single)" --prop border=single
officecli add "$DOCX" /body --type paragraph --prop "text=Red 1pt box (style;size;color)" --prop "border=single;8;FF0000"

# --- vertical text alignment within the line ---
heading "Vertical text alignment"
officecli add "$DOCX" /body --type paragraph --prop "text=textAlignment=center (glyphs centered on the line box)" --prop textAlignment=center
officecli add "$DOCX" /body --type paragraph --prop "text=textAlignment=top" --prop textAlignment=top

# --- EastAsian typography toggles (handled via the generic fallback) ---
heading "EastAsian typography"
officecli add "$DOCX" /body --type paragraph --prop "text=kinsoku off — permit breaks at forbidden CJK chars" --prop kinsoku=false
officecli add "$DOCX" /body --type paragraph --prop "text=autoSpace off — no auto gap between CJK and Latin/digits" --prop autoSpaceDE=false --prop autoSpaceDN=false
officecli add "$DOCX" /body --type paragraph --prop "text=overflowPunct + topLinePunct on" --prop overflowPunct=true --prop topLinePunct=true

# --- line / hyphenation / indent flags ---
heading "Line & indent flags"
officecli add "$DOCX" /body --type paragraph --prop "text=suppressLineNumbers + suppressAutoHyphens" --prop suppressLineNumbers=true --prop suppressAutoHyphens=true
officecli add "$DOCX" /body --type paragraph --prop "text=mirrorIndents on, adjustRightInd off, snapToGrid off" --prop mirrorIndents=true --prop adjustRightInd=false --prop snapToGrid=false

# --- web / textbox layout hints ---
heading "Web / textbox hints"
officecli add "$DOCX" /body --type paragraph --prop "text=divId (web division id) + textboxTightWrap=allLines" --prop divId=123456 --prop textboxTightWrap=allLines

# --- list numbering (auto-created via listStyle; numId/numLevel reference it) ---
heading "List numbering"
officecli add "$DOCX" /body --type paragraph --prop "text=Bulleted item" --prop listStyle=bullet
officecli add "$DOCX" /body --type paragraph --prop "text=Ordered item starting at 5" --prop listStyle=ordered --prop start=5
officecli add "$DOCX" /body --type paragraph --prop "text=Explicit numId=1 level 0" --prop numId=1 --prop numLevel=0

officecli validate "$DOCX"
echo "Created: $DOCX"
