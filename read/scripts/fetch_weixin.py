#!/usr/bin/env python3
"""Fetch WeChat public account article as Markdown using Playwright + BeautifulSoup.

Special thanks to joeseesun for the excellent qiaomu-markdown-proxy project,
which inspired the Playwright-based WeChat scraping approach in this script.
https://github.com/joeseesun/qiaomu-markdown-proxy

Requirements:
    pip install playwright beautifulsoup4 lxml
    playwright install chromium

Usage:
    python3 fetch_weixin.py <url>
    python3 fetch_weixin.py <url> --json
"""

import sys
import json
import asyncio


def yaml_string(value: str) -> str:
    return json.dumps("" if value is None else str(value), ensure_ascii=False)


async def fetch(url: str) -> dict:
    try:
        from playwright.async_api import async_playwright
        from bs4 import BeautifulSoup
    except ImportError as e:
        return {"error": str(e) + "\nRun: pip install playwright beautifulsoup4 lxml && playwright install chromium"}

    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        page = await browser.new_page(
            user_agent="Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        )
        try:
            await page.goto(url, wait_until="domcontentloaded", timeout=30000)
            await page.wait_for_selector("#js_content", timeout=15000)
            html = await page.content()
        except Exception as e:
            await browser.close()
            return {"error": f"Page load failed: {e}"}
        await browser.close()

    soup = BeautifulSoup(html, "lxml")

    title = (soup.select_one("#activity-name") or soup.new_tag("x")).get_text(strip=True)
    author = (soup.select_one("#js_author_name") or soup.new_tag("x")).get_text(strip=True)
    date = (soup.select_one("#publish_time") or soup.new_tag("x")).get_text(strip=True)

    content_el = soup.select_one("#js_content")
    if not content_el:
        return {"error": "Could not find #js_content"}

    for tag in content_el.find_all(["script", "style"]):
        tag.decompose()

    for img in content_el.find_all("img"):
        src = img.get("data-src") or img.get("src") or ""
        img.replace_with(f"\n![image]({src})\n" if src else "")

    lines = []
    for el in content_el.find_all(["p", "h1", "h2", "h3", "h4", "section", "blockquote"]):
        text = el.get_text(strip=True)
        if not text:
            continue
        if el.name in ("h1", "h2", "h3", "h4"):
            lines.append(f"{'#' * int(el.name[1])} {text}")
        elif el.name == "blockquote":
            lines.append(f"> {text}")
        else:
            lines.append(text)

    content = "\n\n".join(lines) or content_el.get_text("\n", strip=True)
    return {"title": title, "author": author, "date": date, "url": url, "content": content}


def to_markdown(r: dict) -> str:
    if "error" in r:
        return f"Error: {r['error']}"
    parts = [
        "---",
        f"title: {yaml_string(r.get('title', ''))}",
        *([f"author: {yaml_string(r['author'])}"] if r.get("author") else []),
        *([f"date: {yaml_string(r['date'])}"] if r.get("date") else []),
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
        print("Usage: fetch_weixin.py <url> [--json]", file=sys.stderr)
        sys.exit(1)

    result = asyncio.run(fetch(sys.argv[1]))
    if "--json" in sys.argv:
        print(json.dumps(result, ensure_ascii=False, indent=2))
    else:
        print(to_markdown(result))
