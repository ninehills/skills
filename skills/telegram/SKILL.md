---
name: telegram
description: "Interact with Telegram Bot API: send messages/photos/files/videos/audio/stickers/polls to any chat/group/channel, manage groups (pin/unpin/ban/kick/promote/restrict/set title/description/photo), forward/copy/delete/edit messages, react to messages, create invite links, manage forum topics, inline keyboards, and more. Use for ANY Telegram operation."
allowed-tools:
  - Bash
---

# Telegram Bot API Skill

You have full access to the Telegram Bot API. Use CLI commands for common operations, and direct API calls for everything else.

## Getting the Bot Token

```bash
BOT_TOKEN=$(curl -s http://localhost:23001/api/settings | jq -r '.telegram.botToken')
```

## API Call Pattern

```bash
# JSON body (most methods)
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/<METHOD>" \
  -H "Content-Type: application/json" \
  -d '{ ... }'

# Multipart form (file uploads)
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/<METHOD>" \
  -F "chat_id=<CHAT_ID>" \
  -F "photo=@/path/to/file.jpg" \
  -F "caption=optional caption"
```

Full API docs: https://core.telegram.org/bots/api

---

## CLI Quick Commands

```bash
# Current chat (ALMA_CHAT_ID auto-set)
alma send photo /path/to/img.jpg "caption"
alma send file /path/to/doc.pdf "caption"
alma send audio /path/to/song.mp3
alma send video /path/to/vid.mp4

# Group operations
alma group list
alma group send <chatId> "text"
alma group send-photo <chatId> /path "caption"
alma group send-document <chatId> /path "caption"
alma group send-video <chatId> /path "caption"
alma group history <chatId> [limit]
alma group search <chatId> "query"
alma group context <chatId>
alma group pin <chatId> <msgId>
alma group unpin <chatId> [msgId]
alma group leave <chatId>
alma group patrol

# Direct message & message mgmt
alma dm <userId> "message"
alma msg delete <chatId> <msgId>
alma msg react <chatId> <msgId> <emoji>   # Add reaction to a message (e.g. ❤️ 😂 👍)
alma msg sticker <chatId> <sticker_file_id>  # Send a sticker (use file_id from received stickers)
alma msg sticker-search [set_name]          # List sticker sets (no args) or index a specific set
alma msg sticker-find <emoji>               # Find stickers by emoji across all indexed sets
```

---

## All Telegram Bot API Methods

### Sending Messages

| Method | Description |
|--------|-------------|
| `sendMessage` | Send text message. Params: `chat_id`, `text`, `parse_mode` (HTML/Markdown), `reply_parameters`, `reply_markup`, `message_thread_id`, `link_preview_options` |
| `sendPhoto` | Send photo. `chat_id`, `photo` (file_id/URL/upload), `caption`, `has_spoiler` |
| `sendDocument` | Send file. `chat_id`, `document`, `caption`, `thumbnail` |
| `sendVideo` | Send video. `chat_id`, `video`, `caption`, `duration`, `width`, `height`, `has_spoiler` |
| `sendAnimation` | Send GIF. `chat_id`, `animation`, `caption`, `has_spoiler` |
| `sendAudio` | Send audio. `chat_id`, `audio`, `caption`, `duration`, `performer`, `title` |
| `sendVoice` | Send voice message. `chat_id`, `voice`, `caption`, `duration` |
| `sendVideoNote` | Send round video. `chat_id`, `video_note`, `duration`, `length` |
| `sendSticker` | Send sticker. `chat_id`, `sticker` (file_id/URL/upload) |
| `sendLocation` | Send location. `chat_id`, `latitude`, `longitude`, `live_period` |
| `sendVenue` | Send venue. `chat_id`, `latitude`, `longitude`, `title`, `address` |
| `sendContact` | Send contact. `chat_id`, `phone_number`, `first_name`, `last_name` |
| `sendPoll` | Send poll. `chat_id`, `question`, `options` ([{text:...}]), `is_anonymous`, `type` (quiz/regular), `allows_multiple_answers` |
| `sendDice` | Send dice/slot. `chat_id`, `emoji` (🎲🎯🏀⚽🎳🎰) |
| `sendMediaGroup` | Send album (2-10 items). `chat_id`, `media` ([{type,media,caption}]) |
| `sendChatAction` | Show typing/uploading status. `chat_id`, `action` (typing/upload_photo/upload_video/upload_document/record_voice/record_video/etc) |
| `sendMessageDraft` | Stream partial message. `chat_id`, `text`, `business_connection_id` |

