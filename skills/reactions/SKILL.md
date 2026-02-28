---
name: reactions
description: React to a message with an emoji. Works on Telegram, Discord, and Feishu.
allowed-tools:
    - Bash
---

# Reactions Skill

React to a message with an emoji reaction. Platform is auto-detected from the current chat context.

## Telegram

```bash
curl -s -X POST http://localhost:23001/api/reaction/set \
  -H 'Content-Type: application/json' \
  -d '{"emoji": "👌", "messageId": 12345}'
```

## Discord

```bash
curl -s -X POST http://localhost:23001/api/discord/reaction \
  -H 'Content-Type: application/json' \
  -d '{"channelId": "CHANNEL_ID", "messageId": "MSG_ID", "emoji": "👌"}'
```

## Feishu

Feishu uses emoji type strings (e.g. "THUMBSUP", "HEART", "LAUGH", "OK", "FIRE").

```bash
curl -s -X POST http://localhost:23001/api/feishu/messages/MSG_ID/reaction \
  -H 'Content-Type: application/json' \
  -d '{"emoji": "THUMBSUP"}'
```

Common Feishu emoji types: THUMBSUP, HEART, LAUGH, SURPRISED, CRY, OK, FIRE, CLAP, PRAY, MUSCLE, PARTY, FACEPALM, JIAYI (加一/+1)

## Emoji

Choose any emoji that fits the mood naturally. Common ones: 👍 ❤ 🎉 😂 😢 😮 🔥 🤔 👎 🙏 🥰 🤣 👌 🫡 💯 🤝

Note: Each platform has different supported emoji. If rejected, just means that emoji isn't supported on the platform.

## When to Use

- Message evokes genuine emotion — excitement, gratitude, humor, surprise
- Something funny or heartwarming
- A simple acknowledgment when words aren't needed
- **In group chats: react 👌 when you agree to someone's request** (use messageId from `[msg:xxx]`)

## When NOT to Use

- Don't react to every message
- Neutral or factual messages don't need reactions
