---
name: cmux-terminal-multiplexer
description: AI-native terminal multiplexer with programmable socket API, full Playwright-equivalent browser automation, and agent team coordination — built for Claude Code and autonomous agent workflows
triggers:
  - "use cmux to open a split"
  - "cmux browser automation"
  - "open a terminal split with cmux"
  - "control browser with cmux"
  - "cmux agent workspace coordination"
  - "set up cmux splits for parallel work"
  - "cmux send command to terminal"
  - "automate browser with cmux"
---

# cmux — AI-Native Terminal Multiplexer

> Skill by [ara.so](https://ara.so) — Daily 2026 Skills collection

cmux is a terminal multiplexer with a programmable socket API designed for AI coding agents. It provides full Playwright-equivalent browser automation, real-time terminal split management, sidebar status reporting, and agent team coordination — all via a simple CLI.

---

## What cmux Does

- **Terminal splits** — create side-by-side or stacked panes, send commands, capture output
- **Browser automation** — full headless Chromium with snapshot-based element refs (no CSS selectors)
- **Status sidebar** — live progress bars, log messages, and icon badges visible to the user
- **Notifications** — native OS notifications from agent workflows
- **Agent teams** — coordinate parallel subagents, each with their own visible split

---

## Orient Yourself

```bash
cmux identify --json          # current window/workspace/pane/surface context
cmux list-panes               # all panes in current workspace
cmux list-pane-surfaces --pane pane:1  # surfaces within a pane
cmux list-workspaces          # all workspaces (tabs) in current window
```

Environment variables set automatically:
- `$CMUX_SURFACE_ID` — your current surface ref
- `$CMUX_WORKSPACE_ID` — your current workspace ref

Handles use short refs: `surface:N`, `pane:N`, `workspace:N`, `window:N`.

---

## Terminal Splits

### Create splits

```bash
cmux --json new-split right   # side-by-side (preferred for parallel work)
cmux --json new-split down    # stacked (good for logs)
```

Always capture the returned `surface_ref`:

```bash
WORKER=$(cmux --json new-split right | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
```

### Send commands and read output

```bash
cmux send-surface --surface surface:22 "npm run build\n"
cmux capture-pane --surface surface:22              # current screen
cmux capture-pane --surface surface:22 --scrollback  # with full history

cmux send-key-surface --surface surface:22 ctrl-c  # send key
cmux send-key-surface --surface surface:22 enter
```

**Golden rule: never steal focus.** Always use `--surface` targeting.

### Worker split pattern

```bash
WORKER=$(cmux --json new-split right | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
cmux send-surface --surface "$WORKER" "make test 2>&1; echo EXIT_CODE=\$?\n"
sleep 3
cmux capture-pane --surface "$WORKER"
cmux close-surface --surface "$WORKER"   # clean up when done
```

### Pane management

```bash
cmux focus-pane --pane pane:2
cmux close-surface --surface surface:22
cmux swap-pane --pane pane:1 --target-pane pane:2
cmux move-surface --surface surface:7 --pane pane:2 --focus true
cmux reorder-surface --surface surface:7 --before surface:3
```

---

## Browser Automation

cmux embeds a full headless Chromium engine with a Playwright-style API. No external Chrome required. Every command targets a browser surface by ref.

### Workflow pattern

```
navigate → wait for load → snapshot --interactive → act with refs → re-snapshot
```

### Open and navigate

```bash
cmux --json browser open https://example.com        # opens browser split, returns surface ref
cmux browser surface:23 goto https://other.com
cmux browser surface:23 back
cmux browser surface:23 forward
cmux browser surface:23 reload
cmux browser surface:23 get url
cmux browser surface:23 get title
```

Capture the surface ref:

```bash
BROWSER=$(cmux --json browser open https://docs.example.com | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
```

### Snapshot and element refs

Instead of CSS selectors, snapshot to get stable element refs (`e1`, `e2`, ...):

```bash
cmux browser surface:23 snapshot --interactive              # full interactive snapshot
cmux browser surface:23 snapshot --interactive --compact     # compact output
cmux browser surface:23 snapshot --selector "form#login" --interactive  # scoped
```

Refs are invalidated after DOM mutations — always re-snapshot after navigation or clicks. Use `--snapshot-after` to auto-get a fresh snapshot:

```bash
cmux --json browser surface:23 click e1 --snapshot-after
```

### Interact with elements

```bash
# Click and hover
cmux browser surface:23 click e1
cmux browser surface:23 dblclick e2
cmux browser surface:23 hover e3
cmux browser surface:23 focus e4

# Text input
cmux browser surface:23 fill e5 "hello@example.com"   # clear + type
cmux browser surface:23 fill e5 ""                      # clear input
cmux browser surface:23 type e6 "search query"          # type without clearing

# Keys
cmux browser surface:23 press Enter
cmux browser surface:23 press Tab
cmux browser surface:23 keydown Shift

# Forms
cmux browser surface:23 check e7          # checkbox
cmux browser surface:23 uncheck e7
cmux browser surface:23 select e8 "option-value"

# Scroll
cmux browser surface:23 scroll --dy 500
cmux browser surface:23 scroll --selector ".container" --dy 300
cmux browser surface:23 scroll-into-view e9
```

### Wait for state

```bash
cmux browser surface:23 wait --load-state complete --timeout-ms 15000
cmux browser surface:23 wait --selector "#ready" --timeout-ms 10000
cmux browser surface:23 wait --text "Success" --timeout-ms 10000
cmux browser surface:23 wait --url-contains "/dashboard" --timeout-ms 10000
cmux browser surface:23 wait --function "document.readyState === 'complete'" --timeout-ms 10000
```

### Read page content

```bash
cmux browser surface:23 get text body        # visible text
cmux browser surface:23 get html body        # raw HTML
cmux browser surface:23 get value "#email"   # input value
cmux browser surface:23 get attr "#link" --attr href
cmux browser surface:23 get count ".items"   # element count
cmux browser surface:23 get box "#button"    # bounding box
cmux browser surface:23 get styles "#el" --property color

# State checks
cmux browser surface:23 is visible "#modal"
cmux browser surface:23 is enabled "#submit"
cmux browser surface:23 is checked "#agree"
```

### Locators (Playwright-style)

```bash
cmux browser surface:23 find role button
cmux browser surface:23 find text "Sign In"
cmux browser surface:23 find label "Email"
cmux browser surface:23 find placeholder "Enter email"
cmux browser surface:23 find testid "submit-btn"
cmux browser surface:23 find first ".item"
cmux browser surface:23 find last ".item"
cmux browser surface:23 find nth ".item" 3
```

### JavaScript evaluation

```bash
cmux browser surface:23 eval "document.title"
cmux browser surface:23 eval "document.querySelectorAll('.item').length"
cmux browser surface:23 eval "window.scrollTo(0, document.body.scrollHeight)"
```

### Frames and dialogs

```bash
cmux browser surface:23 frame "#iframe-selector"   # switch to iframe
cmux browser surface:23 frame main                  # back to main frame
cmux browser surface:23 dialog accept
cmux browser surface:23 dialog dismiss
cmux browser surface:23 dialog accept "prompt text"
```

### Cookies, storage, and state

```bash
# Cookies
cmux browser surface:23 cookies get
cmux browser surface:23 cookies set session_token "abc123"
cmux browser surface:23 cookies clear

# Local/session storage
cmux browser surface:23 storage local get
cmux browser surface:23 storage local set myKey "myValue"
cmux browser surface:23 storage session clear

# Save/restore full browser state (cookies + storage + tabs)
cmux browser surface:23 state save ./auth-state.json
cmux browser surface:23 state load ./auth-state.json
```

### Authentication flow

```bash
BROWSER=$(cmux --json browser open https://app.example.com/login | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
cmux browser $BROWSER wait --load-state complete --timeout-ms 15000
cmux browser $BROWSER snapshot --interactive
cmux browser $BROWSER fill e1 "user@example.com"
cmux browser $BROWSER fill e2 "my-password"
cmux browser $BROWSER click e3
cmux browser $BROWSER wait --url-contains "/dashboard" --timeout-ms 20000

# Save auth for reuse
cmux browser $BROWSER state save ./auth-state.json

# Reuse in a new surface
BROWSER2=$(cmux --json browser open https://app.example.com | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
cmux browser $BROWSER2 state load ./auth-state.json
cmux browser $BROWSER2 goto https://app.example.com/dashboard
```

### Diagnostics

```bash
cmux browser surface:23 console list     # JS console output
cmux browser surface:23 console clear
cmux browser surface:23 errors list      # JS errors
cmux browser surface:23 errors clear
cmux browser surface:23 highlight "#el"  # visual highlight
cmux browser surface:23 screenshot       # capture screenshot
```

### Script and style injection

```bash
cmux browser surface:23 addscript "console.log('injected')"
cmux browser surface:23 addstyle "body { background: red; }"
cmux browser surface:23 addinitscript "window.__injected = true"  # runs on every nav
```

---

## Sidebar Status and Progress

Show live status to the user without interrupting their flow:

```bash
cmux set-status agent "working" --icon hammer --color "#ff9500"
cmux set-status agent "done" --icon checkmark --color "#34c759"
cmux clear-status agent

cmux set-progress 0.3 --label "Running tests..."
cmux set-progress 1.0 --label "Complete"
cmux clear-progress

cmux log "Starting build"
cmux log --level success "All tests passed"
cmux log --level error --source build "Compilation failed"
```

---

## Notifications

```bash
cmux notify --title "Task Complete" --body "All tests passing"
cmux notify --title "Need Input" --subtitle "Permission" --body "Approve deployment?"
```

---

## Agent Teams with cmux

Use cmux splits to give each agent teammate a visible workspace. Coordinate via `SendMessage` and task lists — never via reading each other's terminal output.

### The pattern

1. Create splits for each teammate
2. Spawn teammates via Agent tool — pass each their cmux surface ref
3. Teammates run commands in their split via `cmux send-surface`
4. Teammates report status via `cmux set-status` and `cmux log`
5. User sees all work side-by-side

### Example: 3-agent team

```bash
# Create visible splits for each teammate
SPLIT_1=$(cmux --json new-split right | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
SPLIT_2=$(cmux --json new-split down | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
SPLIT_3=$(cmux --json new-split down | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
```

Then in each teammate's prompt:
```
You have a cmux terminal split at surface:42.
Run commands:  cmux send-surface --surface surface:42 "command\n"
Read output:   cmux capture-pane --surface surface:42
Set status:    cmux set-status myagent "working" --icon hammer
Log progress:  cmux log "message"
Never steal focus — always use --surface targeting.
```

### Mixed layout: terminals + browsers

```bash
BUILD=$(cmux --json new-split right | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
DOCS=$(cmux --json browser open https://docs.example.com | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
TEST=$(cmux --json new-split down | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
```

### Key rules

- **Never spawn `claude -p` in splits** — use the Agent tool with `team_name` instead
- **Create splits before spawning teammates** — pass refs in their prompts
- **One split per teammate** — each owns their visible workspace
- **Coordinate via SendMessage**, not by reading each other's terminal output
- **Clean up**: `cmux close-surface --surface <ref>` when done

---

## Quick Reference

| Task | Command |
|------|---------|
| Where am I? | `cmux identify --json` |
| Split right | `cmux --json new-split right` |
| Split down | `cmux --json new-split down` |
| Send command | `cmux send-surface --surface <ref> "cmd\n"` |
| Read output | `cmux capture-pane --surface <ref>` |
| Open browser | `cmux --json browser open <url>` |
| Page snapshot | `cmux browser <ref> snapshot --interactive` |
| Click element | `cmux browser <ref> click e1` |
| Fill input | `cmux browser <ref> fill e1 "text"` |
| Wait for load | `cmux browser <ref> wait --load-state complete --timeout-ms 15000` |
| Read page text | `cmux browser <ref> get text body` |
| Evaluate JS | `cmux browser <ref> eval "expression"` |
| Find by role | `cmux browser <ref> find role button` |
| Save auth | `cmux browser <ref> state save ./auth.json` |
| Load auth | `cmux browser <ref> state load ./auth.json` |
| Set status | `cmux set-status <key> "text" --icon <name>` |
| Progress bar | `cmux set-progress 0.5 --label "Working..."` |
| Log message | `cmux log "message"` |
| Notify | `cmux notify --title "T" --body "B"` |
| Close split | `cmux close-surface --surface <ref>` |
| Screenshot | `cmux browser <ref> screenshot` |

---

## Common Patterns

### Run build in background split, tail logs

```bash
LOG=$(cmux --json new-split down | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
cmux send-surface --surface "$LOG" "cargo build --release 2>&1 | tee /tmp/build.log\n"
# ... do other work ...
cmux capture-pane --surface "$LOG" --scrollback | tail -20
```

### QA test flow

```bash
BROWSER=$(cmux --json browser open https://myapp.vercel.app | python3 -c "import sys,json; print(json.load(sys.stdin)['surface_ref'])")
cmux browser $BROWSER wait --load-state complete --timeout-ms 15000
cmux browser $BROWSER snapshot --interactive
# Interact using e1, e2, e3 refs...
cmux browser $BROWSER screenshot
cmux browser $BROWSER errors list
cmux close-surface --surface $BROWSER
```

### Status-driven long task

```bash
cmux set-status task "starting" --icon clock --color "#ff9500"
cmux set-progress 0.0 --label "Initializing..."

# ... step 1 ...
cmux set-progress 0.33 --label "Building..."

# ... step 2 ...
cmux set-progress 0.66 --label "Testing..."

# ... step 3 ...
cmux set-progress 1.0 --label "Done"
cmux set-status task "complete" --icon checkmark --color "#34c759"
cmux clear-progress
cmux notify --title "Task complete" --body "All steps passed"
```
