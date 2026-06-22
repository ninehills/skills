// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeCli.Core;

namespace OfficeCli.Handlers;

public partial class WordHandler
{
    // ==================== Form Fields ====================

    /// <summary>
    /// Find all legacy form fields (FORMTEXT, FORMCHECKBOX, FORMDROPDOWN) in the document.
    /// </summary>
    private List<(FieldInfo Field, FormFieldData FfData)> FindFormFields()
    {
        var allFields = FindFields();
        var result = new List<(FieldInfo, FormFieldData)>();
        foreach (var field in allFields)
        {
            var beginChar = field.BeginRun.GetFirstChild<FieldChar>();
            var ffData = beginChar?.FormFieldData;
            if (ffData != null)
                result.Add((field, ffData));
        }
        return result;
    }

    /// <summary>
    /// Convert a form field to a DocumentNode.
    /// </summary>
    private DocumentNode FormFieldToNode((FieldInfo Field, FormFieldData FfData) ff, string path)
    {
        var node = new DocumentNode { Path = path, Type = "formfield" };
        var ffData = ff.FfData;

        // Name
        var name = ffData.GetFirstChild<FormFieldName>()?.Val?.Value;
        if (name != null) node.Format["name"] = name;

        // Enabled
        var enabled = ffData.GetFirstChild<Enabled>();
        node.Format["enabled"] = enabled?.Val?.Value ?? true;

        // R14-bug3: ffData carries optional helpText/statusText/macro
        // attribution + calcOnExit; surface them so dump/get callers
        // (and AI agents introspecting a form field) see the full
        // wrapper rather than just name/type/default.
        var helpText = ffData.GetFirstChild<HelpText>()?.Val?.Value;
        if (!string.IsNullOrEmpty(helpText)) node.Format["helpText"] = helpText;
        var statusText = ffData.GetFirstChild<StatusText>()?.Val?.Value;
        if (!string.IsNullOrEmpty(statusText)) node.Format["statusText"] = statusText;
        var entryMacro = ffData.GetFirstChild<EntryMacro>()?.Val?.Value;
        if (!string.IsNullOrEmpty(entryMacro)) node.Format["entryMacro"] = entryMacro;
        var exitMacro = ffData.GetFirstChild<ExitMacro>()?.Val?.Value;
        if (!string.IsNullOrEmpty(exitMacro)) node.Format["exitMacro"] = exitMacro;
        var calcOnExit = ffData.GetFirstChild<CalculateOnExit>();
        if (calcOnExit != null)
            node.Format["calcOnExit"] = calcOnExit.Val?.Value ?? true;

        // Determine formfield type and read type-specific properties
        var textInput = ffData.GetFirstChild<TextInput>();
        var checkBox = ffData.GetFirstChild<CheckBox>();
        var dropDown = ffData.GetFirstChild<DropDownListFormField>();

        if (textInput != null)
        {
            // Schema canonical key is `type` (alias `formfieldtype`).
                node.Format["type"] = "text";
            var defaultVal = textInput.GetFirstChild<DefaultTextBoxFormFieldString>()?.Val?.Value;
            if (defaultVal != null) node.Format["default"] = defaultVal;
            var maxLen = textInput.GetFirstChild<MaxLength>()?.Val?.Value;
            if (maxLen != null) node.Format["maxLength"] = (int)maxLen;
            // R14-bug3: textInput.type and textInput.format govern how Word
            // validates / formats the typed value (regular / number /
            // date / currentTime / currentDate / calculated; \@ format
            // mask). Both are optional but must round-trip through dump.
            var textType = textInput.GetFirstChild<TextBoxFormFieldType>()?.Val?.InnerText;
            if (!string.IsNullOrEmpty(textType)) node.Format["textType"] = textType;
            var textFmt = textInput.GetFirstChild<Format>()?.Val?.Value;
            if (!string.IsNullOrEmpty(textFmt)) node.Format["textFormat"] = textFmt;
            // Result text (current value)
            var resultText = string.Join("", ff.Field.ResultRuns.SelectMany(r => r.Elements<Text>()).Select(t => t.Text));
            node.Text = resultText;
        }
        else if (checkBox != null)
        {
            node.Format["type"] = "checkbox";
            var checkedEl = checkBox.GetFirstChild<Checked>();
            var defaultEl = checkBox.GetFirstChild<DefaultCheckBoxFormFieldState>();
            var isChecked = checkedEl?.Val?.Value ?? defaultEl?.Val?.Value ?? false;
            node.Format["checked"] = isChecked;
            // R14-bug3: checkBox.size (half-points) drives the visual size
            // of the rendered checkmark; expose it so dump round-trips
            // the value AddFormField defaults to 20.
            var cbSize = checkBox.GetFirstChild<FormFieldSize>()?.Val?.Value;
            if (!string.IsNullOrEmpty(cbSize)) node.Format["checkBoxSize"] = cbSize;
            node.Text = isChecked ? "true" : "false";
        }
        else if (dropDown != null)
        {
            node.Format["type"] = "dropdown";
            var items = dropDown.Elements<ListEntryFormField>().Select(li => li.Val?.Value ?? "").ToList();
            if (items.Count > 0) node.Format["items"] = string.Join(",", items);
            // BUG-DUMP-R27-3: <w:result> is the CURRENT selection, <w:default>
            // is the default entry — surface them distinctly (the old code
            // stored the selection under `default`, masking the real default
            // and losing the selection on round-trip).
            var resultIdx = dropDown.GetFirstChild<DropDownListSelection>()?.Val?.Value;
            if (resultIdx != null) node.Format["result"] = (int)resultIdx;
            var ddDefaultIdx = dropDown.GetFirstChild<DefaultDropDownListItemIndex>()?.Val?.Value;
            if (ddDefaultIdx != null) node.Format["default"] = (int)ddDefaultIdx;
            // BUG-DUMP-FORMDROPDOWN-RESULT: node.Text is the field's CACHED
            // display text, read ONLY from a real result run (the runs between
            // fldChar(separate) and fldChar(end)). A FORMDROPDOWN often has NO
            // result run at all — the source defers the display, Word renders the
            // selected <w:result> entry on open. The old fallback SYNTHESIZED
            // node.Text from items[selIdx] in that case; the dump then forwarded
            // it as text=<entry> and AddFormField fabricated a separate+result
            // run, injecting visible text the source never had (e.g. a dropdown
            // came back showing "（征求意见稿）" baked into the document body).
            // Read only the genuine cache; the selection itself round-trips via
            // Format["result"]. An absent result run leaves node.Text empty, so
            // the emitter pins text="" and AddFormField suppresses the fabricated
            // run — mirroring the deferred-display `evaluated` protocol.
            node.Text = string.Join("", ff.Field.ResultRuns.SelectMany(r => r.Elements<Text>()).Select(t => t.Text));
        }

        // Editable status based on protection
        node.Format["editable"] = IsFormFieldEditable(ffData);

        return node;
    }

