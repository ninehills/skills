#!/usr/bin/env python3
import argparse
import json
import re
import sys
from typing import List

from tvscreener import Market, StockField, StockScreener


OPS = [">=", "<=", "!=", ">", "<", "="]


def parse_field(token: str):
    # Supports: PRICE or RELATIVE_STRENGTH_INDEX_14|60
    if "|" in token:
        name, interval = token.split("|", 1)
        base = getattr(StockField, name)
        return base.with_interval(interval)
    return getattr(StockField, token)


def parse_value(v: str, force_string: bool = False):
    if force_string:
        return v
    if re.fullmatch(r"-?\d+", v):
        return int(v)
    if re.fullmatch(r"-?\d+\.\d+", v):
        return float(v)
    return v


def apply_filter(ss: StockScreener, expr: str):
    # FIELD>=123, FIELD=abc, FIELD<10.5
    op = next((o for o in OPS if o in expr), None)
    if not op:
        raise ValueError(f"Invalid filter: {expr}")
    left, right = expr.split(op, 1)
    left = left.strip()
    force_string = left in {"NAME", "ACTIVE_SYMBOL", "EXCHANGE"}
    right = parse_value(right.strip(), force_string=force_string)
    field = parse_field(left)

    if op == "=":
        ss.where(field == right)
    elif op == "!=":
        ss.where(field != right)
    elif op == ">":
        ss.where(field > right)
    elif op == "<":
        ss.where(field < right)
    elif op == ">=":
        ss.where(field >= right)
    elif op == "<=":
        ss.where(field <= right)


def main() -> int:
    p = argparse.ArgumentParser(description="Flexible tvscreener query")
    p.add_argument("--market", default="HONGKONG", help="e.g. HONGKONG/CHINA/AMERICA")
    p.add_argument("--symbol", default="", help="Exact symbol preference (e.g. HKEX:700)")
    p.add_argument("--fields", default="NAME,PRICE,CHANGE_PERCENT,VOLUME,RELATIVE_STRENGTH_INDEX_14,MACD_LEVEL_12_26,MACD_SIGNAL_12_26,MACD_HIST,SIMPLE_MOVING_AVERAGE_20,SIMPLE_MOVING_AVERAGE_50,SIMPLE_MOVING_AVERAGE_200,EXPONENTIAL_MOVING_AVERAGE_20,EXPONENTIAL_MOVING_AVERAGE_50,EXPONENTIAL_MOVING_AVERAGE_200,BOLLINGER_UPPER_BAND_20,BOLLINGER_LOWER_BAND_20,STOCHASTIC_PERCENTK_14_3_3,STOCHASTIC_PERCENTD_14_3_3,AVERAGE_TRUE_RANGE_14,MOVING_AVERAGES_RATING", help="Comma-separated StockField names; supports interval as FIELD|60")
    p.add_argument("--filter", action="append", default=[], help="Filter expression, e.g. PRICE>100 or NAME=600519")
    p.add_argument("--limit", type=int, default=100)
    p.add_argument("--csv", default="")
    args = p.parse_args()

    try:
        market = getattr(Market, args.market)
    except AttributeError:
        print(f"Invalid market: {args.market}", file=sys.stderr)
        return 2

    ss = StockScreener()
    ss.set_markets(market)
    ss.set_range(0, args.limit)

    field_tokens = [x.strip() for x in args.fields.split(",") if x.strip()]
    selected = [parse_field(x) for x in field_tokens]
    ss.select(*selected)

    for f in args.filter:
        apply_filter(ss, f)

    df = ss.get()

    if args.symbol:
        token = args.symbol.split(":")[-1]
        row = df[df["Symbol"] == args.symbol]
        if row.empty and "Name" in df.columns:
            row = df[df["Name"].astype(str) == token]
        df = row

    if df.empty:
        print(json.dumps({"found": False}, ensure_ascii=False))
        return 1

    if args.csv:
        df.to_csv(args.csv, index=False)

    print(df.to_json(orient="records", force_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
