#!/bin/bash
# Generate a Word tracked-revision showcase document covering every
# revision element the docx handler supports on this branch.
#
# Marker creation (covered):
#   Run scope:
#     * w:ins                      set + revision.type=ins
#     * w:del                      set + revision.type=del
#     * w:rPrChange (implicit)     set + font.* / bold / ... + revision.author
#     * w:rPrChange (explicit)     set + revision.type=format + <prop change>
#     * w:moveFrom / w:moveTo      set + revision.type=moveFrom|moveTo + shared revision.id
#   Paragraph scope:
#     * w:ins (paragraph)          add paragraph + revision.author
#     * w:del (paragraph)          remove paragraph + revision.author
#     * w:pPrChange                set paragraph prop (align/indent/...) + revision.author
#   Table scope:
#     * w:tblPrChange              set tbl + style/... + revision.author
#     * w:trPrChange               set tr + height/header/... + revision.author
#     * w:tcPrChange               set tc + shd/borders/... + revision.author
#       (implicitly cascades w:tblPrExChange + w:tblGridChange when grid mutates)
#     * w:trPr/w:ins  (rowInsertion)    add row + revision.author
#     * w:tcPr/w:cellIns              add cell + revision.author
#     * w:tcPr/w:cellDel              remove cell + revision.author
#   Section scope:
#     * w:sectPrChange             set /body/sectPr[N] + revision.author
#   Bonus:
#     * Default author (revision.author="" -> "OfficeCLI")
#     * Auto-allocated revision.id (omit; comes from shared paraId pool)
#     * Explicit revision.id (required for move pair; allowed everywhere)
#
# Action verb demo (on a temp copy at the bottom):
#   * /revision[@author=NAME]            accept/reject by author
#   * /revision[@type=ins|del|...]       accept/reject by type
#   * /revision[@id=N]                   accept/reject by stable id
#   * /revision[@id=N][@type=moveTo]     single-end of a move pair
#   * native path (/body/p[N]/ins[M] ...) accept/reject in DOM terms
#   * /revision                          accept-all / reject-all (terminal sweep)

set -e

DIR="$(dirname "$0")"
DOCX="$DIR/revisions.docx"

echo "=========================================="
echo "Generating tracked-revision showcase: $DOCX"
echo "=========================================="

rm -f "$DOCX"
officecli create "$DOCX"
officecli open "$DOCX"

# --------------------------------------------------------------------------
# Paragraph index map (1-based; counts EVERY body paragraph). Comments inline
# below show what each set targets so the indices stay correct as the doc
# grows. Tracked-delete keeps the paragraph element in place (wrapped in
# w:del), so subsequent indices do NOT shift after step 2b.
# --------------------------------------------------------------------------

# p[1] title, p[2] spacer
officecli add "$DOCX" /body --type paragraph --prop text="Revision API — Full Coverage" --prop style=Heading1 --prop align=center
officecli add "$DOCX" /body --type paragraph --prop text=""

# ==========================================================================
# Section 1 — Run-level edits.
#   p[3] H2,  p[4]=ins target,  p[5]=del target,
#   p[6]=implicit-format target,  p[7]=explicit-format target
# ==========================================================================
echo "  -> Section 1: run-level edits (ins / del / rPrChange implicit + explicit)"
officecli add "$DOCX" /body --type paragraph --prop text="1. Run-level edits" --prop style=Heading2
officecli add "$DOCX" /body --type paragraph --prop text="This run will be marked as an INSERTION."
officecli add "$DOCX" /body --type paragraph --prop text="This run will be marked as a DELETION."
officecli add "$DOCX" /body --type paragraph --prop text="This run keeps the text and gets an IMPLICIT format change (font.color)."
officecli add "$DOCX" /body --type paragraph --prop text="This run gets an EXPLICIT revision.type=format with italic toggle."

# 1a. w:ins around the run.
officecli set "$DOCX" '/body/p[4]/r[1]' \
    --prop revision.type=ins \
    --prop revision.author=Alice \
    --prop revision.date=2026-05-25T10:00:00Z