### Forwarding & Copying

| Method | Description |
|--------|-------------|
| `forwardMessage` | Forward message. `chat_id` (target), `from_chat_id`, `message_id` |
| `forwardMessages` | Forward multiple. `chat_id`, `from_chat_id`, `message_ids` ([array]) |
| `copyMessage` | Copy without "Forwarded" header. `chat_id`, `from_chat_id`, `message_id`, `caption` |
| `copyMessages` | Copy multiple. `chat_id`, `from_chat_id`, `message_ids` |

### Editing Messages

| Method | Description |
|--------|-------------|
| `editMessageText` | Edit text. `chat_id`, `message_id`, `text`, `parse_mode`, `reply_markup` |
| `editMessageCaption` | Edit caption. `chat_id`, `message_id`, `caption` |
| `editMessageMedia` | Edit media. `chat_id`, `message_id`, `media` |
| `editMessageReplyMarkup` | Edit inline keyboard. `chat_id`, `message_id`, `reply_markup` |
| `editMessageLiveLocation` | Update live location. `chat_id`, `message_id`, `latitude`, `longitude` |
| `stopMessageLiveLocation` | Stop live location. `chat_id`, `message_id` |
| `deleteMessage` | Delete message. `chat_id`, `message_id` |
| `deleteMessages` | Delete multiple. `chat_id`, `message_ids` ([array]) |

### Reactions

| Method | Description |
|--------|-------------|
| `setMessageReaction` | React to message. `chat_id`, `message_id`, `reaction` ([{"type":"emoji","emoji":"👍"}]), `is_big` |

### Chat Management

| Method | Description |
|--------|-------------|
| `getChat` | Get chat info. `chat_id` |
| `getChatMemberCount` | Member count. `chat_id` |
| `getChatMember` | Get one member's info. `chat_id`, `user_id` |
| `getChatAdministrators` | List admins. `chat_id` |
| `setChatTitle` | Set title. `chat_id`, `title` |
| `setChatDescription` | Set description. `chat_id`, `description` |
| `setChatPhoto` | Set photo. `chat_id`, `photo` (upload) |
| `deleteChatPhoto` | Delete photo. `chat_id` |
| `setChatPermissions` | Set default permissions. `chat_id`, `permissions` ({can_send_messages, can_send_photos, ...}) |
| `setChatStickerSet` | Set sticker set. `chat_id`, `sticker_set_name` |
| `deleteChatStickerSet` | Remove sticker set. `chat_id` |
| `leaveChat` | Leave chat. `chat_id` |
| `pinChatMessage` | Pin message. `chat_id`, `message_id`, `disable_notification` |
| `unpinChatMessage` | Unpin message. `chat_id`, `message_id` |
| `unpinAllChatMessages` | Unpin all. `chat_id` |

### Member Management

| Method | Description |
|--------|-------------|
| `banChatMember` | Ban user. `chat_id`, `user_id`, `until_date`, `revoke_messages` |
| `unbanChatMember` | Unban user. `chat_id`, `user_id`, `only_if_banned` |
| `restrictChatMember` | Restrict user. `chat_id`, `user_id`, `permissions`, `until_date` |
| `promoteChatMember` | Promote to admin. `chat_id`, `user_id`, `can_change_info`, `can_post_messages`, `can_edit_messages`, `can_delete_messages`, `can_invite_users`, `can_restrict_members`, `can_pin_messages`, `can_promote_members`, `can_manage_video_chats`, `can_manage_chat` |
| `setChatAdministratorCustomTitle` | Set admin title. `chat_id`, `user_id`, `custom_title` |
| `approveChatJoinRequest` | Approve join request. `chat_id`, `user_id` |
| `declineChatJoinRequest` | Decline join request. `chat_id`, `user_id` |

### Invite Links

