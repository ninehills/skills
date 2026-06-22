#!/usr/bin/env python3
"""
Workbook Settings Showcase — generates workbook-settings.xlsx exercising the
full xlsx `workbook` property surface (schemas/help/xlsx/workbook.json): the
workbook-level settings with no per-cell or per-sheet equivalent.

`workbook` is a read-only container addressed at path "/"; you never add or
remove it, only `set`/`get`. Four groups: metadata, calc engine,
protection/display, theme.

SDK twin of workbook-settings.sh. Drives the officecli Python SDK
(`pip install officecli-sdk`) and maps onto the shell script one-for-one:

    officecli.create(...)          ≈  officecli create + open  (file + resident)
    doc.send({...})                ≈  one officecli set/add    (one call each, no batch)
    doc.close()                    ≈  officecli close          (flush to disk)

Usage:
  pip install officecli-sdk          # plus the `officecli` binary on PATH
  python3 workbook-settings.py
"""

import os
import officecli  # pip install officecli-sdk

FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "workbook-settings.xlsx")

print("\n==========================================")
print(f"Generating workbook-settings showcase: {FILE}")
print("==========================================")

doc = officecli.create(FILE, "--force")      # create the .xlsx + start its resident


def cell(path, **props):                     # one cell write = one `officecli set`
    doc.send({"command": "set", "path": path, "props": props})


def wb(**props):                             # one workbook-container `set`
    doc.send({"command": "set", "path": "/", "props": props})


# --- A small data sheet + a live formula (governed by calc.mode) ---
print("\n--- Data sheet ---")
cell("/Sheet1/A1", value="Region", **{"font.bold": "true"})
cell("/Sheet1/B1", value="Units", **{"font.bold": "true"})
cell("/Sheet1/C1", value="Price", **{"font.bold": "true"})
cell("/Sheet1/D1", value="Revenue", **{"font.bold": "true"})
rows = [("North", 120, 9.5), ("South", 95, 11.0), ("East", 140, 8.75)]
for i, (region, units, price) in enumerate(rows, start=2):
    cell(f"/Sheet1/A{i}", value=region)
    cell(f"/Sheet1/B{i}", value=str(units))
    cell(f"/Sheet1/C{i}", value=str(price))
    cell(f"/Sheet1/D{i}", formula=f"=B{i}*C{i}", numberformat="$#,##0.00")
last = len(rows) + 2
cell(f"/Sheet1/D{last}", formula=f"=SUM(D2:D{last - 1})",
     numberformat="$#,##0.00", **{"font.bold": "true"})

# --- 1. Metadata (core + extended) ---
print("--- Metadata ---")
wb(author="Jane Author", title="2026 Revenue Model", subject="Finance",
   keywords="finance,2026,model", description="Annual revenue summary.",
   category="Reports", lastModifiedBy="Editorial", revisionNumber="3")
wb(**{"extended.company": "Acme Corp", "extended.manager": "Dana Lead",
      "extended.template": "Book.xltx"})

# --- 2. Calc engine ---
print("--- Calc engine ---")
wb(**{"calc.mode": "manual",                 # auto | manual | autoNoTable
      "calc.iterate": "true",                # allow circular-reference iteration
      "calc.iterateCount": "100",
      "calc.iterateDelta": "0.001",
      "calc.fullPrecision": "true"})         # full precision, not as-displayed

# --- 3. Protection & display ---
print("--- Protection & display ---")
wb(**{"workbook.lockStructure": "true",      # can't add/delete/rename sheets
      "workbook.lockWindows": "false",
      "workbook.password": "secret",         # structure-protection password
      "workbook.dateCompatibility": "false",  # false = 1900 date system
      "workbook.filterPrivacy": "true",
      "workbook.showObjects": "all"})        # all | placeholders | none

# --- 4. Theme — palette (dk/lt + accent1..6) and major/minor fonts ---
print("--- Theme ---")
wb(**{"theme.color.dk1": "1A1A1A", "theme.color.lt1": "FFFFFF",
      "theme.color.dk2": "2F3640", "theme.color.lt2": "EEF1F5",
      "theme.color.accent1": "1F6FEB", "theme.color.accent2": "E3572A",
      "theme.color.accent3": "2DA44E", "theme.color.accent4": "BF8700",
      "theme.color.accent5": "8250DF", "theme.color.accent6": "1B7C83",
      "theme.color.hlink": "0969DA", "theme.color.folHlink": "8250DF"})
wb(**{"theme.font.major.latin": "Georgia", "theme.font.minor.latin": "Calibri",
      "theme.font.major.eastAsia": "SimHei", "theme.font.minor.eastAsia": "SimSun"})

# --- Get round-trip: confirm canonical keys read back (over the pipe) ---
print("\n--- Round-trip readback (get / ) ---")
node = doc.send({"command": "get", "path": "/"})
fmt = node.get("data", {}).get("results", [{}])[0].get("format", {})
for k in ["author", "title", "category", "revisionNumber", "calc.mode",
          "calc.iterate", "calc.iterateCount", "workbook.lockStructure",
          "workbook.showObjects", "theme.color.accent1", "theme.font.major.latin"]:
    if k in fmt:
        print(f"  {k} = {fmt[k]}")

# --- Validate over the pipe (in-session, no extra process) ---
print("\n--- Validate ---")
v = doc.send({"command": "validate"})
print("  Validation passed: no errors found." if v.get("success")
      else f"  {v.get('warnings')}")

doc.close()                                  # stop the resident (flushes to disk)
print(f"\nCreated: {FILE}")
