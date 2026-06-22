#!/bin/bash
# conditional-formatting.sh — exercise the full xlsx `conditionalformatting` rule
# family (schemas/help/xlsx/conditionalformatting.json) using the officecli CLI.
#
# 7 sheets, one rule family each: cellIs, text, top/bottom/average, data bars,
# colour scales, icon sets, formula/date/dup-unique. CLI twin of
# conditional-formatting.py (officecli SDK); both produce an equivalent
# conditional-formatting.xlsx.
#
# Each rule is one `add` against the sheet, with type= selecting the rule kind
# and ref= the target range. The fill lands in the workbook <dxfs> table.
set -e

FILE="$(dirname "$0")/conditional-formatting.xlsx"
echo "Building $FILE ..."
rm -f "$FILE"
officecli create "$FILE"
officecli open "$FILE"

# helper: write a header in A1 then a column of values from A2 down
col() {  # col <sheet> <title> <v1> <v2> ...
  local sheet="$1" title="$2"; shift 2
  officecli set "$FILE" "/$sheet/A1" --prop value="$title" --prop font.bold=true --prop fill=1F4E79 --prop font.color=FFFFFF
  local i=2
  for v in "$@"; do officecli set "$FILE" "/$sheet/A$i" --prop value="$v"; i=$((i+1)); done
}
cf() { officecli add "$FILE" "/$1" --type conditionalformatting "${@:2}"; }   # cf <sheet> --prop ...

# ===== Sheet1: CellIs (comparison) =====
col Sheet1 Scores 42 58 91 73 30 88 65 100 12 77
cf Sheet1 --prop type=cellIs --prop ref=A2:A11 --prop operator=greaterThan --prop value=80 --prop fill=C6EFCE
cf Sheet1 --prop type=cellIs --prop ref=A2:A11 --prop operator=lessThan --prop value=40 --prop fill=FFC7CE
cf Sheet1 --prop type=cellIs --prop ref=A2:A11 --prop operator=between --prop value=50 --prop value2=70 --prop fill=FFEB9C
cf Sheet1 --prop type=cellIs --prop ref=A2:A11 --prop operator=equal --prop value=100 --prop fill=63BE7B

# ===== Sheet2: Text rules =====
officecli add "$FILE" / --type sheet --prop name=Text
col Text "Log line" "ERROR: timeout" ok "WARNING low" "error code 5" passed "Begins here" "ends with END" neutral
cf Text --prop type=containsText --prop ref=A2:A9 --prop text=error --prop fill=FFC7CE
cf Text --prop type=notContains --prop ref=A2:A9 --prop text=error --prop fill=C6EFCE   # handler token: notContains
cf Text --prop type=beginsWith --prop ref=A2:A9 --prop text=Begins --prop fill=BDD7EE
cf Text --prop type=endsWith --prop ref=A2:A9 --prop text=END --prop fill=FFE699

# ===== Sheet3: Top / Bottom / Average =====
officecli add "$FILE" / --type sheet --prop name=TopBottom
col TopBottom Revenue 120 340 90 510 275 60 430 180 295 75 360 145
cf TopBottom --prop type=top10 --prop ref=A2:A13 --prop rank=3 --prop fill=C6EFCE
cf TopBottom --prop type=bottom --prop ref=A2:A13 --prop rank=3 --prop fill=FFC7CE
cf TopBottom --prop type=topPercent --prop ref=A2:A13 --prop rank=25 --prop percent=true --prop fill=63BE7B
cf TopBottom --prop type=aboveAverage --prop ref=A2:A13 --prop aboveAverage=true --prop fill=BDD7EE
cf TopBottom --prop type=belowAverage --prop ref=A2:A13 --prop fill=F8CBAD   # type implies direction
cf TopBottom --prop type=aboveAverage --prop ref=A2:A13 --prop aboveAverage=true --prop stdDev=1 --prop equalAverage=true --prop fill=FFEB9C

