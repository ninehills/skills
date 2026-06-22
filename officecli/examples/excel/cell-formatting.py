#!/usr/bin/env python3
"""
Cell Formatting Showcase — generates cell-formatting.xlsx exercising the full
xlsx `cell` property surface (schemas/help/xlsx/cell.json).

5 sheets, one property group each:
  Fonts    — font.name/size/bold/italic/color, underline, strike
  Fills    — fill (hex/named/rgb), alignment.horizontal/vertical/wrapText/readingOrder
  Borders  — border shorthand, border.all, per-side styles, border.color
  Numbers  — numberformat codes (thousands, %, currency, date, scientific, accounting)
  Data     — value/type, formula, link + tooltip, locked, merge

Closes with Set -> Get round-trip readbacks proving the canonical keys come back.
`set` auto-creates the target cell, so no explicit `add` is needed per cell.

Usage:
  python3 cell-formatting.py
"""

import subprocess, os, atexit, shlex

FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "cell-formatting.xlsx")


def cli(cmd):
    """Run: officecli <cmd> and echo any output."""
    r = subprocess.run(f"officecli {cmd}", shell=True, capture_output=True, text=True)
    out = (r.stdout or "").strip()
    if out:
        for line in out.split("\n"):
            if line.strip():
                print(f"  {line.strip()}")
    if r.returncode != 0:
        err = (r.stderr or "").strip()
        if err and "process cannot access" not in err:
            print(f"  ERROR: {err}")


def cell(path, **props):
    """officecli set <FILE> <path> --prop k=v ...

    shlex.quote each k=v so format codes containing shell metacharacters
    (e.g. ``numberformat=$#,##0.00`` — the ``$#`` would otherwise expand) survive.
    """
    args = " ".join("--prop " + shlex.quote(f"{k}={v}") for k, v in props.items())
    cli(f'set "{FILE}" "{path}" {args}')


if os.path.exists(FILE):
    os.remove(FILE)

print("\n==========================================")
print(f"Generating cell formatting showcase: {FILE}")
print("==========================================")

_closed = False
def close_file():
    global _closed
    if not _closed:
        _closed = True
        cli(f'close "{FILE}"')

cli(f'create "{FILE}"')
cli(f'open "{FILE}"')
atexit.register(close_file)  # guarantees resident close even on mid-script error

# ==========================================================================
# Sheet1: Fonts — font.* family + underline/strike
# ==========================================================================
print("\n--- Sheet1: Fonts ---")
cell("/Sheet1/A1", value="Cell font properties", **{"font.bold": "true", "font.size": "14", "fill": "1F4E79", "font.color": "FFFFFF"})
cell("/Sheet1/A2", value="Property", **{"font.bold": "true", "fill": "D9E1F2"})
cell("/Sheet1/B2", value="Rendered sample", **{"font.bold": "true", "fill": "D9E1F2"})

# (label, sample-text, {props applied to the sample cell})
FONT_ROWS = [
    ("font.name=Georgia", "Georgia serif",              {"font.name": "Georgia"}),
    ("font.size=18",      "18pt text",                  {"font.size": "18"}),
    ("font.bold=true",    "Bold text",                  {"font.bold": "true"}),
    ("font.italic=true",  "Italic text",                {"font.italic": "true"}),
    ("font.color=C00000", "Red text",                   {"font.color": "C00000"}),
    ("underline=single",  "Underlined",                 {"underline": "single"}),
    ("underline=double",  "Double underline",           {"underline": "double"}),
    ("strike=true",       "Struck out",                 {"strike": "true"}),
    ("superscript=true",  "Superscript cell",           {"superscript": "true"}),
    ("subscript=true",    "Subscript cell",             {"subscript": "true"}),
    ("combined",          "Bold + italic + blue + 14pt", {"font.bold": "true", "font.italic": "true", "font.color": "2E75B6", "font.size": "14"}),
]
for i, (label, sample, props) in enumerate(FONT_ROWS, start=3):
    cell(f"/Sheet1/A{i}", value=label)
    cell(f"/Sheet1/B{i}", value=sample, **props)

cell("/Sheet1/col[1]", width="22")
cell("/Sheet1/col[2]", width="32")

# ==========================================================================
# Sheet2: Fills & alignment
# ==========================================================================
print("\n--- Sheet2: Fills & alignment ---")
cli(f'add "{FILE}" / --type sheet --prop name=Fills')
cell("/Fills/A1", value="Fills & alignment", **{"font.bold": "true", "font.size": "14", "fill": "548235", "font.color": "FFFFFF"})

