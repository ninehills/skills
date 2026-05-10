# Lightpanda Skill for LLM Agents

A skill that provides integration with [Lightpanda](https://github.com/lightpanda-io/browser), a fast headless browser optimized for data extraction and web automation.

## What is this?

This is a generalist skill for LLM agents (Claude Code, Openclaw, and others) that teaches agents how to use Lightpanda as a drop-in replacement for Chrome/Chromium when performing web scraping and automation tasks.

## Features

- Faster and lighter than Chrome for headless operations (9x faster, 16x less memory)
- Native MCP server with agent-optimized tools
- CLI fetch command with wait strategies and multiple output formats
- CDP (Chrome DevTools Protocol) compatible with custom LP domain extensions
- Works with Playwright and Puppeteer
- JavaScript execution support
- Semantic tree and markdown extraction

## Installation

### Skill.sh

```bash
npx skills add https://github.com/lightpanda-io/agent-skill --skill Lightpanda
```

### Claude Code Plugin

Add the marketplace and install the plugin:

```bash
/plugin marketplace add lightpanda-io/agent-skill
/plugin install lightpanda@lightpanda-io-agent-skill
```

Then install the Lightpanda binary:

```bash
bash scripts/install.sh
```

### Manual Setup

Copy `SKILL.md` to your agent's skills directory, then run the install script:

```bash
bash scripts/install.sh
```

Then add the MCP server to Claude Code:

```bash
claude mcp add lightpanda -- $HOME/.local/bin/lightpanda mcp
```

## Three Ways to Use Lightpanda

| Interface | Best for | Command |
|-----------|----------|---------|
| **MCP server** | Agent workflows, interactive browsing | `lightpanda mcp` |
| **CLI fetch** | Quick one-off page extraction | `lightpanda fetch --dump markdown URL` |
| **CDP server** | Custom Playwright/Puppeteer automation | `lightpanda serve --port 9222` |

## Platform Support

- Linux (x86_64, aarch64)
- macOS (x86_64, arm64)
- Windows via WSL2

## Usage

See [SKILL.md](SKILL.md) for detailed usage instructions, including:

- MCP server setup and available tools
- CLI fetch options and output formats
- CDP server with Playwright or Puppeteer
- Custom LP CDP domain methods
- Important notes and limitations

## License

Apache 2.0