# 1b. w:del around the run (text becomes w:delText).
officecli set "$DOCX" '/body/p[5]/r[1]' \
    --prop revision.type=del \
    --prop revision.author=Bob \
    --prop revision.date=2026-05-25T10:05:00Z

# 1c. Implicit format change — any font.* prop + revision.author captures the
#     previous rPr in w:rPrChange. Most natural form.
officecli set "$DOCX" '/body/p[6]/r[1]' \
    --prop font.color=C00000 \
    --prop bold=true \
    --prop revision.author=Carol \
    --prop revision.date=2026-05-25T10:10:00Z

# 1d. Explicit revision.type=format. Still needs a real property change
#     alongside (empty rPrChange records nothing, so the handler errors out).
officecli set "$DOCX" '/body/p[7]/r[1]' \
    --prop revision.type=format \
    --prop italic=true \
    --prop revision.author=Carol \
    --prop revision.date=2026-05-25T10:11:00Z

# ==========================================================================
# Section 2 — Paragraph-level edits.
#   p[8]  H2
#   p[9]  whole-paragraph tracked insertion (add + revision.author)
#   p[10] plain paragraph that becomes a tracked deletion (remove + ...)
#   p[11] paragraph that gets a pPrChange (set align + revision.author)
# ==========================================================================
echo "  -> Section 2: paragraph-level edits (ins / del / pPrChange)"
officecli add "$DOCX" /body --type paragraph --prop text="2. Paragraph-level edits" --prop style=Heading2

# 2a. w:ins around the entire paragraph (plus paragraphMarkInsertion on the ¶).
officecli add "$DOCX" /body --type paragraph \
    --prop text="This whole paragraph was inserted by Alice as a tracked change." \
    --prop revision.author=Alice \
    --prop revision.date=2026-05-25T10:15:00Z

# 2b. w:del around the entire paragraph (plus paragraphMarkDeletion).
#     remove + revision.author KEEPS the element (wraps it); it does not drop it.
officecli add "$DOCX" /body --type paragraph --prop text="This whole paragraph will be tracked-deleted by Bob."
officecli remove "$DOCX" '/body/p[10]' \
    --prop revision.author=Bob \
    --prop revision.date=2026-05-25T10:20:00Z

# 2c. pPrChange — set a paragraph-level property (alignment here) + revision.author.
#     Surfaces in query as revision.type=paragraph.
officecli add "$DOCX" /body --type paragraph --prop text="This paragraph had alignment changed (pPrChange) by Carol."
officecli set "$DOCX" '/body/p[11]' \
    --prop align=center \
    --prop revision.author=Carol \
    --prop revision.date=2026-05-25T10:21:00Z

# ==========================================================================
# Section 3 — Paired move (shared revision.id binds the two halves).
#   p[12] H2, p[13]=moveFrom source, p[14]=moveTo destination
# ==========================================================================
echo "  -> Section 3: paired move (moveFrom + moveTo, shared id)"
officecli add "$DOCX" /body --type paragraph --prop text="3. Moved content" --prop style=Heading2
officecli add "$DOCX" /body --type paragraph --prop text="Source: this sentence is being relocated."
officecli add "$DOCX" /body --type paragraph --prop text="Destination: it will land here in its new home."

# revision.id MUST be supplied (and equal) for the two halves to pair.
officecli set "$DOCX" '/body/p[13]/r[1]' \
    --prop revision.type=moveFrom \
    --prop revision.author=Alice \
    --prop revision.date=2026-05-25T10:25:00Z \
    --prop revision.id=500
officecli set "$DOCX" '/body/p[14]/r[1]' \
    --prop revision.type=moveTo \
    --prop revision.author=Alice \
    --prop revision.date=2026-05-25T10:25:00Z \
    --prop revision.id=500

