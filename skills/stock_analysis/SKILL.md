---
name: stock-analysis
description: "Use when the user asks to analyze stocks, check stock prices, get technical analysis, query market data, or evaluate buy/sell signals. Fetches real-time quotes and technical indicators (MACD, RSI, moving averages, support/resistance) for A-shares, funds, and HK stocks. Generates a decision dashboard with scoring and actionable trade signals."
---

## Workflow

1. **Receive stock codes**: Get the stock code(s) from the user. HK stocks use `HKxxxx` format
2. **Execute query**: Run `uv run python stock.py --stocks <codes>` from the working directory
3. **Review output**: Check the returned data modules (quotes, trend, MACD, RSI, signals)
4. **Generate analysis**: Apply the trading framework below to produce a decision dashboard with scores and recommendations

Working directory: `/Users/cynic/src/github.com/ninehills/daily_stock_analysis`

## Usage

### Single stock
```bash
uv run python stock.py --stocks 600519
```

### Multiple stocks
```bash
uv run python stock.py --stocks 600519,000001,300750
```

## Output Modules

| Module | Content |
|--------|---------|
| 实时行情 | Price, change%, volume, turnover, market cap, P/E |
| 趋势分析 | Trend state, MA alignment (MA5/10/20/60), deviation rate |
| 量能分析 | Volume state, volume ratio |
| 支撑压力 | Support and resistance levels |
| MACD | DIF, DEA, histogram, golden/death cross signals |
| RSI | RSI(6/12/24), overbought/oversold state |
| 筹码分布 | Profit ratio, average cost, chip concentration |
| 操作信号 | Buy/sell/hold recommendation, composite score, reasoning |

## Analysis Framework

### Trading Rules

- Never chase highs: do not buy when price deviates >5% from MA5
- Trend trading: only trade stocks with bullish MA alignment (MA5 > MA10 > MA20)
- Best entry: pullback to MA5 support on declining volume
- Watch for risks: insider selling, profit warnings, regulatory actions, large unlocks

### Scoring

| Score | Signal | Conditions |
|-------|--------|------------|
| 80-100 | Strong buy | Bullish MA, deviation <2%, healthy chips, positive catalyst |
| 60-79 | Buy | Bullish or weak-bullish MA, deviation <5%, normal volume |
| 40-59 | Hold/Watch | Deviation >5%, unclear trend, or risk events |
| 0-39 | Sell/Reduce | Bearish MA, broke MA20, heavy selling, major negative news |

### Dashboard Principles

1. Lead with the core conclusion (buy/sell/hold in one sentence)
2. Separate advice for existing holders vs new positions
3. Give specific price targets, not vague ranges
4. Visualize checklist with clear pass/warn/fail markers
5. Highlight risk items prominently
