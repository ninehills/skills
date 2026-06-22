# Presentation Showcase

This demo consists of three files that work together:

- **presentation.sh** — Shell script that calls `officecli raw-set` to build all slides. Raw XML is injected per-element for full design control.
- **presentation.pptx** — The generated 6-slide deck: title, pillars, data, quote, process, closing.
- **presentation.md** — This file. Maps each slide to the techniques it demonstrates.

## Regenerate

```bash
cd examples/ppt
bash presentation.sh
# → presentation.pptx
```

## Slides

### Slide 1 — Title Slide

Full-bleed 3-stop dark gradient background, two decorative semi-transparent circles, a gradient accent line, main title, two-paragraph subtitle, and a tiny rotated diamond.

```bash
officecli add presentation.pptx /presentation --type slide

# 3-stop vertical gradient background (dark navy)
officecli raw-set presentation.pptx /slide[1] \
  --xpath "//p:cSld" --action prepend --xml '
<p:bg><p:bgPr>
  <a:gradFill rotWithShape="0">
    <a:gsLst>
      <a:gs pos="0"><a:srgbClr val="0D1B2A"/></a:gs>
      <a:gs pos="50000"><a:srgbClr val="1B2838"/></a:gs>
      <a:gs pos="100000"><a:srgbClr val="0A1628"/></a:gs>
    </a:gsLst>
    <a:lin ang="5400000" scaled="1"/>
  </a:gradFill><a:effectLst/>
</p:bgPr></p:bg>'

# Decorative teal ellipse, top-right (alpha=8000 ≈ 31% opacity)
officecli raw-set presentation.pptx /slide[1] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:nvSpPr><p:cNvPr id="100" name="Deco Circle 1"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
  <p:spPr>
    <a:xfrm><a:off x="8500000" y="-1200000"/><a:ext cx="4800000" cy="4800000"/></a:xfrm>
    <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
    <a:solidFill><a:srgbClr val="00B4D8"><a:alpha val="8000"/></a:srgbClr></a:solidFill>
    <a:ln><a:noFill/></a:ln>
  </p:spPr>
  <p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:endParaRPr/></a:p></p:txBody>
</p:sp>'

# Gradient accent line (teal → lavender, horizontal)
officecli raw-set presentation.pptx /slide[1] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:nvSpPr><p:cNvPr id="102" name="Accent Line"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
  <p:spPr>
    <a:xfrm><a:off x="800000" y="4200000"/><a:ext cx="5000000" cy="0"/></a:xfrm>
    <a:prstGeom prst="line"><a:avLst/></a:prstGeom>
    <a:ln w="28575">
      <a:gradFill>
        <a:gsLst>
          <a:gs pos="0"><a:srgbClr val="00B4D8"/></a:gs>
          <a:gs pos="100000"><a:srgbClr val="E0AAFF"/></a:gs>
        </a:gsLst>
        <a:lin ang="0" scaled="1"/>
      </a:gradFill>
    </a:ln>
  </p:spPr>
  <p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:endParaRPr/></a:p></p:txBody>
</p:sp>'

# Main title: sz=5400 bold white, Segoe UI, anchor=b
officecli raw-set presentation.pptx /slide[1] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:nvSpPr><p:cNvPr id="103" name="Title"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
  <p:spPr>
    <a:xfrm><a:off x="800000" y="1600000"/><a:ext cx="8000000" cy="1200000"/></a:xfrm>
    <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
    <a:noFill/><a:ln><a:noFill/></a:ln>
  </p:spPr>
  <p:txBody>
    <a:bodyPr wrap="square" anchor="b"/>
    <a:lstStyle/>
    <a:p>
      <a:pPr algn="l"/>
      <a:r>
        <a:rPr lang="en-US" sz="5400" b="1" dirty="0">
          <a:solidFill><a:srgbClr val="FFFFFF"/></a:solidFill>
          <a:latin typeface="Segoe UI"/>
        </a:rPr>
        <a:t>The Art of Design</a:t>
      </a:r>
    </a:p>
  </p:txBody>
</p:sp>'

# Subtitle: paragraph 1 = sz=2000 cyan; paragraph 2 = sz=1400 grey, spc=600
officecli raw-set presentation.pptx /slide[1] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:nvSpPr><p:cNvPr id="104" name="Subtitle"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
  <p:spPr>
    <a:xfrm><a:off x="800000" y="2900000"/><a:ext cx="8000000" cy="1100000"/></a:xfrm>
    <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
    <a:noFill/><a:ln><a:noFill/></a:ln>
  </p:spPr>
  <p:txBody>
    <a:bodyPr wrap="square" anchor="t"/>
    <a:lstStyle/>
    <a:p>
      <a:pPr algn="l"/>
      <a:r>
        <a:rPr lang="en-US" sz="2000" dirty="0">
          <a:solidFill><a:srgbClr val="90E0EF"/></a:solidFill>
          <a:latin typeface="Segoe UI"/>
        </a:rPr>
        <a:t>Crafting Beautiful Experiences</a:t>
      </a:r>
    </a:p>
    <a:p>
      <a:pPr algn="l"/>
      <a:r>
        <a:rPr lang="en-US" sz="1400" dirty="0" spc="600">
          <a:solidFill><a:srgbClr val="8B95A2"/></a:solidFill>
          <a:latin typeface="Segoe UI"/>
        </a:rPr>
        <a:t>SIMPLICITY  ·  ELEGANCE  ·  FUNCTION</a:t>
      </a:r>
    </a:p>
  </p:txBody>
</p:sp>'

# Diamond accent: rot=2700000 (45°) tiny rect, teal fill
officecli raw-set presentation.pptx /slide[1] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:nvSpPr><p:cNvPr id="105" name="Diamond"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
  <p:spPr>
    <a:xfrm rot="2700000"><a:off x="600000" y="4050000"/><a:ext cx="200000" cy="200000"/></a:xfrm>
    <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
    <a:solidFill><a:srgbClr val="00B4D8"/></a:solidFill>
    <a:ln><a:noFill/></a:ln>
  </p:spPr>
  <p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:endParaRPr/></a:p></p:txBody>
</p:sp>'
```

