// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using M = DocumentFormat.OpenXml.Math;

namespace OfficeCli.Core;

/// <summary>
/// Bidirectional converter between LaTeX-subset formula syntax and Office Math (OMML).
///
/// Supported LaTeX syntax:
///   _{}        subscript       H_{2}O
///   ^{}        superscript     x^{2}
///   \frac{}{}  fraction        \frac{a}{b}
///   \sqrt{}    square root     \sqrt{x}
///   \sqrt[n]{} nth root        \sqrt[3]{x}
///   \sum       summation       \sum_{i=1}^{n}
///   \int       integral        \int_{0}^{1}
///   \prod      product         \prod_{i=1}^{n}
///   \left( \right)  auto-sized delimiters  \left(\frac{a}{b}\right)
///   \begin{pmatrix} a & b \\ c & d \end{pmatrix}   matrix (pmatrix/bmatrix/vmatrix/matrix)
///   \overset{}{} upper annotation   \overset{\triangle}{\rightarrow}
///   \underset{}{} lower annotation   \underset{k}{\rightarrow}
///   \text{}     text mode (upright)   \text{if } x > 0
///   \overline{} overline              \overline{AB}
///   \underline{} underline            \underline{x}
///   \hat{} \bar{} \vec{} \dot{} \ddot{} \tilde{}  accent marks
///   \lim \sin \cos \tan \log \ln \exp \min \max    function names (upright)
///   \binom{}{} binomial coefficient   \binom{n}{k}
///   \cases     piecewise function     \begin{cases} x & x>0 \\ -x & x\leq 0 \end{cases}
///   \pm \times \cdot \rightarrow \leftarrow \uparrow \downarrow \triangle
///   \alpha \beta \gamma \delta \pi \theta \sigma \omega \lambda \mu \epsilon
///   Single-char shorthand: H_2 x^2 (braces optional for single char)
/// </summary>
internal static class FormulaParser
{
    // ==================== LaTeX → OMML ====================

    private const string KatexDocsHint = "See https://katex.org/docs/supported.html for supported syntax.";

    // Bound on LaTeX group nesting. Every recursion flows back through
    // ParseGroup, so counting its depth caps the whole parser. Deeply nested
    // input like \frac{{{…}}} (tens of thousands deep) would otherwise blow the
    // stack with an UNCATCHABLE StackOverflowException, crashing the process
    // (and, in resident mode, the server holding the open document). Real
    // formulas never approach this; exceeding it throws a normal catchable
    // FormulaParseException instead.
    // CONSISTENCY(dos-hardening): the depth threshold is single-sourced from
    // DocumentLimits.MaxRecursionDepth — the same cap the document-tree walkers
    // and HTML/SVG renderers use — rather than a duplicate local constant. Only
    // the thrown exception type differs (FormulaParseException carries a KaTeX
    // hint; the tree walkers throw CliException).
    [ThreadStatic] private static int _groupDepth;

    public static OpenXmlElement Parse(string latex)
    {
        try
        {
            // Preprocess: fix double-escaped backslashes (common AI/JSON over-escaping)
            // \\frac → \frac, \\sqrt → \sqrt, etc. (only when \\ is directly followed by a letter)
            latex = FixDoubleEscapedCommands(latex);
            // Preprocess: convert {a \over b} to \frac{a}{b}
            latex = RewriteOver(latex);
            var tokens = Tokenize(latex);
            var pos = 0;
            _groupDepth = 0; // reset per parse; recursion guard lives in ParseGroup
            var nodes = ParseGroup(tokens, ref pos, false);
            var root = WrapInOfficeMath(nodes);
            // Defense in depth: several builders wrap grouped content in an
            // <m:oMath> (via WrapInOfficeMath) to make a single scriptable node.
            // If such a wrapper lands inside an <m:e>/<m:num>/<m:den>/… without
            // being unwrapped, the result is a nested <m:oMath>, which is invalid
            // OMML — Word refuses to open the file ("file may be corrupt") even
            // though the SDK validator tolerates it. Flatten any non-root oMath.
            FlattenNestedOfficeMath(root);
            return root;
        }
        catch (Exception ex)
        {
            throw new FormulaParseException(
                $"Failed to parse formula: {ex.Message} {KatexDocsHint}", ex);
        }
    }

    /// <summary>
    /// Fix double-escaped backslashes from AI/JSON over-escaping.
    /// Converts \\cmd → \cmd when \\ is directly followed by a letter sequence.
    /// Safe because \\letter is not valid LaTeX (line break immediately followed by
    /// a bare word has no mathematical meaning). Legitimate usage like \\ \frac always
    /// has a space between the line break and the next command.
    /// </summary>
    private static string FixDoubleEscapedCommands(string latex)
    {
        // Replace \\ followed directly by a letter with \ (single pass, left to right)
        var sb = new System.Text.StringBuilder(latex.Length);
        int i = 0;
        while (i < latex.Length)
        {
            if (i + 2 < latex.Length && latex[i] == '\\' && latex[i + 1] == '\\' && char.IsLetter(latex[i + 2]))
            {
                // Collapse \\ to \ before the command
                sb.Append('\\');
                i += 2; // skip both backslashes, the letter will be consumed in the next iteration
            }
            else
            {
                sb.Append(latex[i]);
                i++;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Rewrite LaTeX old-style {numerator \over denominator} to \frac{numerator}{denominator}.
    /// Handles nested braces correctly.
    /// </summary>
    private static string RewriteOver(string latex)
    {
        while (true)
        {
            var idx = latex.IndexOf("\\over");
            if (idx < 0) break;

            // Find the opening brace that contains \over
            int braceStart = -1;
            int depth = 0;
            for (int i = idx - 1; i >= 0; i--)
            {
                if (latex[i] == '}') depth++;
                else if (latex[i] == '{')
                {
                    if (depth == 0) { braceStart = i; break; }
                    depth--;
                }
            }

            // Find the closing brace
            int braceEnd = -1;
            depth = 0;
            for (int i = idx + 5; i < latex.Length; i++)
            {
                if (latex[i] == '{') depth++;
                else if (latex[i] == '}')
                {
                    if (depth == 0) { braceEnd = i; break; }
                    depth--;
                }
            }

            if (braceStart < 0 || braceEnd < 0)
                break; // malformed, skip

            var num = latex.Substring(braceStart + 1, idx - braceStart - 1).Trim();
            var den = latex.Substring(idx + 5, braceEnd - idx - 5).Trim();
            latex = latex.Substring(0, braceStart) + $"\\frac{{{num}}}{{{den}}}" + latex.Substring(braceEnd + 1);
        }
        return latex;
    }

    public static OpenXmlElement ParseAsDisplayParagraph(string latex)
    {
        var math = Parse(latex);
        return new M.Paragraph(new M.OfficeMath(math.ChildElements.Select(e => e.CloneNode(true)).ToArray()));
    }

    // ==================== OMML → LaTeX ====================

    public static string ToLatex(OpenXmlElement element)
    {
        return TrimControlWordDelimiterSpaces(ToLatexByName(element));
    }

    // BUG-DUMP-R41-1: SymbolToCommandMap encodes a trailing space after every
    // control word (e.g. "π" → "\pi ") so a following letter can't fuse into a
    // bogus command ("\pix"). But that space is a LaTeX *delimiter* that is only
    // NEEDED before an ASCII letter; before "}"/digit/"+"/"\"/end it is swallowed
    // by a real TeX reader. Our own re-parser instead treats it as a literal
    // space run (Tokenizer's default arm collects the space as Text), so
    // "\frac{\pi }{2}" round-trips with an extra space run in the numerator
    // (src m:t ['π','2',...] → reb ['π',' ','2',...]). Strip the delimiter space
    // exactly where LaTeX would: after a control word (backslash + letters) when
    // the next non-... char is not an ASCII letter. Intentional spacing the
    // serializer emits is Unicode (thin/medium space), never "\, "/"~", so this
    // pass never touches deliberate spacing.
    private static string TrimControlWordDelimiterSpaces(string latex)
    {
        if (string.IsNullOrEmpty(latex) || latex.IndexOf('\\') < 0)
            return latex;
        var sb = new System.Text.StringBuilder(latex.Length);
        int i = 0;
        while (i < latex.Length)
        {
            char c = latex[i];
            sb.Append(c);
            i++;
            if (c == '\\' && i < latex.Length && char.IsLetter(latex[i]))
            {
                // Consume the control word's letters.
                while (i < latex.Length && char.IsLetter(latex[i]))
                {
                    sb.Append(latex[i]);
                    i++;
                }
                // A single delimiter space follows the control word: keep it only
                // if the next char is an ASCII letter (where it actually delimits).
                if (i < latex.Length && latex[i] == ' ')
                {
                    char next = i + 1 < latex.Length ? latex[i + 1] : '\0';
                    bool nextIsAsciiLetter = (next >= 'a' && next <= 'z') || (next >= 'A' && next <= 'Z');
                    if (nextIsAsciiLetter)
                        sb.Append(' ');
                    i++; // consume the delimiter space regardless
                }
            }
        }
        return sb.ToString();
    }

    private static string ToLatexByName(OpenXmlElement element)
    {
        var name = element.LocalName;

        switch (name)
        {
            case "oMathPara":
                return JoinChildren(element);

            case "oMath":
                return JoinChildren(element);

            case "r":
            {
                var tElem = element.ChildElements.FirstOrDefault(e => e.LocalName == "t");
                var text = tElem?.InnerText ?? "";
                // Check for math style in run properties (mathbf, mathrm, etc.)
                var rPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "rPr");
                // Check for w:rPr with w:color (used by \color{})
                var wRPr = element.ChildElements.FirstOrDefault(e =>
                    e is DocumentFormat.OpenXml.Wordprocessing.RunProperties);
                string? colorHex = null;
                if (wRPr != null)
                {
                    var colorEl = wRPr.ChildElements.FirstOrDefault(e => e.LocalName == "color");
                    colorHex = colorEl?.GetAttribute("val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main").Value;
                }
                string result;
                if (rPr != null)
                {
                    var sty = rPr.ChildElements.FirstOrDefault(e => e.LocalName == "sty");
                    var styVal = sty?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value;
                    var hasNor = rPr.ChildElements.Any(e => e.LocalName == "nor");
                    // BUG-R8A(BUG1) secondary: m:scr (math script style) was
                    // dropped on dump — the write path emits \mathbb→double-struck
                    // and \mathcal→script (m:scr in m:rPr), but XML→LaTeX never
                    // read m:scr back, so those runs round-tripped as plain text.
                    var scr = rPr.ChildElements.FirstOrDefault(e => e.LocalName == "scr");
                    var scrVal = scr?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value;
                    if (hasNor)
                    {
                        // m:nor (NormalText) flags an upright run. Function
                        // names like sin/cos/tan/log/ln go through this same
                        // node on the write path (see ParseCommand case "lim"
                        // or "sin" or "cos" ...), so an upright run whose text
                        // is one of those names round-trips back to \sin
                        // rather than \text{sin}. CONSISTENCY(formula-funcname):
                        // keep this set in sync with the upright-name list in
                        // ParseCommand's "lim or sin or cos ..." arm.
                        if (_uprightFunctionNames.Contains(text))
                            result = "\\" + text;
                        else
                            result = $"\\text{{{EscapeLatex(text)}}}";
                    }
                    // BUG-R8A(BUG4): m:sty (weight/posture: p/b/i/bi) and m:scr
                    // (script alphabet: double-struck/script) are orthogonal OMML
                    // axes. The script-alphabet command (\mathbb/\mathcal) forces
                    // m:sty=p on the write side, so a run carrying BOTH (e.g.
                    // scr=double-struck + sty=bi) must emit the composed form
                    // \mathbb{\boldsymbol{R}} — the writer's mathbb/mathcal arm
                    // reads the inner style command's m:sty back. Inner style
                    // wrapper per m:sty value (the math default is italic, so a
                    // bare run already round-trips to "i"; emit \mathit only when
                    // it must compose with a script wrapper):
                    //   b → \mathbf, i → \mathit, bi → \boldsymbol, p → \mathrm.
                    else if (scrVal == "double-struck")
                        result = $"\\mathbb{{{WrapMathStyle(styVal, text)}}}";
                    else if (scrVal == "script")
                        result = $"\\mathcal{{{WrapMathStyle(styVal, text)}}}";
                    else
                        result = WrapMathStyle(styVal, text);
                }
                else
                    result = EscapeLatex(text);
                // Hex-gate before interpolating into LaTeX: a crafted w:color
                // val could close the \textcolor brace group and inject
                // \href{…} / \url{…} that KaTeX may honor when trust=true.
                if (colorHex != null && IsLaTeXHex(colorHex))
                    result = $"\\textcolor{{#{colorHex}}}{{{result}}}";
                return result;
            }

            case "sSub":
            {
                // SymbolToCommandMap appends a trailing space after each
                // Greek/symbol command (e.g. "α" -> "\alpha ") so a
                // following letter ("\alphax") doesn't fuse into a bogus
                // command name. When the very next char on the assembled
                // LaTeX is "_" or "^", that trailing space detaches the
                // sub/sup from its base on re-parse: "\alpha _1" parses as
                // a bare \alpha followed by a stray "_1" subscript, which
                // round-trips through Add as an extra m:r run plus a
                // headless m:sSub — visible as "α 1" instead of α₁.
                // Strip the trailing space before stitching the script.
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e")).TrimEnd();
                var subText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "sub"));
                return NeedsBraces(subText) ? $"{baseText}_{{{subText}}}" : $"{baseText}_{subText}";
            }

            case "sSup":
            {
                // CONSISTENCY(latex-script-base-trim): see sSub above —
                // trailing space on the base detaches "^" on re-parse.
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e")).TrimEnd();
                var supText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "sup"));
                return NeedsBraces(supText) ? $"{baseText}^{{{supText}}}" : $"{baseText}^{supText}";
            }

