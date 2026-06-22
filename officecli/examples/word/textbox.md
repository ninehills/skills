# Textbox Showcase

Demonstrates 10 complex textbox scenarios using the `wps:wsp` WordprocessingShape model (OOXML Drawing ML). Because `officecli` does not yet have a high-level `--type textbox` add command, each scenario is injected via `officecli raw-set` with pre-authored XML. Three files:

- **textbox.sh** — builds the document with `officecli raw-set` (676 lines, 10 scenarios).
- **textbox.docx** — generated output; open in Word to see each floating shape.
- **textbox.md** — this file.

## Regenerate

```bash
cd examples/word
bash textbox.sh
# → textbox.docx
```

## How Textboxes Are Inserted

All 10 scenarios use the same insertion pattern:

```bash
officecli raw-set textbox.docx /document \
  --xpath "//w:body/w:sectPr" \
  --action insertbefore \
  --xml '<w:p> ... <mc:AlternateContent> ... </mc:AlternateContent> ... </w:p>'
```

The `mc:AlternateContent` wrapper follows the OOXML spec:
- `mc:Choice Requires="wps"` — the modern `wps:wsp` WordprocessingShape (Word 2010+).
- `mc:Fallback` — optional VML fallback for older renderers (Scenario 1 only).

Each `wps:wsp` element has three children:
- `wps:cNvSpPr` — marks it as a text box (`txBox="1"`).
- `wps:spPr` — geometry, fill, border, effects.
- `wps:txbx` → `w:txbxContent` — the actual paragraph/table content inside the box.
- `wps:bodyPr` — text body layout: rotation, vertical flow, wrap, insets, anchor.

## Scenario 1: Basic Textbox (Solid Fill + Border + VML Fallback)

A rectangle with a solid light-blue fill, a 2pt blue border, and top-and-bottom text wrapping. Includes a VML `mc:Fallback` for legacy renderers.

Key `wps:spPr` attributes:
- `a:solidFill` → `a:srgbClr val="E6F3FF"` (light blue fill)
- `a:ln w="25400"` → `a:solidFill val="0070C0"` (2pt blue border)
- `wp:wrapTopAndBottom` (body text flows above and below)
- `wps:bodyPr anchor="t"` (text anchored to top of box)

**Features:** solid fill (`a:solidFill`), border width (`a:ln w`), border color, `wp:wrapTopAndBottom`, VML fallback (`v:shape` in `mc:Fallback`)

## Scenario 2: Multi-Paragraph Rich Text Textbox

A taller box with a dashed orange border and rich mixed-format content: bold, italic, underline, strikethrough, color, highlight, and right-aligned text — all inside `w:txbxContent`.

Key `wps:spPr` attributes:
- `a:prstDash val="dash"` on `a:ln` (dashed border)
- Multiple paragraphs with varied `w:rPr` combinations inside `w:txbxContent`

**Features:** dashed border (`a:prstDash val="dash|solid|dot|…"`), multi-paragraph textbox content, mixed run formatting inside `w:txbxContent` (bold/italic/underline/strike/color/highlight)

## Scenario 3: Textbox with Nested Table

A gray-bordered box containing a paragraph header followed by a `w:tbl` — demonstrating that `w:txbxContent` can hold any valid body content including tables.

Key structure inside `w:txbxContent`:
```xml
<w:p> ... heading ... </w:p>
<w:tbl>
  <w:tblPr><w:tblStyle w:val="TableGrid"/></w:tblPr>
  <w:tr> ... header cells with blue fill ... </w:tr>
  <w:tr> ... data cells ... </w:tr>
</w:tbl>
```

**Features:** `w:tbl` nested inside `w:txbxContent` (full table-in-textbox), `w:tblStyle` reference, per-cell `w:shd` fill

## Scenario 4: Rotated Textbox (45 degrees + Gradient Fill)

A box rotated 45° using `a:xfrm rot="2700000"` (OOXML rotation units; 60000 = 1°, so 2700000 = 45°) with a red-to-yellow gradient fill.

Key `wps:spPr` attributes:
- `a:xfrm rot="2700000"` (45° clockwise rotation)
- `a:gradFill` with two gradient stops: `FF6B6B` at position 0 and `FFE66D` at position 100000
- `wps:bodyPr anchor="ctr"` (text centred in the box despite rotation)

**Features:** shape rotation (`a:xfrm rot`; 60000 per degree), gradient fill (`a:gradFill`/`a:gsLst`/`a:gs pos`), body text centering (`anchor="ctr"`)

## Scenario 5: Vertical Text Textbox

A narrow tall box with `wps:bodyPr vert="eaVert"` — East-Asian vertical text flow where characters are rotated 90° and read top-to-bottom.

Key `wps:bodyPr` attribute:
- `vert="eaVert"` (East-Asian vertical layout; also available: `horz`, `vert`, `vert270`, `wordArtVert`)

**Features:** `wps:bodyPr vert="eaVert"` (vertical text orientation)

## Scenario 6: Rounded Rectangle Textbox + Drop Shadow

Uses `a:prstGeom prst="roundRect"` for rounded corners and an `a:effectLst/a:outerShdw` for a soft drop shadow.