| Method | Description |
|--------|-------------|
| `exportChatInviteLink` | Create new primary link. `chat_id` |
| `createChatInviteLink` | Create invite link. `chat_id`, `name`, `expire_date`, `member_limit`, `creates_join_request` |
| `editChatInviteLink` | Edit invite link. `chat_id`, `invite_link`, ... |
| `revokeChatInviteLink` | Revoke link. `chat_id`, `invite_link` |
| `createChatSubscriptionInviteLink` | Paid subscription link. `chat_id`, `subscription_period`, `subscription_price` |

### Forum Topics

| Method | Description |
|--------|-------------|
| `createForumTopic` | Create topic. `chat_id`, `name`, `icon_color`, `icon_custom_emoji_id` |
| `editForumTopic` | Edit topic. `chat_id`, `message_thread_id`, `name`, `icon_custom_emoji_id` |
| `closeForumTopic` | Close topic. `chat_id`, `message_thread_id` |
| `reopenForumTopic` | Reopen topic. `chat_id`, `message_thread_id` |
| `deleteForumTopic` | Delete topic. `chat_id`, `message_thread_id` |
| `unpinAllForumTopicMessages` | Unpin all in topic. `chat_id`, `message_thread_id` |
| `getForumTopicIconStickers` | Get available topic icons |
| `editGeneralForumTopic` | Edit General topic. `chat_id`, `name` |
| `closeGeneralForumTopic` | Close General. `chat_id` |
| `reopenGeneralForumTopic` | Reopen General. `chat_id` |
| `hideGeneralForumTopic` | Hide General. `chat_id` |
| `unhideGeneralForumTopic` | Unhide General. `chat_id` |

### Bot Profile

| Method | Description |
|--------|-------------|
| `getMe` | Get bot info |
| `setMyName` | Set bot name. `name`, `language_code` |
| `getMyName` | Get bot name |
| `setMyDescription` | Set bot description. `description`, `language_code` |
| `getMyDescription` | Get bot description |
| `setMyShortDescription` | Set short description. `short_description` |
| `getMyShortDescription` | Get short description |
| `setMyProfilePhoto` | Set bot profile photo. `photo` |
| `removeMyProfilePhoto` | Remove bot profile photo |
| `setMyCommands` | Set command list. `commands` ([{command,description}]), `scope`, `language_code` |
| `getMyCommands` | Get commands |
| `deleteMyCommands` | Delete commands |
| `setChatMenuButton` | Set menu button. `chat_id`, `menu_button` |
| `getChatMenuButton` | Get menu button |
| `setMyDefaultAdministratorRights` | Set default admin rights. `rights`, `for_channels` |
| `getMyDefaultAdministratorRights` | Get default admin rights |

### User Info

| Method | Description |
|--------|-------------|
| `getUserProfilePhotos` | Get profile photos. `user_id`, `offset`, `limit` |
| `getUserProfileAudios` | Get profile audios. `user_id` |

### Files

| Method | Description |
|--------|-------------|
| `getFile` | Get file download info. `file_id`. Download: `https://api.telegram.org/file/bot<TOKEN>/<file_path>` |

### Inline Keyboards & Callbacks

```json
// reply_markup for inline buttons
{
  "inline_keyboard": [
    [{"text": "Button 1", "callback_data": "btn1"}, {"text": "URL", "url": "https://example.com"}],
    [{"text": "Row 2", "callback_data": "btn2"}]
  ]
}
```

| Method | Description |
|--------|-------------|
| `answerCallbackQuery` | Answer callback from inline button. `callback_query_id`, `text`, `show_alert` |

### Stickers

| Method | Description |
|--------|-------------|
| `sendSticker` | Send sticker. `chat_id`, `sticker` |
| `getStickerSet` | Get sticker set. `name` |
| `getCustomEmojiStickers` | Get custom emoji. `custom_emoji_ids` |
| `createNewStickerSet` | Create sticker set. `user_id`, `name`, `title`, `stickers` |
| `addStickerToSet` | Add sticker. `user_id`, `name`, `sticker` |
| `setStickerPositionInSet` | Reorder. `sticker`, `position` |
| `deleteStickerFromSet` | Delete sticker. `sticker` |
| `setStickerSetTitle` | Set title. `name`, `title` |
| `setStickerSetThumbnail` | Set thumbnail. `name`, `user_id`, `thumbnail` |

