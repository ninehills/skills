# Enums API Reference

Enumeration types for type-safe configuration.

## IndexSymbol

Major stock indices for filtering.

```python
from tvscreener import IndexSymbol
```

### Major US Indices

| Index | Symbol |
|-------|--------|
| S&P 500 | `IndexSymbol.SP500` |
| NASDAQ 100 | `IndexSymbol.NASDAQ_100` |
| Dow Jones Industrial | `IndexSymbol.DOW_JONES` |
| Russell 2000 | `IndexSymbol.RUSSELL_2000` |
| Russell 1000 | `IndexSymbol.RUSSELL_1000` |

### Sector Indices

| Index | Symbol |
|-------|--------|
| S&P 500 Technology | `IndexSymbol.SP500_INFORMATION_TECHNOLOGY` |
| S&P 500 Healthcare | `IndexSymbol.SP500_HEALTH_CARE` |
| S&P 500 Financials | `IndexSymbol.SP500_FINANCIALS` |
| S&P 500 Energy | `IndexSymbol.SP500_ENERGY` |
| PHLX Semiconductor | `IndexSymbol.PHLX_SEMICONDUCTOR` |

### Usage

```python
from tvscreener import StockScreener, IndexSymbol

ss = StockScreener()

# Single index
ss.set_index(IndexSymbol.SP500)

# Multiple indices
ss.set_index(IndexSymbol.NASDAQ_100, IndexSymbol.DOW_JONES)
```

### Search

```python
# Find indices by name
results = IndexSymbol.search("technology")
for idx in results:
    print(idx.name, idx.value)
```

---

## Market

Geographic market regions.

```python
from tvscreener import Market
```

### Available Markets

| Market | Description |
|--------|-------------|
| `Market.ALL` | All markets |
| `Market.AMERICA` | United States |
| `Market.UK` | United Kingdom |
| `Market.GERMANY` | Germany |
| `Market.FRANCE` | France |
| `Market.JAPAN` | Japan |
| `Market.CHINA` | China |
| `Market.HONG_KONG` | Hong Kong |
| `Market.INDIA` | India |
| `Market.BRAZIL` | Brazil |
| `Market.CANADA` | Canada |
| `Market.AUSTRALIA` | Australia |

### Usage

```python
ss = StockScreener()

# Single market
ss.set_markets(Market.AMERICA)

# Multiple markets
ss.set_markets(Market.AMERICA, Market.UK, Market.GERMANY)
```

---

## SymbolType

Security types for filtering.

```python
from tvscreener import SymbolType
```

### Available Types

| Type | Description |
|------|-------------|
| `SymbolType.COMMON_STOCK` | Common shares |
| `SymbolType.ETF` | Exchange-Traded Funds |
| `SymbolType.PREFERRED_STOCK` | Preferred shares |
| `SymbolType.REIT` | Real Estate Investment Trusts |
| `SymbolType.CLOSED_END_FUND` | Closed-end funds |
| `SymbolType.MUTUAL_FUND` | Mutual funds |
| `SymbolType.ADR` | American Depositary Receipts |

### Usage

```python
ss = StockScreener()

# Single type
ss.set_symbol_types(SymbolType.COMMON_STOCK)

# Multiple types
ss.set_symbol_types(SymbolType.COMMON_STOCK, SymbolType.ETF)
```

### ETF Screening Example

```python
ss = StockScreener()
ss.set_symbol_types(SymbolType.ETF)
ss.where(StockField.VOLUME > 1_000_000)
ss.sort_by(StockField.VOLUME, ascending=False)

df = ss.get()
```

---

## Exchange

Stock exchanges.

```python
from tvscreener import Exchange
```

### Available Exchanges

| Exchange | Symbol |
|----------|--------|
| NASDAQ | `Exchange.NASDAQ` |
| NYSE | `Exchange.NYSE` |
| NYSE American | `Exchange.NYSE_AMERICAN` |
| NYSE Arca | `Exchange.NYSE_ARCA` |

### Usage with Filters

```python
ss = StockScreener()
ss.where(StockField.EXCHANGE == Exchange.NASDAQ)

# Or multiple
ss.where(StockField.EXCHANGE.isin([Exchange.NASDAQ, Exchange.NYSE]))
```

---

## FilterOperator

Filter operations for comparisons.

```python
from tvscreener import FilterOperator
```

### Available Operators

| Operator | Value | Python Equivalent |
|----------|-------|-------------------|
| `FilterOperator.ABOVE` | `'greater'` | `>` |
| `FilterOperator.ABOVE_OR_EQUAL` | `'egreater'` | `>=` |
| `FilterOperator.BELOW` | `'less'` | `<` |
| `FilterOperator.BELOW_OR_EQUAL` | `'eless'` | `<=` |
| `FilterOperator.EQUAL` | `'equal'` | `==` |
| `FilterOperator.NOT_EQUAL` | `'nequal'` | `!=` |
| `FilterOperator.IN_RANGE` | `'in_range'` | `.between()` |
| `FilterOperator.NOT_IN_RANGE` | `'not_in_range'` | `.not_between()` |

### Legacy Usage

```python
ss = StockScreener()
ss.where(StockField.PRICE, FilterOperator.ABOVE, 100)
```

### Modern Syntax (Preferred)

```python
ss = StockScreener()
ss.where(StockField.PRICE > 100)
```

---

## TimeInterval

Time intervals for technical indicators.

### Available Intervals

| Interval | Code |
|----------|------|
| 1 minute | `'1'` |
| 5 minutes | `'5'` |
| 15 minutes | `'15'` |
| 30 minutes | `'30'` |
| 1 hour | `'60'` |
| 2 hours | `'120'` |
| 4 hours | `'240'` |
| Daily | `'1D'` |
| Weekly | `'1W'` |
| Monthly | `'1M'` |

### Usage

```python
# RSI on 1-hour timeframe
rsi_1h = StockField.RELATIVE_STRENGTH_INDEX_14.with_interval('60')

# MACD on 4-hour timeframe
macd_4h = StockField.MACD_LEVEL_12_26.with_interval('240')

# SMA on weekly timeframe
sma_weekly = StockField.SIMPLE_MOVING_AVERAGE_50.with_interval('1W')
```

---

## Complete Example

```python
from tvscreener import (
    StockScreener,
    StockField,
    IndexSymbol,
    Market,
    SymbolType,
    Exchange
)

ss = StockScreener()

# Market and type filters
ss.set_markets(Market.AMERICA)
ss.set_symbol_types(SymbolType.COMMON_STOCK)
ss.set_index(IndexSymbol.SP500)

# Exchange filter
ss.where(StockField.EXCHANGE.isin([Exchange.NASDAQ, Exchange.NYSE]))

# Price and volume
ss.where(StockField.PRICE.between(50, 500))
ss.where(StockField.VOLUME >= 1_000_000)

# Technical with interval
rsi_1h = StockField.RELATIVE_STRENGTH_INDEX_14.with_interval('60')
ss.where(rsi_1h < 40)

df = ss.get()
```
