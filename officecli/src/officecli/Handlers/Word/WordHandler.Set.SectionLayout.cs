// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeCli.Core;

namespace OfficeCli.Handlers;

public partial class WordHandler
{
    /// <summary>
    /// Set section-level layout properties: Columns, SectionType.
    /// Called from TrySetDocSetting for keys with recognized prefixes.
    /// Returns true if the key was handled.
    /// </summary>
    private bool TrySetSectionLayout(string key, string value)
    {
        switch (key)
        {
            // ==================== Columns ====================
            case "columns.count":
            {
                var ccCount = ParseHelpers.SafeParseInt(value, "columns.count");
                if (ccCount < 1 || ccCount > 45)
                    throw new ArgumentException($"Invalid 'columns.count' value: '{value}'. cols must be between 1 and 45 (OOXML CT_Columns/@num MaxInclusive=45).");
                var cols = EnsureColumns();
                cols.ColumnCount = (short)ccCount;
                // No auto-stamp — see `columns` case above. equalWidth is
                // implicitly true per OOXML when no <w:col> children carry
                // explicit widths.
                return true;
            }
            // CONSISTENCY(canonical-key): 'columnSpace' is the canonical key
            // returned by Get/Query (see WordHandler.Query.cs:491); accept it
            // alongside the dotted alias so Set has parity with the read side.
            case "columns.space" or "columnspace":
            {
                var cols = EnsureColumns();
                cols.Space = ParseTwips(value).ToString();
                return true;
            }
            case "columns.equalwidth":
            {
                var cols = EnsureColumns();
                cols.EqualWidth = IsTruthy(value);
                return true;
            }
            case "columns.separator":
            {
                var cols = EnsureColumns();
                cols.Separator = IsTruthy(value);
                return true;
            }
            // colWidths / colSpaces — non-equal-width column layout. Mirrors the
            // canonical Get-side emit (separate width and space lists; see
            // WordHandler.Query.cs `colWidths` / `colSpaces`). Without this,
            // Get emitted both keys but Set silently dropped them — dump→batch
            // round-trip lost the non-equal layout.
            case "colwidths":
            case "colspaces":
            {
                var sectPr = EnsureSectionProperties();
                var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { key, value } };
                ApplySectionColumnWidthsSpaces(props, sectPr);
                return true;
            }

            // ==================== Title page / page numbering ====================
            // CONSISTENCY(section-layout-fallback): SetSectionPath (/section[N]) and
            // TrySetSectionLayout (/) must accept the same property vocabulary on the
            // body-level sectPr; titlePage/pageNumFmt/pageStart historically lived only
            // in the per-section dispatch (Set.Dispatch.cs:664-715) and slipped past the
            // root-path fallback. Logic mirrors the dispatch cases verbatim.
            case "titlepage" or "titlepg":
            {
                var sectPr = EnsureSectionProperties();
                if (IsTruthy(value))
                {
                    if (sectPr.GetFirstChild<TitlePage>() == null)
                        InsertSectPrChildInOrder(sectPr, new TitlePage());
                }
                else
                {
                    sectPr.RemoveAllChildren<TitlePage>();
                }
                return true;
            }
            case "pagenumfmt" or "pagenumberformat" or "pagenumberfmt":
            {
                var sectPr = EnsureSectionProperties();
                var pgNum = sectPr.GetFirstChild<PageNumberType>();
                if (pgNum == null)
                {
                    pgNum = new PageNumberType();
                    InsertSectPrChildInOrder(sectPr, pgNum);
                }
                pgNum.Format = ParseNumberFormat(value);
                return true;
            }
            case "pgborders" or "pageborders":
            {
                // R9-5: shorthand to materialize all four sides on a sectPr.
                // Accepts:
                //   "none"        — strip pgBorders entirely
                //   "box"         — single 4pt thin solid on top/left/bottom/right
                // Borders are emitted in CT_PageBorders schema order
                // (top, left, bottom, right) so consumers picking up the section
                // see the standard 4-sided layout.
                var sectPr = EnsureSectionProperties();
                sectPr.RemoveAllChildren<PageBorders>();
                var lower = value.ToLowerInvariant().Trim();
                if (lower == "none" || lower == "off" || lower == "false")
                    return true;
                if (lower != "box")
                    throw new ArgumentException(
                        $"Invalid pgBorders value: '{value}'. Valid: box, none, or per-side keys pgBorders.<top|left|bottom|right>=STYLE[;SIZE[;COLOR[;SPACE]]] and pgBorders.offsetFrom=page|text.");
                var pb = new PageBorders
                {
                    TopBorder    = new TopBorder    { Val = BorderValues.Single, Size = 4U, Color = "auto", Space = 24U },
                    LeftBorder   = new LeftBorder   { Val = BorderValues.Single, Size = 4U, Color = "auto", Space = 24U },
                    BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = "auto", Space = 24U },
                    RightBorder  = new RightBorder  { Val = BorderValues.Single, Size = 4U, Color = "auto", Space = 24U },
                };
                InsertSectPrChildInOrder(sectPr, pb);
                return true;
            }
            // Per-side page border detail + position. Mirrors the paragraph/table
            // per-side border vocabulary (pbdr.top / border.top with the
            // STYLE;SIZE;COLOR;SPACE value form) so the dump→batch round-trip
            // preserves source line style/weight/color/spacing instead of
            // collapsing to the hardcoded box default. Each key materialises (or
            // overlays onto an existing) <w:pgBorders> child; offsetFrom sets the
            // position attribute.
            case "pgborders.top" or "pgborders.left"
                or "pgborders.bottom" or "pgborders.right":
            {
                var sectPr = EnsureSectionProperties();
                var pb = EnsurePageBorders(sectPr);
                var (style, size, color, space) = ParseBorderValue(value);
                var sf = ParseBorderShadowFrame(value);
                switch (key)
                {
                    case "pgborders.top":    pb.TopBorder    = MakeBorder<TopBorder>(style, size, color, space, sf.shadow, sf.frame); break;
                    case "pgborders.left":   pb.LeftBorder   = MakeBorder<LeftBorder>(style, size, color, space, sf.shadow, sf.frame); break;
                    case "pgborders.bottom": pb.BottomBorder = MakeBorder<BottomBorder>(style, size, color, space, sf.shadow, sf.frame); break;
                    case "pgborders.right":  pb.RightBorder  = MakeBorder<RightBorder>(style, size, color, space, sf.shadow, sf.frame); break;
                }
                return true;
            }
            case "pgborders.offsetfrom":
            {
                var sectPr = EnsureSectionProperties();
                var pb = EnsurePageBorders(sectPr);
                pb.OffsetFrom = value.ToLowerInvariant().Trim() switch
                {
                    "page" => PageBorderOffsetValues.Page,
                    "text" => PageBorderOffsetValues.Text,
                    _ => throw new ArgumentException(
                        $"Invalid pgBorders.offsetFrom value: '{value}'. Valid: page, text.")
                };
                return true;
            }
            // BUG-DUMP-R44-5: zOrder (front/back — border in front of vs behind
            // text) and display (allPages/firstPage/notFirstPage — which pages the
            // page border renders on). Both are meaning-changing and were dropped
            // on round-trip; mirror offsetFrom's get-or-create overlay.
            case "pgborders.zorder":
            {
                var sectPr = EnsureSectionProperties();
                var pb = EnsurePageBorders(sectPr);
                pb.ZOrder = value.ToLowerInvariant().Trim() switch
                {
                    "front" => PageBorderZOrderValues.Front,
                    "back" => PageBorderZOrderValues.Back,
                    _ => throw new ArgumentException(
                        $"Invalid pgBorders.zOrder value: '{value}'. Valid: front, back.")
                };
                return true;
            }
            case "pgborders.display":
            {
                var sectPr = EnsureSectionProperties();
                var pb = EnsurePageBorders(sectPr);
                pb.Display = value.ToLowerInvariant().Trim() switch
                {
                    "allpages" => PageBorderDisplayValues.AllPages,
                    "firstpage" => PageBorderDisplayValues.FirstPage,
                    "notfirstpage" => PageBorderDisplayValues.NotFirstPage,
                    _ => throw new ArgumentException(
                        $"Invalid pgBorders.display value: '{value}'. Valid: allPages, firstPage, notFirstPage.")
                };
                return true;
            }
            case "direction" or "dir" or "bidi":
            {
                // CONSISTENCY(section-layout-fallback): mirrors the per-section
                // dispatch case in Set.Dispatch.cs. <w:bidi/> in sectPr flips
                // page direction for Arabic / Hebrew layouts.
                var sectPr = EnsureSectionProperties();
                sectPr.RemoveAllChildren<BiDi>();
                if (ParseDirectionRtl(value)) InsertSectPrChildInOrder(sectPr, new BiDi());
                return true;
            }
            case "rtlgutter":
            {
                // <w:rtlGutter/> places the gutter (binding margin) on the right
                // side, used in conjunction with RTL page layout (Arabic/Hebrew).
                var sectPr = EnsureSectionProperties();
                sectPr.RemoveAllChildren<GutterOnRight>();
                if (IsTruthy(value))
                    InsertSectPrChildInOrder(sectPr, new GutterOnRight());
                return true;
            }
            // BUG-DUMP11-03: <w:noEndnote/> on/off toggle — when present the
            // section's endnote collection is suppressed. Bare element, no val.
            case "noendnote":
            {
                var sectPr = EnsureSectionProperties();
                sectPr.RemoveAllChildren<NoEndnote>();
                if (IsTruthy(value))
                    InsertSectPrChildInOrder(sectPr, new NoEndnote());
                return true;
            }
            // BUG-DUMP11-01: w:pgNumType chapter-numbering attributes —
            // chapStyle = heading level (1-9) used for chapter prefix,
            // chapSep = separator between chapter and page (hyphen, period,
            // colon, emDash, enDash). Mirrors pageNumFmt/pageStart cases.
            case "chapstyle":
            {
                var sectPr = EnsureSectionProperties();
                var pgNum = sectPr.GetFirstChild<PageNumberType>();
                if (pgNum == null)
                {
                    pgNum = new PageNumberType();
                    InsertSectPrChildInOrder(sectPr, pgNum);
                }
                if (!byte.TryParse(value, out var lvl) || lvl < 1 || lvl > 9)
                    throw new ArgumentException(
                        $"Invalid chapStyle value: '{value}'. Must be 1-9 (heading level).");
                pgNum.ChapterStyle = lvl;
                return true;
            }
            case "chapsep":
            {
                var sectPr = EnsureSectionProperties();
                var pgNum = sectPr.GetFirstChild<PageNumberType>();
                if (pgNum == null)
                {
                    pgNum = new PageNumberType();
                    InsertSectPrChildInOrder(sectPr, pgNum);
                }
                pgNum.ChapterSeparator = value.ToLowerInvariant() switch
                {
                    "hyphen" or "-" => ChapterSeparatorValues.Hyphen,
                    "period" or "." => ChapterSeparatorValues.Period,
                    "colon" or ":" => ChapterSeparatorValues.Colon,
                    "emdash" or "—" => ChapterSeparatorValues.EmDash,
                    "endash" or "–" => ChapterSeparatorValues.EnDash,
                    _ => throw new ArgumentException(
                        $"Invalid chapSep value: '{value}'. Valid: hyphen, period, colon, emDash, enDash.")
                };
                return true;
            }
            case "pagestart" or "pagenumberstart" or "pagenumstart":
            {
                var sectPr = EnsureSectionProperties();
                var lower = value.ToLowerInvariant();
                if (lower is "none" or "off" or "false" or "auto")
                {
                    sectPr.RemoveAllChildren<PageNumberType>();
                }
                else
                {
                    var startN = ParseHelpers.SafeParseInt(value, "pageStart");
                    if (startN < 0)
                        throw new ArgumentException("pageStart must be a non-negative integer.");
                    var pgNum = sectPr.GetFirstChild<PageNumberType>();
                    if (pgNum == null)
                    {
                        pgNum = new PageNumberType();
                        InsertSectPrChildInOrder(sectPr, pgNum);
                    }
                    pgNum.Start = startN;
                }
                return true;
            }

