#!/usr/bin/env python3
"""Fetch Feishu/Lark document as Markdown via Feishu Open API.

Special thanks to joeseesun for the excellent qiaomu-markdown-proxy project,
which inspired the Feishu API integration and document parsing approach here.
https://github.com/joeseesun/qiaomu-markdown-proxy

Requirements:
    pip install requests

Setup:
    export FEISHU_APP_ID=your_app_id
    export FEISHU_APP_SECRET=your_app_secret
    App needs: docx:document:readonly, wiki:wiki:readonly

Usage:
    python3 fetch_feishu.py <feishu_url>
    python3 fetch_feishu.py <feishu_url> --json
"""

import sys
import json
import os
import re
import urllib.parse

try:
    import requests
except ImportError:
    print("Error: requests not installed. Run: pip install requests", file=sys.stderr)
    sys.exit(1)

API = "https://open.feishu.cn/open-apis"


def yaml_string(value):
    return json.dumps("" if value is None else str(value), ensure_ascii=False)


def get_token():
    app_id = os.environ.get("FEISHU_APP_ID")
    app_secret = os.environ.get("FEISHU_APP_SECRET")
    if not app_id or not app_secret:
        return None, "FEISHU_APP_ID or FEISHU_APP_SECRET not set"
    resp = requests.post(f"{API}/auth/v3/tenant_access_token/internal",
                         json={"app_id": app_id, "app_secret": app_secret})
    d = resp.json()
    if d.get("code") != 0:
        return None, f"Auth failed: {d.get('msg', resp.text)}"
    return d["tenant_access_token"], None


def parse_url(url):
    patterns = [
        (r"feishu\.cn/docx/([A-Za-z0-9]+)", "docx"),
        (r"feishu\.cn/docs/([A-Za-z0-9]+)", "legacy_doc"),
        (r"feishu\.cn/wiki/([A-Za-z0-9]+)", "wiki"),
        (r"larksuite\.com/docx/([A-Za-z0-9]+)", "docx"),
        (r"larksuite\.com/docs/([A-Za-z0-9]+)", "legacy_doc"),
        (r"larksuite\.com/wiki/([A-Za-z0-9]+)", "wiki"),
    ]
    for pattern, doc_type in patterns:
        m = re.search(pattern, url)
        if m:
            return m.group(1), doc_type
    return url, "docx"


def resolve_wiki(token, wiki_token):
    resp = requests.get(f"{API}/wiki/v2/spaces/get_node",
                        headers={"Authorization": f"Bearer {token}"},
                        params={"token": wiki_token})
    d = resp.json()
    if d.get("code") == 0:
        node = d["data"]["node"]
        return node.get("obj_token"), node.get("obj_type")
    return None, None


def get_blocks(token, doc_id):
    blocks, page_token = [], None
    while True:
        params = {"page_size": 500}
        if page_token:
            params["page_token"] = page_token
        resp = requests.get(f"{API}/docx/v1/documents/{doc_id}/blocks",
                            headers={"Authorization": f"Bearer {token}"},
                            params=params)
        d = resp.json()
        if d.get("code") != 0:
            return None, f"Blocks fetch failed: {d.get('msg', resp.text)}"
        blocks.extend(d["data"].get("items", []))
        if not d["data"].get("has_more"):
            break
        page_token = d["data"].get("page_token")
    return blocks, None


def extract_text(elements):
    if not elements:
        return ""
    parts = []
    for el in elements:
        if "text_run" in el:
            tr = el["text_run"]
            text = tr.get("content", "")
            s = tr.get("text_element_style", {})
            if s.get("bold"):        text = f"**{text}**"
            if s.get("italic"):      text = f"*{text}*"
            if s.get("inline_code"): text = f"`{text}`"
            if s.get("link", {}).get("url"):
                text = f"[{text}]({urllib.parse.unquote(s['link']['url'])})"
            parts.append(text)
        elif "mention_user" in el:
            parts.append(f"@{el['mention_user'].get('user_id', 'user')}")
        elif "equation" in el:
            parts.append(f"${el['equation'].get('content', '')}$")
    return "".join(parts)


