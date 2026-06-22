// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using OfficeCli.Core;

namespace OfficeCli.Handlers;

public static partial class PptxBatchEmitter
{
    // CONSISTENCY(emit-table-mirror): mirrors WordBatchEmitter.Table.cs in
    // shape — emit `add table` with rows/cols, then per-row `set tr[N]`,
    // per-cell `set tc[K]`, and finally cell text via `set tc[K] text=...`.
    // PPT tables are simpler than Word tables (no nested tables, no
    // tblGrid/tblBorders aggregate elements), so this is a much smaller
    // method than the docx version.

    private static void EmitTable(PowerPointHandler ppt, DocumentNode tableNode,
                                  string parentSlidePath, string replayPath,
                                  List<BatchItem> items, SlideEmitContext ctx)
    {
        // depth=2 so /slide/table/tr/tc cell nodes materialize with text.
        var fullTable = ppt.Get(tableNode.Path, depth: 2);
        var props = FilterEmittableProps(fullTable.Format);
        if (!props.ContainsKey("rows") || !props.ContainsKey("cols")) return;

        // AddTable seeds rows×cols empty cells; per-cell text + per-row
        // height + per-cell tcPr (fill/borders/padding/valign/spans) get
        // pushed via subsequent `set` rows. Avoid re-emitting the `data=`
        // shortcut form — it's mutually exclusive with rows/cols and would
        // hide per-cell formatting we want to preserve.
        items.Add(new BatchItem
        {
            Command = "add",
            Parent = parentSlidePath,
            Type = "table",
            Props = props.Count > 0 ? props : null,
        });

        var tablePath = replayPath;
        if (fullTable.Children == null) return;
        var rows = fullTable.Children.Where(c => c.Type == "tr").ToList();

        // Collect external-hyperlink rIds referenced by any cell's verbatim
        // txBodyRaw. A cell that links a run to a URL keeps <a:hlinkClick
        // r:id="rIdN"> in the raw body, but the typed emit never re-creates that
        // external relationship — the rebuilt slide dangled. Carry each below.
        var cellHlinkRids = new HashSet<string>(StringComparer.Ordinal);

        int rIdx = 0;
        foreach (var row in rows)
        {
            rIdx++;
            var rowProps = FilterEmittableProps(row.Format);
            // Row height — Set tr accepts `height=`; other row-level keys
            // round-trip through the per-cell set path so emit only the
            // narrow whitelist here.
            var emittedRow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (rowProps.TryGetValue("height", out var h))
                emittedRow["height"] = h;
            if (emittedRow.Count > 0)
            {
                items.Add(new BatchItem
                {
                    Command = "set",
                    Path = $"{tablePath}/tr[{rIdx}]",
                    Props = emittedRow,
                });
            }

            int cIdx = 0;
            foreach (var cell in row.Children ?? new List<DocumentNode>())
            {
                if (cell.Type != "tc") continue;
                cIdx++;
                var cellProps = FilterEmittableProps(cell.Format);
                // CONSISTENCY(empty-run-preserve): NodeBuilder surfaces
                // hasEmptyRun=true when the source cell carries a run-bearing
                // empty paragraph (<a:r><a:rPr/><a:t/></a:r>). AddTable's
                // blank-cell seed uses <a:endParaRPr/>, so dump→replay drifts
                // unless we force the run-bearing form by issuing `set
                // text=""` — which routes through AppendLineWithTabs and
                // produces the canonical empty run.
                bool forceEmptyText = cellProps.Remove("hasEmptyRun");

                // Border verbatim-raw wins over the granular border.<edge>.*
                // keys. The Set path reuses an existing border element when the
                // granular op runs, so a stray granular op after the raw inject
                // appends an out-of-order child (e.g. solidFill into an lnB that
                // already carries noFill) and breaks the schema. Drop every
                // granular border key for an edge whose .raw counterpart is
                // present; the raw element is authoritative and self-contained.
                foreach (var edge in new[] { "left", "right", "top", "bottom", "tl2br", "tr2bl" })
                {
                    if (!cellProps.ContainsKey($"border.{edge}.raw")) continue;
                    foreach (var suffix in new[] { "", ".width", ".color", ".dash", ".compound" })
                        cellProps.Remove($"border.{edge}{suffix}");
                }
                // The summary keys (border / border.all) fan out to all four
                // straight edges; if any of those edges has a raw, the summary
                // would re-touch it. Drop the summaries whenever any edge raw is
                // present — raws fully describe their edges.
                if (new[] { "left", "right", "top", "bottom" }
                        .Any(e => cellProps.ContainsKey($"border.{e}.raw")))
                {
                    cellProps.Remove("border");
                    cellProps.Remove("border.all");
                }

                // When the cell carries a verbatim txBodyRaw (rich pPr/lstStyle/
                // rPr the text= rebuild would drop), the raw passthrough already
                // contains the full text — emitting a companion text= op would
                // rebuild bare paragraphs and clobber it. Suppress text= in that
                // case and let the txBodyRaw set op restore the body verbatim.
                bool hasRawBody = cellProps.ContainsKey("txBodyRaw");
                if (hasRawBody && cellProps.TryGetValue("txBodyRaw", out var rawBody) && rawBody != null)
                {
                    foreach (System.Text.RegularExpressions.Match hm in
                             System.Text.RegularExpressions.Regex.Matches(rawBody, @"<a:hlink(?:Click|Hover)\b[^>]*r:id=""([^""]+)"""))
                        cellHlinkRids.Add(hm.Groups[1].Value);
                }
                // Set tc accepts text= for replacing the cell's text body.
                if (hasRawBody)
                {
                    // raw body is authoritative; no text= companion.
                }
                else if (!string.IsNullOrEmpty(cell.Text))
                    cellProps["text"] = cell.Text!;
                else if (forceEmptyText)
                    cellProps["text"] = "";
                if (cellProps.Count == 0) continue;
                items.Add(new BatchItem
                {
                    Command = "set",
                    Path = $"{tablePath}/tr[{rIdx}]/tc[{cIdx}]",
                    Props = cellProps,
                });
            }
        }

        // Carry external hyperlink rels referenced by the cells' verbatim
        // txBodyRaw so <a:hlinkClick r:id="rIdN"> resolves instead of dangling.
        // Host is the slide (cell hlink rels live on the SlidePart); pin the
        // source rId + URL. Mirrors the layout/notes external-hyperlink carrier.
        if (cellHlinkRids.Count > 0)
        {
            var slideM = System.Text.RegularExpressions.Regex.Match(parentSlidePath, @"^/slide\[(\d+)\]");
            if (slideM.Success)
            {
                var slideHostPath = $"/slide[{slideM.Groups[1].Value}]";
                var slideNumForCell = int.Parse(slideM.Groups[1].Value);
                try
                {
                    foreach (var (relId, target) in
                             ppt.GetSlideExternalHyperlinksByRelId(slideNumForCell, cellHlinkRids))
                    {
                        items.Add(new BatchItem
                        {
                            Command = "add-part",
                            Parent = slideHostPath,
                            Type = "hyperlink",
                            Props = new Dictionary<string, string>
                            {
                                ["rid"] = relId,
                                ["target"] = target,
                            },
                        });
                    }
                }
                catch { /* best-effort */ }

                // Internal slide-jump links (<a:hlinkClick action="…hlinksldjump"
                // r:id> → another slide) in a cell's txBodyRaw. The target slide
                // is re-added later, so DEFER the pinned slide relationship to the
                // end of the emit (ctx.DeferredLinks replays after every slide
                // exists), mapping the source rId to the rebuilt target ordinal.
                // Without this the rebuilt cell's r:id dangles → PowerPoint refuses
                // the deck (0x80070570).
                try
                {
                    foreach (var (relId, targetOrd) in
                             ppt.GetSlideInternalSlideJumpRels(slideNumForCell, cellHlinkRids))
                    {
                        ctx.DeferredLinks.Add(new BatchItem
                        {
                            Command = "add-part",
                            Parent = slideHostPath,
                            Type = "sliderel",
                            Props = new Dictionary<string, string>
                            {
                                ["rid"] = relId,
                                ["target"] = targetOrd.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            },
                        });
                    }
                }
                catch { /* best-effort */ }
            }
        }
    }
}
