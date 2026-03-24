---
name: mx-macro-data
description: “Use when the user asks for macroeconomic data, economic indicators, GDP figures, CPI, inflation rates, money supply, commodity prices, or needs economic statistics exported to CSV. Queries macroeconomic data via natural language, converts results to CSV files grouped by frequency, and generates description files. Requires explicit entity names, dates, and indicator names.”
---

# 宏观经济数据查询 (MX_MacroData)

## Workflow

1. **Pre-process the query**: Resolve all ambiguous terms before calling this tool. Convert region groups to specific names, relative dates to absolute `YYYY-MM-DD` or `YYYY-Qx` ranges, and category labels to specific indicator/commodity names
2. **Execute query**: Run `python -m scripts.get_data “<processed_query>”`
3. **Check for errors**: If the tool returns an error, identify which constraint was violated, fix the query, and retry
4. **Verify output**: Confirm CSV and description files exist in the output directory
5. **Report results**: Summarize the data from the description file to the user

## Input Constraints

All inputs must use absolute, explicit values. Resolve any ambiguity before calling.

| Constraint | Forbidden | Required |
|-----------|-----------|----------|
| Regions | Group names (华东五市, 金砖国家) | Specific names (上海市, 南京市, ...) |
| Commodities | Category labels (稀土金属, 能源) | Specific names (氧化镨钕, 铜, 铝) |
| Rankings | Relative (GDP Top 5, 最高的三个) | Explicit entity list |
| Time | Relative (过去三年, 疫情期间) | Absolute dates (2007-12至2009-06) |
| Indicators | Broad concepts (中国经济) | Specific names (GDP同比增速, CPI同比) |

## Supported Data

- Economic indicators: GDP, CPI, PPI, PMI, unemployment, industrial output
- Monetary/financial: M1/M2 money supply, government bond rates, exchange rates
- Commodity prices: gold, silver, crude oil, copper, rare earth oxides
- Output frequency: auto-grouped by year, quarter, month, week, or day

## Quick Start

```bash
python -m scripts.get_data “中国GDP”
```

Or via pipe:
```bash
echo “白银价格” | python -m scripts.get_data
```

Output:
```
CSV: workspace/macro_data/macro_data_中国GDP_年.csv
CSV: workspace/macro_data/macro_data_中国GDP_季.csv
描述: workspace/macro_data/macro_data_中国GDP_description.txt
行数: 年: 10行, 季: 40行
```

## Error Handling

If a constraint is violated, the tool returns an error without executing:
- `Error: Ambiguous Region Detected` -- resolve to specific city/country names
- `Error: Ambiguous Commodity Category` -- resolve to exact commodity names
- `Error: Relative Time/Ranking Detected` -- resolve to specific dates or entity lists

## Output Files

| File | Description |
|------|-------------|
| `macro_data_<summary>_<freq>.csv` | Frequency-grouped data table, UTF-8, compatible with Excel/pandas |
| `macro_data_<summary>_description.txt` | Data statistics, sources, and unit information |