    /// <summary>
    /// Check if a form field is editable based on document protection.
    /// </summary>
    private bool IsFormFieldEditable(FormFieldData ffData)
    {
        var (mode, enforced) = GetDocumentProtection();

        // No protection → editable
        if (!enforced || mode == "none")
            return true;

        // Forms protection → form fields are always editable (unless disabled)
        if (mode == "forms")
        {
            var enabled = ffData.GetFirstChild<Enabled>();
            return enabled?.Val?.Value ?? true;
        }

        // readOnly → not editable
        return false;
    }

    /// <summary>
    /// Set properties on a form field.
    /// </summary>
    private List<string> SetFormField((FieldInfo Field, FormFieldData FfData) ff, Dictionary<string, string> properties)
    {
        var unsupported = new List<string>();
        var ffData = ff.FfData;

        foreach (var (key, value) in properties)
        {
            switch (key.ToLowerInvariant())
            {
                case "text" or "value":
                {
                    var textInput = ffData.GetFirstChild<TextInput>();
                    var checkBox = ffData.GetFirstChild<CheckBox>();
                    var dropDown = ffData.GetFirstChild<DropDownListFormField>();

                    if (checkBox != null)
                    {
                        // Set checkbox state
                        var isChecked = ParseHelpers.IsTruthy(value);
                        var checkedEl = checkBox.GetFirstChild<Checked>();
                        if (checkedEl != null) checkedEl.Val = new OnOffValue(isChecked);
                        else checkBox.AppendChild(new Checked { Val = new OnOffValue(isChecked) });

                        // Update result text (Word uses special checkbox symbol)
                        SetFormFieldResultText(ff.Field, isChecked ? "\u2612" : "\u2610");
                    }
                    else if (dropDown != null)
                    {
                        // Set dropdown selection by text or index
                        var items = dropDown.Elements<ListEntryFormField>().Select(li => li.Val?.Value ?? "").ToList();
                        int idx;
                        if (int.TryParse(value, out idx))
                        {
                            // By index
                            if (idx >= 0 && idx < items.Count)
                            {
                                var selEl = dropDown.GetFirstChild<DropDownListSelection>();
                                if (selEl != null) selEl.Val = idx;
                                else dropDown.AppendChild(new DropDownListSelection { Val = idx });
                                SetFormFieldResultText(ff.Field, items[idx]);
                            }
                        }
                        else
                        {
                            // By text match
                            var matchIdx = items.FindIndex(i => string.Equals(i, value, StringComparison.OrdinalIgnoreCase));
                            if (matchIdx >= 0)
                            {
                                var selEl = dropDown.GetFirstChild<DropDownListSelection>();
                                if (selEl != null) selEl.Val = matchIdx;
                                else dropDown.AppendChild(new DropDownListSelection { Val = matchIdx });
                                SetFormFieldResultText(ff.Field, items[matchIdx]);
                            }
                            else
                            {
                                SetFormFieldResultText(ff.Field, value);
                            }
                        }
                    }
                    else
                    {
                        // Text input - just replace result text
                        SetFormFieldResultText(ff.Field, value);
                    }
                    break;
                }
                case "checked":
                {
                    var checkBox = ffData.GetFirstChild<CheckBox>();
                    if (checkBox != null)
                    {
                        var isChecked = ParseHelpers.IsTruthy(value);
                        var checkedEl = checkBox.GetFirstChild<Checked>();
                        if (checkedEl != null) checkedEl.Val = new OnOffValue(isChecked);
                        else checkBox.AppendChild(new Checked { Val = new OnOffValue(isChecked) });
                        SetFormFieldResultText(ff.Field, isChecked ? "\u2612" : "\u2610");
                    }
                    else
                        unsupported.Add(key);
                    break;
                }
                case "name":
                {
                    var nameEl = ffData.GetFirstChild<FormFieldName>();
                    if (nameEl != null) nameEl.Val = value;
                    else ffData.PrependChild(new FormFieldName { Val = value });
                    break;
                }
                default:
                    unsupported.Add(key);
                    break;
            }
        }

        SaveDoc();
        return unsupported;
    }

