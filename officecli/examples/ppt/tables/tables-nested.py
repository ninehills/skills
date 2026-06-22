#!/usr/bin/env python3
"""
tables-nested.py — BUILD, NAVIGATE, and FULLY EXERCISE a nested pptx element:
the table tree (slide → table → tr → tc). 4 slides, so the full property surface
of each level fits without cramming one table:

  Slide 1  Structure & ownership — levels, path tokens, property ownership,
                                   colspan, navigation/readback.
  Slide 2  Table-level surface   — every `table` property (banding, fills,
                                   per-side borders, sizing, name/zorder, data).
  Slide 3  Cell box surface      — every `tc` box property (borders incl.
                                   diagonals, padding, valign, wrap, textdir,
                                   direction, bevel, opacity, image fill, merge).
  Slide 4  Cell text surface     — every `tc` text property (font, size, weight,
                                   underline/strike, color, align, line/para spacing).

Coverage: 100% of the settable props on pptx table / table-row / table-cell
(`help pptx <element> --json`), EXCEPT `id` — table ids are auto-assigned and
must stay unique, so it's settable for round-trip fidelity but never set by hand.

SDK twin of tables-nested.sh, mapped one-for-one (no batch):
    officecli.create(...)  ≈ create + open ;  doc.send({...}) ≈ one set/add ;  doc.close() ≈ close

Usage:
  pip install officecli-sdk
  python3 tables-nested.py
"""

import os
import base64
import officecli  # pip install officecli-sdk

FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tables-nested.pptx")
# 1x1 PNG for the cell image-fill demo (slide 3); image= needs a real file path.
IMG = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".cell-dot.png")
with open(IMG, "wb") as fh:
    fh.write(base64.b64decode(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="))

print(f"Building {FILE} ...")
doc = officecli.create(FILE, "--force")          # create the .pptx + start its resident


def add(parent, type_, **props):
    doc.send({"command": "add", "parent": parent, "type": type_, "props": props})


def setp(path, **props):                         # one `officecli set`
    doc.send({"command": "set", "path": path, "props": props})


def cell(sl, p, **props):                        # set on /slide[sl]/table[1]/<p>
    setp(f"/slide[{sl}]/table[1]/{p}", **props)


def title(sl, text):
    add(f"/slide[{sl}]", "shape", geometry="rect", x="1.2cm", y="0.6cm",
        width="30cm", height="1.3cm", fill="none", line="none",
        text=text, size="20", bold="true", color="1F4E79")


# ═══════════════ SLIDE 1 — Structure & ownership ═══════════════
add("/", "slide")
title(1, "1 · Structure & ownership  (slide → table → tr → tc)")
# rows/cols/colWidths are add-time structure; style/banding are settable later.
add("/slide[1]", "table", rows="5", cols="3", x="2.5cm", y="2.4cm",
    width="28cm", height="9cm", colWidths="12cm,8cm,8cm")           # → /slide[1]/table[1]
setp("/slide[1]/table[1]", style="medium2-accent1", firstRow="true", bandedRows="true")  # table owns style + banding
setp("/slide[1]/table[1]/tr[1]", height="2cm")                      # a row owns only its height
for c, label in enumerate(["Region", "Units", "Revenue"], start=1):
    cell(1, f"tr[1]/tc[{c}]", text=label, bold="true", color="FFFFFF",
         align="center", valign="middle", fill="1F6FEB")            # cell owns box + text
for r, (region, units, rev) in enumerate(
        [("North", "1,240", "$11,780"), ("South", "980", "$9,310"),
         ("East", "1,520", "$14,440")], start=2):
    cell(1, f"tr[{r}]/tc[1]", text=region, align="left", valign="middle")
    cell(1, f"tr[{r}]/tc[2]", text=units, align="right", valign="middle")
    cell(1, f"tr[{r}]/tc[3]", text=rev, align="right", valign="middle")
# Nesting-only op: colspan (alias gridspan). Total row spans all 3 columns.
cell(1, "tr[5]/tc[1]", colspan="3", valign="middle", bold="true", align="center",
     text="TOTAL    3,740 units    $35,530", fill="DDEBF7")
# Navigate: address a deep node AFTER building — same path that built it reaches it.
node = doc.send({"command": "get", "path": "/slide[1]/table[1]/tr[4]/tc[3]"})
print("  deep readback:", node.get("data", {}).get("results", [{}])[0].get("text"))
cell(1, "tr[4]/tc[3]", fill="FFF2CC", bold="true")

# ═══════════════ SLIDE 2 — Table-level full surface ═══════════════
add("/", "slide")
title(2, "2 · Table level — banding · fills · per-side borders · sizing")
# data= bulk-fills + defines the grid (rows ';', cols ','); zorder/rowHeight/
# colWidths/header+body fills are add-only. `id` is intentionally omitted (auto).
add("/slide[2]", "table", x="2cm", y="2.4cm", width="29cm", height="9cm",
    data="Q,FY24,FY25,Growth;Q1,120,138,+15%;Q2,95,121,+27%;Q3,140,162,+16%",
    zorder="2", rowHeight="1.8cm", colWidths="8cm,7cm,7cm,7cm",
    headerFill="1F6FEB", bodyFill="EEF3FB")
setp("/slide[2]/table[1]", name="QuarterlySales",
     firstRow="true", lastRow="true", firstCol="true", lastCol="false",
     bandedRows="true", bandedCols="false")
setp("/slide[2]/table[1]", **{
    "border.all": "1pt solid B7C7E0",
    "border.top": "3pt solid 1F4E79", "border.bottom": "3pt solid 1F4E79",
    "border.left": "1.5pt solid 1F6FEB", "border.right": "1.5pt solid 1F6FEB",
    "border.horizontal": "1pt solid CCD8EC", "border.vertical": "1pt solid CCD8EC"})

# ═══════════════ SLIDE 3 — Cell box full surface ═══════════════
add("/", "slide")
title(3, "3 · Cell box — borders · padding · valign · direction · bevel · opacity · image · merge")
add("/slide[3]", "table", rows="5", cols="4", x="2cm", y="2.4cm",
    width="29cm", height="12cm", style="none")
# Row 1 — per-side, full, and diagonal borders (one kind per cell)
cell(3, "tr[1]/tc[1]", text="border.all", **{"border.all": "1.5pt solid 1F6FEB"})
cell(3, "tr[1]/tc[2]", text="top+bottom", **{"border.top": "3pt solid C00000", "border.bottom": "3pt solid C00000"})
cell(3, "tr[1]/tc[3]", text="left+right", **{"border.left": "3pt solid 2DA44E", "border.right": "3pt solid 2DA44E"})
cell(3, "tr[1]/tc[4]", text="diagonals", **{"border.tl2br": "1.5pt solid BF8700", "border.tr2bl": "1.5pt solid BF8700"})
# Row 2 — fill, opacity, bevel, image fill
cell(3, "tr[2]/tc[1]", text="fill", fill="FFE699")
cell(3, "tr[2]/tc[2]", text="opacity=0.5", fill="1F6FEB", opacity="0.5")
cell(3, "tr[2]/tc[3]", text="bevel=circle", fill="DDEBF7", bevel="circle")
cell(3, "tr[2]/tc[4]", text="image fill", image=IMG)
# Row 3 — padding, padding.bottom, valign (top + bottom)
cell(3, "tr[3]/tc[1]", text="padding=0.4cm", padding="0.4cm", fill="F2F2F2")
cell(3, "tr[3]/tc[2]", text="padding.bottom=0.5cm", **{"padding.bottom": "0.5cm", "fill": "F2F2F2"})
cell(3, "tr[3]/tc[3]", text="valign=top", valign="top", fill="F2F2F2")
cell(3, "tr[3]/tc[4]", text="valign=bottom", valign="bottom", fill="F2F2F2")
# Row 4 — wrap, vertical text, RTL, and merge.down (eats the cell below it)
cell(3, "tr[4]/tc[1]", text="wrap=false: this long line will not wrap inside the cell", wrap="false", fill="E2EFDA")
cell(3, "tr[4]/tc[2]", text="textdir=vertical270", textdirection="vertical270", fill="E2EFDA")
cell(3, "tr[4]/tc[3]", text="direction=rtl العربية", direction="rtl", fill="E2EFDA")
cell(3, "tr[4]/tc[4]", text="merge.down=1 ↓", align="center", fill="FCE4D6", **{"merge.down": "1"})
# Row 5 — merge.right (eats the cell to its right); tc[4] swallowed by merge.down above.
cell(3, "tr[5]/tc[1]", text="merge.right=2 →", align="center", fill="FCE4D6", **{"merge.right": "2"})

# ═══════════════ SLIDE 4 — Cell text full surface ═══════════════
add("/", "slide")
title(4, "4 · Cell text — font · size · weight · underline/strike · color · align · spacing")
add("/slide[4]", "table", rows="4", cols="3", x="2cm", y="2.4cm",
    width="29cm", height="10cm", style="light1")
cell(4, "tr[1]/tc[1]", text="font=Georgia", font="Georgia")
cell(4, "tr[1]/tc[2]", text="size=20pt", size="20pt")
cell(4, "tr[1]/tc[3]", text="color", color="C00000")
cell(4, "tr[2]/tc[1]", text="bold", bold="true")
cell(4, "tr[2]/tc[2]", text="italic", italic="true")
cell(4, "tr[2]/tc[3]", text="underline", underline="double")
cell(4, "tr[3]/tc[1]", text="strike", strike="single")
cell(4, "tr[3]/tc[2]", text="align=center", align="center")
cell(4, "tr[3]/tc[3]", text="align=right", align="right")
cell(4, "tr[4]/tc[1]", text="linespacing=1.5x — line one is followed by line two in this cell", linespacing="1.5x")
cell(4, "tr[4]/tc[2]", text="spacebefore=10pt", spacebefore="10pt")
cell(4, "tr[4]/tc[3]", text="spaceafter=10pt", spaceafter="10pt")
setp("/slide[4]/table[1]/tr[4]", height="2.4cm")     # table-row also owns height

# Validate over the pipe (in-session), then close.
v = doc.send({"command": "validate"})
print("  Validation passed: no errors found." if v.get("success") else f"  {v.get('warnings')}")
doc.close()
os.remove(IMG)
print(f"Created: {FILE}")
