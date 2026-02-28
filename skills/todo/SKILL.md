---
name: todo
description: Manage a structured task list using a Markdown file in the workspace. Track progress on complex multi-step tasks. File-based — just Read and Write the todo file.
allowed-tools:
  - Read
  - Write
---

# Todo Skill

Manage tasks using a Markdown file at `.alma/todos-<THREAD_ID>.md` in the current workspace directory. Use the thread ID from the system prompt to create the filename (e.g., `.alma/todos-abc123.md`). This prevents conflicts when multiple threads share the same workspace.

## File Format

```markdown
# Todos

- [x] Fix authentication bug
- [ ] ~Add unit tests~ *(in progress)*
- [ ] Update documentation
- [ ] Write changelog
```

### Status markers
- `- [ ]` — pending
- `- [ ] ~Task name~ *(in progress)*` — currently working on
- `- [x]` — completed

## How to Use

1. **Read** the file to see current tasks: `Read .alma/todos.md`
2. **Write** the file to update tasks: `Write .alma/todos.md`
3. Create the `.alma/` directory if it doesn't exist

## Rules

- Only ONE task should be *in progress* at a time
- Mark tasks `[x]` IMMEDIATELY after finishing — don't batch
- Keep the full list when updating (this is a replace, not append)
- Add new tasks at the bottom

## When to Use

- Complex multi-step tasks (3+ steps)
- User provides multiple related tasks
- User explicitly asks for a task list
- Non-trivial work requiring progress tracking

## When NOT to Use

- Single, simple tasks
- Quick conversational responses
- Tasks completable in <3 steps

## Example Session

First, create the file:
```
Write .alma/todos.md

# Todos

- [ ] ~Refactor database layer~ *(in progress)*
- [ ] Add migration support
- [ ] Update API endpoints
- [ ] Write tests
```

After completing first task:
```
Write .alma/todos.md

# Todos

- [x] Refactor database layer
- [ ] ~Add migration support~ *(in progress)*
- [ ] Update API endpoints
- [ ] Write tests
```