LANG_MAP = {
    7: "bash", 8: "c", 9: "csharp", 10: "cpp", 14: "css", 19: "dockerfile",
    25: "go", 29: "html", 31: "java", 32: "javascript", 33: "json",
    35: "kotlin", 40: "markdown", 46: "php", 50: "python", 52: "ruby",
    53: "rust", 58: "sql", 59: "swift", 62: "typescript", 68: "xml", 69: "yaml",
}


def blocks_to_md(blocks):
    lines = []
    counters = {}
    for block in blocks:
        bt = block.get("block_type")
        pid = block.get("parent_id", "")

        if bt == 2:
            text = extract_text(block.get("text", {}).get("elements", []))
            lines.append(text if text.strip() else "")
        elif bt in range(3, 10):
            level = bt - 2
            key = f"heading{level}"
            data = block.get(key) or block.get("heading", {})
            text = extract_text(data.get("elements", []))
            lines.append(f"{'#' * level} {text}")
        elif bt == 10:
            text = extract_text(block.get("bullet", {}).get("elements", []))
            lines.append(f"- {text}")
        elif bt == 11:
            text = extract_text(block.get("ordered", {}).get("elements", []))
            n = counters.get(pid, 0) + 1
            counters[pid] = n
            lines.append(f"{n}. {text}")
        elif bt == 12:
            code_data = block.get("code", {})
            text = extract_text(code_data.get("elements", []))
            lang = LANG_MAP.get(code_data.get("style", {}).get("language", 0), "")
            lines.extend([f"```{lang}", text, "```"])
        elif bt == 13:
            text = extract_text(block.get("quote", {}).get("elements", []))
            lines.append(f"> {text}")
        elif bt == 15:
            todo_data = block.get("todo", {})
            text = extract_text(todo_data.get("elements", []))
            done = todo_data.get("style", {}).get("done", False)
            lines.append(f"- {'[x]' if done else '[ ]'} {text}")
        elif bt == 16:
            lines.append("---")
        elif bt == 17:
            tok = block.get("image", {}).get("token", "")
            lines.append(f"![image](feishu-image://{tok})")
        elif bt == 1:
            pass
        else:
            for key, val in block.items():
                if isinstance(val, dict) and "elements" in val:
                    text = extract_text(val["elements"])
                    if text.strip():
                        lines.append(text)
                    break

    return "\n\n".join(lines)


def fetch_feishu(url):
    doc_id, doc_type = parse_url(url)

    if doc_type == "legacy_doc":
        return {
            "error": (
                "Legacy Feishu /docs/ pages are not supported by this script. "
                "Convert the document to docx first, or use a public-page fallback if the page is accessible without the API."
            )
        }

    token, err = get_token()
    if err:
        return {"error": err}

    if doc_type == "wiki":
        real_id, real_type = resolve_wiki(token, doc_id)
        if not real_id:
            return {"error": f"Cannot resolve wiki node: {doc_id}"}
        doc_id, doc_type = real_id, real_type or "docx"

    info_resp = requests.get(f"{API}/docx/v1/documents/{doc_id}",
                             headers={"Authorization": f"Bearer {token}"})
    doc_info = info_resp.json().get("data", {}).get("document", {})
    title = doc_info.get("title", "")

    blocks, err = get_blocks(token, doc_id)
    if err:
        return {"error": err}

    return {"title": title, "document_id": doc_id, "url": url, "content": blocks_to_md(blocks)}


def to_markdown(r):
    if "error" in r:
        return f"Error: {r['error']}"
    parts = [
        "---",
        f"title: {yaml_string(r.get('title', ''))}",
        f"document_id: {yaml_string(r.get('document_id', ''))}",
        f"url: {yaml_string(r.get('url', ''))}",
        "---",
        "",
        f"# {r['title']}" if r.get("title") else "",
        "",
        r.get("content", ""),
    ]
    return "\n".join(parts)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: fetch_feishu.py <feishu_url> [--json]", file=sys.stderr)
        print("  Requires: FEISHU_APP_ID, FEISHU_APP_SECRET", file=sys.stderr)
        sys.exit(1)

    result = fetch_feishu(sys.argv[1])
    if "--json" in sys.argv:
        print(json.dumps(result, ensure_ascii=False, indent=2))
    else:
        print(to_markdown(result))
    if "error" in result:
        sys.exit(1)