    /// <summary>
    /// Replace the result text of a form field (runs between separate and end).
    /// </summary>
    private static void SetFormFieldResultText(FieldInfo field, string text)
    {
        if (field.SeparateRun == null) return;

        // Remove existing result runs
        foreach (var run in field.ResultRuns)
            run.Remove();
        field.ResultRuns.Clear();

        // Insert new result run after the separate fieldchar run
        var newRun = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        // Copy run properties from the separate run or begin run for consistent formatting
        var sourceProps = field.SeparateRun.RunProperties ?? field.BeginRun.RunProperties;
        if (sourceProps != null)
            newRun.PrependChild(sourceProps.CloneNode(true));

        field.SeparateRun.InsertAfterSelf(newRun);
    }

    /// <summary>
    /// Add a legacy form field to a paragraph.
    /// </summary>
    private string AddFormField(OpenXmlElement parent, string parentPath, int? index, Dictionary<string, string> properties)
    {
        var body = _doc.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException("Document body not found");

        Paragraph para;
        if (parent is Paragraph p)
        {
            para = p;
        }
        else if (parent is Body bodyEl)
        {
            para = new Paragraph();
            // Honor index (ChildElements-based) and the Body's trailing sectPr
            // — raw AppendChild put the paragraph AFTER sectPr, making the
            // document schema-invalid.
            InsertAtIndexOrAppend(bodyEl, para, index);
            // index was consumed by the placement above; clear it so the
            // later FormField re-threading (which also inspects index)
            // doesn't try to rearrange runs inside the new paragraph.
            index = null;
            var paraIdx = bodyEl.Elements<Paragraph>().ToList().IndexOf(para) + 1;
            parentPath = $"/body/{BuildParaPathSegment(para, paraIdx)}";
        }
        else
        {
            throw new ArgumentException("Form fields must be added to a paragraph or /body");
        }

        var ciProps = new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase);
        var ffType = ciProps.GetValueOrDefault("formfieldtype",
            ciProps.GetValueOrDefault("type", "text")).ToLowerInvariant();
        // Treat explicit name="" the same as missing name: auto-generate.
        // Empty bookmark names are addressable-invalid (predicate validator
        // rejects bare empty values), and the validator below would crash
        // on name[0] if we let "" through.
        var name = ciProps.GetValueOrDefault("name", "");
        if (string.IsNullOrEmpty(name))
            name = $"ff_{Guid.NewGuid():N}"[..12];
        if (name.Any(c => c == '/' || c == '[' || c == ']'))
            throw new ArgumentException(
                $"Form field name '{name}' contains path-special characters " +
                "('/', '[', ']'). These characters prevent later addressing via " +
                "selectors. Use only letters, digits, '.', '_', '-' in form field names.");
        // Form fields embed a BookmarkStart/End with the same name, so they
        // must obey the same addressability rules as bookmarks (R18): no
        // whitespace, no leading '@'/'\'', no embedded '"', and no duplicate
        // names anywhere in the document.
        if (name.Any(char.IsWhiteSpace) || name[0] == '@' || name[0] == '\'' || name.Contains('"'))
            throw new ArgumentException(
                $"Form field name '{name}' contains whitespace or quote/@ chars " +
                "that prevent later addressing via bare attribute selectors. " +
                "Use only letters, digits, '.', '_', '-' in form field names.");
        // BUG-DUMP-FFCHECKBOX-BOOKMARK: a form field's wrapping bookmark is
        // OPTIONAL — Word wraps a field in a same-name bookmark so REF fields can
        // target it, but a plain FORMCHECKBOX/FORMTEXT authored without that wrap
        // has none. The dump→batch round-trip pins noBookmark=true when the
        // SOURCE field had no surrounding bookmark; honour it so we don't
        // fabricate one (a 54-checkbox grid was gaining 54 Check1/Check1_N
        // bookmarks, altering the cell content and nudging table row heights →
        // page reflow). A typed `add formfield` with no noBookmark pin still gets
        // the wrapper (the historical default, needed for fillability/REF).
        var emitBookmark = !(properties.TryGetValue("nobookmark", out var nbVal)
            && ParseHelpers.IsTruthy(nbVal));
        // Word permits multiple form fields to share an ffData NAME (a form with
        // five "Check1" checkboxes is legal and common), so a hard reject broke
        // dump→batch round-trip of any such document. BUT the BOOKMARK that wraps
        // each form field must have a document-unique NAME — duplicate bookmark
        // names make Word REFUSE to open the file (it validates fine in the SDK
        // and well-formed XML checks, so the corruption is invisible until a real
        // Word load). BUG-DUMP-R34-FFBOOKMARK: a budget template with repeated
        // "FY3" fields produced two <w:bookmarkStart w:name="FY3"> and the rebuilt
        // would not open. Keep the ffData name as the source authored it, but give
        // the wrapping bookmark a unique name on collision (Word does the same —
        // only the first such field keeps the bare name).
        var bookmarkName = name;
        if (emitBookmark && body.Descendants<BookmarkStart>()
                .Any(b => string.Equals(b.Name?.Value, bookmarkName, StringComparison.Ordinal)))
        {
            int bmSuffix = 1;
            while (body.Descendants<BookmarkStart>()
                    .Any(b => string.Equals(b.Name?.Value, $"{name}_{bmSuffix}", StringComparison.Ordinal)))
                bmSuffix++;
            bookmarkName = $"{name}_{bmSuffix}";
        }
        var text = ciProps.GetValueOrDefault("text", ciProps.GetValueOrDefault("value", ""));
        // Dump pins `text=""` for a field whose SOURCE has no cached result
        // run; suppress the NBSP placeholder so the empty field round-trips
        // without gaining a glyph (it shifts table row heights).
        bool textPinnedEmpty = (ciProps.ContainsKey("text") || ciProps.ContainsKey("value"))
            && string.IsNullOrEmpty(text);

