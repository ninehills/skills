# Run / Character Formatting Showcase

Exercises the docx **run** (character-level) property surface. Three files:

- **run-formatting.sh** — builds the document with `officecli` (147 lines, ~50 commands).
- **run-formatting.docx** — generated output, one paragraph per property family.
- **run-formatting.md** — this file.

## Regenerate

```bash
cd examples/word
bash run-formatting.sh
# → run-formatting.docx
```

## Weight & Style

Bold, italic, and their combination via the implicit run on a paragraph.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Bold text" --prop bold=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Italic text" --prop italic=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Bold + italic" --prop bold=true --prop italic=true
```

**Features:** `bold` (true/false), `italic` (true/false)

## Underline Variants

All named underline styles, including a colored wave underline.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=single" --prop underline=single
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=double" --prop underline=double
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=thick" --prop underline=thick
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=dotted" --prop underline=dotted
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=wave (red)" --prop underline=wave --prop underline.color=FF0000
```

**Features:** `underline` (single/double/thick/dotted/wave/dash/dotDash/dotDotDash/dashLong/wavyDouble/wavyHeavy/…), `underline.color` (hex color for the underline rule, independent of run text color)

## Strikethrough

Single and double strikethrough.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=single strike" --prop strike=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=double strike" --prop dstrike=true
```

**Features:** `strike` (single strikethrough), `dstrike` (double strikethrough)

## Case

All-caps and small-caps rendering — text content is unchanged; Word renders it in the requested case.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=all caps rendering" --prop caps=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=small caps rendering" --prop smallcaps=true
```

**Features:** `caps` (force uppercase display), `smallcaps` (small-caps display)

## Super / Subscript (Mixed Runs)

Most paragraphs use the implicit single run. For mixed-format lines — like E=mc² or H₂O — explicit `--type run` children are appended to the last paragraph.

```bash
# E = mc²
officecli add run-formatting.docx /body --type paragraph --prop "text=E = mc"
officecli add run-formatting.docx "/body/p[last()]" --type run \
  --prop "text=2" --prop superscript=true

# H₂O
officecli add run-formatting.docx /body --type paragraph --prop "text=H"
officecli add run-formatting.docx "/body/p[last()]" --type run \
  --prop "text=2" --prop subscript=true
officecli add run-formatting.docx "/body/p[last()]" --type run --prop "text=O"
```

**Features:** `superscript` (raises run above baseline), `subscript` (lowers run below baseline), `vertAlign` (enum alias: superscript/subscript/baseline)

> The paragraph path `/body/p[last()]` must be quoted in the shell — `[` and `(` are shell metacharacters.

## Color, Size, Highlight

Font color in hex, point size, and Word highlight palette.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Red 16pt" --prop color=C00000 --prop size=16
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Highlighted" --prop highlight=yellow
```

**Features:** `color` (6-digit hex, no `#`; e.g. `C00000`), `size` (half-point units; `16` = 8pt, or use `16pt`), `highlight` (yellow/green/cyan/magenta/blue/red/darkBlue/darkCyan/darkGreen/darkMagenta/darkRed/darkYellow/darkGray/lightGray/black/none)

## Per-Script Fonts

Assign different typefaces to Latin and East-Asian script ranges within a single run.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Latin Georgia + CJK 宋体" \
  --prop font.latin=Georgia --prop font.eastAsia=SimSun --prop size=14
```

**Features:** `font.latin` (ASCII/latin script font), `font.eastAsia` (CJK script font), `font.cs` (complex-script font), `font` (shorthand sets all scripts at once)

## Text Effects

Classic WordprocessingML text rendering effects.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=emboss" --prop emboss=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=imprint" --prop imprint=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=outline" --prop outline=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=shadow" --prop shadow=true
```

**Features:** `emboss` (raised 3D look), `imprint` (pressed-in 3D look), `outline` (hollow letterforms), `shadow` (drop shadow)

## Character Spacing & Position

Horizontal character tracking and baseline shift.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=expanded spacing" --prop charSpacing=2pt
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=raised 3pt" --prop position=3pt
```

**Features:** `charSpacing` (extra space between each character; accepts `2pt`, `2`, or EMU), `position` (baseline shift; positive raises, negative lowers; e.g. `3pt`)

> `charSpacing` maps to `w:spacing` (fixed gap added between characters). `kern` is distinct: it sets the minimum font size threshold for pair-kerning (`kern=28` means kern when size ≥ 14pt).

## Language Tag

Tag a run for spell-check / grammar with a BCP-47 locale.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Tagged en-US for spellcheck" --prop lang=en-US
```