# ==========================================================================
# Section 4 — Table-scope revisions (all five table elements).
#   p[15] H2, tbl[1] = 3 rows x 3 cols seed.
#   Order of operations is chosen so per-row/per-cell indices stay correct.
# ==========================================================================
echo "  -> Section 4: table scope (tblPrChange + trPrChange + tcPrChange + row/cell ins/del)"
officecli add "$DOCX" /body --type paragraph --prop text="4. Table-scope revisions" --prop style=Heading2
officecli add "$DOCX" /body --type table --prop rows=3 --prop cols=3

# Seed content
officecli set "$DOCX" '/body/tbl[1]/tr[1]/tc[1]' --prop text="Header A" --prop bold=true
officecli set "$DOCX" '/body/tbl[1]/tr[1]/tc[2]' --prop text="Header B" --prop bold=true
officecli set "$DOCX" '/body/tbl[1]/tr[1]/tc[3]' --prop text="Header C" --prop bold=true
officecli set "$DOCX" '/body/tbl[1]/tr[2]/tc[1]' --prop text="row2 a"
officecli set "$DOCX" '/body/tbl[1]/tr[2]/tc[2]' --prop text="row2 b (shading change)"
officecli set "$DOCX" '/body/tbl[1]/tr[2]/tc[3]' --prop text="row2 c"
officecli set "$DOCX" '/body/tbl[1]/tr[3]/tc[1]' --prop text="row3 a (cell delete)"
officecli set "$DOCX" '/body/tbl[1]/tr[3]/tc[2]' --prop text="row3 b"
officecli set "$DOCX" '/body/tbl[1]/tr[3]/tc[3]' --prop text="row3 c"

# 4a. tblPrChange — table-level property change.
officecli set "$DOCX" '/body/tbl[1]' \
    --prop style=TableGrid \
    --prop revision.author=Dan \
    --prop revision.date=2026-05-25T10:30:00Z

# 4b. trPrChange — row-level property change (row height).
officecli set "$DOCX" '/body/tbl[1]/tr[1]' \
    --prop height=600 \
    --prop revision.author=Dan \
    --prop revision.date=2026-05-25T10:31:00Z

# 4c. tcPrChange — cell-level property change (shading).
#     Cascades tblPrExChange / tblGridChange automatically when needed.
officecli set "$DOCX" '/body/tbl[1]/tr[2]/tc[2]' \
    --prop shd=FFE699 \
    --prop revision.author=Dan \
    --prop revision.date=2026-05-25T10:32:00Z

# 4d. Cell insertion — add a 4th cell to row 2.
officecli add "$DOCX" '/body/tbl[1]/tr[2]' --type cell \
    --prop text="row2 d (inserted)" \
    --prop revision.author=Eve \
    --prop revision.date=2026-05-25T10:33:00Z

# 4e. Cell deletion — drop cell 1 of row 3 (tracked, not destructive).
officecli remove "$DOCX" '/body/tbl[1]/tr[3]/tc[1]' \
    --prop revision.author=Eve \
    --prop revision.date=2026-05-25T10:34:00Z

# 4f. Row insertion — append a row at the table tail; whole row marked inserted.
officecli add "$DOCX" '/body/tbl[1]' --type row \
    --prop revision.author=Eve \
    --prop revision.date=2026-05-25T10:35:00Z

# ==========================================================================
# Section 5 — Section properties (sectPrChange).
#   The body's section properties live at /body/sectPr[1] (NOT /body/sect[1]).
# ==========================================================================
echo "  -> Section 5: section properties (sectPrChange)"
officecli add "$DOCX" /body --type paragraph --prop text="5. Section properties" --prop style=Heading2
officecli add "$DOCX" /body --type paragraph --prop text="The body sectPr below got a tracked pageWidth change."

officecli set "$DOCX" '/body/sectPr[1]' \
    --prop pageWidth=11906 \
    --prop revision.author=Frank \
    --prop revision.date=2026-05-25T10:40:00Z

# ==========================================================================
# Section 6 — Defaults & explicit-id (bonus).
# ==========================================================================
echo "  -> Section 6: default author + auto-allocated id"
officecli add "$DOCX" /body --type paragraph --prop text="6. Defaults" --prop style=Heading2

