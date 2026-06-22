// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using OfficeCli.Core;

namespace OfficeCli.Handlers;

public static partial class PptxBatchEmitter
{
    // CONSISTENCY(emit-shape-mirror): mirrors WordBatchEmitter.Paragraph.cs
    // logic shape — get the node, filter props, decide collapsed-single-run
    // vs multi-run, emit the parent then iterate children. PowerPoint
    // shapes can carry many paragraphs (a slide text body is a list of
    // <a:p> elements), so the collapse heuristic is per-paragraph, not
    // per-shape.

    // Forward slide-jump form emitted by NodeBuilder ("slide[3]"). Internal
    // PowerPoint actions (firstslide/lastslide/nextslide/previousslide/endshow)
    // don't depend on a relationship and replay fine at shape-add time.
    private static readonly System.Text.RegularExpressions.Regex SlideJumpLink =
        new(@"^slide\[\d+\]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase
                              | System.Text.RegularExpressions.RegexOptions.Compiled);

    // Strip-only variant for nested bags (paragraph/run) — the shape-level
    // emit owns the deferred slide-jump set; nested bags must not re-emit it.
    private static void DummyCtxStripSlideJump(Dictionary<string, string> props)
    {
        if (props.TryGetValue("link", out var v) && SlideJumpLink.IsMatch(v ?? ""))
            props.Remove("link");
    }

