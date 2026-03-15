---
name: MX_MacroData
description: 通过自然语言查询宏观经济数据，结果转为 CSV 并生成描述文件。支持查询 GDP、CPI、货币供应量等宏观指标。
---

# 宏观经济数据查询 (MX_MacroData)

通过**文本输入**查询宏观经济数据，接口返回 JSON 后会自动转换为 **CSV** 并生成对应的**内容描述 txt** 文件。

## ⚠️ 核心输入约束 (Critical Input Constraints)

**本工具为严格的数据执行器，严禁接收包含模糊语义、相对指代或需先验知识推断的查询请求。** 
所有输入的实体、时间、地域及指标必须为**绝对明确**的值。任何模糊表述必须在调用本工具前由上层模型完成“语义解包”。

### 1. 禁止模糊地域与集合 (No Ambiguous Regions)
- **禁止输入**：地域集合简称（如“华东五市”、“东北三省”、“长三角”）、政治经济联盟（如“金砖国家”、“一带一路沿线”）、泛指区域（如“西方国家”）。
- **要求**：必须解包为具体的**省/市/国家全称列表**。
  - ❌ 错误：`"查询华东五市GDP"`
  - ✅ 正确：`"查询上海市、南京市、杭州市、合肥市、福州市的GDP"`

### 2. 禁止模糊商品类别 (No Ambiguous Commodities)
- **禁止输入**：大类统称（如“稀土金属”、“有色金属”、“农产品”、“能源”、“科技股”）。
- **要求**：必须解包为具体的**交易品种名称或代码**。
  - ❌ 错误：`"查询稀土价格走势"`
  - ✅ 正确：`"查询氧化镨钕、氧化镝、氧化铽的价格走势"`

### 3. 禁止相对排名与动态集合 (No Relative Rankings)
- **禁止输入**：依赖实时排名的指代（如“GDP Top 5 国家”、“通胀最高的三个国家”、“主要产油国”）。
- **要求**：必须由上层模型先通过搜索确定具体名单，再传入**明确的实体名称列表**。
  - ❌ 错误：`"查询全球GDP前5国家的黄金储备"`
  - ✅ 正确：`"查询美国、中国、德国、日本、印度的黄金储备"` (基于最新数据解包后的名单)

### 4. 禁止相对时间与未定义事件 (No Relative Time/Events)
- **禁止输入**：相对时间段（如“过去三年”、“去年”、“今年一季度”）、未定义时间边界的事件（如“两次金融危机期间”、“上次加息周期”、“疫情期间”）。
- **要求**：必须转换为绝对的 **`YYYY-MM-DD`** 或 **`YYYY-Qx`** 时间区间。
  - ❌ 错误：`"查询两次金融危机期间的美股走势"`
  - ✅ 正确：`"查询美股在 2007-12至2009-06 以及 2020-02至2020-04 期间的走势"`

### 5. 禁止宏观泛指 (No Macro Generalizations)
- **禁止输入**：宽泛的经济概念（如“中国经济”、“美国制造业”、“全球通胀”）。
- **要求**：必须指定具体的**指标名称**（如 GDP、CPI、PMI、失业率等）。
  - ❌ 错误：`"查询中国经济数据"`
  - ✅ 正确：`"查询中国 GDP 同比增速、中国 CPI 同比"`

---

## 功能范围

### 基础查询能力
- **经济指标**：GDP、CPI、PPI、PMI、失业率、工业增加值等（需指定具体国家/地区及指标名）。
- **货币金融**：M1/M2 货币供应量、社融规模、国债利率、汇率（需指定具体币种对）。
- **商品价格**：黄金、白银、原油、铜、特定稀土氧化物等（需指定具体品种）。
- **多维频率**：自动识别并按年、季、月、周、日频率分组输出（需配合明确的时间区间）。

### 查询示例

| 类型     | ❌ 禁止的模糊查询                     | ✅ 允许的明确查询                                     |
|----------|--------------------------------------|------------------------------------------------------|
| 国内经济 | 查询华东地区GDP                      | 查询上海市、江苏省、浙江省、安徽省、福建省的GDP数据       |
| 货币供应 | 查询主要新兴市场货币供应             | 查询中国、印度、巴西的M2货币供应量                      |
| 商品价格 | 查询稀土和有色金属价格               | 查询氧化镨钕、铜、铝的现货价格走势                      |
| 全球宏观 | 查询Top 3经济体非农数据              | 查询美国、中国、德国的非农就业数据 |
| 历史事件 | 查询08年危机期间股市                 | 查询标普500指数在 2007-10-01 至 2009-03-31 期间的走势    |

---

## 错误处理机制

若接收到违反上述约束的输入，工具将直接返回错误信息，**不执行任何数据检索**，并提示用户或上层模型进行修正：
- `Error: Ambiguous Region Detected. Please provide specific city/country names instead of "[Input]".`
- `Error: Ambiguous Commodity Category. Please specify exact commodity names instead of "[Input]".`
- `Error: Relative Time/Ranking Detected. Please resolve to specific dates or entity lists before calling.`
- 

### 配置 API Key（必填）
需用户自行从官网获取API Key进行配置

### 1. 命令行脚本

在项目根目录或配置的工作目录下执行：

```bash
python -m scripts.get_data  "中国GDP"
```
通过管道传入查询文本：

```bash
echo "白银价格" | python -m scripts.get_data
```
### 2. 代码调用

```python
import asyncio
from pathlib import Path
from scripts.get_data import query_macro_data

async def main():
    result = await query_macro_data(
        query="中国近五年GDP",
        output_dir=Path("workspace/macro_data"),
    )
    if "error" in result:
        print(result["error"])
    else:
        print(f"CSV: {r['csv_paths']}")
        print(f"描述: {r['description_path']}")
        print(f"行数: {r['row_counts']}")

asyncio.run(main())
```

输出示例：
```
CSV: /path/to/workspace/macro_data/macro_data_中国GDP_年.csv
CSV: /path/to/workspace/macro_data/macro_data_中国GDP_季.csv
描述:/path/to/workspace/macro_data/macro_data_中国GDP_description.txt
行数: 年: 10行, 季: 40行
```

## 输出文件说明

| 文件 | 说明 |
|------|------|
| `macro_data_<查询摘要>_<频率>.csv` | 按频率分组的宏观数据表，UTF-8 编码，可直接用 Excel 或 pandas 打开。 |
| `macro_data_<查询摘要>_description.txt` | 说明文件，含各频率数据统计、数据来源和单位等信息。 |


