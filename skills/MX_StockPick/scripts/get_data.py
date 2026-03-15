"""
选股/选板块/选基金：通过 MCP 股票基金筛选获取数据，转为中文列名的 CSV 与描述文件。

支持 A股、港股、美股选股，以及选板块、选基金、选ETF、选可转债；
返回英文列名替换为中文后输出完整数据的 CSV 及数据说明。

需要配置EM_API_KEY。
"""

import asyncio
import csv
import json
import os
import re
import uuid
from pathlib import Path
from typing import Any
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


TOOL_NAME = "股票基金筛选"
DEFAULT_OUTPUT_DIR = Path("workspace") / "MX_StockPick"
# MCP 服务器地址
MCP_URL = "https://ai-saas.eastmoney.com/proxy/b/mcp/tool/selectSecurity"


def get_metadata(
        query: str = "",
        selectType: str = "",
) -> dict:
    """
    生成 MCP 调用所需的 meta_data 字典

    Args:
        query: 查询文本

    Returns:
        包含完整 meta 信息的字典
    """

    return {
        "query": query,
        "selectType": selectType,
        "toolContext": {
            "callId": str(uuid.uuid4()),
            "userInfo": {
                "userId": EM_API_KEY,
            },
        },
    }


def _safe_filename(s: str, max_len: int = 80) -> str:
    """将查询文本转为安全文件名片段。"""
    s = re.sub(r'[<>:"/\\|?*]', "_", s)
    s = s.strip().replace(" ", "_")[:max_len]
    return s or "query"


def _build_column_map(columns: list[dict[str, Any]]) -> dict[str, str]:
    """
    从 MCP 返回的 columns 构建 英文列名 -> 中文列名 的映射。
    支持常见字段名：field/name/key -> displayName/title/label。
    """
    name_map: dict[str, str] = {}
    for col in columns or []:
        if not isinstance(col, dict):
            continue
        en_key = col.get("field", "") or col.get("name", "") or col.get("key", "")
        cn_name = col.get("displayName", "") or col.get("title", "") or col.get("label", "")
        cn_name = cn_name + ' ' + col.get('dateMsg') if col.get('dateMsg') else cn_name
        if en_key is not None and cn_name is not None:
            name_map[str(en_key)] = str(cn_name)
    return name_map


def _columns_order(columns: list[dict[str, Any]]) -> list[str]:
    """按 columns 顺序返回英文列名列表，用于 CSV 表头顺序。"""
    order: list[str] = []
    for col in columns or []:
        if not isinstance(col, dict):
            continue
        en_key = col.get("field") or col.get("name") or col.get("key")
        if en_key is not None:
            order.append(str(en_key))
    return order


def _parse_partial_results_table(partial_results: str) -> list[dict[str, str]]:
    """
    将 partialResults 的 Markdown 表格字符串解析为行字典列表。
    格式: "|序号|代码|名称|...|\\n|---|\\n|1|000001|平安银行|...|"
    """
    if not partial_results or not isinstance(partial_results, str):
        return []
    lines = [ln.strip() for ln in partial_results.strip().splitlines() if ln.strip()]
    if not lines:
        return []

    # 表头行：按 | 分割，去掉首尾空
    def split_cells(line: str) -> list[str]:
        return [c.strip() for c in line.split("|") if c.strip() != ""]

    header_cells = split_cells(lines[0])
    if not header_cells:
        return []
    # 跳过分隔行（如 |---|---|）
    data_start = 1
    if data_start < len(lines) and re.match(r"^[\s\|\-]+$", lines[data_start]):
        data_start = 2
    rows: list[dict[str, str]] = []
    for i in range(data_start, len(lines)):
        cells = split_cells(lines[i])
        if len(cells) != len(header_cells):
            # 列数不一致时按长度对齐，缺的补空
            if len(cells) < len(header_cells):
                cells.extend([""] * (len(header_cells) - len(cells)))
            else:
                cells = cells[: len(header_cells)]
        rows.append(dict(zip(header_cells, cells)))
    return rows


def _datalist_to_rows(
        datalist: list[dict[str, Any]],
        column_map: dict[str, str],
        column_order: list[str],
) -> list[dict[str, str]]:
    """
    将 datalist 中每行的英文键按 column_map 替换为中文键，保证顺序与 partialResults 风格一致。
    覆盖全部 datalist 数据。
    """
    if not datalist:
        return []

    # 表头顺序：优先按 columns 顺序，未在 columns 中的键按首次出现顺序排在后面
    first = datalist[0]
    extra_keys = [k for k in first if k not in column_order]
    header_order = column_order + extra_keys

    rows: list[dict[str, str]] = []
    for row in datalist:
        if not isinstance(row, dict):
            continue
        cn_row: dict[str, str] = {}
        for en_key in header_order:
            if en_key not in row:
                continue
            cn_name = column_map.get(en_key, en_key)
            val = row[en_key]
            if val is None:
                cn_row[cn_name] = ""
            elif isinstance(val, (dict, list)):
                cn_row[cn_name] = json.dumps(val, ensure_ascii=False)
            else:
                cn_row[cn_name] = str(val)
        rows.append(cn_row)

    return rows


