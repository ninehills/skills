---
name: web-fetch
description: "Use when the user shares a URL, link, or website, or asks to fetch a webpage, scrape content, read an article, call an API endpoint, or download online resources. Retrieves and extracts text from web pages, JSON APIs, and HTTP endpoints."
allowed-tools: "Bash, WebFetch"
---

# Web Fetch Skill

Fetch web content using the best available method, in priority order:

## 1. WebFetch Tool (Primary — always try this first)

Always try WebFetch first. It renders pages with full JavaScript execution, handles SPAs, anti-bot protections, and returns clean markdown.

```
WebFetch(url="https://example.com/article", prompt="Extract the main content")
```

- Handles JS-rendered pages (React, Vue, etc.)
- Shares session cookies (can access logged-in content)
- Auto-extracts article content via Readability

Only fall back to options below if WebFetch fails.

## 2. Browser Skill (Fallback for interactive pages)

If WebFetch fails (e.g., page requires login interaction, CAPTCHA, or multi-step navigation), use the `alma browser` skill:

```bash
# Open URL in a real Chrome browser tab
alma browser open "https://example.com"

# Read the page content as markdown
alma browser read <tabId>

# For pages that need interaction first
alma browser click <tabId> "button.load-more"
alma browser read <tabId>
```

Use the browser skill when you need:
- To interact with the page (click, scroll, fill forms) before reading
- Access to the user's authenticated Chrome sessions
- To handle CAPTCHAs or complex anti-bot pages

## 3. curl (Last resort)

Only use curl when both WebFetch and browser skill are unavailable or for simple API/JSON endpoints:

```bash
# JSON API (curl is fine for APIs)
curl -s "https://api.example.com/data" | jq '.'

# API with auth headers
curl -s -H "Authorization: Bearer TOKEN" -H "Accept: application/json" "URL"

# Download a file
curl -sL -o /tmp/file.pdf "URL"

# Check HTTP status
curl -sL -o /dev/null -w "%{http_code}" "URL"
```

**Do NOT use curl for regular web pages** — it cannot execute JavaScript and gets blocked by most modern sites. Use WebFetch instead.

## Tips

- For JSON APIs, curl is acceptable since no JS rendering is needed.
- Pipe curl output to `head -N` to limit output size.
- Use `jq` for JSON responses.
