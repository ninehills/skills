# gpt-image-gen End-to-End Test Report

**Date:** 2026-05-02  
**Model:** gpt-image-2 via Codex OAuth → `chatgpt.com/backend-api/codex`

## Test Matrix

| # | Test Case | Category | Result | Exit Code |
|---|-----------|----------|--------|-----------|
| 1 | Syntax check: gen.py + templates.py | Compile | ✅ | — |
| 2 | --help output (both scripts) | CLI | ✅ | 0 |
| 3 | Empty --api-key → falls through to Codex token → 401 on direct API | Error | ✅ | 1 |
| 4 | --mask without -i | Error | ✅ | 2 |
| 5 | -n 0 (invalid count) | Error | ✅ | 2 |
| 6 | --size 10x10 (below min pixels) → falls back to default | Fallback | ✅ | 1 |
| 7 | templates --list (15 templates returned) | Template | ✅ | 0 |
| 8 | templates --search "水墨画海报" → poster-ink(3), poster-typo(1) | Template | ✅ | 0 |
| 9 | templates --show food → full struct + defaults + prompt_template | Template | ✅ | 0 |
| 10 | txt2img: "orange cat watercolor" --quality low --json | API-txt2img | ✅ | 0 |
| 11 | txt2img: "pixel art spaceship" --quality medium --size 1k --json | API-txt2img | ✅ | 0 |
| 12 | Full Agent workflow: search→show→craft→generate (food template) | E2E | ✅ | 0 |
| 13 | .prompt.txt sidecar exists for each generated image | Output | ✅ | — |

## Generated Images (cache dir)

```
~/.hermes/cache/images/
  20260502_074830_..._959505.png  (1.9 MB, 1024×1024, orange cat watercolor)
  20260502_074830_..._959505.prompt.txt
  20260502_074916_..._b52ee3.png  (1024×1024, pixel art spaceship)
  20260502_074916_..._b52ee3.prompt.txt
  20260502_075050_..._4cd008.png  (1024×1024, ramen food photography)
  20260502_075050_..._4cd008.prompt.txt
```

## Sidecar Format

```
# GPT Image 2 — Prompt Sidecar
# Generated: 2026-05-02 07:48:30

prompt: <full prompt>
model: gpt-image-2
size: 1024x1024
quality: low
format: png
n: 1
moderation: low
```

## Known Limitations (Codex Backend)

- Codex OAuth token only works via `chatgpt.com/backend-api/codex` (Responses API with `image_generation` tool)
- Codex path does NOT support: `--mask` inpainting, `--n` > 1, `--format` jpeg/webp, `--compression`, `--moderation`, `--background`
- These features require a standard `OPENAI_API_KEY` (api.openai.com direct)
- `images.edit` endpoint has strict content policy on real-person likeness — expect 400 errors
