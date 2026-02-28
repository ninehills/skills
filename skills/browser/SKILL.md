---
name: browser
description: Control the user's real Chrome browser with existing sessions, cookies, and logins via Chrome Relay. Use for form filling, clicking buttons, navigating multi-step flows, logging in, and any task requiring web interaction.
allowed-tools:
  - Bash
  - Read
---

# Browser Skill

Control the user's real Chrome browser via Chrome Relay CLI commands. This gives you access to their existing sessions, cookies, and logins — no need to log in again.

All browser operations use `alma browser` CLI commands via Bash.

## Available Commands

| Command | Purpose |
|---------|---------|
| `alma browser status` | Check Chrome Relay connection status |
| `alma browser tabs` | List all open tabs — shows `[tabId] title` |
| `alma browser open [url]` | Open a new tab, optionally with a URL |
| `alma browser goto <tabId> <url>` | Navigate an existing tab to a URL |
| `alma browser click <tabId> <selector>` | Click element by CSS selector |
| `alma browser type <tabId> <selector> <text> [--enter]` | Type into an input field, optionally press Enter |
| `alma browser read <tabId>` | Get page content as clean markdown — preferred for reading |
| `alma browser read-dom <tabId>` | List interactive elements with CSS selector hints (JSON) |
| `alma browser screenshot [tabId]` | Take screenshot — prints file path to .png |
| `alma browser eval <tabId> <code>` | Execute JavaScript in page context |
| `alma browser scroll <tabId> <up\|down> [amount]` | Scroll page up or down (default 500px) |
| `alma browser back <tabId>` | Go back in history |
| `alma browser forward <tabId>` | Go forward in history |

## Workflow

1. **Check connection**: `alma browser status` — confirm Chrome Relay is connected
2. **List tabs**: `alma browser tabs` — see what's already open (note the `[tabId]`)
3. **Navigate**: `alma browser goto <tabId> <url>` or `alma browser open <url>` for a new tab
4. **Discover elements**: `alma browser read-dom <tabId>` — find interactive elements with CSS selectors
5. **Read content**: `alma browser read <tabId>` — extract text as markdown
6. **Interact**: `alma browser click`, `alma browser type`, `alma browser eval`
7. **Verify**: `alma browser screenshot <tabId>` then `Read` the .png file to see the result

## When to Use This vs WebFetch/WebSearch

- **Use this skill** when you need to: fill forms, click buttons, navigate multi-step flows, log in, interact with web apps, or read a page the user already has open
- **Use WebFetch** when you just need to read a public page's content (faster, no browser needed)
- **Use WebSearch** when you just need to search the web

## Finding Elements

To click or type, you need CSS selectors:
- Run `alma browser read-dom <tabId>` — returns all interactive elements with CSS selector hints
- Take a screenshot to visually understand the layout
- Common selectors: `input[name="email"]`, `button[type="submit"]`, `a[href*="login"]`, `#search-input`, `.submit-btn`

## Example: Search on a Site

```bash
# 1. Find existing tabs
alma browser tabs

# 2. Open a new tab (note the tab ID in output)
alma browser open https://example.com

# 3. Discover form elements
alma browser read-dom 123

# 4. Type into search and press Enter
alma browser type 123 "input[name='q']" "search query" --enter

# 5. Read the results
alma browser read 123
```

## Example: Multi-Step Form

```bash
# 1. Open the page
alma browser open https://example.com/register

# 2. Discover form fields
alma browser read-dom 123

# 3. Fill in fields
alma browser type 123 "#name" "John Doe"
alma browser type 123 "#email" "john@example.com"

# 4. Submit
alma browser click 123 "button[type='submit']"

# 5. Verify — screenshot then read the image
alma browser screenshot 123
# Use Read tool on the printed file path to see the screenshot
```

## Example: Reading Browser Content

```bash
# List tabs to find the one the user is looking at
alma browser tabs

# Read page content as markdown
alma browser read 456
```

## Tips

- Always run `alma browser tabs` first to find existing tabs — don't create unnecessary new tabs
- For reading text content, prefer `alma browser read` over `alma browser screenshot`
- After `alma browser screenshot`, use the `Read` tool on the returned file path to view the image
- For advanced interactions (dropdowns, scrolling to specific elements), use `alma browser eval`
- Tab IDs are shown in brackets like `[123]` in the tabs listing output
