// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;
using DocumentFormat.OpenXml;

namespace OfficeCli.Core;

/// <summary>
/// SDK-authoritative schema-order placement for children added via the generic
/// reflection fallbacks (<see cref="GenericXmlQuery.TryCreateTypedChild"/>,
/// <see cref="TypedAttributeFallback"/>) and the hand-rolled run/paragraph
/// property inserters.
///
/// <para>
/// The OpenXML SDK's <c>AppendChild</c> only appends, and even
/// <c>OpenXmlCompositeElement.AddChild</c> does not reliably reposition. Strict
/// consumers (and OOXML schema validation) reject children that sit out of the
/// CT_* sequence — e.g. <c>&lt;w:kern&gt;</c> after <c>&lt;w:sz&gt;</c> in rPr,
/// or <c>&lt;w:pStyle&gt;</c> not first in pPr. PowerPoint silently drops
/// out-of-order DrawingML elements.
/// </para>
///
/// <para>
/// Rather than hand-maintain per-container order arrays (CT_RPr alone has ~40
/// children; the prior <c>InsertRunPropInSchemaOrder</c> switch silently
/// dropped <c>kern</c>/<c>w</c>/<c>position</c> to "append at end"), this reads
/// the SDK's own compiled particle comparator (<c>CompiledParticle.Compare</c>)
/// via reflection — the exact ordering the SDK validator uses. It is complete
/// by construction and stays correct as the schema evolves. See
/// <c>DrawingEffectsHelper.InsertEffectInSchemaOrder</c> for the hand-array
/// precedent this generalizes.
/// </para>
///
/// <para>
/// Placement is MINIMAL and SAFE: only the newly added child is moved, to the
/// first slot where it precedes an existing sibling. Existing children —
/// including foreign / unknown elements (<c>mc:AlternateContent</c>, extension
/// lists) that the comparator can't order — are never reordered. The comparator
/// sorts unknown elements to the front, so a blind full sort would corrupt
/// extension placement; single-child placement avoids that entirely.
/// </para>
/// </summary>
internal static class SchemaOrder
{
    // Per-parent-type comparator cache. The compiled particle is static per
    // element type and the comparison depends only on the child element types,
    // so caching by the parent's runtime type is safe across instances and
    // threads. A null entry means the type has no usable particle (leaf
    // elements, reflection shape changed) — callers then leave order as-is.
    private static readonly ConcurrentDictionary<Type, Comparison<OpenXmlElement>?> s_cmpCache = new();

    /// <summary>
    /// Move <paramref name="child"/> — already a child of <paramref name="parent"/>,
    /// typically just appended — to its schema-correct slot: immediately before
    /// the first existing sibling it should precede per the SDK particle order.
    /// No-op when the parent is not composite, has no compiled particle, the
    /// comparator can't order the child, or nothing should follow it.
    /// </summary>
    public static void Place(OpenXmlElement parent, OpenXmlElement child)
    {
        if (parent is not OpenXmlCompositeElement) return;
        if (!ReferenceEquals(child.Parent, parent)) return;

        var cmp = GetComparison(parent);
        if (cmp == null) return;

        OpenXmlElement? anchor = null;
        foreach (var sibling in parent.ChildElements)
        {
            if (ReferenceEquals(sibling, child)) continue;
            int order;
            try { order = cmp(child, sibling); }
            catch { return; } // comparator failure → leave as-is (already appended)
            if (order < 0) { anchor = sibling; break; }
        }

        // anchor == null: child belongs at/after the current tail → leave it
        // where it was appended. Otherwise hoist it before the first sibling it
        // precedes. The SDK's InsertBefore refuses an element that is still
        // attached ("part of a tree"), so detach first, then re-insert.
        if (anchor != null && !ReferenceEquals(child.NextSibling(), anchor))
        {
            child.Remove();
            parent.InsertBefore(child, anchor);
        }
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Reflecting over the SDK's internal element Metadata.Particle to reach the public CompiledParticle.Compare comparator. These members back the SDK's own validation and are preserved; if trimmed away, the probe returns null and callers leave child order unchanged (the prior, also-valid-enough behavior).")]
    private static Comparison<OpenXmlElement>? GetComparison(OpenXmlElement parent)
    {
        return s_cmpCache.GetOrAdd(parent.GetType(), static (_, p) =>
        {
            try
            {
                // Metadata is an instance property on OpenXmlElement; the
                // particle it exposes is identical across instances of the same
                // type, so building the delegate from any live instance and
                // caching by type is correct.
                var metaProp = typeof(OpenXmlElement).GetProperty("Metadata",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var meta = metaProp?.GetValue(p);
                var particle = meta?.GetType().GetProperty("Particle")?.GetValue(meta);
                if (particle == null) return null;
                var compare = particle.GetType().GetMethod("Compare",
                    new[] { typeof(OpenXmlElement), typeof(OpenXmlElement) });
                if (compare == null) return null;
                return (a, b) => (int)compare.Invoke(particle, new object[] { a, b })!;
            }
            catch
            {
                return null;
            }
        }, parent);
    }
}
