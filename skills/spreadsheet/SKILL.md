---
name: "spreadsheet"
description: "Use when tasks involve creating, editing, analyzing, or formatting spreadsheets (`.xlsx`, `.csv`, `.tsv`) using Python (`openpyxl`, `pandas`), especially when formulas, references, and formatting need to be preserved and verified."
---


# Spreadsheet Skill (Create, Edit, Analyze, Visualize)

## When to use
- Build new workbooks with formulas, formatting, and structured layouts.
- Read or analyze tabular data (filter, aggregate, pivot, compute metrics).
- Modify existing workbooks without breaking formulas or references.
- Visualize data with charts/tables and sensible formatting.

IMPORTANT: System and user instructions always take precedence.

## Workflow
1. **Identify goals**: Confirm file type and task (create, edit, analyze, visualize).
2. **Choose tooling**: `openpyxl` for `.xlsx` edits, `pandas` for analysis and CSV/TSV.
3. **Implement**: Build or modify the spreadsheet.
4. **Validate**: Check for formula errors (`#REF!`, `#DIV/0!`, `#VALUE!`) and verify references are intact.
5. **Visual review**: If layout matters, render to PDF/PNG (see Rendering section).
6. **Save and clean up**: Write final output, remove intermediate files.

## Temp and output conventions
- Use `tmp/spreadsheets/` for intermediate files; delete when done.
- Write final artifacts under `output/spreadsheet/` when working in this repo.
- Keep filenames stable and descriptive.

## Primary tooling
- Use `openpyxl` for creating/editing `.xlsx` files and preserving formatting.
- Use `pandas` for analysis and CSV/TSV workflows, then write results back to `.xlsx` or `.csv`.
- If you need charts, prefer `openpyxl.chart` for native Excel charts.

## Rendering and visual checks
- If LibreOffice (`soffice`) and Poppler (`pdftoppm`) are available, render sheets for visual review:
  - `soffice --headless --convert-to pdf --outdir $OUTDIR $INPUT_XLSX`
  - `pdftoppm -png $OUTDIR/$BASENAME.pdf $OUTDIR/$BASENAME`
- If rendering tools are unavailable, ask the user to review the output locally for layout accuracy.

## Common Patterns

**Create a workbook with formulas:**
```python
from openpyxl import Workbook
wb = Workbook()
ws = wb.active
ws["A1"], ws["B1"] = "Revenue", "Tax"
ws["A2"], ws["B2"] = 100000, "=A2*0.21"
wb.save("output/spreadsheet/report.xlsx")
```

**Read and analyze CSV:**
```python
import pandas as pd
df = pd.read_csv("data.csv")
summary = df.groupby("category")["amount"].sum()
summary.to_excel("output/spreadsheet/summary.xlsx")
```

## Dependencies (install if missing)

```bash
uv pip install openpyxl pandas          # preferred
python3 -m pip install openpyxl pandas  # fallback
uv pip install matplotlib               # optional: charts
```

Rendering tools: `brew install libreoffice poppler` (macOS) or `apt-get install -y libreoffice poppler-utils` (Debian/Ubuntu).

## Environment
No required environment variables.

## Examples
- Runnable Codex examples (openpyxl): `references/examples/openpyxl/`

## Formula requirements
- Use formulas for derived values rather than hardcoding results.
- Keep formulas simple and legible; use helper cells for complex logic.
- Avoid volatile functions like INDIRECT and OFFSET unless required.
- Prefer cell references over magic numbers (e.g., `=H6*(1+$B$3)` not `=H6*1.04`).
- Guard against errors (#REF!, #DIV/0!, #VALUE!, #N/A, #NAME?) with validation and checks.
- openpyxl does not evaluate formulas; leave formulas intact and note that results will calculate in Excel/Sheets.

## Citation requirements
- Cite sources inside the spreadsheet using plain text URLs.
- For financial models, cite sources of inputs in cell comments.
- For tabular data sourced from the web, include a Source column with URLs.

## Formatting requirements (existing formatted spreadsheets)
- Render and inspect a provided spreadsheet before modifying it when possible.
- Preserve existing formatting and style exactly.
- Match styles for any newly filled cells that were previously blank.

## Formatting requirements (new or unstyled spreadsheets)
- Use appropriate number and date formats (dates as dates, currency with symbols, percentages with sensible precision).
- Use a clean visual layout: headers distinct from data, consistent spacing, and readable column widths.
- Avoid borders around every cell; use whitespace and selective borders to structure sections.
- Ensure text does not spill into adjacent cells.

## Color conventions (if no style guidance)
- Blue: user input
- Black: formulas/derived values
- Green: linked/imported values
- Gray: static constants
- Orange: review/caution
- Light red: error/flag
- Purple: control/logic
- Teal: visualization anchors (key KPIs or chart drivers)

## Finance-specific requirements
- Format zeros as "-".
- Negative numbers should be red and in parentheses.
- Always specify units in headers (e.g., "Revenue ($mm)").
- Cite sources for all raw inputs in cell comments.

## Investment banking layouts
If the spreadsheet is an IB-style model (LBO, DCF, 3-statement, valuation):
- Totals should sum the range directly above.
- Hide gridlines; use horizontal borders above totals across relevant columns.
- Section headers should be merged cells with dark fill and white text.
- Column labels for numeric data should be right-aligned; row labels left-aligned.
- Indent submetrics under their parent line items.
