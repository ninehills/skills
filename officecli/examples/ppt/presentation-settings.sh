#!/bin/bash
# presentation-settings.sh — exercise the full pptx `presentation` property
# surface (schemas/help/pptx/presentation.json) using the officecli CLI directly.
#
# `presentation` is a read-only container at path "/"; you only set/get it. Six
# groups: metadata, slide setup, print, slideshow, privacy, theme. CLI twin of
# presentation-settings.py (officecli SDK); both produce an equivalent
# presentation-settings.pptx.
set -e

FILE="$(dirname "$0")/presentation-settings.pptx"
echo "Building $FILE ..."
rm -f "$FILE"
officecli create "$FILE"
officecli open "$FILE"

# --- A title slide (blank pptx has master+layouts but no slides) ---
officecli add "$FILE" / --type slide
officecli add "$FILE" "/slide[1]" --type shape --prop geometry=rect \
  --prop left=2cm --prop top=3cm --prop width=26cm --prop height=4cm \
  --prop fill=accent1 --prop text="Presentation Settings" \
  --prop fontSize=40 --prop color=FFFFFF --prop bold=true

# --- 1. Metadata (core + extended) ---
officecli set "$FILE" / --prop author="Jane Author" --prop title="Q4 Business Review" \
  --prop subject=Strategy --prop keywords="q4,review,strategy" \
  --prop description="Quarterly business review deck." --prop category=Marketing \
  --prop lastModifiedBy=Editorial --prop revisionNumber=3
officecli set "$FILE" / --prop extended.company="Acme Corp" \
  --prop extended.manager="Dana Lead" --prop extended.template="Widescreen.potx"

# --- 2. Slide setup (slideSize preset; explicit slideWidth/Height = custom) ---
officecli set "$FILE" / --prop slideSize=widescreen \
  --prop firstSlideNum=1 --prop rtl=false --prop compatMode=false

# --- 3. Print ---
officecli set "$FILE" / --prop print.what=slides --prop print.colorMode=color \
  --prop print.frameSlides=true --prop print.hiddenSlides=false --prop print.scaleToFitPaper=true

# --- 4. Slideshow ---
officecli set "$FILE" / --prop show.loop=false --prop show.narration=true \
  --prop show.animation=true --prop show.useTimings=true

# --- 5. Privacy ---
officecli set "$FILE" / --prop removePersonalInfo=false

# --- 6. Theme — palette (dk/lt + accent1..6) and major/minor fonts ---
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

officecli close "$FILE"
officecli validate "$FILE"
echo "Created: $FILE"
