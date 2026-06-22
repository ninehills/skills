// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using OfficeCli.Core;

namespace OfficeCli.Handlers;

public static partial class PptxBatchEmitter
{
    // CONSISTENCY(emit-resources-mirror): mirrors WordBatchEmitter.Resources.cs
    // — each whole-part-XML block emits as a single raw-set replace. Theme /
    // master / layout / notesMaster carry rich structured XML (clrScheme,
    // fontScheme, txStyles, fmtScheme, …) that has no typed Set vocabulary; the
    // natural operation is "swap the whole block". Replay's raw-set overwrites
    // whatever the blank deck stamped during BlankDocCreator.

    // CONSISTENCY(raw-xmlns-canonicalize): mirrors
    // WordBatchEmitter.Resources.CanonicalizeRawXml. RawXmlHelper.Execute
    // propagates the root's xmlns declarations onto every direct child so the
    // SDK's InnerXml setter can resolve prefixes (SDK does not inherit root
    // xmlns scope when parsing inner content). After replay, the part's XML
    // carries redundant xmlns:p / xmlns:a attrs on each child of /theme,
    // /slideMaster[N], /slideLayout[N] — observed first-replay growth on a
    // blank-deck round-trip: 16657 → 17923 bytes (≈1.2 KB across 7 raw-set
    // parts), then stable on subsequent rounds. Canonicalise on emit so the
    // first-pass (clean source) and second-pass (post-replay bloated) shapes
    // collapse identically.
    private static string CanonicalizeRawXml(string xml)
    {
        if (string.IsNullOrEmpty(xml) || !xml.StartsWith("<")) return xml;
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            if (doc.Root == null) return xml;
            var rootNsAttrs = doc.Root.Attributes()
                .Where(a => a.IsNamespaceDeclaration)
                .ToDictionary(a => a.Name, a => a.Value);
            foreach (var desc in doc.Root.Descendants())
            {
                var toRemove = desc.Attributes()
                    .Where(a => a.IsNamespaceDeclaration
                                && rootNsAttrs.TryGetValue(a.Name, out var v)
                                && v == a.Value)
                    .ToList();
                foreach (var a in toRemove) a.Remove();
            }
            return doc.Root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        }
        catch
        {
            // Malformed XML — leave as-is rather than corrupting.
            return xml;
        }
    }

    // Remove every <p:custDataLst> (programmability tag references) from a raw
    // part XML. A UserDefinedTagsPart added to a slideMaster does not survive
    // Save, so a master <p:tags r:id> reference left dangling makes PowerPoint
    // refuse the deck; dropping the (invisible) reference keeps it openable.
    private static string StripCustDataLst(string xml)
    {
        if (string.IsNullOrEmpty(xml) || xml.IndexOf("custDataLst", StringComparison.Ordinal) < 0)
            return xml;
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            if (doc.Root == null) return xml;
            var pNs = System.Xml.Linq.XNamespace.Get(
                "http://schemas.openxmlformats.org/presentationml/2006/main");
            foreach (var el in doc.Root.DescendantsAndSelf(pNs + "custDataLst").ToList())
                el.Remove();
            return doc.Root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        }
        catch { return xml; }
    }

    private static void EmitThemeRaw(PowerPointHandler ppt, List<BatchItem> items)
    {
        // The blank scaffold shares ONE theme part (/ppt/theme/theme1.xml)
        // between the presentation and master1 — exactly the source topology for
        // master1. So raw-set master1's theme content into that existing shared
        // part here, and let EmitMasterRawOne emit DISTINCT theme parts only for
        // masters 2..N (which the scaffold doesn't provide). This keeps the
        // presentation<->master1 theme sharing intact while giving each extra
        // master its own theme.
        string xml;
        try { xml = ppt.Raw("/theme"); }
        catch { return; }
        if (string.IsNullOrEmpty(xml) || !xml.StartsWith("<") || xml == "(no theme)")
            return;
        xml = CanonicalizeRawXml(xml);

        // Carry texture images referenced by the theme's fmtScheme fillStyleLst
        // blipFill BEFORE the raw-set, so the embed rId resolves on replay. The
        // blank scaffold's theme has no such images, so a pinned source rId is
        // free; without this the raw-set'd theme XML keeps a dangling r:embed and
        // PowerPoint refuses to open the deck (mirrors the master/layout carrier).
        try
        {
            foreach (var imageInfo in ppt.GetThemeImageParts())
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = "/theme",
                    Type = "image",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = imageInfo.RelId,
                        ["content-type"] = imageInfo.ContentType,
                        ["data"] = imageInfo.Base64Data,
                    },
                });
            }
        }
        catch { /* best-effort — theme raw replace still runs */ }

        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = "/theme",
            Xpath = "/a:theme",
            Action = "replace",
            Xml = xml
        });
    }

    private static void EmitNotesMasterRaw(PowerPointHandler ppt, List<BatchItem> items)
    {
        if (!ppt.HasNotesMaster) return;
        string xml;
        try { xml = ppt.Raw("/notesMaster"); }
        catch { return; }
        if (string.IsNullOrEmpty(xml) || !xml.StartsWith("<")) return;
        xml = CanonicalizeRawXml(xml);

        // Raw-set FIRST — it creates the notesMaster part on demand on a blank
        // target. The add-part theme below then attaches the theme; ordering the
        // theme after the part-create avoids "notesMaster does not exist yet".
        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = "/notesMaster",
            Xpath = "/p:notesMaster",
            Action = "replace",
            Xml = xml
        });

        // The notes master is a theme-owning master too: source notesMaster.rels
        // references its own theme part. The on-demand notesMaster create wired no
        // theme relationship, so the rebuilt notesMaster had no .rels at all.
        // Emit its theme part (distinct content + pinned rId).
        try
        {
            var nmt = ppt.GetNotesMasterTheme();
            if (nmt is { } nmtv)
            {
                var nmtProps = new Dictionary<string, string>
                {
                    ["rid"] = nmtv.RelId,
                    ["data"] = nmtv.ThemeXml,
                };
                // Carry texture images the notes theme references (else r:embed dangles).
                EmitDiagramImageProps(nmtProps, "themeImage", ppt.GetNotesMasterThemeImages());
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = "/notesMaster",
                    Type = "theme",
                    Props = nmtProps,
                });
            }
        }
        catch { /* best-effort */ }
    }

    private static void EmitMasterRaw(PowerPointHandler ppt, List<BatchItem> items)
    {
        var n = ppt.SlideMasterCount;
        for (int i = 1; i <= n; i++) EmitMasterRawOne(ppt, i, items);
    }

    private static bool EmitMasterRawOne(PowerPointHandler ppt, int idx, List<BatchItem> items)
    {
        string xml;
        try { xml = ppt.Raw($"/slideMaster[{idx}]"); }
        catch { return false; }
        if (string.IsNullOrEmpty(xml) || !xml.StartsWith("<")) return false;
        xml = CanonicalizeRawXml(xml);

        // Emit ImageParts attached to this master BEFORE the raw-set replace.
        // The master XML carries <p:pic> blipFill r:embed="rIdN" references;
        // without a matching ImagePart + relationship the post-replay validator
        // flags "rIdN does not exist" and PowerPoint refuses to open
        // (templates with decorative master-level images, e.g. gov_bja_template
        // master2's blue band). add-part image pins the source's rId so the
        // raw-set'd master XML resolves on replay.
        try
        {
            foreach (var imageInfo in ppt.GetMasterImageParts(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideMaster[{idx}]",
                    Type = "image",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = imageInfo.RelId,
                        ["content-type"] = imageInfo.ContentType,
                        ["data"] = imageInfo.Base64Data,
                    },
                });
            }
        }
        catch { /* best-effort — master raw replace still runs */ }

        // Emit THIS master's own theme part for masters 2..N (distinct content).
        // master1's theme is the shared /ppt/theme/theme1.xml that the scaffold
        // already wires to BOTH the presentation and master1 — EmitThemeRaw
        // raw-sets master1's content into it, so re-creating it here would break
        // the presentation<->master1 sharing. Masters 2..N have no scaffold theme,
        // so without this they collapse onto theme1, losing their own theme
        // content and producing a deck PowerPoint refuses.
        if (idx >= 2)
        {
            try
            {
                var mt = ppt.GetMasterTheme(idx);
                if (mt is { } mtv)
                {
                    var mtProps = new Dictionary<string, string>
                    {
                        ["rid"] = mtv.RelId,
                        ["data"] = mtv.ThemeXml,
                    };
                    // Carry texture images this master's theme references.
                    EmitDiagramImageProps(mtProps, "themeImage", ppt.GetMasterThemeImages(idx));
                    items.Add(new BatchItem
                    {
                        Command = "add-part",
                        Parent = $"/slideMaster[{idx}]",
                        Type = "theme",
                        Props = mtProps,
                    });
                }
            }
            catch { /* best-effort */ }
        }

        // Non-image binary parts (HD Photo .wdp backup layer referenced by a
        // master-level picture's <a14:imgLayer r:embed>). GetMasterImageParts
        // above only carries typed ImageParts; the ExtendedPart would dangle.
        try
        {
            foreach (var comp in ppt.GetMasterExtendedParts(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideMaster[{idx}]",
                    Type = "extpart",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = comp.RelId,
                        ["rel-type"] = comp.RelType,
                        ["content-type"] = comp.ContentType,
                        ["ext"] = comp.TargetExt,
                        ["data"] = comp.Base64Data,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        // External image links on the master (<a:blip r:link="rIdN"> →
        // TargetMode=External image). GetMasterImageParts covers only embedded
        // images; without this the external rel dangles.
        try
        {
            foreach (var (relId, relType, uri) in ppt.GetMasterExternalImageLinks(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideMaster[{idx}]",
                    Type = "extrel",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = relId,
                        ["rel-type"] = relType,
                        ["target"] = uri,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        // Master <p:custDataLst><p:tags r:id="rIdN"/> (programmability tags) are
        // NOT carried: a UserDefinedTagsPart added to a SlideMasterPart does not
        // survive Save (the SDK prunes it — unlike a slide/layout tag part), so
        // pinning the rId left the raw-set'd r:id dangling and PowerPoint refused
        // the deck (0x80070570). Strip custDataLst from the master XML instead so
        // there is no dangling reference. Tags are invisible programmability
        // metadata; this mirrors how slides drop custDataLst on the typed emit.
        xml = StripCustDataLst(xml);

        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = $"/slideMaster[{idx}]",
            Xpath = "/p:sldMaster",
            Action = "replace",
            Xml = xml
        });
        return true;
    }

    private static void EmitLayoutRaw(PowerPointHandler ppt, List<BatchItem> items)
    {
        var n = ppt.SlideLayoutCount;
        for (int i = 1; i <= n; i++) EmitLayoutRawOne(ppt, i, items);
    }

    private static bool EmitLayoutRawOne(PowerPointHandler ppt, int idx, List<BatchItem> items)
    {
        string xml;
        try { xml = ppt.Raw($"/slideLayout[{idx}]"); }
        catch { return false; }
        if (string.IsNullOrEmpty(xml) || !xml.StartsWith("<")) return false;
        xml = CanonicalizeRawXml(xml);

        // Mirrors EmitMasterRawOne — layout-level ImageParts must materialise
        // before the raw-set replace so r:embed references survive.
        try
        {
            foreach (var imageInfo in ppt.GetLayoutImageParts(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideLayout[{idx}]",
                    Type = "image",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = imageInfo.RelId,
                        ["content-type"] = imageInfo.ContentType,
                        ["data"] = imageInfo.Base64Data,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        // External hyperlink relationships on the layout — the raw-set XML below
        // carries <a:hlinkClick r:id="rIdN">, but the relationship is external (a
        // URL) so the ImagePart carrier above doesn't re-create it. Pin each id
        // BEFORE the raw-set replace so the renumbered rebuilt layout's .rels
        // resolves the reference. (mirrors the add-part image pattern)
        try
        {
            foreach (var (relId, target) in ppt.GetLayoutExternalHyperlinks(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideLayout[{idx}]",
                    Type = "hyperlink",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = relId,
                        ["target"] = target,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        // External image links on the layout (<a:blip r:link> → external image) —
        // same as the master external-image-link carrier; the embedded-image
        // carrier above doesn't cover external links.
        try
        {
            foreach (var (relId, relType, uri) in ppt.GetLayoutExternalImageLinks(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideLayout[{idx}]",
                    Type = "extrel",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = relId,
                        ["rel-type"] = relType,
                        ["target"] = uri,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        // Non-image binary parts (HD Photo .wdp layer referenced by a
        // layout-level picture's <a14:imgLayer r:embed>) — same as the master
        // ExtendedPart carrier; GetLayoutImageParts covers only typed ImageParts.
        try
        {
            foreach (var comp in ppt.GetLayoutExtendedParts(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideLayout[{idx}]",
                    Type = "extpart",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = comp.RelId,
                        ["rel-type"] = comp.RelType,
                        ["content-type"] = comp.ContentType,
                        ["ext"] = comp.TargetExt,
                        ["data"] = comp.Base64Data,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        // UserDefinedTags parts referenced by the layout XML's
        // <p:custDataLst><p:tags r:id="rIdN"/>. Like the external-hyperlink rel,
        // the tags part lives in the layout's own .rels (enumerated separately),
        // so the ImagePart carrier never re-creates it — without this the raw-set'd
        // r:id="rIdN" dangles and PowerPoint refuses the whole deck (OPC corrupt).
        // Pin each id + verbatim tag XML BEFORE the raw-set replace.
        try
        {
            foreach (var (relId, tagXml) in ppt.GetLayoutTagParts(idx))
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = $"/slideLayout[{idx}]",
                    Type = "tags",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = relId,
                        ["data"] = tagXml,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = $"/slideLayout[{idx}]",
            Xpath = "/p:sldLayout",
            Action = "replace",
            Xml = xml
        });
        return true;
    }

    private static bool EmitNoteSlideRawOne(PowerPointHandler ppt, int idx, List<BatchItem> items)
    {
        string xml;
        try { xml = ppt.Raw($"/noteSlide[{idx}]"); }
        catch { return false; }
        if (string.IsNullOrEmpty(xml) || !xml.StartsWith("<")) return false;
        xml = CanonicalizeRawXml(xml);

        items.Add(new BatchItem
        {
            Command = "raw-set",
            Part = $"/noteSlide[{idx}]",
            Xpath = "/p:notes",
            Action = "replace",
            Xml = xml
        });
        return true;
    }

    // Presentation-level structural children that the typed Add/Set/EmitPresentationProps
    // surface does not round-trip: custShowLst (custom slide shows) and extLst
    // (extension children — sectionLst / modifyVerifier / etc.). Both reference
    // slides by rId; `add slide` on replay mints fresh rIds, so a verbatim
    // raw-set replace would point at stale targets and PowerPoint would refuse
    // to open. Honest path: emit the source XML as a best-effort append AND
    // record an UnsupportedWarning so callers know the references may need
    // manual rewiring. Mirrors the "loud not silent" rule for content we cannot
    // faithfully serialize through the typed vocabulary.
    private static void EmitPresentationExtras(
        PowerPointHandler ppt, List<BatchItem> items, SlideEmitContext ctx)
    {
        string presXml;
        try { presXml = ppt.Raw("/presentation"); }
        catch { return; }
        if (string.IsNullOrEmpty(presXml) || !presXml.StartsWith("<")) return;

        System.Xml.Linq.XDocument doc;
        try { doc = System.Xml.Linq.XDocument.Parse(presXml); }
        catch { return; }
        if (doc.Root == null) return;

        var pNs = System.Xml.Linq.XNamespace.Get(
            "http://schemas.openxmlformats.org/presentationml/2006/main");

        // Custom binary parts attached to the presentation part (e.g. Google
        // Slides' ppt/metadata, referenced by <go:slidesCustomData r:id="rIdN">
        // inside the extLst emitted below). The extLst raw-set carries the r:id;
        // pin the part + its source rId here so the reference resolves instead
        // of dangling (PowerPoint refuses the deck otherwise). Mirrors the
        // master/layout extpart carrier.
        try
        {
            foreach (var comp in ppt.GetPresentationExtendedParts())
            {
                items.Add(new BatchItem
                {
                    Command = "add-part",
                    Parent = "/presentation",
                    Type = "extpart",
                    Props = new Dictionary<string, string>
                    {
                        ["rid"] = comp.RelId,
                        ["rel-type"] = comp.RelType,
                        ["content-type"] = comp.ContentType,
                        ["ext"] = comp.TargetExt,
                        ["data"] = comp.Base64Data,
                    },
                });
            }
        }
        catch { /* best-effort */ }

        // CT_Presentation child order (ECMA-376 §19.2.1.26) is significant —
        // PowerPoint's strict validator (and replay's OOXML validator) flags
        // any element that appears after a later-schema sibling as an
        // "unexpected child". The relevant tail is:
        //   …, custShowLst, photoAlbum, custDataLst, kinsoku,
        //   defaultTextStyle, modifyVerifier, extLst.
        // Emit in schema order so each `raw-set append` lands on the
        // trailing-most slot at that moment. Previously extLst was appended
        // before kinsoku / defaultTextStyle / photoAlbum, which then chained
        // after extLst in the wrong order — PowerPoint refused the file
        // (0x80070570) on every deck that carried both a section list and
        // a deck-level default text style (gov_bja_template, …).

        // custShowLst — `<p:custShowLst><p:custShow><p:sldLst><p:sld r:id="…"/>`.
        var custShow = doc.Root.Element(pNs + "custShowLst");
        if (custShow != null)
        {
            var xml = CanonicalizeRawXml(custShow.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
            items.Add(new BatchItem
            {
                Command = "raw-set",
                Part = "/presentation",
                Xpath = "/p:presentation",
                Action = "append",
                Xml = xml,
            });
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: "presentation.custShowLst",
                SlidePath: "/presentation",
                Reason: "Custom slide shows reference slides by relationship id; replay's `add slide` mints fresh rIds, so the custShow targets may point at stale relationships. Verify in PowerPoint before relying on the round-tripped show."));
        }

        // photoAlbum — flags marking the deck as a photo album
        // (`<p:photoAlbum bw="…" showCaptions="…" layout="…" frame="…"/>`).
        var photo = doc.Root.Element(pNs + "photoAlbum");
        if (photo != null)
        {
            var xml = CanonicalizeRawXml(photo.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
            items.Add(new BatchItem
            {
                Command = "raw-set",
                Part = "/presentation",
                Xpath = "/p:presentation",
                Action = "append",
                Xml = xml,
            });
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: "presentation.photoAlbum",
                SlidePath: "/presentation",
                Reason: "photoAlbum (PowerPoint Photo Album metadata: bw / captions / layout / frame attributes) is preserved verbatim via raw-set; no typed Set vocabulary exists for these attributes."));
        }

        // kinsoku — East-Asian line-break rules (`<p:kinsoku invalChars=… hangChars=…/>`).
        var kins = doc.Root.Element(pNs + "kinsoku");
        if (kins != null)
        {
            var xml = CanonicalizeRawXml(kins.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
            items.Add(new BatchItem
            {
                Command = "raw-set",
                Part = "/presentation",
                Xpath = "/p:presentation",
                Action = "append",
                Xml = xml,
            });
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: "presentation.kinsoku",
                SlidePath: "/presentation",
                Reason: "kinsoku (East-Asian line-break rules: invalid / hanging chars) is preserved verbatim via raw-set; no typed Set vocabulary exists yet to edit individual rule entries."));
        }

        // defaultTextStyle — body-text level defaults inherited by every
        // slide layout / master that doesn't override them (`<p:defaultTextStyle>
        // <a:defPPr/> <a:lvl1pPr/> …</p:defaultTextStyle>`).
        var dts = doc.Root.Element(pNs + "defaultTextStyle");
        if (dts != null)
        {
            var xml = CanonicalizeRawXml(dts.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
            items.Add(new BatchItem
            {
                Command = "raw-set",
                Part = "/presentation",
                Xpath = "/p:presentation",
                Action = "append",
                Xml = xml,
            });
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: "presentation.defaultTextStyle",
                SlidePath: "/presentation",
                Reason: "defaultTextStyle (deck-level paragraph defaults inherited by layouts/masters) is preserved verbatim via raw-set; no typed Set surface for individual level paragraph properties at this level yet."));
        }

        // extLst — MUST be last (CT_Presentation tail). `<p:extLst><p:ext uri="…">`
        // carries sectionLst, modifyVerifier, misc 2010+ extensions.
        var ext = doc.Root.Element(pNs + "extLst");
        if (ext != null)
        {
            var xml = CanonicalizeRawXml(ext.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
            items.Add(new BatchItem
            {
                Command = "raw-set",
                Part = "/presentation",
                Xpath = "/p:presentation",
                Action = "append",
                Xml = xml,
            });
            ctx.Unsupported.Add(new UnsupportedWarning(
                Element: "presentation.extLst",
                SlidePath: "/presentation",
                Reason: "Presentation extensions (sectionLst / modifyVerifier / …) may reference slides by rId; replay mints fresh rIds, so references can go stale. Section names survive; section → slide membership may need manual rewiring."));
        }
    }
}
