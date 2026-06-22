# Nested Elements — Building, Navigating & Fully Exercising a pptx Table

The other `tables-*` examples showcase *styling*. This one teaches what a flat
property example can't, and the schema doesn't spell out — **how to build a
nested element level by level, address a deep node by path, and exercise the
full property surface of every level.** A pptx table is the vehicle: the deepest
common pptx tree, `slide → table → tr → tc`.

Four files: **tables-nested.sh** (CLI — this walkthrough explains it),
**tables-nested.py** (the SDK twin, one `doc.send()` per command),
**tables-nested.pptx** (4-slide result), **tables-nested.md** (this file).

## The two things to learn

### 1. Path addressing — element name ≠ path token

A child is reached by extending its parent's path, but the **path token differs
from the element name**:

| Element (in `help`) | Path token | Example path |
|---|---|---|
| `table` | `table` | `/slide[1]/table[1]` |
| `table-row` | **`tr`** | `/slide[1]/table[1]/tr[2]` |
| `table-cell` | **`tc`** | `/slide[1]/table[1]/tr[2]/tc[3]` |

So row 2 / column 3 is `/slide[1]/table[1]/tr[2]/tc[3]`. Indices are 1-based;
`last()` works (`…/tr[last()]`).

### 2. Property ownership — which level owns which property

| Level | Path | Owns |
|---|---|---|
| **table** | `/slide[1]/table[1]` | structure (`rows`, `cols`, `colWidths`, `data`) + table-wide style (`style`, `firstRow/lastRow/firstCol/lastCol`, `bandedRows/bandedCols`, `headerFill`, `bodyFill`, `border.*` incl. `horizontal`/`vertical`, `rowHeight`, `name`, `zorder`) |
| **row** (`tr`) | `…/tr[R]` | `height` (and `cols`) |
| **cell** (`tc`) | `…/tr[R]/tc[C]` | the **box** (`fill`, `opacity`, `bevel`, `image`, all `border.*` incl. diagonals `tl2br`/`tr2bl`, `padding`/`padding.bottom`, `valign`, `wrap`, `textdirection`, `direction`, `colspan`, `merge.right`, `merge.down`) **and** the **text** (`text`, `font`, `size`, `bold`, `italic`, `underline`, `strike`, `color`, `align`, `linespacing`, `spacebefore`, `spaceafter`) |

> pptx **flattens text onto the cell** — no separate run level inside a table
> cell, so `bold`/`color`/`align` go straight on the `tc`. (docx tables nest one
> deeper: `tc → paragraph → run`.)

## The 4 slides (full property surface, spread out)

One table can't show 30+ cell properties legibly, so the surface is split across
slides — **100% of the settable props on `table` / `table-row` / `table-cell`**:

| Slide | Teaches |
|---|---|
| **1 · Structure & ownership** | levels, path tokens, property ownership, `colspan`, and the **navigation** pass (`get`/`set` a deep cell after building) |
| **2 · Table level** | `data=` bulk fill, `headerFill`/`bodyFill`, `firstRow/lastRow/firstCol/lastCol`, `bandedRows/bandedCols`, every `border.*` edge, `rowHeight`/`colWidths`, `name`, `zorder` |
| **3 · Cell box** | all `border.*` (per-side, full, diagonals), `padding`/`padding.bottom`, `valign`, `wrap`, `textdirection`, `direction`, `bevel`, `opacity`, image fill, `merge.right`/`merge.down` |
| **4 · Cell text** | `font`, `size`, `bold`, `italic`, `underline`, `strike`, `color`, `align`, `linespacing`, `spacebefore`, `spaceafter` |

> `id` is intentionally **not** demonstrated: table ids are auto-assigned and
> must stay unique, so hardcoding one risks a collision. It's settable for
> round-trip fidelity, never something to set by hand.

## Build it, level by level (slide 1)

```bash
officecli add deck.pptx / --type slide                         # blank pptx has no slides

# Level 1 — the table; rows/cols/colWidths are add-time structure → /slide[1]/table[1]
officecli add deck.pptx /slide[1] --type table --prop rows=5 --prop cols=3 \
  --prop x=2.5cm --prop y=2.4cm --prop width=28cm --prop height=9cm --prop colWidths=12cm,8cm,8cm
officecli set deck.pptx /slide[1]/table[1] --prop style=medium2-accent1 \
  --prop firstRow=true --prop bandedRows=true                  # ← table owns style + banding

# Level 2 — a row owns only its height
officecli set deck.pptx /slide[1]/table[1]/tr[1] --prop height=2cm

# Level 3 — a cell owns box + text together
officecli set deck.pptx /slide[1]/table[1]/tr[1]/tc[1] \
  --prop text=Region --prop bold=true --prop color=FFFFFF \
  --prop align=center --prop valign=middle --prop fill=1F6FEB
```

`style` accepts `medium1..4` / `light1..3` / `dark1..2` / `none`, optionally
`-accentN` (e.g. `medium2-accent1`).

## Nesting-only operations

These exist *only* because cells sit in a grid — no flat property has an
equivalent. They consume a neighbour, so place them where the swallowed cell is
intentionally blank:

```bash
officecli set deck.pptx /slide[1]/table[1]/tr[5]/tc[1] --prop colspan=3 --prop text="TOTAL …"   # span 3 cols (alias: gridspan)
officecli set deck.pptx /slide[3]/table[1]/tr[4]/tc[4] --prop merge.down=1 --prop text="↓"      # swallow the cell below
officecli set deck.pptx /slide[3]/table[1]/tr[5]/tc[1] --prop merge.right=2 --prop text="→"      # swallow the cell to the right
```

`colspan` is the canonical key (matches docx, round-trips on `get`); `gridspan`
is accepted as an input alias.

## Add-time vs settable

A few `table` props are **add-only** — pass them on `add`, not `set`:
`id` (auto-assigned identity), `headerFill`/`bodyFill` (creation-time bulk fills),
and the structural `cols`, `rows`, `data`. (`data` defines the grid, so it's
mutually exclusive with `rows`/`cols`.) Everything else — including `colWidths`,
`rowHeight`, and `zorder` — is settable on the live table too.

## Navigate — address a deep node *after* building

The same path that built a cell reaches it later:

```bash
officecli get deck.pptx /slide[1]/table[1]/tr[4]/tc[3]                       # read the East/Revenue cell
officecli set deck.pptx /slide[1]/table[1]/tr[4]/tc[3] --prop fill=FFF2CC    # re-style in place (amber highlight)
officecli query deck.pptx "tc"                                              # or query every cell
```

## Regenerate

```bash
cd examples/ppt/tables
bash tables-nested.sh                # via the CLI
# — or —
pip install officecli-sdk            # the SDK (officecli binary still required)
python3 tables-nested.py             # via the SDK, same result
# → tables-nested.pptx (4 slides)
```

## Why this matters (vs flat property examples)

`presentation-settings` and friends set flat `key=value` on one container. Real
documents are **trees** — the hard part isn't "what properties exist" (the schema
lists those), it's **where each property lives and how to reach a node three
levels down**. Apply the same level-by-level + navigate + full-surface pattern to
docx tables (`tr → tc → paragraph → run`) and xlsx charts (`chart → series + axis`).
