# Paragraph Formatting Showcase

Exercises the docx **paragraph** property surface. Three files:

- **paragraph-formatting.sh** — builds the document with `officecli` (155 lines, ~55 commands).
- **paragraph-formatting.docx** — generated output, one paragraph per property group.
- **paragraph-formatting.md** — this file.

## Regenerate

```bash
cd examples/word
bash paragraph-formatting.sh
# → paragraph-formatting.docx
```

## Alignment

The four standard paragraph alignment values.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Left aligned (default)" --prop align=left
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Center aligned" --prop align=center
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Right aligned" --prop align=right
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Justified text stretched edge to edge..." --prop align=both
```

**Features:** `align` (left/center/right/both/distribute/thai/mediumKashida/highKashida/lowKashida)

## Indentation

Left, right, first-line, and hanging indent — all accepting twips, cm, or in.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Left indent 1cm" --prop indent=1cm
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Right indent 2cm..." --prop rightIndent=2cm
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=First-line indent — only the first line is pushed in." \
  --prop firstLineIndent=1cm
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Hanging indent — first line hangs left." \
  --prop indent=1cm --prop hangingIndent=1cm
```

**Features:** `indent` (left indent; twips, cm, in, pt), `rightIndent`, `firstLineIndent` (positive → indent first line extra), `hangingIndent` (positive → all-but-first lines indented; combine with `indent` of equal value)

## Spacing

Space before/after in points, and line spacing as a multiplier or fixed value.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Space before 18pt, after 6pt" \
  --prop spaceBefore=18pt --prop spaceAfter=6pt
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Line spacing 1.5x across a longer paragraph..." \
  --prop lineSpacing=1.5x
```

**Features:** `spaceBefore` (space above; accepts `18pt`, `0.5cm`, `360` twips), `spaceAfter`, `lineSpacing` (1.5x/150%/18pt/bare number; normalised via `SpacingConverter`)

## Pagination Flags

Control how Word breaks pages around this paragraph.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=keepNext — stays with the following paragraph" \
  --prop keepNext=true
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=keepLines — lines stay together, never split across pages" \
  --prop keepLines=true
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=widowControl on" --prop widowControl=true
```

**Features:** `keepNext` (force this paragraph and the next onto the same page), `keepLines` (prevent page break within the paragraph), `widowControl` (prevent single-line orphans/widows)

## Paragraph-Level Run Formatting

Properties set at the paragraph level apply to **every run** in the paragraph (equivalent to selecting all and formatting).

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Whole paragraph bold + red + 13pt" \
  --prop bold=true --prop color=C00000 --prop size=13
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Whole paragraph italic + highlighted" \
  --prop italic=true --prop highlight=yellow
```

**Features:** `bold`, `italic`, `color`, `size`, `highlight` — when set on `--type paragraph` these become paragraph-level run defaults (`w:pPr/w:rPr`), not inline run properties.

## Shading

Paragraph background fill color, with optional pattern.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Light gray paragraph shading" --prop shading.fill=D9D9D9
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Pale blue shading" --prop shading.fill=DDEBF7
```

**Features:** `shading.fill` (solid fill hex color; writer defaults `w:shd/@val` to `clear`), `shd` (shorthand alias for `shading.fill`), `shading.val` (pattern type: pct15/pct25/pct50/…), `shading.color` (pattern foreground color)

## Paragraph-Mark Formatting (markRPr)

The paragraph mark (¶ pilcrow) has its own run properties, distinct from the paragraph's run defaults. This controls what formatting a newly typed run at the end of the paragraph inherits.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=The mark glyph is bold+red (distinct from run text)" \
  --prop markRPr.bold=true --prop markRPr.color=C00000

# Full markRPr set
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=mark: italic/strike/underline/size/highlight/fonts" \
  --prop markRPr.italic=true --prop markRPr.strike=true \
  --prop markRPr.underline=single --prop markRPr.size=14pt \
  --prop markRPr.highlight=yellow \
  --prop markRPr.font.latin=Georgia \
  --prop markRPr.font.ea=SimSun --prop markRPr.font.cs=Arial
```

**Features:** `markRPr.bold`, `markRPr.italic`, `markRPr.strike`, `markRPr.underline`, `markRPr.size`, `markRPr.color`, `markRPr.highlight`, `markRPr.font.latin`, `markRPr.font.ea`, `markRPr.font.cs`

> Setting `bold=true` on a paragraph makes every run bold. Setting `markRPr.bold=true` formats only the ¶ mark — they are independent and both settable/gettable.

## Outline Level

Assign a paragraph to the document outline (affects navigation pane, table of contents, heading styles).

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Outline level 1 (shows in document map)" --prop outlineLvl=1
```

**Features:** `outlineLvl` (0–8; 0 = body text, 1–8 = heading levels; 9 = no outline level)

## Paragraph Strike & Underline

Strike-through and underline can also be applied at the paragraph level (to all runs).

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Whole paragraph struck out" --prop strike=true
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Underlined paragraph (red wave)" \
  --prop underline=wave --prop underline.color=#FF0000
