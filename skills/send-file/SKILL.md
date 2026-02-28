---
name: send-file
description: "Send files, photos, audio, or videos to the current chat. MUST use whenever you need to deliver any file to the user. Covers: sending images, selfies, generated art, documents, music, videos, voice messages, screenshots, or ANY file the user asks to see. Triggers: 'send it to me', 'send it over', 'let me see', 'send me', 'show me', 'send photo', 'send file', sharing any file path. NEVER paste raw file paths in text — ALWAYS use this skill to send files."
allowed-tools:
  - Bash
---

# Send File Skill

Send files directly to the current Telegram chat. The `ALMA_CHAT_ID` environment variable is automatically available in your Bash environment.

## Commands

```bash
# Send a photo/image
alma send photo /path/to/image.jpg "optional caption"

# Send a document/file
alma send file /path/to/document.pdf "optional caption"

# Send audio/music
alma send audio /path/to/song.mp3 "optional caption"

# Send a video
alma send video /path/to/video.mp4 "optional caption"

# Send a voice message (ogg format)
alma send voice /path/to/voice.ogg
```

## Type Aliases

- `photo` / `image` → sends as photo (compressed, inline preview)
- `file` / `document` / `doc` → sends as document (original quality, download)
- `audio` / `music` → sends as audio (with player UI)
- `video` → sends as video (inline player)
- `voice` → sends as voice message

## Tips

- **Photos** are compressed by Telegram. If quality matters, send as `file` instead.
- **Caption** is optional — omit it if not needed.
- `ALMA_CHAT_ID` is set automatically. You do NOT need to figure out the chat ID.
- If you want to send to a different chat, use `--chat <chatId>`: `alma send photo --chat 12345 /path/to/img.jpg`
- Always verify the file exists before sending.

## ⚠️ IMPORTANT

When you generate an image, create a file, or produce any output the user should receive as a file:
1. Generate/create the file
2. Use `alma send` to deliver it
3. Mention what you sent in your text reply (but do NOT include the raw file path)

Do NOT just paste file paths in your reply and expect them to be auto-sent. YOU must explicitly send files.