**Features:** `raw-set --action prepend` (background), `raw-set --action append` (shapes), 3-stop `gradFill` background, `solidFill` with `alpha`, gradient line stroke (`a:ln/a:gradFill`), `prstGeom prst="ellipse"/"line"/"rect"`, `txBox="1"`, `bodyPr anchor="b"/"t"`, `sz` (hundredths of a point), `b="1"` bold, `spc` letter-spacing, `rot` rotation, `a:latin typeface=`

### Slide 2 — Three Pillars

Dark solid background with three rounded-rectangle cards side by side, each containing a Unicode symbol icon, bold title, and body text.

```bash
officecli add presentation.pptx /presentation --type slide

# Solid background
officecli raw-set presentation.pptx /slide[2] \
  --xpath "//p:cSld" --action prepend --xml '
<p:bg><p:bgPr>
  <a:solidFill><a:srgbClr val="0D1B2A"/></a:solidFill>
  <a:effectLst/>
</p:bgPr></p:bg>'

# Section title: sz=3200 bold white centered
# Sub-line: sz=1400 grey centered
officecli raw-set presentation.pptx /slide[2] \
  --xpath "//p:cSld/p:spTree" --action append --xml '...'

# Card 1 — Simplicity: roundRect adj=8000, solidFill 152238, ln 1E3A5F
# Icon: sz=4800 teal ○; Title: sz=2400 bold white; Body: sz=1200 grey
officecli raw-set presentation.pptx /slide[2] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:nvSpPr><p:cNvPr id="210" name="Card1"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
  <p:spPr>
    <a:xfrm><a:off x="900000" y="2000000"/><a:ext cx="3200000" cy="4200000"/></a:xfrm>
    <a:prstGeom prst="roundRect">
      <a:avLst><a:gd name="adj" fmla="val 8000"/></a:avLst>
    </a:prstGeom>
    <a:solidFill><a:srgbClr val="152238"/></a:solidFill>
    <a:ln w="12700"><a:solidFill><a:srgbClr val="1E3A5F"/></a:solidFill></a:ln>
  </p:spPr>
  <p:txBody>
    <a:bodyPr wrap="square" lIns="228600" tIns="228600" rIns="228600" bIns="228600" anchor="t"/>
    <a:lstStyle/>
    <!-- paragraph: icon ○ sz=4800 teal -->
    <!-- paragraph: empty spacer -->
    <!-- paragraph: "Simplicity" sz=2400 bold white -->
    <!-- paragraph: body text sz=1200 grey -->
  </p:txBody>
</p:sp>'

# Card 2 — Hierarchy: same structure, lavender △ icon
# Card 3 — Harmony: same structure, amber ◇ icon
```

