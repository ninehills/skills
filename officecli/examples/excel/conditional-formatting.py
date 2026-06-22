#!/usr/bin/env python3
"""
Conditional Formatting Showcase — generates conditional-formatting.xlsx
exercising the full xlsx `conditionalformatting` rule family
(schemas/help/xlsx/conditionalformatting.json).

Unlike the other excel/*.py (which shell out per command), this one drives the
**officecli Python SDK** (`pip install officecli-sdk`): one resident is started,
every write goes over the named pipe, and all the rules for a sheet are applied
in a single `doc.batch(...)` round-trip. Same `{"command","parent","type",
"props"}` dict shape you'd put in an `officecli batch` list.

7 sheets, one rule family each:
  CellIs       — greaterThan/lessThan/between/equal/notEqual + fill
  Text         — containsText/notContainsText/beginsWith/endsWith + needle + fill
  TopBottom    — top10/topN/topPercent/bottom + aboveAverage/belowAverage (+stdDev)
  DataBars     — bar color, min/max, negativeColor, axisColor/Position, showValue
  ColorScales  — 2-colour (min/max) and 3-colour (min/mid/max + midPoint) scales
  IconSets     — 3TrafficLights/3Arrows/4Rating/5Rating, reverse, showValue
  FormulaEtc   — formula, dateOccurring, duplicateValues, uniqueValues

Closes with a Get round-trip proving the canonical keys read back.

Usage:
  pip install officecli-sdk          # plus the `officecli` binary on PATH
  python3 conditional-formatting.py
"""

import os
import sys
import subprocess

# --- locate the SDK: prefer an installed `officecli-sdk`, else the in-repo copy
try:
    import officecli  # pip install officecli-sdk
except ImportError:
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "..", "sdk", "python"))
    import officecli

FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "conditional-formatting.xlsx")


def col_data(sheet, start_row, values, **header):
    """Build batch `set` items writing `values` down a column from A{start_row}.
    Returns the item list (caller batches them)."""
    items = []
    if header:
        items.append({"command": "set", "path": f"/{sheet}/A{start_row - 1}",
                      "props": {"value": header.pop("title"), "font.bold": "true",
                                "fill": "1F4E79", "font.color": "FFFFFF", **header}})
    for i, v in enumerate(values, start=start_row):
        items.append({"command": "set", "path": f"/{sheet}/A{i}", "props": {"value": str(v)}})
    return items


def cf(sheet, **props):
    """One `add conditionalformatting` item in batch-shape."""
    return {"command": "add", "parent": f"/{sheet}", "type": "conditionalformatting", "props": props}


def add_sheet(name):
    return {"command": "add", "parent": "/", "type": "sheet", "props": {"name": name}}


def label(sheet, ref, text):
    return {"command": "set", "path": f"/{sheet}/{ref}",
            "props": {"value": text, "font.italic": "true", "font.color": "595959"}}


print("\n==========================================")
print(f"Generating conditional-formatting showcase: {FILE}")
print("==========================================")