Key `wps:spPr` attributes:
- `a:prstGeom prst="roundRect"` with `a:gd name="adj" fmla="val 16667"` (corner radius ≈ 16.7%)
- `a:effectLst` → `a:outerShdw blurRad="50800" dist="38100" dir="5400000"` → `a:srgbClr val="000000"` + `a:alpha val="40000"` (40% opacity)

**Features:** preset geometry (`a:prstGeom prst`; rect/roundRect/ellipse/…), corner radius guide (`a:gd`), outer shadow (`a:outerShdw` with blur/distance/direction/opacity)

## Scenario 7: Side-by-Side Textboxes (Dashboard Cards)

Three `wp:anchor` boxes in a single paragraph, positioned horizontally with `wp:positionH/wp:posOffset` and using `wp:wrapNone` so they float without pushing body text.

Positioning:
- Card A: `posOffset=0`
- Card B: `posOffset=1900000` (≈ 1.5 inches right)
- Card C: `posOffset=3800000` (≈ 3 inches right)

**Features:** multiple `wp:anchor` elements in one paragraph (side-by-side layout), `wp:wrapNone` (no text wrap — boxes float freely), `wp:positionH relativeFrom="column"/wp:posOffset` (horizontal absolute positioning), `a:prstGeom prst="roundRect"` (card shape)

## Scenario 8: Borderless Transparent Textbox

A box with `a:noFill` for the background and `a:ln/a:noFill` for the border — completely invisible container, only the text is visible.

```xml
<a:solidFill> → replaced by → <a:noFill/>
<a:ln> → <a:noFill/> </a:ln>
```

**Features:** `a:noFill` (transparent fill), `a:ln/a:noFill` (no border), italic large light-gray text for a watermark-style overlay

## Scenario 9: Text Overflow Textbox

A fixed-height box (`cy="600000"` = 47pt) with content that exceeds the height, testing how Word handles overflow (`wps:bodyPr` has no `spAutoFit` — fixed height).

`wps:bodyPr` attributes in overflow mode:
- No `a:spAutoFit` element → height is fixed
- `anchor="t"` → clip overflow at the bottom

**Features:** fixed-height textbox (no auto-fit), overflow clipping behaviour, `wps:bodyPr anchor="t"`

## Scenario 10: Textbox Z-Order Stacking (behindDoc)

Two overlapping boxes in one paragraph demonstrating Z-order via `relativeHeight` and the `behindDoc` attribute:
- **Bottom layer:** `behindDoc="1"` — the box sits behind the document body text.
- **Top layer:** `behindDoc="0"` + `a:alpha val="80000"` — 80% opacity (semi-transparent red fill).

```xml
<!-- Bottom (behind doc) -->
<wp:anchor relativeHeight="251670528" behindDoc="1" ...>

<!-- Top (in front, semi-transparent) -->
<wp:anchor relativeHeight="251671552" behindDoc="0" ...>
  <a:solidFill>
    <a:srgbClr val="FFCDD2"><a:alpha val="80000"/></a:srgbClr>
  </a:solidFill>
```

**Features:** `wp:anchor behindDoc` (0 = in front, 1 = behind body), `wp:anchor relativeHeight` (Z-order; higher number = front), `a:alpha val` (per-color transparency; 100000 = opaque, 0 = fully transparent), overlapping anchor positioning

## Complete Feature Coverage

| Feature | Scenario |
|---------|---------|
| Solid fill (`a:solidFill`) | 1, 3, 7, 8, 10 |
| Gradient fill (`a:gradFill`/`a:gsLst`) | 4 |
| No fill (`a:noFill`) | 8 |
| Border width (`a:ln w`) + solid color | 1, 2, 3, 6, 7 |
| Dashed border (`a:prstDash`) | 2 |
| No border (`a:ln/a:noFill`) | 8 |
| Preset geometry: `rect` | 1, 8, 9, 10 |
| Preset geometry: `roundRect` + corner radius | 6, 7 |
| Shape rotation (`a:xfrm rot`) | 4 |
| Drop shadow (`a:outerShdw`) | 6 |
| Color transparency (`a:alpha`) | 10 |
| Vertical text (`wps:bodyPr vert="eaVert"`) | 5 |
| Body text anchor (`anchor="t"/"ctr"`) | 1, 4, 6, 9 |
| Text wrap: `wp:wrapTopAndBottom` | 1, 2, 3, 4, 5, 6 |
| Text wrap: `wp:wrapNone` (float freely) | 7, 10 (bottom) |
| Z-order: `relativeHeight`, `behindDoc` | 10 |
| Horizontal positioning: `wp:positionH/posOffset` | 7 |
| Nested table in textbox | 3 |
| Rich mixed-format content (`w:rPr` variants) | 2, 3 |
| VML fallback (`mc:Fallback` / `v:shape`) | 1 |
| `mc:AlternateContent`/`mc:Choice Requires="wps"` | All |

## Inspect the Generated File

```bash
# View the document outline (headings and textbox scenario labels)
officecli view textbox.docx outline

# Query all drawing anchors (each textbox is a wp:anchor drawing)
officecli query textbox.docx drawing

# Validate the generated file
officecli validate textbox.docx
```
