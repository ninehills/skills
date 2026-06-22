#!/usr/bin/env python3
"""
Document Formatting Showcase — generates document-formatting.docx exercising the
docx `document` property surface (schemas/help/docx/document.json): the
document-level settings that have no per-paragraph equivalent.

`document` is a read-only container addressed at path "/"; you never add or
remove it, only `set`/`get` its properties. They fall into seven groups:

  Metadata     — author/title/subject/keywords/description + extended.company/...
  Page setup   — pageWidth/Height, orientation, margins (mirror/gutter/book-fold)
  docDefaults  — the document-wide run/paragraph defaults unstyled text inherits
  Theme        — theme.color.accentN / dk/lt / hlink and theme.font.major/minor
  CJK grid     — docGrid.*, autoSpaceDE/DN, kinsoku, overflowPunct
  Fonts        — embedFonts / embedSystemFonts / saveSubsetFonts
  Display      — evenAndOddHeaders, autoHyphenation, defaultTabStop, privacy flags

Like examples/excel/conditional-formatting.py, this drives the officecli Python
SDK (`pip install officecli-sdk`): one resident, writes shipped over the pipe.

Usage:
  python3 document-formatting.py
"""

import os
import sys
import subprocess

try:
    import officecli  # pip install officecli-sdk
except ImportError:
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "..", "sdk", "python"))
    import officecli

FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "document-formatting.docx")


def doc_set(**props):
    """A `set` on the document container (path '/')."""
    return {"command": "set", "path": "/", "props": props}


def para(text, **props):
    return {"command": "add", "parent": "/body", "type": "paragraph",
            "props": {"text": text, **props}}


print("\n==========================================")
print(f"Generating document-formatting showcase: {FILE}")
print("==========================================")

with officecli.create(FILE, "--force") as doc:

    # ----------------------------------------------------------------------
    # Body — a few paragraphs so the inherited docDefaults / theme are visible.
    # An unstyled paragraph (no run formatting) renders in docDefaults.font at
    # docDefaults.fontSize in docDefaults.color; Heading paragraphs pick up the
    # theme major font.
    # ----------------------------------------------------------------------
    print("\n--- Body (inherits docDefaults + theme) ---")
    doc.batch([
        para("Document Formatting Showcase", style="Title"),
        para("This heading uses the theme major font.", style="Heading1"),
        para("This body paragraph carries NO run formatting, so it renders in "
             "the document defaults: Georgia 12pt, dark slate — set via "
             "docDefaults.* on the document, not on the run."),
        para("A second default paragraph, to show the inherited line spacing "
             "and space-after also come from docDefaults."),
        para("Theme accents", style="Heading2"),
        para("Accent colors below are remapped at the theme level; any element "
             "that references accent1..6 (styles, charts, shapes) shifts with "
             "them."),
    ])

    # ----------------------------------------------------------------------
    # 1. Metadata (core + extended document properties)
    # ----------------------------------------------------------------------
    print("--- Metadata ---")
    doc.batch([doc_set(
        author="Jane Author", title="Q3 Field Report", subject="Finance",
        keywords="report,q3,finance", description="Quarterly field summary.",
        lastModifiedBy="Editorial",
    ), doc_set(**{
        "extended.company": "Acme Corp",
        "extended.manager": "Dana Lead",
        "extended.template": "Normal.dotm",
    })])

    # ----------------------------------------------------------------------
    # 2. Page setup — A4 portrait, mirrored margins, book-fold off
    # ----------------------------------------------------------------------
    print("--- Page setup ---")
    doc.batch([doc_set(
        pageWidth="21cm", pageHeight="29.7cm", orientation="portrait",
        marginTop="2.54cm", marginBottom="2.54cm",
        marginLeft="3.18cm", marginRight="3.18cm",
        marginHeader="1.5cm", marginFooter="1.75cm",
    ), doc_set(
        mirrorMargins="true", gutterAtTop="false", bookFoldPrinting="false",
    )])

    # ----------------------------------------------------------------------
    # 3. docDefaults — the document-wide run/paragraph defaults
    # ----------------------------------------------------------------------
    print("--- docDefaults ---")
    doc.batch([doc_set(**{
        "docDefaults.font": "Georgia",
        "docDefaults.font.eastAsia": "SimSun",
        "docDefaults.fontSize": "12",
        "docDefaults.color": "2F3640",
        "docDefaults.bold": "false",
        "docDefaults.italic": "false",
        "docDefaults.alignment": "left",
        "docDefaults.spaceAfter": "8pt",
        "docDefaults.lineSpacing": "1.15x",
    })])

    # ----------------------------------------------------------------------
    # 4. Theme — remap palette accents and major/minor fonts
    # ----------------------------------------------------------------------
    print("--- Theme ---")
    doc.batch([doc_set(**{
        "theme.color.accent1": "1F6FEB",
        "theme.color.accent2": "E3572A",
        "theme.color.accent3": "2DA44E",
        "theme.color.accent4": "BF8700",
        "theme.color.accent5": "8250DF",
        "theme.color.accent6": "1B7C83",
        "theme.color.hlink": "0969DA",
        "theme.color.folHlink": "8250DF",
    }), doc_set(**{
        "theme.font.major.latin": "Georgia",
        "theme.font.minor.latin": "Calibri",
        "theme.font.major.eastAsia": "SimHei",
        "theme.font.minor.eastAsia": "SimSun",
    })])

    # ----------------------------------------------------------------------
    # 5. CJK grid & spacing controls
    # ----------------------------------------------------------------------
    print("--- CJK grid ---")
    doc.batch([doc_set(**{
        "docGrid.type": "lines",
        "docGrid.linePitch": "312",
        "charSpacingControl": "compressPunctuation",
        "autoSpaceDE": "true",
        "autoSpaceDN": "true",
        "kinsoku": "true",
        "overflowPunct": "true",
    })])

    # ----------------------------------------------------------------------
    # 6. Font embedding
    # ----------------------------------------------------------------------
    print("--- Font embedding ---")
    doc.batch([doc_set(
        embedFonts="true", embedSystemFonts="false", saveSubsetFonts="true",
    )])

    # ----------------------------------------------------------------------
    # 7. Display / print / privacy
    # ----------------------------------------------------------------------
    print("--- Display & privacy ---")
    doc.batch([doc_set(
        evenAndOddHeaders="true", autoHyphenation="false", defaultTabStop="720",
        displayBackgroundShape="true", removePersonalInformation="false",
        removeDateAndTime="false", printFormsData="false",
    )])

    doc.send({"command": "save"})

    # ----------------------------------------------------------------------
    # Get round-trip: confirm canonical keys read back from the container
    # ----------------------------------------------------------------------
    print("\n--- Round-trip readback (get / ) ---")
    node = doc.send({"command": "get", "path": "/"})
    fmt = node.get("data", {}).get("results", [{}])[0].get("format", {})
    for k in ["author", "title", "subject", "pageWidth", "pageHeight", "orientation",
              "marginLeft", "docDefaults.font", "docDefaults.fontSize",
              "theme.color.accent1", "theme.font.major.latin", "docGrid.type"]:
        if k in fmt:
            print(f"  {k} = {fmt[k]}")

print("\n--- Validate (fresh process, from disk) ---")
r = subprocess.run(["officecli", "validate", FILE], capture_output=True, text=True)
print(" ", (r.stdout or r.stderr).strip().split("\n")[0])

print(f"\nCreated: {FILE}")
