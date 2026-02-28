---
name: tasks
description: Global multi-step task tracking. Create, update, and monitor long-running tasks across threads. Tasks persist across restarts and are visible in all conversations.
allowed-tools:
  - Bash
---

# Tasks Skill

Track complex, multi-step tasks globally. Unlike todos (per-thread), tasks are shared across ALL threads and persist across restarts.

## When to Use

- Starting a complex task with 3+ steps
- User asks you to do something that will take multiple turns
- You need to track progress on ongoing work
- You want to remember what you were doing after a restart

## Commands

### List active tasks
```bash
alma tasks list          # active tasks (default)
alma tasks list all      # all tasks
alma tasks list done     # completed tasks
```

### Create a task
```bash
alma tasks add "Build People settings page"
```

### Update a task (set steps, progress, status)
```bash
# Set steps for a task
alma tasks update <id> --steps "Design API,Build backend,Build frontend,Test,Deploy"

# Mark progress (0-indexed step number)
alma tasks update <id> --step 2 --status in_progress

# Link to current thread
alma tasks update <id> --thread <threadId>

# Change title
alma tasks update <id> --title "New title"
```

### Mark done
```bash
alma tasks done <id>
```

### Show details
```bash
alma tasks show <id>
```

### Delete
```bash
alma tasks delete <id>
```

## Status Values

- `pending` — Created but not started
- `in_progress` — Currently being worked on
- `done` — Completed
- `blocked` — Waiting on something

## Workflow Example

```bash
# 1. Create task when starting complex work
alma tasks add "Add Discord chat logging"

# 2. Define steps
alma tasks update t1abc --steps "Add log method to bridge,Log user messages,Log bot replies,Test,Release"

# 3. Start working
alma tasks update t1abc --status in_progress --step 0

# 4. Progress through steps
alma tasks update t1abc --step 1
alma tasks update t1abc --step 2

# 5. Complete
alma tasks done t1abc
```

## Rules

- **ALWAYS create a task** when starting multi-step work (3+ steps)
- **Update progress** as you complete each step
- **Mark done** immediately when finished — don't leave stale tasks
- Keep task titles concise but descriptive
- Steps should be actionable and specific
- Clean up completed tasks periodically (or let them stay for history)

## Storage

Tasks are stored at `~/.config/alma/tasks.json` and injected into your context automatically. You can always see your active tasks.
