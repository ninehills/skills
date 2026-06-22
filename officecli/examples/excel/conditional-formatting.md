# Conditional Formatting Showcase

Exercises the full xlsx `conditionalformatting` rule family — the one major
spreadsheet feature the other excel examples don't cover. Three files work
together:

- **conditional-formatting.py** — builds the workbook via the **officecli Python SDK**.
- **conditional-formatting.xlsx** — the generated 7-sheet workbook.
- **conditional-formatting.md** — this file.

## Built on the SDK (not subprocess)

Unlike the sibling `*.py` examples (which `subprocess.run("officecli …")` once
per command), this script drives the [`officecli-sdk`](../../sdk/python) Python
client. One resident process is started; every rule is shipped over the named
pipe; all the rules for a sheet go in a single `doc.batch(...)` round-trip:

```python
import officecli                      # pip install officecli-sdk

with officecli.create(FILE, "--force") as doc:
    doc.batch([
        {"command": "set", "path": "/Sheet1/A2", "props": {"value": "58"}},
        {"command": "add", "parent": "/Sheet1", "type": "conditionalformatting",
         "props": {"type": "cellIs", "ref": "A2:A11",
                   "operator": "greaterThan", "value": "80", "fill": "C6EFCE"}},
    ])
```

The dict shape is identical to an `officecli batch` list item — `command`,
`path`/`parent`/`type`, and `props`. The script falls back to the in-repo SDK
copy if `officecli-sdk` isn't pip-installed, so it runs straight from a checkout.

## Regenerate

```bash
cd examples/excel
pip install officecli-sdk          # plus the `officecli` binary on PATH
python3 conditional-formatting.py
# → conditional-formatting.xlsx
```

## A conditional-formatting rule

Every rule is one `add` against the sheet, with `type=` selecting the rule kind
and `ref=` the target range. The match format (fill colour, bar, scale, icons)
is carried by the remaining props:

```bash
officecli add file.xlsx /Sheet1 --type conditionalformatting \
  --prop type=cellIs --prop ref=A2:A11 --prop operator=greaterThan \
  --prop value=80 --prop fill=C6EFCE
```

The rule lands at `/Sheet1/cf[N]`; `get`/`set`/`remove` address it there. A
rule's differential fill is stored once in the workbook-level `<dxfs>` table
(styles.xml) and referenced by index.

## Sheets

### Sheet1 — CellIs (value comparison)

`operator` ∈ `greaterThan`, `lessThan`, `greaterThanOrEqual`,
`lessThanOrEqual`, `equal`, `notEqual`, `between`, `notBetween`. `between`/
`notBetween` use both `value` and `value2`.

```bash
officecli add file.xlsx /Sheet1 --type conditionalformatting --prop type=cellIs --prop ref=A2:A11 --prop operator=greaterThan --prop value=80 --prop fill=C6EFCE
officecli add file.xlsx /Sheet1 --type conditionalformatting --prop type=cellIs --prop ref=A2:A11 --prop operator=between --prop value=50 --prop value2=70 --prop fill=FFEB9C
```

### Sheet2 — Text rules

`containsText`, `notContainsText`, `beginsWith`, `endsWith`. The needle is
`text=`; the match fill is `fill=`.

```bash
officecli add file.xlsx /Text --type conditionalformatting --prop type=containsText --prop ref=A2:A9 --prop text=error --prop fill=FFC7CE
officecli add file.xlsx /Text --type conditionalformatting --prop type=beginsWith --prop ref=A2:A9 --prop text=Begins --prop fill=BDD7EE
```

### Sheet3 — Top / Bottom / Average

`top10`/`topN` (count via `rank=`), `topPercent` (`rank=` + `percent=true`),
`bottom`, `aboveAverage`/`belowAverage` (`aboveAverage=true|false`, optional
`stdDev=` for an N-sigma band).

```bash
officecli add file.xlsx /TopBottom --type conditionalformatting --prop type=top10 --prop ref=A2:A13 --prop rank=3 --prop fill=C6EFCE
officecli add file.xlsx /TopBottom --type conditionalformatting --prop type=topPercent --prop ref=A2:A13 --prop rank=25 --prop percent=true --prop fill=63BE7B
officecli add file.xlsx /TopBottom --type conditionalformatting --prop type=aboveAverage --prop ref=A2:A13 --prop aboveAverage=true --prop stdDev=1 --prop fill=FFEB9C
```

### Sheet4 — Data bars

`color` is the bar fill; `min`/`max` set the scale (`auto` = automatic bounds —
the default). The 2010+ extension adds `negativeColor`, `axisColor`, and
`axisPosition` (`automatic`/`middle`/`none`), so negative values render leftward
in their own colour about a mid axis.

