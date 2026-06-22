#!/usr/bin/env python3
"""
Demo: build a small sales report .xlsx using the officecli Python client.

Run:  python3 demo.py [path-to-officecli-binary]

Shows the whole loop over a single resident:
  create -> writes applied as one batch -> read back -> save -> close -> reopen.
"""

import os
import sys
import officecli   # the client (officecli.py next to this file)

# Locate the binary: 1st arg, else "officecli" on PATH.
BIN = sys.argv[1] if len(sys.argv) > 1 else "officecli"
OUT = os.path.abspath("sales_report.xlsx")

# Sample data: (region, units, price)
ROWS = [
    ("North", 120, 9.5),
    ("South", 95, 11.0),
    ("East",  140, 8.75),
    ("West",  60, 12.5),
    ("Central", 110, 10.0),
]

COL = "ABCDE"  # A..E


def cell(c, r):
    return f"/Sheet1/{c}{r}"


def main():
    # create returns a live handle bound to the resident it auto-starts.
    # --force overwrites a leftover from a previous run.
    with officecli.create(OUT, "--force", binary=BIN) as doc:   # make file + get handle
        # Build every write as a batch-shaped item, then apply them all in ONE
        # round-trip. Same dict shape officecli's `batch` command documents.
        items = []

        # Header row
        for j, title in enumerate(["Region", "Units", "Price", "Revenue"]):
            items.append({"command": "set", "path": cell(COL[j], 1),
                          "props": {"text": title, "bold": "true"}})

        # Data rows + a live formula for Revenue (=Units*Price)
        for i, (region, units, price) in enumerate(ROWS, start=2):
            items.append({"command": "set", "path": cell("A", i), "props": {"text": region}})
            items.append({"command": "set", "path": cell("B", i), "props": {"text": str(units)}})
            items.append({"command": "set", "path": cell("C", i), "props": {"text": str(price)}})
            items.append({"command": "set", "path": cell("D", i), "props": {"formula": f"=B{i}*C{i}"}})

        # Totals row
        last = len(ROWS) + 1
        items.append({"command": "set", "path": cell("A", last + 1),
                      "props": {"text": "TOTAL", "bold": "true"}})
        items.append({"command": "set", "path": cell("B", last + 1),
                      "props": {"formula": f"=SUM(B2:B{last})"}})
        items.append({"command": "set", "path": cell("D", last + 1),
                      "props": {"formula": f"=SUM(D2:D{last})"}})

        doc.batch(items)   # all writes, one pipe round-trip

        # Read one cell back over the pipe (single command, same dict shape).
        node = doc.send({"command": "get", "path": cell("A", 1)})
        results = node.get("data", {}).get("results", [{}])
        print("A1 reads back as:", results[0].get("text") if results else None)

        # In-session validate over the pipe (no extra process spawn). This is
        # the path that used to corrupt styles.xml; safe now that ValidateDocument
        # validates a clone instead of the live package.
        v = doc.send({"command": "validate"})
        print("validate (in-session):", "OK" if v.get("success") else v)

        doc.send({"command": "save"})   # flush in-memory doc to disk
    # context exit -> close the resident (which flushes to disk too)

    # Round-trip proof: reopen the CLOSED file fresh and confirm it both
    # validates and kept its content. open() does the one-shot bootstrap spawn
    # for us, so the demo stays entirely on the SDK — no hand-rolled subprocess.
    with officecli.open(OUT, binary=BIN) as doc:
        v = doc.send({"command": "validate"})
        print("validate (reopened):", "OK" if v.get("success") else v)
        a1 = doc.send({"command": "get", "path": cell("A", 1)})
        print("A1 after reopen:", a1.get("data", {}).get("results", [{}])[0].get("text"))

    print(f"wrote {OUT} ({os.path.getsize(OUT)} bytes)")


if __name__ == "__main__":
    main()