# 6a. Empty revision.author -> falls back to "OfficeCLI".
#     `add + revision.author=""` silently produces an untracked paragraph
#     (empty author = "no revision"); the default-author fallback fires on
#     the `set` path. So we add the paragraph plain, then `set` it with an
#     empty author to demonstrate the fallback.
officecli add "$DOCX" /body --type paragraph \
    --prop text="This run was wrapped via set with revision.author=\"\" (defaults to OfficeCLI)."
officecli set "$DOCX" '/body/p[19]/r[1]' \
    --prop revision.type=ins \
    --prop revision.author="" \
    --prop revision.date=2026-05-25T10:44:00Z

# 6b. Explicit revision.id outside of a move pair — accepted, you control the
#     w:id attribute. Useful when post-processing needs a deterministic id.
officecli add "$DOCX" /body \
    --type paragraph \
    --prop text="This paragraph carries an explicit revision.id=9001." \
    --prop revision.author=Grace \
    --prop revision.date=2026-05-25T10:45:00Z \
    --prop revision.id=9001

# ==========================================================================
# Section 7 — Find + Replace combined with revision tracking.
#   Mirrors Word's Find&Replace dialog with Track Changes ON: every match is
#   wrapped in the marker shape inferred from the props you pass alongside.
#   The handler auto-allocates a fresh revision.id per marker (one w:del per
#   matched run + one w:ins for the replacement, or one w:rPrChange per match,
#   etc.), so `--prop revision.id=…` is rejected on find — would collide.
# ==========================================================================
echo "  -> Section 7: find + revision (Word-style Find&Replace with Track Changes)"
officecli add "$DOCX" /body --type paragraph --prop text="7. Find + Replace + Revision" --prop style=Heading2

# Helper: add a paragraph and echo the path the handler assigned (e.g.
# /body/p[@paraId=00100012]) so subsequent `set --find …` calls don't
# need to hand-count positional indices. The handler-assigned paraId path
# is stable across content shifts in the body, unlike /body/p[N] which
# drifts every time the section count above changes.
add_para_capture() {
    officecli add "$DOCX" /body --type paragraph --prop text="$1" 2>&1 \
        | grep -oE '/body/p\[@paraId=[A-F0-9]+\]' | tail -1
}

# 7a. find + replace + revision via REGEX — track only the FIRST occurrence
#     of "fox", leave subsequent ones alone. The bare `--find fox` would match
#     every occurrence; controlling which match to track is a job for the
#     regex pattern.
#
#     Pattern:    (?<!fox.*)fox
#     Reading:    match "fox" only when NOT preceded by another "fox" on the
#                 same line. The .NET regex engine supports variable-width
#                 negative lookbehind, so this works without contortions.
#     Effect on this paragraph (two "fox" tokens):
#       The fox jumped and another fox ran fast.
#           ^^^                       ^^^
#           tracked: del+ins          untouched
P7A=$(add_para_capture "7a. The fox jumped and another fox ran fast. (regex tracks only the 1st 'fox'→'cat')")
officecli set "$DOCX" "$P7A" \
    --find '(?<!fox.*)fox' \
    --prop regex=true \
    --replace cat \
    --prop revision.author=Iris \
    --prop revision.date=2026-05-25T10:50:00Z

# 7b. find + format + revision — one w:rPrChange per matched run. The matched
#     text keeps its content; each match becomes a tracked format change.
P7B=$(add_para_capture "7b. Color red apples and the red barn. (tracked bold on every 'red')")
officecli set "$DOCX" "$P7B" \
    --find red \
    --prop bold=true \
    --prop revision.author=Jack \
    --prop revision.date=2026-05-25T10:51:00Z

# 7c. find + replace + format + revision — the inserted run inherits the
#     original rPr from the matched text AND has the new format layered on.
P7C=$(add_para_capture "7c. Replace bar with FOO. (find target → bold-green replacement)")
officecli set "$DOCX" "$P7C" \
    --find bar \
    --replace BAZ \
    --prop bold=true \
    --prop font.color=00B050 \
    --prop revision.author=Kelly \
    --prop revision.date=2026-05-25T10:52:00Z

