# Numbering & List Showcase

End-to-end demo of the docx numbering API — `abstractNum` definitions, `num` instances, and paragraph `numPr` references. Three files:

- **numbering.sh** — builds the document with `officecli` (341 lines, ~60 commands).
- **numbering.docx** — generated output with 8 sections and 5 distinct abstractNum definitions.
- **numbering.md** — this file.

## Regenerate

```bash
cd examples/word
bash numbering.sh
# → numbering.docx
```

## Section 1: Three-Level Custom Numbered List

`abstractNum` with fully customized marker styling on 3 levels (decimal/lowerLetter/lowerRoman), then a `num` instance referencing it.

```bash
# Create the abstractNum definition with id=100
officecli add numbering.docx /numbering --type abstractNum \
  --prop id=100 \
  --prop "name=ShowcaseMultilevel" \
  --prop type=hybridMultilevel \
  --prop "level0.format=decimal" --prop "level0.text=%1." \
  --prop "level0.indent=720" --prop "level0.hanging=360" \
  --prop "level0.justification=left" --prop "level0.suff=tab" \
  --prop "level0.color=C00000" --prop "level0.bold=true" --prop "level0.size=14" \
  --prop "level1.format=lowerLetter" --prop "level1.text=%2)" \
  --prop "level1.indent=1440" --prop "level1.hanging=360" \
  --prop "level1.color=2E74B5" --prop "level1.italic=true" \
  --prop "level2.format=lowerRoman" --prop "level2.text=%3." \
  --prop "level2.indent=2160" --prop "level2.hanging=360" \
  --prop "level2.color=666666"

# Create a num instance pointing at abstractNum #100; capture the assigned id
NUMID_A=$(officecli add numbering.docx /numbering --type num --prop abstractNumId=100 \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

# Paragraphs at various indent levels
officecli add numbering.docx /body --type paragraph \
  --prop "text=Project Phoenix kickoff agenda" \
  --prop "numId=$NUMID_A" --prop ilvl=0
officecli add numbering.docx /body --type paragraph \
  --prop "text=Stakeholder alignment" \
  --prop "numId=$NUMID_A" --prop ilvl=1
officecli add numbering.docx /body --type paragraph \
  --prop "text=identify decision makers" \
  --prop "numId=$NUMID_A" --prop ilvl=2
```

