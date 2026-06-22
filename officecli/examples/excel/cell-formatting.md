# Cell Formatting Showcase

Exercises the full xlsx `cell` property surface — the single most-used Excel
element. Three files work together:

- **cell-formatting.py** — Python script that drives `officecli` to build the workbook.
- **cell-formatting.xlsx** — The generated 6-sheet workbook.
- **cell-formatting.md** — This file.

## Regenerate

```bash
cd examples/excel
python3 cell-formatting.py
# → cell-formatting.xlsx
```

`set` auto-creates the target cell, so no per-cell `add` is needed. The script
uses resident mode (`open` … `close`) for speed and registers an `atexit` close
so the resident process is never left dangling on error.

> The `cell()` helper wraps each `--prop k=v` in `shlex.quote`. This matters:
> a currency format like `numberformat=$#,##0.00` contains `$#`, which a shell
> would otherwise expand to the positional-arg count. Quoting keeps the code
> literal.

## Sheets

### Sheet1 — Fonts

Each row pairs a property label (column A) with a rendered sample (column B):
`font.name`, `font.size`, `font.bold`, `font.italic`, `font.color`,
`underline=single`, `underline=double`, `strike`, and a combined run.

```bash
officecli set file.xlsx /Sheet1/B11 \
  --prop value="Bold + italic + blue + 14pt" \
  --prop font.bold=true --prop font.italic=true \
  --prop font.color=2E75B6 --prop font.size=14
```

### Sheet2 — Fills & alignment

| Feature | Spec |
|---|---|
| Solid hex fill | `fill=E63946` |
| Named color | `fill=gold` |
| `rgb()` form | `fill="rgb(46,157,182)"` |
| Horizontal align | `alignment.horizontal=left\|center\|right` (alias `halign`) |
| Vertical align | `alignment.vertical=top\|center\|bottom` (alias `valign`) |
| Wrap text | `alignment.wrapText=true` (aliases `wrap`, `wrapText`) |
| Reading order | `alignment.readingOrder=rtl` |

Vertical alignment only shows visibly when the row is taller than the text, so
the script bumps `row[6..8]` height to 34pt via `/Fills/row[6] --prop height=34`.

The sheet also shows three alignment properties set directly (canonical keys):

| Feature | Spec |
|---|---|
| Text rotation | `alignment.textRotation=45` (0-90 up / 91-180 down / 255 stacked; alias `rotation`) |
| Indent | `alignment.indent=3` (alias `indent`) |
| Shrink to fit | `alignment.shrinkToFit=true` (alias `shrink`) |

### Sheet3 — Borders

```bash
officecli set file.xlsx /Borders/B3  --prop border=thin           # shorthand: all four sides
officecli set file.xlsx /Borders/B5  --prop border.all=medium     # explicit "all" form
officecli set file.xlsx /Borders/B7  --prop border=thick --prop border.color=C00000
officecli set file.xlsx /Borders/B9  --prop border.bottom=double  # single side
officecli set file.xlsx /Borders/B13 --prop border.left=thick --prop border.top=thin \
                                     --prop border.right=medium --prop border.bottom=double
# Diagonal borders — direction via diagonalUp/Down; color requires a diagonal line.
officecli set file.xlsx /Borders/B15 --prop border.diagonal=thin --prop border.diagonalUp=true
officecli set file.xlsx /Borders/B17 --prop border.diagonal=medium --prop border.diagonalDown=true \
                                     --prop border.diagonal.color=C00000
```

Styles accepted: `thin`, `medium`, `thick`, `double`, `dashed`, … (full list in
`schemas/help/xlsx/cell.json` → `border.*`). `border.diagonal.color` requires a
`border.diagonal` line to attach to.

### Sheet4 — Number formats

The label column is the format **code**; column B is the same kind of value
with that `numberformat` applied:

| `numberformat=` | Value → Display |
|---|---|
| `#,##0` | `1234567` → `1,234,567` |
| `#,##0.00` | `1234.5` → `1,234.50` |
| `0.00%` | `0.1834` → `18.34%` |
| `$#,##0.00` | `29999.9` → `$29,999.90` |
| `yyyy-mm-dd` | `45413` → `2024-05-01` |
| `0.00E+00` | `602214` → `6.02E+05` |
| `_(* #,##0.00_);_(* (#,##0.00);_(* "-"??_)` | `-4250` → `(4,250.00)` |

> The `0.00E+00` **label** cell is written with `type=string`, otherwise Excel
> parses the literal text `0.00E+00` as the number `0`.

### Sheet5 — Values, formulas, links