# 7d. find + regex + revision — multiple matches each get their own marker.
P7D=$(add_para_capture "7d. Prices: \$100, \$250, \$999 (regex \\\$\\d+ → tracked bold)")
officecli set "$DOCX" "$P7D" \
    --find '\$\d+' \
    --prop regex=true \
    --prop bold=true \
    --prop revision.author=Liam \
    --prop revision.date=2026-05-25T10:53:00Z

# ==========================================================================
# Section 8 — Less-common find + revision variants (rounds out Section 7).
#   8a covers `find + replace=""` — a pure tracked deletion via find (one
#   w:del per matched run, no w:ins). 8b covers `find + paragraph property`
#   — paragraph-scope mutation captured as w:pPrChange instead of run-scope
#   w:rPrChange.
# ==========================================================================
echo "  -> Section 8: find variants (replace='' delete-only + paragraph prop pPrChange)"
officecli add "$DOCX" /body --type paragraph --prop text="8. Find variants" --prop style=Heading2

# 8a. find + replace="" + revision — tracked DELETION of every match, no insertion.
#     Useful for "scrub this token from the doc but keep an audit trail".
P8A=$(add_para_capture "8a. Remove the OBSOLETE token here. (delete-only via find — no insertion)")
officecli set "$DOCX" "$P8A" \
    --find OBSOLETE \
    --replace "" \
    --prop revision.author=Mira \
    --prop revision.date=2026-05-25T10:54:00Z

# 8b. find + paragraph prop + revision — one w:pPrChange per matched paragraph.
#     Same code path as `set /body/p[N] --prop align=… --prop revision.author=…`,
#     but filtered to ONLY the paragraphs whose text actually matched the find.
P8B=$(add_para_capture "8b. This paragraph contains MARK so its alignment gets tracked-centered.")
officecli set "$DOCX" "$P8B" \
    --find MARK \
    --prop align=center \
    --prop revision.author=Nora \
    --prop revision.date=2026-05-25T10:55:00Z

officecli close "$DOCX"

# ==========================================================================
# Inspection — list every revision marker in the shipped file.
# ==========================================================================
echo ""
echo "=========================================="
echo "All revisions in $DOCX:"
echo "=========================================="
officecli query "$DOCX" revision

# ==========================================================================
# Action verbs — runs on a TEMP COPY so the shipped artifact keeps every
# marker intact for inspection in Word.
# ==========================================================================
DEMO="$(mktemp -t revisions-demo.XXXXXX).docx"
cp "$DOCX" "$DEMO"

echo ""
echo "=========================================="
echo "Accept/reject demo on temp copy:"
echo "  $DEMO"
echo "=========================================="

