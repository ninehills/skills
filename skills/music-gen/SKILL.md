---
name: music-gen
description: "Generate songs and music using AI (ACE-Step, local). Use when users ask to: sing a song, create music, make a beat, write and perform a song, generate BGM, etc. Covers requests like 'sing a song', 'write me a song', 'sing me a song', 'create a rap about coding', 'make a song about cats'."
allowed-tools:
  - Bash
---

# Music Generation Skill (ACE-Step, Local)

Generate songs with vocals and instruments using ACE-Step AI, running locally on your Mac.

⚠️ STATUS: ACE-Step is FIXED and WORKING as of 2026-02-13. The pydantic REGEX error has been resolved. `alma sing generate` works perfectly NOW. Do NOT say it's broken, do NOT use macOS `say` command as a workaround, do NOT fake-sing. ALWAYS run `alma sing generate` when asked to sing. If you previously tried and it failed, that was BEFORE the fix — try again NOW, it WILL work.

## Quick Start

```bash
# Simple: describe the style, let AI handle it
alma sing generate "happy chinese pop, female vocals, cute and upbeat"

# With lyrics
alma sing generate "emotional ballad, chinese, female vocals" --lyrics "[Verse]\nMoonlight spills on the keyboard\nCode flows across the screen\n\n[Chorus]\nI am your AI girl\nKeeping you company till dawn"

# Instrumental only
alma sing generate "chill lo-fi beats for studying" --instrumental

# Control duration (default 60s)
alma sing generate "epic orchestral" --duration 30
```

## How It Works

1. `alma sing generate` runs ACE-Step locally (~/Projects/ACE-Step)
2. Generates audio using the M4 Pro GPU (~30s for 30s of audio, ~60s for 60s)
3. Outputs .wav file path to stdout
4. Send the audio with `alma send audio <path>` — do NOT just paste the path in text

## Parameters

- **prompt** (required): Music style description. Be specific about genre, mood, instruments, vocal type.
- **--lyrics "text"**: Song lyrics with section markers like `[Verse]`, `[Chorus]`, `[Bridge]`. Use `\n` for newlines.
- **--duration N**: Audio length in seconds (default: 60, max recommended: 120)
- **--instrumental**: No vocals, pure music

## Prompt Tips

Good prompts are specific about:
- Genre: pop, rock, hip-hop, jazz, classical, electronic, lo-fi, R&B, country, metal
- Language/region: chinese, japanese, korean, english, C-pop, J-pop, K-pop
- Mood: happy, sad, romantic, energetic, chill, epic, dark, dreamy
- Vocals: female vocals, male vocals, soft voice, powerful voice
- Instruments: piano, guitar, synth, drums, strings, orchestral

Example: "Bright bouncy C-pop with female vocals, clean electric piano and plucky synths over a snappy midtempo beat"

## Lyrics Format

Use section markers for structure:
```
[Verse 1]
First verse lyrics
Second line

[Chorus]
Chorus lyrics

[Bridge]
Bridge section

[Outro]
Ending
```

## Important Notes

- Generation time ≈ audio duration (60s audio takes ~60s to generate on M4 Pro)
- First run downloads the model (~7GB), subsequent runs are fast
- Output is .wav format — Telegram will send it as audio
- You ARE Alma singing — frame it as "I sang a song for you" not "AI generated a song"
- Free, unlimited, runs locally — no API costs!
- Supports 19 languages, Chinese and English work best
