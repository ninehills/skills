// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using OfficeCli.Core;

namespace OfficeCli.Handlers;

public static partial class PptxBatchEmitter
{
    // Emit speaker notes. The typed `add notes` row only seeds the body
    // placeholder text (AddNotes's input vocabulary is text/direction/lang
    // — no surface for additional shapes, rich rPr, or layout customs in
    // the notesSlide spTree). Without a raw-set passthrough the rest of
    // the notesSlide content silently dropped on round-trip — notes with
    // multiple paragraphs, embedded run formatting, or extra placeholders
    // all degraded to a single plain-text body line.
    //
    // Fix: emit the typed add (creates / instantiates the NotesSlidePart
    // on the blank target, since AddNotes wires up the master/layout
    // links blank decks lack), then immediately overwrite the whole
    // /p:notes root via raw-set replace using the source's verbatim XML.
    // raw-set runs after every shape/animation row, so the notes
    // placeholder created by the typed add is overwritten with the
    // original byte form on replay.
    private static void EmitNotes(PowerPointHandler ppt, string slidePath,
                                  List<BatchItem> items, SlideEmitContext ctx)
    {
        var slideMatch = System.Text.RegularExpressions.Regex.Match(slidePath, @"^/slide\[(\d+)\]$");
        if (!slideMatch.Success) return;
        var slideIdx = int.Parse(slideMatch.Groups[1].Value);
        if (!ppt.SlideHasNotes(slideIdx)) return;

        // Phase 1 — typed add so the NotesSlidePart + slide-rel exist on
        // the replay target. Without this the subsequent raw-set
        // /noteSlide[N] throws because the part hasn't been wired up
        // (blank decks ship with no NotesSlidePart per slide).
        DocumentNode notes;
        try { notes = ppt.Get($"{slidePath}/notes"); }
        catch { notes = new DocumentNode { Type = "notes" }; }
        if (notes.Type != "error")
        {
            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(notes.Text))
                props["text"] = notes.Text!;
            foreach (var key in new[] { "direction", "lang" })
            {
                if (notes.Format.TryGetValue(key, out var v) && v != null)
                {
                    var s = v.ToString() ?? "";
                    if (s.Length > 0) props[key] = s;
                }
            }
            // Always emit the add — even when props are empty, AddNotes
            // still creates the part with an empty body which raw-set
            // then overwrites with the source XML. text="" is the empty-
            // body marker; AddNotes accepts it.
            if (props.Count == 0) props["text"] = "";
            items.Add(new BatchItem
            {
                Command = "add",
                Parent = slidePath,
                Type = "notes",
                Props = props,
            });
        }