```bash
officecli set file.xlsx /Data/B5 --prop formula="B3*B4" --prop numberformat="$#,##0.00"   # 12 × 4.50 = $54.00
officecli set file.xlsx /Data/B7 --prop value=007 --prop type=string                       # keep leading zeros
officecli set file.xlsx /Data/A9 --prop value="OfficeCLI on GitHub" \
  --prop link="https://github.com/iOfficeAI/OfficeCLI" --prop tooltip="Open the repo"
officecli set file.xlsx /Data/A11 --prop value="locked cell" --prop locked=true            # effective once sheet is protected
officecli set file.xlsx /Data/A13 --prop value="Merged title" --prop merge="A13:C13" \
  --prop alignment.horizontal=center
officecli set file.xlsx /Data/B15 --prop arrayformula="B3*2"                                # dynamic-array spill
```

### Sheet6 — Rich-text runs

`runs` is an add-time property (requires `--type cell` and `type=richtext`). Each run is a JSON
object with `"text"` plus optional font props (`bold`, `italic`, `color`, `size`, `underline`,
`strike`, `superscript`, `subscript`). `set` does not support rich-text; use `add`.

```bash
# Bold+red / italic+blue / normal run in one cell
officecli add file.xlsx /RichText --type cell --prop ref=A3 \
  --prop type=richtext \
  --prop 'runs=[{"text":"Bold + Red  ","bold":true,"color":"C00000"},{"text":"Italic + Blue","italic":true,"color":"2E75B6"},{"text":"  Normal"}]'

# Chemical formula with superscript: H₂O
officecli add file.xlsx /RichText --type cell --prop ref=A5 \
  --prop type=richtext \
  --prop 'runs=[{"text":"H","bold":true,"color":"1F4E79","size":18},{"text":"2","superscript":true,"size":10},{"text":"O water formula","color":"1F4E79"}]'

# strike / underline / different size in one cell
officecli add file.xlsx /RichText --type cell --prop ref=A7 \
  --prop type=richtext \
  --prop 'runs=[{"text":"Strike","strike":true},{"text":" | "},{"text":"underline","underline":"single"},{"text":" | "},{"text":"size 14pt","size":14}]'
```

**Features:** `type=richtext`, `runs` (JSON array of run objects), per-run: `text`, `bold`, `italic`, `color`, `size`, `underline`, `strike`, `superscript`, `subscript`

## Complete Feature Coverage

| Feature | Sheet |
|---------|-------|
| `font.name`, `font.size`, `font.bold`, `font.italic`, `font.color` | Sheet1 |
| `underline=single`, `underline=double` | Sheet1 |
| `strike=true` | Sheet1 |
| `superscript=true`, `subscript=true` | Sheet1 |
| `fill` (hex, named, rgb) | Sheet2 |
| `alignment.horizontal` (left/center/right) | Sheet2 |
| `alignment.vertical` (top/center/bottom) | Sheet2 |
| `alignment.wrapText` | Sheet2 |
| `alignment.readingOrder` (rtl) | Sheet2 |
| `alignment.textRotation` (0-255) | Sheet2 |
| `alignment.indent` | Sheet2 |
| `alignment.shrinkToFit` | Sheet2 |
| `border` (shorthand all sides) | Sheet3 |
| `border.all`, `border.top/bottom/left/right` | Sheet3 |
| `border.color`, `border.diagonal`, `border.diagonalUp`, `border.diagonalDown`, `border.diagonal.color` | Sheet3 |
| `numberformat` (thousands, %, currency, date, scientific, accounting) | Sheet4 |
| `value`, `type=string` | Sheet5 |
| `formula`, `arrayformula` | Sheet5 |
| `link`, `tooltip` | Sheet5 |
| `locked` | Sheet5 |
| `merge` | Sheet5 |
| `type=richtext`, `runs` (per-run: bold/italic/color/size/strike/underline/superscript/subscript) | Sheet6 |

## Set → Get round-trip

The script ends by reading three cells back with `get … --json` and printing the
canonical keys, proving the values survive the write and normalize on read:

```
/Sheet1/B11: {'font.bold': True, 'font.italic': True, 'font.color': '#2E75B6', 'font.size': '14pt'}
/Numbers/B6: {'numberformat': '$#,##0.00'}
/Borders/B9: {'border.bottom': 'double'}
```

Note the normalization on `get`: colors gain a `#` prefix (`#2E75B6`) and font
sizes become unit-qualified (`14pt`) — the canonical output forms.

## Inspect the Generated File

```bash
officecli query cell-formatting.xlsx sheet
officecli get cell-formatting.xlsx "/RichText/A3"
```