**Features:** `lang` (BCP-47 locale applied to all scripts; e.g. `en-US`, `zh-CN`, `ar-SA`)

## Complex-Script Variants

Separate bold/italic/size properties that apply only to complex-script (bidirectional) runs, and an RTL run direction flag.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=cs bold + italic + 14pt" \
  --prop bold.cs=true --prop italic.cs=true --prop size.cs=14pt
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Right-to-left run" --prop rtl=true --prop direction=rtl
```

**Features:** `bold.cs` (bold for complex-script glyphs), `italic.cs`, `size.cs` (accepts `14pt` or bare half-point number), `rtl` (sets `w:rtl`), `direction` (alias for `rtl`)

## Theme Fonts

Reference the document theme's minor/major font slots rather than hardcoding a font name.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Latin/CS/EA theme fonts" \
  --prop font.asciiTheme=minorHAnsi \
  --prop font.hAnsiTheme=minorHAnsi \
  --prop font.csTheme=minorBidi \
  --prop font.eaTheme=minorEastAsia
```

**Features:** `font.asciiTheme` (majorHAnsi/minorHAnsi), `font.hAnsiTheme`, `font.csTheme` (majorBidi/minorBidi), `font.eaTheme` (majorEastAsia/minorEastAsia)

## Per-Script Font Keys

The `font` shorthand and explicit per-script keys alongside.

```bash
# All scripts at once
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=font shorthand (all scripts)" --prop font=Calibri

# Individual script slots
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=cs + ea explicit fonts" \
  --prop font.cs="Arial" --prop font.ea="SimSun"
```

**Features:** `font` (sets ascii/hAnsi/eastAsia/cs simultaneously), `font.cs`, `font.ea`

## Per-Script Language

Assign different locales to Latin, East-Asian, and complex-script ranges independently.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=lang per script (latin/ea/cs)" \
  --prop lang.latin=en-US --prop lang.ea=zh-CN --prop lang.cs=ar-SA
```

**Features:** `lang.latin`, `lang.ea`, `lang.cs` (per-script BCP-47 locale overrides)

## Run Shading & Hidden Text

Background shading on the run rectangle, vanish (hidden) flag, and spell-check suppression.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Yellow run shading" --prop shading=FFFF00
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Hidden (vanish) text" --prop vanish=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=No-proof (spellcheck off)" --prop noproof=true
```

**Features:** `shading` (hex fill color behind the run), `vanish` (hide text; Word still shows it in markup view), `noproof` (disable spell/grammar checking for this run)

## w14 Text Effects

WordprocessingML 2010 (`w14` namespace) extended visual effects — text fill, outline stroke, glow, reflection, and shadow.

```bash
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Text fill color" --prop textFill=FF0000 --prop size=16
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Text outline" --prop textOutline=1pt-FF0000 --prop size=16
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=w14 glow" --prop w14glow=FF0000 --prop size=16
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=w14 reflection" --prop w14reflection=true --prop size=16
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=w14 shadow" --prop w14shadow=FF0000 --prop size=16
```

**Features:** `textFill` (solid fill color replaces the default glyph color), `textOutline` (`width-COLOR` format, e.g. `1pt-FF0000`), `w14glow` (outer glow color), `w14reflection` (mirror reflection below), `w14shadow` (drop shadow color)

## Border, Kerning, EastAsian Layout, Run Style

Character border (`bdr`) and run-level character style (`rStyle`) must be set on explicit `--type run` children — on a paragraph element those same prop names bind the paragraph border / paragraph-mark style instead.

```bash
# Kerning threshold (applies to implicit run via paragraph)
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Kerning on (28 = 14pt threshold)" --prop kern=28

# EastAsian layout (tate-chu-yoko + kumimoji)
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=EastAsian layout 縦中横 (vert + combine)" \
  --prop eastAsianLayout.vert=true --prop eastAsianLayout.combine=true

# Character border — must be on an explicit run child
officecli add run-formatting.docx /body --type paragraph --prop "text=Boxed run: "
officecli add run-formatting.docx "/body/p[last()]" --type run \
  --prop "text=single border" --prop bdr=single
officecli add run-formatting.docx "/body/p[last()]" --type run \
  --prop "text=  red 0.5pt" --prop "bdr=single;4;FF0000;0"

# Character style — must be on an explicit run child
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Run character style: "
officecli add run-formatting.docx "/body/p[last()]" --type run \
  --prop "text=Emphasis" --prop rStyle=Emphasis
```

