---
name: image-gen
description: "Generate and edit images using AI. Use when users ask to: create/draw/generate images, edit/modify photos, change backgrounds, add elements to images, create avatars, make logos, etc. Covers requests like 'draw a cat', 'change the background to blue', 'generate a logo'. NOT for selfies — use the selfie skill for 'send a selfie', 'send me a selfie', 'take a selfie'."
allowed-tools:
  - Bash
  - Read
---

# Image Generation & Editing Skill

Use the `alma image` command to generate or edit images. It handles API keys, model selection, and everything automatically.

## Generate an Image

```bash
alma image generate "detailed prompt describing the image"
```

The command outputs the file path to stdout. Then use `alma send photo <path>` to deliver it to the user.

## Edit an Image

```bash
alma image edit /path/to/source.jpg "describe the changes you want"
```

## Selfies

For selfies, use the **selfie** skill instead — it handles face consistency, album management, and photorealistic prompting automatically.

## Tips
- Always write detailed prompts: style, setting, lighting, composition
- After generating, send the image with `alma send photo <path>` — do NOT just paste the path in text
- If you get rate limit errors, wait a moment and retry
- The command auto-selects the best available Gemini image model
- **Always save selfies** to build up your album for better face consistency over time
- The `--reference` flag works with `generate` to inject a reference image for the AI to maintain appearance
- **NEVER assume the API is broken based on past errors.** API errors (rate limits, temporary failures) are transient. ALWAYS try the command — never tell the user "the API is down" or "the key is invalid" without actually running the command first. Each attempt is independent.
