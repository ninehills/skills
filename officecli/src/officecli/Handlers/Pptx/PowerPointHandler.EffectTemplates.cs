// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Presentation;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{
    // ==================== Effect Templates ====================
    //
    // PowerPoint's "Moderate" and "Exciting" entrance/exit effects (Boomerang,
    // Pinwheel, Curve Down, Spiral Out, Center Revolve, ...) can't be expressed
    // as a single <p:animEffect filter="..."> — each is a verbatim
    // <p:childTnLst> body with multiple <p:anim> primitives carrying property
    // formulas (ppt_x, ppt_w, sin/cos keyframes, motion paths). The forms are
    // captured by saving a PowerPoint-authored deck and copying the resulting
    // inner XML into Handlers/Pptx/EffectTemplates/*.xml as embedded resources.
    //
    // At apply time we:
    //   1. Look up (effect, class) in _templateRegistry → resource name + presetID + presetSubtype
    //   2. Load the resource's text content
    //   3. Substitute {SPID} with the target shape ID
    //   4. Renumber {ID0}..{IDn} placeholders to unique cTn IDs (allocated
    //      relative to the slide's nextId counter)
    //   5. Optionally inject <p:graphicEl><a:chart .../></p:graphicEl> into
    //      every <p:spTgt/> for chart per-element fan-out
    //   6. Parse into a ChildTimeNodeList via the SDK, attach to the click-group
    //
    // Templates target style.visibility="hidden" at the tail (exit) or
    // "visible" at the head (entrance). CONSISTENCY(animation-template):
    // duration override is not currently parameterized — the template's
    // PowerPoint default duration is preserved.

    internal record EffectTemplate(
        int PresetId,
        int PresetSubtype,
        string ResourceName);

    // (effectName lowercase, class) → template metadata.
    // Class-agnostic effect names (e.g. "boomerang") that need both entrance
    // and exit forms register twice with different resource names.
    private static readonly Dictionary<(string, TimeNodePresetClassValues), EffectTemplate>
        _templateRegistry = new()
        {
            // Exit effects — Subtle / Moderate / Exciting subset that PowerPoint
            // emits with complex anim primitives, not a single filter.
            [("contract",       TimeNodePresetClassValues.Exit)] = new(55, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_contract.xml"),
            [("centerrevolve",  TimeNodePresetClassValues.Exit)] = new(43, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_center_revolve.xml"),
            [("collapse",       TimeNodePresetClassValues.Exit)] = new(17, 10, "OfficeCli.Handlers.Pptx.EffectTemplates.exit_collapse.xml"),
            [("floatout",       TimeNodePresetClassValues.Exit)] = new(42, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_float_out.xml"),
            [("shrinkturn",     TimeNodePresetClassValues.Exit)] = new(31, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_shrink_turn.xml"),
            [("sinkdown",       TimeNodePresetClassValues.Exit)] = new(37, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_sink_down.xml"),
            [("spinner",        TimeNodePresetClassValues.Exit)] = new(49, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_spinner.xml"),
            [("basiczoom",      TimeNodePresetClassValues.Exit)] = new(23, 32, "OfficeCli.Handlers.Pptx.EffectTemplates.exit_basic_zoom.xml"),
            [("stretchy",       TimeNodePresetClassValues.Exit)] = new(50, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_stretchy.xml"),
            [("boomerang",      TimeNodePresetClassValues.Exit)] = new(25, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_boomerang.xml"),
            [("credits",        TimeNodePresetClassValues.Exit)] = new(28, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_credits.xml"),
            [("curvedown",      TimeNodePresetClassValues.Exit)] = new(52, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_curve_down.xml"),
            // Float-Out is a Moderate exit variant (presetID 42); plain Float
            // is an Exciting exit variant (presetID 30) — different anims.
            [("float",          TimeNodePresetClassValues.Exit)] = new(30, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_float.xml"),
            [("pinwheel",       TimeNodePresetClassValues.Exit)] = new(35, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_pinwheel.xml"),
            [("spiralout",      TimeNodePresetClassValues.Exit)] = new(15, 0,  "OfficeCli.Handlers.Pptx.EffectTemplates.exit_spiral_out.xml"),
            [("basicswivel",    TimeNodePresetClassValues.Exit)] = new(19, 10, "OfficeCli.Handlers.Pptx.EffectTemplates.exit_basic_swivel.xml"),

            // Emphasis effects — Basic / Subtle / Moderate that PowerPoint
            // emits with multi-anim primitives (animClr / animRot / animScale /
            // animEffect). Captured from a PowerPoint-authored deck.
            [("fillcolor",          TimeNodePresetClassValues.Emphasis)] = new(1,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_fillcolor.xml"),
            [("growshrink",         TimeNodePresetClassValues.Emphasis)] = new(6,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_growshrink.xml"),
            [("linecolor",          TimeNodePresetClassValues.Emphasis)] = new(7,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_linecolor.xml"),
            // 'spin' was previously preset 27 (no template); the PowerPoint-authored
            // form is preset 8 with an <p:animRot> primitive. Template wins.
            [("spin",               TimeNodePresetClassValues.Emphasis)] = new(8,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_spin.xml"),
            [("transparency",       TimeNodePresetClassValues.Emphasis)] = new(9,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_transparency.xml"),
            [("complementarycolor", TimeNodePresetClassValues.Emphasis)] = new(21, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_complementarycolor.xml"),
            [("complementarycolor2",TimeNodePresetClassValues.Emphasis)] = new(22, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_complementarycolor2.xml"),
            [("contrastingcolor",   TimeNodePresetClassValues.Emphasis)] = new(23, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_contrastingcolor.xml"),
            [("darken",             TimeNodePresetClassValues.Emphasis)] = new(24, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_darken.xml"),
            [("desaturate",         TimeNodePresetClassValues.Emphasis)] = new(25, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_desaturate.xml"),
            [("lighten",            TimeNodePresetClassValues.Emphasis)] = new(30, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_lighten.xml"),
            [("objectcolor",        TimeNodePresetClassValues.Emphasis)] = new(19, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_objectcolor.xml"),
            [("pulse",              TimeNodePresetClassValues.Emphasis)] = new(26, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_pulse.xml"),
            [("colorpulse",         TimeNodePresetClassValues.Emphasis)] = new(27, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_colorpulse.xml"),
            [("teeter",             TimeNodePresetClassValues.Emphasis)] = new(32, 0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_teeter.xml"),
            // Aliases (NormalizeEffectName strips non-alnum + lowercases, but
            // distinct stems still need explicit entries).
            [("grow",               TimeNodePresetClassValues.Emphasis)] = new(6,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_growshrink.xml"),
            [("shrink",             TimeNodePresetClassValues.Emphasis)] = new(6,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_growshrink.xml"),
            [("rotate",             TimeNodePresetClassValues.Emphasis)] = new(8,  0, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_spin.xml"),
            // Legacy 'bold' / 'boldflash' aliases: prior versions wrote presetID 1
            // (which is actually Fill Color in PowerPoint's table). Route through
            // the fillColor template so previously-written 'bold-emphasis-*' inputs
            // produce a working animation. Readback returns "fillColor" (canonical).
            [("bold",               TimeNodePresetClassValues.Emphasis)] = new(1,  2, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_fillcolor.xml"),
            [("boldflash",          TimeNodePresetClassValues.Emphasis)] = new(1,  2, "OfficeCli.Handlers.Pptx.EffectTemplates.emph_fillcolor.xml"),
        };

    // Aliases accepted on input — normalized to the canonical name before lookup.
    // E.g. "center-revolve" / "center revolve" / "centerRevolve" → "centerrevolve".
    private static string NormalizeEffectName(string effect) =>
        Regex.Replace(effect.ToLowerInvariant(), "[^a-z0-9]", "");

    internal static EffectTemplate? TryGetEffectTemplate(string effect, TimeNodePresetClassValues cls)
    {
        var key = NormalizeEffectName(effect);
        return _templateRegistry.TryGetValue((key, cls), out var t) ? t : null;
    }

    // Cache template bodies after first load (assembly reads are cheap but
    // we avoid the parsing churn on every animation Add).
    private static readonly Dictionary<string, string> _templateBodyCache = new();

    internal static string LoadEffectTemplateBody(string resourceName)
    {
        if (_templateBodyCache.TryGetValue(resourceName, out var cached)) return cached;
        var asm = typeof(PowerPointHandler).Assembly;
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded effect template missing: {resourceName}. Check Handlers/Pptx/EffectTemplates/ "
                + "+ the matching <EmbeddedResource> entry in officecli.csproj.");
        using var reader = new StreamReader(stream);
        var body = reader.ReadToEnd();
        _templateBodyCache[resourceName] = body;
        return body;
    }

    // Render a template into a parsable XML fragment: substitute {SPID} +
    // {IDx} placeholders, optionally inject a chart sub-element graphicEl
    // wrapper into every <p:spTgt/>. The result is wrapped in a root element
    // with the p: and a: namespaces declared, suitable for OpenXmlReader.
    // ref nextId advances by the number of cTn IDs the template consumes.
    internal static string RenderEffectTemplate(
        EffectTemplate tpl,
        string shapeId,
        ref uint nextId,
        (int seriesIdx, int categoryIdx, string bldStep)? chartTarget = null,
        int? durationMsOverride = null)
    {
        var body = LoadEffectTemplateBody(tpl.ResourceName);
        // Optional duration override: rewrite the dur attribute on the {ID0}
        // cTn (the outermost primitive's timeline). Internal child durations
        // (autoRev pulses, sub-keyframes) are preserved — only the top-level
        // length scales with user-supplied duration.
        if (durationMsOverride is int durMs && durMs > 0)
        {
            body = Regex.Replace(body,
                @"(id=""\{ID0\}""\s+)dur=""\d+""",
                $"$1dur=\"{durMs}\"");
        }
        // Count placeholders and allocate sequential IDs.
        var maxIdx = -1;
        foreach (Match m in Regex.Matches(body, @"\{ID(\d+)\}"))
        {
            var idx = int.Parse(m.Groups[1].Value);
            if (idx > maxIdx) maxIdx = idx;
        }
        for (int i = 0; i <= maxIdx; i++)
        {
            body = body.Replace($"{{ID{i}}}", nextId.ToString());
            nextId++;
        }
        body = body.Replace("{SPID}", shapeId);

        if (chartTarget.HasValue)
        {
            var (sIdx, cIdx, bldStep) = chartTarget.Value;
            // Inject <p:graphicEl><a:chart .../></p:graphicEl> into every spTgt.
            // Self-closing form (<p:spTgt spid="N"/>) AND empty-element form
            // (<p:spTgt spid="N"></p:spTgt>) both need to be widened.
            var graphicEl =
                $"<p:graphicEl><a:chart seriesIdx=\"{sIdx}\" categoryIdx=\"{cIdx}\" bldStep=\"{bldStep}\"/></p:graphicEl>";
            body = Regex.Replace(body,
                @"<p:spTgt spid=""([^""]+)""\s*/>",
                $"<p:spTgt spid=\"$1\">{graphicEl}</p:spTgt>");
        }

        // Wrap in a root with namespace declarations so the OpenXmlReader can
        // parse the fragment.
        return "<p:childTnLst "
            + "xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\" "
            + "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" "
            + "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
            + body
            + "</p:childTnLst>";
    }

    // Parse the rendered template XML into a ChildTimeNodeList element ready
    // to plug into a CommonTimeNode. Uses the outer-XML constructor (instead of
    // InnerXml=) so the p:/a:/r: namespace prefixes declared on the wrapper
    // survive into the loaded element tree — InnerXml strips the wrapper's
    // namespace declarations, causing "'a' is undeclared prefix" failures the
    // first time we inject <p:graphicEl><a:chart .../>...
    internal static ChildTimeNodeList ParseTemplateChildTimeNodeList(string xml)
    {
        return new ChildTimeNodeList(xml);
    }
}
