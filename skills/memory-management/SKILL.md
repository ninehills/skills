---
name: memory-management
description: Search and manage Alma's memory and conversation history. Use when the user asks about past conversations, personal facts, preferences, or anything that requires recalling information ("do you know my...", "we talked about before...", "do you remember...", "help me find what we said about..."). Also used to store new memories and search through archived chat threads.
allowed-tools:
  - Bash
  - Read
  - Write
---

# Memory Management Skill

Alma has a built-in memory system with semantic search. Use the `alma` CLI to interact with it.

## Commands

```bash
# List all memories
alma memory list

# Semantic search
alma memory search <query>

# Add a memory
alma memory add <content>

# Delete a memory
alma memory delete <id>

# View memory stats
alma memory stats
```

## When to Use

- **User asks anything about the past** ("do you know what I like", "what did we discuss before", "what was that plan we talked about last time") → Search memories AND grep threads
- **User says "remember this"** → `alma memory add "..."`
- **User asks "do you remember..."** → `alma memory search "..."` + `alma memory grep "..."`
- **User says "forget about..."** → Search and delete matching memories
- **Time-sensitive info** (projects, deadlines) → Store with appropriate context

## Search Strategy

When the user asks about past information, **always try both layers**:
1. `alma memory search "<query>"` — semantic search for related concepts
2. `alma memory grep "<keyword>"` — keyword search in conversation history

If one layer returns nothing, try the other. They complement each other.

## Conversation History Search

Alma automatically archives all threads as markdown files. You can search through past conversations:

```bash
# Keyword search through all archived conversations
alma memory grep <keyword>

# Force re-archive all threads now
alma memory archive
```

Thread archives are stored in the workspace's `threads/` directory as markdown files with YAML frontmatter (threadId, title, createdAt, updatedAt, model, messageCount). Archives are auto-updated every 5 minutes.

### Two-Layer Memory

1. **Vector Memory** (`alma memory search`) — semantic search, finds conceptually related memories
2. **Conversation Archives** (`alma memory grep`) — keyword search, finds exact words/phrases in past conversations

Use vector search when the user asks vague questions ("what did we discuss about React?"). Use grep when looking for specific terms, names, or code snippets.

## Group Chat History

Alma persists all group chat messages to log files. Search and browse them:

```bash
# List all known groups
alma group list

# View recent history (default 50 messages)
alma group history <chatId> [limit]

# Search across all group chats
alma group search <keyword>
```

Log files are stored at `~/.config/alma/groups/<chatId>_<date>.log`. Use this when you need to recall what was discussed in a group chat.

## People Profiles (Per-Person Memory)

For group chats, Alma maintains structured per-person profiles — more reliable than vector search for remembering who is who.

```bash
# List all known people
alma people list

# View someone's profile
alma people show <name>

# Set/overwrite profile
alma people set <name> <content>

# Append to profile
alma people append <name> <fact>

# Delete profile
alma people delete <name>
```

**When to update profiles:**
- Someone shares personal info (job, hobbies, preferences)
- You learn their communication style or language preference
- They mention relationships with other people
- Any fact you'd want to remember next time you talk to them

Profiles are stored at `~/.config/alma/people/<name>.md` and automatically loaded into group chat context.

## Tips

- Always confirm what you stored/deleted with the user
- Use `alma memory search` to find related memories before adding duplicates
- Memories are automatically injected into conversations via semantic search — you don't need to manually recall them every time
- Use `alma memory grep` to search through conversation history when vector search doesn't find what you need
- **For per-person facts, prefer `alma people` over `alma memory`** — structured and won't get mixed up
