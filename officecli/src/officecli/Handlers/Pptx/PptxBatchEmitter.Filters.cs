// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using OfficeCli.Core;

namespace OfficeCli.Handlers;

public static partial class PptxBatchEmitter
{
    // Format keys that must NOT be emitted: derived (Get computes from cache),
    // diagnostic (relIds, cNvPr ids that resolve per package), or coordinate-
    // system (only meaningful in the source document). Same role as
    // WordBatchEmitter.SkipKeys.
    // CONSISTENCY(emit-filter-mirror): see WordBatchEmitter.Filters.cs:14.
    private static readonly HashSet<string> PptxSkipKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        // Internal relationship id — unstable across packages, see WordBatchEmitter.
        "relId",
        // CONSISTENCY(animation-spid-roundtrip): cNvPr id used to be a skip
        // key on the assumption that ids auto-renumber. But PowerPoint
        // animations reference target shapes by raw id (<p:spTgt spid="N"/>),
        // and the animation emitter forwards those ids verbatim — so renaming
        // every shape to a fresh 10000+ id at replay time leaves every
        // animation pointing at a dead shape. AcquireShapeId already honors
        // a caller-supplied id and throws on collision (instead of silently
        // renumbering), which is the right contract for dump→replay. Emit
        // the source id; collisions surface as batch errors that point at
        // the actual problem instead of silently breaking animations.
        // Cached display content for unevaluated fields. The `evaluated`
        // protocol surfaces this for diagnostic Get only; replay would
        // re-emit an a:fld with stale text.
        "evaluated",
        // Aggregate child counts surface only on the Get tree (ChildCount).
        "shapeCount", "layoutCount",
        // Per-presentation metadata that auto-restamps (last-modified-by /
        // revision / created / modified). Mirrors Word's stance on
        // similar metadata.
        "revisionNumber", "lastModifiedBy", "created", "modified",
        // Default font + slide dimensions live at the root presentation
        // node, not slide-level — they roll up into a single root `set /`
        // bag in PR2 (or are already set on the blank-doc baseline).
        "defaultFont",
        // Slide `layoutType` is a derived Get-side descriptor (resolved from
        // the slide's layout relationship — "title", "twoContent", …). Replay
        // drives layout selection via `layout=<name>`; emitting layoutType
        // additionally would surface as UNSUPPORTED on AddSlide and confuse
        // users into thinking the slide lost something.
        "layoutType",
        // Speaker notes text is surfaced on the slide Format bag by
        // NodeBuilder, but AddSlide doesn't accept a `notes=` prop —
        // notes are replayed by EmitNotes as a separate add-paragraph
        // sequence under /slide[N]/notes. Without this filter, every
        // emitted slide carries a `notes=...` prop that AddSlide reports
        // as UNSUPPORTED, flipping the per-item success to false and
        // (per pre-R6 contract) the batch-level success too.
        "notes",
        // ReadShapeAnimation emits Format["animation"] / Format["animationN"]
        // on the shape node as a compound `effect-class-direction-duration`
        // string, originally used by the AddShape `animation=` prop. Dump
        // now emits a separate `add animation` row per effect
        // (EmitAnimationsForShape), so passing the compound through `add
        // shape` would double-add each effect on replay. Drop the
        // shape-level animation keys; the fine-grained rows carry trigger
        // / delay / direction / easing that the compound form loses.
        "animation",
        // R14-bug5: ReadShapeAnimation also surfaces chartBuild on the
        // chart node's Format bag (mirroring how `animation` lives there
        // for non-chart shapes). chart.set / chart.add do not consume
        // chartBuild — it belongs on the per-animation row built by
        // EmitAnimationsForShape, which reads it directly from the
        // per-animation Format bag exposed by Query("animation"). Without
        // this filter, dump emitted `chartBuild=category` as a chart
        // add prop AND the animation row was missing entirely (bug 5
        // double-fault).
        "chartBuild",
        // hmerge / vmerge are Get-side continuation markers (NodeBuilder /
        // Query emit them on cells that participate in a horizontal or
        // vertical span). Set has no case for them — `merge.right=N` /
        // `merge.down=N` (or gridSpan / rowSpan on the anchor cell) are the
        // canonical write paths and already stamp the continuation cells'
        // hMerge / vMerge attributes via OneOnBool(). Emitting hmerge=true
        // on dump→batch would either fall through to the OOXML reflection
        // fallback (which serialises BooleanValue(true) as the literal
        // string "true", producing hMerge="true" instead of the canonical
        // "1" PowerPoint writes) or be rejected as unsupported. Strip both
        // from the emitter so cell-merge round-trips ride on the anchor's
        // gridSpan / rowSpan (which DO route through OneOnBool).
        // CONSISTENCY(merge-bool-form): see PowerPointHandler.ShapeProperties
        // OneOnBool helper (R43 779099bc) — same lexical-form concern as
        // the setter pinned to "1".
        "hmerge", "vmerge",
    };

    // Shape-level `animation` is filtered above. The same readback emits
    // `animation2`, `animation3`, ... for shapes carrying multiple effects;
    // strip those alongside the singular form.
    private static bool IsShapeLevelAnimationKey(string key)
    {
        if (key.StartsWith("animation", StringComparison.OrdinalIgnoreCase)
            && key.Length > "animation".Length
            && int.TryParse(key.AsSpan("animation".Length), out _))
            return true;
        return false;
    }

    private static Dictionary<string, string> FilterEmittableProps(Dictionary<string, object?> raw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, val) in raw)
        {
            if (PptxSkipKeys.Contains(key)) continue;
            if (IsShapeLevelAnimationKey(key)) continue;
            // CONSISTENCY(effective-X-mirror): docx WordBatchEmitter.Filters.cs
            // applies the same `effective.*` prefix filter — those are read-only
            // cascade snapshots, never user-settable.
            if (key.StartsWith("effective.", StringComparison.OrdinalIgnoreCase)) continue;
            if (val == null) continue;
            string s = val switch
            {
                bool b => b ? "true" : "false",
                _ => val.ToString() ?? ""
            };
            if (s.Length > 0) result[key] = s;
        }
        // Get emits both fill=gradient (type marker) and gradient=<spec> (params).
        // ApplyShapeFill would try parsing "gradient" as a color and reject; the
        // spec via gradient= already drives the fill. Same logic for pattern.
        if (result.TryGetValue("fill", out var fillVal))
        {
            if (fillVal.Equals("gradient", StringComparison.OrdinalIgnoreCase) && result.ContainsKey("gradient"))
                result.Remove("fill");
            else if (fillVal.Equals("pattern", StringComparison.OrdinalIgnoreCase) && result.ContainsKey("pattern"))
                result.Remove("fill");
            // fill="image" is a Get-side type marker for a blipFill (e.g. a table
            // cell or shape with an embedded picture fill). The image part is not
            // surfaced as a re-importable source, so replay would parse "image" as
            // a color and fail the op. Drop it — the element replays with default
            // fill until cell/shape image-fill round-trip is implemented end-to-end.
            // Mirrors the background="image" / image="true" filters below.
            else if (fillVal.Equals("image", StringComparison.OrdinalIgnoreCase))
                result.Remove("fill");
        }

        // Slide background="image" is a Get-side type marker — the embedded
        // image part is not surfaced as a re-importable path, so replay would
        // try to parse "image" as a color and reject. Drop the marker; the
        // slide will replay with default (inherited) background until image-
        // background round-trip is implemented end-to-end.
        if (result.TryGetValue("background", out var bgVal)
            && bgVal.Equals("image", StringComparison.OrdinalIgnoreCase))
            result.Remove("background");

        // Merge transitionSpeed into transition as a compound form
        // (e.g. `transition=fade` + `transitionSpeed=slow` → `transition=fade-slow`).
        // AddSlide/ApplyTransition only honor the compound form; emitting them
        // as two separate props would drop the speed on replay.
        if (result.TryGetValue("transitionSpeed", out var spd) && spd.Length > 0)
        {
            if (result.TryGetValue("transition", out var trans) && trans.Length > 0)
                result["transition"] = $"{trans}-{spd}";
            result.Remove("transitionSpeed");
        }

        // Same treatment for transitionDuration — ApplyTransition's parser
        // accepts an integer-ms modifier in the compound form (`fade-500`).
        // Emitting `transitionDuration=500` standalone fell through to the
        // generic prop-validator and the slide replay returned exit 2.
        if (result.TryGetValue("transitionDuration", out var dur) && dur.Length > 0)
        {
            if (result.TryGetValue("transition", out var trans) && trans.Length > 0)
                result["transition"] = $"{trans}-{dur}";
            result.Remove("transitionDuration");
        }

        // Shape image="true" is a NodeBuilder marker emitted for shapes
        // carrying a blipFill — Add has no shape-fill image importer, so
        // pass-through would fail prop validation. Mirror the
        // background="image" filter above; the shape replays with default
        // fill until shape image-fill round-trip is implemented.
        if (result.TryGetValue("image", out var imgVal)
            && imgVal.Equals("true", StringComparison.OrdinalIgnoreCase))
            result.Remove("image");

        // bt-B1: when reflectionRaw was captured, it carries the full
        // user-authored attrs (blurRad/stA/endA/dist/dir/sy/algn). The
        // companion reflection=preset key is informational only and would
        // overwrite the raw element with the preset shape if it ran later in
        // the Set pass. Drop it so the raw passthrough is authoritative.
        if (result.ContainsKey("reflectionRaw"))
            result.Remove("reflection");

        // bt-2: same shape as reflectionRaw — shadowRaw carries the verbatim
        // <a:outerShdw sx=… sy=… …>color</a:outerShdw>. The companion
        // shadow=COLOR-BLUR-… key would overwrite the raw element via
        // ApplyShadow → BuildOuterShadow (which doesn't know about sx/sy)
        // if it ran after the raw install.
        if (result.ContainsKey("shadowRaw"))
            result.Remove("shadow");

        // R56 bt-2: parallel — innerShadowRaw carries the verbatim
        // <a:innerShdw> with lumMod/lumOff color transforms that the
        // compressed innerShadow=COLOR-BLUR-… form encodes via the
        // undocumented `accent1+lumMod50+lumOff50-…` mixed syntax. Drop
        // the companion key so the raw install wins on Set replay.
        if (result.ContainsKey("innerShadowRaw"))
            result.Remove("innerShadow");

        // R58 bt-2: effectsRaw is the whole-effectLst passthrough — when
        // present, the compressed walker also emitted whichever well-known
        // children it recognized (shadow=, innerShadow=, glow=, reflection=,
        // softEdge=, blur=) and the per-child raw variants (shadowRaw etc.)
        // alongside the tint/lum/clrChange child it could not compress. On
        // Set, effectsraw replaces the entire effectLst wholesale — leaving
        // the companion keys in the bag risks the secondary appliers running
        // afterwards and re-introducing the dropped children's defaults. Drop
        // the entire companion set so the raw install is authoritative.
        if (result.ContainsKey("effectsRaw"))
        {
            result.Remove("shadow");
            result.Remove("shadowRaw");
            result.Remove("innerShadow");
            result.Remove("innerShadowRaw");
            result.Remove("glow");
            result.Remove("reflection");
            result.Remove("reflectionRaw");
            result.Remove("softEdge");
            result.Remove("blur");
            result.Remove("fillOverlayRaw");
        }

        // R64 bt-3: lineDashRaw is a verbatim <a:custDash> passthrough; the
        // companion lineDash=<token> would overwrite it on Set since the install
        // path clears both prstDash and custDash before appending. The two are
        // a CT_LineProperties choice anyway (EG_LineDashProperties), so they
        // can never coexist on the same outline — raw wins by definition.
        if (result.ContainsKey("lineDashRaw"))
            result.Remove("lineDash");

        // bt-B2: same shape as reflectionRaw — gradientRaw carries the
        // verbatim <a:gradFill flip=… ><a:tileRect/></a:gradFill>. The
        // companion semantic gradient=linear;… key would overwrite it via
        // ApplyGradientFill if it ran after the raw install.
        if (result.ContainsKey("gradientRaw"))
        {
            result.Remove("gradient");
            // fill=gradient marker is still consistent with the raw element
            // — leave it so the shape Type stays "gradient" on consumers
            // reading Format["fill"].
        }

        return result;
    }
}
