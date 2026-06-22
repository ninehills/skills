# Document Formatting Showcase

Exercises the docx `document` property surface — the document-level settings that
have no per-paragraph or per-run equivalent. Three files work together:

- **document-formatting.py** — builds the doc via the **officecli Python SDK**.
- **document-formatting.docx** — the generated document.
- **document-formatting.md** — this file.

## The `document` container

`document` is a read-only container addressed at path `/` — you never `add` or
`remove` it, only `set`/`get` its properties:

```bash
officecli set file.docx / --prop author="Jane" --prop title="Q3 Report"
officecli get file.docx /            # read the whole property bag back
```

Built on the [`officecli-sdk`](../../sdk/python) (one resident, writes shipped
over the pipe); falls back to the in-repo SDK copy when the package isn't
pip-installed.

## Regenerate

```bash
cd examples/word
pip install officecli-sdk
python3 document-formatting.py
# → document-formatting.docx
```

## Property groups

### 1. Metadata (core + extended properties)

```bash
officecli set file.docx / --prop author="Jane Author" --prop title="Q3 Field Report" \
  --prop subject=Finance --prop keywords="report,q3,finance" \
  --prop description="Quarterly field summary." --prop lastModifiedBy=Editorial
officecli set file.docx / --prop extended.company="Acme Corp" \
  --prop extended.manager="Dana Lead" --prop extended.template="Normal.dotm"
```

`author` ↔ `creator` (alias). A few keys are read-only metadata (`category`,
`revisionNumber`, …) and only surface on `get`.

### 2. Page setup

```bash
officecli set file.docx / --prop pageWidth=21cm --prop pageHeight=29.7cm \
  --prop orientation=portrait \
  --prop marginTop=2.54cm --prop marginBottom=2.54cm \
  --prop marginLeft=3.18cm --prop marginRight=3.18cm \
  --prop marginHeader=1.5cm --prop marginFooter=1.75cm
officecli set file.docx / --prop mirrorMargins=true --prop gutterAtTop=false \
  --prop bookFoldPrinting=false
```

Lengths accept `cm`/`in`/`pt` or bare twips. `orientation=landscape` swaps
width/height. These write the document's section properties (`sectPr`).

> The bare `sectPr=present` toggle is intentionally **not** demonstrated: it's an
> internal dump→batch round-trip marker (forces an empty `<w:sectPr/>` to survive
> a rebuild), `get`-invisible and single-valued, never an interactive edit. Like a
> table's auto-assigned `id`, it's settable only for fidelity — the page-setup
> props above are how you actually shape a section.

### 3. docDefaults — document-wide run/paragraph defaults

The defaults an unstyled paragraph inherits. In the generated file the body
paragraphs carry **no** run formatting, so they render in `Georgia 12pt` slate —
straight from here, not from the runs:

```bash
officecli set file.docx / \
  --prop docDefaults.font=Georgia --prop docDefaults.font.eastAsia=SimSun \
  --prop docDefaults.fontSize=12 --prop docDefaults.color=2F3640 \
  --prop docDefaults.alignment=left \
  --prop docDefaults.spaceAfter=8pt --prop docDefaults.lineSpacing=1.15x
```

Also: `docDefaults.bold`, `docDefaults.italic`, `docDefaults.rtl`,
`docDefaults.font.hAnsi`, `docDefaults.font.complexScript`,
`docDefaults.spaceBefore`.

### 4. Theme — palette accents and major/minor fonts

Remapping the theme shifts every element that references it (styles, charts,
shapes) by reference, not by value:

```bash
officecli set file.docx / \
  --prop theme.color.accent1=1F6FEB --prop theme.color.accent2=E3572A \
  --prop theme.color.accent3=2DA44E --prop theme.color.hlink=0969DA
officecli set file.docx / \
  --prop theme.font.major.latin=Georgia --prop theme.font.minor.latin=Calibri \
  --prop theme.font.major.eastAsia=SimHei --prop theme.font.minor.eastAsia=SimSun
```

Full palette keys: `accent1..6`, `dk1`/`dk2`, `lt1`/`lt2`, `hlink`/`folHlink`.

### 5. CJK grid & spacing

```bash
officecli set file.docx / \
  --prop docGrid.type=lines --prop docGrid.linePitch=312 \
  --prop charSpacingControl=compressPunctuation \
  --prop autoSpaceDE=true --prop autoSpaceDN=true \
  --prop kinsoku=true --prop overflowPunct=true
```

`docGrid.charSpace` sets the character grid pitch for `docGrid.type=linesAndChars`.

### 6. Font embedding

```bash
officecli set file.docx / --prop embedFonts=true --prop embedSystemFonts=false \
  --prop saveSubsetFonts=true
```

### 7. Display / print / privacy

```bash
officecli set file.docx / \
  --prop evenAndOddHeaders=true --prop autoHyphenation=false \
  --prop defaultTabStop=720 --prop displayBackgroundShape=true \
  --prop removePersonalInformation=false --prop removeDateAndTime=false \
  --prop printFormsData=false
```

## Complete feature coverage

| Group | Keys | Visible in render? |
|---|---|---|
| Metadata | `author`, `title`, `subject`, `keywords`, `description`, `lastModifiedBy`, `extended.company/manager/template` | No (file properties) |
| Page setup | `pageWidth`, `pageHeight`, `orientation`, `marginTop/Bottom/Left/Right/Header/Footer/Gutter`, `mirrorMargins`, `gutterAtTop`, `bookFoldPrinting` | Yes (geometry) |
| docDefaults | `docDefaults.font[.eastAsia/hAnsi/complexScript]`, `.fontSize`, `.color`, `.bold/.italic/.rtl`, `.alignment`, `.spaceBefore/.spaceAfter`, `.lineSpacing` | Yes (unstyled text) |
| Theme | `theme.color.accent1..6/dk1/dk2/lt1/lt2/hlink/folHlink`, `theme.font.major/minor.latin/eastAsia` | Yes (themed elements) |
| CJK grid | `docGrid.type/linePitch/charSpace`, `charSpacingControl`, `autoSpaceDE/DN`, `kinsoku`, `overflowPunct` | Yes (CJK layout) |
| Fonts | `embedFonts`, `embedSystemFonts`, `saveSubsetFonts` | No (portability) |
| Display/privacy | `evenAndOddHeaders`, `autoHyphenation`, `defaultTabStop`, `displayBackgroundShape`, `removePersonalInformation`, `removeDateAndTime`, `printFormsData` | Partly |

Full list: `officecli help docx document`.

## Set → Get round-trip

The script ends by reading the container back and printing canonical keys:

```
author = Jane Author
pageWidth = 21cm
pageHeight = 29.7cm
docDefaults.font = Georgia
docDefaults.fontSize = 12pt
theme.color.accent1 = #1F6FEB
docGrid.type = lines
```

Note normalization on `get`: colours gain `#`, font sizes become `pt`-qualified.
