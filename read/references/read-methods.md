# Read Methods Reference

## Proxy Cascade

Try in order. Success = non-empty output with readable content. If a proxy returns empty, an error page, or fewer than 5 lines, treat it as failed and try the next:

### 1. defuddle.md

```bash
curl -sL "https://defuddle.md/{url}"
```

Cleaner output with YAML frontmatter. Try this first.

### 2. r.jina.ai

```bash
curl -sL "https://r.jina.ai/{url}"
```

Wide coverage, preserves image links. Use if defuddle.md returns empty or errors.

### 3. Web search plugin reader (if available)

If a web search plugin is installed (e.g., PipeLLM), the cascade tries its reader tool before local fallback. Handles JavaScript-rendered pages better than free proxies.

### 4. Local tools

```bash
npx agent-fetch "{url}" --json
# or
defuddle parse "{url}" -m -j
```

Last resort if both proxies fail. These commands may return JSON, so extract the Markdown-bearing field before returning or saving the result. Raw JSON is not a valid final output for `/read`.

## GitHub URLs

GitHub file URLs (`github.com/user/repo/blob/...`) render heavy HTML. The proxy cascade often returns partial or nav-heavy content. Prefer:

```bash
# Raw file content (fastest)
curl -sL "https://raw.githubusercontent.com/{user}/{repo}/{branch}/{path}"

# Via gh CLI (works with private repos)
gh api repos/{user}/{repo}/contents/{path} --jq '.content' | base64 -d
```

Use the proxy cascade only as a fallback for GitHub pages that are not raw file views (e.g., issue threads, README renders).

## PDF to Markdown

### Remote PDF URL

r.jina.ai handles PDF URLs directly:

```bash
curl -sL "https://r.jina.ai/{pdf_url}"
```

If that fails, download and extract locally:

```bash
curl -sL "{pdf_url}" -o /tmp/input.pdf
pdftotext -layout /tmp/input.pdf -
```

### Local PDF file

```bash
# Best quality (requires: pip install marker-pdf)
marker_single /path/to/file.pdf --output_dir ~/Downloads/

# Fast, text-heavy PDFs (requires: brew install poppler)
pdftotext -layout /path/to/file.pdf - | sed 's/\f/\n---\n/g'

# No-dependency fallback
python3 -c "
import pypdf, sys
r = pypdf.PdfReader(sys.argv[1])
print('\n\n'.join(p.extract_text() for p in r.pages))
" /path/to/file.pdf
```

Use `marker` when layout matters (papers, tables). Use `pdftotext` for speed.

## Feishu / Lark Document

Built-in script at `${CLAUDE_SKILL_DIR:-~/.agents/skills/read}/scripts/fetch_feishu.py`. Requires `requests` and Feishu app credentials:

```bash
pip install requests  # one-time setup
export FEISHU_APP_ID=your_app_id
export FEISHU_APP_SECRET=your_app_secret
python3 "${CLAUDE_SKILL_DIR:-$HOME/.agents/skills/read}/scripts/fetch_feishu.py" "{url}"
```

Supports: docx and wiki pages. Legacy `/docs/` pages are not supported by this script; convert them to docx first, or use a public-page fallback if the document is accessible without the API. App needs `docx:document:readonly` and `wiki:wiki:readonly` permissions.
Output: YAML frontmatter (title, document_id, url) + Markdown body.

## WeChat Public Account

Use the proxy cascade (r.jina.ai / defuddle.md). Works for most articles without any extra tools.

If the proxy is blocked, use the built-in Playwright script as a last resort (requires ~300 MB one-time install):

```bash
pip install playwright beautifulsoup4 lxml && playwright install chromium
python3 "${CLAUDE_SKILL_DIR:-$HOME/.agents/skills/read}/scripts/fetch_weixin.py" "{url}"
```
