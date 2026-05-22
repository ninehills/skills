#!/usr/bin/env python3
"""Local URL → Markdown extractor. Privacy-preserving tier 1 for fetch.sh.

Two paths, picked at runtime:

1. **Best path**: `readability-lxml` + `html2text` installed. Extract main
   content via readability scoring, convert to clean Markdown. Quality close
   to defuddle.md / r.jina.ai for static pages.

2. **Fallback path**: stdlib only. Strip HTML tags, collapse whitespace, drop
   <script>/<style>/<nav>/<footer>. Quality is poor for JS-heavy pages and
   complex layouts, but works for simple article-style HTML without any
   third-party dependency.

JS-rendered pages (X/Twitter, paywalled news, SPA) are out of reach for both
paths. Use `fetch.sh --use-proxy` for those.

Exit codes:
  0  success, markdown on stdout
  1  fetch or extraction failed; reason on stderr
  2  invocation error (missing URL)
"""

from __future__ import annotations

import argparse
import re
import sys
import urllib.error
import urllib.request
from html.parser import HTMLParser


USER_AGENT = (
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 12_0) "
    "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36"
)
FETCH_TIMEOUT_SECS = 20


def fetch_html(url: str) -> str:
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(req, timeout=FETCH_TIMEOUT_SECS) as resp:
        raw = resp.read()
    # Detect charset from Content-Type header; fall back to utf-8 with replace.
    charset = "utf-8"
    ctype = resp.headers.get("Content-Type", "")
    m = re.search(r"charset=([\w\-]+)", ctype, re.IGNORECASE)
    if m:
        charset = m.group(1).lower()
    try:
        return raw.decode(charset, errors="replace")
    except LookupError:
        return raw.decode("utf-8", errors="replace")


def extract_with_readability(html: str, url: str) -> str | None:
    """Best path: readability-lxml + html2text. Returns Markdown or None if
    deps missing. Raises only on genuine extraction failure."""
    try:
        from readability import Document  # type: ignore
        import html2text  # type: ignore
    except ImportError:
        return None
    doc = Document(html)
    cleaned_html = doc.summary(html_partial=True)
    title = (doc.short_title() or "").strip()
    converter = html2text.HTML2Text()
    converter.body_width = 0
    converter.unicode_snob = True
    converter.ignore_links = False
    converter.ignore_images = False
    body = converter.handle(cleaned_html).strip()
    if not body:
        return None
    header = ""
    if title:
        header = f"# {title}\n\n"
    return f"{header}> Source: {url}\n\n{body}\n"


class _StdlibStripper(HTMLParser):
    """Fallback HTML → text converter using only stdlib. Quality is poor but
    deterministic; intended as a last resort when readability isn't installed.
    Drops common non-content blocks (script/style/nav/footer/aside)."""

    DROP_TAGS = {"script", "style", "nav", "footer", "aside", "noscript", "form"}
    BLOCK_TAGS = {
        "p", "div", "section", "article", "li", "ul", "ol",
        "h1", "h2", "h3", "h4", "h5", "h6", "br", "tr",
    }

    def __init__(self) -> None:
        super().__init__()
        self._buf: list[str] = []
        self._drop_depth = 0
        self._title = ""
        self._in_title = False

    def handle_starttag(self, tag, attrs):
        if tag in self.DROP_TAGS:
            self._drop_depth += 1
        elif tag == "title":
            self._in_title = True
        elif tag in self.BLOCK_TAGS:
            self._buf.append("\n")

    def handle_endtag(self, tag):
        if tag in self.DROP_TAGS:
            self._drop_depth = max(0, self._drop_depth - 1)
        elif tag == "title":
            self._in_title = False
        elif tag in self.BLOCK_TAGS:
            self._buf.append("\n")

    def handle_data(self, data):
        if self._drop_depth:
            return
        if self._in_title:
            self._title += data
            return
        self._buf.append(data)

    def text(self) -> str:
        raw = "".join(self._buf)
        lines = [line.strip() for line in raw.splitlines()]
        # Drop empty lines but keep paragraph breaks.
        out: list[str] = []
        prev_blank = False
        for line in lines:
            if not line:
                if not prev_blank:
                    out.append("")
                prev_blank = True
            else:
                out.append(line)
                prev_blank = False
        return "\n".join(out).strip()


def extract_with_stdlib(html: str, url: str) -> str:
    p = _StdlibStripper()
    p.feed(html)
    body = p.text()
    if not body:
        body = "(no text content extracted)"
    header = ""
    title = (p._title or "").strip()
    if title:
        header = f"# {title}\n\n"
    return f"{header}> Source: {url}\n\n{body}\n"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("url", help="URL to fetch")
    parser.add_argument(
        "--prefer",
        choices=("auto", "readability", "stdlib"),
        default="auto",
        help="Force a specific extractor (default: auto = readability if installed, else stdlib)",
    )
    args = parser.parse_args()

    try:
        html = fetch_html(args.url)
    except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError, OSError) as exc:
        print(
            f"[fetch] tier=local status=fail reason=\"fetch failed: {exc}\"",
            file=sys.stderr,
        )
        return 1

    used = ""
    body: str | None = None

    if args.prefer in ("auto", "readability"):
        body = extract_with_readability(html, args.url)
        if body is not None:
            used = "readability"
        elif args.prefer == "readability":
            print(
                "[fetch] tier=local status=fail "
                "reason=\"--prefer readability but readability-lxml or html2text not installed; "
                "install with: pip install --user readability-lxml html2text\"",
                file=sys.stderr,
            )
            return 1

    if body is None:
        body = extract_with_stdlib(html, args.url)
        used = "stdlib"

    # Sanity floor: if even the stdlib extractor returns essentially nothing,
    # treat as failure so the proxy fallback (if --use-proxy) gets a chance.
    body_lines = [l for l in body.splitlines() if l.strip()]
    if len(body_lines) < 4:
        print(
            f"[fetch] tier=local status=fail reason=\"extractor={used} produced <4 non-empty lines\"",
            file=sys.stderr,
        )
        return 1

    if used == "stdlib":
        print(
            "[fetch] tier=local status=ok extractor=stdlib "
            "hint=\"install readability-lxml + html2text for cleaner output\"",
            file=sys.stderr,
        )
    else:
        print(f"[fetch] tier=local status=ok extractor={used}", file=sys.stderr)

    sys.stdout.write(body)
    return 0


if __name__ == "__main__":
    sys.exit(main())
