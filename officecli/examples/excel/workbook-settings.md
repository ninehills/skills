# Workbook Settings Showcase

Exercises the xlsx `workbook` property surface — the workbook-level settings with
no per-cell or per-sheet equivalent. Four files work together:

- **workbook-settings.sh** — builds the workbook via the `officecli` CLI (this file walks through it).
- **workbook-settings.py** — the same build via the **officecli Python SDK** (one `doc.send()` per command, mirroring the `.sh` line for line).
- **workbook-settings.xlsx** — the generated workbook (either script produces it).
- **workbook-settings.md** — this file.

The CLI commands shown below are exactly what `workbook-settings.sh` runs; the
`.py` issues the identical sequence over the SDK pipe.

## The `workbook` container

`workbook` is a read-only container addressed at path `/` — you never `add` or
`remove` it, only `set`/`get`:

```bash
officecli set file.xlsx / --prop author="Jane" --prop calc.mode=manual
officecli get file.xlsx /
```

## Regenerate

```bash
cd examples/excel
bash workbook-settings.sh            # via the CLI
# — or —
pip install officecli-sdk            # the SDK (officecli binary still required)
python3 workbook-settings.py         # via the SDK, same result
# → workbook-settings.xlsx
```

## Property groups

### 1. Metadata (core + extended properties)

```bash
officecli set file.xlsx / --prop author="Jane Author" --prop title="2026 Revenue Model" \
  --prop subject=Finance --prop keywords="finance,2026,model" \
  --prop description="Annual revenue summary." --prop category=Reports \
  --prop lastModifiedBy=Editorial --prop revisionNumber=3
officecli set file.xlsx / --prop extended.company="Acme Corp" \
  --prop extended.manager="Dana Lead" --prop extended.template="Book.xltx"
```

### 2. Calc engine

```bash
officecli set file.xlsx / \
  --prop calc.mode=manual \            # auto | manual | autoNoTable
  --prop calc.iterate=true \           # allow circular-reference iteration
  --prop calc.iterateCount=100 \
  --prop calc.iterateDelta=0.001 \
  --prop calc.fullPrecision=true       # calculate at full precision, not as-displayed
```

`calc.mode=manual` stops Excel from auto-recalculating on every edit — useful for
heavy models. The generated file has a live `=SUM(...)` so the effect is testable.

### 3. Protection & display

```bash
officecli set file.xlsx / \
  --prop workbook.lockStructure=true \    # can't add/delete/rename sheets
  --prop workbook.lockWindows=false \
  --prop workbook.password=secret \       # structure-protection password
  --prop workbook.dateCompatibility=false \  # false = 1900 date system, true = 1904
  --prop workbook.filterPrivacy=true \
  --prop workbook.showObjects=all         # all | placeholders | none
```

### 4. Theme — palette accents and major/minor fonts

```bash
officecli set file.xlsx / \
  --prop theme.color.accent1=1F6FEB --prop theme.color.accent2=E3572A \
  --prop theme.color.accent3=2DA44E --prop theme.color.hlink=0969DA
officecli set file.xlsx / \
  --prop theme.font.major.latin=Georgia --prop theme.font.minor.latin=Calibri \
  --prop theme.font.major.eastAsia=SimHei --prop theme.font.minor.eastAsia=SimSun
```

Full palette keys: `accent1..6`, `dk1`/`dk2`, `lt1`/`lt2`, `hlink`/`folHlink`.
A freshly created officecli workbook ships a theme part (like docx/pptx), so
these resolve and round-trip.

## Complete feature coverage

| Group | Keys |
|---|---|
| Metadata | `author`, `title`, `subject`, `keywords`, `description`, `category`, `lastModifiedBy`, `revisionNumber`, `extended.company/manager/template` |
| Calc engine | `calc.mode`, `calc.iterate`, `calc.iterateCount`, `calc.iterateDelta`, `calc.fullPrecision` |
| Protection/display | `workbook.lockStructure`, `workbook.lockWindows`, `workbook.password`, `workbook.dateCompatibility`, `workbook.filterPrivacy`, `workbook.showObjects` |
| Theme | `theme.color.accent1..6/dk1/dk2/lt1/lt2/hlink/folHlink`, `theme.font.major/minor.latin/eastAsia` |

Full list: `officecli help xlsx workbook`.

## Set → Get round-trip

```
author = Jane Author
calc.mode = manual
calc.iterate = True
workbook.lockStructure = True
workbook.showObjects = all
theme.color.accent1 = #1F6FEB
theme.font.major.latin = Georgia
```
