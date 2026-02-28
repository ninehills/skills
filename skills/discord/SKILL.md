---
name: discord
description: 'Interact with Discord: send messages, photos, files to any channel. Manage Discord bot integration with Alma.'
allowed-tools:
    - Bash
---

# Discord Bot Skill

Alma can connect to Discord via a bot and participate in guild channels.

## Setup

1. Create a Discord bot at https://discord.com/developers/applications
2. Enable **MESSAGE CONTENT** intent in Bot settings
3. Generate a bot token
4. Add the bot to your server with these permissions: Send Messages, Read Message History, Add Reactions, Attach Files
5. Configure in Alma settings:

```bash
# Set Discord settings via API
curl -X PUT http://localhost:23001/api/settings \
  -H "Content-Type: application/json" \
  -d '{
    "discord": {
      "enabled": true,
      "botToken": "YOUR_BOT_TOKEN",
      "allowedGuildIds": [],
      "allowedChannelIds": []
    }
  }'
```

## Sending Messages

```bash
# Send a text message to a Discord channel
curl -s http://localhost:23001/api/discord/send \
  -H "Content-Type: application/json" \
  -d '{"channelId": "CHANNEL_ID", "message": "Hello from Alma!"}'

# Send a photo
curl -s http://localhost:23001/api/discord/send-photo \
  -H "Content-Type: application/json" \
  -d '{"channelId": "CHANNEL_ID", "filePath": "/path/to/image.jpg", "caption": "Check this out!"}'

# Send a file
curl -s http://localhost:23001/api/discord/send-file \
  -H "Content-Type: application/json" \
  -d '{"channelId": "CHANNEL_ID", "filePath": "/path/to/doc.pdf", "caption": "Here you go"}'
```

## Stickers

```bash
# List all stickers from all servers the bot is in
alma discord sticker-list

# List stickers from a specific server
alma discord sticker-list <guildId>

# Search stickers by name
alma discord sticker-find <query>

# Send a sticker to a channel
alma discord sticker <channelId> <stickerId>
```

## Direct Messages (DM)

```bash
# Send a DM to a Discord user
alma discord dm <userId> "Hello, this is a private message!"
```

- The user must share a server with the bot, or have DMs enabled
- userId is the Discord numeric user ID (find in people profiles under `discord_id`)

## Delete Messages

```bash
# Delete (retract) a message
alma discord delete <channelId> <messageId>
```

- Bot can only delete its own messages, or messages in channels where it has Manage Messages permission

## Bot Commands (in Discord)

Users can use these commands in Discord:

- `/help` or `!help` — Show help
- `/new [title]` — Start a new conversation
- `/stop` — Stop current generation
- `/model` — Show current model

## Notes

- The bot requires **MESSAGE_CONTENT** intent to read message content
- Discord has a 2000 character limit per message — long responses are automatically split
- File attachments (images, documents) sent by users are downloaded and passed to Alma
- The bot responds to: direct mentions (@Alma), replies to bot messages, messages containing "alma", and DMs
- Group chat history is tracked per channel for context-aware responses
