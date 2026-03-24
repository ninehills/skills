---
name: bird
description: “Use when the user wants to interact with X/Twitter — read tweets, search posts, check timelines, view bookmarks/likes, browse news/trending, or look up followers/following. Fast X CLI via GraphQL with cookie auth. Recommended for reading; writing triggers blocks quickly.”
allowed-tools: “Bash, Read, Write”
---

# bird — Fast X/Twitter CLI

Read tweets, search posts, browse timelines, bookmarks, likes, news/trending, and user data via X/Twitter GraphQL (cookie auth).

**Warning:** Writing (tweet/reply) triggers anti-bot blocks quickly. Use bird primarily for reading.

## Typical Workflow

1. **Verify auth**: `bird whoami`
2. **Search or read**: `bird search “query”` or `bird read <tweet-url>`
3. **Explore results**: `bird thread <id>`, `bird replies <id>`, `bird user-tweets @handle`
4. **Export data**: Add `--json` to any read command for structured output

## Install

```bash
npm install -g @steipete/bird    # or: pnpm/bun add -g @steipete/bird
brew install steipete/tap/bird   # macOS Homebrew alternative
```

## Essential Commands

```bash
bird whoami                                          # Check auth
bird read <tweet-id-or-url> [--json]                 # Read a tweet
bird search “<query>” [-n count] [--json]             # Search tweets
bird thread <tweet-id-or-url> [--json]                # Full conversation thread
bird replies <tweet-id-or-url> [--json]               # Replies to a tweet
bird user-tweets <@handle> [-n count] [--json]        # User’s timeline
bird bookmarks [-n count] [--json]                    # Your bookmarks
bird likes [-n count] [--json]                        # Your likes
bird news [--ai-only] [-n count] [--json]             # Trending/news
bird following [--user <id>] [-n count] [--json]      # Who you follow
bird followers [--user <id>] [-n count] [--json]      # Who follows you
bird home [-n count] [--following] [--json]            # Home timeline
```

## Pagination

Add `--all` or `--max-pages n` for multi-page results. Output with `--json` becomes `{ tweets, nextCursor }`.

## Authentication

Credentials resolve in order: CLI flags (`--auth-token`, `--ct0`) > env vars (`AUTH_TOKEN`, `CT0`) > browser cookies (Safari/Chrome/Firefox via `--cookie-source`).

## Full Command Reference

For complete command options, bookmarks flags, global options, JSON schemas, news tab filters, library usage, config, and query ID details, see the upstream README or run `bird help [command]`.
