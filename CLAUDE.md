# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a curated collection of 8 specialized AI skills for Claude and other AI agents. Each skill is a standalone package with its own `SKILL.md` file containing YAML frontmatter (metadata) and markdown documentation.

**Architecture:** All skills follow a consistent structure:
- `SKILL.md` - Core skill definition with frontmatter (`name`, `description`, `version`, `triggers`, `user-invocable`, `allowed-tools`, `hooks`)
- Supporting files vary by skill (scripts, data files, templates)
- Skills are symlinked to `~/.claude/skills` and `~/.config/alma/skills` for use
