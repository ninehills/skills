// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Presentation;
using Drawing = DocumentFormat.OpenXml.Drawing;
using OfficeCli.Core;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{
    /// <summary>
    /// Apply outer shadow effect to ShapeProperties.
    /// Format: "COLOR" or "COLOR-BLUR-ANGLE-DIST" or "COLOR-BLUR-ANGLE-DIST-OPACITY"
    ///   COLOR: hex (e.g. 000000)
    ///   BLUR: blur radius in points, default 4
    ///   ANGLE: direction in degrees, default 45
    ///   DIST: distance in points, default 3
    ///   OPACITY: 0-100 percent, default 40
    /// Examples: "000000", "000000-6-315-4-50", "none"
    /// </summary>
    private static void ApplyShadow(ShapeProperties spPr, string value)
    {
        var effectList = EnsureEffectList(spPr);
        effectList.RemoveAllChildren<Drawing.OuterShadow>();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            if (!effectList.HasChildren) spPr.RemoveChild(effectList);
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Shadow value cannot be empty. Use 'none' to remove shadow.");

        DrawingEffectsHelper.InsertEffectInSchemaOrder(effectList, BuildOuterShadow(value));
    }

    /// <summary>
    /// Apply glow effect to ShapeProperties.
    /// Format: "COLOR" or "COLOR-RADIUS" or "COLOR-RADIUS-OPACITY"
    ///   COLOR: hex (e.g. 0070FF)
    ///   RADIUS: glow radius in points, default 8
    ///   OPACITY: 0-100 percent, default 75
    /// Examples: "0070FF", "FF0000-10", "00B0F0-6-60", "none"
    /// </summary>
    private static void ApplyGlow(ShapeProperties spPr, string value)
    {
        var effectList = EnsureEffectList(spPr);
        effectList.RemoveAllChildren<Drawing.Glow>();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            if (!effectList.HasChildren) spPr.RemoveChild(effectList);
            return;
        }

        DrawingEffectsHelper.InsertEffectInSchemaOrder(effectList, BuildGlow(value));
    }

    /// <summary>
    /// Check if a shape has no fill (transparent background).
    /// </summary>
    private static bool IsNoFillShape(ShapeProperties spPr)
    {
        return spPr.GetFirstChild<Drawing.NoFill>() != null;
    }

    /// <summary>
    /// Build an OuterShadow element from the shadow value string.
    /// </summary>
    private static Drawing.OuterShadow BuildOuterShadow(string value)
        => OfficeCli.Core.DrawingEffectsHelper.BuildOuterShadow(value, BuildColorElement);

    /// <summary>
    /// Apply inner shadow effect to ShapeProperties. Format matches the outer
    /// shadow variant — "COLOR[-BLUR[-ANGLE[-DIST[-OPACITY]]]]" / "none".
    /// Lives alongside `shadow` (outer) as a distinct effectLst child so a
    /// shape may carry both without one overwriting the other.
    /// </summary>
    private static void ApplyInnerShadow(ShapeProperties spPr, string value)
    {
        var effectList = EnsureEffectList(spPr);
        effectList.RemoveAllChildren<Drawing.InnerShadow>();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            if (!effectList.HasChildren) spPr.RemoveChild(effectList);
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("innerShadow value cannot be empty. Use 'none' to remove inner shadow.");

        DrawingEffectsHelper.InsertEffectInSchemaOrder(effectList, BuildInnerShadow(value));
    }

    private static Drawing.InnerShadow BuildInnerShadow(string value)
        => OfficeCli.Core.DrawingEffectsHelper.BuildInnerShadow(value, BuildColorElement);

    private static Drawing.Glow BuildGlow(string value)
        => OfficeCli.Core.DrawingEffectsHelper.BuildGlow(value, BuildColorElement);

    /// <summary>
    /// Get or create EffectList in correct schema position within RunProperties.
    /// CT_TextCharacterProperties schema order: ln → fill → effectLst → highlight → uLnTx/uLn → uFillTx/uFill → latin → ea → cs → sym → hlinkClick → hlinkMouseOver → extLst
    /// </summary>
    private static void InsertFillInRunProperties(Drawing.RunProperties rPr, DocumentFormat.OpenXml.OpenXmlElement fillElement)
        => OfficeCli.Core.DrawingEffectsHelper.InsertFillInRunProperties(rPr, fillElement);

    private static void ApplyTextShadow(Drawing.Run run, string value)
        => OfficeCli.Core.DrawingEffectsHelper.ApplyTextEffect<Drawing.OuterShadow>(run, value, () => BuildOuterShadow(value));

    private static void ApplyTextGlow(Drawing.Run run, string value)
        => OfficeCli.Core.DrawingEffectsHelper.ApplyTextEffect<Drawing.Glow>(run, value, () => BuildGlow(value));

    /// <summary>
    /// Apply reflection effect to ShapeProperties.
    /// Format: "TYPE" where TYPE is one of:
    ///   tight / small  — tight reflection, touching (stA=52000 endA=300 endPos=55000)
    ///   half           — half reflection (stA=52000 endA=300 endPos=90000)
    ///   full           — full reflection (stA=52000 endA=300 endPos=100000)
    ///   true           — alias for half
    ///   none / false   — remove reflection
    /// </summary>
    private static bool TryParseReflectionEndPos(string value, out int endPos)
    {
        switch (value.ToLowerInvariant())
        {
            case "tight": case "small": endPos = 55000; return true;
            case "true":  case "half":  endPos = 90000; return true;
            case "full":               endPos = 100000; return true;
        }
        if (int.TryParse(value, out var pct) && pct >= 0 && pct <= 100)
        {
            endPos = (int)Math.Min((long)pct * 1000, 100000);
            return true;
        }
        endPos = 0;
        return false;
    }

    /// <summary>
    /// Match a captured <a:reflection> against the preset shape ApplyReflection
    /// emits (blurRad=6350 stA=52000 stPos=0 endA=300 endPos∈{55000,90000,100000}
    /// dist=0 dir=5400000 sy=-100000 algn=bl rotWithShape=0). When ANY attribute
    /// differs — including user-authored blurRad/stA/endA/dist tuning — we treat
    /// it as non-preset and surface the OuterXml so dump→replay can reinstall
    /// the source element verbatim instead of collapsing to the nearest preset.
    /// </summary>
    internal static bool IsPlainReflectionPreset(Drawing.Reflection r)
    {
        if (r.BlurRadius?.Value is not 6350) return false;
        if (r.StartOpacity?.Value is not 52000) return false;
        if (r.StartPosition?.HasValue == true && r.StartPosition.Value != 0) return false;
        if (r.EndAlpha?.Value is not 300) return false;
        var endPos = r.EndPosition?.Value ?? 0;
        if (endPos != 55000 && endPos != 90000 && endPos != 100000) return false;
        if (r.Distance?.HasValue == true && r.Distance.Value != 0) return false;
        if (r.Direction?.Value is not 5400000) return false;
        if (r.VerticalRatio?.Value is not -100000) return false;
        if (r.Alignment?.Value != Drawing.RectangleAlignmentValues.BottomLeft) return false;
        if (r.RotateWithShape?.HasValue == true && r.RotateWithShape.Value) return false;
        // Reject if any extra attribute (FadeDirection, HorizontalRatio,
        // HorizontalSkew, VerticalSkew, EndPosition tweaks not in the preset
        // set) is present. ApplyReflection never emits these, so their
        // presence implies user-authored tuning.
        if (r.FadeDirection?.HasValue == true) return false;
        if (r.HorizontalRatio?.HasValue == true) return false;
        if (r.HorizontalSkew?.HasValue == true) return false;
        if (r.VerticalSkew?.HasValue == true) return false;
        return true;
    }

    /// <summary>
    /// bt-2: Detect "plain" outerShdw — one whose attributes are only the
    /// ones BuildOuterShadow round-trips through the `shadow=` compressed
    /// form (BlurRadius / Direction / Distance, plus inferred alpha from
    /// the color child). When ANY of sx/sy (scale), kx/ky (skew), algn,
    /// rotWithShape is set to a non-default value, BuildOuterShadow's
    /// re-emit would drop them — surface OuterXml as shadowRaw instead.
    ///
    /// ApplyShadow emits Alignment=TopLeft, RotateWithShape=false — those
    /// are the compressed form's implicit defaults. We treat them as
    /// "compressible" only when present at exactly those values.
    ///
    /// R56 bt-2: also route to shadowRaw when the color child carries
    /// lumMod/lumOff (or shade/tint/satMod/hueMod) transforms. The composite
    /// `shadow=COLOR-BLUR-ANGLE-DIST-OPACITY` form embeds the color via the
    /// `+lumMod50+lumOff50` suffix vocabulary (AppendColorTransforms), so
    /// the emitted string becomes `accent1+lumMod50+lumOff50-4-45-3-100` —
    /// readable, but the trailing `-4-45-3-100` tuple after a transform
    /// chain is undocumented and not declared canonical in the schema.
    /// Surface shadowRaw so the source <a:schemeClr>/<a:lumMod>/<a:lumOff>
    /// round-trips verbatim, matching reflectionRaw/fillOverlayRaw.
    /// </summary>
    internal static bool IsPlainOuterShadow(Drawing.OuterShadow s)
    {
        if (s.HorizontalRatio?.HasValue == true) return false;
        if (s.VerticalRatio?.HasValue == true) return false;
        if (s.HorizontalSkew?.HasValue == true) return false;
        if (s.VerticalSkew?.HasValue == true) return false;
        if (s.Alignment?.HasValue == true
            && s.Alignment.Value != Drawing.RectangleAlignmentValues.TopLeft)
            return false;
        if (s.RotateWithShape?.HasValue == true && s.RotateWithShape.Value)
            return false;
        if (ColorChildHasNonAlphaTransform(s)) return false;
        return true;
    }

    /// <summary>
    /// R56 bt-2: detect lumMod/lumOff/shade/tint/satMod/satOff/hueMod/hueOff
    /// transform children under the shadow's color element. Alpha is
    /// excluded — the composite shadow= form already encodes alpha via the
    /// trailing OPACITY token, so an alpha-only color child stays
    /// compressible. Used to gate the shadowRaw fallback for outer and
    /// inner shadows.
    /// </summary>
    internal static bool ColorChildHasNonAlphaTransform(OpenXmlElement shadowEl)
    {
        var colorChild = (OpenXmlElement?)shadowEl.GetFirstChild<Drawing.RgbColorModelHex>()
            ?? shadowEl.GetFirstChild<Drawing.SchemeColor>();
        if (colorChild == null) return false;
        foreach (var t in colorChild.Elements())
        {
            switch (t.LocalName)
            {
                case "lumMod":
                case "lumOff":
                case "shade":
                case "tint":
                case "satMod":
                case "satOff":
                case "hueMod":
                case "hueOff":
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// R56 bt-2: mirror of IsPlainOuterShadow for inner shadows. CT_InnerShadow
    /// has BlurRadius/Distance/Direction only (no sx/sy/kx/ky/algn/rotWithShape
    /// to guard), so the only non-compressible signal is a color child carrying
    /// non-alpha color transforms — the same lumMod/lumOff/shade/tint case the
    /// outer-shadow path now routes to shadowRaw.
    /// </summary>
    internal static bool IsPlainInnerShadow(Drawing.InnerShadow s)
    {
        if (ColorChildHasNonAlphaTransform(s)) return false;
        return true;
    }

    /// <summary>
    /// R62 bt-5: lift the verbatim <a:fillOverlay blend=…>…</a:fillOverlay>
    /// passed as the fillOverlayRaw= value into a fresh Drawing.FillOverlay
    /// element. Extracted from the shape-level Set branch so the run-level
    /// branch (ApplyRunFillOverlayRaw) can share the parser — both paths must
    /// build the same OOXML element for the dump→replay round-trip to be
    /// byte-stable.
    /// </summary>
    internal static Drawing.FillOverlay BuildFillOverlayFromRaw(string value)
    {
        var raw = value.Contains("xmlns:a=")
            ? value
            : value.Replace("<a:fillOverlay",
                "<a:fillOverlay xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"");
        var overlay = new Drawing.FillOverlay();
        using var sr = new System.IO.StringReader(raw);
        using var xr = System.Xml.XmlReader.Create(sr);
        xr.MoveToContent();
        if (xr.HasAttributes)
        {
            while (xr.MoveToNextAttribute())
            {
                if (xr.Prefix == "xmlns" || xr.Name == "xmlns") continue;
                overlay.SetAttribute(new OpenXmlAttribute(
                    xr.Prefix, xr.LocalName, xr.NamespaceURI, xr.Value));
            }
            xr.MoveToElement();
        }
        if (!xr.IsEmptyElement)
        {
            var inner = xr.ReadInnerXml();
            if (!string.IsNullOrWhiteSpace(inner))
                overlay.InnerXml = inner;
        }
        return overlay;
    }

    /// <summary>
    /// R62 bt-5: run-level analogue of the shape-spPr fillOverlayRaw apply.
    /// Mirrors ApplyTextShadow / ApplyTextGlow shape — get-or-create the run's
    /// <a:rPr><a:effectLst>, drop any existing fillOverlay child, install the
    /// fresh one at schema-correct position. Empty / whitespace value removes
    /// the overlay (and the effectLst if it becomes empty), matching the
    /// shape-level "value cleared → strip element" contract.
    /// </summary>
    internal static void ApplyRunFillOverlayRaw(Drawing.Run run, string value)
    {
        var rPr = run.RunProperties ?? (run.RunProperties = new Drawing.RunProperties());
        var effectList = OfficeCli.Core.DrawingEffectsHelper.EnsureRunEffectList(rPr);
        effectList.RemoveAllChildren<Drawing.FillOverlay>();
        if (!string.IsNullOrWhiteSpace(value))
        {
            var overlay = BuildFillOverlayFromRaw(value);
            DrawingEffectsHelper.InsertEffectInSchemaOrder(effectList, overlay);
        }
        else if (!effectList.HasChildren)
        {
            rPr.RemoveChild(effectList);
        }
    }

    private static void ApplyReflection(ShapeProperties spPr, string value)
    {
        var effectList = EnsureEffectList(spPr);
        effectList.RemoveAllChildren<Drawing.Reflection>();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            if (!effectList.HasChildren) spPr.RemoveChild(effectList);
            return;
        }

        // endPos controls how much of the shape is reflected. Unknown preset
        // names (and out-of-range numerics) used to silently degrade to "half"
        // (90000); flag them with TryApplyReflection's bool return so the
        // caller can surface the value as unsupported_property instead.
        if (!TryParseReflectionEndPos(value, out var endPos))
            throw new ArgumentException(
                $"Invalid reflection '{value}'. Valid presets: none, tight, small, half, true, full; or a numeric percentage 0-100.");

        var reflection = new Drawing.Reflection
        {
            BlurRadius      = 6350,
            StartOpacity    = 52000,
            StartPosition   = 0,
            EndAlpha        = 300,
            EndPosition     = endPos,
            Distance        = 0,
            Direction       = 5400000,  // 90° — downward
            VerticalRatio   = -100000,  // flip vertically
            Alignment       = Drawing.RectangleAlignmentValues.BottomLeft,
            RotateWithShape = false
        };
        DrawingEffectsHelper.InsertEffectInSchemaOrder(effectList, reflection);
    }

    /// <summary>
    /// Apply soft edge effect to ShapeProperties.
    /// Value: radius in points (e.g. "5") or "none" to remove.
    /// </summary>
    private static void ApplySoftEdge(ShapeProperties spPr, string value)
    {
        var effectList = EnsureEffectList(spPr);
        effectList.RemoveAllChildren<Drawing.SoftEdge>();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            if (!effectList.HasChildren) spPr.RemoveChild(effectList);
            return;
        }

        var numStr = value.EndsWith("pt", StringComparison.OrdinalIgnoreCase) ? value[..^2].Trim() : value;
        if (!double.TryParse(numStr, System.Globalization.CultureInfo.InvariantCulture, out var radiusPt) || double.IsNaN(radiusPt) || double.IsInfinity(radiusPt) || radiusPt < 0)
            throw new ArgumentException($"Invalid 'softedge' value '{value}'. Expected a finite non-negative numeric radius in points.");
        DrawingEffectsHelper.InsertEffectInSchemaOrder(effectList, new Drawing.SoftEdge { Radius = (long)(radiusPt * EmuConverter.EmuPerPoint) });
    }

    /// <summary>
    /// Apply blur effect to ShapeProperties.
    /// Value: radius in points (e.g. "4" or "4pt") with optional grow
    /// boolean (`4pt:true` / `4pt:false`), or "none" to remove. Converts
    /// pt → EMU (1pt = 12700 EMU). Grow defaults to true (OOXML default).
    /// </summary>
    private static void ApplyBlur(ShapeProperties spPr, string value)
    {
        var effectList = EnsureEffectList(spPr);
        effectList.RemoveAllChildren<Drawing.Blur>();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            if (!effectList.HasChildren) spPr.RemoveChild(effectList);
            return;
        }

        // R58 bt-1: NodeBuilder emits `blur=<rad>pt:<grow>` so a source-
        // authored `grow="0"` survives the round-trip. Parse the optional
        // `:bool` tail; legacy bare-radius form (`4pt`) still works with
        // grow defaulting to true per the OOXML schema.
        var radPart = value;
        var grow = true;
        var colonIdx = value.IndexOf(':');
        if (colonIdx >= 0)
        {
            radPart = value[..colonIdx].Trim();
            var growPart = value[(colonIdx + 1)..].Trim();
            grow = IsTruthy(growPart);
        }

        var numStr = radPart.EndsWith("pt", StringComparison.OrdinalIgnoreCase) ? radPart[..^2].Trim() : radPart;
        if (!double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var radiusPt)
            || double.IsNaN(radiusPt) || double.IsInfinity(radiusPt) || radiusPt < 0)
            throw new ArgumentException($"Invalid 'blur' value '{value}'. Expected a finite non-negative numeric radius in points (optionally `<rad>pt:<grow>`).");

        DrawingEffectsHelper.InsertEffectInSchemaOrder(effectList, new Drawing.Blur { Radius = (long)(radiusPt * EmuConverter.EmuPerPoint), Grow = grow });
    }

    private static void ApplyTextReflection(Drawing.Run run, string value)
        => OfficeCli.Core.DrawingEffectsHelper.ApplyTextEffect<Drawing.Reflection>(run, value,
            () => OfficeCli.Core.DrawingEffectsHelper.BuildReflection(value));

    private static void ApplyTextSoftEdge(Drawing.Run run, string value)
        => OfficeCli.Core.DrawingEffectsHelper.ApplyTextEffect<Drawing.SoftEdge>(run, value,
            () => OfficeCli.Core.DrawingEffectsHelper.BuildSoftEdge(value));

    /// <summary>
    /// Apply 3D rotation (scene3d) to ShapeProperties.
    /// Format: "rotX,rotY,rotZ" in degrees (e.g. "45,30,0")
    /// </summary>
    private static void Apply3DRotation(ShapeProperties spPr, string value)
    {
        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            var existing = spPr.GetFirstChild<Drawing.Scene3DType>();
            if (existing != null) spPr.RemoveChild(existing);
            return;
        }

        var parts = value.Split(',');
        if (parts.Length < 3)
            throw new ArgumentException($"Invalid '3drotation' value: '{value}'. Expected 3 components as 'rotX,rotY,rotZ' (e.g. '45,30,0').");
        if (!double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rotX) || double.IsNaN(rotX) || double.IsInfinity(rotX))
            throw new ArgumentException($"Invalid '3drotation' value: '{value}'. Expected finite degrees as 'rotX,rotY,rotZ' (e.g. '45,30,0').");
        if (!double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ry) || double.IsNaN(ry) || double.IsInfinity(ry))
            throw new ArgumentException($"Invalid '3drotation' rotY value: '{parts[1].Trim()}'. Expected a finite number.");
        if (!double.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rz) || double.IsNaN(rz) || double.IsInfinity(rz))
            throw new ArgumentException($"Invalid '3drotation' rotZ value: '{parts[2].Trim()}'. Expected a finite number.");
        var rotY = ry;
        var rotZ = rz;

        var scene3d = EnsureScene3D(spPr);
        var camera = scene3d.Camera!;
        camera.Rotation = new Drawing.Rotation
        {
            Latitude = NormalizeDegrees60k(rotX),
            Longitude = NormalizeDegrees60k(rotY),
            Revolution = NormalizeDegrees60k(rotZ)
        };
    }

    /// <summary>
    /// Normalize degrees to OOXML 60000ths-of-a-degree range [0, 21600000).
    /// Accepts negative values (e.g. -20° → 340° → 20400000).
    /// </summary>
    private static int NormalizeDegrees60k(double degrees)
    {
        var val = (int)(degrees * 60000);
        const int full = 360 * 60000; // 21600000
        val %= full;
        if (val < 0) val += full;
        return val;
    }

    /// <summary>
    /// Apply a single 3D rotation axis.
    /// </summary>
    private static void Apply3DRotationAxis(ShapeProperties spPr, string axis, string value)
    {
        var scene3d = EnsureScene3D(spPr);
        var camera = scene3d.Camera!;
        // CT_SphereCoords requires lat / lon / rev attributes — schema rejects
        // a:rot when any one is missing. Pre-fill all three to 0 so setting
        // only z-rotation (the common case) doesn't leave the other two
        // attributes off the element.
        var rot = camera.Rotation ?? (camera.Rotation = new Drawing.Rotation
        {
            Latitude = 0,
            Longitude = 0,
            Revolution = 0,
        });
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var degVal) || double.IsNaN(degVal) || double.IsInfinity(degVal))
            throw new ArgumentException($"Invalid '3drotation.{axis}' value: '{value}'. Expected a finite number in degrees.");
        var deg = NormalizeDegrees60k(degVal);

        switch (axis)
        {
            case "x": rot.Latitude = deg; break;
            case "y": rot.Longitude = deg; break;
            case "z": rot.Revolution = deg; break;
        }
    }

    /// <summary>
    /// Apply bevel to ShapeProperties (top or bottom).
    /// Format: "preset" or "preset-width-height" (width/height in points)
    /// Presets: circle, relaxedInset, cross, coolSlant, angle, softRound, convex,
    ///          slope, divot, riblet, hardEdge, artDeco
    /// Examples: "circle", "circle-6-6", "none"
    /// </summary>
    private static void ApplyBevel(ShapeProperties spPr, string value, bool top)
    {
        var sp3d = spPr.GetFirstChild<Drawing.Shape3DType>();

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            if (sp3d != null)
            {
                if (top) { sp3d.BevelTop = null; }
                else { sp3d.BevelBottom = null; }
                if (sp3d.BevelTop == null && sp3d.BevelBottom == null &&
                    (sp3d.ExtrusionHeight == null || sp3d.ExtrusionHeight.Value == 0))
                    spPr.RemoveChild(sp3d);
            }
            return;
        }

        sp3d ??= EnsureShape3D(spPr);
        // Normalize alternative separator: "preset;width;height" → "preset-width-height"
        value = value.Replace(';', '-');
        var bevelParts = value.Split('-');
        var preset = ParseBevelPreset(bevelParts[0].Trim());
        long w = 76200L, h;
        if (bevelParts.Length > 1)
        {
            if (!double.TryParse(bevelParts[1].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var wPt) || double.IsNaN(wPt) || double.IsInfinity(wPt))
                throw new ArgumentException($"Invalid bevel width: '{bevelParts[1]}'. Expected a finite number in points. Format: PRESET[-WIDTH[-HEIGHT]]");
            w = (long)(wPt * EmuConverter.EmuPerPoint);
        }
        if (bevelParts.Length > 2)
        {
            if (!double.TryParse(bevelParts[2].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var hPt) || double.IsNaN(hPt) || double.IsInfinity(hPt))
                throw new ArgumentException($"Invalid bevel height: '{bevelParts[2]}'. Expected a finite number in points. Format: PRESET[-WIDTH[-HEIGHT]]");
            h = (long)(hPt * EmuConverter.EmuPerPoint);
        }
        else h = w;

        if (top)
        {
            sp3d.BevelTop = new Drawing.BevelTop { Width = w, Height = h, Preset = preset };
        }
        else
        {
            sp3d.BevelBottom = new Drawing.BevelBottom { Width = w, Height = h, Preset = preset };
        }
    }

    /// <summary>
    /// Apply 3D extrusion depth in points.
    /// </summary>
    private static void Apply3DDepth(ShapeProperties spPr, string value)
    {
        if (value.Equals("none", StringComparison.OrdinalIgnoreCase) || value == "0")
        {
            var sp3d = spPr.GetFirstChild<Drawing.Shape3DType>();
            if (sp3d != null) { sp3d.ExtrusionHeight = 0; }
            return;
        }

        var sp3dEl = EnsureShape3D(spPr);
        // Canonical length input contract (CLAUDE.md): bare number = points,
        // and pt/cm/in/px/emu suffixes are all accepted. Mirror lineWidth's
        // bare-int-as-points behaviour via EmuConverter.ParseLineWidth, which
        // returns EMU.
        long depthEmu;
        try
        {
            depthEmu = OfficeCli.Core.EmuConverter.ParseLineWidth(value);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(
                $"Invalid 'depth' value '{value}'. Expected a finite numeric depth in points (e.g. '10', '10pt', '0.5cm'). {ex.Message}");
        }
        sp3dEl.ExtrusionHeight = depthEmu;
    }

    /// <summary>
    /// Apply 3D material preset.
    /// </summary>
    private static void Apply3DMaterial(ShapeProperties spPr, string value)
    {
        var sp3d = EnsureShape3D(spPr);
        sp3d.PresetMaterial = ParseMaterial(value);
    }

    /// <summary>
    /// Apply light rig preset to scene3d.
    /// </summary>
    private static void ApplyLightRig(ShapeProperties spPr, string value)
    {
        var scene3d = EnsureScene3D(spPr);
        scene3d.LightRig!.Rig = ParseLightRig(value);
    }

    /// <summary>
    /// Apply lightRig @dir (t/tl/tr/l/r/b/bl/br) to scene3d's lightRig.
    /// R55 bt-4: NodeBuilder surfaces this as Format["lightingDir"]; without
    /// a Set hook the source direction was dropped on every dump-replay.
    /// </summary>
    private static void ApplyLightRigDirection(ShapeProperties spPr, string value)
    {
        var scene3d = EnsureScene3D(spPr);
        scene3d.LightRig!.Direction = new Drawing.LightRigDirectionValues(value);
    }

    /// <summary>
    /// Apply &lt;a:rot lat="..." lon="..." rev="..."/&gt; under &lt;a:lightRig&gt;.
    /// Input form mirrors the NodeBuilder Get key: "lat:lon:rev" (60000ths of
    /// a degree, the raw OOXML unit). R55 bt-4: this child was previously
    /// unrepresented in the Set vocabulary, so dump captured lightingRot but
    /// replay rebuilt the lightRig with no rot child.
    /// </summary>
    private static void ApplyLightRigRotation(ShapeProperties spPr, string value)
    {
        var scene3d = EnsureScene3D(spPr);
        var parts = value.Split(':');
        if (parts.Length != 3
            || !int.TryParse(parts[0], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var lat)
            || !int.TryParse(parts[1], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var lon)
            || !int.TryParse(parts[2], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var rev))
        {
            throw new ArgumentException(
                $"Invalid lightingRot: '{value}'. Expected 'lat:lon:rev' in 60000ths of a degree (e.g. '0:0:1200000').");
        }
        var rot = new Drawing.Rotation { Latitude = lat, Longitude = lon, Revolution = rev };
        scene3d.LightRig!.RemoveAllChildren<Drawing.Rotation>();
        scene3d.LightRig.AppendChild(rot);
    }

    // --- Helper methods ---

    /// <summary>
    /// Get or create EffectList in correct schema position.
    /// Schema order: fill → ln → effectLst → scene3d → sp3d → extLst
    /// </summary>
    private static Drawing.EffectList EnsureEffectList(ShapeProperties spPr)
    {
        var effectList = spPr.GetFirstChild<Drawing.EffectList>();
        if (effectList != null) return effectList;

        effectList = new Drawing.EffectList();
        // Insert before scene3d/sp3d/extLst if they exist
        var insertBefore = (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.Scene3DType>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.Shape3DType>()
            ?? spPr.GetFirstChild<Drawing.ShapePropertiesExtensionList>();
        if (insertBefore != null)
            spPr.InsertBefore(effectList, insertBefore);
        else
            spPr.AppendChild(effectList);
        return effectList;
    }

    /// <summary>
    /// Get or create PresetGeometry in correct CT_ShapeProperties schema
    /// position (rank 2, the geometry choice slot). Must precede fill / ln /
    /// effectLst / scene3d / sp3d / extLst. Symmetric to EnsureOutline /
    /// EnsureEffectList — converts a raw AppendChild idiom that produced
    /// schema-invalid order whenever those higher-rank elements existed first.
    /// </summary>
    private static Drawing.PresetGeometry EnsurePresetGeometry(ShapeProperties spPr)
    {
        var prstGeom = spPr.GetFirstChild<Drawing.PresetGeometry>();
        if (prstGeom != null) return prstGeom;

        prstGeom = new Drawing.PresetGeometry();
        // First higher-rank sibling — rank 3 (fill choice) onward.
        var insertBefore = (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.NoFill>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.SolidFill>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.GradientFill>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.BlipFill>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.PatternFill>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.GroupFill>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.Outline>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.EffectList>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.Scene3DType>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.Shape3DType>()
            ?? spPr.GetFirstChild<Drawing.ShapePropertiesExtensionList>();
        if (insertBefore != null)
            spPr.InsertBefore(prstGeom, insertBefore);
        else
            spPr.AppendChild(prstGeom);
        return prstGeom;
    }

    /// <summary>
    /// Get or create Outline in correct schema position.
    /// Schema order: fill → ln → effectLst → scene3d → sp3d → extLst
    /// </summary>
    private static Drawing.Outline EnsureOutline(ShapeProperties spPr)
    {
        var outline = spPr.GetFirstChild<Drawing.Outline>();
        if (outline != null) return outline;

        outline = new Drawing.Outline();
        // Insert before effectLst/scene3d/sp3d/extLst if they exist
        var insertBefore = (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.EffectList>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.Scene3DType>()
            ?? (DocumentFormat.OpenXml.OpenXmlElement?)spPr.GetFirstChild<Drawing.Shape3DType>()
            ?? spPr.GetFirstChild<Drawing.ShapePropertiesExtensionList>();
        if (insertBefore != null)
            spPr.InsertBefore(outline, insertBefore);
        else
            spPr.AppendChild(outline);
        return outline;
    }

    /// <summary>
    /// Ensure an outline has a fill child element. PowerPoint silently
    /// drops a bare <a:ln w="N"/> with no fill descendant (no SolidFill /
    /// NoFill / GradientFill / PatternFill), rendering the shape borderless
    /// despite the width attribute. Called by callers that set width without
    /// also setting an explicit line color, so the user's intent ("draw a
    /// 2pt border") survives to a visible stroke.
    /// </summary>
    private static void EnsureOutlineHasFill(Drawing.Outline outline)
    {
        if (outline.GetFirstChild<Drawing.SolidFill>() != null
            || outline.GetFirstChild<Drawing.NoFill>() != null
            || outline.GetFirstChild<Drawing.GradientFill>() != null
            || outline.GetFirstChild<Drawing.PatternFill>() != null)
            return;
        // Default to black — matches PowerPoint UI behavior when a user
        // sets "Weight" but not "Color" on Format Shape > Line.
        var solid = new Drawing.SolidFill(new Drawing.RgbColorModelHex { Val = "000000" });
        // SolidFill must appear before PresetDash/CustomDash/LineJoin/etc.
        // per CT_LineProperties schema order. Insert at the head of outline.
        outline.InsertAt(solid, 0);
    }

    /// <summary>
    /// Set the extrusion color (<a:extrusionClr>) or contour color
    /// (<a:contourClr>) on the shape's sp3d child. Accepts the same
    /// color forms the rest of the handler accepts (hex with or without
    /// '#', named scheme colors, etc.). "none" removes the element.
    /// </summary>
    private static void ApplySp3DColor(ShapeProperties spPr, string value, bool isExtrusion)
    {
        var sp3d = spPr.GetFirstChild<Drawing.Shape3DType>();
        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            if (sp3d == null) return;
            if (isExtrusion) sp3d.ExtrusionColor = null;
            else sp3d.ContourColor = null;
            return;
        }
        sp3d ??= EnsureShape3D(spPr);
        var colorEl = DrawingColorBuilder.BuildColorElement(value);
        if (isExtrusion)
        {
            sp3d.ExtrusionColor = new Drawing.ExtrusionColor(colorEl);
        }
        else
        {
            sp3d.ContourColor = new Drawing.ContourColor(colorEl);
        }
    }

    /// <summary>
    /// Set the camera preset on a scene3d/camera (a:scene3d/a:camera/@prst).
    /// Value is the OOXML inner-text name (orthographicFront,
    /// perspectiveContrastingRightFacing, isometricTopUp, …). The
    /// PresetCameraValues string ctor validates against the schema enum
    /// and throws on invalid input, so we don't pre-validate here.
    /// </summary>
    private static void ApplyCameraPreset(ShapeProperties spPr, string value)
    {
        var scene3d = EnsureScene3D(spPr);
        var camera = scene3d.Camera!;
        camera.Preset = new Drawing.PresetCameraValues(value);
    }

    private static Drawing.Scene3DType EnsureScene3D(ShapeProperties spPr)
    {
        var scene3d = spPr.GetFirstChild<Drawing.Scene3DType>();
        if (scene3d != null) return scene3d;

        scene3d = new Drawing.Scene3DType(
            new Drawing.Camera { Preset = Drawing.PresetCameraValues.OrthographicFront },
            new Drawing.LightRig { Rig = Drawing.LightRigValues.ThreePoints, Direction = Drawing.LightRigDirectionValues.Top }
        );
        // Schema order: effectLst → scene3d → sp3d → extLst
        // Insert before sp3d if it exists, otherwise append
        var sp3d = spPr.GetFirstChild<Drawing.Shape3DType>();
        if (sp3d != null)
            spPr.InsertBefore(scene3d, sp3d);
        else
            spPr.AppendChild(scene3d);
        return scene3d;
    }

    private static Drawing.Shape3DType EnsureShape3D(ShapeProperties spPr)
    {
        var sp3d = spPr.GetFirstChild<Drawing.Shape3DType>();
        if (sp3d != null) return sp3d;

        // CONSISTENCY(dump-replay-no-auto-scene3d): NO auto-inject of a
        // sibling <a:scene3d>. PowerPoint authors can (and do) ship shapes
        // with only <a:sp3d> in the spPr — NodeBuilder surfaces those as
        // bevel=/depth=/material=/extrusionColor=/contourColor= Format keys
        // WITHOUT a camera= or lighting= key (because the source had no
        // scene3d). Auto-injecting scene3d here re-synthesizes a
        // <a:scene3d><a:camera prst="orthographicFront"/><a:lightRig
        // rig="threePt" dir="t"/></a:scene3d> on round-trip — drift against
        // the source. Users who want their bevel/depth visibly rendered must
        // pass camera= and/or lighting= explicitly; that path threads through
        // EnsureScene3D via ApplyCameraPreset / ApplyLighting.
        sp3d = new Drawing.Shape3DType();
        // Schema order: scene3d → sp3d → extLst
        // Insert before extLst if it exists, otherwise append
        var extLst = spPr.GetFirstChild<Drawing.ShapePropertiesExtensionList>();
        if (extLst != null)
            spPr.InsertBefore(sp3d, extLst);
        else
            spPr.AppendChild(sp3d);
        return sp3d;
    }

    private static Drawing.BevelPresetValues ParseBevelPreset(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "circle" => Drawing.BevelPresetValues.Circle,
            "relaxedinset" => Drawing.BevelPresetValues.RelaxedInset,
            "cross" => Drawing.BevelPresetValues.Cross,
            "coolslant" => Drawing.BevelPresetValues.CoolSlant,
            "angle" => Drawing.BevelPresetValues.Angle,
            "softround" => Drawing.BevelPresetValues.SoftRound,
            "convex" => Drawing.BevelPresetValues.Convex,
            "slope" => Drawing.BevelPresetValues.Slope,
            "divot" => Drawing.BevelPresetValues.Divot,
            "riblet" => Drawing.BevelPresetValues.Riblet,
            "hardedge" => Drawing.BevelPresetValues.HardEdge,
            "artdeco" => Drawing.BevelPresetValues.ArtDeco,
            _ => throw new ArgumentException($"Invalid bevel preset: '{value}'. Valid values: circle, relaxedinset, cross, coolslant, angle, softround, convex, slope, divot, riblet, hardedge, artdeco.")
        };
    }

    private static T WarnAndDefault<T>(string value, T defaultVal, string paramName, string validValues)
    {
        Console.Error.WriteLine($"Warning: unrecognized {paramName} '{value}', using default. Valid values: {validValues}");
        return defaultVal;
    }

    private static Drawing.PresetMaterialTypeValues ParseMaterial(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "warmmatte" => Drawing.PresetMaterialTypeValues.WarmMatte,
            "plastic" => Drawing.PresetMaterialTypeValues.Plastic,
            "metal" => Drawing.PresetMaterialTypeValues.Metal,
            "dkedge" or "darkedge" => Drawing.PresetMaterialTypeValues.DarkEdge,
            "softedge" => Drawing.PresetMaterialTypeValues.SoftEdge,
            "flat" => Drawing.PresetMaterialTypeValues.Flat,
            "wire" or "wireframe" or "legacywireframe" => Drawing.PresetMaterialTypeValues.LegacyWireframe,
            "powder" => Drawing.PresetMaterialTypeValues.Powder,
            "translucentpowder" => Drawing.PresetMaterialTypeValues.TranslucentPowder,
            "clear" => Drawing.PresetMaterialTypeValues.Clear,
            "softmetal" => Drawing.PresetMaterialTypeValues.SoftMetal,
            "matte" => Drawing.PresetMaterialTypeValues.Matte,
            // Legacy 3D presets (ST_PresetMaterialType, the legacy* tokens). Get
            // emits sp3d.PresetMaterial.InnerText verbatim, so these raw OOXML
            // tokens must round-trip back through Set/Add.
            "legacymatte" => Drawing.PresetMaterialTypeValues.LegacyMatte,
            "legacyplastic" => Drawing.PresetMaterialTypeValues.LegacyPlastic,
            "legacymetal" => Drawing.PresetMaterialTypeValues.LegacyMetal,
            _ => throw new ArgumentException($"Invalid material value: '{value}'. Valid values: warmmatte, plastic, metal, darkedge, flat, wire, powder, translucentpowder, clear, softmetal, matte, legacymatte, legacyplastic, legacymetal, legacywireframe.")
        };
    }

    private static Drawing.LightRigValues ParseLightRig(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "threept" or "3pt" => Drawing.LightRigValues.ThreePoints,
            "balanced" => Drawing.LightRigValues.Balanced,
            "soft" => Drawing.LightRigValues.Soft,
            "harsh" => Drawing.LightRigValues.Harsh,
            "flood" => Drawing.LightRigValues.Flood,
            "contrasting" => Drawing.LightRigValues.Contrasting,
            "morning" => Drawing.LightRigValues.Morning,
            "sunrise" => Drawing.LightRigValues.Sunrise,
            "sunset" => Drawing.LightRigValues.Sunset,
            "chilly" => Drawing.LightRigValues.Chilly,
            "freezing" => Drawing.LightRigValues.Freezing,
            "flat" => Drawing.LightRigValues.Flat,
            "twopt" or "2pt" => Drawing.LightRigValues.TwoPoints,
            "glow" => Drawing.LightRigValues.Glow,
            "brightroom" => Drawing.LightRigValues.BrightRoom,
            // Legacy 3D light rigs (ST_LightRigType, the legacy* tokens). Get
            // emits lightRig.Light.InnerText verbatim, so these raw OOXML tokens
            // must round-trip back through Set/Add.
            "legacyflat1" => Drawing.LightRigValues.LegacyFlat1,
            "legacyflat2" => Drawing.LightRigValues.LegacyFlat2,
            "legacyflat3" => Drawing.LightRigValues.LegacyFlat3,
            "legacyflat4" => Drawing.LightRigValues.LegacyFlat4,
            "legacynormal1" => Drawing.LightRigValues.LegacyNormal1,
            "legacynormal2" => Drawing.LightRigValues.LegacyNormal2,
            "legacynormal3" => Drawing.LightRigValues.LegacyNormal3,
            "legacynormal4" => Drawing.LightRigValues.LegacyNormal4,
            "legacyharsh1" => Drawing.LightRigValues.LegacyHarsh1,
            "legacyharsh2" => Drawing.LightRigValues.LegacyHarsh2,
            "legacyharsh3" => Drawing.LightRigValues.LegacyHarsh3,
            "legacyharsh4" => Drawing.LightRigValues.LegacyHarsh4,
            _ => throw new ArgumentException($"Invalid lighting value: '{value}'. Valid values: threept, balanced, soft, harsh, flood, contrasting, morning, sunrise, sunset, chilly, freezing, flat, twopt, glow, brightroom, legacyflat1-4, legacynormal1-4, legacyharsh1-4.")
        };
    }

    /// <summary>
    /// Format a bevel element as "preset-width-height" string for reading back.
    /// </summary>
    internal static string FormatBevel(Drawing.BevelType bevel)
    {
        // OOXML default for both w and h is 76200 EMU = 6 pt. When the stored
        // values match those defaults, emit the preset alone so round-trips of
        // unsized bevel input (e.g. "circle") don't gain a "-6-6" tail.
        // CONSISTENCY(bevel-symmetric): when w==h (single-size shorthand was used),
        // emit "preset-N" rather than "preset-N-N" so the readback mirrors the input
        // form and stays round-trippable (parser sets h=w when height omitted).
        var preset = bevel.Preset?.HasValue == true ? (bevel.Preset.InnerText ?? "circle") : "circle";
        var hasW = bevel.Width?.HasValue == true;
        var hasH = bevel.Height?.HasValue == true;
        var wEmu = hasW ? bevel.Width!.Value : 76200L;
        var hEmu = hasH ? bevel.Height!.Value : 76200L;
        if (wEmu == 76200L && hEmu == 76200L) return preset;
        var w = $"{wEmu / EmuConverter.EmuPerPointF:0.##}";
        var h = $"{hEmu / EmuConverter.EmuPerPointF:0.##}";
        // Emit single value when symmetric — "circle-4" not "circle-4-4".
        return wEmu == hEmu ? $"{preset}-{w}" : $"{preset}-{w}-{h}";
    }
}