# A. Single-end addressing of a move pair: `/revision[@id=N][@type=…]`
#    addresses ONE half of a shared-id pair. Section 3 created moveFrom +
#    moveTo at id=500; reject only the moveTo half here (the moveFrom
#    survives — useful when reviewing decides "keep the deletion at source,
#    discard the insertion at destination"). Done BEFORE step B because B
#    accepts everything Alice authored, including this move pair.
echo "  A) single-end move reject: /revision[@id=500][@type=moveTo]"
officecli set "$DEMO" '/revision[@id=500][@type=moveTo]' --prop revision.action=reject 2>&1 | tail -1
SURVIVING_MOVE=$(officecli query "$DEMO" revision --json 2>/dev/null | python3 -c "
import sys, json
d = json.load(sys.stdin)
for r in d['data']['results']:
    if r.get('format',{}).get('revision.id') == '500':
        print(r['format'].get('revision.type'))
")
echo "  surviving half of id=500: ${SURVIVING_MOVE:-none} (expected: moveFrom)"

# B. Accept everything Alice authored.
echo "  B) accept by author: /revision[@author=Alice]"
officecli set "$DEMO" '/revision[@author=Alice]' --prop revision.action=accept

# C. Reject every w:del-typed revision still left.
echo "  C) reject by type:   /revision[@type=del]"
officecli set "$DEMO" '/revision[@type=del]' --prop revision.action=reject

# D. Accept Carol's explicit-format change by its stable id.
CAROL_FMT_ID=$(officecli query "$DEMO" revision --json 2>/dev/null | python3 -c "
import sys, json
d = json.load(sys.stdin)
for r in d['data']['results']:
    f = r.get('format', {})
    if f.get('revision.author') == 'Carol' and f.get('revision.type') == 'formatChange':
        print(f['revision.id']); break
")
if [ -n "$CAROL_FMT_ID" ]; then
    echo "  D) accept by stable id: /revision[@id=$CAROL_FMT_ID]"
    officecli set "$DEMO" "/revision[@id=$CAROL_FMT_ID]" --prop revision.action=accept
fi

# E. Accept a marker via its native DOM path. Pick the first surviving marker
#    after steps A-D and feed its nativePath back to `set --prop revision.action=accept`.
NATIVE_PATH=$(officecli query "$DEMO" revision --json 2>/dev/null | python3 -c "
import sys, json
d = json.load(sys.stdin)
for r in d['data']['results']:
    np = r.get('format', {}).get('revision.nativePath','')
    if np:
        print(np); break
")
if [ -n "$NATIVE_PATH" ]; then
    echo "  E) accept by native path: $NATIVE_PATH"
    officecli set "$DEMO" "$NATIVE_PATH" --prop revision.action=accept
fi

# F. Sweep — reject everything still pending.
echo "  F) terminal sweep:   /revision  (reject-all)"
officecli set "$DEMO" /revision --prop revision.action=reject

REMAINING=$(officecli query "$DEMO" revision 2>&1 | grep -c "^/revision" || true)
echo "  remaining markers after sweep: $REMAINING (expected: 0)"

rm -f "$DEMO"

# ==========================================================================
# Read-side capabilities — agent-friendly JSON envelope, plain-text render,
# dump→batch round-trip (revision keys survive serialization, so a doc with
# tracked changes can be regenerated end-to-end via `batch --input`).
# ==========================================================================
echo ""
echo "=========================================="
echo "Read-side capabilities on $DOCX:"
echo "=========================================="

echo "  i) query revision --json (first 3 markers, agent-consumable):"
officecli query "$DOCX" revision --json 2>/dev/null | python3 -c "
import sys, json
d = json.load(sys.stdin)
print(f'     matches={d[\"data\"][\"matches\"]}')
for r in d['data']['results'][:3]:
    f = r['format']
    print(f'       path={r[\"path\"]}  type={f.get(\"revision.type\")}  author={f.get(\"revision.author\")}  text={repr(r.get(\"text\",\"\"))[:40]}')
"

echo ""
echo "  ii) view text — runs/paragraphs render with their current content"
echo "      (inserted text shows, deleted text is suppressed; cf. Word's"
echo "       'All Markup' vs 'No Markup' view):"
officecli view "$DOCX" text 2>&1 | head -10 | sed 's/^/       /'

echo ""
echo "  iii) dump --format batch round-trip — revision creation keys survive"
echo "       serialization so a tracked-change doc regenerates end-to-end via"
echo "       'batch --input <dump.json>'. Sample of revision keys in dump:"
officecli dump "$DOCX" / --format batch 2>/dev/null \
    | python3 -c "
import sys, json
batch = json.load(sys.stdin)
rev_steps = [s for s in batch
             if any(k.startswith('revision.') for k in (s.get('props') or {}))]
print(f'       total batch steps: {len(batch)},  steps carrying revision.* props: {len(rev_steps)}')
for s in rev_steps[:3]:
    keys = [k for k in s['props'] if k.startswith('revision.')]
    print(f'       {s[\"command\"]:>6} {s.get(\"path\",s.get(\"parent\",\"\")):40} keys={keys}')
"

echo ""
echo "Done: $DOCX"
ls -lh "$DOCX"