**Features:** `solidFill` background, `prstGeom prst="roundRect"` with `a:gd name="adj" fmla="val 8000"` (corner rounding ≈8%), `a:ln w=` border thickness, `bodyPr lIns/tIns/rIns/bIns` padding, `anchor="t"`, mixed `sz` paragraphs, Unicode in `a:t` (`&#x25CB;` ○, `&#x25B3;` △, `&#x25C7;` ◇), `algn="ctr"` paragraph alignment

### Slide 3 — Data Showcase

Left-to-right gradient background, title, thin gradient accent bar, three stat cards with gradient borders.

```bash
officecli add presentation.pptx /presentation --type slide

# 2-stop diagonal gradient background
officecli raw-set presentation.pptx /slide[3] \
  --xpath "//p:cSld" --action prepend --xml '
<p:bg><p:bgPr>
  <a:gradFill rotWithShape="0">
    <a:gsLst>
      <a:gs pos="0"><a:srgbClr val="0D1B2A"/></a:gs>
      <a:gs pos="100000"><a:srgbClr val="152238"/></a:gs>
    </a:gsLst>
    <a:lin ang="2700000" scaled="1"/>
  </a:gradFill><a:effectLst/>
</p:bgPr></p:bg>'

# Thin gradient accent bar (rect, no text, gradFill fill)
officecli raw-set presentation.pptx /slide[3] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <!-- off y=1050000, ext cx=3000000 cy=50000 — purely decorative horizontal bar -->
  <!-- gradFill: 00B4D8 → E0AAFF, ang=0 (left→right) -->
</p:sp>'

# Stat card 1 — "98%": roundRect solidFill 0E2540, gradient border stroke
officecli raw-set presentation.pptx /slide[3] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:spPr>
    <a:ln w="19050">
      <a:gradFill>
        <a:gsLst>
          <a:gs pos="0"><a:srgbClr val="00B4D8"/></a:gs>
          <a:gs pos="100000"><a:srgbClr val="0077B6"/></a:gs>
        </a:gsLst>
        <a:lin ang="5400000" scaled="1"/>
      </a:gradFill>
    </a:ln>
  </p:spPr>
  <!-- sz=5600 bold teal big number; sz=1400 grey label -->
</p:sp>'
```

**Features:** 2-stop `gradFill` at 270° (`ang="2700000"`), thin decorative rect (height `cy=50000`), gradient line/border (`a:ln/a:gradFill`), `anchor="ctr"` vertical centering, mixed font sizes in single `txBody`

### Slide 4 — Quote Slide

3-stop gradient background, very large low-alpha quote-mark glyph, italic Georgia quote text, teal attribution, and a fade-center accent line.

```bash
officecli add presentation.pptx /presentation --type slide

# 3-stop gradient background
# Large quote mark: sz=12000, solidFill alpha=20000 (very transparent)
officecli raw-set presentation.pptx /slide[4] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:nvSpPr><p:cNvPr id="400" name="QuoteMark"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
  <p:txBody>
    <a:bodyPr wrap="square" anchor="t"/>
    <a:lstStyle/>
    <a:p>
      <a:pPr algn="l"/>
      <a:r>
        <a:rPr lang="en-US" sz="12000" dirty="0">
          <a:solidFill><a:srgbClr val="00B4D8"><a:alpha val="20000"/></a:srgbClr></a:solidFill>
          <a:latin typeface="Georgia"/>
        </a:rPr>
        <a:t>&#x201C;</a:t>
      </a:r>
    </a:p>
  </p:txBody>
</p:sp>'

# Quote text: sz=2800 italic Georgia, 2 paragraphs
officecli raw-set presentation.pptx /slide[4] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <!-- i="1" italic, Georgia, sz=2800, anchor=ctr -->
</p:sp>'

# Attribution line: sz=1600 teal 00B4D8, en-dash prefix
# Fading accent line: 3-stop gradFill (alpha=0 at ends, full at 50%)
officecli raw-set presentation.pptx /slide[4] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <!-- line shape, ln gradFill: pos=0 alpha=0, pos=50000 full, pos=100000 alpha=0 -->
</p:sp>'
```

