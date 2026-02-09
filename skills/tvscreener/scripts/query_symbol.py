#!/usr/bin/env python3
import argparse
import json
import sys

from tvscreener import Market, StockField, StockScreener


def main() -> int:
    p = argparse.ArgumentParser(description="Query one stock symbol from tvscreener")
    p.add_argument("--symbol", default="HKEX:700", help="e.g. HKEX:700")
    p.add_argument("--market", default="HONGKONG", help="Market enum name, e.g. HONGKONG")
    p.add_argument("--csv", default="", help="Optional CSV output path")
    args = p.parse_args()

    try:
        market = getattr(Market, args.market)
    except AttributeError:
        print(f"Invalid market: {args.market}", file=sys.stderr)
        return 2

    ss = StockScreener()
    ss.set_markets(market)
    ss.set_range(0, 500)
    ss.select(
        StockField.NAME,
        StockField.PRICE,
        StockField.CHANGE_PERCENT,
        StockField.VOLUME,
        StockField.RELATIVE_STRENGTH_INDEX_14,
        StockField.MACD_LEVEL_12_26,
        StockField.MACD_SIGNAL_12_26,
        StockField.MACD_HIST,
        StockField.SIMPLE_MOVING_AVERAGE_20,
        StockField.SIMPLE_MOVING_AVERAGE_50,
        StockField.SIMPLE_MOVING_AVERAGE_200,
        StockField.EXPONENTIAL_MOVING_AVERAGE_20,
        StockField.EXPONENTIAL_MOVING_AVERAGE_50,
        StockField.EXPONENTIAL_MOVING_AVERAGE_200,
        StockField.BOLLINGER_UPPER_BAND_20,
        StockField.BOLLINGER_LOWER_BAND_20,
        StockField.STOCHASTIC_PERCENTK_14_3_3,
        StockField.STOCHASTIC_PERCENTD_14_3_3,
        StockField.AVERAGE_TRUE_RANGE_14,
        StockField.MOVING_AVERAGES_RATING,
        StockField.RECOMMENDATION_MARK,
    )

    # Server-side narrow down by ticker part to avoid missing symbols outside top-N page.
    token = args.symbol.split(":")[-1]
    ss.where(StockField.NAME == token)
    df = ss.get()

    row = df[df["Symbol"] == args.symbol]
    if row.empty:
        # Exchange prefix may differ (e.g. SHSE:600519 vs SSE:600519), fallback by token.
        row = df[df["Name"].astype(str) == token]

    if row.empty:
        print(json.dumps({"symbol": args.symbol, "found": False}, ensure_ascii=False))
        return 1

    payload = row.iloc[0].to_dict()
    print(json.dumps(payload, ensure_ascii=False, default=str, indent=2))

    if args.csv:
        row.to_csv(args.csv, index=False)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