```bash
officecli add file.xlsx /DataBars --type conditionalformatting --prop type=dataBar --prop ref=A2:A11 \
  --prop color=638EC6 --prop min=auto --prop max=auto \
  --prop negativeColor=FF0000 --prop axisColor=000000 --prop axisPosition=middle --prop showValue=true
```

> `min=auto`/`max=auto` is the **automatic-bound sentinel** — it serializes to
> `<cfvo type="min"/>`/`<cfvo type="max"/>` (and x14 `autoMin`/`autoMax`), the
> same as omitting the bound. (Passing a real number, e.g. `min=0 max=100`,
> pins the scale instead.)

### Sheet5 — Color scales

2-colour (`minColor`/`maxColor`) or 3-colour (`+ midColor`, midpoint via
`midPoint=`).

```bash
officecli add file.xlsx /ColorScales --type conditionalformatting --prop type=colorScale --prop ref=A2:A11 --prop minColor=FFFFFF --prop maxColor=63BE7B
officecli add file.xlsx /ColorScales --type conditionalformatting --prop type=colorScale --prop ref=B2:B11 --prop minColor=F8696B --prop midColor=FFEB84 --prop maxColor=63BE7B --prop midPoint=50
```

### Sheet6 — Icon sets

`iconset=` names the set (`3TrafficLights1`, `3Arrows`, `4Rating`, `5Rating`, …).
`reverse=true` flips the order; `showValue=false` hides the cell value behind the
icon.

```bash
officecli add file.xlsx /IconSets --type conditionalformatting --prop type=iconSet --prop ref=A2:A11 --prop iconset=3TrafficLights1
officecli add file.xlsx /IconSets --type conditionalformatting --prop type=iconSet --prop ref=D2:D11 --prop iconset=3TrafficLights1 --prop reverse=true
```

### Sheet7 — Formula, date, duplicate / unique

`formula` (a boolean expression, no leading `=`), `dateOccurring` (`period=`
token), `duplicateValues`, `uniqueValues`.

```bash
officecli add file.xlsx /FormulaEtc --type conditionalformatting --prop type=formula --prop ref=A2:A11 --prop formula="ISODD(A2)" --prop fill=BDD7EE
officecli add file.xlsx /FormulaEtc --type conditionalformatting --prop type=duplicateValues --prop ref=A2:A11 --prop fill=FFC7CE
officecli add file.xlsx /FormulaEtc --type conditionalformatting --prop type=dateOccurring --prop ref=B2:B11 --prop period=thisMonth --prop fill=FFEB9C
```

## Complete feature coverage

| Rule family | `type=` | Key props | Sheet |
|---|---|---|---|
| Comparison | `cellIs` | `operator`, `value`, `value2`, `fill` | Sheet1 |
| Text | `containsText` / `notContainsText` / `beginsWith` / `endsWith` | `text`, `fill` | Sheet2 |
| Top/Bottom | `top10` / `topN` / `topPercent` / `bottom` | `rank`, `percent`, `bottom`, `fill` | Sheet3 |
| Average | `aboveAverage` / `belowAverage` | `aboveAverage`, `stdDev`, `equalAverage`, `fill` | Sheet3 |
| Data bar | `dataBar` | `color`, `min`, `max`, `negativeColor`, `axisColor`, `axisPosition`, `showValue` | Sheet4 |
| Colour scale | `colorScale` | `minColor`, `midColor`, `maxColor`, `midPoint` | Sheet5 |
| Icon set | `iconSet` | `iconset`, `reverse`, `showValue` | Sheet6 |
| Formula | `formula` | `formula`, `fill` | Sheet7 |
| Date | `dateOccurring` | `period`, `fill` | Sheet7 |
| Dup/Unique | `duplicateValues` / `uniqueValues` | `fill` | Sheet7 |

Full property list: `officecli help xlsx conditionalformatting` (or
`schemas/help/xlsx/conditionalformatting.json`).

## Read a rule back

```bash
officecli query conditional-formatting.xlsx conditionalformatting
officecli get conditional-formatting.xlsx "/Sheet1/cf[1]" --json
```

`get` normalizes on read: colours gain a `#` prefix (`#C6EFCE`), and the rule
`type` comes back as the canonical camelCase token.

## Validating CF documents

A data-bar / colour-scale fill lives in the workbook `<dxfs>` table, so always
validate the **saved** file from a fresh process:

```bash
officecli validate conditional-formatting.xlsx
```
