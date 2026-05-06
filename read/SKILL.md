---
name: read
description: "Fetches any URL or PDF as clean Markdown for reading, quoting, citation, or downstream work. Handles paywalls, JS-heavy pages, X/Twitter, and Chinese platforms via proxy cascade. Not for local text files already in the repo."
when_to_use: "any URL or PDF to fetch, 看这个链接, 读一下, 看看这个网页, 抓取网页, read this, check this URL, fetch this page"
metadata:
  version: "3.14.0"
---

# Read: Fetch Any URL or PDF as Markdown

Prefix your first line with 🥷 inline, not as its own paragraph.

Convert any URL or local PDF to clean Markdown. No analysis, no summary, no discussion of the content unless explicitly asked after the fetch.

## Routing

| Input | Method |
|-------|--------|
| `feishu.cn`, `larksuite.com` | Feishu API script |
| `mp.weixin.qq.com` | Proxy cascade first, built-in WeChat article script only if the proxies fail |
| `.pdf` URL or local PDF path | PDF extraction |
| GitHub URLs (`github.com`, `raw.githubusercontent.com`) | Prefer raw content or `gh` first. Use the proxy cascade only as fallback. |
| `x.com`, `twitter.com` | Proxy cascade (r.jina.ai keeps image URLs). Do not try WebFetch; it 402s. |
| Everything else | Proxy cascade |

After routing, load `references/read-methods.md` and run the commands for the chosen method.

## Output Format

```
Title:  {title}
Author: {author} (if available)
Source: {platform}
URL:    {original url}

Content
{full Markdown, truncated at 200 lines if long}
```

## Saving

**Default: display only.** Show the converted Markdown inline. Do not create a file.

**Save to `~/Downloads/{title}.md`** with YAML frontmatter when any of these are true:
- User explicitly asks: "save", "download", "保存", "下载", "keep this"
- Called from within `/learn` (Phase 1 expects a file to move)
- User says "save" or "保存" after seeing the output (use conversation content, do not re-fetch)

When saving:
- If the file already exists, append `-1`, `-2`, etc. Never overwrite without confirmation.
- Tell the user the saved path.

When not saving:
- Do not mention that a file was not saved. Just show the content.

## Images

By default only save Markdown. Download images only when the user explicitly asks: "download images", "save images", "带图", "下载图片", or similar.

When asked, after saving the Markdown:

1. Extract image URLs: `grep -oE 'https?://[^ )"]+\.(jpg|jpeg|png|webp|gif)' {md_path} | sort -u`
2. Create `~/Downloads/{title}-images/` and curl each URL in parallel (`&` + `wait`). Use the same proxy env vars as the fetch step.
3. Report the count and folder path. If any download fails, list the failed URLs.

## Hard Rules

- **Do not summarize or analyze the content.** Your job is conversion and storage, not interpretation.
- **Never overwrite without confirmation.** If the target filename already exists, use an auto-incremented suffix.
- **Stop after the save report.** Do not suggest follow-up actions ("Would you like me to summarize?", "Next, you could...") unless the user asks.

## Gotchas

| What happened | Rule |
|---------------|------|
| Fetched a paywalled article and returned a login page as Markdown | Inspect the first 10 lines for paywall signals ("Subscribe", "Sign in", "Continue reading"). If found, stop and warn the user. Do not save the login page. |
| User said "read this" but meant "summarize and act on it" | Deliver the Markdown first, then ask what to do next. Do not save unless asked. |
| URL returned empty page or paywall with no content | Report the failure clearly: what was tried, what failed. Do not fabricate or guess the content. |
| r.jina.ai or defuddle.md returned empty for a JS-heavy site | Try the local fallback (`agent-fetch` or `defuddle parse`) before giving up. |
| Network failures | Prepend local proxy env vars if available and retry once. |
| Long content | Preview with `head -n 200` first; mention truncation when reporting the save. |
| Local fallback tools returned JSON | Extract the Markdown-bearing field. Raw JSON is not a valid final output for `/read`. |
| All methods failed | Stop and tell the user what was tried and what failed. Suggest opening the URL in a browser or providing an alternative. Do not silently return empty or partial results. |

## Content Extraction for Restyling

Activate when: "extract content", "reformat this document", or user hands over a document to restyle

Extract and tag:
- **Headings**: H1/H2/H3 hierarchy
- **Body paragraphs**: Plain text, no styling
- **Lists**: Bullet vs numbered, nesting level
- **Metrics/data**: Numbers, dates, quantifiable claims
- **Images/diagrams**: Descriptions, captions

Output: Clean, tagged content ready to feed into kami or other typesetting tools.