with officecli.create(FILE, "--force") as doc:

    # ======================================================================
    # Sheet1: CellIs — value-comparison rules
    # ======================================================================
    print("\n--- Sheet1: CellIs (comparison) ---")
    scores = [42, 58, 91, 73, 30, 88, 65, 100, 12, 77]
    items = col_data("Sheet1", 2, scores, title="Scores")
    items += [
        # one column per operator so each rule's effect is visible side-by-side
        cf("Sheet1", type="cellIs", ref="A2:A11", operator="greaterThan", value="80", fill="C6EFCE"),
        cf("Sheet1", type="cellIs", ref="A2:A11", operator="lessThan", value="40", fill="FFC7CE"),
        cf("Sheet1", type="cellIs", ref="A2:A11", operator="between", value="50", value2="70", fill="FFEB9C"),
        cf("Sheet1", type="cellIs", ref="A2:A11", operator="equal", value="100", fill="63BE7B"),
        label("Sheet1", "C2", ">80 green · <40 red · 50-70 amber · =100 deep-green"),
    ]
    doc.batch(items)

    # ======================================================================
    # Sheet2: Text rules — needle matching
    # ======================================================================
    print("--- Sheet2: Text rules ---")
    words = ["ERROR: timeout", "ok", "WARNING low", "error code 5", "passed",
             "Begins here", "ends with END", "neutral"]
    items = [add_sheet("Text")] + col_data("Text", 2, words, title="Log line")
    items += [
        cf("Text", type="containsText", ref="A2:A9", text="error", fill="FFC7CE"),
        cf("Text", type="notContainsText", ref="A2:A9", text="error", fill="C6EFCE"),
        cf("Text", type="beginsWith", ref="A2:A9", text="Begins", fill="BDD7EE"),
        cf("Text", type="endsWith", ref="A2:A9", text="END", fill="FFE699"),
        label("Text", "C2", "contains 'error' red · begins 'Begins' blue · ends 'END' gold"),
    ]
    doc.batch(items)

    # ======================================================================
    # Sheet3: Top / Bottom / Average
    # ======================================================================
    print("--- Sheet3: Top/Bottom/Average ---")
    revenue = [120, 340, 90, 510, 275, 60, 430, 180, 295, 75, 360, 145]
    items = [add_sheet("TopBottom")] + col_data("TopBottom", 2, revenue, title="Revenue")
    items += [
        cf("TopBottom", type="top10", ref="A2:A13", rank="3", fill="C6EFCE"),         # top 3 values
        cf("TopBottom", type="bottom", ref="A2:A13", rank="3", fill="FFC7CE"),         # bottom 3 values
        cf("TopBottom", type="topPercent", ref="A2:A13", rank="25", percent="true", fill="63BE7B"),
        cf("TopBottom", type="aboveAverage", ref="A2:A13", aboveAverage="true", fill="BDD7EE"),
        cf("TopBottom", type="belowAverage", ref="A2:A13", aboveAverage="false", fill="F8CBAD"),
        cf("TopBottom", type="aboveAverage", ref="A2:A13", aboveAverage="true", stdDev="1", fill="FFEB9C"),  # >1 sigma
        label("TopBottom", "C2", "top3 / bottom3 / top25% / above & below avg / >1 sigma"),
    ]
    doc.batch(items)

    # ======================================================================
    # Sheet4: Data bars
    # ======================================================================
    print("--- Sheet4: Data bars ---")
    netflow = [120, -45, 300, -80, 210, 60, -150, 90, 175, -30]
    items = [add_sheet("DataBars")] + col_data("DataBars", 2, netflow, title="Net flow")
    items += [
        # gradient-style bar with explicit scale, negative bars + axis styling
        cf("DataBars", type="dataBar", ref="A2:A11", color="638EC6", min="auto", max="auto",
           negativeColor="FF0000", axisColor="000000", axisPosition="middle", showValue="true"),
        label("DataBars", "C2", "blue bars, red negatives, mid axis, values shown"),
    ]
    doc.batch(items)

    # ======================================================================
    # Sheet5: Color scales
    # ======================================================================
    print("--- Sheet5: Color scales ---")
    heat = [10, 25, 40, 55, 70, 85, 100, 30, 60, 90]
    items = [add_sheet("ColorScales")]
    items += col_data("ColorScales", 2, heat, title="2-colour")
    # second column for the 3-colour scale
    items += [{"command": "set", "path": f"/ColorScales/B{i}", "props": {"value": str(v)}}
              for i, v in enumerate(heat, start=2)]
    items += [{"command": "set", "path": "/ColorScales/B1",
               "props": {"value": "3-colour", "font.bold": "true", "fill": "1F4E79", "font.color": "FFFFFF"}}]
    items += [
        cf("ColorScales", type="colorScale", ref="A2:A11", minColor="FFFFFF", maxColor="63BE7B"),
        cf("ColorScales", type="colorScale", ref="B2:B11", minColor="F8696B", midColor="FFEB84",
           maxColor="63BE7B", midPoint="50"),
        label("ColorScales", "D2", "A: white-to-green 2-stop · B: red-amber-green 3-stop @ 50%"),
    ]
    doc.batch(items)

    # ======================================================================
    # Sheet6: Icon sets
    # ======================================================================
    print("--- Sheet6: Icon sets ---")
    ratings = [1, 2, 3, 4, 5, 2, 4, 5, 1, 3]
    items = [add_sheet("IconSets")]
    cols = [("A", "3TrafficLights1", "false"), ("B", "3Arrows", "false"),
            ("C", "5Rating", "false"), ("D", "3TrafficLights1", "true")]
    for c, _, _ in cols:
        items += [{"command": "set", "path": f"/IconSets/{c}{i}", "props": {"value": str(v)}}
                  for i, v in enumerate(ratings, start=2)]
    for c, name, rev in cols:
        items.append({"command": "set", "path": f"/IconSets/{c}1",
                      "props": {"value": f"{name}{' rev' if rev == 'true' else ''}",
                                "font.bold": "true", "fill": "1F4E79", "font.color": "FFFFFF"}})
        items.append(cf("IconSets", type="iconSet", ref=f"{c}2:{c}11", iconset=name,
                        reverse=rev, showValue="true"))
    items.append(label("IconSets", "F2", "lights / arrows / 5-rating / reversed lights"))
    doc.batch(items)

    # ======================================================================
    # Sheet7: Formula, date-occurring, duplicate / unique
    # ======================================================================
    print("--- Sheet7: Formula / date / dup / unique ---")
    nums = [4, 7, 4, 9, 2, 7, 5, 1, 9, 3]
    items = [add_sheet("FormulaEtc")] + col_data("FormulaEtc", 2, nums, title="Value")
    # a date column for dateOccurring
    items += [{"command": "set", "path": f"/FormulaEtc/B{i}",
               "props": {"value": d, "numberformat": "yyyy-mm-dd"}}
              for i, d in enumerate(["45800", "45810", "45820", "45830", "45840",
                                     "45850", "45860", "45870", "45880", "45890"], start=2)]
    items += [{"command": "set", "path": "/FormulaEtc/B1",
               "props": {"value": "Date", "font.bold": "true", "fill": "1F4E79", "font.color": "FFFFFF"}}]
    items += [
        cf("FormulaEtc", type="formula", ref="A2:A11", formula="ISODD(A2)", fill="BDD7EE"),   # odd values
        cf("FormulaEtc", type="duplicateValues", ref="A2:A11", fill="FFC7CE"),
        cf("FormulaEtc", type="uniqueValues", ref="A2:A11", fill="C6EFCE"),
        cf("FormulaEtc", type="dateOccurring", ref="B2:B11", period="thisMonth", fill="FFEB9C"),
        label("FormulaEtc", "D2", "A: odd=blue, dup=red, unique=green · B: this-month=amber"),
    ]
    doc.batch(items)

    # ======================================================================
    # Get round-trip: confirm canonical keys read back (in-session, over pipe)
    # ======================================================================
    print("\n--- Round-trip readback (Get the rules) ---")
    for path in ["/Sheet1/cf[1]", "/DataBars/cf[1]", "/ColorScales/cf[2]", "/IconSets/cf[1]"]:
        node = doc.send({"command": "get", "path": path})
        fmt = node.get("data", {}).get("results", [{}])[0].get("format", {})
        keys = ("type", "ref", "operator", "value", "fill", "color", "minColor", "maxColor",
                "midColor", "iconset", "reverse")
        shown = {k: fmt.get(k) for k in keys if k in fmt}
        print(f"  {path}: {shown}")

    doc.send({"command": "save"})
# context exit closes the resident, flushing the workbook to disk.

# Validate the SAVED file with a fresh one-shot process (NOT in-session): a
# conditional-formatting rule's fill lives in the workbook-level <dxfs> table in
# styles.xml, so validate from disk to confirm those dxf references resolved.
print("\n--- Validate (fresh process, from disk) ---")
r = subprocess.run(["officecli", "validate", FILE], capture_output=True, text=True)
print(" ", (r.stdout or r.stderr).strip().split("\n")[0])

print(f"\nCreated: {FILE}")
