#!/usr/bin/env bash
set -euo pipefail

CURRENT_DIR="$PWD"
ALMA_SKILLS="$HOME/.config/alma/skills"
CLAUDE_SKILLS="$HOME/.claude/skills"
MOLTBOT_JSON="$HOME/.openclaw/openclaw.json"

# 1. Create symlinks if not exist
if [[ -L "$ALMA_SKILLS" ]] && [[ -L "$CLAUDE_SKILLS" ]]; then
    echo "Symlinks already exist, skipping..."
else
    mkdir -p "$(dirname "$ALMA_SKILLS")" "$(dirname "$CLAUDE_SKILLS")"
    ln -sf "$CURRENT_DIR/skills" "$ALMA_SKILLS"
    ln -sf "$CURRENT_DIR/skills" "$CLAUDE_SKILLS"
    echo "Created symlinks for ~/.config/alma/skills and ~/.claude/skills"
fi

# 2. Update ~/.moltbot/moltbot.json using jq
if command -v jq &>/dev/null && [[ -f "$MOLTBOT_JSON" ]]; then
    # Inject current directory into skills.load.extraDirs
    jq --arg dir "$CURRENT_DIR" '
        if .skills.load.extraDirs then
            .skills.load.extraDirs |= (. + [$dir] | unique)
        else
            .skills.load.extraDirs = [$dir]
        end |
        if .skills.load.watch == null then
            .skills.load.watch = true
        end |
        if .skills.load.watchDebounceMs == null then
            .skills.load.watchDebounceMs = 250
        end
    ' "$MOLTBOT_JSON" > "$MOLTBOT_JSON.tmp" && mv "$MOLTBOT_JSON.tmp" "$MOLTBOT_JSON"

    echo "Updated $MOLTBOT_JSON"
else
    echo "jq not found or $MOLTBOT_JSON does not exist, skipping moltbot.json update"
fi
