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

    private static bool IsTruthy(string? value) =>
        ParseHelpers.IsTruthy(value);

    private static bool IsValidBooleanString(string? value) =>
        ParseHelpers.IsValidBooleanString(value);

    // R57 bt-3: parse a `compressionState=` Add/Set input into the typed
    // CT_BlipCompression enum. Accept the schema literals (email, print,
    // hqprint, screen, none) case-insensitively plus a forgiving alias for
    // "highqualityprint" since the SDK constant is HighQualityPrint.
    // Throws ArgumentException on anything else so bad values surface as
    // invalid_value rather than silently writing default compression.
    private static EnumValue<Drawing.BlipCompressionValues>
        ParseBlipCompressionState(string value)
    {
        var v = (value ?? string.Empty).Trim().ToLowerInvariant();
        return v switch
        {
            "email" => new EnumValue<Drawing.BlipCompressionValues>(Drawing.BlipCompressionValues.Email),
            "print" => new EnumValue<Drawing.BlipCompressionValues>(Drawing.BlipCompressionValues.Print),
            "hqprint" or "highqualityprint" => new EnumValue<Drawing.BlipCompressionValues>(Drawing.BlipCompressionValues.HighQualityPrint),
            "screen" => new EnumValue<Drawing.BlipCompressionValues>(Drawing.BlipCompressionValues.Screen),
            "none" => new EnumValue<Drawing.BlipCompressionValues>(Drawing.BlipCompressionValues.None),
            _ => throw new ArgumentException(
                $"Invalid 'compressionState' value: '{value}'. Expected one of: email, print, hqprint, screen, none."),
        };
    }

    /// <summary>
    /// CONSISTENCY(master-layout-shape-edit): resolve a master/layout parent path
    /// to its <see cref="ShapeTree"/> + owning part + root element. Accepts all
    /// three canonical forms:
    ///   /slidemaster[N]
    ///   /slidelayout[N]                      — top-level (flat) layout numbering
    ///   /slidemaster[N]/slidelayout[L]       — nested form
    /// Returns null when the path is not a master/layout parent — callers fall
    /// back to slide-scoped logic. Path matching is case-insensitive, matching
    /// the rest of the pptx handler.
    /// </summary>
    internal (ShapeTree shapeTree, OpenXmlPart part, OpenXmlPartRootElement root, string canonicalPrefix)?
        TryResolveMasterOrLayoutShapeParent(string parentPath)
    {
        var presentationPart = _doc.PresentationPart;
        if (presentationPart == null) return null;

        // Form 1: /slidemaster[N]/slidelayout[L]
        var nested = Regex.Match(parentPath,
            @"^/slidemaster\[(\d+)\]/slidelayout\[(\d+)\]$", RegexOptions.IgnoreCase);
        if (nested.Success)
        {
            var mIdx = int.Parse(nested.Groups[1].Value);
            var lIdx = int.Parse(nested.Groups[2].Value);
            var masters = presentationPart.SlideMasterParts.ToList();
            if (mIdx < 1 || mIdx > masters.Count)
                throw new ArgumentException($"Slide master {mIdx} not found (total: {masters.Count})");
            var layouts = masters[mIdx - 1].SlideLayoutParts.ToList();
            if (lIdx < 1 || lIdx > layouts.Count)
                throw new ArgumentException($"Slide layout {lIdx} not found under master {mIdx} (total: {layouts.Count})");
            var lp = layouts[lIdx - 1];
            var root = lp.SlideLayout
                ?? throw new InvalidOperationException("Corrupt slide layout");
            var tree = root.CommonSlideData?.ShapeTree
                ?? throw new InvalidOperationException("Slide layout has no shape tree");
            return (tree, lp, root, $"/slidemaster[{mIdx}]/slidelayout[{lIdx}]");
        }

        // Form 2: /slidemaster[N]
        var masterOnly = Regex.Match(parentPath,
            @"^/slidemaster\[(\d+)\]$", RegexOptions.IgnoreCase);
        if (masterOnly.Success)
        {
            var mIdx = int.Parse(masterOnly.Groups[1].Value);
            var masters = presentationPart.SlideMasterParts.ToList();
            if (mIdx < 1 || mIdx > masters.Count)
                throw new ArgumentException($"Slide master {mIdx} not found (total: {masters.Count})");
            var mp = masters[mIdx - 1];
            var root = mp.SlideMaster
                ?? throw new InvalidOperationException("Corrupt slide master");
            var tree = root.CommonSlideData?.ShapeTree
                ?? throw new InvalidOperationException("Slide master has no shape tree");
            return (tree, mp, root, $"/slidemaster[{mIdx}]");
        }

        // Form 3: /slidelayout[N] — flat top-level layout numbering
        var layoutOnly = Regex.Match(parentPath,
            @"^/slidelayout\[(\d+)\]$", RegexOptions.IgnoreCase);
        if (layoutOnly.Success)
        {
            var lIdx = int.Parse(layoutOnly.Groups[1].Value);
            var allLayouts = presentationPart.SlideMasterParts
                .SelectMany(m => m.SlideLayoutParts).ToList();
            if (lIdx < 1 || lIdx > allLayouts.Count)
                throw new ArgumentException($"Slide layout {lIdx} not found (total: {allLayouts.Count})");
            var lp = allLayouts[lIdx - 1];
            var root = lp.SlideLayout
                ?? throw new InvalidOperationException("Corrupt slide layout");
            var tree = root.CommonSlideData?.ShapeTree
                ?? throw new InvalidOperationException("Slide layout has no shape tree");
            return (tree, lp, root, $"/slidelayout[{lIdx}]");
        }

        return null;
    }

    /// <summary>
    /// Single source of truth for which `<a:fld type="…">` values
    /// PowerPoint renders dynamically — slidenum and datetime* are
    /// auto-populated when the slide opens. Used by `view text` sentinel
    /// emission (this file), `view issues` slide_field_not_evaluated
    /// (View.cs), and shape Format["evaluated"] (NodeBuilder.cs). Adding a
    /// new dynamic type here propagates everywhere.
    /// </summary>
    internal static bool IsDynamicSlideFieldTypeStatic(string fldType)
    {
        if (string.IsNullOrEmpty(fldType)) return false;
        if (fldType == "slidenum") return true;
        if (fldType.StartsWith("datetime", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    /// Schema order for DrawingML CT_TextCharacterProperties children (a:rPr / a:endParaRPr / a:defRPr).
    /// Source: Open-XML-SDK CompositeParticle definition of TextCharacterPropertiesType.
    /// Children must appear in this order or OpenXmlValidator emits schema warnings and
    /// PowerPoint silently drops the out-of-order ones.
    /// </summary>
    private static readonly (Type type, int order)[] DrawingRunPropChildOrder = new (Type, int)[]
    {
        (typeof(Drawing.Outline),              1),   // ln
        (typeof(Drawing.NoFill),               2),   // noFill
        (typeof(Drawing.SolidFill),            2),   // solidFill
        (typeof(Drawing.GradientFill),         2),   // gradFill
        (typeof(Drawing.BlipFill),             2),   // blipFill
        (typeof(Drawing.PatternFill),          2),   // pattFill
        (typeof(Drawing.GroupFill),            2),   // grpFill
        (typeof(Drawing.EffectList),           3),   // effectLst
        (typeof(Drawing.EffectDag),            3),   // effectDag
        (typeof(Drawing.Highlight),            4),   // highlight
        (typeof(Drawing.UnderlineFollowsText), 5),   // uLnTx
        (typeof(Drawing.Underline),            5),   // uLn
        (typeof(Drawing.UnderlineFillText),    6),   // uFillTx
        (typeof(Drawing.UnderlineFill),        6),   // uFill
        (typeof(Drawing.LatinFont),            7),   // latin
        (typeof(Drawing.EastAsianFont),        8),   // ea
        (typeof(Drawing.ComplexScriptFont),    9),   // cs
        (typeof(Drawing.SymbolFont),          10),   // sym
        (typeof(Drawing.HyperlinkOnClick),    11),   // hlinkClick
        (typeof(Drawing.HyperlinkOnMouseOver),12),   // hlinkMouseOver
        (typeof(Drawing.RightToLeft),         13),   // rtl
        (typeof(Drawing.ExtensionList),       14),   // extLst
    };
}
