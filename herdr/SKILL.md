---
name: herdr
description: "Control herdr from inside it. Manage workspaces and tabs, split panes, run commands, read output, and wait for state changes — all via CLI commands that talk to the running herdr instance over a local unix socket. Use when running inside herdr (HERDR_ENV=1)."
---

# herdr — agent skill

you are running inside herdr, a terminal-native agent multiplexer. herdr gives you workspaces, tabs, and panes — each pane is a real terminal with its own shell, agent, server, or log stream — and you can control all of it from the cli.

this means you can:

- see what other panes and agents are doing
- create tabs for separate subcontexts inside one workspace
- split panes and run commands in them
- start servers, watch logs, and run tests in sibling panes
- wait for specific output before continuing
- wait for another agent to finish
- spawn more agent instances

the `herdr` binary is available in your PATH. its workspace, tab, pane, and wait commands talk to the running herdr instance over a local unix socket.

if you need the raw protocol or full api reference, read the [socket api docs](https://herdr.dev/docs/socket-api/).

## verified version

this skill documents herdr **v0.5.8**. commands were verified against the actual binary. do not assume newer features (like `herdr agent` subcommands) exist unless confirmed.

## concepts

**workspaces** are project contexts. each workspace has one or more tabs. unless manually renamed, a workspace's label follows the first tab's root pane — usually the repo name, otherwise the root pane's current folder name.

**tabs** are subcontexts inside a workspace. each tab has one or more panes.

**panes** are terminal splits inside a tab. each pane runs its own process — a shell, an agent, a server, anything.

**agent status** is detected automatically by herdr. the api exposes one public field for it:

- `agent_status` — `idle`, `working`, `blocked`, `done`, `unknown`

`done` means the agent finished, but you have not looked at that finished pane yet.

plain shells still exist as panes, but herdr's sidebar agent section intentionally focuses on detected agents rather than listing every shell.

**ids** — herdr v0.5.8 uses opaque workspace-scoped pane ids like `w651d456f8444f3-2`. tab ids look like `w651d456f8444f3:2`. workspace ids look like `w651d456f8444f3`. always re-read ids from `herdr pane list` before using them — do not hardcode or guess.

## discover yourself

see what panes exist and which one is focused:

```bash
herdr pane list
```

the focused pane is yours (look for `"focused": true`). other panes are your neighbors.

list workspaces:

```bash
herdr workspace list
```

## do not redirect pane output to files

**do not** use shell redirection like `> /tmp/output.txt` or `2>&1 | tee /tmp/log` when running commands in a herdr pane via `herdr pane run`. herdr's value is seeing the live terminal stream — the agent's own output, colours, formatting, and progress indicators. redirecting hides all of that and makes the pane appear frozen.

instead, observe output with:

- `herdr pane read <id> --source recent --lines N` — read what is already in the scrollback
- `herdr wait output <id> --match "pattern" --timeout N` — block until expected output appears
- `herdr pane read <id> --source visible --ansi` — get an ANSI snapshot for TUI feedback loops

## pane commands (the only way to interact with panes)

herdr v0.5.8 does **not** have `herdr agent` commands. all interaction goes through `herdr pane`:

- `herdr pane list` — list all panes, find your pane id and neighbors
- `herdr pane read <id> --source recent --lines N` — read what is on the screen
- `herdr pane send-text <id> "text"` — send text without pressing enter
- `herdr pane send-keys <id> Enter` — press a key (Enter, CtrlC, Escape, etc.)
- `herdr pane run <id> "command"` — send text + enter atomically
- `herdr pane split <id> --direction right|down [--no-focus] [--cwd PATH]` — split and create a new pane
- `herdr pane close <id>` — close a pane
- `herdr pane rename <id> "label"|--clear` — assign or clear a pane label
- `herdr pane get <id>` — get detailed info about a pane

## sending messages to a pane

`herdr pane run <id> "text"` sends text + Enter atomically and returns immediately. it does not wait for the receiving side to process the message.

```bash
# send a complete message — runs "npm test" in the pane's shell
herdr pane run w651d456f8444f3-3 "npm test"

# send text to an agent in the pane
herdr pane run w651d456f8444f3-3 "review the test coverage in src/api/"
```

`herdr pane send-text <id> "text"` sends raw text without pressing enter. use `herdr pane send-keys <id> Enter` to press enter separately:

```bash
# send text without enter
herdr pane send-text w651d456f8444f3-3 "hello"

# press enter
herdr pane send-keys w651d456f8444f3-3 Enter
```

**important**: `pane run` appends `\r` (carriage return) to the text, equivalent to pressing enter. the command runs in the pane's existing shell process — it does not start a new shell. if the pane is at an agent prompt (e.g. `>` or the pi input line), the command text will be sent to the agent, not executed as a shell command.

**there is no built-in delay or `--wait` flag on `pane run` or `pane send-text`.** both return immediately. to pace messages to an agent, use `herdr wait agent-status --status idle` between sends:

```bash
# send a message
herdr pane run w651d456f8444f3-3 "review the test coverage"

# wait until the agent is idle again
herdr wait agent-status w651d456f8444f3-3 --status idle --timeout 120000

# now send the next message
herdr pane run w651d456f8444f3-3 "also check src/utils/"
```

## spawn a new agent

split a new pane, then run the agent in it:

```bash
# split to the right without stealing focus
herdr pane split w651d456f8444f3-2 --direction right --no-focus

# parse the new pane id from the JSON response
# the response is: {"result":{"pane":{"pane_id":"w651d456f8444f3-6", ...}}, ...}

# run the agent in the new pane
herdr pane run w651d456f8444f3-6 "pi"

# wait for the agent prompt to appear (adjust match pattern for your agent)
herdr wait output w651d456f8444f3-6 --match "thinking" --timeout 30000

# give it a task
herdr pane run w651d456f8444f3-6 "review the test coverage in src/api/"
```

**do not pipe the agent's output** (e.g. `pi | tee /tmp/log`) when starting — the command text is sent to the pane's shell, not as a pipe target. let the agent's output flow naturally to the pane.

## wait patterns

`herdr wait output` watches for text in a pane's terminal output. useful for servers, builds, logs:

```bash
herdr wait output w651d456f8444f3-3 --match "ready on port 3000" --timeout 30000
herdr wait output w651d456f8444f3-3 --match "test result" --timeout 60000
herdr wait output w651d456f8444f3-3 --match "server.*ready" --regex --timeout 30000
```

`herdr wait agent-status` watches for the agent status field on a pane. useful for coding agents:

```bash
herdr wait agent-status w651d456f8444f3-3 --status done --timeout 60000
herdr wait agent-status w651d456f8444f3-3 --status idle --timeout 60000
herdr wait agent-status w651d456f8444f3-3 --status blocked --timeout 300000
```

**tip**: `wait output` matches against unwrapped recent terminal text. pane width and soft wrapping do not break matches. if you want to inspect the same transcript that the waiter matched, use `pane read --source recent-unwrapped`.

both commands exit code `1` on timeout.

## reading output

```bash
# current viewport (what is visible right now)
herdr pane read w651d456f8444f3-3 --source visible --lines 80

# recent scrollback (what was printed recently)
herdr pane read w651d456f8444f3-3 --source recent --lines 50

# without soft wrapping (best for logs)
herdr pane read w651d456f8444f3-3 --source recent-unwrapped --lines 120

# with ANSI colours preserved
herdr pane read w651d456f8444f3-3 --source visible --ansi
```

## workspace management

```bash
herdr workspace list
herdr workspace create --cwd /path/to/project
herdr workspace create --cwd /path/to/project --label "api server"
herdr workspace create --no-focus
herdr workspace focus w651d456f8444f3
herdr workspace rename w651d456f8444f3 "api server"
herdr workspace close w651d456f8444f3
```

## tab management

```bash
herdr tab list --workspace w651d456f8444f3
herdr tab create --workspace w651d456f8444f3
herdr tab create --workspace w651d456f8444f3 --label "logs"
herdr tab rename w651d456f8444f3:2 "logs"
herdr tab focus w651d456f8444f3:2
herdr tab close w651d456f8444f3:2
```

## close a pane

```bash
herdr pane close w651d456f8444f3-3
```

## integrations

install built-in agent integrations for more precise state detection:

```bash
herdr integration install pi
herdr integration install claude
herdr integration install codex
herdr integration install opencode
herdr integration status
```

uninstall:

```bash
herdr integration uninstall pi
```

## named sessions

```bash
herdr session list
herdr session attach work
herdr session stop work
```

select a session for CLI commands:

```bash
HERDR_SESSION=side-project herdr workspace list
```

## recipes

### run a server and wait until it is ready

```bash
# split a new pane
RESPONSE=$(herdr pane split w651d456f8444f3-2 --direction right --no-focus)
NEW_PANE=$(echo "$RESPONSE" | python3 -c 'import sys,json; print(json.load(sys.stdin)["result"]["pane"]["pane_id"])')

# start the server
herdr pane run "$NEW_PANE" "npm run dev"

# wait for it to be ready
herdr wait output "$NEW_PANE" --match "ready" --timeout 30000

# read the output
herdr pane read "$NEW_PANE" --source recent --lines 20
```

### run tests in a separate pane and inspect the result

```bash
herdr pane split w651d456f8444f3-2 --direction down --no-focus
herdr pane run w651d456f8444f3-3 "cargo test"
herdr wait output w651d456f8444f3-3 --match "test result" --timeout 60000
herdr pane read w651d456f8444f3-3 --source recent --lines 30
```

### check what another agent is working on

```bash
herdr pane list
herdr pane read w651d456f8444f3-3 --source recent --lines 80
```

### watch another pane robustly

```bash
# inspect what is already there
herdr pane read w651d456f8444f3-3 --source recent --lines 40

# wait for specific output
herdr wait output w651d456f8444f3-3 --match "ready" --timeout 30000

# read unwrapped transcript that the waiter matched
herdr pane read w651d456f8444f3-3 --source recent-unwrapped --lines 40
```

### spawn a new agent and give it a task

```bash
# split pane and start the agent
herdr pane split w651d456f8444f3-2 --direction right --no-focus
herdr pane run w651d456f8444f3-3 "pi"

# wait for the agent prompt
herdr wait output w651d456f8444f3-3 --match "thinking" --timeout 30000

# give it a task
herdr pane run w651d456f8444f3-3 "review the test coverage in src/api/"

# wait for it to finish
herdr wait agent-status w651d456f8444f3-3 --status idle --timeout 120000

# read the result
herdr pane read w651d456f8444f3-3 --source recent --lines 100

# close when done
herdr pane close w651d456f8444f3-3
```

### coordinate with another agent

```bash
# wait for the agent to finish
herdr wait agent-status w651d456f8444f3-3 --status done --timeout 120000

# read its output
herdr pane read w651d456f8444f3-3 --source recent --lines 100
```

### multi-turn conversation with an agent

```bash
# send first message
herdr pane run w651d456f8444f3-3 "review the test coverage"

# wait until idle
herdr wait agent-status w651d456f8444f3-3 --status idle --timeout 120000

# read response
herdr pane read w651d456f8444f3-3 --source recent --lines 80

# send follow-up
herdr pane run w651d456f8444f3-3 "also check src/utils/"

# wait again
herdr wait agent-status w651d456f8444f3-3 --status idle --timeout 120000

# read again
herdr pane read w651d456f8444f3-3 --source recent --lines 80
```

## notes

- `herdr pane list` prints json. parse `result.panes[].pane_id` for pane ids and `result.panes[].focused` to find your pane.
- `herdr pane split` prints json. the new pane id is at `result.pane.pane_id`.
- `herdr workspace create` prints json with `result.workspace`, `result.tab`, and `result.root_pane`.
- `herdr tab create` prints json with `result.tab` and `result.root_pane`.
- `herdr pane read` prints text, not json.
- `herdr pane send-text`, `herdr pane send-keys`, and `herdr pane run` print nothing on success.
- `herdr pane read --ansi` or `herdr pane read --format ansi` returns a rendered ANSI snapshot.
- `herdr pane read --source recent-unwrapped` joins soft wraps — use it to inspect the same transcript that `wait output` matches against.
- `herdr pane run <id> "cmd"` sends text + `\r` (enter). the command runs in the pane's **existing** shell process — it does not start a new shell.
- `herdr pane send-text <id> "text"` sends text without enter. use `herdr pane send-keys <id> Enter` to press enter.
- `herdr pane send-keys` accepts special keys: Enter, Up, Down, Left, Right, Backspace, Tab, etc.
- `--no-focus` on `pane split`, `tab create`, and `workspace create` keeps your current terminal context focused.
- use `herdr pane read` for current output that already exists. use `herdr wait output` for future output you expect next.
- if you are running inside herdr, the `HERDR_ENV` environment variable is set to `1`.
- `HERDR_SESSION=<name>` selects a named session for CLI commands. `HERDR_SOCKET_PATH` is a low-level override.
- there is **no** built-in delay or `--wait` flag on `pane run` or `pane send-text`. to pace messages to an agent, use `herdr wait agent-status --status idle` between sends.
- pane ids in herdr v0.5.8 are opaque workspace-scoped strings like `w651d456f8444f3-2`. always re-read ids from `herdr pane list` — do not hardcode or guess.
- **do not** redirect pane output with `> file` or `| tee`. use `herdr pane read` and `herdr wait output` instead.
- `herdr wait output` exit code is `1` on timeout.
- `herdr agent` subcommands do **not** exist in v0.5.8. use `herdr pane` commands for all pane interaction.