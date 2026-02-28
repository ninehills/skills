---
name: web-search
description: Search the web for information using DuckDuckGo or other search engines via curl. Use when users ask questions requiring up-to-date information, research, or fact-checking.
allowed-tools:
  - Bash
  - WebSearch
  - WebFetch
---

# Web Search Skill

Search the web. **Prefer the built-in WebSearch tool** — it uses Electron's **built-in BrowserWindow** to perform searches, so it can handle JavaScript-rendered search results and bypass basic anti-bot measures. This is NOT a simple HTTP request — it actually opens the page in a real browser window, performs the search, and extracts results from the rendered page. Fall back to curl only if WebSearch is unavailable.

## DuckDuckGo Instant Answer API (No API key needed)

```bash
# Basic search — returns instant answers
curl -s "https://api.duckduckgo.com/?q=QUERY&format=json&no_html=1" | jq '.Abstract, .RelatedTopics[:5]'
```

## DuckDuckGo HTML Search (Full results)

```bash
# Get search results page and extract links
curl -s "https://html.duckduckgo.com/html/?q=QUERY" | grep -o 'href="https\{0,1\}://[^"]*' | head -10
```

## Google Search via SerpAPI (if API key available)

```bash
# Check if SERPAPI_KEY is set
alma config get serpapi.apiKey

# If available:
curl -s "https://serpapi.com/search.json?q=QUERY&api_key=API_KEY" | jq '.organic_results[:5] | .[] | {title, link, snippet}'
```

## Fetching Page Content

After finding URLs from search, use the **WebFetch** tool to get the actual page content:

```
WebFetch(url="https://example.com/article")
```

Or via curl:
```bash
curl -sL "https://example.com/article" | head -200
```

## Tips

- URL-encode the query: replace spaces with `+` or `%20`
- Use `jq` to parse JSON responses
- For complex queries, try multiple search approaches
- Always summarize findings for the user rather than dumping raw results
- If DuckDuckGo doesn't have good results, try fetching specific known sources directly
- For freshness queries (`today`, `latest`, `今天`, `最新`), use the **current year** from runtime context instead of hardcoding a fixed year

## Examples

**"Latest AI news":**
```bash
curl -s "https://html.duckduckgo.com/html/?q=latest+AI+news+<current_year>" | grep -o 'href="https\{0,1\}://[^"]*' | grep -v duckduckgo | head -5
```

**"Python 3.13 new features":**
```bash
curl -s "https://api.duckduckgo.com/?q=python+3.13+new+features&format=json&no_html=1" | jq '.Abstract'
```
