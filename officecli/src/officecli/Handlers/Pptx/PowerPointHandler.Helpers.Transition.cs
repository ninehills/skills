// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{

    /// <summary>
    /// Find existing Transition element or create one, avoiding duplicates with unknown-element transitions.
    /// </summary>
    private static Transition FindOrCreateTransition(Slide slide)
    {
        var typed = slide.GetFirstChild<Transition>();
        if (typed != null) return typed;

        // Check for unknown-element transitions (injected as raw XML to survive SDK serialization)
        var unknown = slide.ChildElements.FirstOrDefault(c => c.LocalName == "transition" && c is not Transition);
        if (unknown != null)
        {
            // Replace with a typed Transition so we can set properties
            var trans = new Transition();
            foreach (var attr in unknown.GetAttributes()) trans.SetAttribute(attr);
            trans.InnerXml = unknown.InnerXml;
            unknown.InsertAfterSelf(trans);
            unknown.Remove();
            return trans;
        }

        return slide.AppendChild(new Transition());
    }

    /// <summary>
    /// Set advanceTime on a slide, handling morph AlternateContent correctly.
    /// </summary>
    internal static void SetAdvanceTime(Slide slide, string value)
    {
        // OOXML @advTm is ST_PositiveUniversalMeasure (>= 0). Bare integer
        // milliseconds is the schema form; reject leading-minus or any
        // negative-prefixed numeric so advanceTime=-1 no longer silently
        // writes a malformed transition that PowerPoint either ignores or
        // mis-renders. Mirrors the >= 0 guard on border.width / padding.
        var trimmed = (value ?? "").Trim();
        // CONSISTENCY(advtime-none): help schema documents `advanceTime=none`
        // as the timer-clear sentinel; treat it before the numeric guard so
        // it doesn't get rejected as "non-negative integer". No-op when no
        // transition / advTm is present (matches the spirit of unsetting).
        var isClear = trimmed.Equals("none", StringComparison.OrdinalIgnoreCase)
            || trimmed.Length == 0
            || trimmed.Equals("false", StringComparison.OrdinalIgnoreCase);
        if (!isClear)
        {
            if (trimmed.StartsWith('-'))
                throw new ArgumentException($"Invalid advanceTime: '{value}' (must be >= 0).");
            // ST_PositiveUniversalMeasure is bare milliseconds (integer). Reject
            // non-numeric garbage like "later" or "5s" up front; PowerPoint
            // silently drops the attribute on open when it fails to parse, so a
            // malformed value used to land on disk with no error to the caller.
            if (!int.TryParse(trimmed, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                throw new ArgumentException($"Invalid advanceTime: '{value}' (expected a non-negative integer in milliseconds, or 'none' to clear).");
        }
        // Any mc:AlternateContent that wraps a <p:transition> descendant counts
        // here, not just morph — p14 (vortex/switch/flip/ripple/glitter/prism/doors/…)
        // and p15 (prstTrans) transitions also live inside mc:AlternateContent.
        // Falling through to FindOrCreateTransition would append a second bare
        // <p:transition> sibling, which PowerPoint rejects with 0x80070570.
        var acWrap = slide.ChildElements.FirstOrDefault(c =>
            c.LocalName == "AlternateContent"
            && c.Descendants().Any(d => d.LocalName == "transition"));
        if (acWrap != null)
        {
            foreach (var trans in acWrap.Descendants().Where(d => d.LocalName == "transition"))
            {
                if (isClear)
                    trans.RemoveAttribute("advTm", "");
                else
                    trans.SetAttribute(new OpenXmlAttribute("", "advTm", null!, trimmed));
            }
        }
        else
        {
            if (isClear)
            {
                // Clear advTm only if a transition already exists — don't
                // synthesize an empty <p:transition/> just to remove the attr.
                var existing = slide.GetFirstChild<Transition>();
                if (existing != null) existing.AdvanceAfterTime = null;
            }
            else
            {
                FindOrCreateTransition(slide).AdvanceAfterTime = trimmed;
            }
        }
    }

    /// <summary>
    /// Set advanceOnClick on a slide, handling morph AlternateContent correctly.
    /// </summary>
    internal static void SetAdvanceClick(Slide slide, bool value)
    {
        // See SetAdvanceTime: any AlternateContent that wraps a <p:transition>
        // (morph/p14/p15) must be updated in place rather than producing a
        // second bare sibling.
        var acWrap = slide.ChildElements.FirstOrDefault(c =>
            c.LocalName == "AlternateContent"
            && c.Descendants().Any(d => d.LocalName == "transition"));
        if (acWrap != null)
        {
            foreach (var trans in acWrap.Descendants().Where(d => d.LocalName == "transition"))
            {
                // Schema default for CT_SlideTransition @advClick is true. Strip the attribute
                // when value matches default to avoid writing redundant XML on round-trip.
                if (value)
                    trans.RemoveAttribute("advClick", "");
                else
                    trans.SetAttribute(new OpenXmlAttribute("", "advClick", null!, "0"));
            }
        }
        else
        {
            var trans = FindOrCreateTransition(slide);
            if (value)
                trans.AdvanceOnClick = null; // schema default = true; strip attribute
            else
                trans.AdvanceOnClick = false;
        }
    }
}
