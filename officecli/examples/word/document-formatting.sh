#!/bin/bash
# document-formatting.sh — exercise the full docx `document` property surface
# (schemas/help/docx/document.json) using the officecli CLI directly.
#
# `document` is a read-only container at path "/"; you only set/get it. Seven
# groups: metadata, page setup, docDefaults, theme, CJK grid, font embedding,
# display/privacy. CLI twin of document-formatting.py (officecli SDK); both
# produce an equivalent document-formatting.docx.
set -e

FILE="$(dirname "$0")/document-formatting.docx"
echo "Building $FILE ..."
rm -f "$FILE"
officecli create "$FILE"
officecli open "$FILE"

# --- Body: unstyled paragraphs inherit the docDefaults set below ---
officecli add "$FILE" /body --type paragraph --prop text="Document Formatting Showcase" --prop style=Title
officecli add "$FILE" /body --type paragraph --prop text="This heading uses the theme major font." --prop style=Heading1
officecli add "$FILE" /body --type paragraph --prop text="This body paragraph carries NO run formatting, so it renders in the document defaults: Georgia 12pt, dark slate — set via docDefaults.* on the document, not on the run."
officecli add "$FILE" /body --type paragraph --prop text="A second default paragraph, to show the inherited line spacing and space-after also come from docDefaults."

# --- 1. Metadata (core + extended) ---
officecli set "$FILE" / --prop author="Jane Author" --prop title="Q3 Field Report" \
  --prop subject=Finance --prop keywords="report,q3,finance" \
  --prop description="Quarterly field summary." --prop lastModifiedBy=Editorial
officecli set "$FILE" / --prop extended.company="Acme Corp" \
  --prop extended.manager="Dana Lead" --prop extended.template="Normal.dotm"

# --- 2. Page setup (A4 portrait, mirrored margins) ---
officecli set "$FILE" / --prop pageWidth=21cm --prop pageHeight=29.7cm --prop orientation=portrait \
  --prop marginTop=2.54cm --prop marginBottom=2.54cm \
  --prop marginLeft=3.18cm --prop marginRight=3.18cm \
  --prop marginHeader=1.5cm --prop marginFooter=1.75cm --prop marginGutter=0cm
officecli set "$FILE" / --prop mirrorMargins=true --prop gutterAtTop=false --prop bookFoldPrinting=false

# --- 3. docDefaults (defaults unstyled text inherits) ---
officecli set "$FILE" / \
  --prop docDefaults.font=Georgia --prop docDefaults.font.eastAsia=SimSun \
  --prop docDefaults.font.hAnsi=Georgia --prop docDefaults.font.complexScript=Arial \
  --prop docDefaults.fontSize=12 --prop docDefaults.color=2F3640 \
  --prop docDefaults.bold=false --prop docDefaults.italic=false --prop docDefaults.rtl=false \
  --prop docDefaults.alignment=left \
  --prop docDefaults.spaceBefore=0pt --prop docDefaults.spaceAfter=8pt --prop docDefaults.lineSpacing=1.15x

# --- 4. Theme — palette (dk/lt + accent1..6) and major/minor fonts ---
officecli set "$FILE" / \
  --prop theme.color.dk1=1A1A1A --prop theme.color.lt1=FFFFFF \
  --prop theme.color.dk2=2F3640 --prop theme.color.lt2=EEF1F5 \
  --prop theme.color.accent1=1F6FEB --prop theme.color.accent2=E3572A \
  --prop theme.color.accent3=2DA44E --prop theme.color.accent4=BF8700 \
  --prop theme.color.accent5=8250DF --prop theme.color.accent6=1B7C83 \
  --prop theme.color.hlink=0969DA --prop theme.color.folHlink=8250DF
officecli set "$FILE" / \
  --prop theme.font.major.latin=Georgia --prop theme.font.minor.latin=Calibri \
  --prop theme.font.major.eastAsia=SimHei --prop theme.font.minor.eastAsia=SimSun

# --- 5. CJK grid & spacing ---
officecli set "$FILE" / --prop docGrid.type=linesAndChars --prop docGrid.linePitch=312 \
  --prop docGrid.charSpace=0 --prop charSpacingControl=compressPunctuation \
  --prop autoSpaceDE=true --prop autoSpaceDN=true --prop kinsoku=true --prop overflowPunct=true

# --- 6. Font embedding ---
officecli set "$FILE" / --prop embedFonts=true --prop embedSystemFonts=false --prop saveSubsetFonts=true

# --- 7. Display / print / privacy ---
officecli set "$FILE" / --prop evenAndOddHeaders=true --prop autoHyphenation=false \
  --prop defaultTabStop=720 --prop displayBackgroundShape=true \
  --prop removePersonalInformation=false --prop removeDateAndTime=false --prop printFormsData=false
officecli set "$FILE" / --prop compatibility.mode=15

officecli close "$FILE"
officecli validate "$FILE"
echo "Created: $FILE"
