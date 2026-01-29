# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a curated collection of 8 specialized AI skills for Claude and other AI agents. Each skill is a standalone package with its own `SKILL.md` file containing YAML frontmatter (metadata) and markdown documentation.

**Architecture:** All skills follow a consistent structure:
- `SKILL.md` - Core skill definition with frontmatter (`name`, `description`, `version`, `triggers`, `user-invocable`, `allowed-tools`, `hooks`)
- Supporting files vary by skill (scripts, data files, templates)
- Skills are symlinked to `~/.claude/skills` and `~/.config/alma/skills` for use

## Adding a New Skill

1. **Clone and remove .git**:
   ```bash
   cd /tmp && git clone <repo-url> && rm -rf <skill-name>/.git
   ```

2. **Analyze before moving**:
   - Check if skill with same name already exists in `skills/skills/`
   - Verify `SKILL.md` exists and has valid frontmatter
   - Confirm directory structure matches expected format

3. **Move to skills directory** (after analysis):
   ```bash
   mv <skill-name> /Users/cynic/src/github.com/ninehills/skills/skills/
   ```

4. **Update README.md** with the new skill reference:
   ```markdown
   - <skill-name>: <repo-url>@<branch-or-commit>
   ```
