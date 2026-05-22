#!/usr/bin/env python3
"""Project audit signals (Phase 1) for /check audit mode.

Walks a project root and emits 10 structured signal blocks to stdout.
Each block ends with `status: PASS|WARN|FAIL` so the LLM driving the
4-axis Linus-style scorecard can skim quickly.

Pure stdlib. Read-only. Exits 0 even on WARN/FAIL so the harness does
not confuse "finding surfaced" with "script broken".

Run as: python3 skills/check/scripts/audit_signals.py --root <path>
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path


EXCLUDED_DIRS = {
    ".git", ".hg", ".svn", "node_modules", "dist", "build", ".next",
    "__pycache__", ".turbo", "target", ".venv", "venv", "vendor",
    "coverage", ".cache", ".parcel-cache", ".pytest_cache", ".mypy_cache",
    ".ruff_cache", "Pods", "Carthage", ".swiftpm", ".gradle",
}

SOURCE_EXTS = {
    ".py", ".swift", ".rs", ".go", ".ts", ".tsx", ".js", ".jsx", ".sh",
    ".bash", ".zsh", ".rb", ".java", ".kt", ".m", ".mm", ".vue", ".c",
    ".cc", ".cpp", ".h", ".hpp", ".cs",
}

HOTSPOT_LINES = 500
HOTSPOT_FAIL = 1500
HEREDOC_LINES = 100
DRIFT_WARN = 50
DRIFT_FAIL = 150
DUP_JACCARD = 0.70

MARKER_RE = re.compile(r"\b(TODO|FIXME|HACK|XXX)\b")
HEREDOC_OPEN_RE = re.compile(
    r"(python3?|node|ruby|perl|php)\b[^|\n]*?<<-?\s*['\"]?(\w+)['\"]?"
)
INSTALL_URL_RE = re.compile(
    r"raw\.githubusercontent\.com/[^/\s]+/[^/\s]+/([^/\s]+)/"
)
# --exclude requires = or trailing value to avoid matching git's --exclude-standard
DENYLIST_HINT_RE = re.compile(
    r"(^\s*(skip|exclude)\s*=|\s--exclude=|!\*\.\w+|grep\s+-v\b|--ignore=)",
    re.IGNORECASE,
)
MINIFIED_RE = re.compile(r"\.min\.[a-z]+$", re.IGNORECASE)


def is_excluded(path: Path, root: Path) -> bool:
    try:
        parts = path.relative_to(root).parts
    except ValueError:
        parts = path.parts
    if any(p in EXCLUDED_DIRS for p in parts):
        return True
    return bool(MINIFIED_RE.search(path.name))


def iter_files(root: Path) -> list[Path]:
    try:
        proc = subprocess.run(
            ["git", "-C", str(root), "ls-files",
             "--cached", "--others", "--exclude-standard"],
            text=True, stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL, check=False,
        )
        if proc.returncode == 0 and proc.stdout.strip():
            out = []
            for line in proc.stdout.splitlines():
                p = root / line
                if p.is_file() and not is_excluded(p, root):
                    out.append(p)
            return out
    except OSError:
        pass
    out = []
    for dirpath, dirnames, filenames in os.walk(root):
        current = Path(dirpath)
        dirnames[:] = [d for d in dirnames if d not in EXCLUDED_DIRS]
        if is_excluded(current, root):
            continue
        for fname in filenames:
            p = current / fname
            if p.is_file() and not is_excluded(p, root):
                out.append(p)
    return out


def line_count(path: Path) -> int:
    try:
        with path.open("rb") as fh:
            return sum(1 for _ in fh)
    except OSError:
        return 0


def read_text(path: Path, limit: int = 0) -> str:
    try:
        data = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return ""
    return data[:limit] if limit else data


def rel(path: Path, root: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


def header(name: str) -> None:
    print(f"=== {name} ===")


def status(label: str) -> None:
    print(f"status: {label}")


def block_hotspots(files: list[Path], root: Path) -> None:
    header("FILE SIZE HOTSPOTS")
    big = sorted(
        ((p, line_count(p)) for p in files
         if p.suffix in SOURCE_EXTS and line_count(p) >= HOTSPOT_LINES),
        key=lambda x: -x[1],
    )[:10]
    if not big:
        print("(no source files >= 800 lines)")
        status("PASS")
        return
    for path, n in big:
        print(f"  {n:>5}  {rel(path, root)}")
    if any(n >= HOTSPOT_FAIL for _, n in big):
        status("FAIL")
    elif len(big) > 3:
        status("WARN")
    else:
        status("WARN")


def block_heredoc(files: list[Path], root: Path) -> None:
    header("HEREDOC BLOAT")
    hits: list[tuple[str, int, str, int]] = []
    for path in files:
        if path.suffix not in {".sh", ".bash", ".zsh"}:
            continue
        text = read_text(path)
        if not text:
            continue
        lines = text.splitlines()
        i = 0
        while i < len(lines):
            m = HEREDOC_OPEN_RE.search(lines[i])
            if not m:
                i += 1
                continue
            lang, marker = m.group(1), m.group(2)
            j = i + 1
            close = re.compile(r"^\s*" + re.escape(marker) + r"\s*$")
            while j < len(lines) and not close.match(lines[j]):
                j += 1
            size = j - i
            if size >= HEREDOC_LINES:
                hits.append((rel(path, root), i + 1, lang, size))
            i = j + 1
    if not hits:
        print("(no python/node/ruby/perl/php heredocs >= 100 lines)")
        status("PASS")
        return
    for f, ln, lang, sz in hits:
        print(f"  {f}:{ln}  lang={lang}  block_lines={sz}")
    status("WARN")


def block_test_ci(files: list[Path], root: Path) -> None:
    header("TEST AND CI SURFACE")
    test_files = [
        p for p in files
        if p.suffix in SOURCE_EXTS
        and (("test" in p.name.lower()) or ("spec" in p.name.lower()))
    ]
    src_files = [p for p in files if p.suffix in SOURCE_EXTS]
    wf_dir = root / ".github" / "workflows"
    workflows = []
    if wf_dir.is_dir():
        workflows = sorted(list(wf_dir.glob("*.yml")) + list(wf_dir.glob("*.yaml")))
    job_names: list[str] = []
    for wf in workflows:
        text = read_text(wf, 50_000)
        for m in re.finditer(r"^name:\s*(.+?)\s*$", text, re.MULTILINE):
            job_names.append(f"{wf.name}: {m.group(1)[:60]}")
            break
    ratio = len(test_files) / max(len(src_files), 1)
    print(f"tests_count={len(test_files)} source_count={len(src_files)} "
          f"ratio={ratio:.1%}")
    print(f"ci_workflow_files={len(workflows)}")
    for j in job_names[:10]:
        print(f"  workflow: {j}")
    if not test_files and not workflows:
        status("FAIL")
    elif not test_files or not workflows:
        status("WARN")
    else:
        status("PASS")


def _grep_version(path: Path, pattern: str) -> str | None:
    text = read_text(path, 20_000)
    if not text:
        return None
    m = re.search(pattern, text, re.MULTILINE)
    return m.group(1).strip() if m else None


def block_version_sources(root: Path) -> None:
    header("VERSION SOURCE COUNT")
    found: list[tuple[str, str]] = []
    v = root / "VERSION"
    if v.is_file():
        first = read_text(v).strip().splitlines()
        if first:
            found.append(("VERSION", first[0]))
    probes = [
        ("package.json", r'"version"\s*:\s*"([^"]+)"'),
        ("Cargo.toml", r'^\s*version\s*=\s*"([^"]+)"'),
        ("pyproject.toml", r'^\s*version\s*=\s*"([^"]+)"'),
        ("setup.py", r"version\s*=\s*['\"]([^'\"]+)['\"]"),
    ]
    for fname, pat in probes:
        p = root / fname
        if p.is_file():
            v_str = _grep_version(p, pat)
            if v_str:
                found.append((fname, v_str))
    for pat in ("*.podspec", "*.csproj"):
        for path in root.glob(pat):
            v_str = _grep_version(
                path, r'(?i)version\s*[:=]\s*["\']?(\d+\.\d+\.\d+[\w.-]*)'
            )
            if v_str:
                found.append((path.name, v_str))
    for path in list(root.glob("build.gradle*")):
        v_str = _grep_version(
            path, r'(?i)version\s*[:=]\s*["\']?(\d+\.\d+\.\d+[\w.-]*)'
        )
        if v_str:
            found.append((path.name, v_str))
    if not found:
        print("(no declared version source found)")
        status("PASS")
        return
    for f, val in found:
        print(f"  {f}: {val}")
    distinct = {val for _, val in found if val}
    print(f"sources={len(found)} distinct_values={len(distinct)}")
    if len(found) > 1 and len(distinct) > 1:
        status("WARN")
    else:
        status("PASS")


def block_packaging_posture(root: Path) -> None:
    header("PACKAGING FILTER POSTURE")
    allowlist_files = list(root.glob("*.allowlist")) + list(root.glob("MANIFEST.in"))
    pkg_scripts = (list(root.glob("scripts/package*.sh"))
                   + list(root.glob("scripts/release*.sh")))
    denylist_hits = 0
    for sp in pkg_scripts:
        for line in read_text(sp).splitlines():
            if DENYLIST_HINT_RE.search(line):
                denylist_hits += 1
    if allowlist_files:
        for f in allowlist_files:
            print(f"  allowlist: {rel(f, root)}")
        print(f"posture=allowlist denylist_hits_in_scripts={denylist_hits}")
        status("PASS")
        return
    if denylist_hits:
        for sp in pkg_scripts:
            print(f"  script: {rel(sp, root)}")
        print(f"posture=denylist denylist_hits_in_scripts={denylist_hits}")
        status("WARN")
        return
    print("posture=none (no packaging scripts)")
    status("N/A")


def block_install_url(root: Path) -> None:
    header("INSTALL URL PINNING")
    targets: list[Path] = [root / "README.md"]
    targets += list(root.glob("scripts/setup*.sh"))
    targets += list(root.glob("scripts/install*.sh"))
    findings: list[tuple[str, int, str]] = []
    for path in targets:
        if not path.is_file():
            continue
        text = read_text(path, 200_000)
        for i, line in enumerate(text.splitlines(), start=1):
            for m in INSTALL_URL_RE.finditer(line):
                findings.append((rel(path, root), i, m.group(1)))
    if not findings:
        print("(no raw.githubusercontent.com refs found)")
        status("PASS")
        return
    moving = [f for f in findings if f[2] in ("main", "master", "HEAD")]
    for f, ln, ref in findings[:20]:
        marker = " [MOVING]" if ref in ("main", "master", "HEAD") else ""
        print(f"  {f}:{ln}  ref={ref}{marker}")
    print(f"total={len(findings)} moving={len(moving)}")
    if moving:
        status("WARN")
    else:
        status("PASS")


def block_agent_doc_dedup(root: Path) -> None:
    header("AGENT DOC DEDUP")
    claude = root / "CLAUDE.md"
    agents = root / "AGENTS.md"
    have_c = claude.exists() or claude.is_symlink()
    have_a = agents.exists() or agents.is_symlink()
    if not have_c and not have_a:
        print("posture=none")
        status("PASS")
        return
    if not (have_c and have_a):
        print(f"posture=single-file ({'CLAUDE.md' if have_c else 'AGENTS.md'} only)")
        status("PASS")
        return
    if claude.is_symlink() and claude.resolve(strict=False).name == "AGENTS.md":
        print("posture=symlink (CLAUDE.md -> AGENTS.md)")
        status("PASS")
        return
    if agents.is_symlink() and agents.resolve(strict=False).name == "CLAUDE.md":
        print("posture=symlink (AGENTS.md -> CLAUDE.md)")
        status("PASS")
        return
    a = read_text(claude)
    b = read_text(agents)
    if a and a == b:
        print("posture=identical (consider symlink to dedup)")
        status("WARN")
        return
    cross = ("AGENTS.md" in a) or ("CLAUDE.md" in b)
    a_set = {ln.strip() for ln in a.splitlines()
             if ln.strip() and not ln.strip().startswith("#")}
    b_set = {ln.strip() for ln in b.splitlines()
             if ln.strip() and not ln.strip().startswith("#")}
    union = a_set | b_set
    jaccard = len(a_set & b_set) / len(union) if union else 0.0
    print(f"jaccard={jaccard:.2f} cross_refs={cross}")
    if jaccard >= 0.20:
        print("posture=divergent-overlap (drift risk; consider symlink)")
        status("WARN")
        return
    if cross:
        print("posture=cross-ref (one references the other)")
        status("WARN")
        return
    print("posture=independent")
    status("PASS")


def block_drift_markers(files: list[Path], root: Path) -> None:
    header("DRIFT MARKERS")
    counts: list[tuple[str, int]] = []
    total = 0
    for path in files:
        if path.suffix not in SOURCE_EXTS:
            continue
        text = read_text(path, 200_000)
        if not text:
            continue
        n = len(MARKER_RE.findall(text))
        if n:
            counts.append((rel(path, root), n))
            total += n
    counts.sort(key=lambda x: -x[1])
    for f, n in counts[:5]:
        print(f"  {n:>4}  {f}")
    print(f"total={total}")
    if total >= DRIFT_FAIL:
        status("FAIL")
    elif total >= DRIFT_WARN:
        status("WARN")
    else:
        status("PASS")


def block_duplicate_setup(root: Path) -> None:
    header("DUPLICATE SETUP SCRIPTS")
    scripts = (list(root.glob("scripts/setup-*.sh"))
               + list(root.glob("scripts/install-*.sh")))
    if len(scripts) < 2:
        print("(fewer than 2 setup-* scripts to compare)")
        status("N/A")
        return
    sets: dict[Path, set[str]] = {}
    for sp in scripts:
        sets[sp] = {ln.strip() for ln in read_text(sp).splitlines()
                    if ln.strip() and not ln.strip().startswith("#")}
    pairs: list[tuple[str, str, float]] = []
    names = list(sets.keys())
    for i, a in enumerate(names):
        for b in names[i + 1:]:
            union = sets[a] | sets[b]
            if not union:
                continue
            j = len(sets[a] & sets[b]) / len(union)
            if j >= DUP_JACCARD:
                pairs.append((rel(a, root), rel(b, root), j))
    if not pairs:
        print("(no setup pairs with jaccard >= 0.70)")
        status("PASS")
        return
    for a, b, j in pairs:
        print(f"  {a} vs {b}  jaccard={j:.2f}")
    status("WARN")


def block_denylist_in_build(root: Path) -> None:
    header("DENYLIST IN BUILD")
    targets = (list(root.glob("scripts/package*.sh"))
               + list(root.glob("scripts/release*.sh"))
               + [root / "Makefile", root / "Justfile"])
    real_targets = [p for p in targets if p.is_file()]
    if not real_targets:
        print("(no build scripts present)")
        status("N/A")
        return
    hits: list[tuple[str, int, str]] = []
    for path in real_targets:
        text = read_text(path, 100_000)
        for i, line in enumerate(text.splitlines(), start=1):
            if DENYLIST_HINT_RE.search(line):
                hits.append((rel(path, root), i, line.strip()[:80]))
    if not hits:
        print("(no denylist patterns found in build scripts)")
        status("PASS")
        return
    for f, ln, s in hits[:20]:
        print(f"  {f}:{ln}  {s}")
    status("WARN")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--root", type=Path, default=Path.cwd(),
        help="Project root to audit (default: current working directory)",
    )
    args = parser.parse_args()
    root = args.root.resolve()
    if not root.is_dir():
        print(f"audit_signals: not a directory: {root}", file=sys.stderr)
        return 2
    files = iter_files(root)
    print(f"project_root: {root}")
    print(f"files_scanned: {len(files)}")
    print()
    block_hotspots(files, root); print()
    block_heredoc(files, root); print()
    block_test_ci(files, root); print()
    block_version_sources(root); print()
    block_packaging_posture(root); print()
    block_install_url(root); print()
    block_agent_doc_dedup(root); print()
    block_drift_markers(files, root); print()
    block_duplicate_setup(root); print()
    block_denylist_in_build(root)
    return 0


if __name__ == "__main__":
    sys.exit(main())