cell("/Fills/A2", value="fill=E63946 (hex)",    fill="E63946", **{"font.color": "FFFFFF"})
cell("/Fills/A3", value="fill=gold (named)",    fill="gold")
cell("/Fills/A4", value="fill=rgb(46,157,182)", fill="rgb(46,157,182)", **{"font.color": "FFFFFF"})

for i, h in zip((6, 7, 8), ("left", "center", "right")):
    cell(f"/Fills/A{i}", value=h, fill="F2F2F2", **{"alignment.horizontal": h})
for i, v in zip((6, 7, 8), ("top", "center", "bottom")):
    cell(f"/Fills/C{i}", value={"center": "middle"}.get(v, v), fill="FCE4D6", **{"alignment.vertical": v})
    cell(f"/Fills/row[{i}]", height="34")

cell("/Fills/A10", value="This is a long sentence that wraps inside one cell via alignment.wrapText.", fill="E2EFDA", **{"alignment.wrapText": "true"})
cell("/Fills/A12", value="RTL reading order", fill="DDEBF7", **{"alignment.readingOrder": "rtl"})

# textRotation / indent / shrinkToFit — set directly on alignment (canonical keys).
cell("/Fills/A14", value="rotated 45deg", fill="FFF2CC", **{"alignment.textRotation": "45"})
cell("/Fills/row[14]", height="40")
cell("/Fills/A16", value="indented 3", fill="F2F2F2", **{"alignment.indent": "3"})
cell("/Fills/A18", value="ThisLongLabelShrinksToFit", fill="E2EFDA", **{"alignment.shrinkToFit": "true"})

cell("/Fills/col[1]", width="30")
cell("/Fills/col[3]", width="14")

# ==========================================================================
# Sheet3: Borders
# ==========================================================================
print("\n--- Sheet3: Borders ---")
cli(f'add "{FILE}" / --type sheet --prop name=Borders')
cell("/Borders/A1", value="Border styles", **{"font.bold": "true", "font.size": "14", "fill": "7030A0", "font.color": "FFFFFF"})

cell("/Borders/B3",  value="border=thin (all)",   border="thin")
cell("/Borders/B5",  value="border.all=medium",   **{"border.all": "medium"})
cell("/Borders/B7",  value="border + color",      border="thick", **{"border.color": "C00000"})
cell("/Borders/B9",  value="double bottom",       **{"border.bottom": "double"})
cell("/Borders/B11", value="dashed box",          **{"border.top": "dashed", "border.bottom": "dashed", "border.left": "dashed", "border.right": "dashed"})
cell("/Borders/B13", value="mixed sides",         **{"border.left": "thick", "border.top": "thin", "border.right": "medium", "border.bottom": "double"})
# Diagonal borders — direction via diagonalUp/Down, color requires a diagonal line.
cell("/Borders/B15", value="diagonal up",         **{"border.diagonal": "thin", "border.diagonalUp": "true"})
cell("/Borders/B17", value="diagonal down + color", **{"border.diagonal": "medium", "border.diagonalDown": "true", "border.diagonal.color": "C00000"})

cell("/Borders/col[1]", width="18")
cell("/Borders/col[2]", width="24")

# ==========================================================================
# Sheet4: Number formats
# ==========================================================================
print("\n--- Sheet4: Number formats ---")
cli(f'add "{FILE}" / --type sheet --prop name=Numbers')
cell("/Numbers/A1", value="numberformat codes", **{"font.bold": "true", "font.size": "14", "fill": "C55A11", "font.color": "FFFFFF"})
cell("/Numbers/A2", value="Format code", **{"font.bold": "true", "fill": "FCE4D6"})
cell("/Numbers/B2", value="Result",      **{"font.bold": "true", "fill": "FCE4D6"})

# (format code, raw value); A-label is the code itself, B-cell carries the format
NUM_ROWS = [
    ("#,##0",       "1234567"),
    ("#,##0.00",    "1234.5"),
    ("0.00%",       "0.1834"),
    ("$#,##0.00",   "29999.9"),
    ("yyyy-mm-dd",  "45413"),
    ("0.00E+00",    "602214"),
    ('_(* #,##0.00_);_(* (#,##0.00);_(* "-"??_)', "-4250"),
]
for i, (code, val) in enumerate(NUM_ROWS, start=3):
    # label cell: show the (short) code as literal text — type=string keeps
    # codes like "0.00E+00" from being parsed as a scientific-notation number.
    cell(f"/Numbers/A{i}", value=code.split(";")[0], type="string")
    cell(f"/Numbers/B{i}", value=val, numberformat=code)

cell("/Numbers/col[1]", width="28")
cell("/Numbers/col[2]", width="18")

