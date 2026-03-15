"""
宏观数据查询：请求接口获取 JSON，转换为 CSV 并生成描述文件。

可通过文本输入查询宏观数据；结果保存为 CSV 与说明 txt。
"""

import asyncio
import csv
import json
import os
import re
import time
import uuid
from pathlib import Path
from typing import Any, Dict, List, Tuple
import httpx

# █████████████████████████████████████████████████████████████████████████
# ██                                                                  ██
# ██   ███████╗███╗   ███╗     █████╗ ██████╗ ██╗    ██╗ ██████╗       ██
# ██   ██╔════╝████╗ ████║    ██╔══██╗██╔══██╗██║    ██║██╔═══██╗      ██
# ██   █████╗  ██╔████╔██║    ███████║██████╔╝██║ █╗ ██║██║   ██║      ██
# ██   ██╔══╝  ██║╚██╔╝██║    ██╔══██║██╔═══╝ ██║███╗██║██║   ██║      ██
# ██   ███████╗██║ ╚═╝ ██║    ██║  ██║██║     ╚███╔███╔╝╚██████╔╝      ██
# ██   ╚══════╝╚═╝     ╚═╝    ╚═╝  ╚═╝╚═╝      ╚══╝╚══╝  ╚═════╝       ██
# ██                                                                  ██
# ██                ⚠️  EM API KEY CONFIGURATION REQUIRED ⚠️           ██
# ██                                                                  ██
# ██          YOU MUST REPLACE THE EM API KEY BELOW BEFORE RUNNING    ██
# ██                                                                  ██
# ██                  EM_API_KEY = "YOUR_EM_API_KEY"                  ██
# ██                                                                  ██
# ██        Get your EM API KEY and paste it in the variable above.   ██
# ██                                                                  ██
# █████████████████████████████████████████████████████████████████████████


EM_API_KEY = ""

if not EM_API_KEY:
    raise RuntimeError(
        """

╔══════════════════════════════════════════════════════════════╗
║                      EM API KEY REQUIRED                     ║
╠══════════════════════════════════════════════════════════════╣
║  You must configure your EM API KEY before running.          ║
║                                                              ║
║  Please edit this file and replace:                          ║
║                                                              ║
║      EM_API_KEY = "YOUR_EM_API_KEY"                          ║
║                                                              ║
║  Then run the program again.                                 ║
╚══════════════════════════════════════════════════════════════╝

"""
    )


DEFAULT_MACRO_DATA_OUTPUT_DIR = Path("workspace")/"MX_MacroData"
# MCP 服务器地址
DEFAULT_URL = "https://ai-saas.eastmoney.com"
DEFAULT_PAHT = "/proxy/b/mcp/tool/searchMacroData"

def _safe_filename(s: str, max_len: int = 80) -> str:
    """将查询文本转为安全文件名片段。"""
    s = re.sub(r'[<>:"/\\|?*]', "_", s)
    s = s.strip().replace(" ", "_")[:max_len]
    return s or "query"


def _flatten_value(v: Any) -> str:
    """将单元格值转为字符串（嵌套结构转为 JSON 字符串）。"""
    if v is None:
        return ""
    if isinstance(v, (dict, list)):
        return json.dumps(v, ensure_ascii=False)
    return str(v)


def _extract_frequency(entity_name: str) -> str:
    """
    从entityName中提取频率信息。
    例如："GDP（年）" -> "年", "宏观数据（周）" -> "周"
    """
    match = re.search(r'[（(]([^）)]+)[）)]', entity_name)
    if match:
        return match.group(1)
    return "unknown"


def _parse_macro_table(data_item: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], str]:
    """
    解析宏观数据的table格式。

    根据实际返回数据格式：
    {
        "table": {
            "EMM00000015": ["140.2万亿", "134.8万亿", ...],
            "headName": ["数据来源", "2025", "2024", ...]
        },
        "nameMap": {
            "EMM00000015": "中国:GDP:现价(元)",
            "headNameSub": "数据来源"
        },
        "entityName": "GDP（年）"
    }

    Returns:
        (rows, frequency) - 解析出的数据行和频率
    """
    rows = []

    table = data_item.get("table", {})
    name_map = data_item.get("nameMap", {})
    entity_name = data_item.get("entityName", "")
    description = data_item.get("description", "")

    # 提取频率
    frequency = _extract_frequency(entity_name)

    if not table or not isinstance(table, dict):
        return rows, frequency

    # 获取列名（headName）
    headers = table.get("headName", [])
    if not headers:
        # 尝试其他可能的列名键
        headers = table.get("date", [])
        if not headers:
            return rows, frequency

    # 找出所有的指标键（排除headName、date等元数据键）
    exclude_keys = {"headName", "headNameSub", "date"}
    metric_keys = [k for k in table.keys() if k not in exclude_keys]

    if not metric_keys:
        return rows, frequency

    # 对于每个指标键，创建一行数据
    for metric_key in metric_keys:
        values = table.get(metric_key, [])
        if not values:
            continue

        # 获取指标名称（从nameMap或使用键名）
        metric_name = name_map.get(metric_key, metric_key)

        # 创建一行数据
        row = {
            "entity_name": entity_name,
            "entity_description": description,
            "indicator_code": metric_key,
            "indicator_name": metric_name,
            "frequency": frequency,  # 添加频率字段
        }

        # 将每个年份/字段的值添加到行中
        for i, header in enumerate(headers):
            if i < len(values):
                value = values[i]
                # 如果值是列表，可能需要特殊处理
                if isinstance(value, list):
                    value = ", ".join(str(v) for v in value) if value else ""
                row[header] = value

        rows.append(row)

    return rows, frequency


