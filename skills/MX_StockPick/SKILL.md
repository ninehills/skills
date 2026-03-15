---
name: MX_StockPick
description: 通过自然语言查询进行选股、选板块、选基金。类型支持 A股、港股、美股、基金、ETF、可转债、板块。调用 MCP 股票基金筛选工具获取数据，将返回的 datalist 按 columns 映射为中文列名，输出全量 CSV 及数据说明文件。需要配置EM_API_KEY。
---

# 选股 / 选板块 / 选基金

通过**自然语言查询**进行选股，支持以下类型：
- **A股**、**港股**、**美股**
- **基金**、**ETF**、**可转债**、**板块**

## 功能范围

### 基础选股能力
- 按股价、市值、涨跌幅、市盈率等**财务/行情指标**筛选
- 按**技术信号**筛选（如连续上涨、突破均线等）
- 按**主营业务、主要产品**筛选
- 按**行业/概念板块**筛选成分股
- 获取**指数成分股**
- **推荐**股票、基金、板块

### A股进阶查询（部分场景）
除基础选股外，还支持A股上市公司的以下查询场景：
- 高管信息、股东信息
- 龙虎榜数据
- 分红、并购、增发、回购
- 主营区域
- 券商金股

> **注意**：上述仅为部分示例，实际支持的条件远多于列举内容

### **查询示例**

| 类型     | query | select-type |
|----------|----------|----------|
| 选A股   | 股价大于1000元的股票、创业板市盈率最低的50只 | A股 |
| 选港股   | 港股的科技龙头 | 港股 |
| 选美股   | 纳斯达克市值前30、苹果产业链美股 | 美股 |
| 选板块   | 今天涨幅最大板块 | 板块 |
| 选基金   | 白酒主题基金、新能源混合基金近一年收益排名 | 基金 |
| 选ETF   | 规模超2亿的电力ETF | ETF |
| 选可转债 | 价格低于110元、溢价率超5个点的可转债 | 可转债 |

## 前提条件

### 配置 Token

1. 请访问东方财富官方网站获取`EM_API_KEY`。
2. 将`EM_API_KEY`填入`scripts/get_data.py`中的`EM_API_KEY`变量中

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