# ==========================================================================
# Sheet5: Data — value/type, formula, link, locked, merge
# ==========================================================================
print("\n--- Sheet5: Data, formulas & links ---")
cli(f'add "{FILE}" / --type sheet --prop name=Data')
cell("/Data/A1", value="Values, formulas, links", **{"font.bold": "true", "font.size": "14", "fill": "2E75B6", "font.color": "FFFFFF"})

cell("/Data/A3", value="Qty");                 cell("/Data/B3", value="12")
cell("/Data/A4", value="Price");               cell("/Data/B4", value="4.5", numberformat="$#,##0.00")
cell("/Data/A5", value="Total", **{"font.bold": "true"})
cell("/Data/B5", formula="B3*B4", numberformat="$#,##0.00", **{"font.bold": "true"})

cell("/Data/A7", value="type=string on a numeric value", type="string")
cell("/Data/B7", value="007", type="string")

cell("/Data/A9", value="OfficeCLI on GitHub", link="https://github.com/iOfficeAI/OfficeCLI",
     tooltip="Open the repo", underline="single", **{"font.color": "0563C1"})

cell("/Data/A11", value="locked cell (effective when sheet is protected)", locked="true")

cell("/Data/A13", value="Merged title across A13:C13", merge="A13:C13", fill="DDEBF7",
     **{"alignment.horizontal": "center", "font.bold": "true"})

# Dynamic-array formula — spills the result across the ref range.
cell("/Data/A15", value="arrayformula = B3*2", **{"font.italic": "true"})
cell("/Data/B15", arrayformula="B3*2")

cell("/Data/col[1]", width="40")
cell("/Data/col[2]", width="16")

# ==========================================================================
# Sheet6: Rich-text — runs (multi-format text within one cell)
# ==========================================================================
# `runs` is an add-time property (requires --type cell + type=richtext).
# Each run is a JSON object with "text" plus any font props (bold, italic,
# color, size, underline).  `set` does not support rich-text; use `add`.
print("\n--- Sheet6: Rich-text runs ---")
cli(f'add "{FILE}" / --type sheet --prop name=RichText')

# Label
cell("/RichText/A1", value="runs — rich-text within one cell", **{"font.bold": "true", "font.size": "14", "fill": "5B2C8B", "font.color": "FFFFFF"})

# Each add creates the cell with multi-format text in a single SST entry.
# Shown once with inline --prop syntax so the example is self-documenting.
cli(f'add "{FILE}" /RichText --type cell --prop ref=A3'
    f' --prop type=richtext'
    f" --prop 'runs=[{{\"text\":\"Bold + Red  \",\"bold\":true,\"color\":\"C00000\"}},{{\"text\":\"Italic + Blue\",\"italic\":true,\"color\":\"2E75B6\"}},{{\"text\":\"  Normal\"}}]'")

cli(f'add "{FILE}" /RichText --type cell --prop ref=A5'
    f' --prop type=richtext'
    f" --prop 'runs=[{{\"text\":\"H\",\"bold\":true,\"color\":\"1F4E79\",\"size\":18}},{{\"text\":\"2\",\"superscript\":true,\"size\":10}},{{\"text\":\"O water formula\",\"color\":\"1F4E79\"}}]'")

cli(f'add "{FILE}" /RichText --type cell --prop ref=A7'
    f' --prop type=richtext'
    f" --prop 'runs=[{{\"text\":\"Strike\",\"strike\":true}},{{\"text\":\" | \"}},{{\"text\":\"underline\",\"underline\":\"single\"}},{{\"text\":\" | \"}},{{\"text\":\"size 14pt\",\"size\":14}}]'")

cell("/RichText/col[1]", width="50")

# flush resident edits to disk before reading back
close_file()

# ==========================================================================
# Set -> Get round-trip: confirm canonical keys read back
# ==========================================================================
print("\n--- Round-trip readback (Set then Get) ---")
for path, keys in [
    ("/Sheet1/B11", ("font.bold", "font.italic", "font.color", "font.size")),
    ("/Numbers/B6", ("value", "numberformat")),
    ("/Borders/B9", ("border.bottom",)),
]:
    r = subprocess.run(f'officecli get "{FILE}" "{path}" --json', shell=True, capture_output=True, text=True)
    import json
    try:
        fmt = json.loads(r.stdout)["data"]["results"][0]["format"]
    except Exception:
        fmt = {}
    shown = {k: fmt.get(k) for k in keys if k in fmt}
    print(f"  {path}: {shown}")

cli(f'validate "{FILE}"')
print(f"\nCreated: {FILE}")
