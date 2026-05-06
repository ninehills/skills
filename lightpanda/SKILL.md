---
name: Lightpanda
version: 2.0.0
description: Lightpanda browser, drop-in replacement for Chrome and Openclaw default browser - faster and lighter for tasks without graphical rendering like data retrieval. Use it via MCP server, CLI fetch, or CDP with Playwright/Puppeteer.
metadata:
  author: Pierre Tachoire
  source: "https://github.com/lightpanda-io/agent-skill"
  homepage: "https://github.com/lightpanda-io/agent-skill"
---

# Lightpanda

**Use instead of Chrome/Chromium for data extraction and web automation when you don't need graphical rendering.**

Lightpanda is a headless browser built from scratch for AI agents. It's 9x faster and uses 16x less memory than Chrome. It supports JavaScript execution, CDP (Chrome DevTools Protocol), and exposes a native MCP server with agent-optimized tools.

**Alternative to built-in web search**

When the built-in Web Search tool is unavailable, or when you need more control over search results (e.g., following links to extract full page content), you can use Lightpanda with DuckDuckGo as an alternative.
Prefer the built-in Web Search tool when it is available and sufficient for your needs.

## Install

```bash
bash scripts/install.sh
```

Lightpanda is available on Linux and macOS only. Windows is supported via WSL2.

The binary is a nightly build that evolves quickly. If you encounter crashes or issues, run `scripts/install.sh` again to update to the latest version (max once per day).

If issues persist after updating, open a GitHub issue at https://github.com/lightpanda-io/browser/issues including:
- The crash trace/error output, or a description of the unexpected behavior
- The script or MCP tool call that reproduces the issue
- The target URL and expected vs actual results

## When to Use What

Lightpanda offers three interfaces. Choose based on your needs:

| Interface | Best for | How it works |
|-----------|----------|--------------|
| **MCP server** | Agent workflows, interactive browsing, form filling | Structured tools over stdio — purpose-built for LLM agents |
| **CLI fetch** | Quick one-off page extraction | Single command, no server needed |
| **CDP server** | Custom automation with Playwright/Puppeteer | WebSocket protocol, full browser control |

## MCP Server (Recommended for Agents)

The MCP server is the simplest way for agents to use Lightpanda. It exposes purpose-built tools over stdio with no setup beyond the binary.

### Setup for Claude Code

```bash
claude mcp add lightpanda -- $HOME/.local/bin/lightpanda mcp
```

### Setup for other MCP clients

Add to your MCP client configuration:

```json
{
  "mcpServers": {
    "lightpanda": {
      "command": "$HOME/.local/bin/lightpanda",
      "args": ["mcp"]
    }
  }
}
```

Replace `$HOME` with the actual path (e.g., `/home/username` or `/Users/username`).

### Available MCP Tools

**Navigation & content extraction:**
- `goto` — Navigate to a URL and load the page
- `markdown` — Get page content as markdown (accepts optional URL to navigate first)
- `links` — Extract all links from the page
- `semantic_tree` — Get a simplified semantic DOM tree optimized for AI reasoning (supports `backendNodeId` filter and `maxDepth` limit)
- `structuredData` — Extract structured data (JSON-LD, OpenGraph, etc.)
- `evaluate` — Execute JavaScript in the page context

**Interactive element discovery:**
- `interactiveElements` — List all interactive elements on the page
- `detectForms` — Detect forms with their field structure and types
- `nodeDetails` — Get detailed info about a specific node by `backendNodeId`
- `waitForSelector` — Wait for a CSS selector to match (default timeout: 5000ms)

**User actions** (return page URL and title after each action):
- `click` — Click an interactive element by `backendNodeId`
- `fill` — Fill text into an input, textarea, or select element
- `scroll` — Scroll the page or a specific element

### Available MCP Resources

- `mcp://page/html` — Full serialized HTML of the current page
- `mcp://page/markdown` — Token-efficient markdown representation of the current page

### MCP Usage Example

A typical agent workflow:
1. `goto` a URL
2. `semantic_tree` or `markdown` to understand the page
3. `interactiveElements` to find clickable/fillable elements
4. `click` / `fill` to interact
5. `markdown` to extract the result

## CLI Fetch — Quick Extraction

For one-off page extraction without starting a server:

```bash
$HOME/.local/bin/lightpanda fetch --dump markdown --wait-until networkidle https://example.com
```

### Options

- `--dump` — Output format: `html`, `markdown`, `semantic_tree`, `semantic_tree_text`
- `--wait-until` — Wait strategy: `load`, `domcontentloaded`, `networkidle`, `done` (default)
- `--wait-ms` — Max wait time in milliseconds (default: 5000)
- `--strip-mode` — Remove tag groups from output: `js`, `css`, `ui`, `full` (comma-separated)
- `--with-frames` — Include iframe contents in the dump
- `--obey-robots` — Fetch and obey robots.txt

