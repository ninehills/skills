# Tracked Revisions Showcase

End-to-end demo of the docx revision (track-changes) API, covering every revision element the handler supports. Three files:

- **revisions.sh** — builds the document with `officecli` (482 lines, 8 sections + accept/reject demo).
- **revisions.docx** — generated output; open in Word to inspect all markers in the review pane.
- **revisions.md** — this file.

## Regenerate

```bash
cd examples/word
bash revisions.sh
# → revisions.docx  (accept/reject demo runs on a temp copy; artifact is untouched)
```

## Section 1: Run-Level Edits

Four run-scope revision types: insertion, deletion, implicit format change (any property + `revision.author`), and explicit format change (`revision.type=format`).

```bash
# 1a. w:ins — mark a run as an insertion
officecli set revisions.docx '/body/p[4]/r[1]' \
  --prop revision.type=ins \
  --prop revision.author=Alice \
  --prop revision.date=2026-05-25T10:00:00Z

# 1b. w:del — mark a run as a deletion (text becomes w:delText)
officecli set revisions.docx '/body/p[5]/r[1]' \
  --prop revision.type=del \
  --prop revision.author=Bob \
  --prop revision.date=2026-05-25T10:05:00Z

# 1c. w:rPrChange (implicit) — any font.*/bold change + revision.author captures
#     the previous rPr snapshot inside w:rPrChange
officecli set revisions.docx '/body/p[6]/r[1]' \
  --prop font.color=C00000 \
  --prop bold=true \
  --prop revision.author=Carol \
  --prop revision.date=2026-05-25T10:10:00Z

# 1d. w:rPrChange (explicit) — same path but revision.type=format is stated
officecli set revisions.docx '/body/p[7]/r[1]' \
  --prop revision.type=format \
  --prop italic=true \
  --prop revision.author=Carol \
  --prop revision.date=2026-05-25T10:11:00Z
```

**Features:** `revision.type` (ins/del/format), `revision.author` (any string; `""` falls back to `"OfficeCLI"`), `revision.date` (ISO-8601; omitted → UTC now), implicit format change (property change + `revision.author` without explicit `revision.type`)

## Section 2: Paragraph-Level Edits

Whole-paragraph insertion, tracked deletion (`remove` + `revision.author`), and paragraph property change (`pPrChange`).

```bash
# 2a. w:ins + paragraphMarkInsertion — entire paragraph tracked as inserted
officecli add revisions.docx /body --type paragraph \
  --prop text="This whole paragraph was inserted by Alice as a tracked change." \
  --prop revision.author=Alice \
  --prop revision.date=2026-05-25T10:15:00Z

# 2b. w:del + paragraphMarkDeletion — remove keeps the element (wraps it in w:del)
officecli add revisions.docx /body --type paragraph \
  --prop text="This whole paragraph will be tracked-deleted by Bob."
officecli remove revisions.docx '/body/p[10]' \
  --prop revision.author=Bob \
  --prop revision.date=2026-05-25T10:20:00Z

# 2c. w:pPrChange — set a paragraph-level property + revision.author
officecli set revisions.docx '/body/p[11]' \
  --prop align=center \
  --prop revision.author=Carol \
  --prop revision.date=2026-05-25T10:21:00Z
```

**Features:** `add paragraph + revision.author` (tracked insertion), `remove + revision.author` (tracked deletion — element stays in DOM, wrapped in `w:del`), `set paragraph-prop + revision.author` (writes `w:pPrChange`)

## Section 3: Paired Move (moveFrom + moveTo)

A move pair is two `set` calls on different runs sharing the same `revision.id`. The handler emits `w:moveFrom` / `w:moveTo` wrappers and the corresponding Range markers (`w:moveFromRangeStart/End`, `w:moveToRangeStart/End`).

```bash
# Both halves must share the same revision.id
officecli set revisions.docx '/body/p[13]/r[1]' \
  --prop revision.type=moveFrom \
  --prop revision.author=Alice \
  --prop revision.date=2026-05-25T10:25:00Z \
  --prop revision.id=500

officecli set revisions.docx '/body/p[14]/r[1]' \
  --prop revision.type=moveTo \
  --prop revision.author=Alice \
  --prop revision.date=2026-05-25T10:25:00Z \
  --prop revision.id=500
```

**Features:** `revision.type=moveFrom`, `revision.type=moveTo`, `revision.id` (must be equal for both halves to pair; also acts as the `w:id` attribute on the range markers)

## Section 4: Table-Scope Revisions

All five table revision element types — table, row, cell property changes, plus cell and row insertions/deletions.