        // Generate unique bookmark ID
        var existingIds = body.Descendants<BookmarkStart>()
            .Select(b => int.TryParse(b.Id?.Value, out var id) ? id : 0);
        var bkId = (existingIds.Any() ? existingIds.Max() + 1 : 1).ToString();

        // Child-element count BEFORE this field's elements are appended. The
        // appended count varies (bookmark wrapper optional, result run optional),
        // so the --index re-thread below snapshots from here instead of assuming
        // a fixed 7. BUG-DUMP-FFCHECKBOX-BOOKMARK made the count drop by 2.
        var preAppendChildCount = para.ChildElements.Count;

        // BookmarkStart — only when the field is wrapped in a bookmark (see
        // emitBookmark / BUG-DUMP-FFCHECKBOX-BOOKMARK above).
        if (emitBookmark)
        {
            var bookmarkStart = new BookmarkStart { Id = bkId, Name = bookmarkName };
            para.AppendChild(bookmarkStart);
        }

        // Begin run with FieldChar(Begin) + FormFieldData
        var beginRun = new Run();
        var beginChar = new FieldChar { FieldCharType = FieldCharValues.Begin };

        var ffData = new FormFieldData();
        ffData.AppendChild(new FormFieldName { Val = name });
        // R14-bug3: honor an explicit enabled=false (defaults to enabled).
        // FormFieldData schema order is: name, enabled, calcOnExit, entryMacro,
        // exitMacro, helpText, statusText, type-specific child (textInput/
        // checkBox/ddList). Append in that order so Word doesn't silently
        // drop the wrappers.
        if (ciProps.TryGetValue("enabled", out var enVal) && !ParseHelpers.IsTruthy(enVal))
            ffData.AppendChild(new Enabled { Val = OnOffValue.FromBoolean(false) });
        else
            ffData.AppendChild(new Enabled());
        if (ciProps.TryGetValue("calconexit", out var coeVal))
            ffData.AppendChild(new CalculateOnExit { Val = OnOffValue.FromBoolean(ParseHelpers.IsTruthy(coeVal)) });
        if (ciProps.TryGetValue("entrymacro", out var emVal) && !string.IsNullOrEmpty(emVal))
            ffData.AppendChild(new EntryMacro { Val = emVal });
        if (ciProps.TryGetValue("exitmacro", out var xmVal) && !string.IsNullOrEmpty(xmVal))
            ffData.AppendChild(new ExitMacro { Val = xmVal });
        // BUG-DUMP-R34-FFHELPTEXT: <w:helpText>/<w:statusText> REQUIRE the
        // @w:type attribute ("text"|"autoText") — Word REFUSES to open a document
        // whose form-field helpText/statusText omits it (it validates clean in the
        // SDK and as well-formed XML, so the corruption is invisible until a real
        // Word load). The dump emitted only the val, so any FORMTEXT field with a
        // help/status string produced an unopenable rebuild. Default to "text"
        // (literal help string; "autoText" references an AutoText entry and is
        // rare) and round-trip an explicit type when the dump carried one.
        if (ciProps.TryGetValue("helptext", out var htVal) && !string.IsNullOrEmpty(htVal))
        {
            var ht = new HelpText { Val = htVal, Type = InfoTextValues.Text };
            if (ciProps.TryGetValue("helptext.type", out var htType)
                && string.Equals(htType, "autoText", StringComparison.OrdinalIgnoreCase))
                ht.Type = InfoTextValues.AutoText;
            ffData.AppendChild(ht);
        }
        if (ciProps.TryGetValue("statustext", out var stVal) && !string.IsNullOrEmpty(stVal))
        {
            var st = new StatusText { Val = stVal, Type = InfoTextValues.Text };
            if (ciProps.TryGetValue("statustext.type", out var stType)
                && string.Equals(stType, "autoText", StringComparison.OrdinalIgnoreCase))
                st.Type = InfoTextValues.AutoText;
            ffData.AppendChild(st);
        }