```

**Features:** `strike` (paragraph-level single strikethrough), `underline` (paragraph-level underline style), `underline.color` (accepts leading `#` — stripped before storage)

## Complex-Script (cs) Properties

Separate formatting for complex-script (bidirectional, Arabic, Hebrew) glyph runs.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=cs bold/italic/14pt + RTL" \
  --prop bold.cs=true --prop italic.cs=true \
  --prop size.cs=14pt --prop direction=rtl
```

**Features:** `bold.cs`, `italic.cs`, `size.cs` (complex-script weight/style/size), `direction` (rtl sets `w:bidi` on the paragraph)

## Spacing & Pagination Extras

Additional spacing and pagination controls beyond the basic set.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=contextualSpacing (collapse between same-style paras)" \
  --prop contextualSpacing=true
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=lineSpacing 14pt, lineRule=atLeast" \
  --prop lineSpacing=14pt --prop lineRule=atLeast
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=pageBreakBefore" --prop pageBreakBefore=true
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=wordWrap off (break long URLs anywhere)" --prop wordWrap=false
```

**Features:** `contextualSpacing` (suppress space-before/after between paragraphs of the same style), `lineRule` (atLeast/exactly/auto), `pageBreakBefore` (force a page break before this paragraph), `wordWrap` (false = break at any character, not just word boundaries)

## Chars-Based Indent

CJK-friendly indentation in 1/100-character units (avoids relying on absolute twip values).

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=first-line 200 chars, hanging 100 chars" \
  --prop firstLineChars=200 --prop hangingChars=100
```

**Features:** `firstLineChars` (first-line indent in 1/100-char units; 200 = 2 characters), `hangingChars`

## Fonts (Explicit & Theme)

Per-script font families and theme font slots.

```bash
# Shorthand — sets all scripts
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=font shorthand Times New Roman" \
  --prop font="Times New Roman"

# Explicit per-script
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=per-script latin/ea/cs" \
  --prop font.latin=Calibri --prop font.ea=SimSun --prop font.cs=Arial

# Theme references
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=theme fonts" \
  --prop font.asciiTheme=minorHAnsi --prop font.hAnsiTheme=minorHAnsi \
  --prop font.eaTheme=minorEastAsia --prop font.csTheme=minorBidi
```

**Features:** `font` (all-scripts shorthand), `font.latin`, `font.ea`, `font.cs`, `font.asciiTheme` (majorHAnsi/minorHAnsi), `font.hAnsiTheme`, `font.eaTheme`, `font.csTheme`

## Styles

Apply a named paragraph style or a character style to all runs.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Paragraph style Heading1" --prop style=Heading1
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Character style on the run" --prop rStyle=Emphasis
```

