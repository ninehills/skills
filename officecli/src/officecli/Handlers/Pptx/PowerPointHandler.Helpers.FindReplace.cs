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
    /// Find and replace text across all slides. Returns the number of replacements made.
    /// </summary>
    // ==================== Find / Format / Replace ====================

    /// <summary>
    /// Build a flat list of (Run, Text, charStart, charEnd) spans for a PPT paragraph.
    /// </summary>
    private static List<(Drawing.Run Run, Drawing.Text TextElement, int Start, int End)> BuildPptRunTexts(Drawing.Paragraph para)
    {
        var runTexts = new List<(Drawing.Run Run, Drawing.Text TextElement, int Start, int End)>();
        int pos = 0;
        foreach (var run in para.Descendants<Drawing.Run>())
        {
            var text = run.GetFirstChild<Drawing.Text>();
            var len = text?.Text?.Length ?? 0;
            if (len > 0)
                runTexts.Add((run, text!, pos, pos + len));
            pos += len;
        }
        return runTexts;
    }

    /// <summary>
    /// Split a PPT run at a character offset. Returns the new right-side run.
    /// RunProperties are deep-cloned.
    /// </summary>
    private static Drawing.Run SplitPptRunAtOffset(Drawing.Run run, int charOffset)
    {
        var text = run.GetFirstChild<Drawing.Text>();
        if (text?.Text == null || charOffset <= 0 || charOffset >= text.Text.Length)
            return run;

        var leftText = text.Text[..charOffset];
        var rightText = text.Text[charOffset..];

        // Clone the run for the right side
        var rightRun = (Drawing.Run)run.CloneNode(true);

        // Set text
        text.Text = leftText;
        var rightTextElem = rightRun.GetFirstChild<Drawing.Text>();
        if (rightTextElem != null) rightTextElem.Text = rightText;

        // Insert after original
        run.InsertAfterSelf(rightRun);
        return rightRun;
    }

    /// <summary>
    /// Split runs in a PPT paragraph so that [charStart, charEnd) is covered by dedicated runs.
    /// Returns the runs covering that range.
    /// </summary>
    private static List<Drawing.Run> SplitPptRunsAtRange(Drawing.Paragraph para, int charStart, int charEnd)
    {
        // Split at charEnd first
        var runTexts = BuildPptRunTexts(para);
        foreach (var rt in runTexts)
        {
            if (charEnd > rt.Start && charEnd < rt.End)
            {
                SplitPptRunAtOffset(rt.Run, charEnd - rt.Start);
                break;
            }
        }

        // Rebuild, then split at charStart
        runTexts = BuildPptRunTexts(para);
        foreach (var rt in runTexts)
        {
            if (charStart > rt.Start && charStart < rt.End)
            {
                SplitPptRunAtOffset(rt.Run, charStart - rt.Start);
                break;
            }
        }

        // Collect runs covering [charStart, charEnd)
        runTexts = BuildPptRunTexts(para);
        var result = new List<Drawing.Run>();
        foreach (var rt in runTexts)
        {
            if (rt.Start >= charStart && rt.End <= charEnd)
                result.Add(rt.Run);
        }
        return result;
    }

    /// <summary>
    /// Process find in a single PPT paragraph: replace text and/or apply formatting.
    /// </summary>
    private static int ProcessFindInPptParagraph(
        Drawing.Paragraph para,
        string pattern,
        bool isRegex,
        string? replace,
        Dictionary<string, string>? formatProps,
        Shape? shape = null,
        int? runIndexFilter = null)
    {
        var runTexts = BuildPptRunTexts(para);
        if (runTexts.Count == 0) return 0;

        // BUG-TESTER+FUZZER R32: when scope is /r[K], restrict find to that
        // run's text range only. Out-of-bound was already rejected upstream.
        int scanStart = 0;
        int scanEnd = runTexts[^1].End;
        if (runIndexFilter.HasValue)
        {
            if (runIndexFilter.Value < 1 || runIndexFilter.Value > runTexts.Count)
                return 0;
            scanStart = runTexts[runIndexFilter.Value - 1].Start;
            scanEnd = runTexts[runIndexFilter.Value - 1].End;
        }

        var fullText = string.Concat(runTexts.Select(rt => rt.TextElement.Text));
        // CONSISTENCY(regex-backref-expand): mirror Word ProcessFindInParagraph.
        // BUG-TESTER+FUZZER R31: wrap with try/catch so RegexMatchTimeoutException is
        // converted to ArgumentException, and avoid a second Regex.Matches call by
        // deriving ranges from the same Match list.
        List<System.Text.RegularExpressions.Match>? matchObjs = null;
        List<(int Start, int Length)> matches;
        if (isRegex)
        {
            try
            {
                matchObjs = System.Text.RegularExpressions.Regex.Matches(
                        fullText,
                        pattern,
                        System.Text.RegularExpressions.RegexOptions.None,
                        FindHelpers.RegexMatchTimeout)
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Where(m => m.Length > 0)
                    .ToList();
            }
            catch (System.Text.RegularExpressions.RegexParseException ex)
            {
                throw new ArgumentException($"Invalid regex pattern '{pattern}': {ex.Message}", ex);
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException ex)
            {
                throw new ArgumentException(
                    $"Regex pattern '{pattern}' exceeded {FindHelpers.RegexMatchTimeout.TotalSeconds}s match timeout (catastrophic backtracking?)",
                    ex);
            }
            matches = matchObjs.Select(m => (m.Index, m.Length)).ToList();
        }
        else
        {
            matches = FindHelpers.FindMatchRanges(fullText, pattern, isRegex);
        }

        // Apply run-scope filter (R32): keep only matches fully contained in the run.
        if (runIndexFilter.HasValue)
        {
            var keepIdx = new HashSet<int>();
            for (int k = 0; k < matches.Count; k++)
            {
                var (s, l) = matches[k];
                if (s >= scanStart && s + l <= scanEnd)
                    keepIdx.Add(k);
            }
            matches = matches.Where((_, k) => keepIdx.Contains(k)).ToList();
            if (matchObjs != null)
                matchObjs = matchObjs.Where((_, k) => keepIdx.Contains(k)).ToList();
        }

        if (matches.Count == 0) return 0;

        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var (matchStart, matchLen) = matches[i];
            var matchEnd = matchStart + matchLen;

            if (replace != null)
            {
                // Expand backrefs via Match.Result so lookarounds keep their context.
                string effectiveReplace = replace;
                if (isRegex && matchObjs != null && i < matchObjs.Count)
                {
                    effectiveReplace = matchObjs[i].Result(replace);
                }

                // Replace text in affected runs
                var currentRunTexts = BuildPptRunTexts(para);
                bool first = true;
                foreach (var rt in currentRunTexts)
                {
                    if (rt.End <= matchStart || rt.Start >= matchEnd)
                        continue;

                    var textStr = rt.TextElement.Text ?? "";
                    var localStart = Math.Max(0, matchStart - rt.Start);
                    var localEnd = Math.Min(textStr.Length, matchEnd - rt.Start);

                    if (first)
                    {
                        rt.TextElement.Text = textStr[..localStart] + effectiveReplace + textStr[localEnd..];
                        first = false;
                    }
                    else
                    {
                        rt.TextElement.Text = textStr[..Math.Max(0, matchStart - rt.Start)] + textStr[localEnd..];
                    }
                }

                // BUG-TESTER fuzz-1 (PPTX mirror): drop orphan empty <a:r> runs left
                // by cross-run replace. Only remove runs with empty <a:t> and no other
                // semantic children (RunProperties alone is not semantic content).
                var emptyRunsToRemove = new List<Drawing.Run>();
                foreach (var run in para.Descendants<Drawing.Run>())
                {
                    bool hasContent = false;
                    bool hasEmptyText = false;
                    foreach (var child in run.ChildElements)
                    {
                        if (child is Drawing.RunProperties)
                            continue;
                        if (child is Drawing.Text t)
                        {
                            if (string.IsNullOrEmpty(t.Text))
                                hasEmptyText = true;
                            else
                                hasContent = true;
                        }
                        else
                        {
                            hasContent = true;
                        }
                    }
                    if (hasEmptyText && !hasContent)
                        emptyRunsToRemove.Add(run);
                }
                foreach (var run in emptyRunsToRemove)
                    run.Remove();

                if (formatProps != null && formatProps.Count > 0 && effectiveReplace.Length > 0)
                {
                    var replacedEnd = matchStart + effectiveReplace.Length;
                    var targetRuns = SplitPptRunsAtRange(para, matchStart, replacedEnd);
                    foreach (var run in targetRuns)
                        foreach (var (key, value) in formatProps)
                            ApplyPptRunFormatting(run, key, value, shape);
                }
            }
            else if (formatProps != null && formatProps.Count > 0)
            {
                var targetRuns = SplitPptRunsAtRange(para, matchStart, matchEnd);
                foreach (var run in targetRuns)
                    foreach (var (key, value) in formatProps)
                        ApplyPptRunFormatting(run, key, value, shape);
            }
        }

        return matches.Count;
    }

    /// <summary>
    /// Unified find across all paragraphs in the resolved scope.
    /// </summary>
    private int ProcessPptFind(string path, string findValue, string? replace, Dictionary<string, string> formatProps)
    {
        var (pattern, isRegex) = FindHelpers.ParseFindPattern(findValue);
        if (string.IsNullOrEmpty(pattern) && !isRegex) return 0;

        int totalCount = 0;

        if (path is "/" or "" or "/presentation")
        {
            // All slides
            foreach (var slidePart in _doc.PresentationPart?.SlideParts ?? Enumerable.Empty<SlidePart>())
            {
                var slide = slidePart.Slide;
                if (slide == null) continue;
                foreach (var para in slide.Descendants<Drawing.Paragraph>())
                    totalCount += ProcessFindInPptParagraph(para, pattern, isRegex, replace,
                        formatProps.Count > 0 ? formatProps : null);
                slidePart.Slide!.Save();
                // R21-2: the global root sweep must also cover speaker notes,
                // which live in NotesSlidePart, not the slide shape tree.
                var notesSlide = slidePart.NotesSlidePart?.NotesSlide;
                if (notesSlide != null)
                {
                    foreach (var para in notesSlide.Descendants<Drawing.Paragraph>())
                        totalCount += ProcessFindInPptParagraph(para, pattern, isRegex, replace,
                            formatProps.Count > 0 ? formatProps : null);
                    notesSlide.Save();
                }
            }
        }
        else
        {
            // Path-scoped: resolve to specific paragraphs (and optional run filter)
            var (paragraphs, runIndex) = ResolvePptParagraphsForFindInternal(path);
            Shape? contextShape = null;
            // Try to resolve shape for color context (anchored shape segment only).
            var shapeMatch = Regex.Match(path, @"^/slide\[(\d+)\]/(\w+)\[(\d+)\](?:/|$)");
            if (shapeMatch.Success && shapeMatch.Groups[2].Value is not ("table" or "notes"))
            {
                try
                {
                    var (_, shape) = ResolveShape(int.Parse(shapeMatch.Groups[1].Value), int.Parse(shapeMatch.Groups[3].Value));
                    contextShape = shape;
                }
                catch { }
            }

            foreach (var para in paragraphs)
                totalCount += ProcessFindInPptParagraph(para, pattern, isRegex, replace,
                    formatProps.Count > 0 ? formatProps : null, contextShape, runIndex);

            // Save affected slides
            foreach (var slidePart in _doc.PresentationPart?.SlideParts ?? Enumerable.Empty<SlidePart>())
                slidePart.Slide?.Save();
        }

        return totalCount;
    }

    /// <summary>
    /// Resolve paragraphs from a PPT path for find operations.
    /// BUG-TESTER+FUZZER R32: paths must match exactly (anchored). Out-of-bound
    /// indices and unrecognized PPT paths throw ArgumentException instead of
    /// silently falling back to a wider scope (e.g. all slides).
    /// </summary>
    private List<Drawing.Paragraph> ResolvePptParagraphsForFind(string path)
    {
        var (paragraphs, _) = ResolvePptParagraphsForFindInternal(path);
        return paragraphs;
    }

    /// <summary>
    /// Resolve paragraphs and an optional 1-based run filter from a PPT path.
    /// When the path ends with /r[R] or /run[R], only that run within the
    /// resolved paragraph participates in find/replace.
    /// </summary>
    private (List<Drawing.Paragraph> Paragraphs, int? RunIndex) ResolvePptParagraphsForFindInternal(string path)
    {
        var paragraphs = new List<Drawing.Paragraph>();

        // /slide[N]/notes → paragraphs in notes slide
        var notesMatch = Regex.Match(path, @"^/slide\[(\d+)\]/notes$", RegexOptions.IgnoreCase);
        if (notesMatch.Success)
        {
            var slideIdx = int.Parse(notesMatch.Groups[1].Value);
            var slideParts = GetSlideParts().ToList();
            if (slideIdx < 1 || slideIdx > slideParts.Count)
                throw new ArgumentException($"Slide index out of range: {slideIdx} (have {slideParts.Count} slides)");
            var notesPart = slideParts[slideIdx - 1].NotesSlidePart;
            if (notesPart?.NotesSlide != null)
                paragraphs.AddRange(notesPart.NotesSlide.Descendants<Drawing.Paragraph>());
            return (paragraphs, null);
        }

        // /slide[N]/table[M]/tr[R]/tc[C][/p[P][/r[K]]] → paragraphs in table cell
        var tableCellMatch = Regex.Match(path, @"^/slide\[(\d+)\]/table\[(\d+)\]/tr\[(\d+)\]/tc\[(\d+)\](?:/p(?:aragraph)?\[(\d+)\](?:/r(?:un)?\[(\d+)\])?)?$");
        if (tableCellMatch.Success)
        {
            var slideIdx = int.Parse(tableCellMatch.Groups[1].Value);
            var tableIdx = int.Parse(tableCellMatch.Groups[2].Value);
            var rowIdx = int.Parse(tableCellMatch.Groups[3].Value);
            var colIdx = int.Parse(tableCellMatch.Groups[4].Value);
            int? paraIdx = tableCellMatch.Groups[5].Success ? int.Parse(tableCellMatch.Groups[5].Value) : (int?)null;
            int? runIdx = tableCellMatch.Groups[6].Success ? int.Parse(tableCellMatch.Groups[6].Value) : (int?)null;
            var slideParts = GetSlideParts().ToList();
            if (slideIdx < 1 || slideIdx > slideParts.Count)
                throw new ArgumentException($"Slide index out of range: {slideIdx}");
            var slide = slideParts[slideIdx - 1].Slide;
            var tables = slide?.Descendants<Drawing.Table>().ToList() ?? new List<Drawing.Table>();
            if (tableIdx < 1 || tableIdx > tables.Count)
                throw new ArgumentException($"Table index out of range: {tableIdx}");
            var rows = tables[tableIdx - 1].Elements<Drawing.TableRow>().ToList();
            if (rowIdx < 1 || rowIdx > rows.Count)
                throw new ArgumentException($"Row index out of range: {rowIdx}");
            var cells = rows[rowIdx - 1].Elements<Drawing.TableCell>().ToList();
            if (colIdx < 1 || colIdx > cells.Count)
                throw new ArgumentException($"Column index out of range: {colIdx}");
            var cellParas = cells[colIdx - 1].Descendants<Drawing.Paragraph>().ToList();
            if (paraIdx.HasValue)
            {
                if (paraIdx.Value < 1 || paraIdx.Value > cellParas.Count)
                    throw new ArgumentException($"Paragraph index out of range: {paraIdx.Value} (cell has {cellParas.Count})");
                paragraphs.Add(cellParas[paraIdx.Value - 1]);
            }
            else
            {
                paragraphs.AddRange(cellParas);
            }
            if (runIdx.HasValue)
            {
                var runCount = paragraphs[0].Descendants<Drawing.Run>().Count(r => (r.GetFirstChild<Drawing.Text>()?.Text?.Length ?? 0) > 0);
                if (runIdx.Value < 1 || runIdx.Value > runCount)
                    throw new ArgumentException($"Run index out of range: {runIdx.Value} (paragraph has {runCount} runs)");
            }
            return (paragraphs, runIdx);
        }

        // /slide[N]/table[M] → all paragraphs in table
        var tableMatch = Regex.Match(path, @"^/slide\[(\d+)\]/table\[(\d+)\]$");
        if (tableMatch.Success)
        {
            var slideIdx = int.Parse(tableMatch.Groups[1].Value);
            var tableIdx = int.Parse(tableMatch.Groups[2].Value);
            var slideParts = GetSlideParts().ToList();
            if (slideIdx < 1 || slideIdx > slideParts.Count)
                throw new ArgumentException($"Slide index out of range: {slideIdx}");
            var slide = slideParts[slideIdx - 1].Slide;
            var tables = slide?.Descendants<Drawing.Table>().ToList() ?? new List<Drawing.Table>();
            if (tableIdx < 1 || tableIdx > tables.Count)
                throw new ArgumentException($"Table index out of range: {tableIdx}");
            paragraphs.AddRange(tables[tableIdx - 1].Descendants<Drawing.Paragraph>());
            return (paragraphs, null);
        }

        // /slide[N]/<shape>[M][/p[P][/r[K]]] — shape with optional paragraph/run suffix
        // BUG-TESTER+FUZZER R32: anchored ($) so /p[P] suffix is not silently
        // swallowed as a prefix match against the shape selector.
        var shapeMatch = Regex.Match(path, @"^/slide\[(\d+)\]/(\w+)\[(\d+)\](?:/p(?:aragraph)?\[(\d+)\](?:/r(?:un)?\[(\d+)\])?)?$");
        if (shapeMatch.Success)
        {
            var slideIdx = int.Parse(shapeMatch.Groups[1].Value);
            var shapeKind = shapeMatch.Groups[2].Value;
            // Reject path segments that are not shape-like containers handled here.
            if (shapeKind is "table" or "notes")
                throw new ArgumentException($"Unsupported find scope path: {path}");
            var shapeIdx = int.Parse(shapeMatch.Groups[3].Value);
            int? paraIdx = shapeMatch.Groups[4].Success ? int.Parse(shapeMatch.Groups[4].Value) : (int?)null;
            int? runIdx = shapeMatch.Groups[5].Success ? int.Parse(shapeMatch.Groups[5].Value) : (int?)null;
            Shape shape;
            try
            {
                (_, shape) = ResolveShape(slideIdx, shapeIdx);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot resolve shape at {path}: {ex.Message}", ex);
            }
            if (shape.TextBody == null)
                return (paragraphs, null);
            var shapeParas = shape.TextBody.Elements<Drawing.Paragraph>().ToList();
            if (paraIdx.HasValue)
            {
                if (paraIdx.Value < 1 || paraIdx.Value > shapeParas.Count)
                    throw new ArgumentException($"Paragraph index out of range: {paraIdx.Value} (shape has {shapeParas.Count})");
                paragraphs.Add(shapeParas[paraIdx.Value - 1]);
            }
            else
            {
                paragraphs.AddRange(shapeParas);
            }
            if (runIdx.HasValue)
            {
                var runCount = paragraphs[0].Descendants<Drawing.Run>().Count(r => (r.GetFirstChild<Drawing.Text>()?.Text?.Length ?? 0) > 0);
                if (runIdx.Value < 1 || runIdx.Value > runCount)
                    throw new ArgumentException($"Run index out of range: {runIdx.Value} (paragraph has {runCount} runs)");
            }
            return (paragraphs, runIdx);
        }

        // /slide[N] → all paragraphs in slide
        var slideOnlyMatch = Regex.Match(path, @"^/slide\[(\d+)\]$");
        if (slideOnlyMatch.Success)
        {
            var slideIdx = int.Parse(slideOnlyMatch.Groups[1].Value);
            var slideParts = GetSlideParts().ToList();
            if (slideIdx < 1 || slideIdx > slideParts.Count)
                throw new ArgumentException($"Slide index out of range: {slideIdx}");
            var slide = slideParts[slideIdx - 1].Slide;
            if (slide != null)
                paragraphs.AddRange(slide.Descendants<Drawing.Paragraph>());
            return (paragraphs, null);
        }

        // BUG-FUZZER R32: unrecognized PPT path (e.g. /body) must not silently
        // fall back to all-slides global scope. Reject it.
        throw new ArgumentException($"Unrecognized PPT find scope path: '{path}'. Expected /, /slide[N], /slide[N]/<shape>[M][/p[P][/r[K]]], /slide[N]/notes, or /slide[N]/table[M][/tr[R]/tc[C]].");
    }

    /// <summary>
    /// Build a color element for PPT highlight from a color value.
    /// CONSISTENCY(highlight): delegate to the canonical DrawingML color
    /// builder — the previous NormalizeArgbColor form wrote an 8-digit
    /// AARRGGBB into a:srgbClr@val (ST_HexColorRGB is 6 hex digits), which
    /// PowerPoint and the HTML renderer both misread. BuildColorElement
    /// emits a 6-digit srgbClr (+ a:alpha child when alpha given) or a
    /// schemeClr for theme names.
    /// </summary>
    private static OpenXmlElement BuildSolidFillColor(string value)
        => BuildColorElement(value);

    /// <summary>
    /// Add an element at a text-find position within a PPT paragraph.
    /// For PPT, this only supports inline types (run) — splits the run at the find position.
    /// </summary>
    private string AddPptAtFindPosition(
        string parentPath,
        string type,
        string findValue,
        bool isAfter,
        Dictionary<string, string> properties)
    {
        // find: anchor is only valid for inline types (run/text). Block-level types
        // like shape, row, col, table cannot be inserted at a text-find position —
        // reject early with a clear error instead of silently doing the wrong thing
        // (e.g. inserting a run into a cell paragraph when type=row was requested).
        var normalizedType = type.ToLowerInvariant();
        if (normalizedType is not ("run" or "text"))
            throw new ArgumentException(
                $"find: anchor is not supported for type '{type}'. " +
                $"Use a positional anchor (--before /slide[N]/table[K]/tr[R] or --index N) instead.");

        // Resolve paragraphs from parent path
        var paragraphs = ResolvePptParagraphsForFind(parentPath);
        if (paragraphs.Count == 0)
            throw new ArgumentException($"No paragraphs found at path: {parentPath}");

        // Support regex=true prop as alternative to r"..." prefix.
        // CONSISTENCY(find-regex): mirror of WordHandler.Set.cs:60-61. grep
        // "CONSISTENCY(find-regex)" for every project-wide call site.
        if (properties.TryGetValue("regex", out var regexFlag) && ParseHelpers.IsTruthySafe(regexFlag) && !findValue.StartsWith("r\"") && !findValue.StartsWith("r'"))
            findValue = $"r\"{findValue}\"";

        var (pattern, isRegex) = FindHelpers.ParseFindPattern(findValue);

        // Find first match in any paragraph
        Drawing.Paragraph? targetPara = null;
        int splitPoint = -1;

        foreach (var para in paragraphs)
        {
            var runTexts = BuildPptRunTexts(para);
            if (runTexts.Count == 0) continue;
            var fullText = string.Concat(runTexts.Select(rt => rt.TextElement.Text));
            var matches = FindHelpers.FindMatchRanges(fullText, pattern, isRegex);
            if (matches.Count > 0)
            {
                targetPara = para;
                var (matchStart, matchLen) = matches[0];
                splitPoint = isAfter ? matchStart + matchLen : matchStart;
                break;
            }
        }

        if (targetPara == null)
            throw new ArgumentException($"Text '{findValue}' not found in paragraphs at {parentPath}.");

        // Split run at the position
        var rts = BuildPptRunTexts(targetPara);
        Drawing.Run? insertAfterRun = null;

        foreach (var rt in rts)
        {
            if (splitPoint >= rt.Start && splitPoint <= rt.End)
            {
                if (splitPoint == rt.Start)
                    insertAfterRun = rt.Run.PreviousSibling<Drawing.Run>();
                else if (splitPoint == rt.End)
                    insertAfterRun = rt.Run;
                else
                {
                    SplitPptRunAtOffset(rt.Run, splitPoint - rt.Start);
                    insertAfterRun = rt.Run;
                }
                break;
            }
        }

        // Build and insert new run directly into targetPara (avoids path-based routing
        // that only supports /slide[N]/shape[M] paths, not table cell or other paths).
        var newRun = BuildPptRunFromProperties(properties);

        if (insertAfterRun != null)
            insertAfterRun.InsertAfterSelf(newRun);
        else
        {
            // Insert at beginning: before first run or end-paragraph props
            var firstChild = targetPara.FirstChild;
            if (firstChild != null)
                firstChild.InsertBeforeSelf(newRun);
            else
                targetPara.Append(newRun);
        }

        // Save all slides
        foreach (var slidePart in _doc.PresentationPart?.SlideParts ?? Enumerable.Empty<SlidePart>())
            slidePart.Slide?.Save();

        return parentPath;
    }

    /// <summary>
    /// Build a Drawing.Run from a properties dictionary (text, bold, italic, color, size, font, etc.)
    /// </summary>
    private static Drawing.Run BuildPptRunFromProperties(Dictionary<string, string> properties)
    {
        var newRun = new Drawing.Run();
        var rProps = new Drawing.RunProperties { Language = "en-US" };

        if (properties.TryGetValue("size", out var rSize))
            rProps.FontSize = (int)Math.Round(ParseFontSize(rSize) * 100);
        if (properties.TryGetValue("bold", out var rBold))
            rProps.Bold = IsTruthy(rBold);
        if (properties.TryGetValue("italic", out var rItalic))
            rProps.Italic = IsTruthy(rItalic);
        if (properties.TryGetValue("underline", out var rUnderline))
            rProps.Underline = rUnderline.ToLowerInvariant() switch
            {
                "true" or "single" or "sng" => Drawing.TextUnderlineValues.Single,
                "double" or "dbl" => Drawing.TextUnderlineValues.Double,
                "heavy" => Drawing.TextUnderlineValues.Heavy,
                "dotted" => Drawing.TextUnderlineValues.Dotted,
                "dash" => Drawing.TextUnderlineValues.Dash,
                "wavy" => Drawing.TextUnderlineValues.Wavy,
                "false" or "none" => Drawing.TextUnderlineValues.None,
                _ => throw new ArgumentException($"Invalid underline value: '{rUnderline}'.")
            };
        if (properties.TryGetValue("strikethrough", out var rStrike) || properties.TryGetValue("strike", out rStrike))
            rProps.Strike = rStrike.ToLowerInvariant() switch
            {
                "true" or "single" => Drawing.TextStrikeValues.SingleStrike,
                "double" => Drawing.TextStrikeValues.DoubleStrike,
                "false" or "none" => Drawing.TextStrikeValues.NoStrike,
                _ => throw new ArgumentException($"Invalid strikethrough value: '{rStrike}'.")
            };
        if (properties.TryGetValue("color", out var rColor))
            rProps.AppendChild(BuildSolidFill(rColor));
        if (properties.TryGetValue("font", out var rFont))
        {
            rProps.Append(new Drawing.LatinFont { Typeface = rFont });
            rProps.Append(new Drawing.EastAsianFont { Typeface = rFont });
        }
        if (properties.TryGetValue("spacing", out var rSpacing) || properties.TryGetValue("charspacing", out rSpacing))
            rProps.Spacing = (int)(ParseHelpers.SafeParseDouble(rSpacing, "charspacing") * 100);

        newRun.RunProperties = rProps;
        var runText = properties.GetValueOrDefault("text", "");
        XmlTextValidator.ValidateOrThrow(runText, "text");
        newRun.Text = MakePreservingText(runText);
        return newRun;
    }
}