            // ==================== Page orientation ====================
            // CONSISTENCY(section-layout-fallback): orientation/columns/lineNumbers also
            // belong on the body-level sectPr fallback path, not just per-section dispatch
            // (Set.Dispatch.cs:583-752). Logic mirrors the dispatch cases verbatim.
            case "orientation":
            {
                var sectPr = EnsureSectionProperties();
                var ps = EnsureSectPrPageSize(sectPr);
                var lower = value.ToLowerInvariant();
                if (lower != "landscape" && lower != "portrait")
                    throw new ArgumentException($"Invalid orientation: '{value}'. Valid: portrait, landscape.");
                var isLandscape = lower == "landscape";
                ps.Orient = isLandscape
                    ? PageOrientationValues.Landscape : PageOrientationValues.Portrait;
                var w = ps.Width?.Value ?? WordPageDefaults.A4WidthTwips;
                var h = ps.Height?.Value ?? WordPageDefaults.A4HeightTwips;
                if ((isLandscape && w < h) || (!isLandscape && w > h))
                {
                    ps.Width = h;
                    ps.Height = w;
                }
                return true;
            }

            // ==================== Columns (shorthand) ====================
            case "columns" or "cols" or "col":
            {
                var eqCols = EnsureColumns();
                var colParts = value.Split(',');
                if (!short.TryParse(colParts[0], out var colCount) || colCount < 1 || colCount > 45)
                    throw new ArgumentException($"Invalid 'cols' value: '{value}'. cols must be between 1 and 45 (OOXML CT_Columns/@num MaxInclusive=45), optionally followed by ',space' (e.g. '3' or '3,720').");
                eqCols.ColumnCount = (DocumentFormat.OpenXml.Int16Value)colCount;
                // Don't auto-stamp equalWidth. Per OOXML spec, equalWidth is
                // implicitly true when no <w:col> children carry explicit
                // widths — so the auto-stamp was always redundant. Leaving
                // it off lets the round-trip preserve source's no-equalWidth
                // shape (complex-textbox-test.docx, 03_filesamples_sample3).
                // Callers that want unequal columns must populate <w:col>
                // children separately or pass `columns.equalWidth=false`.
                if (colParts.Length > 1)
                    eqCols.Space = colParts[1];
                else
                    eqCols.Space ??= "720";
                eqCols.RemoveAllChildren<Column>();
                return true;
            }

