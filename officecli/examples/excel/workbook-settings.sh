#!/bin/bash
# workbook-settings.sh — exercise the full xlsx `workbook` property surface
# (schemas/help/xlsx/workbook.json) using the officecli CLI directly.
#
# `workbook` is a read-only container at path "/"; you only set/get it. Four
# groups: metadata, calc engine, protection/display, theme. This is the CLI twin
# of workbook-settings.py (which drives the same writes over the officecli SDK);
# both produce an equivalent workbook-settings.xlsx.
set -e

FILE="$(dirname "$0")/workbook-settings.xlsx"
echo "Building $FILE ..."
rm -f "$FILE"
officecli create "$FILE"
officecli open "$FILE"          # resident mode: many sets in one process

# --- A small data sheet + a live formula (governed by calc.mode) ---
officecli set "$FILE" /Sheet1/A1 --prop value=Region --prop font.bold=true
officecli set "$FILE" /Sheet1/B1 --prop value=Units  --prop font.bold=true
officecli set "$FILE" /Sheet1/C1 --prop value=Price  --prop font.bold=true
officecli set "$FILE" /Sheet1/D1 --prop value=Revenue --prop font.bold=true
i=2
for row in "North 120 9.5" "South 95 11.0" "East 140 8.75"; do
  set -- $row
  officecli set "$FILE" "/Sheet1/A$i" --prop value="$1"
  officecli set "$FILE" "/Sheet1/B$i" --prop value="$2"
  officecli set "$FILE" "/Sheet1/C$i" --prop value="$3"
  officecli set "$FILE" "/Sheet1/D$i" --prop formula="=B$i*C$i" --prop numberformat='$#,##0.00'
  i=$((i+1))
done
officecli set "$FILE" "/Sheet1/D$((i))" --prop formula="=SUM(D2:D$((i-1)))" --prop font.bold=true --prop numberformat='$#,##0.00'

# --- 1. Metadata (core + extended) ---
officecli set "$FILE" / --prop author="Jane Author" --prop title="2026 Revenue Model" \
  --prop subject=Finance --prop keywords="finance,2026,model" \
  --prop description="Annual revenue summary." --prop category=Reports \
  --prop lastModifiedBy=Editorial --prop revisionNumber=3
officecli set "$FILE" / --prop extended.company="Acme Corp" \
  --prop extended.manager="Dana Lead" --prop extended.template="Book.xltx"

# --- 2. Calc engine ---
officecli set "$FILE" / --prop calc.mode=manual --prop calc.iterate=true \
  --prop calc.iterateCount=100 --prop calc.iterateDelta=0.001 --prop calc.fullPrecision=true

# --- 3. Protection & display ---
officecli set "$FILE" / --prop workbook.lockStructure=true --prop workbook.lockWindows=false \
  --prop workbook.password=secret --prop workbook.dateCompatibility=false \
  --prop workbook.filterPrivacy=true --prop workbook.showObjects=all

# --- 4. Theme — palette accents (dk/lt + accent1..6) and major/minor fonts ---
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

officecli close "$FILE"          # flush resident to disk
officecli validate "$FILE"
echo "Created: $FILE"