async def query_MX_StockPick(
        query: str,
        selectType: str,
        output_dir: Path | None = None
) -> dict[str, Any]:
    """
    通过自然语言查询进行选股（A股/港股/美股）、选板块、选基金；
    使用 MCP 股票基金筛选工具，将返回的 datalist 按 columns 转为中文列名 CSV 并生成描述文件。

    Args:
        query: 自然语言查询，如「股价大于1000元的股票」「港股科技龙头」「新能源板块」「白酒主题基金」
        selectType: 选股指定标的类型，格式：A股、港股、美股、基金、ETF、可转债、板块
        output_dir: 保存 CSV 和描述文件的目录；默认 workspace/MX‑StockPick

    Returns:
        包含 csv_path, description_path, row_count, query，selectType；若失败则含 error。
    """
    output_dir = output_dir or Path(os.environ.get("MX‑StockPick_OUTPUT_DIR", str(DEFAULT_OUTPUT_DIR)))
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    result: dict[str, Any] = {
        "csv_path": None,
        "description_path": None,
        "row_count": 0,
        "query": query,
        "selectType": selectType
    }

    try:
        raw = await mcp_single_call_v2(
            TOOL_NAME,
            {"query": query, "selectType": selectType},
        )
    except Exception as e:
        result["error"] = f"MCP 调用失败: {e!s}"
        return result

    if not raw or not isinstance(raw, dict):
        result["error"] = "MCP 返回为空或非 JSON 对象"
        result["raw_preview"] = str(raw)[:500] if raw else ""
        return result

    # 使用 datalist（全量），兼容不同命名；若无则回退到解析 partialResults 表格字符串
    dataList = raw.get("allResults", {}).get("result", {}).get("dataList", [])
    if not isinstance(dataList, list):
        dataList = []

    columns = raw.get("allResults", {}).get("result", {}).get("columns", [])
    if not isinstance(columns, list):
        columns = []

    rows: list[dict[str, str]] = []
    data_source = ""

    if dataList:
        column_map = _build_column_map(columns)
        column_order = _columns_order(columns)
        rows = _datalist_to_rows(dataList, column_map, column_order)
        data_source = "dataList"

    if not rows:
        partial_results = raw.get("partialResults")
        if partial_results:
            rows = _parse_partial_results_table(partial_results)
            data_source = "partialResults"

    if not rows:
        result["error"] = "返回中无有效 datalist 且 partialResults 无法解析或为空"
        result["raw_preview"] = json.dumps(raw, ensure_ascii=False)[:500]
        return result

    fieldnames = list(rows[0].keys())
    safe_name = _safe_filename(selectType+'_'+query)
    csv_path = output_dir / f"MX_StockPick_{safe_name}.csv"
    desc_path = output_dir / f"MX_StockPick_{safe_name}_description.txt"

    with open(csv_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames, extrasaction="ignore")
        writer.writeheader()
        for row in rows:
            writer.writerow(row)

    description_lines = [
        "选股/选板块/选基金 结果说明",
        "=" * 40,
        f"查询内容: {query}",
        f"数据行数: {len(rows)}（来源: {data_source}）",
        f"列名（中文）: {', '.join(fieldnames)}",
        "",
        "说明: 数据来源于 MCP 股票基金筛选；"
        + ("列名已按 columns 映射为中文。" if data_source == "dataList" else "表格来自 partialResults。"),
    ]
    desc_path.write_text("\n".join(description_lines), encoding="utf-8")

    result["csv_path"] = str(csv_path)
    result["description_path"] = str(desc_path)
    result["row_count"] = len(rows)
    return result


def run_cli() -> None:
    import argparse
    import sys
    """命令行入口：从参数或 stdin 读取查询文本，执行并打印结果路径。"""

    parser = argparse.ArgumentParser(description='通过自然语言查询进行选股、选板块、选基金')

    # 添加位置参数
    parser.add_argument('--query', type=str, help='自然语言查询，如「股价大于1000元的股票」')

    # 添加可选参数
    parser.add_argument('--select-type', dest='selectType',
                        choices=['A股', '港股', '美股', '基金', 'ETF', '可转债', '板块'],
                        help='选股指定标的类型')
    args = parser.parse_args()

    print(f"选股问句: {args.query}，选股类型: {args.selectType}")

    if not args.query:
        print("用法: python -m scripts.get_data --query \"查询文本\" --select-type \"查询领域\"")
        print("示例: 股价大于1000元的股票 / 港股科技龙头 / 新能源板块 / 白酒主题基金")
        sys.exit(1)

    async def _main() -> None:
        out_dir = Path(os.environ.get("MX_StockPick_OUTPUT_DIR", str(DEFAULT_OUTPUT_DIR)))
        r = await query_MX_StockPick(args.query, output_dir=out_dir, selectType=args.selectType)
        if "error" in r:
            print(f"错误: {r['error']}", file=sys.stderr)
            if "raw_preview" in r:
                print(r["raw_preview"], file=sys.stderr)
            sys.exit(2)
        print(f"CSV: {r['csv_path']}")
        print(f"描述: {r['description_path']}")
        print(f"行数: {r['row_count']}")

    asyncio.run(_main())


# MCP 调用函数
async def mcp_single_call_v2(tool_name, arguments):
    """MCP 异步调用函数"""

    query = arguments.get("query", "")
    selectType = arguments.get("selectType", "")
    meta = get_metadata(query=query, selectType=selectType)
    result_data = {}

    try:
        async with httpx.AsyncClient(timeout=30.0) as client:
            result = await client.post(
                MCP_URL,
                json=meta,
                headers={
                    "Content-Type": "application/json",
                    "em_api_key": EM_API_KEY,
                },
            )

            result_data = result.json()['data']
            if result_data:
                print("调用成功！")
                return result_data
            else:
                print("------------  返回结果格式未解析成功  ------------")
                return result_data

    except Exception as e:
        print(f"调用工具时出错: {e}")
        import traceback
        traceback.print_exc()
        return result_data


if __name__ == "__main__":
    run_cli()