### Payments

| Method | Description |
|--------|-------------|
| `sendInvoice` | Send invoice. `chat_id`, `title`, `description`, `payload`, `currency`, `prices` |
| `createInvoiceLink` | Create payment link |
| `answerShippingQuery` | Answer shipping. `shipping_query_id`, `ok`, `shipping_options` |
| `answerPreCheckoutQuery` | Answer checkout. `pre_checkout_query_id`, `ok` |
| `refundStarPayment` | Refund stars. `user_id`, `telegram_payment_charge_id` |

### Games

| Method | Description |
|--------|-------------|
| `sendGame` | Send game. `chat_id`, `game_short_name` |
| `setGameScore` | Set score. `user_id`, `score`, `chat_id`, `message_id` |
| `getGameHighScores` | Get scores. `user_id`, `chat_id`, `message_id` |

---

## Common Examples

### Post to a channel
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/sendMessage" \
  -H "Content-Type: application/json" \
  -d '{"chat_id": "@channel_username", "text": "Hello!"}'
```

### Send photo with inline keyboard
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/sendPhoto" \
  -H "Content-Type: application/json" \
  -d '{
    "chat_id": "'$ALMA_CHAT_ID'",
    "photo": "https://example.com/image.jpg",
    "caption": "Check this out!",
    "reply_markup": {"inline_keyboard": [[{"text":"👍 Like","callback_data":"like"},{"text":"👎","callback_data":"dislike"}]]}
  }'
```

### Forward message between chats
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/forwardMessage" \
  -H "Content-Type: application/json" \
  -d '{"chat_id": "<TARGET>", "from_chat_id": "<SOURCE>", "message_id": 123}'
```

### Create a poll
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/sendPoll" \
  -H "Content-Type: application/json" \
  -d '{"chat_id": "'$ALMA_CHAT_ID'", "question": "Best language?", "options": [{"text":"Rust"},{"text":"TypeScript"},{"text":"Python"}], "is_anonymous": false}'
```

### Reply to a specific message
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/sendMessage" \
  -H "Content-Type: application/json" \
  -d '{"chat_id": "'$ALMA_CHAT_ID'", "text": "Replying!", "reply_parameters": {"message_id": 5678}}'
```

### Promote user to admin
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/promoteChatMember" \
  -H "Content-Type: application/json" \
  -d '{"chat_id": "<GROUP>", "user_id": 12345, "can_pin_messages": true, "can_invite_users": true}'
```

### Upload and send a local file
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/sendDocument" \
  -F "chat_id=$ALMA_CHAT_ID" \
  -F "document=@/path/to/file.pdf" \
  -F "caption=Here's the document"
```

### Send album (media group)
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/sendMediaGroup" \
  -H "Content-Type: application/json" \
  -d '{"chat_id": "'$ALMA_CHAT_ID'", "media": [
    {"type":"photo","media":"https://example.com/1.jpg","caption":"First"},
    {"type":"photo","media":"https://example.com/2.jpg"}
  ]}'
```

### Create invite link
```bash
curl -s "https://api.telegram.org/bot${BOT_TOKEN}/createChatInviteLink" \
  -H "Content-Type: application/json" \
  -d '{"chat_id": "<GROUP>", "name": "My Link", "member_limit": 100}'
```

## Tips

- **Channel posts**: Use `@channel_username` or numeric chat_id (starts with `-100`)
- **Bot must be admin** to: post to channels, pin, delete others' messages, ban, manage topics
- **ALMA_CHAT_ID** is auto-set in Bash tool env — use it for current chat
- **File uploads**: Use `-F` multipart form, not `-d` JSON
- **parse_mode**: Use `"HTML"` for `<b>bold</b> <i>italic</i> <code>code</code> <a href="url">link</a>`
- **Group IDs** are negative; user IDs are positive
- **Message IDs** are visible as `[msg:xxx]` in chat
- For methods not listed here, check https://core.telegram.org/bots/api