**Features:** `kern` (pair-kerning threshold in half-points; 28 = enable kerning for fonts ≥ 14pt), `eastAsianLayout.vert` (tate-chu-yoko vertical layout), `eastAsianLayout.combine` (kumimoji combine), `bdr` (character border: `single` or `style;size;color;space`), `rStyle` (character style ID, e.g. `Emphasis`, `Strong`)

## Emphasis Mark & Visibility Effects

Long-tail OOXML run properties handled via the generic typed-attribute fallback — they round-trip through `add`/`get` even without dedicated handler cases.

```bash
# East-Asian emphasis marks (着重号)
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=着重号 dots above (em=dot)" --prop em=dot
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=着重号 dots below (em=underDot)" --prop em=underDot
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Circle emphasis (em=circle)" --prop em=circle

# Legacy animation effect
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Legacy text animation (effect=blinkBackground)" \
  --prop effect=blinkBackground

# Web and layout flags — on explicit run children to avoid paragraph-level binding
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Hidden in web layout (webHidden)" --prop webHidden=true
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Fit run to 1 inch (fitText=1440 twips)" --prop fitText=1440
officecli add run-formatting.docx /body --type paragraph \
  --prop "text=Layout grid + special vanish: "
officecli add run-formatting.docx "/body/p[last()]" --type run \
  --prop "text=snapToGrid=false" --prop snapToGrid=false
officecli add run-formatting.docx "/body/p[last()]" --type run \
  --prop "text=  specVanish" --prop specVanish=true
```

**Features:** `em` (East-Asian emphasis mark: dot/underDot/circle/comma/underComma), `effect` (legacy animation: blinkBackground/shimmer/sparkle/…), `webHidden` (hide in web-layout view), `fitText` (compress run to fit N twips), `snapToGrid` (align to document grid), `specVanish` (special vanish; structural field-result hiding)

## Complete Feature Coverage

| Feature | Section |
|---------|---------|
| `bold`, `italic` | Weight & Style |
| `underline` variants, `underline.color` | Underline Variants |
| `strike`, `dstrike` | Strikethrough |
| `caps`, `smallcaps` | Case |
| `superscript`, `subscript`, `vertAlign` | Super / Subscript |
| `color`, `size`, `highlight` | Color, Size, Highlight |
| `font.latin`, `font.eastAsia` | Per-Script Fonts |
| `emboss`, `imprint`, `outline`, `shadow` | Text Effects |
| `charSpacing`, `position` | Character Spacing & Position |
| `lang` | Language Tag |
| `bold.cs`, `italic.cs`, `size.cs`, `rtl`, `direction` | Complex-Script Variants |
| `font.asciiTheme`, `font.hAnsiTheme`, `font.csTheme`, `font.eaTheme` | Theme Fonts |
| `font`, `font.cs`, `font.ea` | Per-Script Font Keys |
| `lang.latin`, `lang.ea`, `lang.cs` | Per-Script Language |
| `shading`, `vanish`, `noproof` | Run Shading & Hidden Text |
| `textFill`, `textOutline`, `w14glow`, `w14reflection`, `w14shadow` | w14 Text Effects |
| `kern`, `eastAsianLayout.*`, `bdr`, `rStyle` | Border, Kerning, EastAsian |
| `em`, `effect`, `webHidden`, `fitText`, `snapToGrid`, `specVanish` | Emphasis & Visibility |

## Inspect the Generated File

```bash
# List every paragraph in the document
officecli query run-formatting.docx paragraph

# Inspect a specific paragraph's run properties
officecli get run-formatting.docx "/body/p[3]"

# Inspect an explicit run child (e.g. the superscript run in E=mc²)
officecli get run-formatting.docx "/body/p[11]/r[2]"

# Check all run nodes in a paragraph
officecli query run-formatting.docx "/body/p[11]" run
```
