#!/usr/bin/env python3
import argparse
import json

from tvscreener import StockField


def main() -> int:
    p = argparse.ArgumentParser(description="Discover tvscreener StockField constants")
    p.add_argument("--keyword", default="", help="keyword to search, e.g. rsi/macd/volume")
    p.add_argument("--limit", type=int, default=30)
    args = p.parse_args()

    if args.keyword:
        fields = StockField.search(args.keyword)
    else:
        fields = StockField.technicals()

    out = []
    for f in fields[: args.limit]:
        out.append({"name": f.name, "label": getattr(f, "label", "")})

    print(json.dumps(out, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
