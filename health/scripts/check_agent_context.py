#!/usr/bin/env python3
"""Summarize the agent-instruction surface for a project.

Inventories AGENTS.md / CLAUDE.md / Codex / Copilot / Gemini instruction files,
parses Codex config.toml for project trust + plugin/feature state (with sensitive
values redacted), and flags drift between Claude and Codex surfaces.

Run as: python3 check_agent_context.py [ROOT] [summary|deep]
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path

SENSITIVE_RE = re.compile(r"(api[_-]?key|token|secret|password|credential)", re.IGNORECASE)
PROJECT_RE = re.compile(r'^\[projects\."(.+)"\]\s*$')
TABLE_RE = re.compile(r'^\[([A-Za-z0-9_.@"\-/]+)\]\s*$')
OPERATIONAL_RULE_RE = re.compile(
    r"(Git Safety|Public Issue Replies|Investigation Honesty|Verification|Response Style|Commit|Security)",
    re.IGNORECASE,
)


def rel(path: Path, root: Path) -> str:
    try:
        return path.resolve().relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


def read(path: Path, limit: int | None = None) -> str:
    try:
        data = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return ""
    return data[:limit] if limit else data


def yes(path: Path) -> str:
    return "yes" if path.exists() else "no"


def print_list(title: str, items: list[str], empty: str = "(none)", limit: int | None = None) -> None:
    print(f"{title}:")
    shown = items if limit is None else items[:limit]
    if not shown:
        print(f"  {empty}")
        return
    for item in shown:
        print(f"  {item}")
    if limit is not None and len(items) > limit:
        print(f"  ... {len(items) - limit} more")


def load_json(path: Path) -> tuple[object | None, str | None]:
    if not path.is_file():
        return None, None
    try:
        return json.loads(read(path)), None
    except json.JSONDecodeError as exc:
        return None, f"{path.name}: invalid JSON at line {exc.lineno}"


def redact_sensitive_entries(value: object, prefix: str = "") -> list[str]:
    entries: list[str] = []
    if isinstance(value, dict):
        for key, child in value.items():
            child_prefix = f"{prefix}.{key}" if prefix else str(key)
            if SENSITIVE_RE.search(str(key)):
                entries.append(f"{child_prefix}=[REDACTED]")
                continue
            entries.extend(redact_sensitive_entries(child, child_prefix))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            entries.extend(redact_sensitive_entries(child, f"{prefix}[{index}]"))
    return entries


def string_list(value: object) -> list[str]:
    if isinstance(value, list):
        return [str(item) if not SENSITIVE_RE.search(str(item)) else "[REDACTED]" for item in value]
    if isinstance(value, dict):
        return sorted(str(key) for key in value)
    if isinstance(value, str):
        return ["[REDACTED]" if SENSITIVE_RE.search(value) else value]
    return []


def skill_root_count(path: Path, include_root_md: bool) -> int:
    if not path.is_dir():
        return 0
    count = len(list(path.rglob("SKILL.md")))
    if include_root_md:
        count += len([p for p in path.glob("*.md") if p.name != "SKILL.md"])
    return count


def project_instruction_files(root: Path) -> list[Path]:
    files = [
        root / "AGENTS.md",
        root / "CLAUDE.md",
        root / ".github" / "copilot-instructions.md",
        root / "GEMINI.md",
    ]
    instructions_dir = root / ".github" / "instructions"
    if instructions_dir.is_dir():
        files.extend(sorted(instructions_dir.glob("*.md")))
    return [path for path in files if path.is_file()]


def claude_delegates_to_agents(path: Path) -> bool:
    text = read(path, 20_000)
    if not text:
        return False
    meaningful = [
        line.strip()
        for line in text.splitlines()
        if line.strip() and not line.strip().startswith("#")
    ]
    return any("AGENTS.md" in line for line in meaningful)


def has_operational_rules(path: Path) -> bool:
    text = read(path, 40_000)
    if not text:
        return False
    return len(set(m.group(1).lower() for m in OPERATIONAL_RULE_RE.finditer(text))) >= 2


def looks_identity_only(path: Path) -> bool:
    text = read(path, 40_000)
    if not text:
        return False
    return "nian-identity:start" in text and not has_operational_rules(path)


def parse_codex_config(
    path: Path,
) -> tuple[dict[str, str], list[str], list[str], list[str], list[str]]:
    projects: dict[str, str] = {}
    features: list[str] = []
    plugins: list[str] = []
    marketplaces: list[str] = []
    redacted: list[str] = []
    if not path.is_file():
        return projects, features, plugins, marketplaces, redacted

    section = ""
    for raw in read(path).splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        project_match = PROJECT_RE.match(line)
        if project_match:
            section = f'projects."{project_match.group(1)}"'
            projects.setdefault(project_match.group(1), "")
            continue
        table_match = TABLE_RE.match(line)
        if table_match:
            section = table_match.group(1)
            marketplace_match = re.match(r'marketplaces\.([A-Za-z0-9_.@-]+)$', section)
            plugin_match = re.match(r'plugins\."?([^"]+)"?$', section)
            if marketplace_match:
                marketplaces.append(marketplace_match.group(1))
            if plugin_match:
                plugins.append(plugin_match.group(1))
            continue

        if SENSITIVE_RE.search(line):
            key = line.split("=", 1)[0].strip() if "=" in line else "sensitive"
            redacted.append(f"{key}=[REDACTED]")
            continue

        if "=" not in line:
            continue
        key, value = [part.strip() for part in line.split("=", 1)]
        if section == "features" and value.split("#", 1)[0].strip().strip('"').lower() == "true":
            features.append(key)
        elif section.startswith('projects."') and key == "trust_level":
            project = section[len('projects."'): -1]
            projects[project] = value.strip('"')

    return (
        projects,
        sorted(set(features)),
        sorted(set(plugins)),
        sorted(set(marketplaces)),
        sorted(set(redacted)),
    )


def project_trust(projects: dict[str, str], root: Path) -> str:
    root_text = root.as_posix()
    if root_text in projects:
        return f"exact:{projects[root_text] or 'configured'}"
    candidates = []
    for project, level in projects.items():
        try:
            project_path = Path(project).expanduser().resolve()
        except OSError:
            continue
        if project_path == root:
            return f"exact:{level or 'configured'}"
        try:
            root.relative_to(project_path)
        except ValueError:
            continue
        candidates.append(
            (len(project_path.as_posix()), level or "configured", project_path.as_posix())
        )
    if candidates:
        _, level, project = sorted(candidates, reverse=True)[0]
        return f"inherited:{level} from {project}"
    return "missing"


def summarize_pi_surface(root: Path, home: Path) -> tuple[str, list[str]]:
    global_settings = home / ".pi" / "agent" / "settings.json"
    project_settings = root / ".pi" / "settings.json"
    settings_sources = [
        ("global_settings", global_settings),
        ("project_settings", project_settings),
    ]

    configured_skills: list[str] = []
    configured_packages: list[str] = []
    redacted_entries: list[str] = []
    findings: list[str] = []
    malformed = False

    for label, path in settings_sources:
        data, error = load_json(path)
        if error:
            malformed = True
            findings.append(error)
            continue
        if not isinstance(data, dict):
            continue
        configured_skills.extend(
            f"{label}.skills: {item}" for item in string_list(data.get("skills"))
        )
        configured_packages.extend(
            f"{label}.packages: {item}" for item in string_list(data.get("packages"))
        )
        redacted_entries.extend(
            f"{label}.{item}" for item in redact_sensitive_entries(data)
        )

    package_path = root / "package.json"
    package_pi_skills: list[str] = []
    data, error = load_json(package_path)
    if error:
        findings.append(error)
    elif isinstance(data, dict):
        pi_manifest = data.get("pi")
        if isinstance(pi_manifest, dict):
            package_pi_skills = string_list(pi_manifest.get("skills"))

    pi_skill_dirs = [
        ("global_pi_skill_roots", home / ".pi" / "agent" / "skills", True),
        ("project_pi_skill_roots", root / ".pi" / "skills", True),
        ("global_agents_skill_roots", home / ".agents" / "skills", False),
        ("project_agents_skill_roots", root / ".agents" / "skills", False),
    ]
    skill_counts = [
        f"{label}: {skill_root_count(path, include_root_md)}"
        for label, path, include_root_md in pi_skill_dirs
    ]

    has_pi_surface = (
        global_settings.is_file()
        or project_settings.is_file()
        or bool(package_pi_skills)
        or any(not line.endswith(": 0") for line in skill_counts)
        or bool(configured_skills)
        or bool(configured_packages)
    )
    if not has_pi_surface:
        findings.append("no Pi settings, package manifest, or skill directories found")

    status = "WARN" if malformed else "PASS"
    lines = [
        "=== PI SURFACE ===",
        f"pi_status: {status}",
        f"global_settings_json: {yes(global_settings)}",
        f"project_settings_json: {yes(project_settings)}",
        f"package_json: {yes(package_path)}",
    ]
    lines.extend(skill_counts)
    lines.append("package_pi_skills:")
    lines.extend(f"  {item}" for item in (package_pi_skills or ["(none)"]))
    lines.append("configured_skills:")
    lines.extend(f"  {item}" for item in (configured_skills or ["(none)"]))
    lines.append("configured_packages:")
    lines.extend(f"  {item}" for item in (configured_packages or ["(none)"]))
    lines.append("redacted_pi_entries:")
    lines.extend(f"  {item}" for item in (redacted_entries or ["(none)"]))
    lines.append("pi_findings:")
    lines.extend(f"  {item}" for item in (findings or ["(none)"]))
    return status, lines


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", nargs="?", default=".", help="Repo root (default: cwd)")
    parser.add_argument(
        "mode", nargs="?", default="summary", choices=("summary", "deep"),
        help="Output detail level",
    )
    args = parser.parse_args()
    root = Path(args.root).resolve()
    mode = args.mode
    home = Path(os.environ.get("HOME", str(Path.home()))).expanduser()

    if not root.is_dir():
        print(f"Repo root not found: {root}", file=sys.stderr)
        return 2

    instruction_files = project_instruction_files(root)
    agents = root / "AGENTS.md"
    claude = root / "CLAUDE.md"
    claude_delegates = claude_delegates_to_agents(claude)
    github_instructions_dir = root / ".github" / "instructions"
    github_instruction_count = (
        len(list(github_instructions_dir.glob("*.md"))) if github_instructions_dir.is_dir() else 0
    )

    instruction_findings: list[str] = []
    if not instruction_files:
        instruction_findings.append("no project agent instruction files")
    if agents.is_file() and claude.is_file() and not claude_delegates:
        claude_lines = len(read(claude).splitlines())
        agents_lines = len(read(agents).splitlines())
        if claude_lines > 20 and agents_lines > 20:
            instruction_findings.append(
                "AGENTS.md and CLAUDE.md both contain substantial guidance without delegation"
            )

    global_codex_agents = home / ".codex" / "AGENTS.md"
    codex_config = home / ".codex" / "config.toml"
    projects, features, plugins, marketplaces, redacted = parse_codex_config(codex_config)
    trust = project_trust(projects, root) if codex_config.is_file() else "unavailable"
    codex_findings: list[str] = []
    if not global_codex_agents.is_file() and not codex_config.is_file():
        codex_findings.append("Codex surface not found")
    elif codex_config.is_file() and trust == "missing":
        codex_findings.append("current project is not configured in Codex trust table")

    global_claude = home / ".claude" / "CLAUDE.md"
    project_settings = root / ".claude" / "settings.local.json"
    project_rules = root / ".claude" / "rules"
    project_skills = root / ".claude" / "skills"
    global_skills = home / ".claude" / "skills"
    claude_findings: list[str] = []
    if claude.is_file() and claude_delegates:
        claude_findings.append("CLAUDE.md delegates to AGENTS.md")
    if not global_claude.is_file() and not claude.is_file():
        claude_findings.append("Claude instruction surface not found")

    if (
        global_claude.is_file()
        and has_operational_rules(global_claude)
        and global_codex_agents.is_file()
        and looks_identity_only(global_codex_agents)
    ):
        codex_findings.append(
            "global Codex AGENTS.md has identity/memory context but lacks operational rules present in global Claude CLAUDE.md"
        )
    codex_config_text = read(codex_config) if codex_config.is_file() else ""
    if (
        'sandbox_mode = "danger-full-access"' in codex_config_text
        and 'approval_policy = "never"' in codex_config_text
        and "deny" not in codex_config_text.lower()
    ):
        codex_findings.append(
            "Codex high-permission mode lacks a deny floor; add denies for secrets, credentials, pipe-to-shell installers, and outbound shells"
        )

    conflict_findings: list[str] = []
    if agents.is_file() and claude.is_file() and not claude_delegates:
        conflict_findings.append("AGENTS.md and CLAUDE.md both exist; verify they do not diverge")

    instruction_status = "FAIL" if not instruction_files else ("WARN" if instruction_findings else "PASS")
    codex_status = "WARN" if codex_findings else "PASS"
    claude_status = (
        "WARN"
        if claude_findings and "surface not found" in " ".join(claude_findings)
        else "PASS"
    )
    conflict_status = "WARN" if conflict_findings else "PASS"

    print("=== AGENT INSTRUCTION SURFACE ===")
    print(f"agent_instruction_status: {instruction_status}")
    print(f"mode: {mode}")
    print(f"AGENTS.md: {yes(agents)}")
    print(f"CLAUDE.md: {yes(claude)}")
    print(f"claude_delegates_to_agents: {'yes' if claude_delegates else 'no'}")
    print(f".github/copilot-instructions.md: {yes(root / '.github' / 'copilot-instructions.md')}")
    print(f".github/instructions/*.md: {github_instruction_count}")
    print(f"GEMINI.md: {yes(root / 'GEMINI.md')}")
    print_list("instruction_files", [rel(path, root) for path in instruction_files])
    print_list("instruction_findings", instruction_findings)

    print("=== CODEX SURFACE ===")
    print(f"codex_status: {codex_status}")
    print(f"global_agents_md: {yes(global_codex_agents)}")
    print(f"global_config_toml: {yes(codex_config)}")
    print(f"project_trust: {trust}")
    print_list("features", features, limit=20 if mode == "summary" else None)
    print_list("enabled_plugins", plugins, limit=20 if mode == "summary" else None)
    print_list("marketplaces", marketplaces, limit=20 if mode == "summary" else None)
    print_list("redacted_config_entries", redacted)
    print_list("codex_findings", codex_findings)

    print("=== CLAUDE SURFACE ===")
    print(f"claude_status: {claude_status}")
    print(f"global_claude_md: {yes(global_claude)}")
    print(f"project_claude_md: {yes(claude)}")
    print(f"settings_local_json: {yes(project_settings)}")
    rule_count = len(list(project_rules.glob('*.md'))) if project_rules.is_dir() else 0
    local_skill_count = len(list(project_skills.glob('*/SKILL.md'))) if project_skills.is_dir() else 0
    global_skill_count = len(list(global_skills.glob('*/SKILL.md'))) if global_skills.is_dir() else 0
    print(f"project_rules: {rule_count}")
    print(f"project_skills: {local_skill_count}")
    print(f"global_skills: {global_skill_count}")
    print_list("claude_findings", claude_findings)

    _, pi_lines = summarize_pi_surface(root, home)
    for line in pi_lines:
        print(line)

    print("=== INSTRUCTION CONFLICTS ===")
    print(f"conflict_status: {conflict_status}")
    print_list("conflict_findings", conflict_findings)
    return 0


if __name__ == "__main__":
    sys.exit(main())