        // Phase 2a — emit ImageParts attached to the NotesSlidePart BEFORE the
        // raw-set replace. R58 bt-5: the notesSlide raw XML carries
        // <p:pic> blipFill r:embed="rIdN" references when the speaker notes
        // contain a pasted image. The R46 41c5e2ae fix forwarded the notes
        // XML byte-equal but left the sidecar ImagePart unreplicated, so the
        // post-replay notesSlide held a dangling rId and PowerPoint rendered
        // the picture as a broken placeholder.
        //
        // Mirrors EmitMasterRawOne / EmitLayoutRawOne — add-part image pins
        // the source's rId so the raw-set'd notes XML resolves on replay.
        IReadOnlyList<PowerPointHandler.MasterImageInfo> noteImages;
        try { noteImages = ppt.GetNoteSlideImageParts(slideIdx); }
        catch { noteImages = System.Array.Empty<PowerPointHandler.MasterImageInfo>(); }
        var resolvedRids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var imageInfo in noteImages)
        {
            items.Add(new BatchItem
            {
                Command = "add-part",
                Parent = $"/noteSlide[{slideIdx}]",
                Type = "image",
                Props = new Dictionary<string, string>
                {
                    ["rid"] = imageInfo.RelId,
                    ["content-type"] = imageInfo.ContentType,
                    ["data"] = imageInfo.Base64Data,
                },
            });
            resolvedRids.Add(imageInfo.RelId);
        }

        // Phase 2a' — external hyperlink relationships in the speaker notes. The
        // notes raw XML carries <a:hlinkClick r:id="rIdN"> (a URL inserted into a
        // notes run), but the external relationship is not an embedded part, so
        // the ImagePart carrier above never re-creates it — the rebuilt notesSlide
        // kept a dangling rId and PowerPoint refused the whole deck (OPC corrupt).
        // Pin each id via add-part hyperlink BEFORE the raw-set replace. Mirrors
        // the slideLayout external-hyperlink carrier (EmitLayoutRawOne).
        IReadOnlyList<(string RelId, string Target)> noteLinks;
        try { noteLinks = ppt.GetNoteSlideExternalHyperlinks(slideIdx); }
        catch { noteLinks = System.Array.Empty<(string, string)>(); }
        foreach (var (relId, target) in noteLinks)
        {
            items.Add(new BatchItem
            {
                Command = "add-part",
                Parent = $"/noteSlide[{slideIdx}]",
                Type = "hyperlink",
                Props = new Dictionary<string, string>
                {
                    ["rid"] = relId,
                    ["target"] = target,
                },
            });
            resolvedRids.Add(relId);
        }

        // Phase 2b — raw-set replace with the verbatim source /p:notes XML.
        // Mirrors EmitNoteSlideRawOne in PptxBatchEmitter.Resources.cs but
        // is called per-slide here so it lands AFTER the typed add has
        // created the underlying NotesSlidePart.
        string notesXml;
        try { notesXml = ppt.Raw($"/noteSlide[{slideIdx}]"); }
        catch { return; }
        if (string.IsNullOrEmpty(notesXml) || !notesXml.StartsWith("<")) return;
        notesXml = CanonicalizeRawXml(notesXml);
        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = $"/noteSlide[{slideIdx}]",
            Xpath = "/p:notes",
            Action = "replace",
            Xml = notesXml,
        });

        // Phase 2c — surface unresolved rIds. R58 bt-5 fallback: when the
        // notesSlide XML references an rId we did not just materialise as an
        // ImagePart (embedded media, OLE, hyperlinks to chart parts, …),
        // PowerPoint will still flag a broken reference on open. The R58
        // primary fix covers ImageParts; everything else surfaces here as a
        // notes_unresolved_rid warning so callers can investigate without a
        // silent rendering regression. Drops the layout/master inherited rels
        // (rId1 typically targets the notesSlideLayout — relinked by AddNotes).
        IReadOnlyList<string> referencedRids;
        try { referencedRids = ppt.GetNoteSlideExternalRelIds(slideIdx); }
        catch { referencedRids = System.Array.Empty<string>(); }
        foreach (var rid in referencedRids)
        {
            if (resolvedRids.Contains(rid)) continue;
            // Heuristic: the inherited layout/slide rels (rId1, rId2) are
            // re-wired by the typed `add notes` row above. Anything beyond
            // those that we did not just emit is genuinely unresolved.
            if (rid == "rId1" || rid == "rId2") continue;
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: OfficeCli.Core.IssueSubtypes.NotesUnresolvedRid,
                SlidePath: $"{slidePath}/notes",
                Reason: $"notesSlide raw-set passthrough references r:id='{rid}' which the dump pass cannot reproduce on the replay target. PowerPoint may render the referenced object as a broken placeholder. Likely cause: embedded media, OLE, or other non-ImagePart relationship attached to the speaker notes."));
        }
    }

    // Slide-level legacy comments (`<p:cm>`) live in SlideCommentsPart, not
    // the shape tree, so the standard EmitSlide walk never reaches them —
    // dump silently lost every author/date/anchor on a deck that carried
    // review comments. Re-emit each as `add comment parent=/slide[N]` using
    // the same vocabulary AddSlideComment accepts (text/author/initials/x/y/
    // date). Index-1 is emitted with no `--index`, so AddSlideComment appends
    // monotonically and the source order is preserved on replay.
    private static void EmitComments(PowerPointHandler ppt, string slidePath,
                                     List<BatchItem> items, SlideEmitContext ctx)
    {
        var slideMatch = System.Text.RegularExpressions.Regex.Match(slidePath, @"^/slide\[(\d+)\]$");
        if (!slideMatch.Success) return;
        var slideIdx = int.Parse(slideMatch.Groups[1].Value);

        List<DocumentNode> commentNodes;
        try { commentNodes = ppt.EnumerateComments(slideIdx); }
        catch { return; }
        if (commentNodes.Count == 0) return;

        foreach (var cmt in commentNodes)
        {
            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(cmt.Text))
                props["text"] = cmt.Text!;
            // Mirror the AddSlideComment vocabulary verbatim. `index` is a
            // node-level Get-only field (the per-author monotonic counter
            // PowerPoint assigns); replaying it would force-collide with the
            // counter AddSlideComment maintains on the target deck.
            foreach (var key in new[] { "author", "initials", "x", "y", "date" })
            {
                if (cmt.Format.TryGetValue(key, out var v) && v != null)
                {
                    var s = v.ToString() ?? "";
                    if (s.Length > 0) props[key] = s;
                }
            }

            items.Add(new BatchItem
            {
                Command = "add",
                Parent = slidePath,
                Type = "comment",
                Props = props.Count > 0 ? props : null,
            });
        }
    }

    // Modern p188 threaded comments — distinct OOXML part from legacy p:cm.
    // Emit one `add modernComment parent=/slide[N]` row per top-level thread
    // followed by `add modernComment parent=/slide[N]` rows with
    // parent=/slide[N]/modernComment[K] for each reply, in document order so
    // replay rebuilds the thread tree in shape.
    private static void EmitModernComments(PowerPointHandler ppt, string slidePath,
                                           List<BatchItem> items, SlideEmitContext ctx)
    {
        var slideMatch = System.Text.RegularExpressions.Regex.Match(slidePath, @"^/slide\[(\d+)\]$");
        if (!slideMatch.Success) return;
        var slideIdx = int.Parse(slideMatch.Groups[1].Value);

        List<DocumentNode> threads;
        try { threads = ppt.EnumerateModernComments(slideIdx); }
        catch { return; }
        if (threads.Count == 0) return;

        int topIdx = 0;
        foreach (var top in threads)
        {
            topIdx++;
            // Top-level row.
            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(top.Text)) props["text"] = top.Text!;
            foreach (var key in new[] { "author", "initials", "created" })
            {
                if (top.Format.TryGetValue(key, out var v) && v != null)
                {
                    var s = v.ToString() ?? "";
                    if (s.Length > 0) props[key] = s;
                }
            }
            // resolved is bool — only emit when true (false is the default).
            if (top.Format.TryGetValue("resolved", out var rv) && rv is bool rb && rb)
                props["resolved"] = "true";
            items.Add(new BatchItem
            {
                Command = "add",
                Parent = slidePath,
                Type = "modernComment",
                Props = props.Count > 0 ? props : null,
            });

            // Reply rows. The top-level rows we just emitted are indexed
            // 1..N on the replayed deck in the same order we emit them, so
            // parent= can reference /slide[N]/modernComment[topIdx].
            var parentPath = $"/slide[{slideIdx}]/modernComment[{topIdx}]";
            foreach (var r in top.Children)
            {
                var rp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(r.Text)) rp["text"] = r.Text!;
                foreach (var key in new[] { "author", "initials", "created" })
                {
                    if (r.Format.TryGetValue(key, out var v) && v != null)
                    {
                        var s = v.ToString() ?? "";
                        if (s.Length > 0) rp[key] = s;
                    }
                }
                rp["parent"] = parentPath;
                items.Add(new BatchItem
                {
                    Command = "add",
                    Parent = slidePath,
                    Type = "modernComment",
                    Props = rp,
                });
            }
        }
    }
}
