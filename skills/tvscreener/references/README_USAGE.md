# README_USAGE

Practical usage guide for this skill + mapping to API references.

Environment requirement: use `python3` with version `>=3.10` (recommended: conda `workshop` env).

## 1) Preferred execution order

1. Use `scripts/query_symbol.py` for a fast single-symbol snapshot.
2. Use `scripts/custom_query.py` for custom fields and filters.
3. Use `scripts/discover_fields.py` when field names are unknown.
4. If still insufficient, write direct Python using the native tvscreener API.

## 2) Script examples

```bash
# Tencent (HK)
python3 scripts/query_symbol.py --symbol HKEX:700 --market HONGKONG

# Moutai (A-share)
python3 scripts/custom_query.py --market CHINA --symbol SHSE:600519 --fields NAME,PRICE,CHANGE_PERCENT,VOLUME,RELATIVE_STRENGTH_INDEX_14,MACD_LEVEL_12_26,MACD_SIGNAL_12_26,MACD_HIST,SIMPLE_MOVING_AVERAGE_20,SIMPLE_MOVING_AVERAGE_50,SIMPLE_MOVING_AVERAGE_200,EXPONENTIAL_MOVING_AVERAGE_20,EXPONENTIAL_MOVING_AVERAGE_50,EXPONENTIAL_MOVING_AVERAGE_200,BOLLINGER_UPPER_BAND_20,BOLLINGER_LOWER_BAND_20,STOCHASTIC_PERCENTK_14_3_3,STOCHASTIC_PERCENTD_14_3_3,AVERAGE_TRUE_RANGE_14,MOVING_AVERAGES_RATING --filter "NAME=600519"

# CSI300 ETF (A-share ETF)
python3 scripts/custom_query.py --market CHINA --symbol SHSE:510300 --filter "NAME=510300"

# BIDU (US)
python3 scripts/custom_query.py --market AMERICA --symbol NASDAQ:BIDU --filter "NAME=BIDU"
```

## 3) Field/filter conventions

- Field example (full technical template): `PRICE`, `CHANGE_PERCENT`, `VOLUME`, `RELATIVE_STRENGTH_INDEX_14`, `MACD_LEVEL_12_26`, `MACD_SIGNAL_12_26`, `MACD_HIST`, `SIMPLE_MOVING_AVERAGE_20/50/200`, `EXPONENTIAL_MOVING_AVERAGE_20/50/200`, `BOLLINGER_UPPER_BAND_20`, `BOLLINGER_LOWER_BAND_20`, `STOCHASTIC_PERCENTK_14_3_3`, `STOCHASTIC_PERCENTD_14_3_3`, `AVERAGE_TRUE_RANGE_14`, `MOVING_AVERAGES_RATING`
- Interval field: `RELATIVE_STRENGTH_INDEX_14|60`
- Filter ops: `=`, `!=`, `>`, `<`, `>=`, `<=`
- String-like fields (`NAME`, `ACTIVE_SYMBOL`, `EXCHANGE`) are treated as strings.

## 4) Native Python fallback

Use this when scripts cannot express the requested logic.

```python
from tvscreener import StockScreener, StockField, Market

ss = StockScreener()
ss.set_markets(Market.HONGKONG)
ss.set_range(0, 200)

ss.select(
    StockField.NAME,
    StockField.PRICE,
    StockField.CHANGE_PERCENT,
    StockField.RELATIVE_STRENGTH_INDEX_14,
    StockField.MACD_LEVEL_12_26,
)
ss.where(StockField.NAME == "700")

print(ss.get().to_json(orient="records", force_ascii=False, indent=2))
```

## 5) API reference map

- Screeners: `references/api/screeners.md`
- Fields: `references/api/fields.md`
- Filters: `references/api/filters.md`
- Enums/Markets: `references/api/enums.md`

## 6) Known caveats

- Exchange prefix may differ in returned symbol (`SHSE:600519` vs `SSE:600519`).
- If `with_interval()` combinations fail, fallback to base daily fields first.