**Features:** `id` (abstractNum identifier), `name` (label shown in Word's Numbering dialog), `type` (hybridMultilevel/multilevel/singleLevel), `level<N>.format` (decimal/lowerLetter/lowerRoman/upperLetter/upperRoman/bullet/…), `level<N>.text` (%N inserts level counter), `level<N>.indent` (left margin in twips), `level<N>.hanging` (hanging indent in twips), `level<N>.justification` (left/center/right), `level<N>.suff` (tab/space/nothing), `level<N>.color`, `level<N>.bold`, `level<N>.italic`, `level<N>.size`, `abstractNumId` (num→abstractNum link), `ilvl` (indent level, 0-based alias for `numLevel`)

## Section 2: Independent Counters vs. Continuation

Two `num` instances on the same `abstractNum` — by default each gets an auto-injected `startOverride.0=1`, giving independent counters. A third instance with `continue=true` opts into Word's literal counter continuation.

```bash
# Independent counter (default: auto startOverride injected)
NUMID_B=$(officecli add numbering.docx /numbering --type num \
  --prop abstractNumId=100 \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

# Word-style continuation — no startOverride injected
NUMID_CONT=$(officecli add numbering.docx /numbering --type num \
  --prop abstractNumId=100 --prop continue=true \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

officecli add numbering.docx /body --type paragraph \
  --prop "text=List B starts fresh at 1 (default behavior)" \
  --prop "numId=$NUMID_B" --prop ilvl=0
officecli add numbering.docx /body --type paragraph \
  --prop "text=List C continues from List A's count (continue=true)" \
  --prop "numId=$NUMID_CONT" --prop ilvl=0
```

**Features:** `continue` (true = do not inject `startOverride`; false/absent = inject `startOverride.0=1` so counter is fresh), multiple `num` instances sharing one `abstractNum`

## Section 3: Restart Numbering with startOverride

`num` with an explicit `start` forces a `lvlOverride.startOverride` at level 0, allowing a list to begin at any number.

```bash
NUMID_C=$(officecli add numbering.docx /numbering --type num \
  --prop abstractNumId=100 --prop start=100 \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

officecli add numbering.docx /body --type paragraph \
  --prop "text=Numbered starting from 100" \
  --prop "numId=$NUMID_C" --prop ilvl=0
officecli add numbering.docx /body --type paragraph \
  --prop "text=Continues from 101" \
  --prop "numId=$NUMID_C" --prop ilvl=0
```

**Features:** `start` (on `num` add: injects `lvlOverride.startOverride` at level 0; separate from `abstractNum level<N>.start`)

## Section 4: Custom-Styled Bullet List

`abstractNum` with Unicode bullet glyphs and per-level glyph colors, sizes, and fonts.

```bash
officecli add numbering.docx /numbering --type abstractNum \
  --prop id=200 --prop "name=StarBullet" --prop type=hybridMultilevel \
  --prop "level0.format=bullet" --prop "level0.text=★" \
  --prop "level0.color=E8B003" --prop "level0.size=12" \
  --prop "level1.format=bullet" --prop "level1.text=▶" \
  --prop "level1.font=Arial" \
  --prop "level1.color=2E74B5" --prop "level1.indent=1440" \
  --prop "level2.format=bullet" --prop "level2.text=●" \
  --prop "level2.color=70AD47" --prop "level2.indent=2160"

NUMID_BULLET=$(officecli add numbering.docx /numbering --type num \
  --prop abstractNumId=200 \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

officecli add numbering.docx /body --type paragraph \
  --prop "text=Top-level milestone" \
  --prop "numId=$NUMID_BULLET" --prop ilvl=0
officecli add numbering.docx /body --type paragraph \
  --prop "text=Sub-milestone with deliverable" \
  --prop "numId=$NUMID_BULLET" --prop ilvl=1
officecli add numbering.docx /body --type paragraph \
  --prop "text=Nitty-gritty detail" \
  --prop "numId=$NUMID_BULLET" --prop ilvl=2
```

**Features:** `level<N>.format=bullet` (marker is a literal glyph, not a counter), `level<N>.text` (Unicode character as glyph; e.g. ★ ▶ ●), `level<N>.font` (font containing the glyph)

## Section 5: Mode A — num Auto-Creates abstractNum

When a `num` add specifies `level<N>.*` props directly (with no `abstractNumId`), the handler creates a matching `abstractNum` on the fly and links the new `num` to it.

```bash
NUMID_AUTO=$(officecli add numbering.docx /numbering --type num \
  --prop "level0.format=upperRoman" --prop "level0.text=%1." \
  --prop "level0.indent=720" --prop "level0.size=12" \
  --prop "level0.color=7030A0" --prop "level0.bold=true" \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

officecli add numbering.docx /body --type paragraph \
  --prop "text=The first part of the proposal" \
  --prop "numId=$NUMID_AUTO" --prop ilvl=0
```

**Features:** Mode A — `num` add with `level<N>.*` props and no `abstractNumId` auto-creates a fresh `abstractNum`; output path contains the newly assigned `@id`

## Section 6: Style-Borne Numbering

A paragraph style holds the `numPr` reference. Paragraphs inherit numbering by applying the style — no `numId` needed on the paragraph itself.

```bash
# Dedicated abstractNum + num for this style
officecli add numbering.docx /numbering --type abstractNum \
  --prop id=300 --prop "name=StyleBorne" \
  --prop "level0.format=decimalZero" --prop "level0.text=%1." \
  --prop "level0.indent=720" --prop "level0.color=C00000"

NUMID_STYLE=$(officecli add numbering.docx /numbering --type num \
  --prop abstractNumId=300 \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

# Paragraph style that owns the numPr
officecli add numbering.docx /styles --type style \
  --prop id=ShowcaseListItem \
  --prop "name=Showcase List Item" \
  --prop type=paragraph --prop basedOn=Normal \
  --prop "numId=$NUMID_STYLE" --prop ilvl=0

# Paragraphs reference only the style — no direct numId
officecli add numbering.docx /body --type paragraph \
  --prop "text=Inherits numbering through style" \
  --prop style=ShowcaseListItem
```

**Features:** `style` add on `/styles` (id/name/type/basedOn/numId/ilvl), style-borne `numPr` (paragraphs inherit numbering from paragraph style without carrying their own `numId`)

## Section 7: Modify abstractNum After Creation

`set` on `/numbering/abstractNum[@id=N]/level[L]` updates an existing level's properties after the `abstractNum` was created.

```bash
# Override level 3 with new format, label, color, and size
officecli set numbering.docx '/numbering/abstractNum[@id=100]/level[3]' \
  --prop format=decimal --prop "text=Step %4 ⇒" \
  --prop color=70AD47 --prop bold=true --prop size=12

# New num instance to exercise the modified level
NUMID_DEEP=$(officecli add numbering.docx /numbering --type num \
  --prop abstractNumId=100 \
  | sed -n 's|.*@id=\([0-9]*\)\].*|\1|p')

officecli add numbering.docx /body --type paragraph \
  --prop "text=Deepest step (modified after creation)" \
  --prop "numId=$NUMID_DEEP" --prop ilvl=3
```

**Features:** `set` on `/numbering/abstractNum[@id=N]/level[L]` (modify level format/text/color/bold/size after creation)

## Section 8: styleLink, numStyleLink, level.start, direction, isLgl, lvlRestart

Covers the remaining `abstractNum` Add-only keys and the `Set`-only level flags not shown earlier.

```bash
officecli add numbering.docx /numbering --type abstractNum \
  --prop id=400 --prop "name=CoverageAbs" --prop type=multilevel \
  --prop "styleLink=CoverageStyle" \
  --prop "numStyleLink=OutlineRef" \
  --prop "level0.format=decimal" --prop "level0.text=%1." --prop "level0.start=1" \
  --prop "level1.format=lowerLetter" --prop "level1.text=%2)" --prop "level1.start=3" \
  --prop "level2.format=lowerRoman" --prop "level2.text=%3." --prop "level2.start=5"

# Set-only flags: direction, isLgl, lvlRestart
officecli set numbering.docx '/numbering/abstractNum[@id=400]/level[1]' \
  --prop direction=rtl
officecli set numbering.docx '/numbering/abstractNum[@id=400]/level[2]' \
  --prop isLgl=true --prop lvlRestart=0
```

**Features:** `styleLink` (back-reference style name for `w:styleLink`), `numStyleLink` (link to another abstractNum via numbering style; `w:numStyleLink`), `level<N>.start` (per-level starting counter), `direction=rtl` (writes `w:bidi` on the level's `pPr`), `isLgl` (render counter as decimal regardless of numFmt — legal numbering style), `lvlRestart` (0 = never restart this counter automatically)

## Complete Feature Coverage

| Feature | Section |
|---------|---------|
| `abstractNum` with `id`, `name`, `type` | 1, 4, 5, 6, 7, 8 |
| `level<N>.format` (decimal/lowerLetter/lowerRoman/upperRoman/bullet/decimalZero) | 1, 4, 5, 6, 8 |
| `level<N>.text` (`%N` counter substitution + Unicode glyphs) | 1, 4, 5, 8 |
| `level<N>.indent`, `level<N>.hanging`, `level<N>.justification`, `level<N>.suff` | 1 |
| `level<N>.color`, `level<N>.bold`, `level<N>.italic`, `level<N>.size`, `level<N>.font` | 1, 4, 5 |
| `level<N>.start` (per-level starting counter) | 8 |
| `num` with `abstractNumId` (Mode B) | 1, 2, 3, 6, 7, 8 |
| `num` with `level<N>.*` only (Mode A — auto-creates abstractNum) | 5 |
| `num start` (injects `lvlOverride.startOverride`) | 3 |
| `continue=true` (skip startOverride injection) | 2 |
| Independent counters (multiple `num` on same `abstractNum`) | 2 |
| Style-borne `numPr` via `/styles --type style` | 6 |
| `set` on `/numbering/abstractNum[@id=N]/level[L]` | 7, 8 |
| `styleLink`, `numStyleLink` | 8 |
| `direction=rtl`, `isLgl`, `lvlRestart` (Set-only level flags) | 8 |

## Inspect the Generated File

```bash
# List all numbering definitions
officecli query numbering.docx /numbering

# Inspect a specific abstractNum
officecli get numbering.docx '/numbering/abstractNum[@id=100]'

# Inspect a specific level within an abstractNum
officecli get numbering.docx '/numbering/abstractNum[@id=100]/level[1]'

# List all paragraphs that reference a numbered list
officecli query numbering.docx paragraph
```
