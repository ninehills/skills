#!/usr/bin/env bash
# Auto-detect and run project verification (lint + typecheck + tests).
# Run from the project root. Exits non-zero on failure.
set -euo pipefail

if [ -f Cargo.toml ]; then
  cargo check && cargo test
elif [ -f tsconfig.json ]; then
  npx tsc --noEmit && npm test
elif [ -f package.json ] && grep -q '"test"' package.json; then
  npm test
elif [ -f Makefile ] && grep -q '^test:' Makefile; then
  make test
elif [ -f pytest.ini ] || [ -f pyproject.toml ] || find . -maxdepth 2 -name "test_*.py" | grep -q .; then
  pytest
else
  echo "(no test command detected - ask the user for the verification command)"
  exit 1
fi
