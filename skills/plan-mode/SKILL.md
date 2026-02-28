---
name: plan-mode
description: Switch into structured planning mode before outlining multi-step solutions, and exit when done.
allowed-tools:
  - Bash
---

# Plan Mode Skill

Enter and exit structured planning mode.

## Enter Plan Mode

```bash
curl -s -X POST http://localhost:23001/api/plan-mode/enter
```

Response: `{"active": true, "since": "2026-01-01T00:00:00.000Z", "message": "Plan mode activated."}`

## Exit Plan Mode

```bash
curl -s -X POST http://localhost:23001/api/plan-mode/exit
```

Response: `{"active": false, "since": null, "message": "Plan mode exited."}`

## Check Status

```bash
curl -s http://localhost:23001/api/plan-mode
```

## When to Use

- Before outlining a complex multi-step solution
- When the user asks you to "plan" or "think through" an approach
- Exit after the plan is finalized and you're ready to execute