        switch (ffType)
        {
            case "checkbox" or "check":
            {
                var checkBox = new CheckBox();
                // R14-bug3: honor explicit checkBoxSize (half-points) so dump
                // round-trips a user-customized checkbox size. With no explicit
                // size, write <w:sizeAuto/> — Word's own default, sizing the
                // box to the surrounding text. The old fixed size=20 changed
                // the glyph height versus an auto-sized source, nudging table
                // row heights and reflowing form pages on dump→batch.
                var cbSize = ciProps.GetValueOrDefault("checkboxsize");
                if (!string.IsNullOrEmpty(cbSize))
                    checkBox.AppendChild(new FormFieldSize { Val = cbSize });
                else
                    checkBox.AppendChild(new AutomaticallySizeFormField());
                var isChecked = ciProps.TryGetValue("checked", out var chkVal) && ParseHelpers.IsTruthy(chkVal);
                checkBox.AppendChild(new DefaultCheckBoxFormFieldState { Val = new OnOffValue(isChecked) });
                if (isChecked)
                    checkBox.AppendChild(new Checked { Val = new OnOffValue(true) });
                ffData.AppendChild(checkBox);
                // BUG-DUMP-FFCHECKBOX-GLYPH: only synthesize the \u2610/\u2611 result glyph
                // for a typed `add formfield type=checkbox` that gives no explicit
                // result. The dump emits the cached glyph as text= when the source
                // stored one, and text="" when the source FORMCHECKBOX has no
                // cached result (Word renders the box from the ffData checkBox, not
                // from a literal glyph). Unconditionally writing the glyph made a
                // round-tripped empty checkbox gain a literal \u2610 \u2014 113 of them in a
                // medical-device questionnaire \u2014 whose font metrics differ from the
                // ffData-rendered box, shifting form/table layout. Honor the
                // explicit result instead (the text variable already holds it).
                if (!ciProps.ContainsKey("text") && !ciProps.ContainsKey("value"))
                    text = isChecked ? "\u2612" : "\u2610";
                break;
            }
            case "dropdown" or "drop":
            {
                var ddl = new DropDownListFormField();
                // BUG-DUMP-R27-3: <w:ddList> schema order is result, default,
                // listEntry* — the current selection (<w:result>) and default
                // (<w:default>) precede the entries. Append them first so the
                // dropdown's selected value survives dump→batch round-trip
                // (was dropped entirely: neither element was ever written).
                int? resultIdx = null;
                if (ciProps.TryGetValue("result", out var resStr)
                    && int.TryParse(resStr, out var resIdx))
                {
                    ddl.AppendChild(new DropDownListSelection { Val = resIdx });
                    resultIdx = resIdx;
                }
                if (ciProps.TryGetValue("default", out var ddDefStr)
                    && int.TryParse(ddDefStr, out var ddDefIdx))
                    ddl.AppendChild(new DefaultDropDownListItemIndex { Val = ddDefIdx });
                var entries = new List<string>();
                if (ciProps.TryGetValue("items", out var items))
                {
                    foreach (var item in items.Split(','))
                    {
                        // BUG-DUMP-FORMDROPDOWN-LISTENTRY-WS: do NOT trim listEntry
                        // values. The dump joins them with a bare "," (no padding,
                        // see GetFormField), so Split(',') already yields the exact
                        // source values. Trimming destroyed significant whitespace
                        // ("  LE  " → "LE") and — worse — turned an all-spaces entry
                        // ("      ") into an EMPTY <w:listEntry w:val=""/>, which makes
                        // Word REFUSE TO OPEN the document. Form-field dropdown entries
                        // are whitespace-significant; preserve them verbatim.
                        entries.Add(item);
                        ddl.AppendChild(new ListEntryFormField { Val = item });
                    }
                }
                ffData.AppendChild(ddl);
                // Initial display text: the selected entry when a result index
                // was given, otherwise the first item (legacy default).
                // BUG-DUMP-FORMDROPDOWN-RESULT: honor the explicit empty pin. The
                // dump emits text="" for a FORMDROPDOWN whose SOURCE has no cached
                // result run (Word defers the display, rendering <w:result> on
                // open). Re-synthesizing the selected entry here would fabricate a
                // separate+result run the source never had — baking the dropdown
                // value into the body as static visible text. When text is pinned
                // empty, leave it empty so the separate/result run is suppressed
                // below; the selection still round-trips via <w:result>.
                if (string.IsNullOrEmpty(text) && !textPinnedEmpty)
                {
                    if (resultIdx is int ri && ri >= 0 && ri < entries.Count)
                        text = entries[ri];
                    else if (entries.Count > 0)
                        text = entries[0];
                }
                break;
            }
            default: // "text"
            {
                var textInput = new TextInput();
                // R14-bug3: textType / textFormat \u2014 Word's <w:type>/<w:format>
                // children of <w:textInput>. Schema order: type, default,
                // maxLength, format.
                if (ciProps.TryGetValue("texttype", out var ttVal) && !string.IsNullOrEmpty(ttVal))
                {
                    // TextBoxFormFieldType.Val is the typed EnumValue<TextBoxFormFieldValues>;
                    // SDK rejects unknown names so normalize/validate via lowercase canonical
                    // names (regular/number/date/currentTime/currentDate/calculated).
                    var canon = ttVal.ToLowerInvariant() switch
                    {
                        "regular" => "regular",
                        "number" => "number",
                        "date" => "date",
                        "currenttime" => "currentTime",
                        "currentdate" => "currentDate",
                        "calculated" => "calculated",
                        _ => "regular"
                    };
                    var ttypEl = new TextBoxFormFieldType();
                    ttypEl.Val = new EnumValue<TextBoxFormFieldValues>();
                    ttypEl.Val.InnerText = canon;
                    textInput.AppendChild(ttypEl);
                }
                if (ciProps.TryGetValue("default", out var defaultVal))
                {
                    textInput.AppendChild(new DefaultTextBoxFormFieldString { Val = defaultVal });
                    // Use default value as initial text if no explicit text/value provided
                    if (string.IsNullOrEmpty(text))
                        text = defaultVal;
                }
                if (ciProps.TryGetValue("maxlength", out var maxLenStr) && int.TryParse(maxLenStr, out var maxLen))
                    textInput.AppendChild(new MaxLength { Val = (short)maxLen });
                if (ciProps.TryGetValue("textformat", out var tfVal) && !string.IsNullOrEmpty(tfVal))
                    textInput.AppendChild(new Format { Val = tfVal });
                ffData.AppendChild(textInput);
                break;
            }
        }