            // ==================== Line numbers ====================
            case "linenumbers" or "linenumbering":
            {
                var sectPr = EnsureSectionProperties();
                var lower = value.ToLowerInvariant();
                if (lower == "none" || lower == "off" || lower == "false")
                {
                    sectPr.RemoveAllChildren<LineNumberType>();
                }
                else
                {
                    var lnNum = sectPr.GetFirstChild<LineNumberType>();
                    if (lnNum == null)
                    {
                        lnNum = new LineNumberType();
                        InsertSectPrChildInOrder(sectPr, lnNum);
                    }
                    if (int.TryParse(lower, out var countBy))
                    {
                        lnNum.CountBy = (short)countBy;
                        lnNum.Restart = LineNumberRestartValues.Continuous;
                    }
                    else
                    {
                        lnNum.CountBy = 1;
                        lnNum.Restart = lower switch
                        {
                            "continuous" => LineNumberRestartValues.Continuous,
                            "restartpage" or "page" => LineNumberRestartValues.NewPage,
                            "restartsection" or "section" => LineNumberRestartValues.NewSection,
                            _ => throw new ArgumentException(
                                $"Invalid lineNumbers value: '{value}'. Valid: continuous, restartPage, restartSection, none, or a positive integer.")
                        };
                    }
                }
                return true;
            }

