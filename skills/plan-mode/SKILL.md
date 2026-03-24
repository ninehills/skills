---
name: plan-mode
description: "Use when the user asks to plan, think through, break down, outline, or create a roadmap for a complex task. Activates structured planning mode via a local API so multi-step solutions are outlined before execution. Enter before planning, exit when ready to execute."
allowed-tools: "Bash"
---

# Plan Mode Skill

## Workflow

1. **Enter plan mode**: Call the enter endpoint before outlining any multi-step solution
2. **Create the plan**: Outline phases, dependencies, and milestones while plan mode is active
3. **Verify status**: Confirm plan mode is active if unsure
4. **Exit plan mode**: Call the exit endpoint once the plan is finalized and ready for execution

## API Endpoints

### Enter Plan Mode

```bash
curl -s -X POST http://localhost:23001/api/plan-mode/enter
```

Response: `{"active": true, "since": "2026-01-01T00:00:00.000Z", "message": "Plan mode activated."}`

### Exit Plan Mode

```bash
curl -s -X POST http://localhost:23001/api/plan-mode/exit
```

Response: `{"active": false, "since": null, "message": "Plan mode exited."}`

### Check Status

```bash
curl -s http://localhost:23001/api/plan-mode
```

## When to Use

- User says "plan", "think through", "break down", "outline", or "step by step"
- Before tackling a complex multi-step solution
- Exit after the plan is finalized and you are ready to execute
