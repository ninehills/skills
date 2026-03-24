---
name: mx-fin-search
description: "当用户要求查找股票新闻、获取券商研报、检索公司公告、分析市场动态或查询金融资讯时使用。通过自然语言检索时效性金融信息（新闻、公告、研报），提取正文并可保存为本地文本文件。需配置 EM_API_KEY。"
metadata:
  openclaw:
    emoji: "📰"
---

# 金融资讯搜索

通过**自然语言查询**检索时效性金融信息，适用场景包括：
- **最新新闻与政策动态**
- **公司公告与事件跟踪**
- **券商研报与市场解读**
- **宏观事件对市场/板块影响分析**

## 查询示例

| 类型 | query 示例 |
|---|---|
| 个股资讯 | 格力电器最新研报与公告、寒武纪 688256 最新动态 |
| 板块/主题 | 商业航天板块近期新闻、新能源政策解读 |
| 宏观/风险 | 美联储加息对A股影响、汇率风险相关公司案例 |
| 综合解读 | 今日大盘异动原因、北向资金流向解读 |

## 前提条件

### 配置 API Key（必填）
需用户自行从官网获取API Key进行配置

## 快速开始

### 1. 命令行调用

```bash
python -m scripts.get_data "寒武纪 688256 最新研报与公告"
```

或通过标准输入：

```bash
echo "A股 汇率风险 自然对冲 公司" | python -m scripts.get_data
```

**输出示例**
```text
Saved: /path/to/.openclaw/workspace/financial_search/financial_search_A股_汇率风险_自然对冲_公司.txt
（随后输出资讯正文内容）
```

**参数说明：**

| 参数 | 说明 | 必填 |
|---|---|---|
| `query`（位置参数） | 自然语言查询文本 | ✅（位置参数或 stdin 二选一） |
| `--no-save` | 仅输出结果，不写入本地文件 | 否 |

### 2. 代码调用

```python
import asyncio
from pathlib import Path
from scripts.get_data import query_financial_news

async def main():
    result = await query_financial_news(
        query="新能源板块近期政策与龙头公司动态",
        output_dir=Path(".openclaw/workspace/financial_search"),
        save_to_file=True,
    )
    if "error" in result:
        print(result["error"])
    else:
        print(result["content"])
        if result.get("output_path"):
            print("已保存至:", result["output_path"])

asyncio.run(main())
```

## 输出文件说明

| 文件 | 说明 |
|---|---|
| `financial_search_<查询摘要>.txt` | 资讯正文文本（从返回中提取） |

## 返回字段说明

- `content`：提取后的资讯正文（优先 `llmSearchResponse`）。
- `output_path`：当 `save_to_file=True` 且有内容时，返回保存路径。
- `raw`：原始接口返回，便于调试或二次处理。
- `error`：检索失败时返回错误信息。

## 环境变量

| 变量 | 说明 | 默认 |
|---|---|---|
| `EM_API_KEY` | 新闻检索接口鉴权 key（必填） | 空 |

## 错误处理

| 错误 | 处理方式 |
|---|---|
| `EM_API_KEY is required` | 提示用户配置 EM_API_KEY 环境变量 |
| API 返回空结果 | 简化查询关键词后重试一次；仍无结果则告知用户 |
| 网络超时 | 等待 5 秒后重试一次 |
| `result["error"]` 非空 | 向用户展示错误信息，不编造结果 |

**如何只看输出，不落盘？**
```bash
python -m scripts.get_data "商业航天板块近期新闻" --no-save
```

## 合规说明
- 禁止在代码或提示词中硬编码账号 ID、会话 ID 或 token。
- 环境变量按敏感信息处理，不在日志或回复中泄露。
- 检索失败时不得编造事实，应返回明确错误或不确定性说明。
- 输出应保持可追溯、可审计。

