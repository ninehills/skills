---
name: gogcli
description: "Use when the user asks to send or read Gmail messages, create or list Google Calendar events, upload or search Google Drive files, manage Google Contacts, edit Google Sheets, or export Google Docs. CLI wrapper for Google Workspace via OAuth."
homepage: https://gogcli.sh
metadata: {"clawdbot":{"emoji":"🎮","requires":{"bins":["gog"]},"install":[{"id":"brew","kind":"brew","formula":"steipete/tap/gogcli","bins":["gog"],"label":"Install gog (brew)"}]}}
---

# gog

Use `gog` for Gmail/Calendar/Drive/Contacts/Sheets/Docs. Requires OAuth setup.

Setup (once)
1. `gog auth credentials /path/to/client_secret.json`
2. `gog auth add you@gmail.com --services gmail,calendar,drive,contacts,sheets,docs`
3. Verify: `gog auth list` — confirm the account appears with all services
4. Test: `gog gmail search "in:inbox" --max 1` — confirm authentication works

Common commands
- Gmail search: `gog gmail search 'newer_than:7d' --max 10`
- Gmail send: `gog gmail send --to a@b.com --subject "Hi" --body "Hello"`
- Calendar: `gog calendar events <calendarId> --from <iso> --to <iso>`
- Drive search: `gog drive search "query" --max 10`
- Contacts: `gog contacts list --max 20`
- Sheets get: `gog sheets get <sheetId> "Tab!A1:D10" --json`
- Sheets update: `gog sheets update <sheetId> "Tab!A1:B2" --values-json '[["A","B"],["1","2"]]' --input USER_ENTERED`
- Sheets append: `gog sheets append <sheetId> "Tab!A:C" --values-json '[["x","y","z"]]' --insert INSERT_ROWS`
- Sheets clear: `gog sheets clear <sheetId> "Tab!A2:Z"`
- Sheets metadata: `gog sheets metadata <sheetId> --json`
- Docs export: `gog docs export <docId> --format txt --out /tmp/doc.txt`
- Docs cat: `gog docs cat <docId>`

Notes
- Set `GOG_ACCOUNT=you@gmail.com` to avoid repeating `--account`.
- For scripting, prefer `--json` plus `--no-input`.
- Sheets values can be passed via `--values-json` (recommended) or as inline rows.
- Docs supports export/cat/copy. In-place edits require a Docs API client (not in gog).

Send/create workflow (Gmail, Calendar):
1. Show the user a preview of the message or event details
2. Ask for explicit confirmation before executing
3. Send/create and report the result

Gmail:

- Search Inbox: `gog gmail messages search "in:inbox" --max 200 --json`
- Archive: `gog gmail batch modify <id> --remove=INBOX --json --no-input`
