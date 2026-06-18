#!/usr/bin/env bash
# Fetch a URL as Markdown.
#
# Privacy-first cascade:
#   Default (no --use-proxy): local extractor only. URL is never sent to a
#   third party. Best quality when readability-lxml + html2text are pip-
#   installed; degrades to a stdlib-only stripper otherwise.
#
#   With --use-proxy: tries local first, then defuddle.md, then r.jina.ai.
#   Use this for JS-heavy pages, X/Twitter, paywalls, or anything the local
#   extractor cannot reach. Be aware: the URL is sent to those third-party
#   services and may be cached or logged. Never feed sensitive URLs through
#   --use-proxy.
#
# Every tier writes a structured stderr line:
#   [fetch] tier=<local|defuddle|jina> status=<ok|fail|skip> reason="..."
#
# Special thanks to joeseesun for the qiaomu-markdown-proxy project, which
# inspired the proxy cascade design:
# https://github.com/joeseesun/qiaomu-markdown-proxy
#
# Usage:
#   fetch.sh <url> [proxy_url]
#   fetch.sh --use-proxy <url> [proxy_url]
set -euo pipefail

USE_PROXY=0
if [ "${1:-}" = "--use-proxy" ]; then
  USE_PROXY=1
  shift
fi

URL="${1:?Usage: fetch.sh [--use-proxy] <url> [proxy_url]}"
PROXY="${2:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

LOCAL_ERR="$(mktemp)"
trap 'rm -f "$LOCAL_ERR"' EXIT

# shellcheck disable=SC2329,SC2317  # called indirectly via _with_retry / _try_once
_curl() {
  if [ -n "$PROXY" ]; then
    https_proxy="$PROXY" http_proxy="$PROXY" curl -sfL --connect-timeout 10 --max-time 30 "$@"
  else
    curl -sfL --connect-timeout 10 --max-time 30 "$@"
  fi
}

_has_content() {
  local content="$1"
  [ "$(printf '%s' "$content" | wc -l)" -gt 5 ] || return 1
  # Reject pages dominated by login walls, captchas, or bot challenges that
  # otherwise pass the line-count check. Add new markers here, not new branches.
  if printf '%s' "$content" | grep -qE "Don't miss what's happening|Sign in to continue|Please sign in|Log in to continue|请登录|登录后查看|机器人验证|人机验证|Just a moment\.\.\.|Checking your browser" 2>/dev/null; then
    return 1
  fi
  return 0
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

# Tier 1: local extractor. Always tried first.
if OUT=$(python3 "$SCRIPT_DIR/fetch_local.py" "$URL" 2>"$LOCAL_ERR"); then
  cat "$LOCAL_ERR" >&2 2>/dev/null || true
  echo "$OUT"
  exit 0
fi
cat "$LOCAL_ERR" >&2 2>/dev/null || true

# Without --use-proxy, stop here. URL never leaves the machine.
if [ "$USE_PROXY" -eq 0 ]; then
  echo "[fetch] status=fail reason=\"local extractor failed; rerun with --use-proxy to try defuddle.md and r.jina.ai (URL will be sent to those services)\"" >&2
  exit 1
fi

# Tier 2: defuddle.md (third party; user opted in via --use-proxy).
if OUT=$(_with_retry _curl "https://defuddle.md/$URL"); then
  echo "[fetch] tier=defuddle status=ok" >&2
  echo "$OUT"
  exit 0
fi
echo "[fetch] tier=defuddle status=fail reason=\"empty or paywall-like response\"" >&2

# Tier 3: r.jina.ai (third party; user opted in via --use-proxy).
if OUT=$(_with_retry _curl "https://r.jina.ai/$URL"); then
  echo "[fetch] tier=jina status=ok" >&2
  echo "$OUT"
  exit 0
fi
echo "[fetch] tier=jina status=fail reason=\"empty or paywall-like response\"" >&2

echo "[fetch] status=fail reason=\"all tiers (local, defuddle, jina) failed for $URL\"" >&2
exit 1
