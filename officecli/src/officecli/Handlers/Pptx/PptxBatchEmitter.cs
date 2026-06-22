// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

// CONSISTENCY(emit-X-mirror): scaffold mirrors WordBatchEmitter.cs — same
// public entry shape (full-doc + subtree overloads), same Get-driven
// transcription, same partial-class split (entry / Filters / Shape / Notes).
//
// PR1 scope (text-only): slide / shape / textbox / title / connector /
// group / placeholder + paragraph + run. Tables, pictures, charts, notes
// bodies, layout/master/theme raw — PR2.
public static partial class PptxBatchEmitter
{
    /// <summary>
    /// Carry-state for one emit run. Mirrors WordBatchEmitter.BodyEmitContext
    /// but trimmed for PR1 (no footnote/endnote/chart cursors yet —
    /// PowerPoint has no notes-with-numbering concept; chart/table content
    /// lands in PR2).
    /// </summary>
    internal sealed record SlideEmitContext(
        List<UnsupportedWarning> Unsupported)
    {
        // Forward slide-jump links (e.g. shape[1] on slide[1] linking to
        // slide[3]) must replay AFTER every slide is added — otherwise the
        // `link=slide[N]` prop on shape Add resolves against a deck where
        // the target slide does not yet exist and ResolveHyperlinkTarget
        // throws "Slide jump target out of range". Defer those props into
        // a second set-pass appended at the end of EmitPptx.
        public List<BatchItem> DeferredLinks { get; } = new();

        // CONSISTENCY(placeholder-id-preserve-on-spTgt-ref): cache the set
        // of cNvPr ids that the slide's raw <p:timing> tree references via
        // <p:spTgt spid="N"/>. Used by EmitPlaceholder to keep the original
        // id on a placeholder whose id is the target of an animation (raw-
        // set timing slice keeps the literal spid; if the placeholder were
        // auto-assigned a fresh 10000+ id, every spTgt would dangle).
        // Lazily populated per slidePath.
        public Dictionary<string, HashSet<uint>> SlideTimingSpTgtIds { get; } =
            new(StringComparer.Ordinal);

        // CONSISTENCY(group-id-autoassign): group-descendant cNvPr ids are
        // reassigned to explicit values from this HIGH range (instead of being
        // stripped → replay auto-assign). Replay's auto-assign base is 100000,
        // which collides on decks whose authored TOP-LEVEL ids exceed it (seen:
        // 124930+): a stripped group child auto-assigns max+1 = a value a sibling
        // top-level shape preserves LATER in the same slide, throwing "id already
        // in use". Group-descendant ids are never externally referenced (Set ops
        // resolve them positionally), so any unique value is safe; this base sits
        // above every realistic PowerPoint-authored id, and being monotonic it is
        // unique across the whole emit.
        public const uint GroupChildIdBase = 2_000_000_000u;
        private uint _groupChildId = GroupChildIdBase;
        public uint NextGroupChildId() => _groupChildId++;
    }

    /// <summary>
    /// Captured at emit time when a slide carries content we cannot round-trip
    /// through the existing handler vocabulary (animations, SmartArt, OLE,
    /// video/audio, exotic transitions). The slide itself is emitted; the
    /// unsupported element is dropped silently from `items` but recorded
    /// here so the CLI can surface a warning bundle to the caller.
    /// </summary>
    public sealed record UnsupportedWarning(string Element, string SlidePath, string Reason);

    /// <summary>
    /// Emit a full PowerPoint document as a sequence of BatchItem rows.
    /// Returns the items plus any unsupported-element warnings.
    /// </summary>
    public static (List<BatchItem> Items, List<UnsupportedWarning> Warnings) EmitPptx(PowerPointHandler ppt)
    {
        var items = new List<BatchItem>();
        var ctx = new SlideEmitContext(new List<UnsupportedWarning>());

        // Clear the target deck's slides FIRST so replay onto a non-empty
        // target lands on a clean slate. Without this, `add slide` items
        // append after existing slides while every `add shape parent=/slide[N]`
        // path still resolves to the original slide[N] — the target ends up
        // with 2× the slide count (existing + freshly added empties) on each
        // round-trip. `remove /slide[*]` is a no-op on a deck with 0 slides,
        // so this is safe for the clean-target case too.
        items.Add(new BatchItem { Command = "remove", Path = "/slide[*]" });

        // Resource parts FIRST — theme, notesMaster, masters, layouts.
        // Order matters: replay's raw-set must overwrite the blank deck's
        // seeded baseline before slide content is added so per-slide
        // layout refs (sld@layout="rId4") resolve against the source's
        // layout set, not blank's. Mirrors docx's
        // settings → theme → numbering → styles → body ordering.
        EmitThemeRaw(ppt, items);
        EmitNotesMasterRaw(ppt, items);
        EmitMasterRaw(ppt, items);
        EmitLayoutRaw(ppt, items);
        // R8-5: emit presentation-level slide dimensions so custom sldSz
        // round-trips through dump → batch. Previously EmitPptx skipped the
        // root node entirely; replay always landed on the blank-deck default
        // (33.87cm × 19.05cm widescreen), silently resizing decks built for
        // 4:3, A4, custom banners, etc.
        EmitPresentationProps(ppt, items);

        // CONSISTENCY(slide-order): always iterate via the handler's
        // GetSlideParts() (sldIdLst-driven). Walking SlideParts off the
        // package returns parts in zip URI order — `slide12.xml` sorts
        // before `slide3.xml`, scrambling user-visible order.
        // CONSISTENCY(emit-skip-on-validate): a non-standard attribute or
        // element on a single slide must not abort the whole dump. The
        // OpenXml SDK throws a flat InvalidOperationException ("The element
        // does not allow the specified attribute.") when its strict-mode
        // validator catches a foreign/extension attribute (common in vendor
        // templates: gov_bja_template, 1.pptx, ...). Iterate slides one by
        // one and surface OOXML validation failures as unsupported_element
        // warnings instead of crashing the whole dump.
        var slideCount = ppt.SlideCount;
        for (int slideNum = 1; slideNum <= slideCount; slideNum++)
        {
            var slidePath = $"/slide[{slideNum}]";
            // CONSISTENCY(slide-ordinal-stub): every iteration MUST contribute
            // exactly one `add slide` so subsequent set paths /slide[N+1]/…
            // resolve to the same N+1 slot on replay. Pre-R5 we just
            // `continue`d on validation failure, emitting zero items for the
            // skipped slide — every later set drifted by one slot and
            // dump → batch on a deck with one bad slide could orphan
            // hundreds of items.
            DocumentNode slideNode;
            int preCount = items.Count;
            try { slideNode = ppt.Get(slidePath); }
            catch (Exception ex) when (ex.Message.Contains("does not allow", StringComparison.Ordinal)
                                    || ex.Message.Contains("not allowed", StringComparison.Ordinal))
            {
                ctx.Unsupported.Add(new UnsupportedWarning(
                    Element: "slide.ooxml_validation",
                    SlidePath: slidePath,
                    Reason: ex.Message));
                items.Add(new BatchItem { Command = "add", Parent = "/", Type = "slide" });
                continue;
            }
            try
            {
                EmitSlide(ppt, slideNode, slideNum, items, ctx);
            }
            catch (Exception ex) when (ex.Message.Contains("does not allow", StringComparison.Ordinal)
                                    || ex.Message.Contains("not allowed", StringComparison.Ordinal))
            {
                ctx.Unsupported.Add(new UnsupportedWarning(
                    Element: "slide.ooxml_validation",
                    SlidePath: slidePath,
                    Reason: ex.Message));
                // Roll back partial emits from the failing slide and replace
                // with a single blank-slide stub to keep ordinals aligned.
                if (items.Count > preCount)
                    items.RemoveRange(preCount, items.Count - preCount);
                items.Add(new BatchItem { Command = "add", Parent = "/", Type = "slide" });
            }
        }

        // Flush deferred slide-jump link sets — every target slide now exists,
        // so `ResolveHyperlinkTarget` can map slide[N] to the relationship.
        if (ctx.DeferredLinks.Count > 0)
            items.AddRange(ctx.DeferredLinks);

        // Best-effort passthrough for presentation-level structural children
        // that the typed emit path doesn't model — custShowLst, extLst
        // (sectionLst / modifyVerifier / …). Runs LAST so the raw-set append
        // lands after `add slide` has populated sldIdLst. References by rId
        // may go stale on replay; UnsupportedWarning surfaces that risk.
        EmitPresentationExtras(ppt, items, ctx);

        // R12a aux-parts: surface a warning per package part the dump surface
        // does not round-trip (tableStyles, viewProps, handoutMasters,
        // printerSettings, customXml, embedded fonts, programmability tags,
        // user docProps). Silent data loss is worse than a noisy warning —
        // the warning channel lets agents/users see exactly which content
        // vanished on dump. Mirrors WordBatchEmitter's R11 aux-part scan.
        // Scoped to full-document dumps only; the subtree EmitPptx overload
        // intentionally omits sibling parts and would otherwise warn every
        // time. See PptxBatchEmitter.AuxParts.cs.
        EmitAuxiliaryPartsScan(ppt, ctx.Unsupported);

        return (items, ctx.Unsupported);
    }

    // tester-2: cheap descendants-vs-default check for a <p:timing> slice
    // whose only signal is "the tnLst carries more than an empty tmRoot
    // <p:cTn>". The literal substring tests in the caller cover the
    // well-known effect elements (<p:set>, <p:anim*>, <p:audio>, <p:video>,
    // motion paths). This helper catches the residual: a tmRoot cTn that
    // has a <p:childTnLst>/<p:iterate>/<p:subTnLst>/<p:stCondLst>/<p:endCondLst>
    // descendant — PowerPoint's container nodes that hold conditional
    // triggers, sub-sequences, or iteration timing that BuildSlideAnimationIndex
    // does not model. Anything inside these containers means there are
    // effects the semantic emit can't reconstruct.
    private static bool HasNonTrivialTimingBody(string slice)
    {
        // The default skeleton is a tmRoot cTn with no children; presence
        // of any of these markers means the timing body is substantive.
        return slice.Contains("<p:childTnLst", StringComparison.Ordinal)
            || slice.Contains("<p:iterate", StringComparison.Ordinal)
            || slice.Contains("<p:subTnLst", StringComparison.Ordinal)
            || slice.Contains("<p:stCondLst", StringComparison.Ordinal)
            || slice.Contains("<p:endCondLst", StringComparison.Ordinal)
            || slice.Contains("<p:tmAbs", StringComparison.Ordinal)
            || slice.Contains("<p:tmPct", StringComparison.Ordinal)
            || slice.Contains("<p:tav", StringComparison.Ordinal)
            || slice.Contains("<p:tgtEl", StringComparison.Ordinal);
    }

    // R8-5: emit a single `set /` carrying slideWidth/slideHeight when the
    // source deck deviates from the blank-baseline widescreen size. The
    // blank-doc default is hard-coded inside BlankDocCreator (SlideSizeDefaults)
    // and not surfaced by Get, so we string-compare the canonical FormatEmu
    // output. These baseline strings are DERIVED from FormatEmu of the same
    // SlideSizeDefaults constants BlankDocCreator writes — never hard-coded —
    // so they track FormatEmu's chosen unit automatically (e.g. width
    // 12192000 EMU emits as "960pt", height 6858000 EMU as "19.05cm"). A
    // literal would silently rot whenever FormatEmu's canonical form changes,
    // re-introducing a spurious slideWidth row on every round-trip.
    // EmitPresentationProps is a no-op for the default case to keep unchanged
    // decks from gaining a spurious item on round-trip.
    // CONSISTENCY(paired-slide-units): mirror PowerPointHandler.Query.cs
    // which uses FormatEmuPaired so width+height share a unit. Without this,
    // DefaultSlideHeight resolved to "19.05cm" while Get / emits "540pt" for
    // the same EMU value, breaking the idempotency guard and re-emitting
    // a spurious `slideHeight=540pt` row on every dump of an unchanged deck.
    private static readonly (string Width, string Height) DefaultSlideSizePair =
        Core.EmuConverter.FormatEmuPaired(
            Core.SlideSizeDefaults.Widescreen16x9Cx,
            Core.SlideSizeDefaults.Widescreen16x9Cy);
    private static readonly string DefaultSlideWidth = DefaultSlideSizePair.Width;
    private static readonly string DefaultSlideHeight = DefaultSlideSizePair.Height;