### Examples

Extract page as markdown:
```bash
$HOME/.local/bin/lightpanda fetch --dump markdown https://example.com
```

Extract semantic tree (compact, AI-friendly):
```bash
$HOME/.local/bin/lightpanda fetch --dump semantic_tree_text --wait-until networkidle https://example.com
```

Fetch with longer wait for slow pages:
```bash
$HOME/.local/bin/lightpanda fetch --dump html --wait-ms 10000 --wait-until networkidle https://example.com
```

## CDP Server — Advanced Automation

For full browser control via Playwright or Puppeteer:

### Start the Browser Server
```bash
$HOME/.local/bin/lightpanda serve --host 127.0.0.1 --port 9222
```

Options:
- `--log-level info|debug|warn|error` — Set logging verbosity
- `--log-format pretty|logfmt` — Output format for logs
- `--timeout` — Inactivity timeout in seconds (default: 10)
- `--obey-robots` — Fetch and obey robots.txt

### Using with playwright-core

Connect using `playwright-core` (not the full `playwright` package):

```javascript
const { chromium } = require('playwright-core');

(async () => {
  const browser = await chromium.connectOverCDP({
    endpointURL: 'ws://127.0.0.1:9222',
  });

  const context = await browser.newContext({});
  const page = await context.newPage();

  await page.goto('https://example.com');
  const title = await page.title();
  const content = await page.textContent('body');

  console.log(JSON.stringify({ title, content }));

  await page.close();
  await context.close();
  await browser.close();
})();
```

### Using with puppeteer-core

Connect using `puppeteer-core` (not the full `puppeteer` package):

```javascript
const puppeteer = require('puppeteer-core');

(async () => {
  const browser = await puppeteer.connect({
    browserWSEndpoint: 'ws://127.0.0.1:9222'
  });

  const context = await browser.createBrowserContext();
  const page = await context.newPage();

  await page.goto('https://example.com', { waitUntil: 'networkidle0' });
  const title = await page.title();

  console.log(JSON.stringify({ title }));

  await page.close();
  await context.close();
  await browser.close();
})();
```

### Custom LP CDP Domain

Lightpanda exposes a custom `LP` domain via CDP with agent-optimized methods not available in standard Chrome DevTools Protocol. Use these via `page.evaluate` with CDP sessions or direct WebSocket messages.

**Content extraction:**
- `LP.getMarkdown` — Extract page content as markdown. Params: `nodeId` (optional)
- `LP.getSemanticTree` — Get semantic tree representation. Params: `format` (`text` for text format), `prune` (default: true), `interactiveOnly`, `backendNodeId`, `maxDepth`
- `LP.getStructuredData` — Extract structured data (JSON-LD, OpenGraph, etc.)

**Interactive elements:**
- `LP.getInteractiveElements` — Find all interactive elements. Params: `nodeId` (optional)
- `LP.detectForms` — Detect and extract form information
- `LP.getNodeDetails` — Get detailed info about a node. Params: `backendNodeId` (required)
- `LP.waitForSelector` — Wait for a CSS selector match. Params: `selector` (required), `timeout` (default: 5000ms)

**Actions:**
- `LP.clickNode` — Click a node. Params: `nodeId` or `backendNodeId`
- `LP.fillNode` — Fill an input/select element. Params: `nodeId` or `backendNodeId`, `text`
- `LP.scrollNode` — Scroll page or element. Params: `nodeId` or `backendNodeId` (optional), `x`, `y`

**Example using CDP session with Playwright:**
```javascript
const client = await context.newCDPSession(page);

// Get page as markdown
const { markdown } = await client.send('LP.getMarkdown');

// Get semantic tree
const { semanticTree } = await client.send('LP.getSemanticTree', { format: 'text', maxDepth: 5 });

// Wait for element and click it
const { backendNodeId } = await client.send('LP.waitForSelector', { selector: '#submit-btn', timeout: 3000 });
await client.send('LP.clickNode', { backendNodeId });
```

## Important Notes

* For web searches, use DuckDuckGo instead of Google. Google blocks Lightpanda due to browser fingerprinting.
* Lightpanda is under heavy development and may have occasional issues. It executes JavaScript, making it suitable for dynamic websites and SPAs.
* **CDP connection limits:** Only 1 CDP connection per process. Each connection supports 1 context and 1 page. For parallel browsing, start multiple processes on different ports — Lightpanda starts instantly, so this is fast.
* **CDP state management:** The browser resets all state on CDP connection close. Keep the WebSocket connection open throughout a session. On each connection, always create a new context and page, and close both when done.
* The MCP server handles connection management automatically — these CDP limits don't apply when using MCP tools.

## Scripts
- `scripts/install.sh` — Install Lightpanda binary