        beginChar.AppendChild(ffData);
        beginRun.AppendChild(beginChar);
        para.AppendChild(beginRun);

        // Instruction run
        var instrText = ffType switch
        {
            "checkbox" or "check" => " FORMCHECKBOX ",
            "dropdown" or "drop" => " FORMDROPDOWN ",
            _ => " FORMTEXT "
        };
        var instrRun = new Run(new FieldCode(instrText) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(instrRun);

        // Separate run. Kept unconditionally so a field created empty
        // (text="") stays FILLABLE via a later Set (SetFormFieldResultText
        // needs the separate boundary to insert the result run). The separate
        // marker renders nothing on its own, so it is visually inert.
        var separateRun = new Run(new FieldChar { FieldCharType = FieldCharValues.Separate });
        para.AppendChild(separateRun);

        // Result run. BUG-DUMP-FORMDROPDOWN-RESULT: when the source had no
        // cached result run (text pinned empty), emit NO result text \u2014 do NOT
        // fabricate the selected dropdown entry (that baked the value into the
        // body as visible text). The selection still round-trips via <w:result>;
        // the AddFormField dropdown branch above already declines to synthesize
        // `text` from the result index when textPinnedEmpty.
        Run? resultRun = null;
        if (!string.IsNullOrEmpty(text))
        {
            resultRun = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            para.AppendChild(resultRun);
        }
        else if (!textPinnedEmpty)
        {
            // Add default placeholder for FORMTEXT
            resultRun = new Run(new Text("\u00A0") { Space = SpaceProcessingModeValues.Preserve }); // non-breaking space
            para.AppendChild(resultRun);
        }

        // End run
        var endRun = new Run(new FieldChar { FieldCharType = FieldCharValues.End });
        para.AppendChild(endRun);

        // Field-run formatting (theme/literal font slots, size, bold, color):
        // stamp the forwarded rPr onto THIS field's own runs so the form renders
        // in the host face instead of the docDefaults font.
        // BUG-DUMP-R49-FFSCOPE: stamp ONLY the runs this call created (begin /
        // instr / separate / result / end). The old loop walked
        // `para.Elements<Run>()` and stamped every run carrying a <w:t> — so a
        // form field appended to a paragraph that ALREADY held styled text (a
        // Heading1 title run + an inline Date field) overwrote those sibling
        // runs' font/size, dropping their character-style size (a 16pt heading
        // re-rendered at the field's 10pt). Scope the stamp to the field's runs.
        {
            var ffFontKeys = new[]
            {
                "font", "font.latin", "font.ascii", "font.hAnsi",
                "font.asciiTheme", "font.hAnsiTheme", "font.eaTheme", "font.csTheme",
                "size", "bold", "color",
            };
            var ffFmt = ffFontKeys
                .Where(k => ciProps.ContainsKey(k))
                .Select(k => (Key: k, Val: ciProps[k]))
                .Where(t => !string.IsNullOrEmpty(t.Val))
                .ToList();
            if (ffFmt.Count > 0)
            {
                var fieldRuns = new List<Run> { beginRun, instrRun, separateRun };
                if (resultRun != null) fieldRuns.Add(resultRun);
                fieldRuns.Add(endRun);
                foreach (var ffRun in fieldRuns)
                {
                    var rp = ffRun.RunProperties ?? ffRun.PrependChild(new RunProperties());
                    foreach (var (k, v) in ffFmt)
                        ApplyRunFormatting(rp, k, v);
                }
            }
        }

        // BookmarkEnd — paired with the BookmarkStart above; skipped together
        // when the source field had no wrapping bookmark.
        if (emitBookmark)
        {
            var bookmarkEnd = new BookmarkEnd { Id = bkId };
            para.AppendChild(bookmarkEnd);
        }

        // CONSISTENCY(add-index): honor --index / --after / --before (#76).
        // When an anchor/index was supplied, re-thread the appended elements
        // into the requested child-element position. Simpler than restructuring
        // the construction path above.
        if (index.HasValue)
        {
            // Snapshot: every element appended after preAppendChildCount, in
            // order (count varies — optional bookmark wrapper / result run).
            var appendedCount = para.ChildElements.Count - preAppendChildCount;
            var ffElements = para.ChildElements
                .Reverse().Take(appendedCount).Reverse().ToList();
            // The anchor position was computed against the children BEFORE we
            // appended these elements.
            var origChildCount = preAppendChildCount;
            if (index.Value < origChildCount)
            {
                var anchor = para.ChildElements[index.Value];
                foreach (var el in ffElements) el.Remove();
                para.InsertBefore(ffElements[0], anchor);
                for (int ffI = 1; ffI < ffElements.Count; ffI++)
                    para.InsertAfter(ffElements[ffI], ffElements[ffI - 1]);
            }
            // else: index is at or past the end — current append position is correct.
        }

        SaveDoc();

        // Compute result path
        int ffIdx = 0;
        var allFf = FindFormFields();
        for (int i = 0; i < allFf.Count; i++)
        {
            if (allFf[i].Field.BeginRun == beginRun)
            { ffIdx = i + 1; break; }
        }
        return $"/formfield[{ffIdx}]";
    }
}