    // Presentation-level Format keys that TrySetPresentationSetting accepts
    // on `set /`. The Get side surfaces these via PopulatePresentationSettings
    // (Set.Presentation.cs); without this allowlist, only slideWidth/Height
    // round-tripped — firstSlideNum, show.loop, print.*, compatMode, etc.
    // were silently dropped on dump.
    //
    // Get emits `direction = rtl` for RTL presentations but the setter case
    // key is `rtl`. We rewrite the key on emit so replay's TrySetPresentationSetting
    // accepts it. Mirrors the `direction → rtl` alias that already lives in
    // Set.cs path-pattern dispatch.
    private static readonly HashSet<string> PresentationEmitKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "firstSlideNum", "compatMode", "removePersonalInfo",
            "print.what", "print.colorMode", "print.hiddenSlides",
            "print.scaleToFitPaper", "print.frameSlides",
            "show.loop", "show.narration", "show.animation", "show.useTimings",
        };

    private static void EmitPresentationProps(PowerPointHandler ppt, List<BatchItem> items)
    {
        DocumentNode root;
        try { root = ppt.Get("/"); }
        catch { return; }
        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Round-trip the slide-size TYPE, not just cx/cy. A named preset
        // (standard/4:3, widescreen, a4, …) is emitted as slidesize=<preset>,
        // which sets cx/cy/type atomically — emitting only slideWidth/slideHeight
        // forces type="custom", and PowerPoint/officeshot render a custom-typed
        // deck at a different DPI than its screen4x3 source, scaling every page.
        // Custom-typed decks fall through to slideWidth/slideHeight below.
        var slideSizeType = root.Format.TryGetValue("slideSize", out var ssObj) ? ssObj as string : null;
        bool emittedPreset = false;
        if (!string.IsNullOrEmpty(slideSizeType)
            && !string.Equals(slideSizeType, "custom", StringComparison.OrdinalIgnoreCase)
            && OfficeCli.Core.SlideSizeDefaults.Presets.ContainsKey(slideSizeType))
        {
            props["slidesize"] = slideSizeType!;
            emittedPreset = true;
        }
        if (!emittedPreset)
        {
            if (root.Format.TryGetValue("slideWidth", out var wObj) && wObj is string w
                && !string.Equals(w, DefaultSlideWidth, StringComparison.OrdinalIgnoreCase))
                props["slideWidth"] = w;
            if (root.Format.TryGetValue("slideHeight", out var hObj) && hObj is string h
                && !string.Equals(h, DefaultSlideHeight, StringComparison.OrdinalIgnoreCase))
                props["slideHeight"] = h;
        }

        // Presentation attributes / print / show settings — only emit non-default
        // values (Get omits keys that match the OOXML defaults).
        foreach (var key in PresentationEmitKeys)
        {
            if (!root.Format.TryGetValue(key, out var v) || v == null) continue;
            var s = v switch { bool b => b ? "true" : "false", _ => v.ToString() ?? "" };
            if (s.Length == 0) continue;
            props[key] = s;
        }

        // direction → rtl: Get emits `direction = rtl`, setter accepts `rtl`.
        if (root.Format.TryGetValue("direction", out var dObj) && dObj is string ds
            && ds.Equals("rtl", StringComparison.OrdinalIgnoreCase))
            props["rtl"] = "true";

        if (props.Count == 0) return;
        items.Add(new BatchItem
        {
            Command = "set",
            Path = "/",
            Props = props,
        });
    }

    /// <summary>
    /// Emit a subtree of a PowerPoint document. Supported subtree paths:
    /// `/slide[N]`, `/theme`, `/notesMaster`, `/slideMaster[N]`, `/slideLayout[N]`,
    /// `/noteSlide[N]`, `/presentation`. Resource subtrees emit a single raw-set
    /// replace; replay onto a foreign deck does NOT carry cross-part dependency
    /// closure (e.g. a `/slideLayout[K]` dump only stamps the layout's XML — the
    /// referenced master, theme, and per-slide layout rId rewiring are NOT
    /// included). Mirrors WordBatchEmitter's raw-emit subtree surface
    /// (/theme, /settings, /numbering, /styles).
    /// </summary>
    public static (List<BatchItem> Items, List<UnsupportedWarning> Warnings) EmitPptx(
        PowerPointHandler ppt, string path)
    {
        const string SupportedHint = "Supported: /, /presentation, /slide[N], /theme, /notesMaster, /slideMaster[N], /slideLayout[N], /noteSlide[N]";

        if (string.IsNullOrEmpty(path))
            throw new CliException($"dump path cannot be empty. Use '/' for the full document or a subtree path like /slide[N]. {SupportedHint}")
                { Code = "invalid_path" };
        if (path == "/") return EmitPptx(ppt);

        var items = new List<BatchItem>();
        var ctx = new SlideEmitContext(new List<UnsupportedWarning>());

        // CONSISTENCY(case-insensitive-subtree): paths with [N] go through
        // regex `IgnoreCase`, so case-folding the literal-prefix branches too
        // aligns the dispatcher to a single rule and matches the docx subtree
        // dispatcher (WordBatchEmitter uses `path.ToLowerInvariant()`).
        var lp = path.ToLowerInvariant();

        if (lp == "/presentation")
        {
            EmitPresentationProps(ppt, items);
            return (items, ctx.Unsupported);
        }
        if (lp == "/theme")
        {
            EmitThemeRaw(ppt, items);
            return (items, ctx.Unsupported);
        }
        if (lp == "/notesmaster")
        {
            EmitNotesMasterRaw(ppt, items);
            return (items, ctx.Unsupported);
        }

        // Index parsing: regex restricts to ASCII [0-9]+ (not Unicode \d, which
        // matches Arabic-Indic numerals etc. that int.Parse rejects under
        // InvariantCulture). int.TryParse guards against Int32 overflow.
        static int ParseIndexOrThrow(string raw, string fullPath)
        {
            if (!int.TryParse(raw, System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out var n))
                throw new CliException($"dump path not found: {fullPath} (index '{raw}' out of range or not an integer)")
                    { Code = "path_not_found" };
            return n;
        }

        var slideMatch = System.Text.RegularExpressions.Regex.Match(path, @"^/slide\[([0-9]+)\]$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (slideMatch.Success)
        {
            var idx = ParseIndexOrThrow(slideMatch.Groups[1].Value, path);
            DocumentNode slideNode;
            try { slideNode = ppt.Get(path); }
            catch (Exception ex)
            {
                throw new CliException($"dump path not found: {path} ({ex.Message})") { Code = "path_not_found" };
            }
            EmitSlide(ppt, slideNode, idx, items, ctx);
            // CONSISTENCY(deferred-link-flush): subtree slide dump must flush
            // ctx.DeferredLinks before returning, otherwise any `link=slide[N]`
            // prop on a shape inside the dumped slide is silently dropped from
            // the output (DeferSlideJumpLink moves it out of the shape's
            // prop bag into ctx.DeferredLinks expecting the full-doc EmitPptx
            // tail flush, which the subtree path never reaches).
            //
            // Cross-slide targets (e.g. dump /slide[1] when the shape links
            // to /slide[3]) still emit the set row — replay against a deck
            // missing the target slide fails with ResolveHyperlinkTarget's
            // "Slide jump target out of range", which is a clearer error
            // than silent prop loss.
            if (ctx.DeferredLinks.Count > 0)
                items.AddRange(ctx.DeferredLinks);
            return (items, ctx.Unsupported);
        }

        var masterMatch = System.Text.RegularExpressions.Regex.Match(path, @"^/slideMaster\[([0-9]+)\]$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (masterMatch.Success)
        {
            var idx = ParseIndexOrThrow(masterMatch.Groups[1].Value, path);
            if (idx < 1 || idx > ppt.SlideMasterCount)
                throw new CliException($"dump path not found: {path} (total slideMasters: {ppt.SlideMasterCount})")
                    { Code = "path_not_found" };
            if (!EmitMasterRawOne(ppt, idx, items))
                throw new CliException($"dump path not found: {path} (slideMaster {idx} raw read failed)")
                    { Code = "path_not_found" };
            return (items, ctx.Unsupported);
        }

        var layoutMatch = System.Text.RegularExpressions.Regex.Match(path, @"^/slideLayout\[([0-9]+)\]$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (layoutMatch.Success)
        {
            var idx = ParseIndexOrThrow(layoutMatch.Groups[1].Value, path);
            if (idx < 1 || idx > ppt.SlideLayoutCount)
                throw new CliException($"dump path not found: {path} (total slideLayouts: {ppt.SlideLayoutCount})")
                    { Code = "path_not_found" };
            if (!EmitLayoutRawOne(ppt, idx, items))
                throw new CliException($"dump path not found: {path} (slideLayout {idx} raw read failed)")
                    { Code = "path_not_found" };
            return (items, ctx.Unsupported);
        }

        var noteMatch = System.Text.RegularExpressions.Regex.Match(path, @"^/noteSlide\[([0-9]+)\]$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (noteMatch.Success)
        {
            var idx = ParseIndexOrThrow(noteMatch.Groups[1].Value, path);
            if (idx < 1 || idx > ppt.SlideCount)
                throw new CliException($"dump path not found: {path} (total slides: {ppt.SlideCount})")
                    { Code = "path_not_found" };
            if (!EmitNoteSlideRawOne(ppt, idx, items))
                throw new CliException($"dump path not found: {path} (slide {idx} has no notes)")
                    { Code = "path_not_found" };
            return (items, ctx.Unsupported);
        }

        throw new CliException(
            $"dump path not supported: {path}. {SupportedHint}")
            { Code = "unsupported_path" };
    }

    private static void EmitSlide(PowerPointHandler ppt, DocumentNode slideNode, int slideNum,
                                  List<BatchItem> items, SlideEmitContext ctx)
    {
        var slidePath = slideNode.Path;
        // Snapshot: every shape/picture/connector/raw-set row this slide emits
        // is appended to `items` from here on, so items[sliceStart..] is this
        // slide's emitted content — the source of truth for which cNvPr ids
        // actually survive into the rebuilt slide (used to prune dangling
        // <p:timing> targets that point at by-design-dropped shapes).
        int sliceStart = items.Count;
        ProbeUnsupportedOnSlide(ppt, slidePath, ctx);

        // Detect exotic transition / timing content that the semantic emit
        // can't faithfully reproduce (morph + p14/p15 transitions, motion
        // paths, sequence groupings). When present, we suppress the semantic
        // emit for that category and emit a raw-set passthrough at the end
        // of the slide — single source of truth per slide-per-category.
        var exotic = ScanSlideExoticContent(ppt, slidePath);

        // Pull the full slide node so layout / hidden / background etc. surface
        // even when the entry passed us a depth-truncated tree from "/".
        var fullSlide = ppt.Get(slidePath);
        var slideProps = FilterEmittableProps(fullSlide.Format);

        // Re-bind to the EXACT source layout by ordinal rather than the
        // human-facing layout name. Layout names collide (decks routinely carry
        // several "标题幻灯片"/"Title Slide" layouts under different masters);
        // ResolveSlideLayout's name match would pick the first, chaining the
        // slide to the wrong master and dropping any master-level background.
        // The ordinal resolves through ResolveSlideLayout's numeric-index path,
        // which walks the same master→layout enumeration replay reconstructs.
        if (slideProps.ContainsKey("layout"))
        {
            var layoutOrdinal = ppt.GetSlideLayoutOrdinal(slideNum);
            if (layoutOrdinal.HasValue)
                slideProps["layout"] = layoutOrdinal.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (exotic.HasExoticTransition)
        {
            // Strip transition-related props so the add slide doesn't write a
            // semantic <p:transition> that would then collide with the
            // raw-set append below (schema permits only one <p:transition>).
            foreach (var k in new[] { "transition", "transitionSpeed", "transitionDuration",
                                       "advanceTime", "advanceClick" })
                slideProps.Remove(k);
        }
        if (exotic.BgXml != null)
        {
            // Same shape as the exotic-transition strip above. The raw-set
            // prepend of <p:bg> below is the authoritative payload; if we
            // also let `add slide background=...` semantic-set fire, the
            // replay slide ends up with TWO <p:bg> blocks under <p:cSld>
            // (semantic Set appends, raw-set prepends — both survive). The
            // <p:cSld> schema permits at most one <p:bg>; PowerPoint silently
            // keeps the first and discards the second, so the lost block
            // depends on order. Strip the slide-level background prop here
            // so only the raw-set passthrough lands.
            slideProps.Remove("background");
        }
        if (exotic.MultiHfXmls != null)
        {
            // R55 bt-3: multi-hf source. Strip the semantic showX flags so
            // SetSlideByPath doesn't synthesize an additional single <p:hf>
            // alongside the raw-emitted siblings. Each captured slice replays
            // verbatim via raw-set append below.
            slideProps.Remove("showFooter");
            slideProps.Remove("showSlideNumber");
            slideProps.Remove("showDate");
            slideProps.Remove("showHeader");
        }

        items.Add(new BatchItem
        {
            Command = "add",
            Parent = "/",
            Type = "slide",
            Props = slideProps.Count > 0 ? slideProps : null,
        });

        // Picture-bullet image carrier. A paragraph/shape whose bullet glyph is an
        // image (<a:buBlip><a:blip r:embed="rIdN">) round-trips its bullet markup
        // verbatim via bulletRaw/lstStyleRaw, but the slide ImagePart it points at
        // is not re-created by `add picture` — the rebuilt slide dangled and the
        // bullet glyph was lost. Emit add-part image with the SOURCE rId pinned
        // HERE, right after `add slide` and BEFORE any shape/picture is added, so
        // the bullet image claims its source rId before AddPicture auto-assigns
        // (which would otherwise grab it and force a collision). Mirrors the
        // slide background-image carrier.
        foreach (var bi in ppt.GetSlideBulletImageParts(slideNum))
        {
            items.Add(new BatchItem
            {
                Command = "add-part",
                Parent = slidePath,
                Type = "image",
                Props = new Dictionary<string, string>
                {
                    ["rid"] = bi.RelId,
                    ["content-type"] = bi.ContentType,
                    ["data"] = bi.Base64Data,
                },
            });
        }

        // ShapeToNode tags placeholder shapes as plain "textbox"/"title". To
        // emit them as `add placeholder` we cross-reference each shape's cNvPr
        // id with the slide's Query("placeholder") result.
        // Only index placeholders defined on the slide itself. Query also
        // returns layout-inherited placeholders (Format["inheritedFrom"]
        // = "layout") whose ph index/id can collide with auto-assigned
        // textbox cNvPr ids on the slide (python-pptx starts at 2, layout
        // ftr/dt/sldNum live at id 2..4) — without this filter, the second
        // textbox would be misclassified as `ftr` and crash placeholder
        // type parsing, or silently disappear in dump.
        var placeholderById = new Dictionary<string, DocumentNode>(StringComparer.Ordinal);
        foreach (var ph in ppt.Query("placeholder"))
        {
            if (!ph.Path.StartsWith(slidePath + "/", StringComparison.Ordinal)) continue;
            if (ph.Format.TryGetValue("inheritedFrom", out var inh) && inh as string == "layout") continue;
            if (ph.Format.TryGetValue("id", out var phId) && phId != null)
                placeholderById[phId.ToString()!] = ph;
        }

        // Children: walk shape-tree level. Get already routed group/connector/
        // textbox/title/equation into typed nodes, so just iterate and dispatch.
        if (fullSlide.Children == null) return;
        // CONSISTENCY(positional-emit): dump references its own added elements
        // by positional `/slide[N]/shape[K]` (mirrors docx /body/p[K]) rather
        // than cNvPr `@id=N`. Add accepts caller-supplied id but emit chooses
        // not to use it — id collisions with layout-inherited placeholders
        // would otherwise break replay (animations/video deck cascade).
        //
        // CONSISTENCY(unified-shape-counter): placeholders are <p:sp> siblings
        // of plain shapes in the OOXML shape tree, so ResolveShape counts them
        // together. AddPlaceholder also appends a <p:sp> and returns
        // `/slide[N]/shape[<count>]` (Add.Misc.cs). The emitter must therefore
        // share a SINGLE positional counter across textbox/title/shape/equation
        // /placeholder and emit replay paths as `/slide[N]/shape[K]` for ALL
        // of them — otherwise a placeholder dispatched first leaves the
        // shape counter at 1, and the next textbox emits `set
        // /slide[N]/shape[1]/...` which on replay clobbers the placeholder.
        // Previously the emitter kept separate `shape`/`placeholder` counters
        // and emitted `/slide[N]/placeholder[K]` for placeholders, but the
        // replay paths for paragraph/run inside that placeholder still used
        // the same `/slide[N]/shape[K]` form — see EmitTextBody — so every
        // shape after a placeholder collided.
        // Pre-build the per-slide animation index keyed by source shape @id
        // (or positional fallback). EmitAnimationsForShape pulls per-shape
        // entries from this map as we emit each <p:sp>.
        //
        // When the slide has exotic timing content (motion paths, sequence
        // groupings, custom triggers the Query doesn't enumerate), we skip
        // semantic per-shape animation emits entirely and rely on a raw-set
        // passthrough of the whole <p:timing> tree appended at slide end.
        // Mixing the two would silently corrupt the replay (semantic add
        // would inject duplicate effect nodes alongside the raw-set tree).
        var animIndex = exotic.HasExoticTiming
            ? new Dictionary<string, List<DocumentNode>>(StringComparer.Ordinal)
            : BuildSlideAnimationIndex(ppt, slideNum);

        // R48: connectors can forward-reference shapes (source spTree may
        // hold a <p:cxnSp> ahead of a <p:sp> the connector's start/end
        // points target). The semantic walk dispatches in source spTree
        // order, so a forward-referencing connector emits its `add connector`
        // row with `from=/slide[N]/shape[K]` BEFORE shape[K] exists in the
        // replay slide — AddConnector then throws "Shape index K out of
        // range" and the whole slide goes uncreated. Hold connector emits
        // in a per-slide buffer and flush AFTER the rest of the loop so
        // every referenced shape has been added by then. Z-order regresses
        // for the rare cross-referencing case but no slide gets corrupted.
        var deferredConnectors = new List<(DocumentNode Node, int Ordinal)>();

        var ord = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in fullSlide.Children)
        {
            // Placeholder dispatch first — overrides textbox/title type.
            if ((child.Type == "textbox" || child.Type == "title" || child.Type == "shape")
                && child.Format.TryGetValue("id", out var cid) && cid != null
                && placeholderById.TryGetValue(cid.ToString()!, out var phNode))
            {
                ord["shape"] = ord.GetValueOrDefault("shape", 0) + 1;
                var phReplay = $"{slidePath}/shape[{ord["shape"]}]";
                EmitPlaceholder(ppt, phNode, slidePath, phReplay, items, ctx);
                EmitAnimationsForShape(GetAnimationsForChild(animIndex, child, ord["shape"]), phReplay, items);
                continue;
            }
            switch (child.Type)
            {
                case "textbox":
                case "title":
                case "shape":
                case "equation":
                    ord["shape"] = ord.GetValueOrDefault("shape", 0) + 1;
                    {
                        var shReplay = $"{slidePath}/shape[{ord["shape"]}]";
                        EmitShape(ppt, child, slidePath, shReplay, items, ctx);
                        EmitAnimationsForShape(GetAnimationsForChild(animIndex, child, ord["shape"]), shReplay, items);
                    }
                    break;
                case "placeholder":
                    ord["shape"] = ord.GetValueOrDefault("shape", 0) + 1;
                    {
                        var phReplay2 = $"{slidePath}/shape[{ord["shape"]}]";
                        EmitPlaceholder(ppt, child, slidePath, phReplay2, items, ctx);
                        EmitAnimationsForShape(GetAnimationsForChild(animIndex, child, ord["shape"]), phReplay2, items);
                    }
                    break;
                case "connector":
                    // R48: defer to slide-end so any forward-referenced
                    // <p:sp> the connector's start/end points to has been
                    // added by the time AddConnector resolves the
                    // /slide[N]/shape[K] form.
                    ord["connector"] = ord.GetValueOrDefault("connector", 0) + 1;
                    deferredConnectors.Add((child, ord["connector"]));
                    break;
                case "group":
                    ord["group"] = ord.GetValueOrDefault("group", 0) + 1;
                    EmitGroup(ppt, child, slidePath, $"{slidePath}/group[{ord["group"]}]", items, ctx);
                    break;
                case "table":
                    ord["table"] = ord.GetValueOrDefault("table", 0) + 1;
                    EmitTable(ppt, child, slidePath, $"{slidePath}/table[{ord["table"]}]", items, ctx);
                    break;
                case "picture":
                    ord["picture"] = ord.GetValueOrDefault("picture", 0) + 1;
                    EmitPicture(ppt, child, slidePath, $"{slidePath}/picture[{ord["picture"]}]", items, ctx);
                    break;
                case "chart":
                    ord["chart"] = ord.GetValueOrDefault("chart", 0) + 1;
                    {
                        EmitChart(ppt, child, slidePath, items, ctx, ord["chart"]);
                        // R14-bug5: chart-targeted animations were never emitted
                        // because BuildSlideAnimationIndex only indexed shape
                        // hosts, and the chart switch arm here never called
                        // EmitAnimationsForShape. The result: dumping a deck
                        // with a chart animation produced 0 `add animation`
                        // rows; on replay the chart entered as a static
                        // graphicFrame with no <p:timing> entry. Also
                        // chartBuild was being routed onto chart's add props
                        // (which chart.set / chart.add do not consume), so
                        // even a manual replay couldn't surface a per-series/
                        // per-category build. Re-route the animation list now.
                        var chartReplay = $"{slidePath}/chart[{ord["chart"]}]";
                        EmitAnimationsForShape(GetAnimationsForChartChild(animIndex, child, ord["chart"]), chartReplay, items);
                    }
                    break;
                case "video":
                case "audio":
                    // Phase 3c-media: routed through EmitMediaForSlide (a
                    // per-slide pass at slide-end) so the entire <p:pic>
                    // including <a:videoFile>/<a:audioFile>/p14:media rel
                    // references survive via add-part + raw-set passthrough.
                    // The typed walk skips them here to avoid double-emit
                    // (EmitPicture would only re-emit the picture shape
                    // without the media wiring).
                    break;
                case "3dmodel":
                case "model3d":
                    // Phase 3c-3d: routed through EmitModel3dForSlide (a
                    // per-slide pass at slide-end) so the <mc:AlternateContent>
                    // wrapper (Choice am3d:model3d + Fallback p:pic) and
                    // the underlying ExtendedPart .glb + thumbnail ImagePart
                    // survive via add-part + raw-set passthrough. The typed
                    // walk skips them here to avoid double-emit.
                    break;
                case "ole":
                    // Phase 3c-ole: routed through EmitOleForSlide (a
                    // per-slide pass at slide-end) so the <p:graphicFrame>
                    // hosting <p:oleObj> + its EmbeddedPackagePart /
                    // EmbeddedObjectPart payload + the thumbnail icon
                    // ImagePart survive via add-part + raw-set passthrough.
                    // The typed walk skips it here to avoid double-emit.
                    break;
                case "zoom":
                    // PR3+ scope. ProbeUnsupportedOnSlide already records the
                    // zoom markers via raw-XML sniff; this branch catches
                    // the children that surfaced via the typed Get tree
                    // (when NodeBuilder learns to tag them).
                    ctx.Unsupported.Add(new UnsupportedWarning(
                        Element: child.Type ?? "unknown",
                        SlidePath: slidePath,
                        Reason: "deferred to later PR"));
                    break;
                default:
                    ctx.Unsupported.Add(new UnsupportedWarning(
                        Element: child.Type ?? "unknown",
                        SlidePath: slidePath,
                        Reason: "unrecognized child type"));
                    break;
            }
        }

        // R48: flush deferred connectors — every referenced <p:sp> now
        // exists in the rebuilt slide so /slide[N]/shape[K] resolves.
        foreach (var (cxnChild, cxnOrdinal) in deferredConnectors)
            EmitConnector(ppt, cxnChild, slidePath, items, ctx, cxnOrdinal);

        // Raw-XML passthrough for exotic transition / timing content. Emitted
        // AFTER all shape/animation rows so they replace anything the semantic
        // emit produced (defensive — slideProps already stripped, animIndex
        // already nulled, but raw-set is the authoritative payload).
        // Append into /p:sld preserves OOXML schema order because we removed
        // the corresponding props upstream: the slide carries neither
        // <p:transition> nor <p:timing> at this point in replay.
        // R42-B1: append slide-level children that the semantic emit path
        // doesn't write. Order follows OOXML schema (cSld → clrMapOvr →
        // transition → timing → extLst). Since the freshly-added slide
        // carries none of these (semantic emit covered only cSld and the
        // optional <p:transition> via prop), an "append on /p:sld" sequence
        // in schema order produces a schema-valid result.
        if (exotic.BgXml != null)
            EmitRawSlideBgSlice(ppt, slideNum, slidePath, exotic.BgXml, items, ctx);
        if (exotic.ClrMapOvrXml != null)
            EmitRawSlideSlice(slidePath, "p:clrMapOvr", exotic.ClrMapOvrXml, items, ctx);
        if (exotic.HasExoticTransition && exotic.TransitionXml != null)
            EmitRawSlideSlice(slidePath, "p:transition", exotic.TransitionXml, items, ctx);
        if (exotic.HasExoticTiming && exotic.TimingXml != null)
        {
            // Strip animation subtrees that target a shape the rebuilt slide
            // no longer carries. By-design drops (OLE objects whose payload or
            // thumbnail won't resolve) remove the <p:graphicFrame> from the
            // emitted spTree, but the verbatim <p:timing> passthrough still
            // references the dropped shape's cNvPr id via <p:spTgt spid="N"/>.
            // PowerPoint rejects a deck whose animation tree targets an absent
            // shape ("could not open") even though validate / the SDK tolerate
            // it. Prune the smallest self-contained timing subtree holding each
            // dangling target so the surviving animation tree stays
            // schema-valid; the dropped object simply isn't animated.
            var emittedShapeIds = ComputeEmittedShapeIds(items, sliceStart);
            var prunedTiming = PruneDanglingTimingTargets(exotic.TimingXml, emittedShapeIds);
            if (prunedTiming != null)
                EmitRawSlideSlice(slidePath, "p:timing", prunedTiming, items, ctx);
        }
        if (exotic.ExtLstXml != null)
            EmitRawSlideSlice(slidePath, "p:extLst", exotic.ExtLstXml, items, ctx);
        if (exotic.TrailingTransitionXml != null)
            EmitRawSlideSlice(slidePath, "p:transition", exotic.TrailingTransitionXml, items, ctx);
        if (exotic.MultiHfXmls != null)
        {
            // R55 bt-3: append each captured <p:hf .../> sibling verbatim. We
            // intentionally do NOT canonicalise (NormalizeSlideRawSlice's SDK
            // round-trip resolves namespace prefixes from the original part
            // root, but a bare <p:hf .../> has no nested content and only
            // attribute tokens — the source slice is already canonical).
            foreach (var hfXml in exotic.MultiHfXmls)
                EmitRawSlideSlice(slidePath, "p:hf", hfXml, items, ctx);
        }

        // SmartArt graphicFrames live in /p:sld/p:cSld/p:spTree but are
        // skipped by NodeBuilder (table/chart-only routing). Phase 3b emits
        // them as add-part smartart (creates the four diagram sub-parts with
        // caller-pinned rIds) followed by raw-set rows that fill each part's
        // XML, and a final raw-set append on /p:sld/p:cSld/p:spTree with the
        // graphicFrame XML. Caller-pinned rIds make the graphicFrame's
        // <dgm:relIds> round-trip byte-equal.
        EmitSmartArtsForSlide(ppt, slideNum, slidePath, items, ctx);

        // Phase 3c-media: video/audio <p:pic> hosts with their underlying
        // MediaDataPart + thumbnail ImagePart, mirroring the SmartArt
        // passthrough. The typed walk skipped video/audio children above.
        EmitMediaForSlide(ppt, slideNum, slidePath, items, ctx);

        // Phase 3c-3d: am3d 3D-model AlternateContent blocks with their
        // underlying ExtendedPart .glb + thumbnail ImagePart, mirroring
        // the video/audio passthrough. The typed walk skipped
        // 3dmodel/model3d children above.
        EmitModel3dForSlide(ppt, slideNum, slidePath, items, ctx);

        // Phase 3c-ole: <p:graphicFrame> hosting <p:oleObj> with the
        // underlying EmbeddedPackagePart (OOXML containers) or
        // EmbeddedObjectPart (generic binaries) + thumbnail icon ImagePart,
        // mirroring the model3d passthrough. The typed walk skipped the
        // OLE child above.
        EmitOleForSlide(ppt, slideNum, slidePath, items, ctx);

        // Generic <mc:AlternateContent> passthrough — covers AlternateContent
        // blocks in spTree that don't match any of the specific emitters
        // above (am3d:model3d in EmitModel3dForSlide, SmartArt in
        // EmitSmartArtsForSlide, media in EmitMediaForSlide, OLE in
        // EmitOleForSlide). NodeBuilder's typed walk explicitly skips the
        // mc:AlternateContent wrapper (Choice + Fallback would otherwise
        // double-count <p:sp> children), so without this catch-all every
        // such block is silently dropped on dump→replay — meaningful for
        // any emerging-feature wrapping the semantic walk doesn't model.
        EmitGenericAlternateContentForSlide(ppt, slideNum, slidePath, items, ctx);

        // Notes body content — stub for PR1. Notes part presence does not
        // surface in the slide subtree's children today (notes live under
        // /slide[N]/notes); PR2 will reach in and emit them.
        EmitNotes(ppt, slidePath, items, ctx);

        // Legacy slide comments — also off the shape tree (SlideCommentsPart).
        // Emit AFTER notes so the per-slide row order is stable: shapes →
        // notes → comments, mirroring how a reader would traverse the slide.
        EmitComments(ppt, slidePath, items, ctx);

        // Modern p188 threaded comments — distinct from legacy p:cm; live in
        // PowerPointCommentPart. Emit after legacy comments to keep a stable
        // per-slide row ordering.
        EmitModernComments(ppt, slidePath, items, ctx);
    }

    // Touch the raw slide XML to find content that has no handler vocabulary
    // yet. Each match adds an UnsupportedWarning entry; we never throw.
    private static void ProbeUnsupportedOnSlide(PowerPointHandler ppt, string slidePath,
                                                SlideEmitContext ctx)
    {
        string xml;
        try { xml = ppt.Raw(slidePath); }
        catch { return; }

        // <p:timing> = slide animation. EmitAnimationsForShape now emits the
        // entrance/exit/emphasis effects per shape via the `animation` Query
        // surface, so the timing tree no longer aborts to an unsupported
        // warning. Exotic timing constructs (motion paths, sequence groupings)
        // still go through the Query — animations the Query doesn't enumerate
        // are silently dropped.

        // SmartArt sits inside a graphicFrame as a dgm:relIds element.
        // Phase 3b: handled by EmitSmartArtsForSlide via add-part smartart +
        // raw-set passthrough; no warning is raised when we can extract the
        // four diagram parts. If extraction fails the SmartArt emit silently
        // falls back to a missing slice — caller sees a degraded slide but
        // no crash.

        // Phase 3c-ole: <p:oleObj> hosts round-trip via EmitOleForSlide
        // (add-part ole + raw-set). No probe warning — the slice owns the
        // entire emit. EmbeddedPackagePart / EmbeddedObjectPart auto-select
        // is by source content-type.
        // Phase 3c-media: video/audio <p:pic> hosts round-trip via
        // EmitMediaForSlide (add-part + raw-set). No probe warning here —
        // even if the slide carries a <p:video>/<p:audio> timing node, the
        // <p:pic> shape itself surfaces in the typed Get tree as
        // child.Type = "video"/"audio" and is now handled.
        // Phase 3c-3d: am3d:model3d AlternateContent blocks round-trip via
        // EmitModel3dForSlide (add-part model3d + raw-set). No probe warning
        // is raised — the slice owns the entire emit. The legacy <p:model3d>
        // bare form never appears in PowerPoint-authored decks, so we no
        // longer match it either.

        // Exotic transitions (morph, p15:prstTrans gallery, p14:* like flip/
        // gallery/conveyor) and exotic animation timing (motion paths,
        // sequence groupings) now round-trip via a raw-set passthrough on the
        // <p:transition> / <p:timing> elements — see ScanSlideExoticContent
        // and EmitRawSlideSlice. No UnsupportedWarning is raised for them
        // because the slice is emitted verbatim. The warning is reserved for
        // cases where the slice itself cannot be canonicalised (handled in
        // EmitRawSlideSlice).
    }

    /// <summary>
    /// Result of scanning a slide's raw XML for content that the semantic
    /// emit path cannot reproduce. Both transition and timing fields are
    /// null when the slide carries only vanilla content (or none).
    /// </summary>
    private readonly record struct SlideExoticContent(
        bool HasExoticTransition, string? TransitionXml,
        bool HasExoticTiming, string? TimingXml,
        // R42-B1: two sld-level children that the semantic emit path never
        // reads. Both round-trip via raw-set append on /p:sld, mirroring
        // the transition/timing passthrough. Schema order under <p:sld>
        // is cSld → clrMapOvr → transition → timing → extLst, so appending
        // these in scan order (clrMapOvr before, extLst after) preserves
        // ordering relative to the raw-emitted transition/timing slices.
        // Plain (non-exotic) <p:timing> is owned by the animation index
        // path; we deliberately do NOT raw-emit it here to avoid
        // duplicating effects with the semantic add-animation rows.
        string? ClrMapOvrXml,
        string? ExtLstXml,
        // R48: slide-level <p:bg> (inside <p:cSld>, before <p:spTree>).
        // NodeBuilder does not surface slide background, and the semantic
        // setter path (set /slide[N] background=...) is only fired when the
        // dump produces a `background` prop on the `add slide` row — which
        // it never does because Get does not emit it. Capture the raw bg
        // slice and prepend it onto /p:sld/p:cSld so it lands before
        // <p:spTree> in schema order. Image-fill backgrounds (r:embed
        // pointing to a slide-rels ImagePart) are flagged with a warning
        // because the freshly-added replay slide has no matching relationship
        // — a follow-up add-part pass would be needed for full image bg.
        string? BgXml,
        // R48: trailing plain <p:transition> appearing AFTER <p:timing> as a
        // direct sibling. The first-transition scan above stops at the first
        // <p:transition (typically inside mc:AlternateContent for morph /
        // p14 / p15 decks); a second plain transition with attributes like
        // advTm / advClick is then silently dropped. ReadSlideTransition's
        // typed accessor returns this trailing one (it's the first DIRECT
        // <p:transition> child since the inner one is wrapped), but the
        // regex fallback path harvests advanceTime from the mc-inner block
        // and never inspects the trailing element. Captured as its own
        // verbatim slice and raw-set appended on /p:sld so both transitions
        // round-trip in source order.
        string? TrailingTransitionXml,
        // R55 bt-3: 2+ <p:hf .../> siblings in source <p:sld>. PowerPoint's
        // semantic Set surface only writes one <p:hf> per slide
        // (GetFirstChild<HeaderFooter>), so the semantic showFooter /
        // showSlideNumber / showDate / showHeader emit collapses a multi-hf
        // source down to a single block. When the source has more than one
        // bare <p:hf .../> sibling, capture each verbatim and raw-set append
        // them onto /p:sld in source order; strip the semantic hf props
        // upstream so the replay slide carries exactly the source's hf set.
        // The single-hf common case stays on the semantic showFooter /
        // showSlideNumber / showDate / showHeader emit (this list is empty).
        IReadOnlyList<string>? MultiHfXmls);

    private static SlideExoticContent ScanSlideExoticContent(PowerPointHandler ppt, string slidePath)
    {
        string xml;
        try { xml = ppt.Raw(slidePath); }
        catch { return default; }

        string? transXml = null;
        bool transExotic = false;
        var tIdx = xml.IndexOf("<p:transition", StringComparison.Ordinal);
        if (tIdx >= 0)
        {
            // Capture the full <p:transition>...</p:transition> slice, OR the
            // self-closing form. Also include any enclosing
            // <mc:AlternateContent> wrapper because morph/p14/p15 transitions
            // live INSIDE that wrapper (the typed <p:transition> sits as the
            // mc:Fallback child); the wrapper is the natural replace target.
            var mcWrapStart = xml.LastIndexOf("<mc:AlternateContent", tIdx, StringComparison.Ordinal);
            // mcWrapStart is valid only if its closing </mc:AlternateContent>
            // tag lies after tIdx (i.e. <p:transition> is nested inside it).
            int sliceStart, sliceEnd;
            if (mcWrapStart >= 0)
            {
                var mcWrapEnd = xml.IndexOf("</mc:AlternateContent>", mcWrapStart, StringComparison.Ordinal);
                if (mcWrapEnd > tIdx)
                {
                    sliceStart = mcWrapStart;
                    sliceEnd = mcWrapEnd + "</mc:AlternateContent>".Length;
                }
                else
                {
                    sliceStart = tIdx;
                    sliceEnd = SliceEnd(xml, tIdx, "p:transition");
                }
            }
            else
            {
                sliceStart = tIdx;
                sliceEnd = SliceEnd(xml, tIdx, "p:transition");
            }
            if (sliceEnd > sliceStart)
            {
                var slice = xml.Substring(sliceStart, sliceEnd - sliceStart);
                // Exotic markers: any markup outside the plain <p:transition>
                // grammar — namespaces other than p:/a: under the transition
                // tree, mc:AlternateContent wrapping, p14/p15/p159 extension
                // elements. Vanilla fade/push/wipe/cut/cover/cut/etc. that the
                // semantic `transition=` prop already round-trips through
                // ReadSlideTransition NEVER carry these markers, so the
                // semantic path remains authoritative for them.
                if (slice.Contains("mc:AlternateContent", StringComparison.Ordinal)
                    || slice.Contains("p159:", StringComparison.Ordinal)
                    || slice.Contains("p15:", StringComparison.Ordinal)
                    || slice.Contains("p14:", StringComparison.Ordinal))
                {
                    transExotic = true;
                    transXml = slice;
                }
            }
        }

        string? timingXml = null;
        bool timingExotic = false;
        var pIdx = xml.IndexOf("<p:timing", StringComparison.Ordinal);
        if (pIdx >= 0)
        {
            var sliceEnd = SliceEnd(xml, pIdx, "p:timing");
            if (sliceEnd > pIdx)
            {
                var slice = xml.Substring(pIdx, sliceEnd - pIdx);
                // Motion paths surface as presetClass="path"; sequence
                // groupings beyond the per-shape entrance/exit/emphasis tree
                // that Query enumerates show up as <p:tnLst> nested under
                // <p:par> with no presetID anchor that we currently parse,
                // OR as <p:set>/<p:anim>/<p:animMotion>/<p:animRot>/etc.
                // direct timing-effect nodes which BuildSlideAnimationIndex
                // doesn't materialise. The cheapest precise signal is
                // presetClass="path" (motion path) OR any <p:animMotion>
                // element OR a presetClass we don't enumerate.
                // Precise signal: `presetClass="path"` flags motion-path
                // animations (the Query selector "animation" excludes
                // presetClass=motion/path entirely, so they vanish under the
                // semantic emit). `<p:animMotion>` is the lower-level
                // OOXML element a motion-path expands to but rarely appears
                // without the presetClass marker on the enclosing effect.
                // Other p:anim* variants (animScale/animRot/animClr) are
                // how the SDK implements ordinary zoom/spin/colorChange
                // EMPHASIS effects that the Query DOES enumerate via
                // PopulateAnimationNode — flagging those would force every
                // emphasis slide through raw-set and break the basic
                // animation round-trip.
                // R10-bug2: media-only timing (`<p:audio>/<p:video>` carrying
                // <p:cMediaNode vol=… repeatCount=…> + the play-cmd seq under
                // <p:tnLst>) holds the only copy of volume / loop / autoPlay /
                // trim props for audio/video shapes. BuildSlideAnimationIndex
                // does not materialise those nodes (they're not shape entrance
                // /exit/emphasis presets), so without exotic-flagging the
                // entire <p:timing> slice the semantic emit drops every audio
                // /video playback prop. Route through the same raw-set
                // passthrough used for motion-path animations. Same caveat:
                // a slide carrying both materialised animations AND a media
                // timing block will double-emit the animation portion — rare
                // in practice (audio/video decks seldom also carry shape
                // entrance effects), and the alternative (surgically slicing
                // <p:audio>/<p:video> out of the timing tree and stitching
                // around BuildSlideAnimationIndex output) is materially more
                // work than the warning-vs-correctness tradeoff justifies.
                // <p:animClr>-bearing timing trees: the semantic emit recreates
                // the effect body from canned EffectTemplates (emph_colorpulse,
                // emph_objectcolor, emph_lighten, …) with placeholder
                // substitution. When the source animClr's durations, attribute
                // names, color targets, or override flags diverge from the
                // template (which is the typical case for hand-authored or
                // tool-generated decks — PowerPoint itself rarely emits the
                // canned form verbatim), dump→replay produces a structurally
                // different body that PowerPoint silently renders as a different
                // effect. Route the entire <p:timing> slice through raw-set so
                // the source bytes survive intact. Tradeoff: a deck with ONLY
                // template-matched animClr emphasis effects round-trips via
                // raw-set instead of the semantic add-animation row — slightly
                // bigger batch, but byte-faithful.
                // R53 tester-3: <p:set> emphasis-preset trees (presetID=27 et
                // al., colorPulse / pulse / wave / fillColor) suffer the same
                // template-rebuild rot as <p:animClr>: EmitAnimationsForShape
                // expands the source <p:set> into a richer animClr / animEffect
                // / animScale body using canned EffectTemplates, so dump→replay
                // produces a body PowerPoint renders as a substantively
                // different effect (multiple emphasis layers per the template
                // instead of the source's single-shot <p:set>). Same passthrough
                // tradeoff as animClr — route the whole <p:timing> slice
                // through raw-set when any literal <p:set> appears.
                if (slice.Contains("presetClass=\"path\"", StringComparison.Ordinal)
                    || slice.Contains("<p:animMotion", StringComparison.Ordinal)
                    || slice.Contains("<p:audio", StringComparison.Ordinal)
                    || slice.Contains("<p:video", StringComparison.Ordinal)
                    || slice.Contains("<p:animClr", StringComparison.Ordinal)
                    || slice.Contains("<p:set", StringComparison.Ordinal)
                    // tester-2: pre-R53 the detector only fired on <p:set>
                    // and the well-known animMotion / animClr / media tags.
                    // A <p:timing> slice whose only effect node is a bare
                    // <p:cTn> (or any other p:anim* / p:cmd / p:par / p:seq
                    // descendant beyond the default empty tnLst skeleton)
                    // bypassed every signal and the entire animation tree was
                    // silently dropped on dump → replay. Extend to any p:anim
                    // family element plus the sequence/parallel/cmd timing
                    // primitives PowerPoint emits for trigger-based effects.
                    || slice.Contains("<p:anim", StringComparison.Ordinal)
                    || slice.Contains("<p:cmd", StringComparison.Ordinal)
                    || slice.Contains("<p:par>", StringComparison.Ordinal)
                    || slice.Contains("<p:seq>", StringComparison.Ordinal)
                    || HasNonTrivialTimingBody(slice))
                {
                    timingExotic = true;
                    timingXml = slice;
                }
            }
        }

        // R42-B1: scan for <p:clrMapOvr> with a child <p:masterClrMapping/> or
        // <p:overrideClrMapping ...>. The empty <p:clrMapOvr><p:masterClrMapping/>
        // form is the default and conveys no information beyond "use master"
        // (PowerPoint silently inserts it on every slide), so we still emit it
        // to keep dump→replay byte-faithful. Captured by literal substring
        // match — clrMapOvr never carries namespace-extension content.
        string? clrMapOvrXml = null;
        var cmIdx = xml.IndexOf("<p:clrMapOvr", StringComparison.Ordinal);
        if (cmIdx >= 0)
        {
            var sliceEnd = SliceEnd(xml, cmIdx, "p:clrMapOvr");
            if (sliceEnd > cmIdx)
                clrMapOvrXml = xml.Substring(cmIdx, sliceEnd - cmIdx);
        }

        // R42-B1: scan for <p:extLst> directly under <p:sld>. Slide-level
        // extLst typically carries section IDs, slide-id markers, and other
        // extension content the semantic path doesn't model. We capture the
        // last <p:extLst>...</p:extLst> in the doc because shapes inside
        // spTree may also carry their own extLst — those round-trip via the
        // shape passthrough. Slide-level extLst always appears AFTER timing,
        // so the right anchor is the last occurrence not nested in a shape.
        // Heuristic: scan for </p:timing> or </p:cSld> and take any extLst
        // appearing AFTER all of those, before </p:sld>.
        string? extLstXml = null;
        var sldEnd = xml.LastIndexOf("</p:sld>", StringComparison.Ordinal);
        if (sldEnd > 0)
        {
            // Find the last <p:extLst that begins after the spTree close.
            var spTreeEnd = xml.LastIndexOf("</p:spTree>", sldEnd, StringComparison.Ordinal);
            var anchor = spTreeEnd > 0 ? spTreeEnd : xml.IndexOf("</p:cSld>", StringComparison.Ordinal);
            if (anchor > 0)
            {
                var eIdx = xml.IndexOf("<p:extLst", anchor, StringComparison.Ordinal);
                while (eIdx > 0 && eIdx < sldEnd)
                {
                    var eEnd = SliceEnd(xml, eIdx, "p:extLst");
                    if (eEnd > eIdx && eEnd <= sldEnd)
                    {
                        extLstXml = xml.Substring(eIdx, eEnd - eIdx);
                        // Only one slide-level extLst is legal per schema;
                        // stop at the first match past the spTree close.
                        break;
                    }
                    eIdx = xml.IndexOf("<p:extLst", eIdx + 1, StringComparison.Ordinal);
                }
            }
        }

        // R48: slide-level <p:bg> capture (inside <p:cSld>, before <p:spTree>).
        // Detect the first <p:bg ...> within the slide xml that precedes
        // <p:spTree>. The cSld parent contains an optional bg slot in schema
        // order (<p:cSld><p:bg?/><p:spTree/>...</p:cSld>), so anchoring at
        // the earliest <p:bg appearing before <p:spTree> is unambiguous.
        //
        // The search MUST be bounded to the region before <p:spTree>. The
        // <p:timing> tree (after </p:spTree>) carries animation targets of the
        // form <p:tgtEl><p:spTgt spid="N"><p:bg/></...>, and an unbounded
        // IndexOf would latch onto that timing <p:bg/> on slides that animate
        // the background but have no cSld-level <p:bg>. That emitted a spurious
        // empty <p:bg/> prepend; <p:cSld> requires <p:bg> to carry <p:bgPr> or
        // <p:bgRef>, so an empty <p:bg/> is schema-invalid and PowerPoint
        // refuses the whole file (0x80070570 / could-not-open). Bounding to the
        // pre-spTree window keeps only a genuine slide background.
        string? bgXml = null;
        var spTreeOpen = xml.IndexOf("<p:spTree", StringComparison.Ordinal);
        var bgSearchLimit = spTreeOpen >= 0 ? spTreeOpen : xml.Length;
        var bgIdx = xml.IndexOf("<p:bg", StringComparison.Ordinal);
        if (bgIdx >= 0 && bgIdx < bgSearchLimit)
        {
            // Guard: ensure this <p:bg is truly the cSld-level background and
            // not a substring match inside something like <p:bgClr=…>. The
            // valid follow-up chars are '>' (open tag) or ' ' (attributes) or
            // '/' (self-closing form). 'P' / 'C' would indicate <p:bgPr> /
            // <p:bgClr> which are children of <p:bg>, not <p:bg> itself.
            char next = bgIdx + 5 < xml.Length ? xml[bgIdx + 5] : '\0';
            if (next == '>' || next == ' ' || next == '/')
            {
                var bgEnd = SliceEnd(xml, bgIdx, "p:bg");
                if (bgEnd > bgIdx)
                    bgXml = xml.Substring(bgIdx, bgEnd - bgIdx);
            }
        }

        // R48: trailing <p:transition> sibling after the first one (and
        // outside any mc:AlternateContent wrap we already captured). Walk
        // from just past the first <p:transition> end (or its mc wrap end)
        // and look for another. Skip transitions nested under <mc:Choice> /
        // <mc:Fallback> branches that belong to ALREADY captured wrappers.
        string? trailingTransXml = null;
        if (tIdx >= 0)
        {
            // Resume scanning at the end of the first transition slice (or
            // the end of its mc:AlternateContent wrap if present).
            int resumeFrom;
            var firstMcStart = xml.LastIndexOf("<mc:AlternateContent", tIdx, StringComparison.Ordinal);
            if (firstMcStart >= 0)
            {
                var firstMcEnd = xml.IndexOf("</mc:AlternateContent>", firstMcStart, StringComparison.Ordinal);
                resumeFrom = firstMcEnd > tIdx
                    ? firstMcEnd + "</mc:AlternateContent>".Length
                    : SliceEnd(xml, tIdx, "p:transition");
            }
            else
            {
                resumeFrom = SliceEnd(xml, tIdx, "p:transition");
            }
            if (resumeFrom > 0 && resumeFrom < xml.Length)
            {
                var t2Idx = xml.IndexOf("<p:transition", resumeFrom, StringComparison.Ordinal);
                if (t2Idx >= 0)
                {
                    var t2End = SliceEnd(xml, t2Idx, "p:transition");
                    if (t2End > t2Idx)
                        trailingTransXml = xml.Substring(t2Idx, t2End - t2Idx);
                }
            }
        }

        // R55 bt-3: scan for 2+ bare <p:hf .../> siblings under <p:sld>.
        // Skip <p:hf nested inside <p:hdr>/<p:ftr>/<p:dt>/<p:sldNum> placeholder
        // text bodies — those are not the slide-level header/footer flags
        // element. The legal slide-level form is <p:hf ... /> self-closed
        // (attributes ftr/sldNum/dt/hdr); collect each occurrence verbatim.
        List<string>? multiHfXmls = null;
        if (sldEnd > 0)
        {
            var spTreeEnd = xml.LastIndexOf("</p:spTree>", sldEnd, StringComparison.Ordinal);
            if (spTreeEnd > 0)
            {
                var hfRegex = new System.Text.RegularExpressions.Regex(@"<p:hf\b[^>]*/>");
                var hfMatches = hfRegex.Matches(xml, spTreeEnd);
                if (hfMatches.Count >= 2)
                {
                    multiHfXmls = hfMatches.Select(m => m.Value).ToList();
                }
            }
        }

        return new SlideExoticContent(transExotic, transXml, timingExotic, timingXml,
            clrMapOvrXml, extLstXml, bgXml, trailingTransXml, multiHfXmls);
    }

    // Normalize a slide raw slice into a stable textual form so the first-pass
    // (clean source XML) and second-pass (post-SDK-round-trip) produce
    // byte-identical raw-set rows. The SDK round-trip aggressively rewrites
    // ambient prefixes: it may render <mc:AlternateContent> as <AlternateContent
    // xmlns="…/markup-compatibility/2006"> (default-namespaced) on output even
    // when the inserted source used the prefixed form. This normalizer parses
    // the slice, forces every element in the four ambient pptx namespaces
    // (p, a, r, mc) onto its canonical prefix, then strips redundant xmlns
    // decls. The result compares equal across rounds regardless of which
    // serialization the SDK picked.
    private static string NormalizeSlideRawSlice(string sliceXml)
    {
        if (string.IsNullOrEmpty(sliceXml) || !sliceXml.StartsWith("<")) return sliceXml;
        try
        {
            // The slice is extracted as a raw substring of /p:sld and inherits
            // its ambient namespace bindings from the slide root — so the
            // standalone slice text may use prefixes (mc, p, a, r) that aren't
            // declared on its own root. XDocument.Parse would then throw an
            // unbound-prefix error and the whole normalize step would silently
            // bail (catch below), leaving slice bytes drifting between rounds.
            // Inject the ambient decls onto the slice root tag so parsing
            // succeeds. The later "drop ambient from root tag" pass strips
            // them again post-serialize, so the emitted slice still travels
            // without redundant decls.
            sliceXml = EnsureAmbientXmlnsOnRootTag(sliceXml);
            var doc = System.Xml.Linq.XDocument.Parse(sliceXml);
            if (doc.Root == null) return sliceXml;
            var ambient = new (string Prefix, System.Xml.Linq.XNamespace Ns)[]
            {
                ("p",  "http://schemas.openxmlformats.org/presentationml/2006/main"),
                ("a",  "http://schemas.openxmlformats.org/drawingml/2006/main"),
                ("r",  "http://schemas.openxmlformats.org/officeDocument/2006/relationships"),
                ("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006"),
            };
            // Force the root to carry the canonical prefix decls for any
            // ambient namespace it uses on itself or its descendants. We do
            // not strip non-ambient decls (e.g. p159, p14, p15) since those
            // are extension namespaces specific to this slice and must
            // travel with it.
            foreach (var (prefix, ns) in ambient)
            {
                bool used = doc.Root.DescendantsAndSelf().Any(e => e.Name.Namespace == ns)
                            || doc.Root.DescendantsAndSelf().SelectMany(e => e.Attributes())
                                .Any(a => !a.IsNamespaceDeclaration && a.Name.Namespace == ns);
                if (!used) continue;
                // Remove any default-namespace decls pointing at this ambient
                // namespace, anywhere in the tree — they will be supplanted
                // by the prefixed form on the root.
                foreach (var el in doc.Root.DescendantsAndSelf().ToList())
                {
                    var toRemove = el.Attributes()
                        .Where(a => a.IsNamespaceDeclaration && a.Value == ns.NamespaceName)
                        .ToList();
                    foreach (var a in toRemove) a.Remove();
                }
                // Stamp the canonical prefix decl onto the root.
                doc.Root.SetAttributeValue(System.Xml.Linq.XNamespace.Xmlns + prefix, ns.NamespaceName);
            }
            // Lift extension prefix declarations (p14, p15, p159, am3d, …) up
            // to the slice root if any descendant binds them and the root does
            // not already declare them. The SDK normalizes namespace decls to
            // the highest needed ancestor on serialize, so a slice that
            // declares xmlns:p14 on <p:transition> on pass 1 round-trips
            // through the SDK and comes back with xmlns:p14 on
            // <mc:AlternateContent> on pass 2 — same semantics, different
            // bytes. Mirror that transform here so pass-1 and pass-2 slices
            // are byte-equal. Only lift prefixes that are NOT already in the
            // ambient set (those were handled above) and only the FIRST
            // binding seen for each prefix (per-prefix singleton — extensions
            // never use multiple URIs in one slice).
            var ambientPrefixes = new HashSet<string>(StringComparer.Ordinal) { "p", "a", "r", "mc" };
            var liftedPrefixes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var desc in doc.Root.DescendantsAndSelf())
            {
                var nsDecls = desc.Attributes()
                    .Where(a => a.IsNamespaceDeclaration && !ambientPrefixes.Contains(a.Name.LocalName))
                    .ToList();
                foreach (var a in nsDecls)
                {
                    var prefix = a.Name.LocalName;
                    if (!liftedPrefixes.ContainsKey(prefix))
                        liftedPrefixes[prefix] = a.Value;
                }
            }
            foreach (var (prefix, uri) in liftedPrefixes)
            {
                if (doc.Root.GetNamespaceOfPrefix(prefix) != null) continue;
                doc.Root.SetAttributeValue(System.Xml.Linq.XNamespace.Xmlns + prefix, uri);
            }
            // R42-T2: drop unused non-ambient xmlns decls from the slice root.
            //
            // Pass 1 extracts a transition AlternateContent slice that declares
            // ONLY xmlns:p159 because that's what its <mc:Choice> references.
            // After replay, the SDK serializes the slide such that the SAME
            // AlternateContent picks up sibling decls (xmlns:am3d, xmlns:a16,
            // ...) from the slide root, because the SDK propagates every
            // root-level namespace down to top-level children when it can't
            // prove they are unused. Pass 2 then extracts a slice carrying
            // xmlns:am3d that pass 1 did not — same semantics, drifting bytes.
            //
            // A non-ambient prefix is "used" by this slice iff it appears on an
            // element name, an attribute name, OR as a token inside
            // mc:Choice/@Requires or any @mc:Ignorable. Anything else is dead
            // weight inherited from the parent and should be stripped to keep
            // the slice byte-stable across rounds.
            var mcNs = System.Xml.Linq.XNamespace.Get("http://schemas.openxmlformats.org/markup-compatibility/2006");
            var usedPrefixes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var el in doc.Root.DescendantsAndSelf())
            {
                if (!string.IsNullOrEmpty(el.Name.NamespaceName))
                {
                    var p = el.GetPrefixOfNamespace(el.Name.Namespace);
                    if (!string.IsNullOrEmpty(p)) usedPrefixes.Add(p);
                }
                foreach (var attr in el.Attributes())
                {
                    if (attr.IsNamespaceDeclaration) continue;
                    if (!string.IsNullOrEmpty(attr.Name.NamespaceName))
                    {
                        var p = el.GetPrefixOfNamespace(attr.Name.Namespace);
                        if (!string.IsNullOrEmpty(p)) usedPrefixes.Add(p);
                    }
                    // mc:Choice/@Requires + mc:Ignorable are space-separated
                    // prefix lists referencing namespaces by their bound name.
                    if (attr.Name == mcNs + "Ignorable" || attr.Name.LocalName == "Requires"
                        || attr.Name.LocalName == "Ignorable")
                    {
                        foreach (var tok in attr.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            usedPrefixes.Add(tok);
                    }
                }
            }
            var stripDecls = doc.Root.Attributes()
                .Where(a => a.IsNamespaceDeclaration
                            && !ambientPrefixes.Contains(a.Name.LocalName)
                            && !usedPrefixes.Contains(a.Name.LocalName))
                .ToList();
            foreach (var d in stripDecls) d.Remove();
            // Drop redundant prefix decls on descendants that match the root's
            // (mirrors CanonicalizeRawXml but on the post-rewrite tree).
            var rootDecls = doc.Root.Attributes()
                .Where(a => a.IsNamespaceDeclaration)
                .ToDictionary(a => a.Name, a => a.Value);
            foreach (var desc in doc.Root.Descendants())
            {
                var dups = desc.Attributes()
                    .Where(a => a.IsNamespaceDeclaration
                                && rootDecls.TryGetValue(a.Name, out var v) && v == a.Value)
                    .ToList();
                foreach (var a in dups) a.Remove();
            }
            // Final pass: drop ambient namespace decls from the slice root.
            // They are guaranteed to be in scope at the /p:sld replay site,
            // so keeping them only causes textual drift between rounds (the
            // SDK re-stamps them on read-back, our source-side extraction
            // may not have them at all).
            //
            // We CANNOT use XLinq's RemoveAttributes here naively — XLinq
            // refuses to remove a namespace declaration that is currently
            // in use by the element's own name or attribute names; doing so
            // would silently break the serialization. So we serialize first,
            // THEN textually drop the ambient decls from the root tag.
            var serialized = doc.Root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
            return StripAmbientXmlnsFromRootTag(serialized);
        }
        catch { return sliceXml; }
    }

    // Prefix -> URI table used to repair a slice substring whose prefixes were
    // declared on the SOURCE slide root (and so are absent when the slice is
    // lifted out as standalone text). The ambient four (p/a/r/mc) plus the
    // well-known Office extension namespaces that appear inside slide extLst /
    // mc:AlternateContent fragments. Without the extension entries, an
    // `append`-action fragment carrying e.g. <p14:creationId> with no
    // xmlns:p14 failed XDocument.Parse in NormalizeSlideRawSlice (unbound
    // prefix) — the normalize bailed and emitted the undeclared fragment, which
    // then broke batch replay. The whole-part `replace` raw-sets were unaffected
    // because the SDK serializes the full part with the decl on its root.
    private static readonly (string Prefix, string Uri)[] SliceXmlnsBindings =
    {
        ("p",  "http://schemas.openxmlformats.org/presentationml/2006/main"),
        ("a",  "http://schemas.openxmlformats.org/drawingml/2006/main"),
        ("r",  "http://schemas.openxmlformats.org/officeDocument/2006/relationships"),
        ("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006"),
        ("p14",  "http://schemas.microsoft.com/office/powerpoint/2010/main"),
        ("p15",  "http://schemas.microsoft.com/office/powerpoint/2012/main"),
        ("p16",  "http://schemas.microsoft.com/office/powerpoint/2015/main"),
        ("p159", "http://schemas.microsoft.com/office/powerpoint/2015/09/main"),
        ("p188", "http://schemas.microsoft.com/office/powerpoint/2018/8/main"),
        ("a14",  "http://schemas.microsoft.com/office/drawing/2010/main"),
        ("a16",  "http://schemas.microsoft.com/office/drawing/2014/main"),
        ("am3d", "http://schemas.microsoft.com/office/drawing/2017/model3d"),
    };

    // Collect the cNvPr ids that this slide's emitted rows (items[start..])
    // actually carry — i.e. the shapes that will exist in the rebuilt slide.
    // Shapes emitted via raw-set slices carry <p:cNvPr id="N"> in their Xml;
    // id-preserving typed adds (e.g. a placeholder that is an animation target)
    // carry the id in Props["id"]. By-design-dropped shapes (OLE/3d/media whose
    // payload can't round-trip) emit NO row, so their id is absent here — which
    // is exactly how a dangling <p:timing> target is detected.
    private static HashSet<uint> ComputeEmittedShapeIds(List<BatchItem> items, int start)
    {
        var ids = new HashSet<uint>();
        var rx = new System.Text.RegularExpressions.Regex(
            @"<(?:\w+:)?cNvPr\b[^>]*\bid=""(\d+)""",
            System.Text.RegularExpressions.RegexOptions.Compiled);
        for (int i = start; i < items.Count; i++)
        {
            var it = items[i];
            if (!string.IsNullOrEmpty(it.Xml))
                foreach (System.Text.RegularExpressions.Match m in rx.Matches(it.Xml))
                    if (uint.TryParse(m.Groups[1].Value, out var v)) ids.Add(v);
            if (it.Props != null && it.Props.TryGetValue("id", out var idProp)
                && uint.TryParse(idProp, out var pv)) ids.Add(pv);
        }
        return ids;
    }

    // Prune <p:timing> animation steps whose target shape isn't in the rebuilt
    // slide. A verbatim timing passthrough still references a dropped shape's
    // cNvPr id via <p:spTgt spid="N"/>; PowerPoint rejects a deck whose
    // animation tree targets an absent shape ("could not open" / 0x80070570)
    // even though the SDK validator tolerates it. Remove the smallest enclosing
    // <p:par> (an independent timing step) for each dangling target; if nothing
    // dangles, return the input unchanged; if every animation step is pruned or
    // the result won't re-parse, return null so the caller drops <p:timing>
    // entirely (the slide simply has no animation — graceful degradation).
    private static string? PruneDanglingTimingTargets(string timingXml, HashSet<uint> emittedIds)
    {
        System.Xml.Linq.XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
        System.Xml.Linq.XDocument doc;
        try { doc = System.Xml.Linq.XDocument.Parse(EnsureAmbientXmlnsOnRootTag(timingXml)); }
        catch { return timingXml; } // unparseable — leave verbatim (best effort, no worse than before)

        bool removedAny = false;
        foreach (var spTgt in doc.Descendants(p + "spTgt").ToList())
        {
            var spid = spTgt.Attribute("spid")?.Value;
            if (spid == null || !uint.TryParse(spid, out var id)) continue;
            if (emittedIds.Contains(id)) continue;              // target survives — keep
            // Dangling: remove the nearest (innermost) <p:par> timing step.
            var par = spTgt.Ancestors(p + "par").FirstOrDefault();
            var toRemove = par ?? spTgt; // fall back to the bare target if no par wrapper
            if (toRemove.Parent != null) { toRemove.Remove(); removedAny = true; }
        }
        if (!removedAny) return timingXml;
        // If no animation behaviors survive, drop the whole timing tree.
        if (!doc.Descendants(p + "spTgt").Any() && !doc.Descendants(p + "cBhvr").Any())
            return null;
        var outXml = doc.Root!.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        try { System.Xml.Linq.XDocument.Parse(EnsureAmbientXmlnsOnRootTag(outXml)); }
        catch { return null; } // surgery left it malformed — drop timing rather than ship a bad slice
        return outXml;
    }

    private static string EnsureAmbientXmlnsOnRootTag(string xml)
    {
        if (string.IsNullOrEmpty(xml) || xml[0] != '<') return xml;
        var gtIdx = xml.IndexOf('>');
        if (gtIdx <= 0) return xml;
        var head = xml[..gtIdx];
        var tail = xml[gtIdx..];
        // For each ambient prefix that appears anywhere in the slice text but
        // is NOT already declared on the root tag, inject the canonical
        // xmlns:<prefix>="<uri>" pair. Pattern match keeps the helper text-
        // only so we don't need a parse for the parse precondition.
        foreach (var (prefix, uri) in SliceXmlnsBindings)
        {
            // Already declared somewhere in the head? Look for xmlns:<prefix>=.
            if (head.Contains($"xmlns:{prefix}=\"", StringComparison.Ordinal)) continue;
            // Used by an element or attribute name in the slice?
            if (!xml.Contains($"<{prefix}:", StringComparison.Ordinal)
                && !xml.Contains($" {prefix}:", StringComparison.Ordinal)) continue;
            // Inject xmlns:<prefix>="<uri>" inside the root tag, before the '>'.
            // Be defensive: head might be self-closing ("<tag/>"); place
            // declaration before the trailing /  if present.
            if (head.EndsWith('/'))
            {
                head = head[..^1] + $" xmlns:{prefix}=\"{uri}\"" + "/";
            }
            else
            {
                head = head + $" xmlns:{prefix}=\"{uri}\"";
            }
        }
        return head + tail;
    }

    private static string StripAmbientXmlnsFromRootTag(string xml)
    {
        if (string.IsNullOrEmpty(xml) || xml[0] != '<') return xml;
        var gtIdx = xml.IndexOf('>');
        if (gtIdx <= 0) return xml;
        var head = xml.Substring(0, gtIdx);
        var tail = xml.Substring(gtIdx);
        // Remove ` xmlns:p="…/presentationml/2006/main"` etc. only when the
        // URI matches the well-known ambient. Other xmlns decls (xmlns:p159,
        // xmlns:p14, …) stay — they are extension-scoped and must travel.
        var ambientUris = new (string Prefix, string Uri)[]
        {
            ("p",  "http://schemas.openxmlformats.org/presentationml/2006/main"),
            ("a",  "http://schemas.openxmlformats.org/drawingml/2006/main"),
            ("r",  "http://schemas.openxmlformats.org/officeDocument/2006/relationships"),
            ("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006"),
        };
        foreach (var (prefix, uri) in ambientUris)
        {
            var pat = $" xmlns:{prefix}=\"{uri}\"";
            head = head.Replace(pat, "");
        }
        return head + tail;
    }

    // Strip well-known pptx slide ambient namespace declarations from the
    // ROOT of a slice destined for raw-set into /p:sld. The slide root
    // always declares xmlns:p, xmlns:a, xmlns:r, xmlns:mc — the SDK's
    // round-trip serialization stamps them onto every direct child of the
    // slide root, so a slice extracted from the source's raw XML (no
    // root-level decls) and a slice extracted from the post-replay raw XML
    // (root-level decls present, courtesy of the SDK) would not compare
    // equal under CanonicalizeRawXml alone — that helper only strips
    // descendant-vs-root duplicates, never the root's own ambient decls.
    private static string StripSlideAmbientXmlns(string xml)
    {
        if (string.IsNullOrEmpty(xml) || !xml.StartsWith("<")) return xml;
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            if (doc.Root == null) return xml;
            // Match the slide root's known ambient namespaces. Any
            // declaration on the slice root pointing at one of these is
            // redundant once the slice is appended under /p:sld.
            var ambient = new Dictionary<string, string>
            {
                ["p"]  = "http://schemas.openxmlformats.org/presentationml/2006/main",
                ["a"]  = "http://schemas.openxmlformats.org/drawingml/2006/main",
                ["r"]  = "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
                ["mc"] = "http://schemas.openxmlformats.org/markup-compatibility/2006",
            };
            var toRemove = doc.Root.Attributes()
                .Where(a => a.IsNamespaceDeclaration
                            && ambient.TryGetValue(a.Name.LocalName, out var u)
                            && u == a.Value)
                .ToList();
            foreach (var a in toRemove) a.Remove();
            return doc.Root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        }
        catch { return xml; }
    }

    private static int SliceEnd(string xml, int start, string localName)
    {
        // Find the end of the element starting at `start`. Handles both
        // self-closing form (`<p:transition .../>`) and paired form
        // (`<p:transition ...> ... </p:transition>`).
        var gtIdx = xml.IndexOf('>', start);
        if (gtIdx < 0) return -1;
        // Self-closing: char before '>' is '/'.
        if (gtIdx > start && xml[gtIdx - 1] == '/')
            return gtIdx + 1;
        var closeTag = $"</{localName}>";
        var closeIdx = xml.IndexOf(closeTag, gtIdx, StringComparison.Ordinal);
        if (closeIdx < 0) return -1;
        return closeIdx + closeTag.Length;
    }

    // SmartArt passthrough: per slide, scan for <p:graphicFrame> hosts that
    // carry <dgm:relIds>; emit an `add-part smartart` row that creates the
    // four diagram sub-parts (data/layout/colors/quickStyle) under the
    // slide with the SOURCE's rIds pinned via --prop. Then emit four
    // raw-set replace rows (one per diagram part) and one raw-set append
    // on /p:sld/p:cSld/p:spTree carrying the graphicFrame XML verbatim.
    //
    // rId stability: pinning the rIds on add-part makes the graphicFrame's
    // <dgm:relIds dm=... lo=... cs=... qs=...> attributes resolve to the
    // same diagram parts after replay. Without pinning, AddNewPart<T>()
    // would allocate rId{slide+K} sequentially which would NOT match the
    // source's rIds and the SDK's serialized graphicFrame would drift on
    // re-emit.
    //
    // Diagram part XML canonicalization is the same shape-stripping/canon
    // pass as the slide-slice path; both passes need to be idempotent so
    // first emit (raw XML from source) and second emit (raw XML from
    // post-replay SDK-roundtripped doc) compare byte-equal.
    private static void EmitSmartArtsForSlide(PowerPointHandler ppt, int slideNum,
                                              string slidePath, List<BatchItem> items,
                                              SlideEmitContext ctx)
    {
        IReadOnlyList<PowerPointHandler.SmartArtInfo> smartArts;
        try { smartArts = ppt.GetSmartArtsOnSlide(slideNum); }
        catch { return; }
        if (smartArts.Count == 0) return;

        foreach (var sa in smartArts)
        {
            // add-part smartart with pinned rIds. Props carry the source's
            // rIds; replay's AddPart calls AddNewPart<T>(rId) for each.
            // `skip-frame=true` suppresses add-part's stub graphicFrame
            // injection — the raw-set append below carries the source's
            // full graphicFrame (with real position/size/name/cNvPr id), so
            // letting add-part also inject a stub would produce a duplicate.
            // Carry each diagram sub-part's verbatim (canonicalised) XML
            // inline so add-part writes the content directly into the parts
            // it creates. The legacy flow created empty seed parts and then
            // issued four separate `raw-set` rows targeting the SOURCE's
            // /ppt/diagrams/dataN.xml URIs — but the SDK allocates the new
            // parts under /ppt/graphics/dataM.xml (its own base, M global),
            // so FindPartByZipUri never resolved the raw-set target and the
            // parts persisted EMPTY (blank/broken SmartArt). Inlining the
            // content is URI-agnostic: the part is filled at creation, and
            // the real AddNewPart relationship (pinned rId) keeps it from
            // being pruned at save.
            var saProps = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["data"] = sa.DataRelId,
                ["layout"] = sa.LayoutRelId,
                ["colors"] = sa.ColorsRelId,
                ["quickStyle"] = sa.QuickStyleRelId,
                ["dataXml"] = CanonDiagramXml(sa.DataXml),
                ["layoutXml"] = CanonDiagramXml(sa.LayoutXml),
                ["colorsXml"] = CanonDiagramXml(sa.ColorsXml),
                ["quickStyleXml"] = CanonDiagramXml(sa.QuickStyleXml),
                ["skip-frame"] = "true",
            };
            // The DSP cached-drawing part (child of the data part, referenced
            // by <dsp:dataModelExt relId="...">). Carry it + the pinned relId
            // so the data XML's reference resolves — without it PowerPoint
            // refuses the file (0x80070570).
            if (sa.DrawingXml != null) saProps["drawingXml"] = CanonDiagramXml(sa.DrawingXml);
            if (sa.DrawingRelId != null) saProps["drawingRelId"] = sa.DrawingRelId;
            // Pictures embedded in the diagram: the data part and the DSP drawing
            // part each reference them via their own .rels with <a:blip r:embed>.
            // Carry the bytes + pinned rIds so replay re-attaches them to the
            // freshly-created diagram parts — otherwise the r:embed dangles and
            // PowerPoint refuses the whole deck (0x80070570). Flat numbered keys
            // (dataImage{k}.rid/.ct/.data) — the batch props dictionary is
            // string→string and the app's JSON layer is source-gen only (no
            // reflection serialization for a nested image list).
            EmitDiagramImageProps(saProps, "dataImage", sa.DataImages);
            EmitDiagramImageProps(saProps, "drawingImage", sa.DrawingImages);
            // External hyperlinks on diagram nodes (data + DSP drawing parts):
            // carry (rId, target) so replay re-adds the relationship and the
            // verbatim <a:hlinkClick r:id> resolves instead of dangling.
            EmitDiagramHyperlinkProps(saProps, "dataHlink", sa.DataHyperlinks);
            EmitDiagramHyperlinkProps(saProps, "drawingHlink", sa.DrawingHyperlinks);
            items.Add(new BatchItem
            {
                Command = "add-part",
                Parent = slidePath,
                Type = "smartart",
                Props = saProps,
            });

            // Append the graphicFrame into /p:sld/p:cSld/p:spTree. The
            // slice carries the <dgm:relIds> with the source's rIds, which
            // resolve to the just-created diagram parts via the pinned rIds.
            string gfCanon;
            try { gfCanon = NormalizeSlideRawSlice(sa.GraphicFrameXml); }
            catch { gfCanon = sa.GraphicFrameXml; }
            items.Add(new BatchItem
            {
                Command = "raw-set",
                Part = slidePath,
                Xpath = "/p:sld/p:cSld/p:spTree",
                Action = "append",
                Xml = gfCanon,
            });
        }
    }

    // Diagram sub-part XML is carried inline on the add-part smartart row and
    // written DIRECTLY into the created part's stream (whole-part body, not a
    // /p:sld slice). It must therefore stay self-contained: the source value
    // is the part root's SDK-serialized OuterXml, which already declares every
    // namespace it uses (dgm:, a:, r:, …) on its own root. We must NOT run it
    // through NormalizeSlideRawSlice — that canonicalizer is built for slices
    // that get re-parsed into a typed root at the /p:sld replay site and so it
    // STRIPS the ambient a:/r:/p:/mc: decls from the root tag, which would
    // leave a standalone diagram part with undeclared prefixes (MalformedXml).
    // Round-trip byte-stability holds because the rebuilt part is read back
    // via the same OuterXml path, yielding identical bytes on the next pass.
    private static string CanonDiagramXml(string partXml) => partXml;

    // Flatten a diagram part's referenced images into numbered string props the
    // add-part smartart handler reads back (see AttachDiagramImages). Keys:
    // {prefix}{k}.rid / .ct / .data.
    private static void EmitDiagramImageProps(
        Dictionary<string, string> props, string prefix,
        IReadOnlyList<PowerPointHandler.MasterImageInfo> images)
    {
        for (int k = 0; k < images.Count; k++)
        {
            props[$"{prefix}{k}.rid"] = images[k].RelId;
            props[$"{prefix}{k}.ct"] = images[k].ContentType;
            props[$"{prefix}{k}.data"] = images[k].Base64Data;
        }
    }

    // Flatten a diagram part's external hyperlink relationships into numbered
    // props the add-part smartart handler reads back (see AttachDiagramHyperlinks).
    // Keys: {prefix}{k}.rid / .target.
    private static void EmitDiagramHyperlinkProps(
        Dictionary<string, string> props, string prefix,
        IReadOnlyList<(string RelId, string Target)> hyperlinks)
    {
        for (int k = 0; k < hyperlinks.Count; k++)
        {
            props[$"{prefix}{k}.rid"] = hyperlinks[k].RelId;
            props[$"{prefix}{k}.target"] = hyperlinks[k].Target;
        }
    }

    // R48: slide-level <p:bg> raw passthrough. The bg slot sits inside
    // <p:cSld> BEFORE <p:spTree>, so the standard append-on-/p:sld helper
    // (which puts the slice at the end of <p:sld>) is the wrong target.
    // Prepend onto /p:sld/p:cSld puts the bg as the first child, matching
    // the cSld schema (bg → spTree). Image-fill bg carries a
    // <a:blipFill><a:blip r:embed="rIdN"> that the freshly-added replay
    // slide has no matching relationship for — Cluster E: mirror the
    // master/layout add-part image carrier (EmitMasterRawOne /
    // GetMasterImageParts) and emit one `add-part image` row per bg-referenced
    // rId BEFORE the bg raw-set, pinning the SOURCE rId so the verbatim
    // r:embed resolves on replay instead of dangling (broken-link background).
    // solidFill/gradFill/pattFill backgrounds carry no r:embed and round-trip
    // through the raw-set alone.
    private static void EmitRawSlideBgSlice(PowerPointHandler ppt, int slideNum,
                                            string slidePath, string sliceXml,
                                            List<BatchItem> items, SlideEmitContext ctx)
    {
        string canon;
        try { canon = NormalizeSlideRawSlice(sliceXml); }
        catch
        {
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: "p:bg",
                SlidePath: slidePath,
                Reason: "raw slice could not be canonicalised; element dropped"));
            return;
        }
        if (string.IsNullOrEmpty(canon))
        {
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: "p:bg",
                SlidePath: slidePath,
                Reason: "raw slice canonicalised to empty; element dropped"));
            return;
        }
        // Carry the referenced image part(s) so r:embed resolves. Scope to
        // the rIds the bg slice actually references — slide pictures already
        // round-trip via the typed `add picture` path (their own fresh rId),
        // so re-creating every slide ImagePart here would double-create them.
        var embedRids = new HashSet<string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(canon, @"r:embed=""([^""]+)"""))
            embedRids.Add(m.Groups[1].Value);
        if (embedRids.Count > 0)
        {
            try
            {
                foreach (var imageInfo in ppt.GetSlideImagePartsByRelId(slideNum, embedRids))
                {
                    items.Add(new BatchItem
                    {
                        Command = "add-part",
                        Parent = slidePath,
                        Type = "image",
                        Props = new Dictionary<string, string>
                        {
                            ["rid"] = imageInfo.RelId,
                            ["content-type"] = imageInfo.ContentType,
                            ["data"] = imageInfo.Base64Data,
                        },
                    });
                    embedRids.Remove(imageInfo.RelId);
                }
            }
            catch { /* best-effort — bg raw-set still runs */ }

            // Any bg-referenced rId we could NOT materialise (external link,
            // missing part) would dangle on replay — keep the honest warning.
            if (embedRids.Count > 0)
                ctx.Unsupported.Add(new UnsupportedWarning(
                    Element: "p:bg.image_rel",
                    SlidePath: slidePath,
                    Reason: "image-fill background references a slide-rels rId with no embeddable ImagePart; replay slide may show a missing-image marker"));
        }
        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = slidePath,
            Xpath = "/p:sld/p:cSld",
            Action = "prepend",
            Xml = canon,
        });
    }

    private static void EmitRawSlideSlice(string slidePath, string localName,
                                          string sliceXml, List<BatchItem> items,
                                          SlideEmitContext ctx)
    {
        // The replay target's freshly-added /slide[N] has no <p:transition>
        // and no <p:timing> (we stripped the semantic props upstream), so
        // raw-set "replace" against `/p:sld/p:transition` would fail with
        // "XPath matched no elements". Use append on /p:sld instead — the
        // OOXML schema order (cSld → clrMapOvr → transition → timing) is
        // preserved because we always emit transition before timing, and
        // neither was present before this append.
        string canon;
        try { canon = NormalizeSlideRawSlice(sliceXml); }
        catch
        {
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: localName,
                SlidePath: slidePath,
                Reason: "raw slice could not be canonicalised; element dropped"));
            return;
        }
        if (string.IsNullOrEmpty(canon))
        {
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: localName,
                SlidePath: slidePath,
                Reason: "raw slice canonicalised to empty; element dropped"));
            return;
        }
        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = slidePath,
            Xpath = "/p:sld",
            Action = "append",
            Xml = canon,
        });
    }

    // Emit one `add animation` BatchItem per effect attached to this shape.
    // Replay parent is the shape's positional path in the emitted document
    // (caller-supplied — must match the just-emitted `add shape/placeholder`).
    //
    // Previously animations were caught by ProbeUnsupportedOnSlide and surfaced
    // only as a warning, so dump→batch→replay lost every entrance/exit/emphasis
    // effect plus its trigger/delay/duration. The animation Query surface
    // already produces fine-grained nodes (effect/class/trigger/duration/delay/
    // direction/easein/easeout via PopulateAnimationNode); this helper just
    // forwards each animation's emittable props as an `add animation` row.
    //
    // Direction was added to PopulateAnimationNode in this same change — without
    // it, fly-down would round-trip as fly-up (AddAnimation default).
    //
    // Motion-path animations are routed through the verbatim raw-timing
    // passthrough (flagged exotic on presetClass="path" / <p:animMotion>), so
    // they are not re-emitted here. Other exotic timing
    // constructs (sequence groupings, conditional triggers) are silently
    // dropped — the visible effects round-trip.
    // Per-shape animation emit. Accepts a pre-filtered list of animation
    // nodes whose shape segment matches this shape (resolved by the caller
    // via the @id → positional map built from fullSlide.Children).
    private static void EmitAnimationsForShape(List<DocumentNode> animsForShape,
                                               string replayShapePath, List<BatchItem> items)
    {
        foreach (var anim in animsForShape)
        {
            var animProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Map Format keys → AddAnimation accepted keys. presetId is
            // derived from effect+class on Add, so emitting it would either
            // be ignored or trigger an unsupported_property warning.
            foreach (var (k, v) in anim.Format)
            {
                if (v == null) continue;
                if (k.Equals("presetId", StringComparison.OrdinalIgnoreCase)) continue;
                var s = v.ToString() ?? "";
                if (s.Length == 0) continue;
                animProps[k] = s;
            }
            if (animProps.Count == 0) continue;

            items.Add(new BatchItem
            {
                Command = "add",
                Parent = replayShapePath,
                Type = "animation",
                Props = animProps,
            });
        }
    }

    // Build a map from source @id (or source positional) to the list of
    // animation nodes on that shape. Query("animation") paths use either
    // /slide[N]/shape[@id=X]/animation[A] or /slide[N]/shape[K]/animation[A]
    // depending on whether cNvPr.Id is present.
    // R14-bug5: also accept the `chart[…]/animation[A]` path shape — Query
    // emits animations on a chart as /slide[N]/chart[@id=X]/animation[A]
    // (or positional chart[K]). Without this, chart-targeted animations
    // never landed in the per-slide index and EmitChart had no chance to
    // re-emit them.
    private static Dictionary<string, List<DocumentNode>> BuildSlideAnimationIndex(
        PowerPointHandler ppt, int slideNum)
    {
        var map = new Dictionary<string, List<DocumentNode>>(StringComparer.Ordinal);
        List<DocumentNode> all;
        try { all = ppt.Query("animation"); }
        catch { return map; }

        var slidePrefix = $"/slide[{slideNum}]/";
        // Capture both the host element name (shape | chart | picture | …)
        // and the selector inside the brackets so callers can lookup with
        // a prefix like "chart:@id=10" / "shape:5".
        var rx = new System.Text.RegularExpressions.Regex(
            @"^/slide\[\d+\]/(?<host>\w+)\[(?<sel>[^\]]+)\]/animation\[\d+\]$");
        foreach (var anim in all)
        {
            if (!anim.Path.StartsWith(slidePrefix, StringComparison.Ordinal)) continue;
            var m = rx.Match(anim.Path);
            if (!m.Success) continue;
            var host = m.Groups["host"].Value;
            var sel = m.Groups["sel"].Value;
            // Legacy callers expect bare "@id=…" / "5" keys for shape; preserve
            // that shape and emit a duplicate prefixed key for non-shape hosts.
            var legacyKey = host == "shape" ? sel : $"{host}:{sel}";
            if (!map.TryGetValue(legacyKey, out var list))
            {
                list = new List<DocumentNode>();
                map[legacyKey] = list;
            }
            list.Add(anim);
        }
        return map;
    }

    // R14-bug5: chart variant of GetAnimationsForChild. chart synthesizes its
    // own positional ordinal (separate from shape[]), so the index key is
    // either `chart:@id=X` or `chart:K`.
    private static List<DocumentNode> GetAnimationsForChartChild(
        Dictionary<string, List<DocumentNode>> map, DocumentNode chartChild, int chartOrdinal)
    {
        if (chartChild.Format.TryGetValue("id", out var cidObj) && cidObj != null)
        {
            var idKey = $"chart:@id={cidObj}";
            if (map.TryGetValue(idKey, out var byId)) return byId;
        }
        if (map.TryGetValue($"chart:{chartOrdinal}", out var byPos)) return byPos;
        return new List<DocumentNode>();
    }

    // Resolve the animation list for the shape currently being emitted.
    // child.Format["id"] (when present) maps to @id=X; otherwise positional.
    private static List<DocumentNode> GetAnimationsForChild(
        Dictionary<string, List<DocumentNode>> map, DocumentNode child, int sourcePositional)
    {
        // Try @id= form first when child carries id.
        if (child.Format.TryGetValue("id", out var cidObj) && cidObj != null)
        {
            var idKey = $"@id={cidObj}";
            if (map.TryGetValue(idKey, out var byId)) return byId;
        }
        // Fall back to positional.
        if (map.TryGetValue(sourcePositional.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            out var byPos))
            return byPos;
        return new List<DocumentNode>();
    }
}