def _build_headers() -> dict[str, str]:
    """构建请求头（与新的curl命令中的headers一致）。"""
    headers = {
        "em_api_key":  EM_API_KEY,
        "Content-Type": "application/json",
    }
    return headers

def _build_request_body(query: str) -> dict[str, Any]:
    """构建 POST 请求体（与新的searchMacroData接口一致）。"""
    call_id = f"call_{uuid.uuid4().hex[:8]}"
    user_id = f"user_{uuid.uuid4().hex[:8]}"

    body = {
        "query": query,
        "toolContext": {
            "callId": call_id,
            "userInfo": {
                "userId": user_id
            }
        }
    }
    return body

def _write_csv_file(rows: List[Dict[str, Any]], frequency: str, query: str, output_dir: Path) -> Tuple[Path, int]:
    """
    将指定频率的数据写入CSV文件。

    Returns:
        (csv_path, row_count)
    """
    if not rows:
        return None, 0

    # 列名：统一取所有出现过的键
    fieldnames_set: dict[str, None] = {}
    for row in rows:
        for k in row:
            fieldnames_set[k] = None

    # 调整列的顺序，让重要字段靠前
    priority_fields = ["entity_name", "indicator_name", "indicator_code", "frequency", "数据来源"]
    fieldnames = []
    for field in priority_fields:
        if field in fieldnames_set:
            fieldnames.append(field)
            del fieldnames_set[field]

    # 将日期列排序
    date_fields = []
    other_fields = []
    for field in fieldnames_set.keys():
        # 判断是否是日期格式（年或年月日）
        if (field.isdigit() and len(field) == 4) or \
           (re.match(r'^\d{4}-\d{2}-\d{2}$', field)):  # 匹配 YYYY-MM-DD 格式
            date_fields.append(field)
        else:
            other_fields.append(field)

    # 日期降序排列（最新的在前）
    date_fields.sort(reverse=True)
    other_fields.sort()

    fieldnames.extend(other_fields)
    fieldnames.extend(date_fields)

    # 生成文件名
    safe_query = _safe_filename(query)
    safe_frequency = _safe_filename(frequency)
    csv_path = output_dir / f"macro_data_{safe_query}_{safe_frequency}.csv"

    # 写CSV
    with open(csv_path, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        for row in rows:
            writer.writerow({k: _flatten_value(v) for k, v in row.items()})

    return csv_path, len(rows)


# async def query_macro_data(
async def query_macro_data(
    query: str,
    output_dir: Path | None = None,
    api_base: str | None = None,
) -> dict[str, Any]:
    """
    通过文本查询宏观数据，将返回的JSON转为CSV并生成描述txt。

    Args:
        query: 自然语言查询，如「中国GDP」
        output_dir: 保存CSV和txt的目录；默认当前目录

    Returns:
        包含 csv_paths, description_path, row_counts, error(若有) 的字典
    """
    output_dir = output_dir or Path.cwd()
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    result: dict[str, Any] = {
        "csv_paths": [],  # 改为列表，支持多个CSV文件
        "description_path": None,
        "row_counts": {},  # 频率 -> 行数
        "query": query,
    }

    try:
        headers = _build_headers()
        body = _build_request_body(query)
        api_base = DEFAULT_URL.rstrip("/")
        url = f"{api_base}{DEFAULT_PAHT}"
        print(f"发送请求到: {url}")
        print(f"请求体: {json.dumps(body, ensure_ascii=False)}")

        async with httpx.AsyncClient(timeout=30.0) as client:
            resp = await client.post(
                url,
                json=body,
                headers=headers,
            )
            resp.raise_for_status()
            data = resp.json().get("data")

        # 保存原始返回数据用于调试
        debug_file = output_dir / f"macro_data_{_safe_filename(query)}_raw.json"
        with open(debug_file, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print(f"原始数据已保存到: {debug_file}")

    except httpx.HTTPStatusError as e:
        result["error"] = f"HTTP错误: {e.response.status_code} - {e.response.text[:200]}"
        return result
    except Exception as e:
        result["error"] = f"请求失败: {e!s}"
        return result

    # 解析返回的数据结构 - 按频率分组
    frequency_groups: Dict[str, List[Dict[str, Any]]] = {}
    description_parts = []

    # 检查可能的返回路径
    if isinstance(data, dict):
        # 情况1：直接包含result字段（文本结果）
        if "result" in data:
            description_parts.append(f"文本结果: {data['result']}")

        # 情况2：包含dataList字段（主要数据结构）
        data_list = data.get("dataTables", [])
        if data_list and isinstance(data_list, list):
            for item_list in data_list:

                # 解析table数据，同时获取频率
                rows, frequency = _parse_macro_table(item_list)

                if rows:
                    if frequency not in frequency_groups:
                        frequency_groups[frequency] = []
                    frequency_groups[frequency].extend(rows)

                    # 收集描述信息
                    entity_name = item_list.get("entityName", "")
                    description = item_list.get("description", "")
                    title = item_list.get("title", "")

                    if entity_name:
                        description_parts.append(f"实体名称 [{frequency}]: {entity_name}")
                    if description:
                        description_parts.append(f"描述 [{frequency}]: {description}")
                    if title:
                        description_parts.append(f"标题 [{frequency}]: {title}")

                    # 添加字段信息
                    field_set = item_list.get("fieldSet", [])
                    if field_set and isinstance(field_set, list) and len(field_set) > 0:
                        field = field_set[0]
                        data_source = field.get("dataSource", "")
                        unit_name = field.get("unitName", "")
                        if data_source:
                            description_parts.append(f"数据来源 [{frequency}]: {data_source}")
                        if unit_name:
                            description_parts.append(f"单位 [{frequency}]: {unit_name}")

        # 情况3：rawDataTables
        if not frequency_groups:
            raw_data_list = data.get("rawDataTables", [])
            if raw_data_list and isinstance(raw_data_list, list):
                for item_list in raw_data_list:
                    if not isinstance(item_list, list):
                        continue

                    for data_item in item_list:
                        if not isinstance(data_item, dict):
                            continue

                        rows, frequency = _parse_macro_table(data_item)
                        if rows:
                            if frequency not in frequency_groups:
                                frequency_groups[frequency] = []
                            frequency_groups[frequency].extend(rows)

    if not frequency_groups:
        result["error"] = "无法解析表格数据"
        result["raw_preview"] = json.dumps(data, ensure_ascii=False)[:500]
        return result

    # 按频率分别写入CSV文件
    csv_paths = []
    row_counts = {}

    for frequency, rows in frequency_groups.items():
        print(f"处理频率 [{frequency}] 的数据: {len(rows)} 行")
        csv_path, row_count = _write_csv_file(rows, frequency, query, output_dir)
        if csv_path:
            csv_paths.append(str(csv_path))
            row_counts[frequency] = row_count
            print(f"  已保存到: {csv_path}")

    # 写描述txt
    desc_path = output_dir / f"macro_data_{_safe_filename(query)}_description.txt"

    description_lines = [
        "宏观数据查询结果说明",
        "=" * 40,
        f"查询内容: {query}",
        f"数据频率组数: {len(frequency_groups)}",
        "",
        "各频率数据统计:",
    ]

    for frequency, count in row_counts.items():
        description_lines.append(f"  - {frequency}: {count} 行")

    description_lines.extend([
        "",
        "生成的文件:",
    ])

    for csv_path in csv_paths:
        description_lines.append(f"  - {Path(csv_path).name}")

    description_lines.extend([
        "",
        "详细说明:",
    ])

    if description_parts:
        # 去重
        unique_parts = []
        seen = set()
        for part in description_parts:
            if part not in seen:
                seen.add(part)
                unique_parts.append(part)
        description_lines.extend(unique_parts)
    else:
        description_lines.append("（无额外说明）")

    # 添加原始返回的结构信息
    description_lines.extend([
        "",
        "数据源信息:",
        f"接口: {url}",
        f"查询时间: {time.strftime('%Y-%m-%d %H:%M:%S')}",
    ])

    desc_path.write_text("\n".join(description_lines), encoding="utf-8")
    print(f"描述文件已保存到: {desc_path}")

    result["csv_paths"] = csv_paths
    result["description_path"] = str(desc_path)
    result["row_counts"] = row_counts
    return result


def run_cli() -> None:
    """命令行入口：从参数或stdin读取查询文本，执行并打印结果路径。"""
    import sys
    if len(sys.argv) > 1:
        query = " ".join(sys.argv[1:])
    else:
        query = (sys.stdin.read() or "").strip()

    if not query:
        print("用法: python -m scripts.get_data <查询文本>")
        print("  或: echo 查询文本 | python -m scripts.get_data")
        sys.exit(1)

    async def _main() -> None:
        out_dir = Path(DEFAULT_MACRO_DATA_OUTPUT_DIR)
        r = await query_macro_data(query, output_dir=out_dir)
        if "error" in r:
            print(f"错误: {r['error']}", file=sys.stderr)
            if "raw_preview" in r:
                print(f"原始数据预览: {r['raw_preview']}", file=sys.stderr)
            sys.exit(2)
        print(f"CSV: {r['csv_paths']}")
        print(f"描述: {r['description_path']}")
        print(f"行数: {r['row_counts']}")

    asyncio.run(_main())


if __name__ == "__main__":
    run_cli()
