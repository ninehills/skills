#!/usr/bin/env bash
# Patent Draft Agent - Environment Initialization Check
# Verifies required API keys are set before running the skill.

set -euo pipefail

SKILL_DIR="$(cd "$(dirname "$0")/.." && pwd)"
MISSING=()

# Required environment variables
for var in SERPAPI_API_KEY; do
  if [ -z "${!var:-}" ]; then
    MISSING+=("$var")
  fi
done

if [ ${#MISSING[@]} -gt 0 ]; then
  echo "ERROR: Missing required environment variables:"
  for var in "${MISSING[@]}"; do
    echo "  - $var"
  done
  echo ""
  echo "Please set them before running this skill:"
  echo "  export SERPAPI_API_KEY=your_serpapi_key"
  exit 1
fi

# Verify mcporter is available
if ! command -v mcporter &>/dev/null; then
  echo "ERROR: mcporter is not installed."
  echo "Install it with: npm install -g mcporter"
  exit 1
fi

# Verify mcporter can discover MCP servers
MCPORTER_CONFIG="${SKILL_DIR}/references/mcporter.json"
if [ ! -f "$MCPORTER_CONFIG" ]; then
  echo "ERROR: mcporter config not found at ${MCPORTER_CONFIG}"
  exit 1
fi

if [ -n "${EXA_API_KEY:-}" ]; then
  echo "OK: EXA_API_KEY is set. Exa search is enabled."
else
  echo "WARN: EXA_API_KEY is not set. Exa search is disabled; fall back to built-in search/browse for non-patent sources."
fi

echo "OK: Required environment variables are set."
echo "OK: mcporter is available."
echo "OK: Config found at ${MCPORTER_CONFIG}"
echo "SKILL_DIR=${SKILL_DIR}"
