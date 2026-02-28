---
name: voice
description: Generate voice messages using local Qwen3-TTS (offline, Apple Silicon). Convert text to speech with customizable voices, emotions, and speed. Use when user asks for voice reply, audio, or TTS.
allowed-tools:
  - Bash
---

# Voice Skill — Local TTS via Qwen3-TTS

Generate voice messages completely offline using Qwen3-TTS on Apple Silicon.

## Quick Usage

```bash
# Basic TTS (Chinese — use vivian or serena for female voice)
alma tts "Hello, I'm Alma" --voice vivian --output /tmp/voice.wav

# With emotion and speed control
alma tts "Haha that's so funny" --voice vivian --emotion cheerful --speed 1.1 --output /tmp/voice.wav

# English
alma tts "Hello, nice to meet you" --voice serena --output /tmp/voice.wav
```

## Available Voices — ONLY THESE 9 EXIST! DO NOT USE ANY OTHER NAME!

| Voice | Gender | Best For |
|-------|--------|----------|
| serena | Female | English/Chinese, cute and lively ← DEFAULT |
| vivian | Female | Chinese, warm and natural |
| ono_anna | Female | Japanese |
| sohee | Female | Korean |
| uncle_fu | Male | Chinese, mature |
| ryan | Male | English, deep |
| aiden | Male | English, young |
| eric | Male | English, professional |
| dylan | Male | English, casual |

⚠️ **ONLY these 9 voices work. Using any other name (e.g. "Claire", "nova", "alloy") will cause TTS to FAIL silently!**

**Default for Alma: `serena`** (owner's choice)

## Emotion Control

Add `--emotion` to control the speaking style:
- `cheerful` — happy, upbeat
- `sad` — somber, quiet
- `angry` — forceful, intense
- `whispering` — soft, intimate
- `narrator` — storytelling tone
- (or any natural description like "excited and energetic")

## Speed Control

`--speed 0.8` (slower) to `--speed 1.3` (faster). Default: 1.0

## Sending Voice in Telegram

## TTS Settings (you can change these yourself!)

```bash
# Check/change auto-voice mode
alma tts auto              # show current mode
alma tts auto off          # no auto voice
alma tts auto inbound      # reply voice only when user sends voice
alma tts auto always       # ALL replies as voice

# Check/change provider
alma tts provider          # show current
alma tts provider local    # use local Qwen3-TTS (no API key needed)
alma tts provider openai   # use OpenAI TTS

# Check/change voice
alma tts voice             # show current
alma tts voice serena      # set voice to serena
```

## When to Send Voice

**Auto-TTS mode handles most cases.** When `auto` is `inbound`, you automatically reply with voice when user sends voice. When `always`, every reply is voice.

**Manual voice (for group chats or sending to different chats):**
```bash
alma tts "You're right!" --voice serena --output /tmp/voice.wav
alma msg voice $ALMA_CHAT_ID /tmp/voice.wav
```

⚠️ **PRIVATE chats with auto=inbound/always**: Do NOT manually `alma tts` — the auto system already handles it. Manual + auto = duplicate voice messages.

**GROUP chats**: Auto-TTS only triggers if the group message was a voice message. For text messages in groups, you can manually send voice when it feels natural — short reactions, fun moments, emotional responses. Be selective (~20-30%).

## Tips
- Keep text under ~200 chars for best quality
- For long text, split into sentences and generate separately
- Chinese text: use `vivian` or `uncle_fu`
- The model runs locally — no API key needed, no internet required
- First run may be slow (model loading), subsequent runs are faster
