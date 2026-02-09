---
name: tvscreener
description: Query TradingView screener data for HK, A-share, A-share ETF, and US symbols with deepentropy/tvscreener. Use for stock lookup, technical indicators (price/change/RSI/MACD/volume), symbol filtering, and custom field/filter-based market queries.
---

# tvscreener

Use this skill for market queries with simple scripts first, then native Python when needed.

## Install

```bash
python3 -m pip install -U tvscreener
```

Python must be `>=3.10`.

## Quick commands (run from skill root)

> Use Python 3.10+ in your preferred environment (venv/pyenv/system Python).

```bash
# Preset single-symbol output (recommended)
python3 scripts/query_symbol.py --symbol HKEX:700 --market HONGKONG

# Custom query (fields + filters)
bash scripts/run_query.sh \
  --market CHINA \
  --symbol SHSE:600519 \
  --fields 'NAME,PRICE,CHANGE_PERCENT,VOLUME,RELATIVE_STRENGTH_INDEX_14,MACD_LEVEL_12_26,MACD_SIGNAL_12_26,MACD_HIST,SIMPLE_MOVING_AVERAGE_20,SIMPLE_MOVING_AVERAGE_50,SIMPLE_MOVING_AVERAGE_200,EXPONENTIAL_MOVING_AVERAGE_20,EXPONENTIAL_MOVING_AVERAGE_50,EXPONENTIAL_MOVING_AVERAGE_200,BOLLINGER_UPPER_BAND_20,BOLLINGER_LOWER_BAND_20,STOCHASTIC_PERCENTK_14_3_3,STOCHASTIC_PERCENTD_14_3_3,AVERAGE_TRUE_RANGE_14,MOVING_AVERAGES_RATING' \
  --filter 'NAME=600519'

# Field discovery
python3 scripts/discover_fields.py --keyword macd --limit 20
```

### Shell quoting notes

- Wrap `--fields` and `--filter` in single quotes.
- If you use interval syntax like `FIELD|60`, quoting is mandatory to avoid shell pipe parsing.


## Query rules

- Core technical set (recommended): `PRICE`, `CHANGE_PERCENT`, `VOLUME`, `RELATIVE_STRENGTH_INDEX_14`, `MACD_LEVEL_12_26`, `MACD_SIGNAL_12_26`, `MACD_HIST`, `SIMPLE_MOVING_AVERAGE_20/50/200`, `EXPONENTIAL_MOVING_AVERAGE_20/50/200`, `BOLLINGER_UPPER_BAND_20`, `BOLLINGER_LOWER_BAND_20`, `STOCHASTIC_PERCENTK_14_3_3`, `STOCHASTIC_PERCENTD_14_3_3`, `AVERAGE_TRUE_RANGE_14`, `MOVING_AVERAGES_RATING`
- Interval fields syntax: `FIELD|60` / `FIELD|240` (example: `RELATIVE_STRENGTH_INDEX_14|60`)
  - **Current caveat**: interval fields may fail in `scripts/custom_query.py` with `FieldWithInterval` attribute errors in some tvscreener versions.
  - Workaround: run without interval fields, or use `scripts/query_symbol.py` for stable single-symbol technical snapshots.
- Filters: `=`, `!=`, `>`, `<`, `>=`, `<=`

## Troubleshooting

- `ImportError: cannot import name 'Market' from 'tvscreener'`
  - Usually caused by mismatched Python/site-packages or multiple Python environments.
  - Fix: ensure commands and installation use the same Python (3.10+), then reinstall:
    - `python3 -m pip install -U tvscreener`
- `zsh: command not found: 60,...`
  - Cause: unquoted `FIELD|60` interpreted as shell pipes.
  - Fix: single-quote the full `--fields` string.

## References

- Workflow + patterns: `references/README_USAGE.md`
- API details:
  - `references/api/screeners.md`
  - `references/api/fields.md`
  - `references/api/filters.md`
  - `references/api/enums.md`

If scripts are insufficient, read references and write direct Python using tvscreener native API.

## Regression test

```bash
bash scripts/test_markets.sh
```

Covers Tencent (HK), Moutai (A), A-share ETF (510300), and BIDU (US).