            // CONSISTENCY(linenumbers-countby-independent): allow setting the
            // count interval without touching restart mode. Mirrors AddSection
            // — when no LineNumberType exists yet, auto-create with restart
            // = continuous so the countBy isn't dropped.
            case "linenumbercountby":
            {
                var sectPr = EnsureSectionProperties();
                if (!int.TryParse(value, out var ncb) || ncb < 1)
                    throw new ArgumentException(
                        $"Invalid lineNumberCountBy value: '{value}'. Must be a positive integer.");
                var lnNum = sectPr.GetFirstChild<LineNumberType>();
                if (lnNum == null)
                {
                    // BUG-DUMP-SECT-LNDIST: do NOT default Restart here. @w:restart's
                    // OOXML default is already "continuous", so stamping it explicitly
                    // fabricated a restart="continuous" attribute on dump→batch replay
                    // of a source that carried only @countBy/@distance (the readback
                    // now omits lineNumbers unless @restart is genuinely present).
                    lnNum = new LineNumberType();
                    InsertSectPrChildInOrder(sectPr, lnNum);
                }
                lnNum.CountBy = (short)ncb;
                return true;
            }

            // BUG-DUMP11-02: w:lnNumType/@w:start — first line number when
            // counting begins. Auto-create LineNumberType if absent so the
            // start value isn't dropped.
            case "linenumberstart":
            {
                var sectPr = EnsureSectionProperties();
                if (!int.TryParse(value, out var lnStart) || lnStart < 0)
                    throw new ArgumentException(
                        $"Invalid lineNumberStart value: '{value}'. Must be a non-negative integer.");
                var lnNum = sectPr.GetFirstChild<LineNumberType>();
                if (lnNum == null)
                {
                    // BUG-DUMP-SECT-LNDIST: do not default Restart — see countBy case.
                    lnNum = new LineNumberType();
                    InsertSectPrChildInOrder(sectPr, lnNum);
                }
                lnNum.Start = (short)lnStart;
                return true;
            }

