---
name: coding-agent
description: Delegate complex coding tasks to Claude Code (your coding subagent). Use for multi-file edits, refactoring, bug fixes, new features, and any task that benefits from a dedicated coding agent with full filesystem access.
allowed-tools:
  - Bash
---

# Coding Agent Skill

Delegate coding tasks to Claude Code — your local coding subagent ("junior assistant").

## Quick Check

```bash
# Check if Claude Code is available and authenticated
alma coding-agent status
```

## Delegate a Task

```bash
# Run a coding task in a specific directory
alma coding-agent run --dir /path/to/project "fix the bug in server.ts where..."

# Run with dangerously-skip-permissions for fully autonomous work
alma coding-agent run --dir /path/to/project --yolo "refactor the auth module to use JWT"
```

## When to Use

- **Multi-file refactoring** — Claude Code can edit across many files at once
- **Complex bug fixes** — when the fix requires understanding a large codebase
- **New features** — scaffolding, implementation, and testing in one go
- **Code review & cleanup** — let Claude Code analyze and improve code quality
- **Any task you'd normally do in a terminal** — it has full shell + file access

## When NOT to Use

- Simple one-liner fixes you can do with Bash directly
- Non-coding tasks (use other skills instead)
- When Claude Code is not available (`alma coding-agent status` returns unavailable)

## Tips

- Be specific in your task description — the more context, the better
- Specify the working directory with `--dir` so Claude Code knows where to work
- Use `--yolo` for trusted tasks that don't need permission prompts
- Results are returned as text — you'll see what Claude Code did and can relay to the user