            case "sSubSup":
            {
                // CONSISTENCY(latex-script-base-trim): see sSub above.
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e")).TrimEnd();
                var subText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "sub"));
                var supText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "sup"));
                var subPart = NeedsBraces(subText) ? $"_{{{subText}}}" : $"_{subText}";
                var supPart = NeedsBraces(supText) ? $"^{{{supText}}}" : $"^{supText}";
                return $"{baseText}{subPart}{supPart}";
            }

            case "f": // fraction
            {
                var num = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "num"));
                var den = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "den"));
                return $"\\frac{{{num}}}{{{den}}}";
            }

            case "rad": // radical
            {
                var deg = element.ChildElements.FirstOrDefault(e => e.LocalName == "deg");
                var baseElem = element.ChildElements.FirstOrDefault(e => e.LocalName == "e");
                var baseText = ArgToLatex(baseElem);
                // Check if degree is hidden or empty
                var radPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "radPr");
                var hideDeg = radPr?.ChildElements.FirstOrDefault(e => e.LocalName == "degHide");
                var isHidden = hideDeg != null && (hideDeg.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value == "1"
                    || hideDeg.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value == "true");
                var degText = isHidden ? "" : ArgToLatex(deg);
                if (string.IsNullOrEmpty(degText))
                    return $"\\sqrt{{{baseText}}}";
                return $"\\sqrt[{degText}]{{{baseText}}}";
            }

            case "nary":
            {
                var naryPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "naryPr");
                var chrElem = naryPr?.ChildElements.FirstOrDefault(e => e.LocalName == "chr");
                var chr = chrElem?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? "∑";
                var cmd = NaryCharToCommand(chr);
                var subText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "sub"));
                var supText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "sup"));
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var result = cmd;
                if (!string.IsNullOrEmpty(subText))
                    result += NeedsBraces(subText) ? $"_{{{subText}}}" : $"_{subText}";
                if (!string.IsNullOrEmpty(supText))
                    result += NeedsBraces(supText) ? $"^{{{supText}}}" : $"^{supText}";
                if (!string.IsNullOrEmpty(baseText))
                    result += $" {baseText}";
                return result;
            }

            case "d": // delimiter
            {
                var dPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "dPr");
                var begChr = dPr?.ChildElements.FirstOrDefault(e => e.LocalName == "begChr");
                var endChr = dPr?.ChildElements.FirstOrDefault(e => e.LocalName == "endChr");
                var begin = begChr?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? "(";
                var end = endChr?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? ")";
                // Check if delimiter wraps a matrix — emit \begin{pmatrix} etc.
                var bases = element.ChildElements.Where(e => e.LocalName == "e").ToList();
                if (bases.Count == 1)
                {
                    var inner = bases[0].ChildElements.FirstOrDefault(e => e.LocalName == "m");
                    if (inner != null)
                    {
                        var envName = (begin, end) switch
                        {
                            ("(", ")") => "pmatrix",
                            ("[", "]") => "bmatrix",
                            ("{", "}") => "Bmatrix",
                            ("|", "|") => "vmatrix",
                            _ => null
                        };
                        var matrixContent = ToLatexByName(inner);
                        if (envName != null)
                            return $"\\begin{{{envName}}}{matrixContent}\\end{{{envName}}}";
                        return $"\\left{begin}\\begin{{matrix}}{matrixContent}\\end{{matrix}}\\right{end}";
                    }
                }
                var content = string.Concat(bases.Select(ArgToLatex));
                return $"{begin}{content}{end}";
            }

            case "limUpp": // upper limit (overset)
            {
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var limText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "lim"));
                return $"\\overset{{{limText}}}{{{baseText}}}";
            }

            case "limLow": // lower limit (underset)
            {
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var limText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "lim"));
                return $"\\underset{{{limText}}}{{{baseText}}}";
            }

            case "bar": // overline/underline
            {
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var barPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "barPr");
                var posElem = barPr?.ChildElements.FirstOrDefault(e => e.LocalName == "pos");
                var posVal = posElem?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value;
                return posVal == "bot" ? $"\\underline{{{baseText}}}" : $"\\overline{{{baseText}}}";
            }

            case "acc": // accent
            {
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var accPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "accPr");
                var chrElem = accPr?.ChildElements.FirstOrDefault(e => e.LocalName == "chr");
                var chr = chrElem?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? "\u0302";
                var cmd = chr switch
                {
                    "\u0302" => "hat",
                    "\u0304" => "bar",
                    "\u20D7" => "vec",
                    "\u0307" => "dot",
                    "\u0308" => "ddot",
                    "\u0303" => "tilde",
                    _ => "hat"
                };
                return $"\\{cmd}{{{baseText}}}";
            }

            case "func":
            {
                // BUG-DUMP-R48-1: m:func named-function (sin/cos/log) — was concatenated to "sinx"
                var fName = element.ChildElements.FirstOrDefault(e => e.LocalName == "fName");
                var fArg = element.ChildElements.FirstOrDefault(e => e.LocalName == "e");
                var nameText = fName != null ? JoinChildren(fName).Trim() : "";
                var argLatex = fArg != null ? ArgToLatex(fArg) : "";
                string nameLatex;
                // CONSISTENCY(formula-funcname): known upright functions emit the
                // dedicated command (\sin etc.); the parser round-trips these to an
                // m:nor run. \operatorname{...} (ParseCommand case "operatorname")
                // covers arbitrary names, so unknown names stay readable too.
                if (_uprightFunctionNames.Contains(nameText))
                    nameLatex = "\\" + nameText;
                else if (!string.IsNullOrEmpty(nameText))
                    nameLatex = $"\\operatorname{{{nameText}}}";
                else
                    // No usable name — fall back to the default concat behavior.
                    return JoinChildren(element);
                return string.IsNullOrEmpty(argLatex) ? nameLatex : nameLatex + " " + argLatex;
            }

            case "m": // matrix
            {
                var matrixRows = element.ChildElements.Where(e => e.LocalName == "mr").ToList();
                // Trim each cell's leading/trailing whitespace before joining
                // with " & "/" \\\\ ". The tokenizer collapses runs of ordinary
                // characters into a single Text token that includes surrounding
                // spaces (e.g. "a " before "&", " b " between "&" and "\\\\"),
                // so a raw join produces "a  &  b  \\\\  c" and round-trips
                // accumulate one space per pass. Trimming at the cell boundary
                // restores "a & b \\\\ c & d" — matrix delimiters carry their
                // own spacing semantics in LaTeX, so cell-internal padding adds
                // nothing.
                var rowStrings = matrixRows.Select(mr =>
                    string.Join(" & ", mr.ChildElements.Where(e => e.LocalName == "e").Select(e => ArgToLatex(e).Trim())));
                var content = string.Join(" \\\\ ", rowStrings);
                // Standalone matrix (not inside a delimiter) needs environment wrapper
                if (element.Parent?.LocalName != "e" || element.Parent?.Parent?.LocalName != "d")
                    return $"\\begin{{matrix}}{content}\\end{{matrix}}";
                return content;
            }

            case "borderBox":
            {
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var bbPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "borderBoxPr");
                var hasStrikeTLBR = bbPr?.ChildElements.Any(e => e.LocalName == "strikeTLBR") ?? false;
                var hasStrikeBLTR = bbPr?.ChildElements.Any(e => e.LocalName == "strikeBLTR") ?? false;
                var hasStrikeH = bbPr?.ChildElements.Any(e => e.LocalName == "strikeH") ?? false;
                if (hasStrikeTLBR && hasStrikeBLTR)
                    return $"\\cancel{{{baseText}}}"; // xcancel → KaTeX uses \cancel for visual
                if (hasStrikeTLBR || hasStrikeBLTR || hasStrikeH)
                    return $"\\cancel{{{baseText}}}";
                return $"\\boxed{{{baseText}}}";
            }

            case "groupChr":
            {
                var baseText = ArgToLatex(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var gcPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "groupChrPr");
                var chrEl = gcPr?.ChildElements.FirstOrDefault(e => e.LocalName == "chr");
                var chr = chrEl?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value;
                var posEl = gcPr?.ChildElements.FirstOrDefault(e => e.LocalName == "pos");
                var pos = posEl?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value;
                if (chr == "\u23DF" || pos == "bot") // ⏟
                    return $"\\underbrace{{{baseText}}}";
                if (chr == "\u23DE" || pos == "top") // ⏞
                    return $"\\overbrace{{{baseText}}}";
                return baseText;
            }

            case "eqArr": // BUG-DUMP-R49-2: equation array — stacked equations
            {
                // m:eqArr holds one m:e child per row. Without a case it fell
                // through to the default below, which concatenated every row's
                // text directly (e.g. "a=1b=2"). Emit \begin{aligned}…\end{aligned}
                // — the existing aligned-environment parser reconstructs a
                // vertical stack — so the rows survive as a structured equation
                // instead of running together as one line.
                var eqRows = element.ChildElements
                    .Where(e => e.LocalName == "e")
                    .Select(e => ArgToLatex(e).Trim());
                return $"\\begin{{aligned}}{string.Join(" \\\\ ", eqRows)}\\end{{aligned}}";
            }

            default:
                // Recurse into unknown containers
                return string.Concat(element.ChildElements.Select(ToLatexByName));
        }
    }

    private static bool NeedsBraces(string text) => text.Length != 1;

    /// <summary>
    /// Convert OMML to readable Unicode text (for view text display).
    /// Uses Unicode subscript/superscript characters where possible.
    /// </summary>
    public static string ToReadableText(OpenXmlElement element)
    {
        var name = element.LocalName;

        switch (name)
        {
            case "oMathPara":
                return string.Concat(element.ChildElements.Select(ToReadableText));

            case "oMath":
                return string.Concat(element.ChildElements.Select(ToReadableText));

            case "r":
            {
                var tElem = element.ChildElements.FirstOrDefault(e => e.LocalName == "t");
                return tElem?.InnerText ?? "";
            }

            case "sSub":
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var subText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "sub"));
                return baseText + ToUnicodeSubscript(subText);
            }

            case "sSup":
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var supText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "sup"));
                return baseText + ToUnicodeSuperscript(supText);
            }

            case "sSubSup":
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var subText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "sub"));
                var supText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "sup"));
                return baseText + ToUnicodeSubscript(subText) + ToUnicodeSuperscript(supText);
            }

            case "f": // fraction
            {
                var num = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "num"));
                var den = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "den"));
                return $"({num})/({den})";
            }

            case "rad": // radical
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                return $"√({baseText})";
            }

            case "nary":
            {
                var naryPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "naryPr");
                var chrElem = naryPr?.ChildElements.FirstOrDefault(e => e.LocalName == "chr");
                var chr = chrElem?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? "∑";
                var subText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "sub"));
                var supText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "sup"));
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var result = chr;
                if (!string.IsNullOrEmpty(subText)) result += ToUnicodeSubscript(subText);
                if (!string.IsNullOrEmpty(supText)) result += ToUnicodeSuperscript(supText);
                result += $" {baseText}";
                return result;
            }

            case "d": // delimiter
            {
                var dPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "dPr");
                var begChr = dPr?.ChildElements.FirstOrDefault(e => e.LocalName == "begChr");
                var endChr = dPr?.ChildElements.FirstOrDefault(e => e.LocalName == "endChr");
                var begin = begChr?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? "(";
                var end = endChr?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? ")";
                var content = string.Concat(element.ChildElements
                    .Where(e => e.LocalName == "e")
                    .Select(ArgToReadable));
                return $"{begin}{content}{end}";
            }

            case "limUpp": // upper limit (overset)
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var limText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "lim"));
                return $"{baseText}({limText})";
            }

            case "limLow": // lower limit (underset)
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var limText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "lim"));
                return $"{baseText}({limText})";
            }

            case "bar": // overline/underline
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                return baseText;
            }

            case "acc": // accent
            {
                var baseText = ArgToReadable(element.ChildElements.FirstOrDefault(e => e.LocalName == "e"));
                var accPr = element.ChildElements.FirstOrDefault(e => e.LocalName == "accPr");
                var chrElem = accPr?.ChildElements.FirstOrDefault(e => e.LocalName == "chr");
                var chr = chrElem?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math").Value ?? "";
                return baseText + chr;
            }

            case "m": // matrix
            {
                var matrixRows = element.ChildElements.Where(e => e.LocalName == "mr").ToList();
                var rowStrings = matrixRows.Select(mr =>
                    string.Join(", ", mr.ChildElements.Where(e => e.LocalName == "e").Select(ArgToReadable)));
                return "[" + string.Join("; ", rowStrings) + "]";
            }

            case "eqArr": // BUG-DUMP-R49-2: equation array — rows joined by semicolons
            {
                var eqRows = element.ChildElements
                    .Where(e => e.LocalName == "e")
                    .Select(ArgToReadable);
                return string.Join("; ", eqRows);
            }

            default:
                return string.Concat(element.ChildElements.Select(ToReadableText));
        }
    }

    /// <summary>
    /// Concat oMath/oMathPara children with whitespace deduping at sibling
    /// boundaries. SymbolToCommandMap entries (e.g. "\pm ", "\sqrt ") encode
    /// a trailing space so the LaTeX command can't fuse with the next token
    /// (e.g. "\pma"). Adjacent text runs in the OMML re-introduce that same
    /// separating space, producing one extra space per round-trip
    /// (BUG-R3-1: \pm becomes \pm  becomes \pm   becomes \pm    after each
    /// dump→batch). Collapse `WS{trailing}WS{leading}` to a single WS so the
    /// LaTeX text stays stable across round-trips.
    /// </summary>
    private static string JoinChildren(OpenXmlElement element)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var child in element.ChildElements)
        {
            var part = ToLatexByName(child);
            if (sb.Length > 0 && part.Length > 0
                && char.IsWhiteSpace(sb[^1]) && char.IsWhiteSpace(part[0]))
            {
                int p = 0;
                while (p < part.Length && char.IsWhiteSpace(part[p])) p++;
                sb.Append(part, p, part.Length - p);
            }
            else
            {
                sb.Append(part);
            }
        }
        return sb.ToString();
    }

    // ==================== Tokenizer ====================

    private enum TokenType { Text, Sub, Sup, LBrace, RBrace, LBracket, RBracket, Command, ColSep, RowSep }

    private record Token(TokenType Type, string Value);

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            switch (c)
            {
                case '_':
                    tokens.Add(new Token(TokenType.Sub, "_"));
                    i++;
                    break;
                case '^':
                    tokens.Add(new Token(TokenType.Sup, "^"));
                    i++;
                    break;
                case '{':
                    tokens.Add(new Token(TokenType.LBrace, "{"));
                    i++;
                    break;
                case '}':
                    tokens.Add(new Token(TokenType.RBrace, "}"));
                    i++;
                    break;
                case '[':
                    tokens.Add(new Token(TokenType.LBracket, "["));
                    i++;
                    break;
                case ']':
                    tokens.Add(new Token(TokenType.RBracket, "]"));
                    i++;
                    break;
                case '&':
                    tokens.Add(new Token(TokenType.ColSep, "&"));
                    i++;
                    break;
                case '\\':
                    i++;
                    // \\ → row separator
                    if (i < input.Length && input[i] == '\\')
                    {
                        tokens.Add(new Token(TokenType.RowSep, "\\\\"));
                        i++;
                        break;
                    }
                    // \| → double vertical bar (‖), distinct from \{ \} which are literal
                    if (i < input.Length && input[i] == '|')
                    {
                        tokens.Add(new Token(TokenType.Command, "Vert"));
                        i++;
                        break;
                    }
                    // Escaped braces: \{ \} → literal text
                    if (i < input.Length && (input[i] == '{' || input[i] == '}'))
                    {
                        tokens.Add(new Token(TokenType.Text, input[i].ToString()));
                        i++;
                        break;
                    }
                    var cmd = "";
                    while (i < input.Length && char.IsLetter(input[i]))
                    {
                        cmd += input[i];
                        i++;
                    }
                    if (cmd.Length == 0)
                    {
                        // \<non-letter> like \, \; \: \! → spacing commands
                        if (i < input.Length)
                        {
                            var spaceChar = input[i] switch
                            {
                                ',' => "\u2009", // thin space
                                ';' => "\u2005", // medium space
                                ':' => "\u2005", // medium space
                                '!' => "",        // negative thin space (ignore)
                                _ => input[i].ToString()
                            };
                            if (spaceChar.Length > 0)
                                tokens.Add(new Token(TokenType.Text, spaceChar));
                            i++;
                        }
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Command, cmd));
                    }
                    break;
                default:
                    // Collect consecutive text characters
                    var text = "";
                    while (i < input.Length && !IsSpecialChar(input[i]))
                    {
                        text += input[i];
                        i++;
                    }
                    if (text.Length > 0)
                        tokens.Add(new Token(TokenType.Text, text));
                    break;
            }
        }

        return tokens;
    }

    private static bool IsSpecialChar(char c) => c is '_' or '^' or '{' or '}' or '[' or ']' or '\\' or '&';

    // ==================== Parser ====================

    private static List<OpenXmlElement> ParseGroup(List<Token> tokens, ref int pos, bool insideBraces)
    {
        if (++_groupDepth > DocumentLimits.MaxRecursionDepth)
        {
            _groupDepth--;
            throw new FormulaParseException(
                $"Formula nesting exceeds the maximum supported depth ({DocumentLimits.MaxRecursionDepth}). {KatexDocsHint}");
        }
        try
        {
        var elements = new List<OpenXmlElement>();

        while (pos < tokens.Count)
        {
            var token = tokens[pos];

            if (token.Type == TokenType.RBrace)
            {
                if (insideBraces) { pos++; break; }
                pos++;
                continue;
            }

            if (token.Type == TokenType.Text)
            {
                pos++;
                OpenXmlElement textElement = MakeMathRun(token.Value);
                // Check if next token is sub or sup
                textElement = TryAttachScript(tokens, ref pos, textElement);
                elements.Add(textElement);
            }
            else if (token.Type == TokenType.LBrace)
            {
                pos++;
                var inner = ParseGroup(tokens, ref pos, true);
                var grouped = WrapInOfficeMath(inner);
                // Check if next is sub/sup
                var result = TryAttachScript(tokens, ref pos, grouped);
                elements.Add(result);
            }
            else if (token.Type == TokenType.Command)
            {
                pos++;
                var cmdElement = ParseCommand(token.Value, tokens, ref pos);
                cmdElement = TryAttachScript(tokens, ref pos, cmdElement);
                elements.Add(cmdElement);
            }
            else if (token.Type == TokenType.Sub || token.Type == TokenType.Sup)
            {
                // Sub/sup without preceding element — use empty base
                var emptyRun = MakeMathRun("");
                var scripted = TryAttachScript(tokens, ref pos, emptyRun);
                elements.Add(scripted);
            }
            else if (token.Type == TokenType.LBracket || token.Type == TokenType.RBracket)
            {
                pos++;
                var bracketText = token.Type == TokenType.LBracket ? "[" : "]";
                OpenXmlElement bracketElement = MakeMathRun(bracketText);
                bracketElement = TryAttachScript(tokens, ref pos, bracketElement);
                elements.Add(bracketElement);
            }
            else
            {
                pos++;
            }
        }

        return elements;
        }
        finally { _groupDepth--; }
    }

    private static OpenXmlElement TryAttachScript(List<Token> tokens, ref int pos, OpenXmlElement baseElement)
    {
        while (pos < tokens.Count)
        {
            if (tokens[pos].Type == TokenType.Sub)
            {
                pos++;
                var subContent = ParseSingleArg(tokens, ref pos);

                // Check if followed by superscript → SubSuperscript
                if (pos < tokens.Count && tokens[pos].Type == TokenType.Sup)
                {
                    pos++;
                    var supContent = ParseSingleArg(tokens, ref pos);
                    baseElement = new M.SubSuperscript(
                        new M.Base(ExtractChildren(baseElement)),
                        new M.SubArgument(ExtractChildren(subContent)),
                        new M.SuperArgument(ExtractChildren(supContent))
                    );
                }
                else
                {
                    baseElement = new M.Subscript(
                        new M.Base(ExtractChildren(baseElement)),
                        new M.SubArgument(ExtractChildren(subContent))
                    );
                }
            }
            else if (tokens[pos].Type == TokenType.Sup)
            {
                pos++;
                var supContent = ParseSingleArg(tokens, ref pos);

                // Check if followed by subscript → SubSuperscript
                if (pos < tokens.Count && tokens[pos].Type == TokenType.Sub)
                {
                    pos++;
                    var subContent = ParseSingleArg(tokens, ref pos);
                    baseElement = new M.SubSuperscript(
                        new M.Base(ExtractChildren(baseElement)),
                        new M.SubArgument(ExtractChildren(subContent)),
                        new M.SuperArgument(ExtractChildren(supContent))
                    );
                }
                else
                {
                    baseElement = new M.Superscript(
                        new M.Base(ExtractChildren(baseElement)),
                        new M.SuperArgument(ExtractChildren(supContent))
                    );
                }
            }
            else
            {
                break;
            }
        }

        return baseElement;
    }

    private static OpenXmlElement ParseSingleArg(List<Token> tokens, ref int pos)
    {
        if (pos >= tokens.Count) return MakeMathRun("");

        if (tokens[pos].Type == TokenType.LBrace)
        {
            pos++;
            var inner = ParseGroup(tokens, ref pos, true);
            return inner.Count == 1 ? inner[0] : WrapInOfficeMath(inner);
        }

        if (tokens[pos].Type == TokenType.Command)
        {
            pos++;
            return ParseCommand(tokens[pos - 1].Value, tokens, ref pos);
        }

        if (tokens[pos].Type == TokenType.Text)
        {
            // Single character for shorthand: H_2 takes just "2", but "2O" should take just "2"
            var text = tokens[pos].Value;
            pos++;
            // Strip leading whitespace before picking the single char. The
            // tokenizer collapses ordinary characters into one Text token that
            // can include the separator space between a command and its
            // argument (e.g. "\sum_{i=1}^n i" tokenises the trailing arg as
            // " i", and "\sin x" tokenises as " x"). Without this skip, the
            // first char picked was the space itself and the actual argument
            // (i, x) leaked out as a sibling sitting outside the nary / func.
            int leadingWs = 0;
            while (leadingWs < text.Length && char.IsWhiteSpace(text[leadingWs]))
                leadingWs++;
            if (leadingWs >= text.Length)
            {
                // Token was pure whitespace — fall through to empty arg.
                return MakeMathRun("");
            }
            text = text[leadingWs..];
            if (text.Length == 1)
                return MakeMathRun(text);
            // For multi-char text in a subscript/superscript arg without braces, take only first char
            // Put the rest back as a new text token
            if (text.Length > 1)
            {
                tokens.Insert(pos, new Token(TokenType.Text, text[1..]));
            }
            return MakeMathRun(text[..1]);
        }

        pos++;
        return MakeMathRun("");
    }

    private static OpenXmlElement ParseCommand(string cmd, List<Token> tokens, ref int pos)
    {
        // Symbol commands
        var symbol = CommandToSymbol(cmd);
        if (symbol != null)
            return MakeMathRun(symbol);

        switch (cmd)
        {
            case "frac":
            {
                var num = ParseBracedArg(tokens, ref pos);
                var den = ParseBracedArg(tokens, ref pos);
                return new M.Fraction(
                    new M.Numerator(ExtractChildren(num)),
                    new M.Denominator(ExtractChildren(den))
                );
            }
            case "sqrt":
            {
                // Check for optional [degree]
                OpenXmlElement? degree = null;
                if (pos < tokens.Count && tokens[pos].Type == TokenType.LBracket)
                {
                    pos++; // skip [
                    var degTokens = new List<Token>();
                    while (pos < tokens.Count && tokens[pos].Type != TokenType.RBracket)
                    {
                        degTokens.Add(tokens[pos]);
                        pos++;
                    }
                    if (pos < tokens.Count) pos++; // skip ]
                    int degPos = 0;
                    var degElements = ParseGroup(degTokens, ref degPos, false);
                    degree = degElements.Count == 1 ? degElements[0] : WrapInOfficeMath(degElements);
                }
                var content = ParseBracedArg(tokens, ref pos);

                var radical = new M.Radical(
                    new M.RadicalProperties(),
                    new M.Degree(degree != null ? ExtractChildren(degree) : Array.Empty<OpenXmlElement>()),
                    new M.Base(ExtractChildren(content))
                );

                // For square root (no degree), hide the degree
                if (degree == null)
                {
                    radical.RadicalProperties!.AppendChild(new M.HideDegree { Val = M.BooleanValues.True });
                }

                return radical;
            }
            case "matrix":
            {
                // \matrix{a&b\\c&d} — shorthand syntax (no \begin/\end)
                if (pos < tokens.Count && tokens[pos].Type == TokenType.LBrace)
                {
                    pos++; // skip {
                    // Temporarily collect tokens until matching }
                    var matrixTokens = new List<Token>();
                    int braceDepth = 1;
                    while (pos < tokens.Count && braceDepth > 0)
                    {
                        if (tokens[pos].Type == TokenType.LBrace) braceDepth++;
                        else if (tokens[pos].Type == TokenType.RBrace) { braceDepth--; if (braceDepth == 0) { pos++; break; } }
                        matrixTokens.Add(tokens[pos]);
                        pos++;
                    }
                    // Insert into tokens stream and parse as matrix
                    int mpos = 0;
                    // Reuse the matrix parser by appending a fake \end token
                    matrixTokens.Add(new Token(TokenType.Command, "end"));
                    matrixTokens.Add(new Token(TokenType.LBrace, "{"));
                    matrixTokens.Add(new Token(TokenType.Text, "matrix"));
                    matrixTokens.Add(new Token(TokenType.RBrace, "}"));
                    return ParseMatrix("matrix", matrixTokens, ref mpos);
                }
                return MakeMathRun("matrix");
            }
            case "begin":
            {
                // Read environment name from {name}
                var envName = "";
                if (pos < tokens.Count && tokens[pos].Type == TokenType.LBrace)
                {
                    pos++;
                    while (pos < tokens.Count && tokens[pos].Type != TokenType.RBrace)
                    {
                        envName += tokens[pos].Value;
                        pos++;
                    }
                    if (pos < tokens.Count) pos++; // skip }
                }

                if (envName is "matrix" or "pmatrix" or "bmatrix" or "Bmatrix" or "vmatrix" or "cases"
                    or "array")
                {
                    // For array, skip optional column spec like {cc}
                    if (envName == "array" && pos < tokens.Count && tokens[pos].Type == TokenType.LBrace)
                    {
                        pos++; // skip {
                        while (pos < tokens.Count && tokens[pos].Type != TokenType.RBrace) pos++;
                        if (pos < tokens.Count) pos++; // skip }
                    }
                    var matrixResult = ParseMatrix(envName, tokens, ref pos);
                    // array should render without implicit delimiters
                    if (envName == "array" && matrixResult is M.Delimiter arrDelim)
                    {
                        var innerMatrix = arrDelim.GetFirstChild<M.Base>()?.GetFirstChild<M.Matrix>();
                        if (innerMatrix != null)
                            return innerMatrix.CloneNode(true);
                    }
                    return matrixResult;
                }
                if (envName is "align" or "align*" or "aligned" or "gathered" or "eqnarray"
                    or "eqnarray*" or "split")
                {
                    // Multi-line equation environments map to m:eqArr (equation
                    // array — a vertical stack of equations), NOT m:m. m:m is a
                    // matrix grid; using it here made \begin{aligned} round-trip
                    // as \begin{matrix}. Rows are tokenized via the matrix parser
                    // (\\ row breaks, & alignment points), then each m:mr row is
                    // flattened into one m:e of the equation array.
                    var matrixEl = ParseMatrix(envName, tokens, ref pos);
                    // ParseMatrix may wrap the matrix in a delimiter; guard like
                    // WrapInOfficeMath (single node returned directly).
                    var rowsMatrix = matrixEl as M.Matrix
                        ?? matrixEl.Descendants<M.Matrix>().FirstOrDefault();
                    if (rowsMatrix == null)
                        return matrixEl;
                    var eqArr = new M.EquationArray();
                    foreach (var mr in rowsMatrix.Elements<M.MatrixRow>())
                    {
                        var rowBase = new M.Base();
                        foreach (var cell in mr.Elements<M.Base>())
                            foreach (var child in cell.ChildElements)
                                rowBase.AppendChild(child.CloneNode(true));
                        eqArr.AppendChild(rowBase);
                    }
                    return eqArr;
                }

                // Unknown environment, render as text
                return MakeMathRun($"\\begin{{{envName}}}");
            }
            case "end":
            {
                // Skip \end{name} — should be consumed by matrix parser
                if (pos < tokens.Count && tokens[pos].Type == TokenType.LBrace)
                {
                    pos++;
                    while (pos < tokens.Count && tokens[pos].Type != TokenType.RBrace) pos++;
                    if (pos < tokens.Count) pos++;
                }
                return MakeMathRun("");
            }
            case "left":
            {
                // Get opening delimiter character from next token
                var openChar = "(";
                if (pos < tokens.Count && tokens[pos].Type == TokenType.Command)
                {
                    // Handle \left\langle, \left\lfloor, \left\lceil, \left\lvert, \left\|
                    var delimCmd = tokens[pos].Value;
                    var mapped = delimCmd switch
                    {
                        "langle" => "\u27E8",
                        "lceil" => "\u2308",
                        "lfloor" => "\u230A",
                        "lvert" => "|",
                        "lVert" => "\u2016",
                        "|" => "\u2016",
                        _ => null
                    };
                    if (mapped != null) { openChar = mapped; pos++; }
                }
                else if (pos < tokens.Count && tokens[pos].Type == TokenType.Text)
                {
                    openChar = tokens[pos].Value[..1];
                    if (tokens[pos].Value.Length > 1)
                        tokens[pos] = new Token(TokenType.Text, tokens[pos].Value[1..]);
                    else
                        pos++;
                }
                else if (pos < tokens.Count && tokens[pos].Type == TokenType.LBracket)
                {
                    openChar = "[";
                    pos++;
                }

                // Parse content until \right
                var content = new List<OpenXmlElement>();
                var closeChar = openChar switch { "(" => ")", "[" => "]", "{" => "}", "|" => "|", "\u27E8" => "\u27E9", "\u2308" => "\u2309", "\u230A" => "\u230B", "\u2016" => "\u2016", _ => ")" };
                while (pos < tokens.Count)
                {
                    if (tokens[pos].Type == TokenType.Command && tokens[pos].Value == "right")
                    {
                        pos++;
                        // Get closing delimiter character — capture the actual delimiter
                        if (pos < tokens.Count && tokens[pos].Type == TokenType.Command)
                        {
                            // Handle \right\rangle, \right\rfloor, \right\rceil, etc.
                            var rDelimCmd = tokens[pos].Value;
                            var rMapped = rDelimCmd switch
                            {
                                "rangle" => "\u27E9",
                                "rceil" => "\u2309",
                                "rfloor" => "\u230B",
                                "rvert" => "|",
                                "rVert" => "\u2016",
                                "|" => "\u2016",
                                _ => null
                            };
                            if (rMapped != null) { closeChar = rMapped; pos++; }
                        }
                        else if (pos < tokens.Count && tokens[pos].Type == TokenType.Text)
                        {
                            closeChar = tokens[pos].Value[..1];
                            if (tokens[pos].Value.Length > 1)
                                tokens[pos] = new Token(TokenType.Text, tokens[pos].Value[1..]);
                            else
                                pos++;
                        }
                        else if (pos < tokens.Count && tokens[pos].Type == TokenType.RBracket)
                        {
                            closeChar = "]";
                            pos++;
                        }
                        break;
                    }

                    // Reuse main parsing logic for each element
                    if (tokens[pos].Type == TokenType.Text)
                    {
                        var textEl = MakeMathRun(tokens[pos].Value);
                        pos++;
                        textEl = (M.Run)TryAttachScript(tokens, ref pos, textEl);
                        content.Add(textEl);
                    }
                    else if (tokens[pos].Type == TokenType.LBrace)
                    {
                        pos++;
                        var inner = ParseGroup(tokens, ref pos, true);
                        var grouped = WrapInOfficeMath(inner);
                        grouped = TryAttachScript(tokens, ref pos, grouped);
                        content.Add(grouped);
                    }
                    else if (tokens[pos].Type == TokenType.Command)
                    {
                        pos++;
                        var cmdEl = ParseCommand(tokens[pos - 1].Value, tokens, ref pos);
                        cmdEl = TryAttachScript(tokens, ref pos, cmdEl);
                        content.Add(cmdEl);
                    }
                    else if (tokens[pos].Type == TokenType.Sub || tokens[pos].Type == TokenType.Sup)
                    {
                        var emptyRun = MakeMathRun("");
                        var scripted = TryAttachScript(tokens, ref pos, emptyRun);
                        content.Add(scripted);
                    }
                    else if (tokens[pos].Type == TokenType.LBracket || tokens[pos].Type == TokenType.RBracket)
                    {
                        var bracketText = tokens[pos].Type == TokenType.LBracket ? "[" : "]";
                        var bracketRun = MakeMathRun(bracketText);
                        pos++;
                        bracketRun = (M.Run)TryAttachScript(tokens, ref pos, bracketRun);
                        content.Add(bracketRun);
                    }
                    else
                    {
                        pos++;
                    }
                }

                var dPr = new M.DelimiterProperties();
                if (openChar != "(")
                    dPr.AppendChild(new M.BeginChar { Val = openChar });
                if (closeChar != ")")
                    dPr.AppendChild(new M.EndChar { Val = closeChar });

                var delimiter = new M.Delimiter(dPr);
                // Flatten any OfficeMath wrappers (a braced subgroup at line ~1117
                // is wrapped via WrapInOfficeMath so a script can attach to it). A
                // <m:e> must hold math runs directly — a nested <m:oMath> inside
                // <m:e> is invalid OMML and makes Word refuse to open the file.
                // ExtractChildren unwraps OfficeMath; non-wrapped nodes pass through.
                var arg = new M.Base(content.SelectMany(ExtractChildren).ToArray());
                delimiter.AppendChild(arg);
                return delimiter;
            }
            case "right":
            {
                // Orphan \right — shouldn't happen if paired with \left, just skip
                return MakeMathRun("");
            }
            case "overset":
            {
                var above = ParseBracedArg(tokens, ref pos);
                var baseArg = ParseBracedArg(tokens, ref pos);
                return new M.LimitUpper(
                    new M.LimitUpperProperties(),
                    new M.Base(ExtractChildren(baseArg)),
                    new M.Limit(ExtractChildren(above))
                );
            }
            case "underset":
            {
                var below = ParseBracedArg(tokens, ref pos);
                var baseArg = ParseBracedArg(tokens, ref pos);
                return new M.LimitLower(
                    new M.LimitLowerProperties(),
                    new M.Base(ExtractChildren(baseArg)),
                    new M.Limit(ExtractChildren(below))
                );
            }
            case "text":
            {
                // \text{...} → M.Run with normal text properties (upright, not math italic)
                var content = ParseBracedArg(tokens, ref pos);
                var text = ExtractText(content);
                var run = new M.Run(
                    new M.RunProperties(new M.NormalText()),
                    new M.Text(text) { Space = SpaceProcessingModeValues.Preserve }
                );
                return run;
            }
            case "overline":
            {
                var arg = ParseBracedArg(tokens, ref pos);
                return new M.Bar(
                    new M.BarProperties(new M.Position { Val = M.VerticalJustificationValues.Top }),
                    new M.Base(ExtractChildren(arg))
                );
            }
            case "underline":
            {
                var arg = ParseBracedArg(tokens, ref pos);
                return new M.Bar(
                    new M.BarProperties(new M.Position { Val = M.VerticalJustificationValues.Bottom }),
                    new M.Base(ExtractChildren(arg))
                );
            }
            case "hat" or "bar" or "vec" or "dot" or "ddot" or "tilde":
            {
                var accentChar = cmd switch
                {
                    "hat" => "\u0302",   // combining circumflex
                    "bar" => "\u0304",   // combining macron
                    "vec" => "\u20D7",   // combining right arrow above
                    "dot" => "\u0307",   // combining dot above
                    "ddot" => "\u0308",  // combining diaeresis
                    "tilde" => "\u0303", // combining tilde
                    _ => "\u0302"
                };
                var arg = ParseBracedArg(tokens, ref pos);
                return new M.Accent(
                    new M.AccentProperties(new M.AccentChar { Val = accentChar }),
                    new M.Base(ExtractChildren(arg))
                );
            }
            case "lim" or "sin" or "cos" or "tan" or "log" or "ln" or "exp" or "min" or "max"
                or "sup" or "inf" or "det" or "gcd" or "dim" or "ker" or "hom" or "deg"
                or "arg" or "sec" or "csc" or "cot" or "sinh" or "cosh" or "tanh":
            {
                // Function names: render upright (non-italic) using M.NormalText
                var funcRun = new M.Run(
                    new M.RunProperties(new M.NormalText()),
                    new M.Text(cmd) { Space = SpaceProcessingModeValues.Preserve }
                );

                // For \lim, check for sub/sup to create nary-like limLow structure
                if (cmd == "lim" && pos < tokens.Count && tokens[pos].Type == TokenType.Sub)
                {
                    pos++;
                    var subArg = ParseSingleArg(tokens, ref pos);
                    return new M.LimitLower(
                        new M.LimitLowerProperties(),
                        new M.Base(funcRun),
                        new M.Limit(ExtractChildren(subArg))
                    );
                }
                return funcRun;
            }
            case "binom":
            {
                var top = ParseBracedArg(tokens, ref pos);
                var bottom = ParseBracedArg(tokens, ref pos);
                // Binomial = parenthesized fraction with no bar
                var frac = new M.Fraction(
                    new M.FractionProperties(new M.FractionType { Val = M.FractionTypeValues.NoBar }),
                    new M.Numerator(ExtractChildren(top)),
                    new M.Denominator(ExtractChildren(bottom))
                );
                var delimiter = new M.Delimiter(new M.DelimiterProperties());
                delimiter.AppendChild(new M.Base(frac));
                return delimiter;
            }
            case "mathbf" or "mathrm" or "mathit" or "mathbb" or "mathcal" or "boldsymbol":
            {
                var arg = ParseBracedArg(tokens, ref pos);
                var text = ExtractText(arg);
                var style = cmd switch
                {
                    "mathbf" => M.StyleValues.Bold,
                    "boldsymbol" => M.StyleValues.BoldItalic,
                    "mathrm" => M.StyleValues.Plain,
                    "mathit" => M.StyleValues.Italic,
                    _ => M.StyleValues.Plain
                };
                if (cmd is "mathbb" or "mathcal")
                {
                    var scriptVal = cmd == "mathbb"
                        ? M.ScriptValues.DoubleStruck
                        : M.ScriptValues.Script;
                    // BUG-R8A(BUG4): m:scr (script alphabet) and m:sty
                    // (weight/posture) are orthogonal OMML axes. A source run may
                    // carry both (e.g. <m:scr m:val="double-struck"/><m:sty
                    // m:val="bi"/>), which dumps to a composed \mathbb{\boldsymbol{R}}.
                    // Read the inner style command's m:sty off the parsed arg and
                    // carry it, instead of hardcoding Plain, so the bold/italic/
                    // bold-italic weight survives the round-trip alongside the
                    // script alphabet.
                    var innerSty = (arg as M.Run)?.GetFirstChild<M.RunProperties>()
                        ?.GetFirstChild<M.Style>()?.Val?.Value
                        ?? M.StyleValues.Plain;
                    var rPr = new M.RunProperties(
                        new M.Script { Val = scriptVal },
                        new M.Style { Val = innerSty }
                    );
                    return new M.Run(
                        rPr,
                        new M.Text(text) { Space = SpaceProcessingModeValues.Preserve }
                    );
                }
                return new M.Run(
                    new M.RunProperties(new M.Style { Val = style }),
                    new M.Text(text) { Space = SpaceProcessingModeValues.Preserve }
                );
            }
            case "sum" or "int" or "iint" or "iiint" or "prod" or "coprod" or "bigcup" or "bigcap":
            {
                var naryChar = cmd switch
                {
                    "sum" => "∑",
                    "int" => "∫",
                    "iint" => "∬",
                    "iiint" => "∭",
                    "prod" => "∏",
                    "coprod" => "∐",
                    "bigcup" => "⋃",
                    "bigcap" => "⋂",
                    _ => "∑"
                };
                var naryProps = new M.NaryProperties(new M.AccentChar { Val = naryChar });

                // Parse optional sub and sup limits (they come as _{}^{} after the command)
                OpenXmlElement? subArg = null;
                OpenXmlElement? supArg = null;

                if (pos < tokens.Count && tokens[pos].Type == TokenType.Sub)
                {
                    pos++;
                    subArg = ParseSingleArg(tokens, ref pos);
                }
                if (pos < tokens.Count && tokens[pos].Type == TokenType.Sup)
                {
                    pos++;
                    supArg = ParseSingleArg(tokens, ref pos);
                }

                // Hide sub/sup limits when not provided to avoid empty boxes
                if (subArg == null)
                    naryProps.AppendChild(new M.HideSubArgument { Val = M.BooleanValues.True });
                if (supArg == null)
                    naryProps.AppendChild(new M.HideSuperArgument { Val = M.BooleanValues.True });

                // Parse the base expression (next arg or next element).
                // Skip a pure-whitespace Text token between the limits and the
                // base: \sum_{n=1}^{\infty} \frac{1}{n^s} tokenises the space
                // before \frac as its own Text(" "). Without this skip,
                // ParseSingleArg would consume the space and return an empty
                // run, leaving \frac stranded as a sibling outside <m:e/>.
                while (pos < tokens.Count
                    && tokens[pos].Type == TokenType.Text
                    && string.IsNullOrWhiteSpace(tokens[pos].Value))
                {
                    pos++;
                }
                OpenXmlElement baseArg;
                if (pos < tokens.Count && tokens[pos].Type == TokenType.LBrace)
                {
                    baseArg = ParseBracedArg(tokens, ref pos);
                }
                else if (pos < tokens.Count && (tokens[pos].Type == TokenType.Text || tokens[pos].Type == TokenType.Command))
                {
                    baseArg = ParseSingleArg(tokens, ref pos);
                }
                else
                {
                    baseArg = MakeMathRun("");
                }

                // A sub/superscript that immediately follows the n-ary operand
                // binds to THAT operand, inside the nary body — not to the whole
                // n-ary. \sum_{i=1}^n i^2 is Σ of i², i.e. the ^2 makes sSup(i,2)
                // and stays inside <m:e>. Attach it here so the caller's
                // TryAttachScript (line ~765) sees no trailing script and the
                // nary is not re-wrapped as (Σi)². The nary's own _{}^{} limits
                // were already consumed above; only the operand's script remains.
                baseArg = TryAttachScript(tokens, ref pos, baseArg);

                return new M.Nary(
                    naryProps,
                    new M.SubArgument(subArg != null ? ExtractChildren(subArg) : Array.Empty<OpenXmlElement>()),
                    new M.SuperArgument(supArg != null ? ExtractChildren(supArg) : Array.Empty<OpenXmlElement>()),
                    new M.Base(ExtractChildren(baseArg))
                );
            }
            case "cancel":
            case "bcancel":
            case "xcancel":
            case "cancelto":
            {
                // Cancel/strikethrough: use m:borderBox with strike properties
                // \cancelto{value}{expr} takes two args — we discard the target value
                if (cmd is "cancelto")
                    ParseBracedArg(tokens, ref pos); // skip target value
                var cancelArg = ParseBracedArg(tokens, ref pos);
                var bbPr = new M.BorderBoxProperties(
                    new M.HideTop { Val = M.BooleanValues.True },
                    new M.HideBottom { Val = M.BooleanValues.True },
                    new M.HideLeft { Val = M.BooleanValues.True },
                    new M.HideRight { Val = M.BooleanValues.True }
                );
                if (cmd is "cancel" or "cancelto")
                    bbPr.AppendChild(new M.StrikeTopLeftToBottomRight { Val = M.BooleanValues.True });
                else if (cmd is "bcancel")
                    bbPr.AppendChild(new M.StrikeBottomLeftToTopRight { Val = M.BooleanValues.True });
                else // xcancel — both diagonals
                {
                    bbPr.AppendChild(new M.StrikeTopLeftToBottomRight { Val = M.BooleanValues.True });
                    bbPr.AppendChild(new M.StrikeBottomLeftToTopRight { Val = M.BooleanValues.True });
                }
                return new M.BorderBox(bbPr, new M.Base(ExtractChildren(cancelArg)));
            }
            case "boxed":
            {
                // \boxed{expr} → m:borderBox (all four sides)
                var arg = ParseBracedArg(tokens, ref pos);
                return new M.BorderBox(
                    new M.BorderBoxProperties(),
                    new M.Base(ExtractChildren(arg))
                );
            }
            case "underbrace":
            {
                // \underbrace{expr}_{label} → m:groupChr with ⏟ below
                var arg = ParseBracedArg(tokens, ref pos);
                var groupChr = new M.GroupChar(
                    new M.GroupCharProperties(
                        new M.AccentChar { Val = "\u23DF" },
                        new M.Position { Val = M.VerticalJustificationValues.Bottom }
                    ),
                    new M.Base(ExtractChildren(arg))
                );
                // Check for subscript label
                if (pos < tokens.Count && tokens[pos].Type == TokenType.Sub)
                {
                    pos++;
                    var label = ParseSingleArg(tokens, ref pos);
                    return new M.LimitLower(
                        new M.LimitLowerProperties(),
                        new M.Base(groupChr),
                        new M.Limit(ExtractChildren(label))
                    );
                }
                return groupChr;
            }
            case "overbrace":
            {
                // \overbrace{expr}^{label} → m:groupChr with ⏞ above
                var arg = ParseBracedArg(tokens, ref pos);
                var groupChr = new M.GroupChar(
                    new M.GroupCharProperties(
                        new M.AccentChar { Val = "\u23DE" },
                        new M.Position { Val = M.VerticalJustificationValues.Top }
                    ),
                    new M.Base(ExtractChildren(arg))
                );
                // Check for superscript label
                if (pos < tokens.Count && tokens[pos].Type == TokenType.Sup)
                {
                    pos++;
                    var label = ParseSingleArg(tokens, ref pos);
                    return new M.LimitUpper(
                        new M.LimitUpperProperties(),
                        new M.Base(groupChr),
                        new M.Limit(ExtractChildren(label))
                    );
                }
                return groupChr;
            }
            case "color":
            case "textcolor":
            {
                // \color{red}{expr} / \textcolor{red}{expr} → preserve math structure, apply color to all runs
                var colorArg = ParseBracedArg(tokens, ref pos);
                var colorName = ExtractText(colorArg);
                var contentArg = ParseBracedArg(tokens, ref pos);
                var colorHex = NamedColorToHex(colorName);
                ApplyColorToRuns(contentArg, colorHex);
                return contentArg;
            }
            case "pmod":
            {
                // \pmod{n} → (mod n) with upright "mod"
                var arg = ParseBracedArg(tokens, ref pos);
                var modRun = new M.Run(
                    new M.RunProperties(new M.NormalText()),
                    new M.Text("mod") { Space = SpaceProcessingModeValues.Preserve }
                );
                var spaceRun = MakeMathRun("\u2003");
                var baseChildren = new List<OpenXmlElement> { modRun, spaceRun };
                baseChildren.AddRange(ExtractChildren(arg));
                var delimiter = new M.Delimiter(
                    new M.DelimiterProperties(),
                    new M.Base(baseChildren)
                );
                return delimiter;
            }
            case "bmod":
            {
                // \bmod → upright "mod" (binary operator form)
                return new M.Run(
                    new M.RunProperties(new M.NormalText()),
                    new M.Text("\u2003mod\u2003") { Space = SpaceProcessingModeValues.Preserve }
                );
            }
            case "arcsin" or "arccos" or "arctan" or "arccot" or "arcsec" or "arccsc":
            {
                // Arc-trig functions: render upright like \sin, \cos, etc.
                var funcRun = new M.Run(
                    new M.RunProperties(new M.NormalText()),
                    new M.Text(cmd) { Space = SpaceProcessingModeValues.Preserve }
                );
                return funcRun;
            }
            case "operatorname":
            {
                // \operatorname{name} → upright function name with limit support
                var arg = ParseBracedArg(tokens, ref pos);
                var opText = ExtractText(arg);
                OpenXmlElement result = new M.Run(
                    new M.RunProperties(new M.NormalText()),
                    new M.Text(opText) { Space = SpaceProcessingModeValues.Preserve }
                );
                // Parse sub/superscript limits (like \lim)
                OpenXmlElement? subArg = null, supArg = null;
                for (var i = 0; i < 2 && pos < tokens.Count; i++)
                {
                    if (tokens[pos].Type == TokenType.Sub && subArg == null)
                    { pos++; subArg = ParseSingleArg(tokens, ref pos); }
                    else if (tokens[pos].Type == TokenType.Sup && supArg == null)
                    { pos++; supArg = ParseSingleArg(tokens, ref pos); }
                    else break;
                }
                if (subArg != null)
                    result = new M.LimitLower(new M.LimitLowerProperties(),
                        new M.Base(result), new M.Limit(ExtractChildren(subArg)));
                if (supArg != null)
                    result = new M.LimitUpper(new M.LimitUpperProperties(),
                        new M.Base(result), new M.Limit(ExtractChildren(supArg)));
                return result;
            }

            default:
                // Unknown command: render as text with backslash
                return MakeMathRun($"\\{cmd}");
        }
    }

    private static OpenXmlElement ParseBracedArg(List<Token> tokens, ref int pos)
    {
        if (pos < tokens.Count && tokens[pos].Type == TokenType.LBrace)
        {
            pos++;
            var inner = ParseGroup(tokens, ref pos, true);
            return inner.Count == 1 ? inner[0] : WrapInOfficeMath(inner);
        }
        return ParseSingleArg(tokens, ref pos);
    }

    private static OpenXmlElement ParseMatrix(string envName, List<Token> tokens, ref int pos)
    {
        var rows = new List<List<List<OpenXmlElement>>>();
        var currentRow = new List<List<OpenXmlElement>>();
        var currentCell = new List<OpenXmlElement>();

        while (pos < tokens.Count)
        {
            // Check for \end{envName}
            if (tokens[pos].Type == TokenType.Command && tokens[pos].Value == "end")
            {
                pos++;
                // Skip {envName}
                if (pos < tokens.Count && tokens[pos].Type == TokenType.LBrace)
                {
                    pos++;
                    while (pos < tokens.Count && tokens[pos].Type != TokenType.RBrace) pos++;
                    if (pos < tokens.Count) pos++;
                }
                break;
            }

            if (tokens[pos].Type == TokenType.RowSep)
            {
                pos++;
                currentRow.Add(currentCell);
                rows.Add(currentRow);
                currentRow = new List<List<OpenXmlElement>>();
                currentCell = new List<OpenXmlElement>();
                continue;
            }

            if (tokens[pos].Type == TokenType.ColSep)
            {
                pos++;
                currentRow.Add(currentCell);
                currentCell = new List<OpenXmlElement>();
                continue;
            }

            // Parse element into current cell (same logic as ParseGroup)
            if (tokens[pos].Type == TokenType.Text)
            {
                var el = MakeMathRun(tokens[pos].Value);
                pos++;
                var result = TryAttachScript(tokens, ref pos, el);
                currentCell.Add(result);
            }
            else if (tokens[pos].Type == TokenType.LBrace)
            {
                pos++;
                var inner = ParseGroup(tokens, ref pos, true);
                var grouped = WrapInOfficeMath(inner);
                grouped = TryAttachScript(tokens, ref pos, grouped);
                currentCell.Add(grouped);
            }
            else if (tokens[pos].Type == TokenType.Command)
            {
                pos++;
                var cmdEl = ParseCommand(tokens[pos - 1].Value, tokens, ref pos);
                cmdEl = TryAttachScript(tokens, ref pos, cmdEl);
                currentCell.Add(cmdEl);
            }
            else if (tokens[pos].Type == TokenType.Sub || tokens[pos].Type == TokenType.Sup)
            {
                var emptyRun = MakeMathRun("");
                var scripted = TryAttachScript(tokens, ref pos, emptyRun);
                currentCell.Add(scripted);
            }
            else
            {
                pos++;
            }
        }

        // Add last cell/row
        if (currentCell.Count > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentCell);
            rows.Add(currentRow);
        }

        // Build OMML Matrix
        var matrix = new M.Matrix(new M.MatrixProperties());
        foreach (var row in rows)
        {
            var mr = new M.MatrixRow();
            foreach (var cell in row)
            {
                // BUG-R10A(BUG4): the tokenizer keeps the spaces that surround
                // the `&` column / `\\` row separators inside the cell's text
                // token (`\begin{matrix} 1 & 2 \\ …` → cell tokens " 1 ", " 2 "),
                // so each cell's <m:t> round-tripped as " 1 "/" 2 " — injecting
                // leading/trailing spaces the source never had. Trim only the
                // cell's outer boundary whitespace (first run's leading, last
                // run's trailing); internal spaces in a multi-token cell like
                // "x + 1" live in the SAME run and are preserved.
                var cleaned = cell.Select(e => e.CloneNode(true)).ToList();
                TrimMatrixCellBoundaryWhitespace(cleaned);
                var baseEl = new M.Base(cleaned.ToArray());
                mr.AppendChild(baseEl);
            }
            matrix.AppendChild(mr);
        }

        // Wrap with delimiter based on environment
        if (envName == "matrix")
            return matrix;

        var (beginChar, endChar) = envName switch
        {
            "pmatrix" => ("(", ")"),
            "bmatrix" => ("[", "]"),
            "Bmatrix" => ("{", "}"),
            "vmatrix" => ("|", "|"),
            "cases" => ("{", ""),
            _ => ("(", ")")
        };

        var dPr = new M.DelimiterProperties();
        if (beginChar != "(")
            dPr.AppendChild(new M.BeginChar { Val = beginChar });
        if (endChar != ")")
            dPr.AppendChild(new M.EndChar { Val = endChar });

        // For cases: left-align cells
        if (envName == "cases")
        {
            // Set column justification to left for the matrix
            var mPr = matrix.ChildElements.FirstOrDefault(e => e.LocalName == "mPr") as M.MatrixProperties;
            if (mPr != null)
            {
                var colCount = rows.Max(r => r.Count);
                var mcs = new M.MatrixColumns();
                for (int ci = 0; ci < colCount; ci++)
                {
                    mcs.AppendChild(new M.MatrixColumn(
                        new M.MatrixColumnProperties(
                            new M.MatrixColumnCount { Val = 1 },
                            new M.MatrixColumnJustification { Val = M.HorizontalAlignmentValues.Left }
                        )
                    ));
                }
                mPr.AppendChild(mcs);
            }
        }

        var delimiter = new M.Delimiter(dPr);
        delimiter.AppendChild(new M.Base(matrix));
        return delimiter;
    }

    // ==================== Helpers ====================

    /// <summary>
    /// BUG-R8A(BUG4): render an m:sty (math weight/posture) value as the
    /// matching LaTeX style command wrapping <paramref name="text"/>.
    /// m:sty: "b" → \mathbf, "i" → \mathit, "bi" → \boldsymbol, "p" → \mathrm.
    /// m:sty and m:scr (script alphabet) are orthogonal OMML axes; when a run
    /// carries both, the caller wraps this style command inside the script
    /// command (\mathbb/\mathcal) so both survive the round-trip.
    /// </summary>
    private static string WrapMathStyle(string? styVal, string text)
    {
        var escaped = EscapeLatex(text);
        return styVal switch
        {
            "b" => $"\\mathbf{{{escaped}}}",
            "bi" => $"\\boldsymbol{{{escaped}}}",
            "p" => $"\\mathrm{{{escaped}}}",
            // An explicit m:sty="i" round-trips to \mathit so the literal value
            // survives; a run with NO m:sty (styVal == null) falls through to
            // bare text — italic is the OMML math default, so the common
            // default-italic letter stays unwrapped as before.
            "i" => $"\\mathit{{{escaped}}}",
            _ => escaped
        };
    }

    private static M.Run MakeMathRun(string text)
    {
        return new M.Run(new M.Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    // BUG-R10A(BUG4): strip only the matrix cell's OUTER boundary whitespace —
    // the leading spaces on the cell's first text run and the trailing spaces
    // on its last text run. Scoped to plain <m:r><m:t> boundary runs so a cell
    // beginning/ending with a fraction, n-ary, script, or nested group is left
    // untouched, and internal spaces (e.g. "x + 1", which the tokenizer keeps in
    // a single text run) survive because only the run-edge whitespace is cut.
    private static void TrimMatrixCellBoundaryWhitespace(List<OpenXmlElement> cell)
    {
        if (cell.Count == 0) return;
        if (cell[0] is M.Run firstRun)
        {
            var t = firstRun.GetFirstChild<M.Text>();
            if (t != null && t.Text != null)
                t.Text = t.Text.TrimStart();
        }
        if (cell[^1] is M.Run lastRun)
        {
            var t = lastRun.GetFirstChild<M.Text>();
            if (t != null && t.Text != null)
                t.Text = t.Text.TrimEnd();
        }
    }

    private static OpenXmlElement WrapInOfficeMath(List<OpenXmlElement> elements)
    {
        if (elements.Count == 1) return elements[0];
        var math = new M.OfficeMath();
        foreach (var e in elements)
            math.AppendChild(e.CloneNode(true));
        return math;
    }

    private static void ApplyColorToRuns(OpenXmlElement element, string colorHex)
    {
        if (element is M.Run run)
        {
            var rPr = run.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.RunProperties>();
            if (rPr == null)
            {
                rPr = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
                // BUG-R8A(BUG1): OMML CT_R order is m:rPr?, (w:rPr|m:ctrlPr)?, m:t*.
                // A math run carrying a math RunProperties (m:rPr, e.g. from
                // \boldsymbol/\mathbf) must keep m:rPr first; insert w:rPr AFTER it,
                // not at index 0 (which inverted the order → schema-invalid m:rPr
                // flagged as an unexpected child).
                var mathRPr = run.GetFirstChild<M.RunProperties>();
                if (mathRPr != null)
                    run.InsertAfter(rPr, mathRPr);
                else
                    run.InsertAt(rPr, 0);
            }
            rPr.Color = new DocumentFormat.OpenXml.Wordprocessing.Color { Val = colorHex };
            return;
        }
        foreach (var child in element.ChildElements)
            ApplyColorToRuns(child, colorHex);
    }

    private static OpenXmlElement[] ExtractChildren(OpenXmlElement element)
    {
        if (element is M.OfficeMath math)
            return math.ChildElements.Select(e => e.CloneNode(true)).ToArray();
        return new[] { element.CloneNode(true) };
    }

    /// <summary>
    /// Replace every non-root <c>&lt;m:oMath&gt;</c> with its children in place.
    /// A nested oMath (one inside another math element) is invalid OMML and makes
    /// Word refuse to open the document, even though the SDK validator accepts it.
    /// Processing innermost-first keeps reparenting well-defined.
    /// </summary>
    private static void FlattenNestedOfficeMath(OpenXmlElement root)
    {
        // Descendants excludes root, so every hit here is by definition nested.
        // Reverse document order → deepest/last handled first.
        var nested = root.Descendants<M.OfficeMath>().Reverse().ToList();
        foreach (var om in nested)
        {
            var parent = om.Parent;
            if (parent == null) continue;
            foreach (var child in om.ChildElements.ToList())
            {
                child.Remove();
                parent.InsertBefore(child, om);
            }
            om.Remove();
        }
    }

    private static string NamedColorToHex(string color)
    {
        // Strip # prefix if present, return 6-digit hex
        color = color.Trim().TrimStart('#');
        if (color.Length == 6 && color.All(c => "0123456789ABCDEFabcdef".Contains(c)))
            return color.ToUpperInvariant();
        return color.ToLowerInvariant() switch
        {
            "red" => "FF0000",
            "blue" => "0000FF",
            "green" => "008000",
            "black" => "000000",
            "white" => "FFFFFF",
            "orange" => "FF8C00",
            "purple" => "800080",
            "brown" => "8B4513",
            "gray" or "grey" => "808080",
            "cyan" => "00FFFF",
            "magenta" => "FF00FF",
            "yellow" => "FFD700",
            "darkred" => "8B0000",
            "darkblue" => "00008B",
            "darkgreen" => "006400",
            "lightblue" => "ADD8E6",
            "lightgreen" => "90EE90",
            "pink" => "FFC0CB",
            "teal" => "008080",
            "navy" => "000080",
            "maroon" => "800000",
            "olive" => "808000",
            _ => "000000"
        };
    }

    private static string ExtractText(OpenXmlElement element)
    {
        if (element is M.Run run)
            return run.ChildElements.FirstOrDefault(e => e.LocalName == "t")?.InnerText ?? "";
        if (element is M.OfficeMath oMath)
            return string.Concat(oMath.ChildElements.Select(ExtractText));
        return element.InnerText;
    }

    private static string ArgToLatex(OpenXmlElement? arg)
    {
        if (arg == null) return "";
        // CONSISTENCY(formula-space-dedup): see JoinChildren — same boundary
        // dedupe must apply inside arg containers (Numerator/Denominator/e/
        // sub/sup) or the per-round-trip space accumulation reappears one
        // level down (BUG-R3-1).
        return JoinChildren(arg);
    }

    private static string ArgToReadable(OpenXmlElement? arg)
    {
        if (arg == null) return "";
        return string.Concat(arg.ChildElements.Select(ToReadableText));
    }

    private static bool IsLaTeXHex(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Length is not (3 or 6 or 8)) return false;
        foreach (var c in s)
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))) return false;
        return true;
    }

    private static string EscapeLatex(string text)
    {
        // Reverse-map special Unicode symbols back to LaTeX commands
        foreach (var (symbol, cmd) in SymbolToCommandMap)
        {
            text = text.Replace(symbol, cmd);
        }
        return text;
    }

    private static string NaryCharToCommand(string chr) => chr switch
    {
        "∑" => "\\sum",
        "∫" => "\\int",
        "∬" => "\\iint",
        "∭" => "\\iiint",
        "∏" => "\\prod",
        "∐" => "\\coprod",
        "⋃" => "\\bigcup",
        "⋂" => "\\bigcap",
        _ => chr
    };

    private static string? CommandToSymbol(string cmd) => cmd switch
    {
        // Arrows
        "rightarrow" => "→",
        "leftarrow" => "←",
        "uparrow" => "↑",
        "downarrow" => "↓",
        "Rightarrow" => "⇒",
        "Leftarrow" => "⇐",
        "leftrightarrow" => "↔",
        "Leftrightarrow" => "⇔",
        "rightleftharpoons" => "⇌",
        "to" => "→",
        "gets" => "←",
        "mapsto" => "↦",
        "iff" => "⟺",
        "implies" => "⟹",
        "impliedby" => "⟸",
        // Logic
        "land" or "wedge" => "∧",
        "lor" or "vee" => "∨",
        "lnot" or "neg" => "¬",
        "mid" => "∣",
        "parallel" => "∥",
        // Operators
        "pm" => "±",
        "mp" => "∓",
        "times" => "×",
        "div" => "÷",
        "cdot" => "·",
        "ast" => "∗",
        "star" => "⋆",
        "circ" => "∘",
        "oplus" => "⊕",
        "ominus" => "⊖",
        "otimes" => "⊗",
        "odot" => "⊙",
        "bullet" => "∙",
        // Relations
        "leq" or "le" => "≤",
        "geq" or "ge" => "≥",
        "neq" or "ne" => "≠",
        "approx" => "≈",
        "equiv" => "≡",
        "sim" => "∼",
        "subset" => "⊂",
        "supset" => "⊃",
        "subseteq" => "⊆",
        "supseteq" => "⊇",
        "in" => "∈",
        "notin" => "∉",
        "forall" => "∀",
        "exists" => "∃",
        "nabla" => "∇",
        "partial" => "∂",
        "infty" => "∞",
        "triangle" => "△",
        "prime" => "′",
        "hbar" => "ℏ",
        "cdots" => "⋯",
        "ldots" => "…",
        "vdots" => "⋮",
        "ddots" => "⋱",
        // Delimiters (when used standalone, not with \left/\right)
        "langle" => "\u27E8",     // ⟨ mathematical left angle bracket
        "rangle" => "\u27E9",     // ⟩ mathematical right angle bracket
        "lceil" => "\u2308",      // ⌈ left ceiling
        "rceil" => "\u2309",      // ⌉ right ceiling
        "lfloor" => "\u230A",     // ⌊ left floor
        "rfloor" => "\u230B",     // ⌋ right floor
        "lvert" => "|",
        "rvert" => "|",
        "lVert" => "\u2016",      // ‖ double vertical line
        "rVert" => "\u2016",
        "vert" => "|",
        "Vert" => "\u2016",
        // Set notation
        "emptyset" => "∅",
        "varnothing" => "∅",
        "setminus" => "∖",
        "complement" => "∁",
        "cap" => "∩",
        "cup" => "∪",
        // Spacing
        "quad" => "\u2003",    // em space
        "qquad" => "\u2003\u2003", // double em space
        "," => "\u2009",       // thin space
        ";" => "\u2005",       // medium mathematical space
        "!" => "",             // negative thin space (approximate with nothing)
        // Greek lowercase
        "alpha" => "α",
        "beta" => "β",
        "gamma" => "γ",
        "delta" => "δ",
        "epsilon" => "ε",
        "zeta" => "ζ",
        "eta" => "η",
        "theta" => "θ",
        "iota" => "ι",
        "kappa" => "κ",
        "lambda" => "λ",
        "mu" => "μ",
        "nu" => "ν",
        "xi" => "ξ",
        "pi" => "π",
        "rho" => "ρ",
        "sigma" => "σ",
        "tau" => "τ",
        "upsilon" => "υ",
        "phi" => "φ",
        "chi" => "χ",
        "psi" => "ψ",
        "omega" => "ω",
        // Greek uppercase
        "Gamma" => "Γ",
        "Delta" => "Δ",
        "Theta" => "Θ",
        "Lambda" => "Λ",
        "Xi" => "Ξ",
        "Pi" => "Π",
        "Sigma" => "Σ",
        "Phi" => "Φ",
        "Psi" => "Ψ",
        "Omega" => "Ω",
        _ => null
    };

    // Function names written by ParseCommand's `case "lim" or "sin" or ...`
    // arm as an m:r + m:nor + literal text. Round-trip back to "\name" on the
    // OMML→LaTeX side when an upright run's payload matches one of these.
    // CONSISTENCY(formula-funcname): mirrors the list in ParseCommand.
    private static readonly HashSet<string> _uprightFunctionNames = new(StringComparer.Ordinal)
    {
        "lim", "sin", "cos", "tan", "log", "ln", "exp", "min", "max",
        "sup", "inf", "det", "gcd", "dim", "ker", "hom", "deg",
        "arg", "sec", "csc", "cot", "sinh", "cosh", "tanh"
    };

    private static readonly (string Symbol, string Command)[] SymbolToCommandMap = new[]
    {
        ("→", "\\rightarrow "), ("←", "\\leftarrow "), ("↑", "\\uparrow "), ("↓", "\\downarrow "),
        ("⇒", "\\Rightarrow "), ("⇐", "\\Leftarrow "),
        ("±", "\\pm "), ("×", "\\times "), ("÷", "\\div "), ("·", "\\cdot "),
        ("≤", "\\leq "), ("≥", "\\geq "), ("≠", "\\neq "), ("≈", "\\approx "), ("≡", "\\equiv "),
        ("∈", "\\in "), ("∀", "\\forall "), ("∃", "\\exists "), ("∞", "\\infty "), ("△", "\\triangle "),
        ("′", "\\prime "), ("ℏ", "\\hbar "), ("⇌", "\\rightleftharpoons "),
        ("α", "\\alpha "), ("β", "\\beta "), ("γ", "\\gamma "), ("δ", "\\delta "),
        ("ε", "\\epsilon "), ("θ", "\\theta "), ("λ", "\\lambda "), ("μ", "\\mu "),
        ("π", "\\pi "), ("σ", "\\sigma "), ("φ", "\\phi "), ("ω", "\\omega "),
        ("Σ", "\\Sigma "), ("Π", "\\Pi "), ("Δ", "\\Delta "), ("Ω", "\\Omega "),
    };

    // ==================== Unicode subscript/superscript ====================

    private static string ToUnicodeSubscript(string text)
    {
        return string.Concat(text.Select(c => c switch
        {
            '0' => '₀', '1' => '₁', '2' => '₂', '3' => '₃', '4' => '₄',
            '5' => '₅', '6' => '₆', '7' => '₇', '8' => '₈', '9' => '₉',
            '+' => '₊', '-' => '₋', '=' => '₌', '(' => '₍', ')' => '₎',
            'a' => 'ₐ', 'e' => 'ₑ', 'i' => 'ᵢ', 'n' => 'ₙ', 'o' => 'ₒ',
            'r' => 'ᵣ', 'x' => 'ₓ',
            _ => c
        }));
    }

    private static string ToUnicodeSuperscript(string text)
    {
        return string.Concat(text.Select(c => c switch
        {
            '0' => '⁰', '1' => '¹', '2' => '²', '3' => '³', '4' => '⁴',
            '5' => '⁵', '6' => '⁶', '7' => '⁷', '8' => '⁸', '9' => '⁹',
            '+' => '⁺', '-' => '⁻', '=' => '⁼', '(' => '⁽', ')' => '⁾',
            'n' => 'ⁿ', 'i' => 'ⁱ',
            _ => c
        }));
    }
}

/// <summary>
/// Exception thrown when FormulaParser fails to parse a LaTeX formula.
/// </summary>
internal class FormulaParseException : Exception
{
    public FormulaParseException(string message)
        : base(message) { }

    public FormulaParseException(string message, Exception innerException)
        : base(message, innerException) { }
}