**Features:** `sz=12000` giant glyph, `a:alpha val="20000"` (low opacity ≈8%), `i="1"` italic, `a:latin typeface="Georgia"`, 3-stop gradient line fading at both ends (creating a center-glow effect), `&#x201C;` left double quote, `&#x2014;` em-dash

### Slide 5 — Process / Timeline

Dark solid background, title, 4-stop rainbow horizontal connector line, four numbered ellipse steps (semi-transparent fill + colored border), and four text labels below.

```bash
officecli add presentation.pptx /presentation --type slide

# Title: sz=3200 bold white centered
# 4-stop rainbow gradient connector line (horizontal)
officecli raw-set presentation.pptx /slide[5] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:spPr>
    <a:xfrm><a:off x="1800000" y="2800000"/><a:ext cx="8600000" cy="0"/></a:xfrm>
    <a:prstGeom prst="line"><a:avLst/></a:prstGeom>
    <a:ln w="25400">
      <a:gradFill>
        <a:gsLst>
          <a:gs pos="0"><a:srgbClr val="00B4D8"/></a:gs>
          <a:gs pos="33000"><a:srgbClr val="E0AAFF"/></a:gs>
          <a:gs pos="66000"><a:srgbClr val="FFD166"/></a:gs>
          <a:gs pos="100000"><a:srgbClr val="06D6A0"/></a:gs>
        </a:gsLst>
        <a:lin ang="0" scaled="1"/>
      </a:gradFill>
    </a:ln>
  </p:spPr>
  <p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:endParaRPr/></a:p></p:txBody>
</p:sp>'

# Loop: 4 step circles (ellipse, fill alpha=15000, border solid color)
LABELS=("Research" "Ideate" "Design" "Validate")
COLORS=("00B4D8" "E0AAFF" "FFD166" "06D6A0")
XPOS=(1400000 3600000 5800000 8000000)
for i in 0 1 2 3; do
  officecli raw-set presentation.pptx "/slide[5]" \
    --xpath "//p:cSld/p:spTree" --action append --xml "
<p:sp>
  <p:spPr>
    <a:xfrm><a:off x=\"${XPOS[$i]}\" y=\"2200000\"/><a:ext cx=\"1200000\" cy=\"1200000\"/></a:xfrm>
    <a:prstGeom prst=\"ellipse\"><a:avLst/></a:prstGeom>
    <a:solidFill><a:srgbClr val=\"${COLORS[$i]}\"><a:alpha val=\"15000\"/></a:srgbClr></a:solidFill>
    <a:ln w=\"38100\"><a:solidFill><a:srgbClr val=\"${COLORS[$i]}\"/></a:solidFill></a:ln>
  </p:spPr>
  <p:txBody>
    <a:bodyPr wrap=\"square\" anchor=\"ctr\"/>
    <a:lstStyle/>
    <a:p><a:pPr algn=\"ctr\"/><a:r><a:rPr lang=\"en-US\" sz=\"2400\" b=\"1\" dirty=\"0\">
      <a:solidFill><a:srgbClr val=\"${COLORS[$i]}\"/></a:solidFill>
    </a:rPr><a:t>0$((i+1))</a:t></a:r></a:p>
  </p:txBody>
</p:sp>"
done
```

**Features:** 4-stop rainbow gradient line stroke, `prstGeom prst="ellipse"`, very low fill alpha (`val="15000"` ≈ 6%), thick border `a:ln w="38100"`, `anchor="ctr"` for centered number label, bash loop for repeated shape generation

### Slide 6 — Closing

Gradient ring (ellipse with gradient stroke, no fill), large "Thank You" heading, closing subtitle, three tiny diamond accent shapes.

