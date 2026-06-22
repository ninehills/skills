// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using OfficeCli.Core;
using Drawing = DocumentFormat.OpenXml.Drawing;

namespace OfficeCli.Handlers;

public partial class PowerPointHandler
{
    // ==================== CSS Helper: Fill ====================

    private static string GetShapeFillCss(ShapeProperties? spPr, OpenXmlPart part, Dictionary<string, string> themeColors)
    {
        if (spPr == null) return "";

        // NoFill
        if (spPr.GetFirstChild<Drawing.NoFill>() != null)
            return "background:transparent";

        // Solid fill
        var solidFill = spPr.GetFirstChild<Drawing.SolidFill>();
        if (solidFill != null)
        {
            var color = ResolveFillColor(solidFill, themeColors);
            if (color != null) return $"background:{color}";
        }

        // Gradient fill
        var gradFill = spPr.GetFirstChild<Drawing.GradientFill>();
        if (gradFill != null)
            return $"background:{GradientToCss(gradFill, themeColors)}";

        // Image fill (blip)
        var blipFill = spPr.GetFirstChild<Drawing.BlipFill>();
        if (blipFill != null)
        {
            var dataUri = BlipToDataUri(blipFill, part);
            if (dataUri != null)
            {
                // R4-4: honor <a:tile> — repeat at native size rather than cover.
                if (blipFill.GetFirstChild<Drawing.Tile>() != null)
                    return $"background:url('{dataUri}') repeat;background-size:auto";
                // R9-5: honor <a:srcRect> crop insets (l/t/r/b, units of 1/1000%).
                // Emulate the crop by zooming the background past the box and
                // shifting its origin so the visible region maps to the kept rect.
                var srcRect = blipFill.GetFirstChild<Drawing.SourceRectangle>();
                if (srcRect != null)
                {
                    double l = (srcRect.Left?.Value ?? 0) / 1000.0;
                    double t = (srcRect.Top?.Value ?? 0) / 1000.0;
                    double r = (srcRect.Right?.Value ?? 0) / 1000.0;
                    double b = (srcRect.Bottom?.Value ?? 0) / 1000.0;
                    double keptW = 100.0 - l - r;
                    double keptH = 100.0 - t - b;
                    if (keptW > 0 && keptH > 0 && (l != 0 || t != 0 || r != 0 || b != 0))
                    {
                        // Scale so the kept fraction fills 100% of the box; offset
                        // so the cropped-away top/left is pushed out of view.
                        var sizeW = (100.0 / keptW * 100.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                        var sizeH = (100.0 / keptH * 100.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                        var posX = (keptW <= 0 ? 0 : l / (l + r == 0 ? 1 : (l + r)) * 100.0);
                        var posY = (keptH <= 0 ? 0 : t / (t + b == 0 ? 1 : (t + b)) * 100.0);
                        var px = posX.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                        var py = posY.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                        return $"background:transparent url('{dataUri}') {px}% {py}%/{sizeW}% {sizeH}% no-repeat";
                    }
                }
                return $"background:transparent url('{dataUri}') center/cover no-repeat";
            }
        }

        // Pattern fill (a:pattFill) — approximate the preset pattern with a CSS
        // repeating-linear-gradient using the fg color over the bg color. Native
        // Office tiles the real preset bitmap; the gradient gives a recognisable
        // striped/cross texture and, crucially, surfaces the fg color so the
        // shape no longer renders as plain white. Mirrors the colorScale/dxf
        // approach of "approximate in CSS, don't leave it blank".
        var pattFill = spPr.GetFirstChild<Drawing.PatternFill>();
        if (pattFill != null)
            return PatternFillToCss(pattFill, themeColors);

        return "";
    }

    /// <summary>
    /// Convert an a:pattFill to a CSS repeating-linear-gradient approximation.
    /// fg = the pattern's foreground color (the lines), bg = background color.
    /// The preset name picks the gradient angle (diagonal / horizontal /
    /// vertical / grid); unrecognised presets fall back to a diagonal stripe.
    /// </summary>
    private static string PatternFillToCss(Drawing.PatternFill pattFill, Dictionary<string, string> themeColors)
    {
        var fg = ResolveWrappedColor(pattFill.GetFirstChild<Drawing.ForegroundColor>(), themeColors) ?? "#000000";
        var bg = ResolveWrappedColor(pattFill.GetFirstChild<Drawing.BackgroundColor>(), themeColors) ?? "#FFFFFF";
        var preset = pattFill.Preset?.HasValue == true ? pattFill.Preset.InnerText : "diagStripe";

        // Map preset family → gradient angle(s). Diagonal patterns use 45deg,
        // horizontal use 0deg, vertical 90deg, grid/cross layer both.
        var p = (preset ?? "").ToLowerInvariant();
        bool isGrid = p.Contains("grid") || p.Contains("cross") || p.Contains("checker") || p.Contains("weave");
        bool isHorz = p.Contains("horz");
        bool isVert = p.Contains("vert");
        var angle = isHorz ? "0deg" : isVert ? "90deg" : "45deg";

        // 4px band: 2px fg line over 2px bg gap.
        var stripe = $"repeating-linear-gradient({angle},{fg} 0,{fg} 2px,{bg} 2px,{bg} 4px)";
        if (isGrid)
        {
            // Layer a perpendicular stripe to form a grid; comma-separated
            // backgrounds stack (first on top).
            var cross = $"repeating-linear-gradient({(isHorz ? "90deg" : "135deg")},{fg} 0,{fg} 2px,transparent 2px,transparent 4px)";
            return $"background:{cross},{stripe}";
        }
        return $"background:{stripe}";
    }

    /// <summary>
    /// Resolve a color from a wrapper element (a:fgClr / a:bgClr) that contains
    /// a srgbClr or schemeClr child — same resolution as a SolidFill body.
    /// </summary>
    private static string? ResolveWrappedColor(OpenXmlCompositeElement? wrapper, Dictionary<string, string> themeColors)
    {
        if (wrapper == null) return null;

        var rgb = wrapper.GetFirstChild<Drawing.RgbColorModelHex>()?.Val?.Value;
        if (rgb != null && rgb.Length >= 6 && rgb[..6].All(char.IsAsciiHexDigit))
            return $"#{rgb[..6]}";

        var schemeColor = wrapper.GetFirstChild<Drawing.SchemeColor>();
        if (schemeColor?.Val?.HasValue == true)
        {
            var schemeName = schemeColor.Val!.InnerText;
            if (schemeName != null && themeColors.TryGetValue(schemeName, out var themeHex))
                return ApplyColorTransforms(themeHex, schemeColor);
        }
        return null;
    }

    // ==================== CSS Helper: Custom Geometry ====================

    /// <summary>
    /// Convert OOXML CustomGeometry (a:custGeom) path data to CSS clip-path.
    /// Supports moveTo, lineTo, cubicBezTo, quadBezTo, close.
    /// Coordinates are in the path's own coordinate system (w/h),
    /// converted to percentages for clip-path.
    /// </summary>
    private static string CustomGeometryToClipPath(Drawing.CustomGeometry custGeom)
    {
        var pathList = custGeom.GetFirstChild<Drawing.PathList>();
        if (pathList == null) return "";

        var path = pathList.GetFirstChild<Drawing.Path>();
        if (path == null) return "";

        // Path coordinate system
        var pathW = path.Width?.HasValue == true ? path.Width.Value : 100000L;
        var pathH = path.Height?.HasValue == true ? path.Height.Value : 100000L;
        if (pathW == 0) pathW = 100000;
        if (pathH == 0) pathH = 100000;

        // Helper: parse Drawing.Point X/Y (StringValue) to double percentage
        static bool TryParsePoint(Drawing.Point? pt, double pw, double ph, out double px, out double py)
        {
            px = py = 0;
            if (pt?.X?.HasValue != true || pt?.Y?.HasValue != true) return false;
            if (!long.TryParse(pt.X.Value, out var xv) || !long.TryParse(pt.Y.Value, out var yv)) return false;
            px = xv * 100.0 / pw;
            py = yv * 100.0 / ph;
            return true;
        }

        // Try polygon first (only moveTo + lineTo + close = all straight lines)
        bool hasOnlyLines = true;
        foreach (var child in path.ChildElements)
        {
            if (child is Drawing.CubicBezierCurveTo or Drawing.QuadraticBezierCurveTo)
            {
                hasOnlyLines = false;
                break;
            }
        }

        if (hasOnlyLines)
        {
            // Use clip-path: polygon() — better browser support
            var points = new List<string>();
            foreach (var child in path.ChildElements)
            {
                switch (child)
                {
                    case Drawing.MoveTo moveTo:
                        if (TryParsePoint(moveTo.GetFirstChild<Drawing.Point>(), pathW, pathH, out var mx, out var my))
                            points.Add($"{mx:0.##}% {my:0.##}%");
                        break;
                    case Drawing.LineTo lineTo:
                        if (TryParsePoint(lineTo.GetFirstChild<Drawing.Point>(), pathW, pathH, out var lx, out var ly))
                            points.Add($"{lx:0.##}% {ly:0.##}%");
                        break;
                    case Drawing.CloseShapePath:
                        break; // polygon implicitly closes
                }
            }
            if (points.Count >= 3)
                return $"clip-path:polygon({string.Join(",", points)})";
        }
        else
        {
            // Has curves — approximate with polygon() by sampling bezier curves
            // clip-path:path() uses pixel coordinates (not percentages), so we must
            // flatten curves into polygon points with percentage coordinates instead.
            var polyPoints = new List<string>();
            double curX = 0, curY = 0;
            const int bezierSegments = 8; // number of line segments per bezier curve

            foreach (var child in path.ChildElements)
            {
                switch (child)
                {
                    case Drawing.MoveTo moveTo:
                        if (TryParsePoint(moveTo.GetFirstChild<Drawing.Point>(), pathW, pathH, out var mx, out var my))
                        {
                            polyPoints.Add($"{mx:0.##}% {my:0.##}%");
                            curX = mx; curY = my;
                        }
                        break;
                    case Drawing.LineTo lineTo:
                        if (TryParsePoint(lineTo.GetFirstChild<Drawing.Point>(), pathW, pathH, out var lx, out var ly))
                        {
                            polyPoints.Add($"{lx:0.##}% {ly:0.##}%");
                            curX = lx; curY = ly;
                        }
                        break;
                    case Drawing.CubicBezierCurveTo cubicBez:
                    {
                        var pts = cubicBez.Elements<Drawing.Point>().ToList();
                        if (pts.Count >= 3
                            && TryParsePoint(pts[0], pathW, pathH, out var c1x, out var c1y)
                            && TryParsePoint(pts[1], pathW, pathH, out var c2x, out var c2y)
                            && TryParsePoint(pts[2], pathW, pathH, out var c3x, out var c3y))
                        {
                            // Sample cubic bezier: B(t) = (1-t)^3*P0 + 3(1-t)^2*t*P1 + 3(1-t)*t^2*P2 + t^3*P3
                            for (int i = 1; i <= bezierSegments; i++)
                            {
                                double t = i / (double)bezierSegments;
                                double u = 1 - t;
                                double px = u * u * u * curX + 3 * u * u * t * c1x + 3 * u * t * t * c2x + t * t * t * c3x;
                                double py = u * u * u * curY + 3 * u * u * t * c1y + 3 * u * t * t * c2y + t * t * t * c3y;
                                polyPoints.Add($"{px:0.##}% {py:0.##}%");
                            }
                            curX = c3x; curY = c3y;
                        }
                        break;
                    }
                    case Drawing.QuadraticBezierCurveTo quadBez:
                    {
                        var pts = quadBez.Elements<Drawing.Point>().ToList();
                        if (pts.Count >= 2
                            && TryParsePoint(pts[0], pathW, pathH, out var q1x, out var q1y)
                            && TryParsePoint(pts[1], pathW, pathH, out var q2x, out var q2y))
                        {
                            // Sample quadratic bezier: B(t) = (1-t)^2*P0 + 2(1-t)*t*P1 + t^2*P2
                            for (int i = 1; i <= bezierSegments; i++)
                            {
                                double t = i / (double)bezierSegments;
                                double u = 1 - t;
                                double px = u * u * curX + 2 * u * t * q1x + t * t * q2x;
                                double py = u * u * curY + 2 * u * t * q1y + t * t * q2y;
                                polyPoints.Add($"{px:0.##}% {py:0.##}%");
                            }
                            curX = q2x; curY = q2y;
                        }
                        break;
                    }
                    case Drawing.CloseShapePath:
                        break; // polygon implicitly closes
                }
            }
            if (polyPoints.Count >= 3)
                return $"clip-path:polygon({string.Join(",", polyPoints)})";
        }

        return "";
    }

    // ==================== CSS Helper: Gradient ====================

    /// <summary>
    /// R15-2: Build an SVG &lt;linearGradient&gt; element from an OOXML &lt;a:gradFill&gt;
    /// for use as a connector/line stroke (stroke="url(#id)"). Reuses the same stop
    /// color resolution as GradientToCss. <paramref name="firstStopColor"/> returns the
    /// first stop's color as a solid fallback. Returns "" when there are fewer than 2 stops.
    /// </summary>
    private static string BuildSvgLinearGradient(Drawing.GradientFill gradFill, string id,
        Dictionary<string, string> themeColors, out string? firstStopColor)
    {
        firstStopColor = null;
        var stops = gradFill.GradientStopList?.Elements<Drawing.GradientStop>().ToList();
        if (stops == null || stops.Count < 2) return "";

        var sb = new System.Text.StringBuilder();
        sb.Append($"<linearGradient id=\"{id}\">");
        foreach (var gs in stops)
        {
            var color = ResolveGradientStopColor(gs, themeColors);
            firstStopColor ??= color;
            var pos = (gs.Position?.Value ?? 0) / 1000.0;
            sb.Append($"<stop offset=\"{pos:0.##}%\" stop-color=\"{CssSanitizeColor(color)}\"/>");
        }
        sb.Append("</linearGradient>");
        return sb.ToString();
    }

    /// <summary>Resolve a single gradient stop's color (shared by GradientToCss / BuildSvgLinearGradient).</summary>
    private static string ResolveGradientStopColor(Drawing.GradientStop gs, Dictionary<string, string> themeColors)
    {
        var color = ResolveFillColor(gs.GetFirstChild<Drawing.SolidFill>(), themeColors);
        if (color != null) return color;
        var rgbEl = gs.GetFirstChild<Drawing.RgbColorModelHex>();
        var rgb = rgbEl?.Val?.Value;
        if (rgb != null && rgb.Length >= 6 && rgb[..6].All(char.IsAsciiHexDigit))
        {
            var alpha = rgbEl!.GetFirstChild<Drawing.Alpha>()?.Val?.Value;
            if (alpha.HasValue && alpha.Value < 100000)
            {
                var (r, g, b) = ColorMath.HexToRgb(rgb[..6]);
                return $"rgba({r},{g},{b},{alpha.Value / 100000.0:0.##})";
            }
            return $"#{rgb[..6]}";
        }
        var schemeEl = gs.GetFirstChild<Drawing.SchemeColor>();
        var scheme = schemeEl?.Val?.InnerText;
        if (scheme != null && themeColors.TryGetValue(scheme, out var tc))
            return ApplyColorTransforms(tc, schemeEl!);
        return "transparent";
    }

    private static string GradientToCss(Drawing.GradientFill gradFill, Dictionary<string, string> themeColors)
    {
        var stops = gradFill.GradientStopList?.Elements<Drawing.GradientStop>().ToList();
        if (stops == null || stops.Count < 2) return "transparent";

        var cssStops = new List<string>();
        foreach (var gs in stops)
        {
            var color = ResolveFillColor(gs.GetFirstChild<Drawing.SolidFill>(), themeColors);
            if (color == null)
            {
                // Try direct color children. A gradient stop carries its color as a
                // direct <a:srgbClr>/<a:schemeClr> child (not wrapped in solidFill),
                // and that color may have an <a:alpha> child. Bug #7: read the alpha
                // and emit rgba() — the old path dropped it, losing transparency.
                var rgbEl = gs.GetFirstChild<Drawing.RgbColorModelHex>();
                var rgb = rgbEl?.Val?.Value;
                if (rgb != null && rgb.Length >= 6 && rgb[..6].All(char.IsAsciiHexDigit))
                {
                    var alpha = rgbEl!.GetFirstChild<Drawing.Alpha>()?.Val?.Value;
                    if (alpha.HasValue && alpha.Value < 100000)
                    {
                        var (r, g, b) = ColorMath.HexToRgb(rgb[..6]);
                        color = $"rgba({r},{g},{b},{alpha.Value / 100000.0:0.##})";
                    }
                    else
                        color = $"#{rgb[..6]}";
                }
                else
                {
                    var schemeEl = gs.GetFirstChild<Drawing.SchemeColor>();
                    var scheme = schemeEl?.Val?.InnerText;
                    if (scheme != null && themeColors.TryGetValue(scheme, out var tc))
                    {
                        // Apply lumMod/lumOff/tint/shade/alpha transforms (same as
                        // ResolveFillColor); without this the stops collapse to the base hex.
                        color = ApplyColorTransforms(tc, schemeEl!);
                    }
                    else
                        color = "transparent";
                }
            }
            var pos = gs.Position?.Value;
            if (pos.HasValue)
                cssStops.Add($"{color} {pos.Value / 1000.0:0.##}%");
            else
                cssStops.Add(color);
        }

        // Radial or linear?
        var pathGrad = gradFill.GetFirstChild<Drawing.PathGradientFill>();
        if (pathGrad != null)
        {
            // OOXML <a:path path="circle"> with default fill rectangle fills to the shape
            // bounds (last stop at the edge). CSS default is `farthest-corner`, which overshoots
            // for square-ish shapes. `closest-side` lands the final stop at the nearer edge,
            // matching Office's rendering for rectangular shapes.
            // Bug #6: the gradient FOCUS comes from <a:fillToRect l/t/r/b> (1/1000%
            // units). The focus point is the rect's top-left (l, t): center =
            // 50/50/50/50 → at 50% 50%; tl = l0/t0 → at 0% 0%; br = l100000/t100000
            // → at 100% 100%; tr = l100000/t0 → at 100% 0%. Previously omitted, so
            // every focal variant rendered centered.
            var ftr = pathGrad.FillToRectangle;
            var cx = (ftr?.Left?.Value ?? 50000) / 1000.0;
            var cy = (ftr?.Top?.Value ?? 50000) / 1000.0;
            // Size keyword must track the focus: `closest-side` is only right for a
            // CENTERED focus (the nearer-edge radius that matches Office on a
            // rectangle). At a corner/edge focus, closest-side's nearest-edge
            // distance collapses to ~0 → the gradient degenerates to a point and
            // the focus colors vanish (only the final stop's color shows). Use
            // `farthest-corner` for off-center foci so the gradient fills the shape
            // out to its far corner, matching native's large color fill.
            var centered = Math.Abs(cx - 50) < 0.5 && Math.Abs(cy - 50) < 0.5;
            var sizeKeyword = centered ? "closest-side" : "farthest-corner";
            return $"radial-gradient(circle {sizeKeyword} at {cx:0.##}% {cy:0.##}%, {string.Join(", ", cssStops)})";
        }

        var linear = gradFill.GetFirstChild<Drawing.LinearGradientFill>();
        var angleDeg = linear?.Angle?.HasValue == true ? linear.Angle.Value / 60000.0 : 90.0;
        // OOXML angle 0° = top→bottom (same as CSS 180deg), so CSS angle = OOXML + 90°
        // Actually OOXML: 0 = right, 90 = bottom; CSS: 0 = up, 90 = right
        var cssAngle = angleDeg + 90;

        // scaled="1": the OOXML angle is measured relative to the shape's
        // bounding box (corner-to-corner), NOT in absolute degrees. A fixed
        // CSS angle ignores aspect ratio and lands off the corners on a
        // non-square shape. CSS corner keywords (`to bottom right`, …) are
        // aspect-aware by spec, so snap a scaled gradient to the corner
        // matching its quadrant. Unscaled gradients keep the literal angle.
        if (linear?.Scaled?.Value == true)
        {
            // Normalize CSS angle into [0,360); pick the nearest of the four
            // corner directions (45/135/225/315 → corners).
            var a = ((cssAngle % 360) + 360) % 360;
            var corner = a switch
            {
                >= 0 and < 90 => "to top right",
                >= 90 and < 180 => "to bottom right",
                >= 180 and < 270 => "to bottom left",
                _ => "to top left",
            };
            return $"linear-gradient({corner}, {string.Join(", ", cssStops)})";
        }

        return $"linear-gradient({cssAngle:0.##}deg, {string.Join(", ", cssStops)})";
    }

    // ==================== CSS Helper: Outline/Border ====================

    /// <summary>
    /// Parse outline into (widthPt, ooxmlDashType, color). Returns null if NoFill.
    /// </summary>
    private static (double widthPt, string dashType, string color, string cap, string cmpd)? ParseOutline(Drawing.Outline outline, Dictionary<string, string> themeColors)
    {
        if (outline.GetFirstChild<Drawing.NoFill>() != null) return null;

        // Empty <a:ln/> (no fill child, no width) means "inherit/default" — for text
        // shapes PowerPoint treats this as no line. Without this guard we fall through
        // to dk1 default + 0.5pt and paint a phantom border on every plain text box.
        if (outline.GetFirstChild<Drawing.SolidFill>() == null
            && outline.GetFirstChild<Drawing.GradientFill>() == null
            && outline.Width?.HasValue != true)
            return null;

        var color = ResolveFillColor(outline.GetFirstChild<Drawing.SolidFill>(), themeColors)
            ?? (themeColors.TryGetValue("dk1", out var dk1Hex) ? $"#{dk1Hex}" : "#000000");
        var widthPt = outline.Width?.HasValue == true ? outline.Width.Value / EmuConverter.EmuPerPointF : 1.0;
        if (widthPt < 0.5) widthPt = 0.5;

        var dash = outline.GetFirstChild<Drawing.PresetDash>();
        var dashType = "solid";
        if (dash?.Val?.HasValue == true)
            dashType = dash.Val.InnerText ?? "solid";

        // Line cap (<a:ln cap="rnd|sq|flat"/>) and compound type
        // (<a:ln cmpd="sng|dbl|thickThin|thinThick|tri"/>).
        var cap = outline.CapType?.HasValue == true ? (outline.CapType.InnerText ?? "flat") : "flat";
        var cmpd = outline.CompoundLineType?.HasValue == true ? (outline.CompoundLineType.InnerText ?? "sng") : "sng";

        return (widthPt, dashType, color, cap, cmpd);
    }

    /// <summary>
    /// Map OOXML line cap (<a:ln cap="rnd|sq|flat"/>) to the SVG stroke-linecap value.
    /// rnd→round (pill dash ends), sq→square, flat/default→butt.
    /// </summary>
    private static string CapToSvgLinecap(string cap) => cap switch
    {
        "rnd" => "round",
        "sq" => "square",
        _ => "butt",
    };

    // ==================== Style-matrix (p:style) resolution ====================
    //
    // A shape may carry a <p:style> with fillRef/lnRef/effectRef/fontRef that index
    // into the theme's <a:fmtScheme> (FormatScheme). When the shape's own spPr has no
    // explicit fill/outline/effect (or the run has no explicit color), these refs
    // supply the value. Explicit spPr/run values always WIN; we only resolve a ref
    // when the explicit value is absent.
    //
    // The FormatScheme is read fresh from the part chain each render (not cached) so
    // SDK-injected effectStyleLst entries are seen after reopen.

    /// <summary>
    /// Resolve the theme FormatScheme for a shape's part:
    /// SlidePart→SlideLayoutPart→SlideMasterPart→ThemePart→Theme→ThemeElements→FormatScheme.
    /// Read directly from the part each call (no caching) so post-reopen theme edits are honored.
    /// </summary>
    private static Drawing.FormatScheme? ResolveFormatScheme(OpenXmlPart part)
    {
        var theme = part switch
        {
            SlidePart sp => sp.SlideLayoutPart?.SlideMasterPart?.ThemePart?.Theme,
            SlideLayoutPart lp => lp.SlideMasterPart?.ThemePart?.Theme,
            SlideMasterPart mp => mp.ThemePart?.Theme,
            _ => null,
        };
        return theme?.ThemeElements?.FormatScheme;
    }

    /// <summary>
    /// Resolve a style-matrix reference's <a:schemeClr> (the ref's own color slot,
    /// e.g. fillRef/lnRef/fontRef child) to a "#RRGGBB"/rgba() string via the theme
    /// color map, applying any lumMod/lumOff/tint/shade/alpha transforms.
    /// </summary>
    private static string? ResolveStyleRefSchemeColor(OpenXmlCompositeElement? styleRef, Dictionary<string, string> themeColors)
    {
        var schemeColor = styleRef?.GetFirstChild<Drawing.SchemeColor>();
        if (schemeColor?.Val?.HasValue != true) return null;
        var name = schemeColor.Val!.InnerText;
        if (name == null || !themeColors.TryGetValue(name, out var hex)) return null;
        return ApplyColorTransforms(hex, schemeColor);
    }

    /// <summary>
    /// Style-matrix fill fallback: resolve <p:style>/<a:fillRef idx=N> against
    /// FormatScheme.FillStyleList[N] (1-based), blending the fillRef's schemeClr into
    /// the indexed fill style. Returns a "background:..." CSS string, or "" if none.
    /// </summary>
    private static string GetStyleFillRefCss(ShapeStyle? style, OpenXmlPart part, Dictionary<string, string> themeColors)
    {
        var fillRef = style?.FillReference;
        var idx = fillRef?.Index?.Value ?? 0;
        if (fillRef == null || idx == 0) return "";

        var refColor = ResolveStyleRefSchemeColor(fillRef, themeColors);

        var fmtScheme = ResolveFormatScheme(part);
        var fillStyle = fmtScheme?.FillStyleList?.ChildElements
            .OfType<OpenXmlElement>()
            .ElementAtOrDefault((int)idx - 1);

        // The indexed fill style entry is most commonly a <a:solidFill> referencing
        // <a:schemeClr val="phClr"/> (the placeholder color = the fillRef's schemeClr).
        // Emit the fillRef schemeClr directly — it is the resolved phClr.
        if (fillStyle is Drawing.SolidFill && refColor != null)
            return $"background:{refColor}";

        // Gradient/other indexed fill: render via GradientToCss but substitute phClr.
        if (fillStyle is Drawing.GradientFill gf)
        {
            var css = GradientToCss(gf, themeColors);
            return string.IsNullOrEmpty(css) ? (refColor != null ? $"background:{refColor}" : "") : $"background:{css}";
        }

        // Unknown/no indexed style entry — fall back to the bare ref color.
        return refColor != null ? $"background:{refColor}" : "";
    }

    /// <summary>
    /// Style-matrix outline fallback: resolve <p:style>/<a:lnRef idx=N> against
    /// FormatScheme.LineStyleList[N] (1-based), coloring it with the lnRef's schemeClr.
    /// Returns a "border:..." CSS string, or "" if none.
    /// </summary>
    private static string GetStyleLineRefCss(ShapeStyle? style, OpenXmlPart part, Dictionary<string, string> themeColors)
    {
        var lnRef = style?.LineReference;
        var idx = lnRef?.Index?.Value ?? 0;
        if (lnRef == null || idx == 0) return "";

        var refColor = ResolveStyleRefSchemeColor(lnRef, themeColors)
            ?? (themeColors.TryGetValue("dk1", out var dk1) ? $"#{dk1}" : "#000000");

        var fmtScheme = ResolveFormatScheme(part);
        var lineStyle = fmtScheme?.LineStyleList?.ChildElements
            .OfType<Drawing.Outline>()
            .ElementAtOrDefault((int)idx - 1);

        var widthPt = lineStyle?.Width?.HasValue == true ? lineStyle.Width!.Value / EmuConverter.EmuPerPointF : 1.0;
        if (widthPt < 0.5) widthPt = 0.5;

        return $"border:{widthPt:0.##}pt solid {refColor}";
    }

    /// <summary>
    /// Style-matrix effect fallback: resolve <p:style>/<a:effectRef idx=N> against
    /// FormatScheme.EffectStyleList[N] (1-based), reusing EffectListToShadowCss on the
    /// indexed effect style's <a:effectLst>. Returns the shadow CSS, or "" if none.
    /// </summary>
    private static string GetStyleEffectRefCss(ShapeStyle? style, OpenXmlPart part, Dictionary<string, string> themeColors)
        => EffectListToShadowCss(ResolveStyleEffectRefList(style, part), themeColors);

    /// <summary>
    /// Resolve the <a:effectLst> from a shape's <p:style>/<a:effectRef idx=N>
    /// against the theme FormatScheme.EffectStyleList[N] (1-based). Returns null
    /// when there is no effectRef or no matching style entry. Callers can pass the
    /// result to the shadow/glow/reflection converters so effectRef-only shapes
    /// surface all three effect kinds (R14-1), not just shadow.
    /// </summary>
    private static Drawing.EffectList? ResolveStyleEffectRefList(ShapeStyle? style, OpenXmlPart part)
    {
        var effectRef = style?.EffectReference;
        var idx = effectRef?.Index?.Value ?? 0;
        if (effectRef == null || idx == 0) return null;

        var fmtScheme = ResolveFormatScheme(part);
        var effectStyle = fmtScheme?.EffectStyleList?.ChildElements
            .OfType<Drawing.EffectStyle>()
            .ElementAtOrDefault((int)idx - 1);
        return effectStyle?.GetFirstChild<Drawing.EffectList>();
    }

    private static string OutlineToCss(Drawing.Outline outline, Dictionary<string, string> themeColors)
    {
        var parsed = ParseOutline(outline, themeColors);
        if (parsed == null) return "";
        var (widthPt, dashType, color, _, cmpd) = parsed.Value;

        var borderStyle = dashType switch
        {
            "dash" or "lgDash" or "sysDash" => "dashed",
            "dot" or "sysDot" => "dotted",
            "dashDot" or "lgDashDot" or "sysDashDot" or "sysDashDotDot" => "dashed",
            _ => "solid"
        };
        // Compound (dbl/thickThin/thinThick/tri) draws multiple parallel lines.
        // CSS `double` renders two parallel lines when the border is wide enough.
        if (cmpd != "sng" && dashType == "solid")
            borderStyle = "double";

        return $"border:{widthPt:0.##}pt {borderStyle} {color}";
    }

    /// <summary>
    /// Convert OOXML dash type to SVG stroke-dasharray relative to stroke width.
    /// </summary>
    private static string DashTypeToSvgDasharray(string dashType, double strokeWidth)
    {
        var w = strokeWidth;
        return dashType switch
        {
            // Dot is a visible short segment (length = stroke width) with linecap=butt
            // so the dot renders as a square of side w. Prior implementation used "0.1"
            // as a zero-length segment relying on stroke-linecap=round to paint a cap;
            // that collapses when linecap=butt or when stroke-width rounds down.
            "solid" => "",
            "dot" or "sysDot" => $"{w:0.##} {w * 2:0.##}",
            "dash" => $"{w * 4:0.##} {w * 3:0.##}",
            "lgDash" => $"{w * 8:0.##} {w * 3:0.##}",
            "sysDash" => $"{w * 3:0.##} {w * 1:0.##}",
            "dashDot" => $"{w * 4:0.##} {w * 2:0.##} {w:0.##} {w * 2:0.##}",
            "lgDashDot" => $"{w * 8:0.##} {w * 2:0.##} {w:0.##} {w * 2:0.##}",
            "sysDashDot" => $"{w * 3:0.##} {w * 1.5:0.##} {w:0.##} {w * 1.5:0.##}",
            "sysDashDotDot" => $"{w * 3:0.##} {w * 1.5:0.##} {w:0.##} {w * 1.5:0.##} {w:0.##} {w * 1.5:0.##}",
            "lgDashDotDot" => $"{w * 8:0.##} {w * 2:0.##} {w:0.##} {w * 2:0.##} {w:0.##} {w * 2:0.##}",
            _ => ""
        };
    }

    // ==================== CSS Helper: Shadow ====================

    private static string EffectListToShadowCss(Drawing.EffectList? effectList, Dictionary<string, string> themeColors)
    {
        if (effectList == null) return "";

        var shadow = effectList.GetFirstChild<Drawing.OuterShadow>();
        if (shadow != null)
        {
            var color = ResolveShadowColor(shadow, themeColors);
            var blurPt = shadow.BlurRadius?.HasValue == true ? shadow.BlurRadius.Value / EmuConverter.EmuPerPointF : 0;
            var distPt = shadow.Distance?.HasValue == true ? shadow.Distance.Value / EmuConverter.EmuPerPointF : 0;
            var angleDeg = shadow.Direction?.HasValue == true ? shadow.Direction.Value / 60000.0 : 0;
            var angleRad = angleDeg * Math.PI / 180;
            var offsetX = distPt * Math.Cos(angleRad);
            var offsetY = distPt * Math.Sin(angleRad);
            return $"filter:drop-shadow({offsetX:0.##}pt {offsetY:0.##}pt {blurPt:0.##}pt {color})";
        }

        // a:innerShdw has no CSS filter equivalent; render as an inset box-shadow.
        var inner = effectList.GetFirstChild<Drawing.InnerShadow>();
        if (inner != null)
        {
            var color = ResolveShadowColor(inner, themeColors);
            var blurPt = inner.BlurRadius?.HasValue == true ? inner.BlurRadius.Value / EmuConverter.EmuPerPointF : 0;
            var distPt = inner.Distance?.HasValue == true ? inner.Distance.Value / EmuConverter.EmuPerPointF : 0;
            var angleDeg = inner.Direction?.HasValue == true ? inner.Direction.Value / 60000.0 : 0;
            var angleRad = angleDeg * Math.PI / 180;
            var offsetX = distPt * Math.Cos(angleRad);
            var offsetY = distPt * Math.Sin(angleRad);
            return $"box-shadow:inset {offsetX:0.##}pt {offsetY:0.##}pt {blurPt:0.##}pt {color}";
        }

        // R9-1: a:prstShdw (preset shadow). No CSS filter equivalent for the
        // preset bitmap; approximate from its dist/dir/color as a box-shadow.
        var preset = effectList.GetFirstChild<Drawing.PresetShadow>();
        if (preset != null)
        {
            var color = ResolveShadowColor(preset, themeColors);
            var distPt = preset.Distance?.HasValue == true ? preset.Distance.Value / EmuConverter.EmuPerPointF : 0;
            var angleDeg = preset.Direction?.HasValue == true ? preset.Direction.Value / 60000.0 : 0;
            var angleRad = angleDeg * Math.PI / 180;
            var offsetX = distPt * Math.Cos(angleRad);
            var offsetY = distPt * Math.Sin(angleRad);
            // Emit as filter:drop-shadow so it merges with the glow/shadow filter
            // chain in RenderShape (box-shadow can't combine there). Blur ≈ half dist.
            return $"filter:drop-shadow({offsetX:0.##}pt {offsetY:0.##}pt {(distPt / 2):0.##}pt {color})";
        }

        return "";
    }

    /// <summary>
    /// Resolve a shadow's color (rgba) from its srgbClr/schemeClr child, applying
    /// lumMod/lumOff/tint/shade/alpha transforms (default 50% opacity when no alpha).
    /// </summary>
    private static string ResolveShadowColor(OpenXmlCompositeElement shadow, Dictionary<string, string> themeColors)
    {
        var alpha = shadow.Descendants<Drawing.Alpha>().FirstOrDefault()?.Val?.Value ?? 50000;
        var opacity = alpha / 100000.0;
        var rgbEl = shadow.GetFirstChild<Drawing.RgbColorModelHex>();
        var rgb = rgbEl?.Val?.Value;
        if (rgb != null && rgb.Length >= 6 && rgb[..6].All(char.IsAsciiHexDigit))
        {
            var transformed = ApplyRgbColorTransforms(rgb[..6], rgbEl!);
            var (r, g, b) = ColorMath.HexToRgb(transformed.StartsWith('#') ? transformed[1..] : transformed);
            return $"rgba({r},{g},{b},{opacity:0.##})";
        }

        var schemeEl = shadow.GetFirstChild<Drawing.SchemeColor>();
        var schemeName = schemeEl?.Val?.InnerText;
        if (schemeName != null && themeColors.TryGetValue(schemeName, out var sc))
        {
            // ApplyTransforms returns #RRGGBB (or rgba when alpha given); strip to hex
            // then re-apply the shadow opacity uniformly. Read by local name so both
            // typed and OpenXmlUnknownElement transform children resolve.
            var transformed = ColorMath.ApplyTransforms(sc,
                tint: ReadTransformVal(schemeEl!, "tint"),
                shade: ReadTransformVal(schemeEl!, "shade"),
                lumMod: ReadTransformVal(schemeEl!, "lumMod"),
                lumOff: ReadTransformVal(schemeEl!, "lumOff"));
            var hex = transformed.StartsWith('#') ? transformed[1..] : transformed;
            var (r, g, b) = ColorMath.HexToRgb(hex);
            return $"rgba({r},{g},{b},{opacity:0.##})";
        }

        return $"rgba(0,0,0,{opacity:0.##})";
    }

    /// <summary>
    /// Apply lumMod/lumOff/tint/shade transforms (if present as children) to an
    /// srgbClr hex. Returns a #RRGGBB hex (alpha handled separately by callers).
    /// </summary>
    private static string ApplyRgbColorTransforms(string hex, Drawing.RgbColorModelHex rgbEl)
    {
        // Alpha is handled by the caller; pass only lum/tint/shade so the result
        // stays a #RRGGBB hex. Reads by local name to support both typed and
        // OpenXmlUnknownElement children (see ApplyColorTransforms remarks).
        return ColorMath.ApplyTransforms(hex,
            tint: ReadTransformVal(rgbEl, "tint"),
            shade: ReadTransformVal(rgbEl, "shade"),
            lumMod: ReadTransformVal(rgbEl, "lumMod"),
            lumOff: ReadTransformVal(rgbEl, "lumOff"));
    }

    // ==================== CSS Helper: Glow ====================

    private static string EffectListToGlowCss(Drawing.EffectList? effectList, Dictionary<string, string> themeColors)
    {
        if (effectList == null) return "";

        var glow = effectList.GetFirstChild<Drawing.Glow>();
        if (glow == null) return "";

        var alpha = glow.Descendants<Drawing.Alpha>().FirstOrDefault()?.Val?.Value ?? 40000;
        var opacity = alpha / 100000.0;
        var radiusPt = glow.Radius?.HasValue == true ? glow.Radius.Value / EmuConverter.EmuPerPointF : 5;

        var rgb = glow.GetFirstChild<Drawing.RgbColorModelHex>()?.Val?.Value;
        (int r, int g, int b)? rgbTuple = null;
        if (rgb != null)
        {
            rgbTuple = ColorMath.HexToRgb(rgb);
        }
        else
        {
            var schemeColor = glow.GetFirstChild<Drawing.SchemeColor>()?.Val?.InnerText;
            var resolved = schemeColor != null && themeColors.TryGetValue(schemeColor, out var sc) ? sc : null;
            if (resolved != null)
            {
                rgbTuple = ColorMath.HexToRgb(resolved);
            }
            else
            {
                // No color specified — use theme accent1 or transparent
                var acc1 = themeColors.TryGetValue("accent1", out var a1) ? a1 : null;
                if (acc1 != null)
                    rgbTuple = ColorMath.HexToRgb(acc1);
            }
        }

        if (rgbTuple == null)
            return ""; // no resolvable color — emit nothing rather than an invisible shadow

        var (gr, gg, gb) = rgbTuple.Value;

        // A single low-alpha drop-shadow is barely visible on a white slide.
        // Real PowerPoint paints a dense saturated halo, so stack several
        // drop-shadow layers at progressively wider radii. Each layer composites
        // over the previous, building up the colored halo to a visible density
        // matching native. Per-layer alpha is boosted relative to the OOXML alpha
        // (clamped) since drop-shadow blur disperses the color heavily.
        double layerAlpha = Math.Min(0.9, Math.Max(0.45, opacity + 0.35));
        string col = $"rgba({gr},{gg},{gb},{layerAlpha:0.##})";
        var layers = new[]
        {
            $"drop-shadow(0 0 {radiusPt * 0.4:0.##}pt {col})",
            $"drop-shadow(0 0 {radiusPt:0.##}pt {col})",
            $"drop-shadow(0 0 {radiusPt:0.##}pt {col})",
            $"drop-shadow(0 0 {radiusPt * 1.6:0.##}pt {col})",
        };
        return $"filter:{string.Join(" ", layers)}";
    }

    // ==================== CSS Helper: Reflection ====================

    /// <summary>
    /// Generates CSS -webkit-box-reflect for an OOXML reflection effect.
    /// Uses the reflection's StartOpacity, EndAlpha, EndPosition, Distance, and BlurRadius
    /// to build an appropriate linear-gradient fade.
    /// </summary>
    private static string EffectListToReflectionCss(Drawing.EffectList? effectList)
    {
        if (effectList == null) return "";

        var refl = effectList.GetFirstChild<Drawing.Reflection>();
        if (refl == null) return "";

        // Distance between shape bottom and reflection start (EMU → pt)
        var distPt = refl.Distance?.HasValue == true ? refl.Distance.Value / EmuConverter.EmuPerPointF : 0;

        // StartOpacity: initial opacity of reflected image (thousandths of a percent)
        var startOpacity = refl.StartOpacity?.HasValue == true ? refl.StartOpacity.Value / 100000.0 : 0.52;

        // EndAlpha: final opacity (thousandths of a percent)
        var endOpacity = refl.EndAlpha?.HasValue == true ? refl.EndAlpha.Value / 100000.0 : 0.0;

        // EndPosition: how much of the shape height is reflected (thousandths of a percent → CSS percentage).
        // In -webkit-box-reflect, 0% is the top of the reflection (closest to the source shape) and
        // 100% is the far edge. The reflection should be most opaque at the top (startOpacity) and
        // fade to endOpacity at endPos%, then fully transparent beyond endPos.
        var endPos = refl.EndPosition?.HasValue == true ? Math.Clamp(refl.EndPosition.Value / 1000.0, 0, 100) : 90.0;

        var startStop = $"rgba(255,255,255,{startOpacity:0.###}) 0%";
        var endStop = $"rgba(255,255,255,{endOpacity:0.###}) {endPos:0.#}%";
        var tailStop = endPos < 100 ? $",transparent 100%" : "";

        return $"-webkit-box-reflect:below {distPt:0.##}pt linear-gradient({startStop},{endStop}{tailStop})";
    }

    // ==================== CSS Helper: Preset Geometry ====================

    /// <summary>Plus/cross polygon with arm width proportional to min(w,h).</summary>
    private static string PlusPolygon(long w, long h)
    {
        // OOXML default: arm width = 25% of min dimension
        var minDim = Math.Min(w, h);
        var armW = minDim * 0.25;
        var hPct = armW / w * 100; // horizontal arm width as % of width
        var vPct = armW / h * 100; // vertical arm width as % of height
        var l = (50 - hPct); var r = (50 + hPct);
        var t = (50 - vPct); var b = (50 + vPct);
        return $"clip-path:polygon({l:0.#}% 0,{r:0.#}% 0,{r:0.#}% {t:0.#}%,100% {t:0.#}%,100% {b:0.#}%,{r:0.#}% {b:0.#}%,{r:0.#}% 100%,{l:0.#}% 100%,{l:0.#}% {b:0.#}%,0 {b:0.#}%,0 {t:0.#}%,{l:0.#}% {t:0.#}%)";
    }

    private static string PresetGeometryToCss(string preset) =>
        PresetGeometryToCss(preset, 0, 0, null);

    /// <summary>
    /// Read an adjustment value from PresetGeometry's AdjustValueList (OOXML "val NNNNN" formula).
    /// </summary>
    private static long ReadAdjValueCss(Drawing.PresetGeometry? presetGeom, int index, long defaultValue)
    {
        var avList = presetGeom?.GetFirstChild<Drawing.AdjustValueList>();
        if (avList == null) return defaultValue;
        var guides = avList.Elements<Drawing.ShapeGuide>().ToList();
        if (index >= guides.Count) return defaultValue;
        var formula = guides[index].Formula?.Value;
        if (formula != null && formula.StartsWith("val "))
        {
            if (long.TryParse(formula.AsSpan(4), out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    /// <summary>
    /// Build a clip-path polygon for rightArrow honoring OOXML avLst.
    /// adj1 = tail height relative to shape height (0..100000, default 50000 = 50%)
    /// adj2 = head width relative to min(w,h) (0..100000, default 50000)
    /// </summary>
    private static string RightArrowPolygon(long widthEmu, long heightEmu, Drawing.PresetGeometry? presetGeom)
    {
        var adj1 = ReadAdjValueCss(presetGeom, 0, 50000);
        var adj2 = ReadAdjValueCss(presetGeom, 1, 50000);
        // Clamp avLst values to sane range
        if (adj1 < 0) adj1 = 0; if (adj1 > 100000) adj1 = 100000;
        if (adj2 < 0) adj2 = 0; if (adj2 > 100000) adj2 = 100000;

        // Tail vertical extent (centered on midline): adj1 fraction of height
        var tailTop = (100000.0 - adj1) / 2000.0;   // e.g. 25%
        var tailBot = 100.0 - tailTop;              // e.g. 75%

        // Head width measured from the right edge. Fallback to square assumption if dims missing.
        double headStartX;
        if (widthEmu > 0 && heightEmu > 0)
        {
            var minSide = Math.Min(widthEmu, heightEmu);
            var headWidthEmu = minSide * adj2 / 100000.0;
            if (headWidthEmu > widthEmu) headWidthEmu = widthEmu;
            headStartX = (widthEmu - headWidthEmu) / (double)widthEmu * 100.0;
        }
        else
        {
            headStartX = 100.0 - adj2 / 1000.0; // fallback: treat adj2 as % of width
        }

        return $"clip-path:polygon(0 {tailTop:0.##}%,{headStartX:0.##}% {tailTop:0.##}%,{headStartX:0.##}% 0,100% 50%,{headStartX:0.##}% 100%,{headStartX:0.##}% {tailBot:0.##}%,0 {tailBot:0.##}%)";
    }

    /// <summary>
    /// Build a directional arrow clip-path honoring avLst, derived from the
    /// rightArrow geometry by mirroring/transposing the point coordinates.
    /// dir: "left" mirrors X, "up" transposes (swap X/Y), "down" transposes
    /// then mirrors Y. adj1/adj2 carry the same meaning as rightArrow.
    /// </summary>
    private static string DirectionalArrowPolygon(string dir, long widthEmu, long heightEmu, Drawing.PresetGeometry? presetGeom)
    {
        // For up/down the head extends along height, so the head-width adj is
        // measured against the perpendicular dimension — swap dims when transposing.
        var (w, h) = (dir == "up" || dir == "down") ? (heightEmu, widthEmu) : (widthEmu, heightEmu);
        var baseCss = RightArrowPolygon(w, h, presetGeom); // "clip-path:polygon(...)"
        var inner = System.Text.RegularExpressions.Regex.Match(baseCss, @"polygon\(([^)]+)\)").Groups[1].Value;
        var pts = inner.Split(',');
        var outPts = new List<string>();
        foreach (var p in pts)
        {
            var xy = p.Trim().Split(' ');
            double x = ParsePct(xy[0]);
            double y = ParsePct(xy[1]);
            double nx, ny;
            switch (dir)
            {
                case "left": nx = 100 - x; ny = y; break;
                case "up":   nx = y; ny = x; break;       // transpose
                case "down": nx = y; ny = 100 - x; break; // transpose + flip Y
                default:     nx = x; ny = y; break;
            }
            outPts.Add($"{nx:0.##}% {ny:0.##}%");
        }
        return $"clip-path:polygon({string.Join(",", outPts)})";
    }

    private static double ParsePct(string s) =>
        double.TryParse(s.TrimEnd('%').Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>
    /// Build a clip-path polygon for a 5-point star honoring OOXML adj value.
    /// adj = inner radius fraction * 50000 (default 19098, giving inner ratio ~0.382).
    /// Star is stretched to fill bounding box (outer radius = min(w,h)/2 scaled independently to w,h).
    /// </summary>
    private static string Star5Polygon(Drawing.PresetGeometry? presetGeom)
    {
        var adj = ReadAdjValueCss(presetGeom, 0, 19098);
        if (adj < 0) adj = 0; if (adj > 50000) adj = 50000;
        var innerRatio = adj / 50000.0;

        var pts = new List<string>();
        // 10 points around the center, alternating outer (radius=0.5) and inner (radius=0.5*innerRatio).
        // Start at top (angle = -90°), step = 36° = PI/5. Scale x,y to 0..100%.
        for (int i = 0; i < 10; i++)
        {
            var angle = -Math.PI / 2 + Math.PI * i / 5;
            var r = (i % 2 == 0) ? 0.5 : 0.5 * innerRatio;
            var x = 50.0 + r * Math.Cos(angle) * 100.0;
            var y = 50.0 + r * Math.Sin(angle) * 100.0;
            pts.Add($"{x:0.##}% {y:0.##}%");
        }
        return $"clip-path:polygon({string.Join(",", pts)})";
    }

    private static string PresetGeometryToCss(string preset, long widthEmu, long heightEmu,
        Drawing.PresetGeometry? presetGeom)
    {
        // Parametric arrows honoring avLst
        if (preset == "rightArrow")
            return RightArrowPolygon(widthEmu, heightEmu, presetGeom);
        if (preset == "leftArrow")
            return DirectionalArrowPolygon("left", widthEmu, heightEmu, presetGeom);
        if (preset == "upArrow")
            return DirectionalArrowPolygon("up", widthEmu, heightEmu, presetGeom);
        if (preset == "downArrow")
            return DirectionalArrowPolygon("down", widthEmu, heightEmu, presetGeom);
        // Parametric star5 honoring avLst
        if (preset == "star5")
            return Star5Polygon(presetGeom);

        // Calculate roundRect corner radius from avLst or default (16.667% of shorter side)
        if (preset is "roundRect" or "round1Rect" or "round2SameRect" or "round2DiagRect")
        {
            var minSide = Math.Min(widthEmu, heightEmu);
            // Default adjustment value is 16667 (= 16.667%)
            long avVal = 16667;
            var avList = presetGeom?.GetFirstChild<Drawing.AdjustValueList>();
            var gd = avList?.GetFirstChild<Drawing.ShapeGuide>();
            if (gd?.Formula?.Value != null && gd.Formula.Value.StartsWith("val "))
            {
                if (long.TryParse(gd.Formula.Value.AsSpan(4), out var parsed))
                    avVal = parsed;
            }
            var radiusEmu = minSide * avVal / 100000;
            var radiusPt = Units.EmuToPt(radiusEmu);
            var r = $"{radiusPt:0.##}pt";
            if (minSide <= 0) r = "6pt"; // fallback if no dimensions

            return preset switch
            {
                "roundRect" => $"border-radius:{r}",
                "round1Rect" => $"border-radius:{r} 0 0 0",
                "round2SameRect" => $"border-radius:{r} {r} 0 0",
                "round2DiagRect" => $"border-radius:{r} 0 {r} 0",
                _ => ""
            };
        }

        // Parametric snip rectangles honoring avLst. The snipped corner size is
        // adj/100000 of the shorter side (matches roundRect's adj semantics).
        // Default adj for snip presets is 16667 (16.667%); the old code hardcoded
        // 8%/92% and ignored avLst entirely, so a custom adj never moved the
        // corner. Read adj1 (and adj2 for snip2Same/DiagRect) like roundRect does.
        if (preset is "snip1Rect" or "snip2SameRect" or "snip2DiagRect")
        {
            var avList = presetGeom?.GetFirstChild<Drawing.AdjustValueList>();
            var gds = avList?.Elements<Drawing.ShapeGuide>().ToList() ?? new List<Drawing.ShapeGuide>();
            long ReadAdj(int i, long dflt)
            {
                if (i < gds.Count && gds[i].Formula?.Value is string f && f.StartsWith("val ")
                    && long.TryParse(f.AsSpan(4), out var v)) return v;
                return dflt;
            }
            // adj is a fraction of the shorter side; convert to per-axis percent so
            // the snip is square (a 50% adj on a non-square box snips minSide/2 on
            // both axes). Clamp to [0,50] — a corner snip cannot exceed half a side.
            long minSideEmu = Math.Min(widthEmu, heightEmu);
            double AdjPct(long adj, bool horizontal)
            {
                var snipEmu = minSideEmu * adj / 100000.0;
                var axis = horizontal ? widthEmu : heightEmu;
                var pct = axis > 0 ? snipEmu / axis * 100.0 : adj / 100000.0 * 100.0;
                return Math.Clamp(pct, 0, 50);
            }
            var a1 = ReadAdj(0, 16667);
            var hx = AdjPct(a1, true);   // horizontal inset %
            var vy = AdjPct(a1, false);  // vertical inset %
            string P(double d) => d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            switch (preset)
            {
                case "snip1Rect":
                    // top-right corner snipped
                    return $"clip-path:polygon(0 0,{P(100 - hx)}% 0,100% {P(vy)}%,100% 100%,0 100%)";
                case "snip2SameRect":
                {
                    // top-left + top-right snipped (adj1 = top corners)
                    return $"clip-path:polygon({P(hx)}% 0,{P(100 - hx)}% 0,100% {P(vy)}%,100% 100%,0 100%,0 {P(vy)}%)";
                }
                case "snip2DiagRect":
                {
                    // top-left + bottom-right snipped (diagonal)
                    var a2 = ReadAdj(1, 0);
                    var hx2 = AdjPct(a2 == 0 ? a1 : a2, true);
                    var vy2 = AdjPct(a2 == 0 ? a1 : a2, false);
                    return $"clip-path:polygon({P(hx)}% 0,100% 0,100% {P(100 - vy2)}%,{P(100 - hx2)}% 100%,0 100%,0 {P(vy)}%)";
                }
            }
        }

        return preset switch
        {
            // Rectangles
            "rect" => "",

            // Ellipses
            "ellipse" => "border-radius:50%",

            // Triangles
            "triangle" or "isosTriangle" => "clip-path:polygon(50% 0,100% 100%,0 100%)",
            "rtTriangle" => "clip-path:polygon(0 0,100% 100%,0 100%)",

            // Diamonds and parallelograms
            "diamond" => "clip-path:polygon(50% 0,100% 50%,50% 100%,0 50%)",
            "parallelogram" => "clip-path:polygon(15% 0,100% 0,85% 100%,0 100%)",
            "trapezoid" => "clip-path:polygon(20% 0,80% 0,100% 100%,0 100%)",

            // Polygons
            "pentagon" => "clip-path:polygon(50% 0,100% 38%,82% 100%,18% 100%,0 38%)",
            "hexagon" => "clip-path:polygon(25% 0,75% 0,100% 50%,75% 100%,25% 100%,0 50%)",
            "heptagon" => "clip-path:polygon(50% 0,90% 20%,100% 60%,75% 100%,25% 100%,0 60%,10% 20%)",
            "octagon" => "clip-path:polygon(29% 0,71% 0,100% 29%,100% 71%,71% 100%,29% 100%,0 71%,0 29%)",
            "decagon" => "clip-path:polygon(35% 0,65% 0,90% 12%,100% 38%,100% 62%,90% 88%,65% 100%,35% 100%,10% 88%,0 62%,0 38%,10% 12%)",
            "dodecagon" => "clip-path:polygon(37% 0,63% 0,87% 13%,100% 37%,100% 63%,87% 87%,63% 100%,37% 100%,13% 87%,0 63%,0 37%,13% 13%)",

            // Stars
            "star4" => "clip-path:polygon(50% 0,62% 38%,100% 50%,62% 62%,50% 100%,38% 62%,0 50%,38% 38%)",
            "star5" => "clip-path:polygon(50% 0,61% 35%,98% 35%,68% 57%,79% 91%,50% 70%,21% 91%,32% 57%,2% 35%,39% 35%)",
            "star6" => "clip-path:polygon(50% 0,63% 25%,100% 25%,75% 50%,100% 75%,63% 75%,50% 100%,37% 75%,0 75%,25% 50%,0 25%,37% 25%)",
            "star8" => "clip-path:polygon(50% 0,62% 19%,85% 15%,81% 38%,100% 50%,81% 62%,85% 85%,62% 81%,50% 100%,38% 81%,15% 85%,19% 62%,0 50%,19% 38%,15% 15%,38% 19%)",
            "star10" => "clip-path:polygon(50% 0,59% 19%,79% 5%,74% 27%,97% 25%,84% 43%,100% 50%,84% 57%,97% 75%,74% 73%,79% 95%,59% 81%,50% 100%,41% 81%,21% 95%,26% 73%,3% 75%,16% 57%,0 50%,16% 43%,3% 25%,26% 27%,21% 5%,41% 19%)",
            "star12" => "clip-path:polygon(50% 0,57% 15%,75% 7%,71% 25%,93% 25%,84% 42%,100% 50%,84% 58%,93% 75%,71% 75%,75% 93%,57% 85%,50% 100%,43% 85%,25% 93%,29% 75%,7% 75%,16% 58%,0 50%,16% 42%,7% 25%,29% 25%,25% 7%,43% 15%)",

            // Arrows
            "rightArrow" => "clip-path:polygon(0 20%,70% 20%,70% 0,100% 50%,70% 100%,70% 80%,0 80%)",
            "leftRightArrow" => "clip-path:polygon(0 50%,15% 20%,15% 35%,85% 35%,85% 20%,100% 50%,85% 80%,85% 65%,15% 65%,15% 80%)",
            "upDownArrow" => "clip-path:polygon(50% 0,80% 15%,65% 15%,65% 85%,80% 85%,50% 100%,20% 85%,35% 85%,35% 15%,20% 15%)",
            "notchedRightArrow" => "clip-path:polygon(0 20%,70% 20%,70% 0,100% 50%,70% 100%,70% 80%,0 80%,10% 50%)",
            "bentArrow" => "clip-path:polygon(0 20%,60% 20%,60% 0,100% 35%,60% 70%,60% 50%,20% 50%,20% 100%,0 100%)",
            "chevron" => "clip-path:polygon(0 0,80% 0,100% 50%,80% 100%,0 100%,20% 50%)",
            "homePlate" => "clip-path:polygon(0 0,85% 0,100% 50%,85% 100%,0 100%)",
            "stripedRightArrow" => "clip-path:polygon(10% 20%,12% 20%,12% 80%,10% 80%,10% 20%,15% 20%,70% 20%,70% 0,100% 50%,70% 100%,70% 80%,15% 80%)",

            // Callouts — rectangle/rounded-rect/ellipse body with a wedge tail pointing down-left
            "wedgeRectCallout" => "clip-path:polygon(0 0,100% 0,100% 75%,40% 75%,10% 100%,30% 75%,0 75%)",
            "wedgeRoundRectCallout" => "clip-path:polygon(8% 0%,92% 0%,95% 1%,98% 3%,100% 5%,100% 8%,100% 67%,100% 70%,98% 73%,95% 75%,92% 75%,40% 75%,10% 100%,30% 75%,8% 75%,5% 75%,2% 73%,1% 70%,0% 67%,0% 8%,0% 5%,1% 3%,2% 1%,5% 0%)",
            "wedgeEllipseCallout" => "clip-path:polygon(50% 0%,60% 1%,70% 3%,78% 7%,85% 13%,90% 20%,94% 28%,97% 37%,98% 47%,97% 56%,95% 64%,91% 71%,40% 75%,10% 100%,35% 72%,27% 76%,19% 72%,12% 65%,7% 57%,3% 48%,2% 38%,3% 29%,6% 20%,11% 13%,18% 7%,26% 3%,35% 1%,42% 0%)",

            // Crosses and plus — arm width scales with aspect ratio
            "plus" or "cross" when widthEmu > 0 && heightEmu > 0 => PlusPolygon(widthEmu, heightEmu),
            "plus" or "cross" => "clip-path:polygon(33% 0,67% 0,67% 33%,100% 33%,100% 67%,67% 67%,67% 100%,33% 100%,33% 67%,0 67%,0 33%,33% 33%)",

            // Heart (polygon approximation)
            "heart" => "clip-path:polygon(50% 18%, 53% 12%, 57% 6%, 62% 2%, 68% 0%, 75% 0%, 82% 0%, 89% 3%, 94% 8%, 98% 14%, 100% 21%, 100% 28%, 99% 35%, 95% 43%, 90% 51%, 84% 59%, 77% 67%, 69% 75%, 60% 84%, 50% 100%, 40% 84%, 31% 75%, 23% 67%, 16% 59%, 10% 51%, 5% 43%, 1% 35%, 0% 28%, 0% 21%, 2% 14%, 6% 8%, 11% 3%, 18% 0%, 25% 0%, 32% 0%, 38% 2%, 43% 6%, 47% 12%)",

            // Cloud — SVG-based clip-path for realistic cloud bumps
            "cloud" => "clip-path:polygon(25% 80%,18% 80%,12% 78%,7% 74%,5% 69%,4% 64%,5% 60%,3% 56%,1% 51%,1% 47%,3% 42%,7% 38%,11% 36%,15% 35%,14% 29%,14% 23%,17% 19%,21% 16%,26% 15%,30% 15%,31% 10%,34% 6%,38% 3%,43% 1%,48% 0%,55% 5%,61% 2%,67% 1%,72% 2%,76% 6%,78% 15%,82% 12%,87% 11%,91% 13%,94% 17%,95% 22%,95% 30%,97% 33%,99% 37%,100% 42%,99% 47%,97% 52%,93% 55%,90% 55%,93% 59%,96% 64%,97% 68%,96% 73%,92% 76%,88% 78%,85% 78%,84% 82%,82% 87%,78% 90%,73% 92%,68% 92%,63% 90%,60% 90%,56% 93%,51% 96%,46% 97%,41% 96%,38% 93%,35% 90%)",
            "cloudCallout" => "clip-path:polygon(25% 80%,18% 80%,12% 78%,7% 74%,5% 69%,4% 64%,5% 60%,3% 56%,1% 51%,1% 47%,3% 42%,7% 38%,11% 36%,15% 35%,14% 29%,14% 23%,17% 19%,21% 16%,26% 15%,30% 15%,31% 10%,34% 6%,38% 3%,43% 1%,48% 0%,55% 5%,61% 2%,67% 1%,72% 2%,76% 6%,78% 15%,82% 12%,87% 11%,91% 13%,94% 17%,95% 22%,95% 30%,97% 33%,99% 37%,100% 42%,99% 47%,97% 52%,93% 55%,90% 55%,93% 59%,96% 64%,97% 68%,96% 73%,92% 76%,88% 78%,85% 78%,84% 82%,82% 87%,78% 90%,73% 92%,68% 92%,63% 90%,60% 90%,56% 93%,51% 96%,46% 97%,41% 96%,38% 93%,35% 90%)",

            // Smiley (circle)
            "smileyFace" or "smiley" => "border-radius:50%",

            // Sun — circle with triangular rays
            "sun" => "clip-path:polygon(50% 0,56% 15%,70% 3%,66% 19%,85% 15%,74% 27%,93% 30%,80% 38%,97% 45%,82% 48%,97% 55%,80% 62%,93% 70%,74% 73%,85% 85%,66% 81%,70% 97%,56% 85%,50% 100%,44% 85%,30% 97%,34% 81%,15% 85%,26% 73%,7% 70%,20% 62%,3% 55%,18% 48%,3% 45%,20% 38%,7% 30%,26% 27%,15% 15%,34% 19%,30% 3%,44% 15%)",

            // Moon (crescent) — outer arc minus inner arc
            "moon" => "clip-path:polygon(75% 0%,65% 5%,56% 12%,49% 21%,44% 31%,42% 42%,42% 50%,42% 58%,44% 69%,49% 79%,56% 88%,65% 95%,75% 100%,63% 100%,50% 98%,38% 93%,27% 86%,18% 77%,10% 66%,5% 54%,2% 42%,2% 30%,5% 18%,10% 9%,18% 3%,27% 0%,38% 0%,50% 0%,63% 0%)",

            // Gear (polygon approximation of 6-tooth gear)
            "gear6" => "clip-path:polygon(50% 0,61% 10%,75% 3%,80% 18%,97% 25%,88% 38%,100% 50%,88% 62%,97% 75%,80% 82%,75% 97%,61% 90%,50% 100%,39% 90%,25% 97%,20% 82%,3% 75%,12% 62%,0 50%,12% 38%,3% 25%,20% 18%,25% 3%,39% 10%)",
            "gear9" => "clip-path:polygon(50% 0,56% 8%,65% 2%,68% 12%,78% 9%,78% 20%,88% 20%,85% 30%,95% 35%,90% 44%,100% 50%,90% 56%,95% 65%,85% 70%,88% 80%,78% 80%,78% 91%,68% 88%,65% 98%,56% 92%,50% 100%,44% 92%,35% 98%,32% 88%,22% 91%,22% 80%,12% 80%,15% 70%,5% 65%,10% 56%,0 50%,10% 44%,5% 35%,15% 30%,12% 20%,22% 20%,22% 9%,32% 12%,35% 2%,44% 8%)",

            // 3D-like shapes (rendered flat)
            "cube" => "clip-path:polygon(10% 0,100% 0,100% 85%,90% 100%,0 100%,0 15%)",
            "can" or "cylinder" => "border-radius:50%/10%",
            "bevel" => "border:3px outset currentColor",
            "foldedCorner" => "clip-path:polygon(0 0,85% 0,100% 15%,100% 100%,0 100%)",
            "lightningBolt" => "clip-path:polygon(35% 0,55% 35%,100% 30%,45% 55%,80% 100%,25% 60%,0 80%,30% 45%)",

            // Misc shapes
            "frame" => "clip-path:polygon(0 0,100% 0,100% 100%,0 100%,0 12%,12% 12%,12% 88%,88% 88%,88% 12%,0 12%)",
            "donut" => "border-radius:50%;-webkit-mask-image:radial-gradient(circle,transparent 38%,black 38%);mask-image:radial-gradient(circle,transparent 38%,black 38%)",
            "noSmoking" => "border-radius:50%",
            "halfFrame" => "clip-path:polygon(0 0,100% 0,100% 15%,15% 15%,15% 100%,0 100%)",
            "corner" => "clip-path:polygon(0 0,50% 0,50% 50%,100% 50%,100% 100%,0 100%)",
            "pie" or "arc" => "border-radius:50%",

            // Ribbons/banners
            "ribbon" or "ribbon2" or "wave" or "doubleWave" => "",
            "horizontalScroll" or "verticalScroll" => "border-radius:4px",

            // Flowchart
            "flowChartProcess" => "",
            "flowChartAlternateProcess" => "border-radius:8px",
            "flowChartDecision" => "clip-path:polygon(50% 0,100% 50%,50% 100%,0 50%)",
            "flowChartInputOutput" or "flowChartData" => "clip-path:polygon(15% 0,100% 0,85% 100%,0 100%)",
            "flowChartPredefinedProcess" => "border-left:3px double currentColor;border-right:3px double currentColor",
            "flowChartDocument" => "",
            "flowChartMultidocument" => "",
            "flowChartTerminator" => "border-radius:50%/100%",
            "flowChartPreparation" => "clip-path:polygon(17% 0,83% 0,100% 50%,83% 100%,17% 100%,0 50%)",
            "flowChartManualInput" => "clip-path:polygon(0 15%,100% 0,100% 100%,0 100%)",
            "flowChartManualOperation" => "clip-path:polygon(0 0,100% 0,85% 100%,15% 100%)",
            "flowChartMerge" => "clip-path:polygon(0 0,100% 0,50% 100%)",
            "flowChartExtract" => "clip-path:polygon(50% 0,100% 100%,0 100%)",
            "flowChartSort" => "clip-path:polygon(50% 0,100% 50%,50% 100%,0 50%)",
            "flowChartCollate" => "clip-path:polygon(0 0,100% 0,50% 50%,100% 100%,0 100%,50% 50%)",
            "flowChartDelay" => "border-radius:0 50% 50% 0",
            "flowChartDisplay" => "clip-path:polygon(0 50%,15% 0,85% 0,100% 50%,85% 100%,15% 100%)",
            "flowChartPunchedCard" => "clip-path:polygon(15% 0,100% 0,100% 100%,0 100%,0 15%)",
            "flowChartPunchedTape" => "",
            "flowChartOnlineStorage" => "border-radius:50% 0 0 50%",
            "flowChartOfflineStorage" => "clip-path:polygon(10% 0,90% 0,50% 100%)",
            "flowChartMagneticDisk" => "border-radius:50%/20%",
            "flowChartConnector" or "flowChartOffpageConnector" => "border-radius:50%",

            // Block arrows (curved)
            "curvedRightArrow" => "clip-path:polygon(0% 85%,0% 55%,2% 40%,6% 28%,12% 19%,20% 13%,30% 10%,70% 10%,70% 0%,100% 20%,70% 40%,70% 30%,40% 30%,32% 33%,26% 38%,22% 45%,20% 55%,20% 85%)",
            "curvedLeftArrow" => "clip-path:polygon(100% 85%,100% 55%,98% 40%,94% 28%,88% 19%,80% 13%,70% 10%,30% 10%,30% 0%,0% 20%,30% 40%,30% 30%,60% 30%,68% 33%,74% 38%,78% 45%,80% 55%,80% 85%)",
            "curvedUpArrow" => "clip-path:polygon(85% 100%,55% 100%,40% 98%,28% 94%,19% 88%,13% 80%,10% 70%,10% 30%,0% 30%,20% 0%,40% 30%,30% 30%,30% 60%,33% 68%,38% 74%,45% 78%,55% 80%,85% 80%)",
            "curvedDownArrow" => "clip-path:polygon(85% 0%,55% 0%,40% 2%,28% 6%,19% 12%,13% 20%,10% 30%,10% 70%,0% 70%,20% 100%,40% 70%,30% 70%,30% 40%,33% 32%,38% 26%,45% 22%,55% 20%,85% 20%)",
            "circularArrow" => "border-radius:50%",

            // Math
            "mathPlus" => "clip-path:polygon(33% 0,67% 0,67% 33%,100% 33%,100% 67%,67% 67%,67% 100%,33% 100%,33% 67%,0 67%,0 33%,33% 33%)",
            "mathMinus" => "clip-path:polygon(0 35%,100% 35%,100% 65%,0 65%)",
            "mathMultiply" => "clip-path:polygon(20% 0,50% 30%,80% 0,100% 20%,70% 50%,100% 80%,80% 100%,50% 70%,20% 100%,0 80%,30% 50%,0 20%)",
            "mathDivide" => "",
            "mathEqual" => "clip-path:polygon(0 25%,100% 25%,100% 40%,0 40%,0 60%,100% 60%,100% 75%,0 75%)",
            "mathNotEqual" => "",

            // Default: render as rectangle
            _ => ""
        };
    }

    // ==================== Color Resolution ====================

    private static string? ResolveFillColor(Drawing.SolidFill? solidFill, Dictionary<string, string> themeColors)
    {
        if (solidFill == null) return null;

        var rgb = solidFill.GetFirstChild<Drawing.RgbColorModelHex>()?.Val?.Value;
        if (rgb != null && rgb.Length >= 6 && rgb[..6].All(char.IsAsciiHexDigit))
        {
            var hexPart = rgb[..6]; // Only use first 6 hex chars, ignore any trailing data
            var rgbEl = solidFill.GetFirstChild<Drawing.RgbColorModelHex>()!;
            // Apply lumMod/lumOff/tint/shade if present (same transforms as schemeClr).
            var transformed = ApplyRgbColorTransforms(hexPart, rgbEl);
            hexPart = transformed.StartsWith('#') ? transformed[1..] : transformed;
            var alpha = rgbEl.GetFirstChild<Drawing.Alpha>()?.Val?.Value;
            if (alpha.HasValue && alpha.Value < 100000)
            {
                var (r, g, b) = ColorMath.HexToRgb(hexPart);
                return $"rgba({r},{g},{b},{alpha.Value / 100000.0:0.##})";
            }
            return $"#{hexPart}";
        }

        var schemeColor = solidFill.GetFirstChild<Drawing.SchemeColor>();
        if (schemeColor?.Val?.HasValue == true)
        {
            var schemeName = schemeColor.Val!.InnerText;
            if (schemeName != null && themeColors.TryGetValue(schemeName, out var themeHex))
            {
                // Check for lumMod/lumOff/tint/shade transforms
                var color = ApplyColorTransforms(themeHex, schemeColor);
                return color;
            }
            return null; // Unknown scheme color
        }

        return null;
    }

    private static string ApplyColorTransforms(string hex, Drawing.SchemeColor schemeColor)
        => ApplyColorTransforms(hex, (OpenXmlElement)schemeColor);

    /// <summary>
    /// Apply lumMod/lumOff/tint/shade/alpha child transforms on any color element
    /// (schemeClr or srgbClr). Reads by local name so it works for both strongly-typed
    /// children (round-tripped from disk) AND OpenXmlUnknownElement children (built
    /// in-memory by DrawingColorBuilder, which appends transforms as unknown elements).
    /// </summary>
    private static string ApplyColorTransforms(string hex, OpenXmlElement colorEl)
    {
        return ColorMath.ApplyTransforms(hex,
            tint: ReadTransformVal(colorEl, "tint"),
            shade: ReadTransformVal(colorEl, "shade"),
            lumMod: ReadTransformVal(colorEl, "lumMod"),
            lumOff: ReadTransformVal(colorEl, "lumOff"),
            alpha: ReadTransformVal(colorEl, "alpha"));
    }

    private static int? ReadTransformVal(OpenXmlElement colorEl, string localName)
    {
        foreach (var child in colorEl.ChildElements)
        {
            if (!child.LocalName.Equals(localName, StringComparison.Ordinal)) continue;
            var raw = child.GetAttributes().FirstOrDefault(a => a.LocalName == "val").Value;
            if (int.TryParse(raw, out var v)) return v;
        }
        return null;
    }

    /// <summary>
    /// Build a map of scheme color names to hex values from the presentation theme.
    /// </summary>
    private Dictionary<string, string> ResolveThemeColorMap()
    {
        // Use the same theme-part resolution as Get/Set (GetThemePart prefers the
        // presentationPart's own theme, then the first master's) so a Set hlink/
        // accent color is reflected here instead of reading a stale master theme.
        var colorScheme = GetColorScheme()
            ?? _doc.PresentationPart?.SlideMasterParts?.FirstOrDefault()
                ?.ThemePart?.Theme?.ThemeElements?.ColorScheme;
        return ThemeColorResolver.BuildColorMap(colorScheme, includePptAliases: true);
    }

    /// <summary>
    /// R9-2: apply a slide's &lt;p:clrMapOvr&gt;&lt;p:overrideClrMapping&gt; on top of
    /// the global theme color map. The override remaps the 12 color slots
    /// (e.g. accent1="accent2" makes the accent1 slot resolve to accent2's hex).
    /// Returns the base map unchanged when the slide carries no override mapping.
    /// </summary>
    private static Dictionary<string, string> ApplySlideColorMapOverride(
        SlidePart slidePart, Dictionary<string, string> baseMap)
    {
        var clrMapOvr = slidePart.Slide?.GetFirstChild<ColorMapOverride>();
        // The overrideClrMapping element may be authored under either the a: or p:
        // namespace (real PowerPoint emits a:overrideClrMapping); read attributes
        // generically by local name to be namespace-agnostic. A masterClrMapping
        // child (no attributes) means "inherit" — nothing to remap.
        var ovr = clrMapOvr?.ChildElements
            .FirstOrDefault(e => e.LocalName == "overrideClrMapping");
        if (ovr == null || !ovr.HasAttributes) return baseMap;

        var remapped = new Dictionary<string, string>(baseMap, StringComparer.OrdinalIgnoreCase);
        // Each attribute (bg1/tx1/bg2/tx2/accent1..6/hlink/folHlink) names a slot
        // and its value is the scheme token it now points at (e.g. accent1="accent2"
        // makes the accent1 slot resolve to accent2's hex).
        foreach (var attr in ovr.GetAttributes())
        {
            var slot = attr.LocalName;
            var token = attr.Value;
            if (string.IsNullOrEmpty(token)) continue;
            if (baseMap.TryGetValue(token!, out var hex))
                remapped[slot] = hex;
        }
        return remapped;
    }

    // ==================== Image Helpers ====================

    private static string? BlipToDataUri(Drawing.BlipFill blipFill, OpenXmlPart part)
    {
        var blip = blipFill.GetFirstChild<Drawing.Blip>();
        if (blip == null) return null;
        // R4-1: an SVG picture stores BOTH a raster fallback (blip.Embed → 1x1
        // PNG) AND the real vector via the asvg:svgBlip extension. Prefer the SVG
        // rel-id so the preview shows the actual artwork, not the 1x1 fallback.
        var svgRelId = OfficeCli.Core.SvgImageHelper.GetSvgRelId(blip);
        if (!string.IsNullOrEmpty(svgRelId))
        {
            try
            {
                var svgPart = part.GetPartById(svgRelId!);
                using var stream = svgPart.GetStream();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return $"data:image/svg+xml;base64,{Convert.ToBase64String(ms.ToArray())}";
            }
            catch { /* unresolved SVG rel — fall back to the raster embed */ }
        }
        if (blip.Embed?.HasValue != true) return null;
        return HtmlPreviewHelper.PartToDataUri(part, blip.Embed.Value!);
    }

    // ==================== Utility ====================

    // Unit conversions moved to shared Units class (Core/Units.cs).

    // CONSISTENCY(html-encode): shared plain entity-encoder lives in Core/HtmlPreviewHelper.
    private static string HtmlEncode(string text) => HtmlPreviewHelper.HtmlEncode(text);

    /// <summary>
    /// Sanitize a value for use inside a CSS style attribute.
    /// Strips characters that could break out of the style context.
    /// </summary>
    private static readonly string[] CjkFallbacks = { "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC", "Hiragino Sans GB" };

    private static string CssFontFamilyWithFallback(string font)
    {
        var sanitized = CssSanitize(font);
        var fallbacks = string.Join(",", CjkFallbacks
            .Where(f => !f.Equals(font, StringComparison.OrdinalIgnoreCase))
            .Select(f => $"'{f}'"));
        return $"font-family:'{sanitized}',{fallbacks},sans-serif";
    }

    /// <summary>
    /// Returns true if the hex color is dark (low luminance).
    /// </summary>
    private static bool IsColorDark(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return false;
        var (r, g, b) = ColorMath.HexToRgb(hex);
        // Relative luminance approximation
        return (r * 0.299 + g * 0.587 + b * 0.114) < 128;
    }

    private static string CssSanitize(string value)
    {
        // Remove characters that could escape the style attribute or inject HTML
        return value.Replace("\"", "").Replace("'", "").Replace("<", "").Replace(">", "")
            .Replace(";", "").Replace("{", "").Replace("}", "");
    }

    /// <summary>
    /// Sanitize a color value for safe embedding in CSS.
    /// Only allows hex colors (#RRGGBB), rgb/rgba() functions, and named CSS colors.
    /// </summary>
    private static string CssSanitizeColor(string color)
    {
        if (string.IsNullOrEmpty(color)) return "transparent";
        // Allow: #hex, rgb(), rgba(), named colors (alphanumeric only)
        var trimmed = color.Trim();
        if (trimmed.StartsWith('#') && trimmed.Length <= 9 && trimmed[1..].All(char.IsAsciiHexDigit))
            return trimmed;
        if (trimmed.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return CssSanitize(trimmed);
        if (trimmed.All(c => char.IsLetterOrDigit(c) || c == '.'))
            return trimmed;
        return "transparent";
    }

    /// <summary>
    /// Sanitize a MIME content type for safe embedding in a data URI.
    /// </summary>
    private static string SanitizeContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return "image/png";
        // Only allow alphanumeric, '/', '+', '-', '.'
        if (contentType.All(c => char.IsLetterOrDigit(c) || c is '/' or '+' or '-' or '.'))
            return contentType;
        return "image/png";
    }
}