# ===== Sheet4: Data bars =====
officecli add "$FILE" / --type sheet --prop name=DataBars
col DataBars "Net flow" 120 -45 300 -80 210 60 -150 90 175 -30
cf DataBars --prop type=dataBar --prop ref=A2:A11 --prop color=638EC6 --prop min=auto --prop max=auto \
  --prop negativeColor=FF0000 --prop axisColor=000000 --prop axisPosition=middle --prop showValue=true

# ===== Sheet5: Colour scales =====
officecli add "$FILE" / --type sheet --prop name=ColorScales
col ColorScales "2-colour" 10 25 40 55 70 85 100 30 60 90
officecli set "$FILE" /ColorScales/B1 --prop value="3-colour" --prop font.bold=true --prop fill=1F4E79 --prop font.color=FFFFFF
b=2; for v in 10 25 40 55 70 85 100 30 60 90; do officecli set "$FILE" "/ColorScales/B$b" --prop value="$v"; b=$((b+1)); done
cf ColorScales --prop type=colorScale --prop ref=A2:A11 --prop minColor=FFFFFF --prop maxColor=63BE7B
cf ColorScales --prop type=colorScale --prop ref=B2:B11 --prop minColor=F8696B --prop midColor=FFEB84 --prop maxColor=63BE7B --prop midPoint=50

# ===== Sheet6: Icon sets =====
officecli add "$FILE" / --type sheet --prop name=IconSets
for c in A B C D; do r=2; for v in 1 2 3 4 5 2 4 5 1 3; do officecli set "$FILE" "/IconSets/$c$r" --prop value="$v"; r=$((r+1)); done; done
officecli set "$FILE" /IconSets/A1 --prop value=3TrafficLights1 --prop font.bold=true --prop fill=1F4E79 --prop font.color=FFFFFF
officecli set "$FILE" /IconSets/B1 --prop value=3Arrows --prop font.bold=true --prop fill=1F4E79 --prop font.color=FFFFFF
officecli set "$FILE" /IconSets/C1 --prop value=5Rating --prop font.bold=true --prop fill=1F4E79 --prop font.color=FFFFFF
officecli set "$FILE" /IconSets/D1 --prop value="3TrafficLights1 rev" --prop font.bold=true --prop fill=1F4E79 --prop font.color=FFFFFF
cf IconSets --prop type=iconSet --prop ref=A2:A11 --prop iconset=3TrafficLights1 --prop showValue=true
cf IconSets --prop type=iconSet --prop ref=B2:B11 --prop iconset=3Arrows --prop showValue=true
cf IconSets --prop type=iconSet --prop ref=C2:C11 --prop iconset=5Rating --prop showValue=true
cf IconSets --prop type=iconSet --prop ref=D2:D11 --prop iconset=3TrafficLights1 --prop reverse=true --prop showValue=true

# ===== Sheet7: Formula / date / duplicate / unique =====
officecli add "$FILE" / --type sheet --prop name=FormulaEtc
col FormulaEtc Value 4 7 4 9 2 7 5 1 9 3
officecli set "$FILE" /FormulaEtc/B1 --prop value=Date --prop font.bold=true --prop fill=1F4E79 --prop font.color=FFFFFF
b=2; for d in 45800 45810 45820 45830 45840 45850 45860 45870 45880 45890; do
  officecli set "$FILE" "/FormulaEtc/B$b" --prop value="$d" --prop numberformat=yyyy-mm-dd; b=$((b+1)); done
cf FormulaEtc --prop type=formula --prop ref=A2:A11 --prop formula="ISODD(A2)" --prop fill=BDD7EE
cf FormulaEtc --prop type=duplicateValues --prop ref=A2:A11 --prop fill=FFC7CE
cf FormulaEtc --prop type=uniqueValues --prop ref=A2:A11 --prop fill=C6EFCE
cf FormulaEtc --prop type=dateOccurring --prop ref=B2:B11 --prop period=thisMonth --prop fill=FFEB9C

officecli close "$FILE"
officecli validate "$FILE"
echo "Created: $FILE"
