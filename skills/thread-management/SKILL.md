---
name: thread-management
description: Manage chat threads — create, list, switch, delete, and search conversations. Use when users want to organize their chats.
allowed-tools:
  - Bash
---

# Thread Management Skill

Manage Alma chat threads via the `alma` CLI.

## Commands

```bash
# List recent threads (default 20)
alma threads [limit]

# Show thread details
alma thread info <thread-id>

# Create a new thread
alma thread create <title> [--model providerId:modelName]

# Delete a thread
alma thread delete <thread-id>

# Read thread messages
alma thread messages <thread-id> [--limit 20]

# Switch current chat to a different thread
alma thread switch <thread-id>

# Search across threads (via API)
curl -s "http://localhost:23001/api/threads/search?q=QUERY&limit=10"
```

## Tips

- Use `alma threads` for a quick overview
- Use `alma thread switch <id>` to switch the current Telegram/Discord chat to a different thread (the ALMA_THREAD_ID env is automatically set)
- When creating threads for the user, give them descriptive titles
- Always confirm before deleting threads
