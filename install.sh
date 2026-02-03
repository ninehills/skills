#!/usr/bin/env bash
set -euo pipefail

CURRENT_DIR="$PWD"
ALMA_SKILLS="$HOME/.config/alma/skills"
CLAUDE_SKILLS="$HOME/.claude/skills"
CODEX_SKILLS="$HOME/.codex/skills"
GEMINI_SKILLS="$HOME/.gemini/skills"
MOLTBOT_JSON="$HOME/.openclaw/openclaw.json"

# 1. Create symlinks if not exist
for SKILLS_DIR in "$ALMA_SKILLS" "$CLAUDE_SKILLS" "$CODEX_SKILLS" "$GEMINI_SKILLS"; do
    if [[ -L "$SKILLS_DIR" ]]; then
        echo "Symlink $SKILLS_DIR already exists, skipping..."
    else
        mkdir -p "$(dirname "$SKILLS_DIR")"
        ln -sf "$CURRENT_DIR/skills" "$SKILLS_DIR"
        echo "Created symlink: $SKILLS_DIR"
    fi
done

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