    // Run-level analogue of DeferSlideJumpLink. Target path is the run's
    // positional path under its paragraph parent; tooltip rides along.
    private static void DeferRunSlideJumpLink(Dictionary<string, string> props, string paraPath,
                                              int runIndex, SlideEmitContext ctx)
    {
        if (!props.TryGetValue("link", out var linkVal) || string.IsNullOrEmpty(linkVal)) return;
        if (!SlideJumpLink.IsMatch(linkVal)) return;
        props.Remove("link");
        var deferredProps = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["link"] = linkVal,
        };
        if (props.TryGetValue("tooltip", out var tt) && !string.IsNullOrEmpty(tt))
        {
            deferredProps["tooltip"] = tt;
            props.Remove("tooltip");
        }
        ctx.DeferredLinks.Add(new BatchItem
        {
            Command = "set",
            Path = $"{paraPath}/run[{runIndex}]",
            Props = deferredProps,
        });
    }

    // R24 — a:pPr accepts none of these (ECMA-376 §21.1.2.2.7 lvlLPr /
    // §21.1.2.2.6 defaultLevelParagraphProperties — language is part of
    // a:rPr only). The single-run-collapse path used to spill these onto
    // the paragraph set bag, which Set then routed into `unsupported`.
    private static readonly HashSet<string> RunOnlyRprAttrs =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "lang", "altLang", "kern", "kumimoji", "normalizeH",
        "smtClean", "smtId", "bmk", "dirty", "err", "baseline",
        // R53 bt-6: `textFill` is a run-level <a:gradFill> on the run's
        // <a:rPr>. The single-run-collapse path used to promote it onto
        // the paragraph set, where Set then re-applied the gradient to
        // every run via `set …/paragraph[1] textFill=` instead of the
        // intended `set …/run[1] textFill=` — drift on the surface form
        // and on the dump→replay path the agent reads.
        "textFill",
        // bt-1: run-level <a:rPr><a:effectLst> children (shadow / glow /
        // reflection / softEdge / innerShadow / blur) belong to the run.
        // The collapse used to lift them onto the paragraph set, where the
        // paragraph dispatcher then fan-out applied them to every run in
        // the paragraph (via the NoFillShape heuristic) — double-emit for
        // single-run paragraphs and over-broad emit for multi-run ones.
        "shadow", "shadowRaw", "innerShadow", "innerShadowRaw", "glow",
        "reflection", "reflectionRaw", "softEdge", "blur",
        // R62 bt-5: run-level <a:rPr><a:effectLst><a:fillOverlay> belongs to
        // the run (per-character tinted overlay). NodeBuilder now emits
        // `fillOverlayRaw=<a:fillOverlay…/>` on run nodes; keep it on the run
        // through single-run-collapse so it doesn't leak onto the paragraph
        // set bag (where the paragraph dispatcher fans it out to every run
        // via the NoFillShape heuristic, double-emit for single-run paragraphs).
        "fillOverlayRaw",
        // R57 bt-2: underline + its uLn/uFill companion keys live on
        // <a:rPr> (run only). The collapse used to promote them onto the
        // paragraph set bag, so a single-run colored underline replayed
        // as `set …/paragraph[1] underline=single` AND `set …/shape[K]
        // underline=single` — every run in the (re-added) paragraph,
        // and the runless shape's endParaRPr, came back underlined. Keep
        // them on the run so a true single-run underline emits exactly
        // one `set …/run[1] underline=…` op.
        "underline", "underline.color", "underline.width",
        // R61 bt-1: textOutline lives on <a:rPr><a:ln> — distinct from
        // shape-level line= on spPr. Keep on the run so a single-run
        // outlined glyph emits `add run textOutline=…` instead of leaking
        // onto the paragraph set bag and silently broadcasting to every
        // run in the paragraph.
        "textOutline", "textOutline.color", "textOutline.width",
    };

    // Pull a `link=slide[N]` prop out of the bag and queue a deferred `set`
    // BatchItem so the link write runs after every slide has been added.
    // External URLs and named actions stay in the prop bag for the normal
    // shape-add path. `enqueue=false` is used for nested para/run prop bags
    // where the shape-level emit already handles the deferred set — we just
    // need to drop the prop so the nested `set` doesn't fail.
    private static void DeferSlideJumpLink(Dictionary<string, string> props, string replayPath,
                                           SlideEmitContext ctx, bool enqueue = true)
    {
        if (!props.TryGetValue("link", out var linkVal) || string.IsNullOrEmpty(linkVal)) return;
        if (!SlideJumpLink.IsMatch(linkVal)) return;
        props.Remove("link");
        if (!enqueue) return;
        var deferredProps = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["link"] = linkVal,
        };
        if (props.TryGetValue("tooltip", out var tt) && !string.IsNullOrEmpty(tt))
        {
            // Tooltip is meaningful only with a link; carry it along.
            deferredProps["tooltip"] = tt;
            props.Remove("tooltip");
        }
        ctx.DeferredLinks.Add(new BatchItem
        {
            Command = "set",
            Path = replayPath,
            Props = deferredProps,
        });
    }

    private static void EmitShape(PowerPointHandler ppt, DocumentNode shapeNode, string parentSlidePath,
                                  string replayPath, List<BatchItem> items, SlideEmitContext ctx)
    {
        // depth=3 so paragraph -> run -> any inline runs all materialize. The
        // single-run collapse heuristic needs the run nodes present to read
        // their text / char-prop bag.
        var fullShape = ppt.Get(shapeNode.Path, depth: 3);
        var shapeProps = FilterEmittableProps(fullShape.Format);
        // CONSISTENCY(shape-link-source): NodeBuilder surfaces Format["link"]
        // on the shape node from two sources: (a) cNvPr.hlinkClick on the
        // shape itself, and (b) the FIRST run's rPr.hlinkClick (a single-run
        // convenience for Get callers — so a fully hyperlinked textbox
        // surfaces its href at the shape level without descending into
        // /p[1]/r[1]). NodeBuilder prefers (b) when present, so the bag's
        // url may not match the shape's cNvPr.hlinkClick at all when a
        // shape carries BOTH. Probe the live XML:
        //   - no shape-level hlink → strip (run emit re-adds it).
        //   - shape-level hlink present → overwrite the bag's url/tooltip
        //     with the cNvPr ones so the emitted `add shape link=` reflects
        //     the actual shape-level target. The per-run emit still adds
        //     its own (possibly different) run-level link separately.
        if (shapeProps.ContainsKey("link"))
        {
            var (hasShape, shapeUrl, shapeTip) = ppt.GetShapeCNvPrHyperlinkInfo(shapeNode.Path);
            if (!hasShape)
            {
                shapeProps.Remove("link");
                shapeProps.Remove("tooltip");
            }
            else
            {
                if (!string.IsNullOrEmpty(shapeUrl)) shapeProps["link"] = shapeUrl!;
                if (!string.IsNullOrEmpty(shapeTip)) shapeProps["tooltip"] = shapeTip!;
                else shapeProps.Remove("tooltip");
            }
        }
        DeferSlideJumpLink(shapeProps, replayPath, ctx);

        // Shape image fill (blipFill) — NodeBuilder emits the marker
        // `image=true` for shapes carrying a <a:blipFill>. The marker is
        // dropped by FilterEmittableProps because it has no actionable
        // value; resolve the embedded image bytes here and re-emit
        // `image=data:<contentType>;base64,…` so AddShape's
        // ApplyShapeImageFill can rebuild the blipFill on replay.
        // (Mirrors EmitPicture base64-inline strategy.)
        if (fullShape.Format.TryGetValue("image", out var imgMarker)
            && string.Equals(imgMarker?.ToString(), "true", StringComparison.OrdinalIgnoreCase)
            && !shapeProps.ContainsKey("image"))
        {
            var binary = ppt.GetShapeImageFillBinary(shapeNode.Path);
            if (binary.HasValue)
            {
                var (bytes, contentType) = binary.Value;
                shapeProps["image"] = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
            }
            else
            {
                ctx.Unsupported.Add(new UnsupportedWarning(
                    Element: "shape",
                    SlidePath: parentSlidePath,
                    Reason: "shape blipFill has no resolvable embedded image part"));
            }
        }

        // NodeBuilder emits `geometry=rect` for every shape with the implicit
        // <a:prstGeom prst="rect"/> body — including plain text boxes and
        // bare `--type shape` calls (no styling). Strip the rect default for
        // textbox/title (they don't "own" a geometry concept, so echoing it
        // back would attach a shape signal to a textbox on replay) and for
        // bare default-flavor shapes that carry no other distinguishing
        // styling. When the source explicitly set fill/line/etc., keep
        // `geometry=rect` so the replay path sees the same prop bag.
        if (shapeProps.TryGetValue("geometry", out var geomVal)
            && geomVal.Equals("rect", StringComparison.OrdinalIgnoreCase))
        {
            bool stripRect = shapeNode.Type == "textbox" || shapeNode.Type == "title";
            if (!stripRect && shapeNode.Type == "shape")
            {
                bool hasExplicitStyling =
                    shapeProps.ContainsKey("fill")
                    || shapeProps.ContainsKey("gradient")
                    || shapeProps.ContainsKey("pattern")
                    || shapeProps.ContainsKey("line")
                    || shapeProps.ContainsKey("lineWidth")
                    || shapeProps.ContainsKey("lineDash")
                    || shapeProps.ContainsKey("lineDashRaw")
                    || shapeProps.ContainsKey("opacity");
                stripRect = !hasExplicitStyling;
            }
            if (stripRect) shapeProps.Remove("geometry");
        }

        // Emit type matches Add dispatch: "title" / "equation" both reduce to
        // "shape" or "textbox" on Add, and the emitted shape carries its
        // distinguishing prop (isTitle=true / formula=...). For now use
        // "textbox" for plain text shapes (no geometry) and "shape" otherwise.
        // CONSISTENCY(equation-emit-degrade): AddEquation throws when neither
        // `formula` nor `text` is present. NodeBuilder.ShapeToNode emits
        // Format["formula"] (LaTeX from OMath) when available; if it isn't
        // (exotic OMath that ToLatex can't render), degrade to a plain textbox
        // emit rather than crash replay.
        bool isEquation = shapeNode.Type == "equation" && shapeProps.ContainsKey("formula");
        // Preserve the shape/textbox distinction on emit: NodeBuilder's Type
        // already reflects the on-disk txBox flag, so route by Type rather
        // than reverse-engineering it from geometry presence (which we may
        // have just stripped above).
        string emitType = shapeNode.Type switch
        {
            "title" => "shape",
            "equation" => isEquation ? "equation" : "shape",
            "shape" => "shape",
            _ => "textbox",
        };

        items.Add(new BatchItem
        {
            Command = "add",
            Parent = parentSlidePath,
            Type = emitType,
            Props = shapeProps.Count > 0 ? shapeProps : null,
        });

        // CONSISTENCY(shape-style-rawset): the <p:style> theme-reference
        // block (<a:lnRef>/<a:fillRef>/<a:effectRef>/<a:fontRef>) sits as a
        // sibling of <p:txBody> under <p:sp> and has no typed Add/Set
        // vocabulary. NodeBuilder doesn't surface it either, so the source
        // block was silently dropped on dump->replay before this hook. Pull
        // the verbatim outer XML from the source and re-inject it via a
        // raw-set append on the freshly added shape's <p:sp>. Slide-root
        // shapes only (mirrors the picture clrChange xpath scope); group-
        // nested shapes fall through unchanged — the xpath form would need
        // to walk <p:grpSp> ancestors, deferred to a follow-up.
        if (System.Text.RegularExpressions.Regex.Match(replayPath,
                @"^/slide\[\d+\]/(?:shape|textbox|title|equation|placeholder)\[(\d+)\]$")
            is { Success: true } shStyleM)
        {
            var styleProbe = ppt.GetShapeStyleXmlWithOrdinal(shapeNode.Path ?? "");
            if (styleProbe.HasValue)
            {
                var (styleXml, spOrd) = styleProbe.Value;
                items.Add(new BatchItem
                {
                    Command = "raw-set",
                    Part = parentSlidePath,
                    Xpath = $"/p:sld/p:cSld/p:spTree/p:sp[{spOrd}]",
                    Action = "append",
                    Xml = styleXml,
                });
            }
        }

        // Equation shapes' text body is AlternateContent (a14:m + readable
        // fallback run); the math content is fully captured by `formula`.
        // Emitting paragraphs/runs here would inject the fallback string as
        // user text — skip the body walk for equations entirely.
        if (isEquation) return;

        // CONSISTENCY(shape-empty-seed): since commit c574db7a, CreateTextShape
        // emits `<a:p/>` (no <a:r>) for the empty-text path — the same shape
        // PowerPoint writes for a fresh empty text body. The emitted shape `add`
        // op above never carries `text=` (text always replays via per-paragraph /
        // per-run ops), so AddShape/AddTextbox always seeds paragraph[1] with
        // zero runs. EmitTextBody must therefore tell EmitParagraph the seeded
        // first paragraph has no run — otherwise the multi-run path emits
        // `set run[1]` against a run that doesn't exist on replay, breaking
        // round-trip for any shape whose first paragraph has >1 run.
        EmitTextBody(ppt, fullShape, replayPath, items, seededFirstParaHasRun: false, ctx: ctx);
    }

    private static void EmitPlaceholder(PowerPointHandler ppt, DocumentNode phNode, string parentSlidePath,
                                        string replayPath, List<BatchItem> items, SlideEmitContext ctx)
    {
        var full = ppt.Get(phNode.Path, depth: 3);
        var props = FilterEmittableProps(full.Format);
        DeferSlideJumpLink(props, replayPath, ctx);

        // CONSISTENCY(shape-id-high-range): preserve the source cNvPr.Id
        // verbatim. AcquireShapeId's auto-assign base is 100000+ (well above
        // the 1..99 range PowerPoint uses for placeholders and the typical
        // 1000..99999 range for regular shapes), so id collisions with the
        // counter are impossible. Per-slide cNvPr uniqueness is required by
        // OOXML, and the source already satisfies it; we just echo it back.
        // This also keeps <p:spTgt spid="N"/> references in the slide's
        // <p:timing> tree (round-tripped via raw-set passthrough) pointing
        // at the right shape.

        // Faithful round-trip: a source slide may carry two title/ctrTitle
        // placeholders (PowerPoint keeps them; it only de-dups via the UI). The
        // interactive uniqueness guard in AddPlaceholder would abort the second
        // one — and cascade the slide's later shapes via the broken shape count.
        // Signal the replay to allow it. Consumed by AddPlaceholder, not written
        // to XML.
        props["allowDuplicate"] = "true";

        items.Add(new BatchItem
        {
            Command = "add",
            Parent = parentSlidePath,
            Type = "placeholder",
            Props = props.Count > 0 ? props : null,
        });

        // AddPlaceholder seeds the first paragraph with <a:endParaRPr> only —
        // no <a:r>. Emitting the first run via `set run[1]` (the shape/textbox
        // path) targets a non-existent run and fails the batch. Tell
        // EmitTextBody the seeded paragraph has zero runs so it issues `add
        // run` for the first run instead.
        EmitTextBody(ppt, full, replayPath, items, seededFirstParaHasRun: false, ctx: ctx);
    }

    private static void EmitConnector(PowerPointHandler ppt, DocumentNode cxnNode, string parentSlidePath,
                                      List<BatchItem> items, SlideEmitContext ctx, int connectorOrdinal)
    {
        // R57 bt-4: depth=3 mirrors EmitShape — surface paragraph→run→inline
        // runs so the connector-label single-run-collapse heuristic below can
        // read the run's text + char-prop bag.
        var full = ppt.Get(cxnNode.Path, depth: 3);
        var props = FilterEmittableProps(full.Format);

        // R57 bt-4: PowerPoint allows a <p:txBody> child on <p:cxnSp> to render
        // an in-line label between the connector's endpoints. NodeBuilder
        // surfaces paragraphs / runs under the connector node; replay them
        // through AddConnector (single-run-collapse: text= inline) + the
        // generic text body walker (multi-run / multi-paragraph fall-through).
        var cxnParagraphs = (full.Children ?? new List<DocumentNode>())
            .Where(c => c.Type == "paragraph" || c.Type == "p").ToList();
        bool seededFirstParaHasRun = false;
        if (cxnParagraphs.Count > 0)
        {
            var firstPara = cxnParagraphs[0];
            var firstParaRuns = (firstPara.Children ?? new List<DocumentNode>())
                .Where(c => c.Type == "run" || c.Type == "r").ToList();
            // Collapse the simplest case (one paragraph, one run, no other
            // children) onto an inline `text=` prop so the connector add ships
            // a complete label without a follow-up paragraph/run op chain.
            if (cxnParagraphs.Count == 1
                && firstParaRuns.Count == 1
                && (firstPara.Children?.Count ?? 0) == 1
                && !string.IsNullOrEmpty(firstParaRuns[0].Text))
            {
                props["text"] = firstParaRuns[0].Text!;
                seededFirstParaHasRun = true;
                // Drop the children from `full` so EmitTextBody downstream
                // doesn't re-emit them — text= already covers the round-trip.
                full.Children = new List<DocumentNode>();
            }
            else if (firstParaRuns.Count >= 1 && !string.IsNullOrEmpty(firstParaRuns[0].Text))
            {
                // Multi-run / multi-paragraph case: still seed the first run
                // via inline `text=` so the connector lands with a paragraph
                // already present; subsequent runs / paragraphs append via
                // EmitTextBody's `add` ops.
                props["text"] = firstParaRuns[0].Text!;
                seededFirstParaHasRun = true;
            }
        }

        // R24 — NodeBuilder emits startShape / endShape as raw OOXML shape IDs.
        // Replay reassigns IDs through AcquireShapeId, so the original numeric
        // ID will reference the wrong shape (or be out of range) by the time
        // Add runs on a fresh deck. Translate to the positional path form that
        // ResolveShapeId already accepts (`/slide[N]/shape[K]`) so the endpoint
        // re-resolves against whatever shape sits at that ordinal in the
        // rebuilt slide. The translation is done eagerly against the source
        // slide because the source still has the original IDs.
        TranslateConnectorEndpoint(ppt, cxnNode, props, "startShape", "from");
        TranslateConnectorEndpoint(ppt, cxnNode, props, "endShape", "to");

        // CONSISTENCY(connector-arrow-recurse): NodeBuilder.ConnectorToNode
        // reads BOTH a:headEnd and a:tailEnd off the outline, but a fuzzer
        // round-trip found cases where the source's <a:headEnd type="..."/>
        // dropped on replay — only the tail-end survived. Defensive: lift
        // headEnd and tailEnd out of the inline `add connector` bag and
        // re-emit them via a deferred `set` once the connector exists. The
        // dedicated Set cases (`case "headend"`, `case "tailend"`) reapply
        // them after the outline's other children settle, so the schema
        // order (fill → prstDash → headEnd → tailEnd) is always respected
        // independent of any Add-side append-order edge case.
        var deferredArrows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in new[] { "headEnd", "tailEnd" })
        {
            if (props.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
            {
                deferredArrows[key] = v;
                props.Remove(key);
            }
        }

        items.Add(new BatchItem
        {
            Command = "add",
            Parent = parentSlidePath,
            Type = "connector",
            Props = props.Count > 0 ? props : null,
        });

        if (deferredArrows.Count > 0)
        {
            // Connector's replay path: parentSlidePath + /connector[K] where
            // K is the connectorOrdinal the caller assigned when walking the
            // parent's children in document order — the SAME ordinal the
            // `add connector` above lands at. Build it positionally rather
            // than reusing cxnNode.Path: NodeBuilder emits the source path in
            // @id= form (`/slide[N]/group[@id=10]/connector[@id=532]`), and
            // group descendants have their cNvPr id reassigned on replay (see
            // CONSISTENCY(group-id-autoassign)), so an @id= selector for a
            // group-nested connector resolves to nothing — the deferred
            // arrowhead `set` then fails with "No connector found with @id=N".
            // The positional form survives because AddConnector lands the
            // connector at exactly connectorOrdinal under parentSlidePath.
            var replayPath = $"{parentSlidePath}/connector[{connectorOrdinal}]";
            ctx.DeferredLinks.Add(new BatchItem
            {
                Command = "set",
                Path = replayPath,
                Props = deferredArrows,
            });
        }

        // R57 bt-4: emit follow-up paragraph/run ops for connector labels that
        // didn't collapse onto inline `text=`. EmitTextBody under a connector
        // path resolves through AddParagraph / AddRun's connector branches.
        if ((full.Children?.Count ?? 0) > 0)
        {
            // Same positional rationale as the deferred-arrow path above:
            // group-nested connectors carry an @id= source path whose id is
            // reassigned on replay, so label paragraph/run ops must target the
            // connector by its document-order ordinal under parentSlidePath.
            var cxnReplayPath = $"{parentSlidePath}/connector[{connectorOrdinal}]";
            EmitTextBody(ppt, full, cxnReplayPath, items, seededFirstParaHasRun: seededFirstParaHasRun, ctx: ctx);
        }
    }

    private static void TranslateConnectorEndpoint(PowerPointHandler ppt,
        DocumentNode cxnNode, Dictionary<string, string> props,
        string srcKey, string dstKey)
    {
        if (!props.TryGetValue(srcKey, out var idStr)) return;
        if (!uint.TryParse(idStr, out var id)) return;
        // cxnNode.Path is /slide[N]/connector[K]; derive the slide number.
        var slideMatch = System.Text.RegularExpressions.Regex.Match(cxnNode.Path ?? "", @"^/slide\[(\d+)\]");
        if (!slideMatch.Success) return;
        var slideIdx = int.Parse(slideMatch.Groups[1].Value);
        var shapePathIdx = ppt.ResolveShapeOrdinalById(slideIdx, id);
        if (shapePathIdx == null) return; // Endpoint refers to a shape we
                                          // can't find on this slide (cross-
                                          // slide cxn, group-nested, etc.);
                                          // leave the raw id and let Add
                                          // emit a warning instead.
        props.Remove(srcKey);
        props[dstKey] = $"/slide[{slideIdx}]/shape[{shapePathIdx}]";
        // bt-6: <a:stCxn idx="M"/> / <a:endCxn idx="M"/> identifies the
        // exact connection-site on the target shape (per-preset glue points:
        // top/right/bottom/left/center for a rect, multiple anchors for
        // complex prsts). Previously the auxiliary index was dropped on
        // the assumption Add would re-derive it, but AddConnector's resolver
        // has no way to recover the source's pinned anchor — every replayed
        // connector landed on anchor 0 (top-center for most presets),
        // breaking the visual routing of source-authored diagrams. Rename
        // startIdx → fromIdx and endIdx → toIdx so the connector emit bag
        // stays internally consistent (startShape → from, endShape → to is
        // the same key-pair renaming the connector emit already does for
        // the shape ref) and surface to AddConnector.
        var idxKey = srcKey == "startShape" ? "startIdx" : "endIdx";
        var renamedIdxKey = dstKey == "from" ? "fromIdx" : "toIdx";
        if (props.TryGetValue(idxKey, out var idxVal))
        {
            props.Remove(idxKey);
            props[renamedIdxKey] = idxVal;
        }
    }

    private static void EmitGroup(PowerPointHandler ppt, DocumentNode grpNode, string parentSlidePath,
                                  string replayPath, List<BatchItem> items, SlideEmitContext ctx,
                                  int depth = 0)
    {
        // CONSISTENCY(dos-hardening): nested-group emission recurses with no
        // structural bound; a crafted deeply-nested grpSp would otherwise hang
        // (or overflow the stack) during `dump`. See DocumentLimits.
        OfficeCli.Core.DocumentLimits.EnsureDepth(depth);

        var full = ppt.Get(grpNode.Path);
        var props = FilterEmittableProps(full.Format);
        // CONSISTENCY(zorder): direct Get on /slide[N]/group[K] strips zorder
        // because the NodeBuilder branch that emits it only runs when the
        // group surfaces as a *child* of the slide enumeration (the source
        // grpNode passed in). Without preserving zorder, a slide with
        // [group, shape] at zorders [1, 2] replays as [shape, group] = [1, 2]
        // — the group lands AFTER the shape because AddGroup defaults to
        // append. Mirror group.json (now declares add/set=true on zorder).
        if (!props.ContainsKey("zorder")
            && grpNode.Format.TryGetValue("zorder", out var grpZ) && grpZ != null)
        {
            var s = grpZ.ToString();
            if (!string.IsNullOrEmpty(s)) props["zorder"] = s!;
        }
        // Preserve the group's OWN cNvPr id. Unlike the group's children (whose
        // ids are stripped below to dodge sibling-id collisions and are only ever
        // resolved positionally), the group's id IS externally referenced — slide
        // animation timing targets the group via <p:spTgt spid="N">. Dropping it
        // (auto-assign on replay) left the raw-set timing block pointing at a
        // non-existent shape id, and PowerPoint refused the whole file. The
        // group-as-slide-child node (grpNode) carries the id in its Format; the
        // direct Get used for `props` above does not, so pull it from grpNode.
        if (!props.ContainsKey("id")
            && grpNode.Format.TryGetValue("id", out var grpIdObj) && grpIdObj != null)
        {
            var s = grpIdObj.ToString();
            if (!string.IsNullOrEmpty(s)) props["id"] = s!;
        }
        DeferSlideJumpLink(props, replayPath, ctx);

        items.Add(new BatchItem
        {
            Command = "add",
            Parent = parentSlidePath,
            Type = "group",
            Props = props.Count > 0 ? props : null,
        });

        if (full.Children == null) return;

        // CONSISTENCY(group-id-autoassign): track the items boundary so we
        // can post-strip `id` from every prop bag emitted for this group's
        // descendants. cNvPr ids in a source group often collide with ids
        // already claimed by sibling slide-level shapes (PowerPoint reuses
        // small-range ids — 10025, etc. — when authoring groups), so
        // AcquireShapeId throws on replay before the group's children are
        // even placed. Auto-assignment via GenerateUniqueShapeId (10000+
        // counter) sidesteps the collision; group-descendant shapes are
        // resolved positionally on subsequent Set ops, so the auto-assigned
        // id is never externally referenced. Recursive EmitGroup calls
        // delegate the same post-strip to their own scope; the inner pass
        // is a no-op when the descendants already had their id stripped.
        int groupChildItemsStart = items.Count;

        // Group children resolve through the same dispatch as slide-level
        // children. Replay parent for the group's children is the group's
        // positional path; children get fresh ordinals within the group scope.
        var ord = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in full.Children)
        {
            switch (child.Type)
            {
                case "textbox":
                case "title":
                case "shape":
                case "equation":
                    ord["shape"] = ord.GetValueOrDefault("shape", 0) + 1;
                    EmitShape(ppt, child, replayPath, $"{replayPath}/shape[{ord["shape"]}]", items, ctx);
                    break;
                case "connector":
                    ord["connector"] = ord.GetValueOrDefault("connector", 0) + 1;
                    EmitConnector(ppt, child, replayPath, items, ctx, ord["connector"]);
                    break;
                case "group":
                    ord["group"] = ord.GetValueOrDefault("group", 0) + 1;
                    EmitGroup(ppt, child, replayPath, $"{replayPath}/group[{ord["group"]}]", items, ctx, depth: depth + 1);
                    break;
                case "placeholder":
                    // CONSISTENCY(unified-shape-counter): placeholders and
                    // plain shapes share <p:sp> sibling positions.
                    ord["shape"] = ord.GetValueOrDefault("shape", 0) + 1;
                    EmitPlaceholder(ppt, child, replayPath, $"{replayPath}/shape[{ord["shape"]}]", items, ctx);
                    break;
                // Group children mirror the slide-level dispatch in
                // EmitSlide: a group can host any shape-tree leaf its parent
                // slide can. Pre-R12 the switch fell through to the
                // "deferred to PR2" warning for picture/table/chart, which
                // silently dropped every picture in `Add picture parent=/slide[N]/group[K]`
                // round-trips. Replay parent for the typed Add is the group's
                // positional path (`replayPath`), so the emitted ordinal
                // matches what AddPicture / AddTable / AddChart produce
                // when targeted at a group parent.
                case "picture":
                    ord["picture"] = ord.GetValueOrDefault("picture", 0) + 1;
                    EmitPicture(ppt, child, replayPath, $"{replayPath}/picture[{ord["picture"]}]", items, ctx);
                    break;
                case "table":
                    ord["table"] = ord.GetValueOrDefault("table", 0) + 1;
                    EmitTable(ppt, child, replayPath, $"{replayPath}/table[{ord["table"]}]", items, ctx);
                    break;
                case "chart":
                    ord["chart"] = ord.GetValueOrDefault("chart", 0) + 1;
                    EmitChart(ppt, child, replayPath, items, ctx, ord["chart"]);
                    break;
                default:
                    ctx.Unsupported.Add(new UnsupportedWarning(
                        Element: child.Type ?? "unknown",
                        SlidePath: replayPath,
                        Reason: "group child type deferred to PR2 / unrecognized"));
                    break;
            }
        }

        // Reassign `id` on every `add` BatchItem emitted between the boundary
        // and now — these are the group's descendants. Set ops within the
        // group reference shapes positionally and carry no `id`, so they're
        // untouched. Previously the id was STRIPPED (→ replay auto-assign), but
        // auto-assign (base 100000) collides on decks with authored top-level
        // ids above that base: a stripped child auto-assigns max+1 = a sibling's
        // preserved source id, throwing "id already in use" and cascading the
        // rest of the group. Assign an explicit high-range id instead — group
        // descendants are resolved positionally, so the value is never
        // externally referenced. Skip items already in the high range (a nested
        // group's recursive pass reassigned them first).
        for (int gi = groupChildItemsStart; gi < items.Count; gi++)
        {
            var bi = items[gi];
            if (bi.Command == "add" && bi.Props != null && bi.Props.ContainsKey("id"))
            {
                if (bi.Props.TryGetValue("id", out var idv)
                    && uint.TryParse(idv, out var idn)
                    && idn >= SlideEmitContext.GroupChildIdBase)
                    continue; // already reassigned by a nested EmitGroup
                bi.Props["id"] = ctx.NextGroupChildId().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    // Walk an emitted shape's text body. Each paragraph becomes an `add
    // paragraph` entry under the shape; runs become `add run` children of the
    // paragraph (with text carried as the canonical "text" prop). Single-run
    // paragraphs collapse run props onto the paragraph itself, mirroring the
    // docx single-run optimization.
    private static void EmitTextBody(PowerPointHandler ppt, DocumentNode shapeNode, string shapeParent, List<BatchItem> items,
                                     bool seededFirstParaHasRun = true, SlideEmitContext? ctx = null)
    {
        if (shapeNode.Children == null) return;
        var paragraphs = shapeNode.Children.Where(c => c.Type == "paragraph" || c.Type == "p").ToList();
        if (paragraphs.Count == 0) return;
        // shapeParent is the positional replay path (e.g. /slide[1]/shape[2]),
        // computed by the caller from per-slide ordinal counters. Replaces
        // the previous shapeNode.Path which carried @id= and broke replay.

        int pIdx = 0;
        foreach (var para in paragraphs)
        {
            pIdx++;
            // PPTX-SPECIFIC(shape-auto-empty-paragraph): AddShape / AddTextbox /
            // AddPlaceholder seed the txBody with one empty <a:p>. If we emit
            // every paragraph as `add`, replay produces an off-by-one empty
            // paragraph[1] that accumulates across round-trips. So the first
            // paragraph under a shape rewrites the seeded one via `set`, and
            // subsequent paragraphs append via `add`. docx body has no
            // equivalent auto-empty seed (AddSection initializes an empty body
            // and AddParagraph appends), so WordBatchEmitter uses pure `add`.
            EmitParagraph(ppt, para, shapeParent, pIdx, items,
                firstParagraph: pIdx == 1,
                seededParaHasRun: pIdx == 1 && seededFirstParaHasRun,
                ctx: ctx);
        }
    }

    private static void EmitParagraph(PowerPointHandler ppt, DocumentNode paraNode, string shapeParent,
                                      int paraIdx, List<BatchItem> items, bool firstParagraph,
                                      bool seededParaHasRun = true,
                                      SlideEmitContext? ctx = null)
    {
        var props = FilterEmittableProps(paraNode.Format);
        // CONSISTENCY(slide-jump-defer): the shape-level emit already deferred
        // the canonical `set link=slide[N]`; strip slide-jump links from any
        // bubbled-through para/run bag so the inline set doesn't fire too
        // early and trip "Slide jump target out of range".
        DummyCtxStripSlideJump(props);
        var runs = (paraNode.Children ?? new List<DocumentNode>())
            .Where(c => c.Type == "run" || c.Type == "r").ToList();
        // <a:br> hard line breaks live as siblings of <a:r> in the paragraph;
        // NodeBuilder surfaces them as Type="linebreak" children in source
        // document order. Preserve that order during the multi-child emit
        // path so `add linebreak` interleaves between runs correctly.
        var paraChildrenSeq = (paraNode.Children ?? new List<DocumentNode>())
            .Where(c => c.Type == "run" || c.Type == "r" || c.Type == "linebreak" || c.Type == "br")
            .ToList();
        bool hasLineBreaks = paraChildrenSeq.Any(c => c.Type == "linebreak" || c.Type == "br");

        // CONSISTENCY(single-run-collapse): mirrors WordBatchEmitter.Paragraph
        // collapseSingleRun — fold a lone run's text + char props onto the
        // paragraph add so simple cases stay one BatchItem. Suppress the
        // collapse when the paragraph also carries <a:br> children — the
        // single-run shortcut never emits the break.
        bool collapseSingleRun = runs.Count == 1
            && (paraNode.Children?.Count ?? 0) == 1
            && !hasLineBreaks;

        if (collapseSingleRun)
        {
            var runProps = FilterEmittableProps(runs[0].Format);
            DummyCtxStripSlideJump(runProps);
            // R24 — run-only rPr attributes (lang, altLang, kern, kumimoji,
            // normalizeH, smtClean, smtId, bmk, dirty, err, baseline) are not
            // valid on a:pPr. The collapse used to dump them onto the
            // paragraph set, which then routed them into `unsupported`. Split
            // them out and apply them via a follow-up `set …/run[1]` so the
            // round-trip still captures the rPr attribute on the right node.
            var runOnly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in RunOnlyRprAttrs)
            {
                if (runProps.TryGetValue(k, out var v))
                {
                    runOnly[k] = v;
                    runProps.Remove(k);
                }
            }
            foreach (var (k, v) in runProps)
            {
                if (!props.ContainsKey(k)) props[k] = v;
            }
            if (!string.IsNullOrEmpty(runs[0].Text))
                props["text"] = runs[0].Text!;
            string collapsedParaPath;
            if (firstParagraph)
            {
                // First paragraph is already seeded by AddShape/AddTextbox.
                // Skip the no-op `set` with empty props (batch rejects
                // `props:null`). Second+ paragraphs always need `add` so
                // the row count still grows.
                if (props.Count > 0)
                {
                    items.Add(new BatchItem
                    {
                        Command = "set",
                        Path = $"{shapeParent}/paragraph[1]",
                        Props = props,
                    });
                }
                collapsedParaPath = $"{shapeParent}/paragraph[1]";
            }
            else
            {
                items.Add(new BatchItem
                {
                    Command = "add",
                    Parent = shapeParent,
                    Type = "paragraph",
                    Props = props.Count > 0 ? props : null,
                });
                collapsedParaPath = $"{shapeParent}/paragraph[{paraIdx}]";
            }
            if (runOnly.Count > 0)
            {
                // CONSISTENCY(empty-paragraph-no-run): when the paragraph carried
                // no text and the run-only attrs reduce to AddRun's hard-coded
                // seeds (lang/altLang), the noise concern is double-emit on
                // multi-empty-run paragraphs. The original short-circuit ALSO
                // dropped the single-empty-run case — which silently collapses
                // <a:p><a:r><a:rPr lang="en-US"/><a:t/></a:r></a:p> (the form
                // PowerPoint writes for an empty line, load-bearing for IME and
                // cursor positioning) into <a:p><a:endParaRPr/></a:p> on replay.
                // Keep the skip only when the seeded paragraph already has an
                // <a:r> at run[1]; for placeholders/textboxes whose seed is
                // endParaRPr-only, emit `add run` with empty text so the
                // resulting paragraph carries the explicit run element.
                bool collapseHasText = props.ContainsKey("text");
                if (!collapseHasText
                    && runOnly.Keys.All(k => RunDefaultOnlyKeys.Contains(k))
                    && (!firstParagraph || seededParaHasRun))
                {
                    return;
                }
                if (firstParagraph && !seededParaHasRun && !collapseHasText)
                {
                    items.Add(new BatchItem
                    {
                        Command = "add",
                        Parent = collapsedParaPath,
                        Type = "run",
                        Props = runOnly,
                    });
                }
                else
                {
                    items.Add(new BatchItem
                    {
                        Command = "set",
                        Path = $"{collapsedParaPath}/run[1]",
                        Props = runOnly,
                    });
                }
            }
            return;
        }

        // Multi-run path: emit the paragraph empty (or with paragraph-level
        // props only) then a run per child. First paragraph rewrites the
        // shape's auto-seeded empty <a:p> via `set`; later paragraphs append.
        string paraParent;
        if (firstParagraph)
        {
            // First paragraph already seeded by AddShape/AddTextbox; skip
            // the no-op `set` when there are no paragraph-level props
            // (batch rejects `props:null`). Runs will still be emitted
            // against /paragraph[1] below.
            if (props.Count > 0)
            {
                items.Add(new BatchItem
                {
                    Command = "set",
                    Path = $"{shapeParent}/paragraph[1]",
                    Props = props,
                });
            }
            paraParent = $"{shapeParent}/paragraph[1]";
        }
        else
        {
            items.Add(new BatchItem
            {
                Command = "add",
                Parent = shapeParent,
                Type = "paragraph",
                Props = props.Count > 0 ? props : null,
            });
            // Target parent path for runs is the just-emitted paragraph at
            // its known positional index (paraIdx). Earlier code used
            // paragraph[last()] but the resolver doesn't walk into a
            // placeholder's txBody to find paragraphs, so explicit index
            // is the portable form.
            // /body/p[last()].
            paraParent = $"{shapeParent}/paragraph[{paraIdx}]";
        }

        // R8-7: AddShape / AddTextbox / AddPlaceholder seed the txBody with
        // one paragraph carrying one empty <a:r>. If we emit every run as
        // `add`, replay produces a phantom empty run[1] before our content
        // and drifts by +1 run per round-trip. Mirror the single-paragraph
        // rewrite: the FIRST run of the FIRST paragraph rewrites the seeded
        // empty run via `set .../run[1]` rather than `add run`.
        // AddPlaceholder's seeded paragraph has zero <a:r> elements (only
        // <a:endParaRPr>), so `set run[1]` would target a missing run. Only
        // rewrite-the-seed when an actual run was seeded (shape/textbox path).
        bool firstRunOnSeededParagraph = firstParagraph && runs.Count > 0 && seededParaHasRun;
        int riCounter = 0;
        bool firstRunHandled = false;
        foreach (var child in paraChildrenSeq)
        {
            if (child.Type == "run" || child.Type == "r")
            {
                if (!firstRunHandled && firstRunOnSeededParagraph)
                {
                    EmitFirstRunAsSet(child, paraParent, items, ctx);
                    firstRunHandled = true;
                }
                else
                {
                    EmitRun(child, paraParent, items, ctx, runIndex: riCounter + 1);
                    firstRunHandled = true;
                }
                riCounter++;
            }
            else if (child.Type == "linebreak" || child.Type == "br")
            {
                // <a:br> hard line break inside the paragraph. AddLineBreak
                // (Add.Text.cs) inserts at the end by default; per-position
                // ordering relative to runs is what gives the visual line
                // wrap, so emit the `add linebreak` AFTER the runs that
                // precede it in source. The `--index` arg is not required
                // because AddLineBreak appends, and we're walking children
                // in source order — each linebreak lands where it was.
                items.Add(new BatchItem
                {
                    Command = "add",
                    Parent = paraParent,
                    Type = "linebreak",
                });
            }
        }
    }

    // R8-7: rewrite the seeded <a:r> via `set` rather than appending another
    // one. Mirrors EmitRun but emits a single set item against
    // <paraParent>/run[1]. Empty/lang-only seeded runs in the source are
    // filtered the same way EmitRun filters; an empty rewrite is a no-op set
    // with no props.
    private static void EmitFirstRunAsSet(DocumentNode runNode, string paraParent, List<BatchItem> items,
                                          SlideEmitContext? ctx = null)
    {
        var props = FilterEmittableProps(runNode.Format);
        if (ctx != null) DeferRunSlideJumpLink(props, paraParent, 1, ctx);
        else DummyCtxStripSlideJump(props);
        bool hasText = !string.IsNullOrEmpty(runNode.Text);
        // CONSISTENCY(empty-run-preserve): with seeded-has-run paragraphs the
        // <a:r> at run[1] is already present, so an empty-text run with only
        // default lang/altLang attrs is a true no-op `set`. Skip to avoid
        // pointless noise. The single-empty-run-as-only-child case is handled
        // by the collapse branch in EmitParagraph — see CONSISTENCY note there.
        if (!hasText && props.Count > 0
            && props.Keys.All(k => RunDefaultOnlyKeys.Contains(k)))
            return;
        if (!hasText && props.Count == 0) return;
        if (hasText) props["text"] = runNode.Text!;
        items.Add(new BatchItem
        {
            Command = "set",
            Path = $"{paraParent}/run[1]",
            Props = props.Count > 0 ? props : null,
        });
    }

    // Run-level Format keys that AddRun seeds on every new <a:r> regardless
    // of caller input — emitting them adds nothing but noise on round-trip
    // AND triggers drift when the source had MORE than one default-only run.
    // `lang` is the canonical culprit: AddRun hard-codes Language="en-US",
    // so a paragraph carrying N empty <a:r> elements with only lang=en-US
    // produces N+M runs on every dump→replay (M = newly-seeded defaults on
    // the freshly-added paragraph). Treat the empty/lang-only run as a
    // no-op marker and skip it entirely.
    private static readonly HashSet<string> RunDefaultOnlyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "lang", "altLang",
    };

    private static void EmitRun(DocumentNode runNode, string paraParent, List<BatchItem> items,
                                SlideEmitContext? ctx = null, int runIndex = 0)
    {
        var props = FilterEmittableProps(runNode.Format);
        // Defer run-level slide-jump links the same way shape-level links are
        // deferred — emit a follow-up `set` BatchItem against the run path
        // once every target slide is materialized. Without this, run-internal
        // `link=slide[N]` was silently stripped and the rendered run lost its
        // hyperlink on replay. External URLs / named actions / mailto stay in
        // the run prop bag and AddRun.ApplyRunHyperlink handles them inline.
        if (ctx != null) DeferRunSlideJumpLink(props, paraParent, runIndex, ctx);
        else DummyCtxStripSlideJump(props);
        bool hasText = !string.IsNullOrEmpty(runNode.Text);

        // Drop runs that carry no text and only default attributes AddRun
        // would seed anyway. Without this, a deck with N lang-only empty
        // runs accumulates N more on each round-trip — the source's N stay
        // (faithfully re-emitted), and AddRun's hard-coded Language="en-US"
        // seeds a fresh lang on every newly-added <a:r>, so the next dump
        // sees N+M runs per paragraph and drifts by M each cycle.
        if (!hasText && props.Count > 0
            && props.Keys.All(k => RunDefaultOnlyKeys.Contains(k)))
        {
            return;
        }
        // Fully empty <a:r> (no text, no props after filtering) — same
        // logic: AddRun would just seed its defaults, no useful content
        // round-trips.
        if (!hasText && props.Count == 0)
            return;

        if (hasText)
            props["text"] = runNode.Text!;

        items.Add(new BatchItem
        {
            Command = "add",
            Parent = paraParent,
            Type = "run",
            Props = props.Count > 0 ? props : null,
        });
    }

    // CONSISTENCY(placeholder-id-preserve-on-spTgt-ref): per-slide one-shot
    // scan of the raw <p:timing> tree for every cNvPr id named by a
    // <p:spTgt spid="N"/>. The full timing tree may travel as raw-set
    // passthrough on exotic content, so the literal spid in that XML must
    // match the placeholder cNvPr.Id we actually emit. Cached on the
    // SlideEmitContext to keep EmitPlaceholder O(1) past the first call.
    private static HashSet<uint> GetSlideSpTgtIds(
        PowerPointHandler ppt, string slidePath, SlideEmitContext ctx)
    {
        if (ctx.SlideTimingSpTgtIds.TryGetValue(slidePath, out var cached))
            return cached;
        var ids = new HashSet<uint>();
        string xml;
        try { xml = ppt.Raw(slidePath); }
        catch { ctx.SlideTimingSpTgtIds[slidePath] = ids; return ids; }
        // Cheap regex over the slide-level <p:timing> slice; safe because
        // any prefix that resolves to the OOXML pres namespace lands as
        // `p:spTgt` (the package writer doesn't realias `p`).
        var rx = new System.Text.RegularExpressions.Regex(
            @"<p:spTgt\s+spid=""(\d+)""",
            System.Text.RegularExpressions.RegexOptions.Compiled);
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(xml))
        {
            if (uint.TryParse(m.Groups[1].Value, out var n))
                ids.Add(n);
        }
        ctx.SlideTimingSpTgtIds[slidePath] = ids;
        return ids;
    }
}
