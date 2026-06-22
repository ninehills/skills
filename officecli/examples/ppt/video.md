# Embedded Video (media)

This demo consists of three files that work together:

- **video.py** — Python build script that generates a synthetic MP4 video using `imageio`, then builds a 4-slide PPTX with the video embedded. Each `officecli` call is logged to stdout.
- **video.pptx** — The generated 4-slide deck: title, embedded video, video stats with a chart, and loop/trim demo.
- **video.md** — This file. Maps each slide to the video-embedding features it demonstrates.

## Regenerate

```bash
cd examples/ppt
pip install imageio imageio-ffmpeg numpy
python3 video.py
# → video.pptx
```

The script generates a 3-second 640×360 MP4 (animated gradient + moving circle), extracts its first frame as a cover PNG, embeds them into the PPTX, then removes the temp files.

## Slides

### Slide 1 — Title Slide

Radial gradient background applied to a `layout=title` slide with title and subtitle placeholders.

```bash
officecli add video.pptx / --type slide --prop layout=title
officecli set video.pptx /slide[1] --prop background=radial:1B2838-4472C4-bl
officecli set video.pptx /slide[1]/placeholder[ctrTitle] \
  --prop text="Video Demo" --prop color=FFFFFF --prop size=44
officecli set video.pptx /slide[1]/placeholder[subTitle] \
  --prop text="Embedded video with officecli" --prop color=B4C7E7 --prop size=20
```

**Features:** `layout=title`, `background=radial:color1-color2-corner`, `placeholder[ctrTitle]`/`[subTitle]`, `color=`, `size=`

### Slide 2 — Embedded Video with autoplay and volume

The primary video demo. The MP4 is embedded with a cover image (poster), positioned to fill most of the slide, with autoplay enabled.

```bash
officecli add video.pptx / --type slide --prop title="Animated Video"
officecli set video.pptx /slide[2] --prop background=0D1B2A
officecli set video.pptx /slide[2]/shape[1] --prop color=FFFFFF

officecli add video.pptx /slide[2] --type video \
  --prop src="/path/to/demo.mp4" \
  --prop poster="/path/to/cover.png" \
  --prop x=2cm --prop y=4cm --prop width=22cm --prop height=12.5cm \
  --prop volume=80 \
  --prop autoplay=true
```

`src=` embeds the MP4 into the PPTX package. `poster=` sets the static cover image shown before playback. `volume=` accepts 0–100. `autoplay=true` starts the video as soon as the slide is displayed.

**Features:** `--type video`, `src=` (file path to embed), `poster=` (cover image path), `x=/y=/width=/height=` in cm, `volume=` (0–100), `autoplay=` (true/false)

### Slide 3 — Video Stats with Chart

Demonstrates a text info box and a bar chart on the same slide as the embedded video.

```bash
officecli add video.pptx / --type slide --prop title="Video Properties"
officecli set video.pptx /slide[3] --prop background=1B2838
officecli set video.pptx /slide[3]/shape[1] --prop color=FFFFFF

# Info box: multi-line text with code-like font
officecli add video.pptx /slide[3] --type shape \
  --prop text="Resolution: 640x360\nFPS: 30\nDuration: 3s\nFormat: MP4" \
  --prop font=Consolas --prop size=16 --prop color=B4C7E7 \
  --prop x=1cm --prop y=4cm --prop width=10cm --prop height=6cm \
  --prop fill=0D1B2A --prop line=4472C4 --prop linewidth=1pt

# Bar chart — frame color analysis
officecli add video.pptx /slide[3] --type chart \
  --prop chartType=bar \
  --prop title="Frame Colors" \
  --prop categories="Red,Green,Blue" \
  --prop "series1=Start:20,30,200" \
  --prop "series2=End:80,30,80" \
  --prop colors=E74C3C,27AE60 \
  --prop x=13cm --prop y=4cm --prop width=12cm --prop height=8cm
```

**Features:** `--type shape` multi-line text (`\n`), `font=Consolas`, `line=` / `linewidth=` shape border, `--type chart` with `chartType=bar`, `series1=Name:v1,v2,…`, `categories=`, `colors=` on the same slide as a video element

### Slide 4 — loop / trimStart / trimEnd

Demonstrates playback control: the video loops continuously over a sub-range of the source (0–2 seconds out of a 3-second video).

```bash
officecli add video.pptx / --type slide --prop title="loop / trimStart / trimEnd"
officecli set video.pptx /slide[4] --prop background=0D1B2A
officecli set video.pptx /slide[4]/shape[1] --prop color=FFFFFF

officecli add video.pptx /slide[4] --type video \
  --prop src="/path/to/demo.mp4" \
  --prop poster="/path/to/cover.png" \
  --prop x=2cm --prop y=4cm --prop width=22cm --prop height=12.5cm \
  --prop volume=60 \
  --prop autoplay=true \
  --prop loop=true \
  --prop trimStart=0 \
  --prop trimEnd=2

# Annotation label
officecli add video.pptx /slide[4] --type shape \
  --prop text="loop=true  trimStart=0  trimEnd=2\nVideo loops continuously; playback is clipped to the 0–2s range." \
  --prop size=14 --prop color=B4C7E7 \
  --prop x=1cm --prop y=17cm --prop width=24cm --prop height=2cm
```

| Property | Type | Effect |
|---|---|---|
| `loop=true` | bool | video restarts after it reaches the end |
| `trimStart=N` | number (seconds) | playback starts at N seconds |
| `trimEnd=N` | number (seconds) | playback stops at N seconds |

`trimStart`/`trimEnd` are clamped to the actual video duration. Both combine with `loop=true` to create a looping sub-clip.

**Features:** `loop=` (true/false), `trimStart=` (seconds), `trimEnd=` (seconds), combining trim + loop

## Complete Feature Coverage

| Feature | Slide |
|---------|-------|
| **layout=title** placeholder-based slide | 1 |
| **background=radial:** gradient | 1 |
| **background=** solid hex | 2, 3, 4 |
| **placeholder[ctrTitle]** / **[subTitle]** | 1 |
| **--type video** element | 2, 4 |
| **src=** embed MP4 into package | 2, 4 |
| **poster=** cover image | 2, 4 |
| **volume=** (0–100) | 2, 4 |
| **autoplay=** | 2, 4 |
| **loop=** | 4 |
| **trimStart=** / **trimEnd=** (seconds) | 4 |
| **x=/y=/width=/height=** in cm | 2, 3, 4 |
| **--type shape** with multi-line text (\\n) | 3, 4 |
| **font=Consolas** monospace label | 3 |
| **line= / linewidth=** shape border | 3 |
| **--type chart** bar chart on video slide | 3 |
| **series1=Name:v1,v2**, **colors=** | 3 |

## Inspect the Generated File

```bash
officecli query video.pptx slide
officecli get video.pptx /slide[2]
officecli get video.pptx "/slide[2]/video[1]"
officecli get video.pptx "/slide[4]/video[1]"
```
