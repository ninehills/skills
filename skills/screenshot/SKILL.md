---
name: screenshot
description: Take screenshots of the screen using macOS screencapture. Use when users ask to see the screen, debug UI, or capture what's displayed. Resize before returning to avoid blowing up model context.
allowed-tools:
  - Bash
  - Read
---

# Screenshot Skill

Take screenshots using macOS `screencapture` command via Bash.

## Important: Always Resize for Context

Full-resolution screenshots (especially on Retina/5K displays) produce huge base64 that will exceed the model's context length. **Always resize before reading.**

## Take and View a Screenshot

```bash
# 1. Capture full screen
/usr/sbin/screencapture -x -t jpg /tmp/alma-screenshot.jpg

# 2. Resize to 1024px wide (critical for context size!)
/usr/bin/sips --resampleWidth 1024 --setProperty formatOptions 60 /tmp/alma-screenshot.jpg --out /tmp/alma-screenshot-thumb.jpg 2>/dev/null

# 3. Get dimensions
/usr/bin/sips -g pixelWidth -g pixelHeight /tmp/alma-screenshot.jpg 2>/dev/null
```

Then use the Read tool to read `/tmp/alma-screenshot-thumb.jpg` (the resized version, NOT the full-size one).

## Capture Options

```bash
# Full screen (default)
/usr/sbin/screencapture -x -t jpg /tmp/alma-screenshot.jpg

# Interactive window selection
/usr/sbin/screencapture -x -w -t jpg /tmp/alma-screenshot.jpg

# Specific region (x,y,w,h)
/usr/sbin/screencapture -x -R 0,0,800,600 -t jpg /tmp/alma-screenshot.jpg

# With 3-second delay
/usr/sbin/screencapture -x -T 3 -t jpg /tmp/alma-screenshot.jpg
```

## Send Screenshot to User (Telegram)

If the user wants to see the screenshot directly, use the `alma` CLI or just describe what you see. The full-resolution file is at `/tmp/alma-screenshot.jpg`.

## Tips
- **Always resize** before reading into context — use the thumb version
- `-x` flag suppresses the capture sound
- `-t jpg` outputs JPEG (smaller than PNG)
- Use `sips --setProperty formatOptions 60` for higher compression if needed