            // BUG-DUMP-SECT-LNDIST: w:lnNumType/@w:distance — the gutter spacing
            // (in twips) between the line-number column and the body text. Sibling
            // attr to @start; was silently dropped on dump round-trip. Auto-create
            // LineNumberType if absent. Do NOT default Restart here — fabricating
            // restart="continuous" on a source that omitted it was BUG-DUMP-SECT-LNDIST's
            // companion regression (see the readback in WordHandler.Navigation.cs).
            case "linenumberdistance":
            {
                var sectPr = EnsureSectionProperties();
                if (!int.TryParse(value, out var lnDist) || lnDist < 0)
                    throw new ArgumentException(
                        $"Invalid lineNumberDistance value: '{value}'. Must be a non-negative integer (twips).");
                var lnNum = sectPr.GetFirstChild<LineNumberType>();
                if (lnNum == null)
                {
                    lnNum = new LineNumberType();
                    InsertSectPrChildInOrder(sectPr, lnNum);
                }
                lnNum.Distance = lnDist.ToString();
                return true;
            }

            // Bare `type` / `break` at the body-level path is by-design unsupported:
            // `/` refers to the final (body-level) section, which has no break type —
            // the break only makes sense between mid-doc sections. Intercept here so
            // users get an actionable error instead of the generic UNSUPPORTED.
            case "type" or "break":
            {
                throw new ArgumentException(
                    "'type'/'break' only applies to mid-document sections (/section[N]). " +
                    "The body-level path (/) refers to the final section which has no break type. " +
                    "Use: officecli set doc.docx /section[N] --prop type=...");
            }

            // ==================== Vertical Text Alignment On Page ====================
            // BUG-DUMP6-03: w:vAlign in sectPr — top / center / bottom / both.
            // Schema enum is VerticalJustificationValues.
            case "valign":
            {
                var sectPr = EnsureSectionProperties();
                sectPr.RemoveAllChildren<VerticalTextAlignmentOnPage>();
                var lower = value.ToLowerInvariant().Trim();
                if (lower is "none" or "off" or "false")
                    return true;
                var enumVal = lower switch
                {
                    "top" => VerticalJustificationValues.Top,
                    "center" or "centre" or "middle" => VerticalJustificationValues.Center,
                    "bottom" => VerticalJustificationValues.Bottom,
                    "both" => VerticalJustificationValues.Both,
                    _ => throw new ArgumentException(
                        $"Invalid vAlign value: '{value}'. Valid: top, center, bottom, both, none.")
                };
                InsertSectPrChildInOrder(sectPr, new VerticalTextAlignmentOnPage { Val = enumVal });
                return true;
            }

            // ==================== Text Direction (section page text flow) ====================
            // BUG-DUMP-SECT-TEXTDIR: w:textDirection in sectPr — East-Asian
            // vertical page text flow (tbRl, etc.). Distinct from the cell-level
            // textDirection in tcPr. ST_TextDirection enum.
            case "textdirection":
            {
                var sectPr = EnsureSectionProperties();
                sectPr.RemoveAllChildren<TextDirection>();
                var lower = value.ToLowerInvariant().Trim();
                if (lower is "none" or "off" or "false")
                    return true;
                InsertSectPrChildInOrder(sectPr, new TextDirection { Val = ParseSectionTextDirection(value) });
                return true;
            }

            // ==================== Printer paper source (BUG-DUMP-SECT-PAPERSRC) ====================
            // <w:paperSrc w:first="…" w:other="…"/> — the printer tray/bin used
            // for the first page vs the remaining pages. Both attrs are
            // CT_PaperSource ST_DecimalNumber. paperSrc sits after pgMar, before
            // pgBorders in CT_SectPr order (rank 7). Get-or-create a single
            // <w:paperSrc> so first/other accumulate onto one element.
            case "papersrc.first" or "papersrc.other":
            {
                var sectPr = EnsureSectionProperties();
                if (!int.TryParse(value, out var paperBin) || paperBin < 0 || paperBin > ushort.MaxValue)
                    throw new ArgumentException(
                        $"Invalid {key} value: '{value}'. Must be an integer 0–65535 (printer bin id).");
                var paperSrc = sectPr.GetFirstChild<PaperSource>();
                if (paperSrc == null)
                {
                    paperSrc = new PaperSource();
                    InsertSectPrChildInOrder(sectPr, paperSrc);
                }
                if (key.EndsWith("first"))
                    paperSrc.First = (ushort)paperBin;
                else
                    paperSrc.Other = (ushort)paperBin;
                return true;
            }

