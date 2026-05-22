#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# Let the Python checker run the doc-ref checker as a subprocess; keep the env
# var so existing callers depending on this delegation keep working.
export DOC_REF_CHECKER="${DOC_REF_CHECKER:-$SCRIPT_DIR/check-doc-refs.sh}"
exec python3 "$SCRIPT_DIR/check_maintainability.py" "$@"
