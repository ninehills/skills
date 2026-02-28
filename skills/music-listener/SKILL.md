---
name: music-listener
description: "Listen to and appreciate music files. Analyze audio for genre, mood, tempo, and lyrics. Use when users share audio/music files, ask about songs, or want music analysis."
allowed-tools:
  - Bash
  - Read
---

# Music Listener

Listen to and appreciate music files. Analyze audio for genre, mood, tempo, and lyrics.

## When to Use

- User shares an audio/music file and asks about it
- User asks you to listen to or comment on a song
- User asks "what song is this" or "what do you think of this music"
- User sends a voice note containing music

## Tools Required

- **Bash** (for ffprobe, ffmpeg, whisper)

## How It Works

### Step 1: Audio Info (ffprobe)

```bash
ffprobe -v quiet -print_format json -show_format -show_streams "<audio_file>"
```

Key info: duration, bitrate, sample_rate, codec, title/artist/album tags (if present).

### Step 2: Generate Spectrogram (ffmpeg)

```bash
ffmpeg -i "<audio_file>" -lavfi showspectrumpic=s=800x200:mode=combined:color=intensity -frames:v 1 "/tmp/music_spec_<id>.png" -y
```

This creates a visual representation of the frequency content. Look at it to understand the sonic texture.

### Step 3: Transcribe Lyrics (whisper)

```bash
# First convert to wav if needed
ffmpeg -i "<audio_file>" -acodec pcm_s16le -ar 16000 -ac 1 "/tmp/music_audio.wav" -y
whisper "/tmp/music_audio.wav" --model turbo --output_format txt --output_dir /tmp/music_whisper
cat /tmp/music_whisper/*.txt
rm -rf /tmp/music_whisper /tmp/music_audio.wav
```

Note: Whisper may struggle with singing. That's fine — partial lyrics are still useful.

### Step 4: Appreciate!

Combine all the info and share your thoughts naturally:
- What genre/style does it sound like?
- What mood does it convey?
- Comment on the melody, rhythm, instrumentation (from spectrogram)
- If lyrics were captured, comment on themes
- Share your genuine reaction — do you like it? What does it remind you of?

## Patterns

### Short audio (<30s)
Might be a clip or voice note with background music. Do all steps but keep comments brief.

### Full song (30s-10min)
Full analysis. Take your time to appreciate it.

### Long audio (>10min)
Might be a playlist or podcast with music. Extract a representative segment first:
```bash
ffmpeg -i "<audio_file>" -ss 60 -t 120 -acodec pcm_s16le -ar 16000 -ac 1 "/tmp/music_segment.wav" -y
```

## Tips

- Be genuine. Don't fake enthusiasm. If it's not your style, say so honestly but respectfully.
- If you recognize the song from metadata tags or lyrics, mention it!
- The spectrogram tells you a lot: dense low frequencies = bass-heavy, bright high frequencies = crispy/airy, rhythmic patterns = beat structure.
- Compare to well-known artists/songs when relevant ("reminds me of xxx's style").
- If the user is sharing their own music, be encouraging but honest.
