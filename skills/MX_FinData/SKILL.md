---
name: mx-fin-data
description: “Use when the user asks to query financial data, look up stock prices, check real-time quotes, retrieve financial statements, or analyze financial indicators. Fetches structured data for stocks (A-shares, HK, US), sectors, indices, bonds, funds, and unlisted companies via natural language queries. Outputs xlsx data files and txt description files. Requires EM_API_KEY.”
metadata:
  {
    “openclaw”: {
      “requires”: {
        “env”: [“EM_API_KEY”],
        “bins”: [“python3”, “pip3”]
      },
      “install”: [
        {
          “id”: “pip-deps”,
          “kind”: “python”,
          “package”: “httpx pandas”,
          “label”: “Install Python dependencies”
        }
      ]
    }
  }
---

# 金融数据查询 (MX_FinData)

## Workflow

1. **Receive query**: Accept a natural language query containing explicit financial entity names and indicators
2. **Validate limits**: Ensure the query has at most 5 entities and 3 indicators (truncate if exceeded)
3. **Execute**: Run `python3 scripts/get_data.py --query “<query>”`
4. **Verify output**: Confirm both xlsx and txt files were created in the output directory
5. **Report results**: Read the description txt file and summarize findings to the user

## Supported Query Objects

- Stocks (A-shares, HK stocks, US stocks), sectors, indices, shareholders
- Bond issuers, bonds, unlisted companies
- Stock/fund/bond markets

## Supported Data Types

- Real-time quotes (price, change%, order book)
- Quantitative data (technical indicators, capital flow)
- Financial statements (revenue, net profit, ratios)
- Relationship data between entities (companies, shareholders, executives)

## Query Limits

| Constraint | Limit |
|-----------|-------|
| Entities per query | 5 max |
| Indicators per query | 3 max |
| Overflow handling | Truncates to limits, notes in description file |

## Query Examples

| Type | Example |
|------|---------|
| Basic indicators | 贵州茅台最近一年的营业收入和净利润 |
| Real-time quotes | 英伟达现在的最新价和涨跌幅 |
| Multi-entity comparison | 对比创业板指、沪深300、中证500春节以来的涨幅 |

## Quick Start

```bash
python3 scripts/get_data.py --query “贵州茅台近期走势如何”
```

Output:
```
xlsx: workspace/MX_FinData/MX_FinData_9535fe18.xlsx
描述: workspace/MX_FinData/MX_FinData_9535fe18_description.txt
行数: 42
```

## Output Files

| File | Description |
| --- | --- |
| `MX_FinData_<id>.xlsx` | Structured data table with requested entities and indicators |
| `MX_FinData_<id>_description.txt` | Query logic, field meanings, and truncation notices |

## Notes

- This skill handles structured data queries only, not subjective analysis or investment advice
- Queries must contain explicit financial entity names
- The skill identifies query objects, indicators, and time ranges from natural language input


