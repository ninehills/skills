#!/usr/bin/env bash
# Fetch a URL as Markdown via proxy cascade.
# Special thanks to joeseesun for the excellent qiaomu-markdown-proxy project,
# which inspired the proxy cascade design and fallback logic in this script.
# https://github.com/joeseesun/qiaomu-markdown-proxy
# Usage: fetch.sh <url> [proxy_url]
# Example: fetch.sh https://example.com http://127.0.0.1:7890
set -euo pipefail

URL="${1:?Usage: fetch.sh <url> [proxy_url]}"
PROXY="${2:-}"

_curl() {
  if [ -n "$PROXY" ]; then
    https_proxy="$PROXY" http_proxy="$PROXY" curl -sfL "$@"
  else
    curl -sfL "$@"
  fi
}

_has_content() {
  [ "$(echo "$1" | wc -l)" -gt 5 ] && echo "$1" | grep -qv "Don't miss what's happening"
}

_try_once() {
  local out
  out=$("$@" 2>/dev/null || true)
  if _has_content "$out"; then echo "$out"; return 0; fi
  return 1
}

_with_retry() {
  _try_once "$@" && return 0
  sleep 2
  _try_once "$@" && return 0
  return 1
}

_agent_fetch_markdown() {
  local raw
  raw=$(npx --yes agent-fetch "$URL" --json 2>/dev/null || true)
  [ -n "$raw" ] || return 1
  command -v python3 >/dev/null 2>&1 || return 1
  printf '%s' "$raw" | python3 -c '
import json
import sys

try:
    data = json.load(sys.stdin)
except Exception:
    raise SystemExit(1)

if not isinstance(data, dict):
    raise SystemExit(1)

for key in ("markdown", "content", "text", "body"):
    value = data.get(key)
    if isinstance(value, str) and value.strip():
        sys.stdout.write(value)
        raise SystemExit(0)

raise SystemExit(1)
' 2>/dev/null
}

# 1. defuddle.md - cleaner output with YAML frontmatter
if OUT=$(_with_retry _curl "https://defuddle.md/$URL"); then echo "$OUT"; exit 0; fi

# 2. r.jina.ai - wide coverage, preserves image links
if OUT=$(_with_retry _curl "https://r.jina.ai/$URL"); then echo "$OUT"; exit 0; fi

# 3. agent-fetch - last resort local tool
if OUT=$(_agent_fetch_markdown); then printf '%s\n' "$OUT"; exit 0; fi

echo "ERROR: All fetch methods failed for: $URL" >&2
exit 1