            // ==================== Section form protection (BUG-DUMP-SECT-FORMPROT) ====================
            // <w:formProt/> — when present (ST_OnOff, bare = true) the section's
            // contents are locked except for form fields. Lives after cols,
            // before vAlign in CT_SectPr order (rank 12). Bare on/off toggle;
            // accepts none/off/false to clear.
            case "formprot":
            {
                var sectPr = EnsureSectionProperties();
                sectPr.RemoveAllChildren<FormProtection>();
                if (IsTruthy(value))
                    InsertSectPrChildInOrder(sectPr, new FormProtection());
                return true;
            }

            // ==================== SectionType ====================
            case "section.type" or "sectiontype":
            {
                var sectPr = EnsureSectionProperties();
                var sectType = sectPr.GetFirstChild<SectionType>();
                if (sectType == null)
                {
                    sectType = new SectionType();
                    sectPr.PrependChild(sectType);
                }
                sectType.Val = value.ToLowerInvariant() switch
                {
                    "nextpage" or "next" => SectionMarkValues.NextPage,
                    "continuous" => SectionMarkValues.Continuous,
                    "evenpage" or "even" => SectionMarkValues.EvenPage,
                    "oddpage" or "odd" => SectionMarkValues.OddPage,
                    "nextcolumn" or "column" => SectionMarkValues.NextColumn,
                    _ => throw new ArgumentException($"Invalid section.type: '{value}'. Valid: nextPage, continuous, evenPage, oddPage, nextColumn")
                };
                return true;
            }

