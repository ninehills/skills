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

"$PYTHON_BIN" "$SCRIPT_DIR/query_symbol.py" --symbol HKEX:700 --market HONGKONG
"$PYTHON_BIN" "$SCRIPT_DIR/query_symbol.py" --symbol SHSE:600519 --market CHINA
"$PYTHON_BIN" "$SCRIPT_DIR/query_symbol.py" --symbol SHSE:510300 --market CHINA
"$PYTHON_BIN" "$SCRIPT_DIR/query_symbol.py" --symbol NASDAQ:BIDU --market AMERICA

# custom query path (caller-defined fields)
"$PYTHON_BIN" "$SCRIPT_DIR/custom_query.py" --market HONGKONG --symbol HKEX:700 --fields NAME,PRICE,RELATIVE_STRENGTH_INDEX_14,MACD_LEVEL_12_26 --filter "NAME=700" >/dev/null
"$PYTHON_BIN" "$SCRIPT_DIR/custom_query.py" --market CHINA --symbol SHSE:600519 --fields NAME,PRICE,CHANGE_PERCENT --filter "NAME=600519" >/dev/null
"$PYTHON_BIN" "$SCRIPT_DIR/custom_query.py" --market CHINA --symbol SHSE:510300 --fields NAME,PRICE,VOLUME --filter "NAME=510300" >/dev/null
"$PYTHON_BIN" "$SCRIPT_DIR/custom_query.py" --market AMERICA --symbol NASDAQ:BIDU --fields NAME,PRICE,CHANGE_PERCENT --filter "NAME=BIDU" >/dev/null

echo "PASS: HK/A-share/A-share ETF/US symbols all resolved (preset + custom fields)."
