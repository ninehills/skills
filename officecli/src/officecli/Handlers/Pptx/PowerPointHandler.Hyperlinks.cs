// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{
    // ==================== Hyperlink helpers ====================

    // Result of resolving a user-supplied link string.
    // Exactly one of (Id, Action) corresponds to a jump; Id may be null when Action is a named
    // action that requires no relationship (firstslide, lastslide, nextslide, previousslide).
    private readonly struct HyperlinkTarget
    {
        public string? Id { get; init; }
        public string? Action { get; init; }
        public bool IsExternal { get; init; }
    }

    /// <summary>
    /// Resolve a user-supplied link string into a hyperlink target. Returns null to mean "remove".
    /// Supports:
    ///   - Absolute URI (https://, mailto:, etc.)        → external relationship
    ///   - slide[N]                                      → internal slide jump (ppaction://hlinksldjump)
    ///   - firstslide/lastslide/nextslide/previousslide  → named PowerPoint actions
    /// </summary>
    private static HyperlinkTarget? ResolveHyperlinkTarget(SlidePart slidePart, string url)
    {
        if (string.IsNullOrEmpty(url) || url.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;

        // Named slide-action shortcuts (no relationship required). 'endshow'
        // is the PowerPoint action that terminates the slide show — emitted
        // as ppaction://hlinkshowjump?jump=endshow same as the navigation
        // jumps. 'prevslide' is intentionally absent: keep 'previousslide'
        // as the only canonical form so input and Get output match (no
        // silent normalization).
        var lower = url.Trim().ToLowerInvariant();
        switch (lower)
        {
            case "firstslide":
                return new HyperlinkTarget { Action = "ppaction://hlinkshowjump?jump=firstslide" };
            case "lastslide":
                return new HyperlinkTarget { Action = "ppaction://hlinkshowjump?jump=lastslide" };
            case "nextslide":
                return new HyperlinkTarget { Action = "ppaction://hlinkshowjump?jump=nextslide" };
            case "previousslide":
                return new HyperlinkTarget { Action = "ppaction://hlinkshowjump?jump=previousslide" };
            case "endshow":
                return new HyperlinkTarget { Action = "ppaction://hlinkshowjump?jump=endshow" };
        }

        // Explicit slide[N] jump
        var m = Regex.Match(url.Trim(), @"^slide\[(\d+)\]$", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var slideIdx = int.Parse(m.Groups[1].Value);
            var pres = slidePart.OpenXmlPackage as PresentationDocument
                ?? throw new InvalidOperationException("SlidePart is not in a PresentationDocument");
            var allSlides = pres.PresentationPart?.SlideParts.ToList()
                ?? throw new InvalidOperationException("PresentationPart missing");
            if (slideIdx < 1 || slideIdx > allSlides.Count)
                throw new ArgumentException(
                    $"Slide jump target out of range: slide[{slideIdx}] (total {allSlides.Count}). " +
                    $"A slide-jump link can only target a slide that already exists — add slide[{slideIdx}] " +
                    "first, then set the link (forward references are not buffered outside batch mode).");
            var targetSlide = allSlides[slideIdx - 1];

            // Reuse an existing slide-to-slide relationship if present
            string? relId = null;
            foreach (var rel in slidePart.Parts)
            {
                if (ReferenceEquals(rel.OpenXmlPart, targetSlide))
                {
                    relId = rel.RelationshipId;
                    break;
                }
            }
            if (relId == null)
                relId = slidePart.CreateRelationshipToPart(targetSlide);

            return new HyperlinkTarget
            {
                Id = relId,
                Action = "ppaction://hlinksldjump",
            };
        }

        // Otherwise treat as external. Both absolute URIs (http/mailto/file/…)
        // AND relative file targets round-trip: an OOXML external hyperlink may
        // carry a relative target — a link to a local document beside the deck,
        // e.g. "项目指南\合同书.docx" with TargetMode="External". PowerPoint
        // authors these for "link to existing file" hyperlinks; rejecting them
        // dropped the entire run/placeholder on replay (content loss, not just a
        // dead link). RequireSafeScheme below no-ops on schemeless relative
        // targets and still blocks dangerous absolute schemes.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && !Uri.TryCreate(url, UriKind.Relative, out uri))
            throw new ArgumentException(
                $"Invalid hyperlink URL '{url}'. Expected an absolute URI (e.g. 'https://example.com'), " +
                $"a relative file target, 'slide[N]', or a named action (firstslide/lastslide/nextslide/previousslide/endshow).");
        // CONSISTENCY(hyperlink-scheme-allowlist): reject javascript:, file:,
        // data:, vbscript:, … before they reach the relationships file.
        // Mirrored in ExcelHandler.Set.cs cell link + WordHandler.Set.Element.cs
        // run/hyperlink writers; ppaction:// URIs are accepted (allowlisted).
        Core.HyperlinkUriValidator.RequireSafeScheme(url, "link");
        // Dedupe external hyperlink rels — AddHyperlinkRelationship always
        // mints a fresh rId without checking existing relationships, so
        // repeated set ops for the same URL (e.g. dump → batch round-trip
        // emitting `link=URL` on the shape add and the paragraph set bag both
        // for the same shape) created N rels pointing at the same URI. Reuse
        // any external hyperlink relationship whose Uri matches the requested
        // one; mirrors the slide-jump branch above (it already reuses).
        foreach (var hl in slidePart.HyperlinkRelationships)
        {
            if (hl.IsExternal && hl.Uri == uri)
                return new HyperlinkTarget { Id = hl.Id, IsExternal = true };
        }
        var extRel = slidePart.AddHyperlinkRelationship(uri, isExternal: true);
        return new HyperlinkTarget { Id = extRel.Id, IsExternal = true };
    }

    private static Drawing.HyperlinkOnClick BuildHyperlinkElement(HyperlinkTarget target, string? tooltip)
    {
        var hlink = new Drawing.HyperlinkOnClick();
        // r:id is required by schema — use empty string when no relationship exists (named actions).
        hlink.Id = target.Id ?? "";
        if (!string.IsNullOrEmpty(target.Action))
            hlink.Action = target.Action;
        if (!string.IsNullOrEmpty(tooltip))
        {
            // Tooltip becomes an attribute value in the saved part XML. Without
            // a guard here, codepoints outside XML 1.0 character data (e.g.
            // U+0007 BEL) escape unrejected and surface as a raw XmlException
            // at PackageProperties.Save() — the catch site then poisons the
            // open package and the next Close writes an empty file over the
            // user's deck. Mirror Add.Text / Add.Shape: validate at the write
            // boundary so the caller sees an `invalid_value` CliException
            // instead of an opaque OOXML save failure.
            Core.XmlTextValidator.ValidateOrThrow(tooltip, "tooltip");
            hlink.Tooltip = tooltip;
        }
        return hlink;
    }

    /// <summary>
    /// `add --type hyperlink /slide[N]/shape[M]` — attach (or replace) a hyperlink on
    /// an existing shape. Path resolution lives outside Resolve.cs's table/placeholder
    /// switch, so this dispatch entry parses /slide[N]/shape[M] directly.
    /// </summary>
    private string AddHyperlinkOnShape(string parentPath, Dictionary<string, string> properties)
    {
        var m = Regex.Match(parentPath, @"^/slide\[(\d+)\]/shape\[(\d+)\]$");
        if (!m.Success)
            throw new ArgumentException(
                "hyperlink must be added to a shape parent: /slide[N]/shape[M]");
        if (!properties.TryGetValue("link", out var url) && !properties.TryGetValue("url", out url))
            throw new ArgumentException(
                "hyperlink requires --prop link=<url>. Example: --prop link=https://example.com");
        var tooltip = properties.GetValueOrDefault("tooltip");
        var sIdx = int.Parse(m.Groups[1].Value);
        var shIdx = int.Parse(m.Groups[2].Value);
        var (slidePart, shape) = ResolveShape(sIdx, shIdx);
        ApplyShapeHyperlink(slidePart, shape, url, tooltip);
        // Echo the canonical /hyperlink sub-path (mirrors the Get path the help
        // schema documents) rather than the bare shape parent, so a follow-up
        // Get on the returned handle resolves the hyperlink node directly.
        var shapePathSeg = BuildElementPathSegment("shape", shape, shIdx);
        return $"/slide[{sIdx}]/{shapePathSeg}/hyperlink";
    }

    /// <summary>
    /// Apply a hyperlink to a shape. Pass "none" or "" to remove.
    /// Stores on nvSpPr/cNvPr (canonical OOXML shape-level location) and also on every run
    /// (for Office compat: some readers rely on run-level hyperlinks to render the shape as clickable).
    /// </summary>
    private static void ApplyShapeHyperlink(SlidePart slidePart, Shape shape, string url, string? tooltip = null)
    {
        var nvDp = shape.NonVisualShapeProperties?.NonVisualDrawingProperties;
        var allRuns = shape.Descendants<Drawing.Run>().ToList();

        if (string.IsNullOrEmpty(url) || url.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            nvDp?.RemoveAllChildren<Drawing.HyperlinkOnClick>();
            foreach (var run in allRuns)
                run.RunProperties?.GetFirstChild<Drawing.HyperlinkOnClick>()?.Remove();
            return;
        }

        var target = ResolveHyperlinkTarget(slidePart, url);
        if (target == null) return;

        // Shape-level element on nvSpPr/cNvPr
        if (nvDp != null)
        {
            nvDp.RemoveAllChildren<Drawing.HyperlinkOnClick>();
            nvDp.AppendChild(BuildHyperlinkElement(target.Value, tooltip));
        }

        // Also mirror onto every run so in-text clicks work too. Same
        // ordering reasoning as ApplyRunHyperlink: hlinkClick is slot 11
        // in CT_TextCharacterProperties so InsertAt(0) lands it before
        // pre-existing fill/font children. Append + reorder to land in
        // the right schema slot.
        foreach (var run in allRuns)
        {
            var rProps = run.RunProperties ?? (run.RunProperties = new Drawing.RunProperties());
            rProps.RemoveAllChildren<Drawing.HyperlinkOnClick>();
            rProps.AppendChild(BuildHyperlinkElement(target.Value, tooltip));
            ReorderDrawingRunProperties(rProps);
        }
    }

    /// <summary>
    /// Apply a click-hyperlink to a group. Groups have no runs of their own,
    /// so the link lives on the group's NonVisualDrawingProperties
    /// (nvGrpSpPr/cNvPr) — mirroring how PowerPoint writes click-targets on
    /// grouped shapes. Pass "none" or "" to remove.
    /// </summary>
    private static void ApplyGroupHyperlink(SlidePart slidePart, GroupShape grp, string url, string? tooltip = null)
    {
        var nvDp = grp.NonVisualGroupShapeProperties?.NonVisualDrawingProperties;
        if (nvDp == null) return;

        if (string.IsNullOrEmpty(url) || url.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            nvDp.RemoveAllChildren<Drawing.HyperlinkOnClick>();
            return;
        }

        var target = ResolveHyperlinkTarget(slidePart, url);
        if (target == null) return;

        nvDp.RemoveAllChildren<Drawing.HyperlinkOnClick>();
        nvDp.AppendChild(BuildHyperlinkElement(target.Value, tooltip));
    }

    /// <summary>
    /// Apply a click-hyperlink to a picture. Pictures have no runs (BlipFill
    /// is image data, not text), so the link lives only on the picture's
    /// NonVisualDrawingProperties (nvPicPr/cNvPr) — mirroring how PowerPoint
    /// writes click-targets on inserted images. Pass "none" or "" to remove.
    /// </summary>
    private static void ApplyPictureHyperlink(SlidePart slidePart, Picture picture, string url, string? tooltip = null)
    {
        var nvDp = picture.NonVisualPictureProperties?.NonVisualDrawingProperties;
        if (nvDp == null) return;

        if (string.IsNullOrEmpty(url) || url.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            nvDp.RemoveAllChildren<Drawing.HyperlinkOnClick>();
            return;
        }

        var target = ResolveHyperlinkTarget(slidePart, url);
        if (target == null) return;

        nvDp.RemoveAllChildren<Drawing.HyperlinkOnClick>();
        nvDp.AppendChild(BuildHyperlinkElement(target.Value, tooltip));
    }

    /// <summary>
    /// Apply a hyperlink to a single run. Pass "none" or "" to remove.
    /// </summary>
    private static void ApplyRunHyperlink(SlidePart slidePart, Drawing.Run run, string url, string? tooltip = null)
    {
        var rProps = run.RunProperties ?? (run.RunProperties = new Drawing.RunProperties());
        rProps.RemoveAllChildren<Drawing.HyperlinkOnClick>();

        if (string.IsNullOrEmpty(url) || url.Equals("none", StringComparison.OrdinalIgnoreCase))
            return;

        var target = ResolveHyperlinkTarget(slidePart, url);
        if (target == null) return;
        // CT_TextCharacterProperties places hlinkClick at slot 11 (after
        // ln/fill/effectLst/highlight/underline/font children). InsertAt(.., 0)
        // would land it before any pre-existing solidFill/latin/ea, producing
        // Sch_UnexpectedElementContentExpectingComplex. Append then reorder
        // so the helper's ordering table is the single source of truth.
        rProps.AppendChild(BuildHyperlinkElement(target.Value, tooltip));
        ReorderDrawingRunProperties(rProps);
    }

    /// <summary>
    /// Read the hyperlink URL + tooltip for a shape — shape-level (nvSpPr/cNvPr)
    /// first, falling back to the first run's rPr. Mirrors the read order in
    /// NodeBuilder's shape Get so the /hyperlink sub-path and the shape-level
    /// link= readback agree. Returns (null, null) when the shape has no link.
    /// </summary>
    private static (string? url, string? tooltip) ReadShapeHyperlink(Shape shape, OpenXmlPart part)
    {
        var nvDp = shape.NonVisualShapeProperties?.NonVisualDrawingProperties;
        var shapeHl = nvDp?.GetFirstChild<Drawing.HyperlinkOnClick>();
        var url = ReadHyperlinkOnClickUrl(shapeHl, part);
        var tooltip = shapeHl?.Tooltip?.Value;
        if (url == null)
        {
            var firstRun = shape.Descendants<Drawing.Run>().FirstOrDefault();
            var runHl = firstRun?.RunProperties?.GetFirstChild<Drawing.HyperlinkOnClick>();
            url = ReadHyperlinkOnClickUrl(runHl, part);
            tooltip = runHl?.Tooltip?.Value ?? tooltip;
        }
        return (url, string.IsNullOrEmpty(tooltip) ? null : tooltip);
    }

    /// <summary>
    /// Read the hyperlink URL from a run's RunProperties. Returns null if no hyperlink.
    /// </summary>
    private static string? ReadRunHyperlinkUrl(Drawing.Run run, OpenXmlPart part)
    {
        var hlClick = run.RunProperties?.GetFirstChild<Drawing.HyperlinkOnClick>();
        return ReadHyperlinkOnClickUrl(hlClick, part);
    }

    /// <summary>
    /// Read the friendly URL form of a HyperlinkOnClick element (action-name,
    /// slide[N], or absolute URI). Returns null if absent or unresolvable.
    /// Shared by run-level (text) and shape-level (cNvPr) hyperlinks —
    /// including picture-level, which uses the same nvDp/cNvPr slot.
    /// </summary>
    private static string? ReadHyperlinkOnClickUrl(Drawing.HyperlinkOnClick? hlClick, OpenXmlPart part)
    {
        if (hlClick == null) return null;
        var id = hlClick.Id?.Value;
        var action = hlClick.Action?.Value;

        // Named actions (no relationship) → reverse-map ppaction:// strings back to
        // the friendly names accepted by ResolveHyperlinkTarget so 'set link=firstslide'
        // round-trips through 'get'.
        if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(action))
        {
            const string showJumpPrefix = "ppaction://hlinkshowjump?jump=";
            if (action.StartsWith(showJumpPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var jump = action[showJumpPrefix.Length..].ToLowerInvariant();
                return jump switch
                {
                    "firstslide" or "lastslide" or "nextslide" or "previousslide" or "endshow" => jump,
                    _ => action
                };
            }
            return action;
        }

        if (id == null) return null;
        try
        {
            var rel = part.HyperlinkRelationships.FirstOrDefault(r => r.Id == id);
            // Prefer Uri.OriginalString over Uri.ToString(): ToString() normalises
            // path-empty URIs by injecting a "/" before the query (so
            // https://example.com round-trips as https://example.com/, and
            // ppaction://hlinkshowjump?jump=firstslide as
            // ppaction://hlinkshowjump/?jump=firstslide). OriginalString preserves
            // the exact form the user supplied at Add/Set time, matching how
            // PowerPoint persists rels/*.rels Target= verbatim.
            if (rel?.Uri != null) return rel.Uri.OriginalString;
            // Internal slide-jump: relationship is to another SlidePart, not a hyperlink relationship
            if (part is SlidePart sp)
            {
                foreach (var irel in sp.Parts)
                {
                    if (irel.RelationshipId == id && irel.OpenXmlPart is SlidePart target)
                    {
                        var pres = sp.OpenXmlPackage as PresentationDocument;
                        var idx = pres?.PresentationPart?.SlideParts.ToList().IndexOf(target) ?? -1;
                        if (idx >= 0) return $"slide[{idx + 1}]";
                    }
                }
            }
            return null;
        }
        catch { return null; }
    }
}