```bash
# 4a. w:tblPrChange — table-level property change
officecli set revisions.docx '/body/tbl[1]' \
  --prop style=TableGrid \
  --prop revision.author=Dan \
  --prop revision.date=2026-05-25T10:30:00Z

# 4b. w:trPrChange — row-level property change (row height)
officecli set revisions.docx '/body/tbl[1]/tr[1]' \
  --prop height=600 \
  --prop revision.author=Dan \
  --prop revision.date=2026-05-25T10:31:00Z

# 4c. w:tcPrChange — cell-level property change (shading)
#     Automatically cascades w:tblPrExChange + w:tblGridChange when grid mutates
officecli set revisions.docx '/body/tbl[1]/tr[2]/tc[2]' \
  --prop shd=FFE699 \
  --prop revision.author=Dan \
  --prop revision.date=2026-05-25T10:32:00Z

# 4d. w:tcPr/w:cellIns — add a cell to an existing row as tracked insertion
officecli add revisions.docx '/body/tbl[1]/tr[2]' --type cell \
  --prop text="row2 d (inserted)" \
  --prop revision.author=Eve \
  --prop revision.date=2026-05-25T10:33:00Z

# 4e. w:tcPr/w:cellDel — tracked cell deletion (cell stays in DOM)
officecli remove revisions.docx '/body/tbl[1]/tr[3]/tc[1]' \
  --prop revision.author=Eve \
  --prop revision.date=2026-05-25T10:34:00Z

# 4f. w:trPr/w:ins — append a row with tracked row-insertion marker
officecli add revisions.docx '/body/tbl[1]' --type row \
  --prop revision.author=Eve \
  --prop revision.date=2026-05-25T10:35:00Z
```

**Features:** `tblPrChange` (set table style + revision.author), `trPrChange` (set row height + revision.author), `tcPrChange` (set cell shading + revision.author, cascades `tblPrExChange`/`tblGridChange`), `cellInsertion` (add cell + revision.author), `cellDeletion` (remove cell + revision.author), `rowInsertion` (add row + revision.author)

## Section 5: Section Properties (sectPrChange)

Track a change to the body's section properties (page width, margins, orientation).

```bash
# The body's sectPr path is /body/sectPr[N] — NOT /section[N]
officecli set revisions.docx '/body/sectPr[1]' \
  --prop pageWidth=11906 \
  --prop revision.author=Frank \
  --prop revision.date=2026-05-25T10:40:00Z
```

**Features:** `set /body/sectPr[N] + revision.author` (writes `w:sectPrChange`), any `sectPr` property (pageWidth/pageHeight/margin.*/orientation) triggers the snapshot

> The body's final section path is **`/body/sectPr[N]`**, not `/section[N]`. Mid-document sections live as `pPr/sectPr` children of their preceding paragraph.

## Section 6: Defaults & Explicit-id

Demonstrate the empty-author fallback and a deterministic `revision.id`.

```bash
# 6a. Empty author on 'set' path falls back to "OfficeCLI"
#     (on 'add', empty author means "do not track"; use 'set' for fallback)
officecli set revisions.docx '/body/p[19]/r[1]' \
  --prop revision.type=ins \
  --prop revision.author="" \
  --prop revision.date=2026-05-25T10:44:00Z

# 6b. Explicit revision.id outside a move pair (deterministic; useful for post-processing)
officecli add revisions.docx /body \
  --type paragraph \
  --prop text="This paragraph carries an explicit revision.id=9001." \
  --prop revision.author=Grace \
  --prop revision.date=2026-05-25T10:45:00Z \
  --prop revision.id=9001
```

**Features:** `revision.author=""` (on `set`: fallback to `"OfficeCLI"`; on `add`: no tracking), `revision.id` (explicit integer; auto-allocated from the paraId pool when omitted)

## Section 7: Find + Revision (Tracked Find & Replace)

Combine `--find` / `--replace` with `revision.*` props — equivalent to Word's "Find & Replace with Track Changes on".

```bash
# 7a. Regex find + replace tracked on first match only
officecli set revisions.docx "$P7A" \
  --find '(?<!fox.*)fox' \
  --prop regex=true \
  --replace cat \
  --prop revision.author=Iris \
  --prop revision.date=2026-05-25T10:50:00Z

# 7b. Find + format (rPrChange per match — text unchanged)
officecli set revisions.docx "$P7B" \
  --find red \
  --prop bold=true \
  --prop revision.author=Jack \
  --prop revision.date=2026-05-25T10:51:00Z

# 7c. Find + replace + format (replacement run gets new formatting layered on)
officecli set revisions.docx "$P7C" \
  --find bar \
  --replace BAZ \
  --prop bold=true \
  --prop font.color=00B050 \
  --prop revision.author=Kelly \
  --prop revision.date=2026-05-25T10:52:00Z

# 7d. Regex with multiple matches — each match gets its own marker
officecli set revisions.docx "$P7D" \
  --find '\$\d+' \
  --prop regex=true \
  --prop bold=true \
  --prop revision.author=Liam \
  --prop revision.date=2026-05-25T10:53:00Z
```

