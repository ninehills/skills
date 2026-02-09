#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PYTHON_BIN="${PYTHON_BIN:-python3}"

"$PYTHON_BIN" - <<'PY'
import sys
if sys.version_info < (3, 10):
    raise SystemExit("Python>=3.10 required. Please activate your conda workshop env, then rerun.")
PY

"$PYTHON_BIN" - <<'PY' >/dev/null 2>&1 || "$PYTHON_BIN" -m pip install -q -U tvscreener
from tvscreener import Market, StockField, StockScreener
PY

exec "$PYTHON_BIN" "$SCRIPT_DIR/custom_query.py" "$@"
