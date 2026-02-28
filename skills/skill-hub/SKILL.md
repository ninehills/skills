---
name: skill-hub
description: Search, install, and manage Alma skills from the skills.sh ecosystem. Use when the user needs a capability Alma doesn't have yet, or when you encounter a task you can't do with current skills.
allowed-tools:
  - Bash
  - Read
  - Write
---

# Skill Hub

Discover, install, and manage skills to extend Alma's capabilities.

## Search for Skills

```bash
# Search the skills.sh ecosystem
alma skill search <query>

# Examples:
alma skill search weather
alma skill search email
alma skill search calendar
```

## Install a Skill

```bash
# Install from skills.sh
alma skill install <user/repo>

# Example:
alma skill install openclaw/weather
```

## List Installed Skills

```bash
alma skill list
```

## Uninstall a Skill

```bash
alma skill uninstall <skill-name>
```

## Update Skills

```bash
alma skill update
```

## When to Use

- User asks you to do something you can't do with current tools/skills
- You encounter a task that would benefit from a specialized skill
- User explicitly asks to install or find new capabilities
- **Be proactive**: if a task fails because you lack a capability, search for a skill before giving up

## Self-Evolution Flow

1. User asks: "Check the weather for me"
2. You don't have a weather skill → search: `alma skill search weather`
3. Found one → install: `alma skill install openclaw/weather`
4. Now use the new skill to complete the task
5. Next time, the skill is already available

## Create a Custom Skill

```bash
mkdir -p ~/.config/alma/skills/my-skill
cat > ~/.config/alma/skills/my-skill/SKILL.md << 'EOF'
---
name: my-skill
description: What this skill does
allowed-tools:
  - Bash
  - Read
---

# My Skill

Instructions for the AI on how to use this skill...
EOF
```

## Tips

- Skills from skills.sh are installed to `~/.agents/skills/` and symlinked to `~/.config/alma/skills/`
- Project-local skills can live in `.alma/skills/` within any project directory
- After installing a skill, it's available in the next conversation turn
- **Don't give up on the first failure** — search for skills, install them, and try again