**Features:** `--find` + `--replace` + `revision.*` (tracked Find & Replace — emits `w:del`+`w:ins` pair per match), `--find` + property change + `revision.*` (tracked format-only find — emits `w:rPrChange` per match), `--prop regex=true` (treat `--find` value as .NET regex)

## Section 8: Find Variants

Pure tracked deletion via `replace=""`, and paragraph-scope `pPrChange` via find.

```bash
# 8a. Tracked deletion only (no replacement insertion)
officecli set revisions.docx "$P8A" \
  --find OBSOLETE \
  --replace "" \
  --prop revision.author=Mira \
  --prop revision.date=2026-05-25T10:54:00Z

# 8b. Paragraph property change tracked for matching paragraphs
officecli set revisions.docx "$P8B" \
  --find MARK \
  --prop align=center \
  --prop revision.author=Nora \
  --prop revision.date=2026-05-25T10:55:00Z
```

**Features:** `--find` + `--replace ""` (delete-only: `w:del` per match, no `w:ins`), `--find` + paragraph prop + `revision.*` (`w:pPrChange` on each paragraph whose text matched)

## Accept / Reject Syntax

All addressing forms demonstrated in the script's temp-copy demo:

```bash
# By scope — all markers
officecli set revisions.docx /revision --prop revision.action=accept
officecli set revisions.docx /revision --prop revision.action=reject

# By author / type
officecli set revisions.docx '/revision[@author=Alice]' --prop revision.action=accept
officecli set revisions.docx '/revision[@type=del]'     --prop revision.action=reject

# By stable id (preferred over positional; ids survive accept/reject)
officecli set revisions.docx '/revision[@id=42]'                --prop revision.action=accept

# Single-end of a move pair (only affects one half)
officecli set revisions.docx '/revision[@id=500][@type=moveTo]' --prop revision.action=reject

# Via native DOM path (from Format["revision.nativePath"] in query output)
officecli set revisions.docx '/body/p[2]/ins[1]'         --prop revision.action=accept
officecli set revisions.docx '/body/tbl[1]/tr[2]/tc[2]'  --prop revision.action=accept
```

**Features:** `revision.action` (accept/reject), `/revision` selector (all), `[@author=]`, `[@type=]`, `[@id=]`, `[@id=][@type=]` (single-end move), native DOM path as target

## Complete Feature Coverage

| Revision Element | OOXML | Section |
|-----------------|-------|---------|
| Run insertion | `w:ins` | 1a |
| Run deletion | `w:del` | 1b |
| Run format change (implicit) | `w:rPrChange` | 1c |
| Run format change (explicit) | `w:rPrChange` | 1d |
| Paragraph insertion | `w:ins` + `paragraphMarkInsertion` | 2a |
| Paragraph deletion | `w:del` + `paragraphMarkDeletion` | 2b |
| Paragraph property change | `w:pPrChange` | 2c |
| Move from (source) | `w:moveFrom` + range markers | 3 |
| Move to (destination) | `w:moveTo` + range markers | 3 |
| Table property change | `w:tblPrChange` | 4a |
| Row property change | `w:trPrChange` | 4b |
| Cell property change | `w:tcPrChange` (+cascades) | 4c |
| Cell insertion | `w:cellIns` | 4d |
| Cell deletion | `w:cellDel` | 4e |
| Row insertion | `w:trPr/w:ins` | 4f |
| Section property change | `w:sectPrChange` | 5 |
| Default author fallback | `revision.author=""` → `"OfficeCLI"` | 6a |
| Explicit revision.id | deterministic `w:id` attribute | 6b |
| Tracked find + replace | `w:del` + `w:ins` per match | 7 |
| Tracked find + format | `w:rPrChange` per match | 7b, 7d |
| Tracked delete-only (replace="") | `w:del` per match | 8a |
| Tracked paragraph pPrChange via find | `w:pPrChange` per matching para | 8b |

## Inspect the Generated File

```bash
# List every revision marker in the document
officecli query revisions.docx revision

# Get all revisions as structured JSON (agent-friendly)
officecli query revisions.docx revision --json

# Inspect a specific revision by stable id
officecli get revisions.docx '/revision[@id=500]'

# Check what revisions exist by a particular author
officecli query revisions.docx '/revision[@author=Alice]'
```