```bash
officecli add presentation.pptx /presentation --type slide

# 3-stop diagonal gradient background
# Gradient ring: large ellipse, noFill, ln w=12700 gradFill 3-stop with alpha
officecli raw-set presentation.pptx /slide[6] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:spPr>
    <a:xfrm><a:off x="3596000" y="800000"/><a:ext cx="5000000" cy="5000000"/></a:xfrm>
    <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
    <a:noFill/>
    <a:ln w="12700">
      <a:gradFill>
        <a:gsLst>
          <a:gs pos="0"><a:srgbClr val="00B4D8"><a:alpha val="30000"/></a:srgbClr></a:gs>
          <a:gs pos="50000"><a:srgbClr val="E0AAFF"><a:alpha val="30000"/></a:srgbClr></a:gs>
          <a:gs pos="100000"><a:srgbClr val="FFD166"><a:alpha val="30000"/></a:srgbClr></a:gs>
        </a:gsLst>
        <a:lin ang="2700000" scaled="1"/>
      </a:gradFill>
    </a:ln>
  </p:spPr>
  <p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:endParaRPr/></a:p></p:txBody>
</p:sp>'

# "Thank You": sz=4800 bold white centered
# Closing subtitle: sz=1600 light-teal 90E0EF
# Three tiny diamonds: rot=2700000 rects, cx=cy=120000, teal/lavender/amber
officecli raw-set presentation.pptx /slide[6] \
  --xpath "//p:cSld/p:spTree" --action append --xml '
<p:sp>
  <p:spPr>
    <a:xfrm rot="2700000"><a:off x="5850000" y="4700000"/><a:ext cx="120000" cy="120000"/></a:xfrm>
    <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
    <a:solidFill><a:srgbClr val="00B4D8"/></a:solidFill>
    <a:ln><a:noFill/></a:ln>
  </p:spPr>
  <p:txBody><a:bodyPr/><a:lstStyle/><a:p><a:endParaRPr/></a:p></p:txBody>
</p:sp>'
```

**Features:** `a:noFill` (transparent shape body), gradient stroke ring effect (`a:ln/a:gradFill` with alpha), tiny rotated rect (`cx=cy=120000` EMU, `rot=2700000`), three-color accent trio (teal/lavender/amber)

## Complete Feature Coverage

| Feature | Slides |
|---------|--------|
| **Backgrounds:** solid, 2-stop gradient, 3-stop gradient | 2, 3, 1/4/6 |
| **Gradient angles:** 0° horizontal, 270° vertical, 90° inverted | 1, 3, 5 |
| **Alpha / transparency on fills** (`a:alpha val=`) | 1, 4, 5 |
| **Alpha / transparency on stroke** | 6 |
| **Gradient fill stroke** (`a:ln/a:gradFill`) | 1, 5, 6 |
| **Gradient fill as shape body** | 3 (accent bar) |
| **prstGeom:** ellipse, line, rect, roundRect | all |
| **roundRect adj corner radius** (`a:gd name="adj"`) | 2, 3 |
| **noFill** (transparent shape body) | 1, 4, 6 |
| **txBox="1"** text-only box | 1, 4 |
| **bodyPr anchor:** t / b / ctr | all |
| **bodyPr padding:** lIns/tIns/rIns/bIns | 2 |
| **Font sizes** 1200–12000 hundredths-of-point range | all |
| **Bold** (`b="1"`), **italic** (`i="1"`) | 1, 4 |
| **Letter-spacing** (`spc=`) | 1 |
| **Typefaces:** Segoe UI, Georgia | 1, 4, 5 |
| **Per-run solidFill** | all |
| **Multi-paragraph txBody with mixed sizes** | 1, 2, 3, 4 |
| **Paragraph alignment** (`algn="l"/"ctr"`) | all |
| **Rotation** (`a:xfrm rot=2700000`) | 1, 6 |
| **Unicode glyphs in a:t** | 1, 2, 4 |
| **Bash loop for repeated shapes** | 5, 6 |
| **raw-set --action prepend** (background injection) | all |
| **raw-set --action append** (shape injection) | all |
| **Decorative non-text shapes** (accent bars, rings, diamonds) | 1, 3, 4, 6 |

## Inspect the Generated File

```bash
officecli query presentation.pptx slide
officecli get presentation.pptx /slide[1]
officecli get presentation.pptx "/slide[2]/shape[3]"
officecli get presentation.pptx "/slide[5]/shape[2]"
```