            // ==================== sectPrChange (format revision marker) ====================
            // dump→batch round-trip: EmitSection folds a source <w:sectPrChange>
            // into the `set /` op as revision.type=format + revision.author
            // (+ .date / .id). The `set /` path routes through SetDocumentProperties
            // (which has no BeginTrackChangeIfRequested decorator, unlike
            // SetSectionPath / the generic SetElement), so handle the marker here.
            // An EMPTY-snapshot <w:sectPrChange><w:sectPr/></w:sectPrChange> is
            // written — the before-snapshot is intentionally NOT reconstructed,
            // matching the empty-pPr pPrChange the paragraph round-trip produces
            // (and tblPrChange/trPrChange/tcPrChange). InsertSectPrChildInOrder
            // keeps it last per CT_SectPr regardless of which structural keys
            // land before/after it within the same set op.
            case "revision.type":
            {
                // Only the format-change kind is meaningful on a section's
                // sectPrChange. ins/del/move have no section-level OOXML shape.
                var k = value.Trim().ToLowerInvariant();
                if (k is not ("format" or "formatchange" or "" or null))
                    throw new ArgumentException(
                        $"revision.type=`{value}` is not supported on a section; "
                        + "only `format` (sectPrChange) is valid.");
                EnsureSectPrChangeMarker();
                return true;
            }
            case "revision.author":
            {
                EnsureSectPrChangeMarker().Author = value;
                return true;
            }
            case "revision.date":
            {
                if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal
                        | System.Globalization.DateTimeStyles.AssumeUniversal, out var d))
                    EnsureSectPrChangeMarker().Date = d;
                return true;
            }
            case "revision.id":
            {
                EnsureSectPrChangeMarker().Id = value;
                return true;
            }
            // BUG-DUMP-R43-9: verbatim prior-sectPr snapshot. The `set /`
            // section path has no BeginTrackChangeIfRequested decorator, so
            // restore the real before-state here (replaces the empty
            // <w:sectPr/> seeded by EnsureSectPrChangeMarker) — the same one
            // mechanism the other *PrChange hosts use.
            case "revision.beforexml":
            {
                ApplyBeforeXmlSnapshot(EnsureSectPrChangeMarker(), value);
                return true;
            }

            // ==================== Footnote / endnote numbering ====================
            // BUG-DUMP-SECT-FOOTNOTE: footnotePr.* / endnotePr.* on the body-level
            // section (`set /`). Shared with the per-section and AddSection paths.
            case "footnotepr.numfmt" or "footnotepr.numrestart" or "footnotepr.numstart" or "footnotepr.pos"
              or "endnotepr.numfmt" or "endnotepr.numrestart" or "endnotepr.numstart" or "endnotepr.pos":
            {
                var sectPr = EnsureSectionProperties();
                return TrySetFootnoteEndnoteNumProps(sectPr, key, value);
            }

            default:
                return false;
        }
    }

    private Columns EnsureColumns()
    {
        var sectPr = EnsureSectionProperties();
        return EnsureSectPrChild<Columns>(sectPr);
    }

    /// <summary>Get-or-create the body sectPr's <w:sectPrChange> format-revision
    /// marker, with an EMPTY <w:sectPr/> before-snapshot. The revision.author /
    /// .date / .id keys (folded from a source sectPrChange by EmitSection) overlay
    /// their attributes onto this single shared element so a multi-key `set /`
    /// accumulates one marker. The empty snapshot mirrors the empty-pPr pPrChange
    /// the paragraph round-trip produces — the before-state can't be recovered
    /// from a dump and is intentionally not reconstructed.</summary>
    private SectionPropertiesChange EnsureSectPrChangeMarker()
    {
        var sectPr = EnsureSectionProperties();
        var existing = sectPr.GetFirstChild<SectionPropertiesChange>();
        if (existing != null) return existing;
        var change = new SectionPropertiesChange();
        // Empty before-snapshot: <w:sectPrChange><w:sectPr/></w:sectPrChange>.
        change.AppendChild(new PreviousSectionProperties());
        InsertSectPrChildInOrder(sectPr, change);
        return change;
    }

    // Get-or-create the sectPr's <w:pgBorders> child. Per-side pgBorders.<side>
    // and pgBorders.offsetFrom keys overlay onto a single shared element so a
    // multi-key dump replay (`set / --prop pgBorders.top=... --prop
    // pgBorders.offsetFrom=page ...`) accumulates one <w:pgBorders> rather than
    // resetting it per key. Inserted in CT_SectPr schema order.
    private PageBorders EnsurePageBorders(SectionProperties sectPr)
    {
        var pb = sectPr.GetFirstChild<PageBorders>();
        if (pb == null)
        {
            pb = new PageBorders();
            InsertSectPrChildInOrder(sectPr, pb);
        }
        return pb;
    }

    // ==================== textDirection (section page text flow) ====================
    // BUG-DUMP-SECT-TEXTDIR: parse a section-level <w:textDirection> value.
    // Canonical ST_TextDirection values are accepted verbatim (round-trip form
    // from Get's InnerText); the cell-level aliases are also accepted for
    // leniency. Shared by the body (set /), per-section (set /section[N]), and
    // AddSection paths so all three apply identical semantics.
    private static TextDirectionValues ParseSectionTextDirection(string value) =>
        value.ToLowerInvariant().Trim() switch
        {
            "lrtb" or "horizontal" => TextDirectionValues.LefToRightTopToBottom,
            "tbrl" or "vertical-rl" => TextDirectionValues.TopToBottomRightToLeft,
            "btlr" or "vertical" => TextDirectionValues.BottomToTopLeftToRight,
            "lrtbv" or "lrtb-r" or "lr-tb-rotated" => TextDirectionValues.LefttoRightTopToBottomRotated,
            "tbrlv" or "tbrl-r" or "tb-rl-rotated" => TextDirectionValues.TopToBottomRightToLeftRotated,
            "tblrv" or "tblr-r" or "tb-lr-rotated" => TextDirectionValues.TopToBottomLeftToRightRotated,
            _ => throw new ArgumentException(
                $"Invalid textDirection value: '{value}'. Valid: lrTb, tbRl, btLr, lrTbV, tbRlV, tbLrV, none.")
        };

    // ==================== footnotePr / endnotePr ====================
    // BUG-DUMP-SECT-FOOTNOTE: section-level footnote/endnote numbering lived
    // in <w:footnotePr>/<w:endnotePr> at the START of sectPr (before <w:type>)
    // but had no Add/Set/Get path, so dump→batch dropped it and footnote
    // markers reverted from i/ii (lowerRoman) to 1/2 (decimal).
    //
    // Canonical keys (mirrors pgBorders.* dotted style; both <w:footnotePr> and
    // <w:endnotePr> share the same CT_FtnEdnNumProps child set):
    //   footnotePr.numFmt   → <w:numFmt w:val="lowerRoman"/>
    //   footnotePr.numRestart → <w:numRestart w:val="eachPage|eachSect|continuous"/>
    //   footnotePr.numStart → <w:numStart w:val="N"/>
    //   footnotePr.pos      → <w:pos w:val="pageBottom|beneath|sectEnd"/> (footnote only)
    //   endnotePr.* (same, pos → <w:pos w:val="sectEnd|docEnd"/>)
    //
    // The SDK property setters (NumberingFormat/NumberingStart/NumberingRestart/
    // FootnotePosition) enforce the internal CT_FtnEdnNumProps child order
    // (pos, numFmt, numStart, numRestart) automatically; the container itself
    // is placed via InsertSectPrChildInOrder at rank 2/3 (before <w:type>).
    //
    // Routing: shared static so the body-level (set /), the per-section
    // (set /section[N]), and AddSection (add section) paths apply identical
    // semantics. Returns true when the key was a footnotePr.*/endnotePr.* key.
    private static bool TrySetFootnoteEndnoteNumProps(SectionProperties sectPr, string key, string value)
    {
        var lower = key.ToLowerInvariant();
        bool isFootnote = lower.StartsWith("footnotepr.");
        bool isEndnote = lower.StartsWith("endnotepr.");
        if (!isFootnote && !isEndnote) return false;

        var sub = lower[(isFootnote ? "footnotepr.".Length : "endnotepr.".Length)..];

        // Get-or-create the container in schema order (footnotePr rank 2,
        // endnotePr rank 3 — both ahead of <w:type>).
        OpenXmlElement container = isFootnote
            ? sectPr.GetFirstChild<FootnoteProperties>() ?? Add(new FootnoteProperties())
            : sectPr.GetFirstChild<EndnoteProperties>() ?? Add(new EndnoteProperties());

        OpenXmlElement Add(OpenXmlElement el) { InsertSectPrChildInOrder(sectPr, el); return el; }

        switch (sub)
        {
            case "numfmt" or "format":
            {
                var fmt = ParseNumberFormat(value);
                if (container is FootnoteProperties fp) fp.NumberingFormat = new NumberingFormat { Val = fmt };
                else ((EndnoteProperties)container).NumberingFormat = new NumberingFormat { Val = fmt };
                return true;
            }
            case "numrestart" or "restart":
            {
                var rv = value.ToLowerInvariant() switch
                {
                    "eachpage" or "page" => RestartNumberValues.EachPage,
                    "eachsect" or "eachsection" or "section" => RestartNumberValues.EachSection,
                    "continuous" or "continue" => RestartNumberValues.Continuous,
                    _ => throw new ArgumentException(
                        $"Invalid {(isFootnote ? "footnotePr" : "endnotePr")}.numRestart value: '{value}'. Valid: continuous, eachSect, eachPage.")
                };
                if (container is FootnoteProperties fp) fp.NumberingRestart = new NumberingRestart { Val = rv };
                else ((EndnoteProperties)container).NumberingRestart = new NumberingRestart { Val = rv };
                return true;
            }
            case "numstart" or "start":
            {
                if (!ushort.TryParse(value, out var n))
                    throw new ArgumentException(
                        $"Invalid {(isFootnote ? "footnotePr" : "endnotePr")}.numStart value: '{value}'. Must be a non-negative integer.");
                if (container is FootnoteProperties fp) fp.NumberingStart = new NumberingStart { Val = n };
                else ((EndnoteProperties)container).NumberingStart = new NumberingStart { Val = n };
                return true;
            }
            case "pos" or "position":
            {
                if (isFootnote)
                {
                    var pv = value.ToLowerInvariant() switch
                    {
                        "pagebottom" or "bottom" => FootnotePositionValues.PageBottom,
                        "beneath" or "beneathtext" => FootnotePositionValues.BeneathText,
                        "sectend" or "sectionend" => FootnotePositionValues.SectionEnd,
                        _ => throw new ArgumentException(
                            $"Invalid footnotePr.pos value: '{value}'. Valid: pageBottom, beneath, sectEnd.")
                    };
                    ((FootnoteProperties)container).FootnotePosition = new FootnotePosition { Val = pv };
                }
                else
                {
                    var pv = value.ToLowerInvariant() switch
                    {
                        "sectend" or "sectionend" => EndnotePositionValues.SectionEnd,
                        "docend" or "documentend" => EndnotePositionValues.DocumentEnd,
                        _ => throw new ArgumentException(
                            $"Invalid endnotePr.pos value: '{value}'. Valid: sectEnd, docEnd.")
                    };
                    ((EndnoteProperties)container).EndnotePosition = new EndnotePosition { Val = pv };
                }
                return true;
            }
            default:
                return false;
        }
    }
}