**Features:** `style` (paragraph style ID; e.g. `Heading1`, `Normal`, `ListBullet`), `rStyle` (character style ID applied to the paragraph's default run properties)

## Shading Variants

Different ways to express paragraph background shading.

```bash
# Shorthand alias
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=shd shorthand (yellow)" --prop shd=FFFF00

# Decomposed: pattern + fill color + pattern foreground color
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=pct15 pattern, blue fill, red pattern color" \
  --prop shading.val=pct15 --prop shading.fill=DDEBF7 --prop shading.color=C00000
```

**Features:** `shd` (shorthand alias for `shading.fill`), `shading.val` (pattern: clear/pct5/pct10/pct15/pct25/pct50/solid/…), `shading.fill` (fill color hex), `shading.color` (pattern foreground hex)

## Tab Stops

Declare tab stop positions in twips.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Tabs at 720 and 1440 twips" --prop tabs=720,1440
```

**Features:** `tabs` (comma-separated list of tab positions in twips; 720 = 0.5in, 1440 = 1in)

## Text Frame (framePr)

A framed paragraph floats in its own positioned box, with body text optionally wrapping around it. Dimensions use twips.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Framed paragraph — floats in a 3-inch box with text wrapping." \
  --prop framePr.w=4320 --prop framePr.h=720 --prop framePr.wrap=around \
  --prop framePr.hAnchor=margin --prop framePr.vAnchor=text \
  --prop framePr.hSpace=180 --prop framePr.vSpace=180
```

**Features:** `framePr.w` (frame width in twips; 4320 = 3in), `framePr.h` (height), `framePr.wrap` (around/notBeside/none/tight/through), `framePr.hAnchor` (margin/page/text), `framePr.vAnchor`, `framePr.hSpace` (horizontal clearance), `framePr.vSpace`

## Paragraph Borders (pBdr)

Whole-box border shorthand, accepting `style`, `style;size;color`, or `style;size;color;space`.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Box border, all sides (single)" --prop border=single
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Red 1pt box (style;size;color)" \
  --prop "border=single;8;FF0000"
```

**Features:** `border` (whole-box paragraph border; format `style` or `style;size;color` or `style;size;color;space`; style values: single/double/thick/dotted/dashed/dotDash/…)

## Vertical Text Alignment

Control how glyphs align vertically within the line box (distinct from cell vertical alignment).

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=textAlignment=center (glyphs centered on the line box)" \
  --prop textAlignment=center
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=textAlignment=top" --prop textAlignment=top
```

**Features:** `textAlignment` (top/center/baseline/bottom/auto)

## EastAsian Typography

CJK line-breaking and spacing rules.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=kinsoku off — permit breaks at forbidden CJK chars" \
  --prop kinsoku=false
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=autoSpace off — no auto gap between CJK and Latin/digits" \
  --prop autoSpaceDE=false --prop autoSpaceDN=false
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=overflowPunct + topLinePunct on" \
  --prop overflowPunct=true --prop topLinePunct=true
```

**Features:** `kinsoku` (Japanese line-break constraint; false = allow breaks at forbidden positions), `autoSpaceDE` (auto spacing between CJK and Latin), `autoSpaceDN` (auto spacing between CJK and digits), `overflowPunct` (allow punctuation to hang outside margin), `topLinePunct` (compress leading punctuation to top of line)

## Line & Indent Flags

Miscellaneous line-number, hyphenation, and indent behaviour toggles.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=suppressLineNumbers + suppressAutoHyphens" \
  --prop suppressLineNumbers=true --prop suppressAutoHyphens=true
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=mirrorIndents on, adjustRightInd off, snapToGrid off" \
  --prop mirrorIndents=true --prop adjustRightInd=false --prop snapToGrid=false
```

**Features:** `suppressLineNumbers` (exclude from line number count), `suppressAutoHyphens` (disable hyphenation), `mirrorIndents` (swap left/right indent on alternate pages for book layout), `adjustRightInd` (auto-adjust right indent for document grid), `snapToGrid` (snap paragraph to the document character grid)

## Web / Textbox Hints

Layout hints for web view and textbox flow.

```bash
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=divId (web division id) + textboxTightWrap=allLines" \
  --prop divId=123456 --prop textboxTightWrap=allLines
```

**Features:** `divId` (integer web-division ID for HTML round-trip), `textboxTightWrap` (none/allLines/firstAndLastLine/firstLineOnly/lastLineOnly)

## List Numbering

Attach a paragraph to a list via a high-level style shorthand or direct numPr references.

```bash
# High-level shorthand
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Bulleted item" --prop listStyle=bullet
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Ordered item starting at 5" \
  --prop listStyle=ordered --prop start=5

# Direct numPr reference
officecli add paragraph-formatting.docx /body --type paragraph \
  --prop "text=Explicit numId=1 level 0" --prop numId=1 --prop numLevel=0
```

**Features:** `listStyle` (bullet/ordered — creates or reuses a numbering definition automatically), `start` (override starting counter for this instance), `numId` (direct reference to a `w:num` element by id), `numLevel` (alias: `ilvl`; 0-based indent level)

## Complete Feature Coverage

| Feature | Section |
|---------|---------|
| `align` (left/center/right/both) | Alignment |
| `indent`, `rightIndent`, `firstLineIndent`, `hangingIndent` | Indentation |
| `spaceBefore`, `spaceAfter`, `lineSpacing` | Spacing |
| `keepNext`, `keepLines`, `widowControl` | Pagination Flags |
| `bold`, `italic`, `color`, `size`, `highlight` (paragraph-level) | Paragraph-Level Run Formatting |
| `shading.fill`, `shd` | Shading |
| `markRPr.*` (full set) | Paragraph-Mark Formatting |
| `outlineLvl` | Outline Level |
| `strike`, `underline`, `underline.color` (paragraph-level) | Paragraph Strike & Underline |
| `bold.cs`, `italic.cs`, `size.cs`, `direction` | Complex-Script |
| `contextualSpacing`, `lineRule`, `pageBreakBefore`, `wordWrap` | Spacing & Pagination Extras |
| `firstLineChars`, `hangingChars` | Chars-Based Indent |
| `font`, `font.latin/ea/cs`, theme fonts | Fonts |
| `style`, `rStyle` | Styles |
| `shading.val`, `shading.fill`, `shading.color` | Shading Variants |
| `tabs` | Tab Stops |
| `framePr.*` (w/h/wrap/hAnchor/vAnchor/hSpace/vSpace) | Text Frame |
| `border` (whole-box pBdr) | Paragraph Borders |
| `textAlignment` | Vertical Text Alignment |
| `kinsoku`, `autoSpaceDE`, `autoSpaceDN`, `overflowPunct`, `topLinePunct` | EastAsian Typography |
| `suppressLineNumbers`, `suppressAutoHyphens`, `mirrorIndents`, `adjustRightInd`, `snapToGrid` | Line & Indent Flags |
| `divId`, `textboxTightWrap` | Web / Textbox Hints |
| `listStyle`, `start`, `numId`, `numLevel` | List Numbering |

## Inspect the Generated File

```bash
# List every paragraph path
officecli query paragraph-formatting.docx paragraph

# Inspect a paragraph's full property set
officecli get paragraph-formatting.docx "/body/p[5]"

# Check the paragraph-mark formatting of a specific paragraph
officecli get paragraph-formatting.docx "/body/p[20]"

# Find all paragraphs with a shading fill set
officecli query paragraph-formatting.docx paragraph --find shading.fill
```
