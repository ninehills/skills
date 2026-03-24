---
name: mx-stock-pick
description: "Use when the user wants to screen stocks, pick sectors, or filter funds. Supports natural language queries to select A-shares, HK stocks, US stocks, funds, ETFs, convertible bonds, and sectors by financial indicators, technical signals, or business characteristics. Outputs CSV with Chinese column names and a description file. Requires EM_API_KEY."
---

# 选股 / 选板块 / 选基金

## Workflow

1. **Parse query**: Identify the user's screening criteria and determine the `--select-type` (A股/港股/美股/基金/ETF/可转债/板块)
2. **Execute**: Run `python -m scripts.get_data --query "<criteria>" --select-type "<type>"`
3. **Verify output**: Confirm CSV and description files exist in the output directory
4. **Report results**: Summarize the screening results (row count, key columns) to the user

## Query Examples

| Type | Query | select-type |
|------|-------|-------------|
| A-shares | 股价大于1000元的股票 | A股 |
| HK stocks | 港股的科技龙头 | 港股 |
| US stocks | 纳斯达克市值前30 | 美股 |
| Sectors | 今天涨幅最大板块 | 板块 |
| Funds | 白酒主题基金 | 基金 |
| ETFs | 规模超2亿的电力ETF | ETF |
| Conv. bonds | 价格低于110元的可转债 | 可转债 |

## Prerequisites

1. Obtain `EM_API_KEY` from the 东方财富 website
2. Set the key in `scripts/get_data.py`

## 快速开始

### 1. 命令行调用

```bash
python -m scripts.get_data --query 股价大于100元的股票；涨跌幅；所属板块 --select-type A股
```

**输出示例**
```
CSV: /path/to/workspace/MX_StockPick/MX_StockPick_A股_股价大于100元的股票.csv
描述: /path/to/workspace/MX_StockPick/MX_StockPick_A股_股价大于100元的股票_description.txt
行数: 42
```

**参数说明：**

| 参数 | 说明 | 必填 |
|------|------|------|
| `--query` | 自然语言查询条件 | ✅ |
| `--select-type` | 查询领域 | ✅ |

### 2. 代码调用

```python
import asyncio
from pathlib import Path
from scripts.get_data import query_select_stock

async def main():
    result = await query_select_stock(
        query="A股半导体板块市值前20",
        selectType="A股",
        output_dir=Path("workspace/MX_StockPick"),
    )
    if "error" in result:
        print(result["error"])
    else:
        print(result["csv_path"], result["row_count"])

asyncio.run(main())
```

## 输出文件说明

| 文件 | 说明 |
|------|------|
| `MX_StockPick_<查询摘要>.csv` | 全量数据表，列名为**中文**（由返回的 columns 映射），UTF-8 编码，可用 Excel 或 pandas 打开 |
| `MX_StockPick_<查询摘要>_description.txt` | 数据说明：查询内容、行数、列名说明等 |

## 环境变量（可选）

| 变量                        | 说明 | 默认 |
|---------------------------|------|------|
| `MX_StockPick_OUTPUT_DIR` | CSV 与描述文件的输出目录 | `workspace/MX_StockPick` |

## 常见问题

**错误：请设置 EM_API_KEY 环境变量**
→ 将东方财富官网提供的`EM_API_KEY`填入`scripts/get_data.py`中的`EM_API_KEY`变量中


**如何指定输出目录？**
```bash
export MX_StockPick_OUTPUT_DIR="/path/to/output"
python -m scripts.get_data --query "查询内容" --select-type "查询领域"
```
