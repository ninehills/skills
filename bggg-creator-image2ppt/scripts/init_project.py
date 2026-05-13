#!/usr/bin/env python3
"""Create a bggg-creator-image2ppt project folder for one conversion run."""

from __future__ import annotations

import argparse
import json
import re
import shutil
from datetime import datetime
from pathlib import Path
from typing import Sequence


SKILL_ROOT = Path(__file__).resolve().parent.parent
PROJECTS_ROOT = SKILL_ROOT / "projects"


def slugify(value: str) -> str:
    text = value.strip().lower()
    text = re.sub(r"[^a-z0-9\u4e00-\u9fff]+", "_", text)
    text = re.sub(r"_+", "_", text).strip("_")
    return text or "image2ppt"


def unique_project_dir(date_prefix: str, slug: str) -> Path:
    base = PROJECTS_ROOT / f"{date_prefix}_{slug}"
    if not base.exists():
        return base
    index = 2
    while True:
        candidate = PROJECTS_ROOT / f"{date_prefix}_{slug}_{index}"
        if not candidate.exists():
            return candidate
        index += 1


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Initialize a project folder under bggg-creator-image2ppt/projects.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument("slug", help="short project name, e.g. investor_cover")
    parser.add_argument(
        "--source",
        action="append",
        default=[],
        help="optional source file to copy into original_inputs/; repeat for multiple files",
    )
    parser.add_argument("--date", help="YYYYMMDD override; defaults to local current date")
    parser.add_argument("--force-dir", help="explicit project directory name under projects/")
    return parser


def copy_sources(project_dir: Path, sources: list[str]) -> list[str]:
    copied: list[str] = []
    inputs_dir = project_dir / "original_inputs"
    for item in sources:
        source = Path(item).expanduser().resolve()
        if not source.exists():
            raise SystemExit(f"source not found: {source}")
        target = inputs_dir / source.name
        if target.exists():
            stem = target.stem
            suffix = target.suffix
            index = 2
            while True:
                candidate = inputs_dir / f"{stem}_{index}{suffix}"
                if not candidate.exists():
                    target = candidate
                    break
                index += 1
        shutil.copy2(source, target)
        copied.append(str(target))
    return copied


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    date_prefix = args.date or datetime.now().strftime("%Y%m%d")
    project_dir = (
        PROJECTS_ROOT / args.force_dir
        if args.force_dir
        else unique_project_dir(date_prefix, slugify(args.slug))
    )
    subdirs = [
        "original_inputs",
        "component_images",
        "imagegen_assets",
        "diagnostics",
        "exports",
    ]
    project_dir.mkdir(parents=True, exist_ok=True)
    for item in subdirs:
        (project_dir / item).mkdir(parents=True, exist_ok=True)

    copied_sources = copy_sources(project_dir, args.source)
    result = {
        "project_dir": str(project_dir),
        "original_inputs": str(project_dir / "original_inputs"),
        "component_images": str(project_dir / "component_images"),
        "imagegen_assets": str(project_dir / "imagegen_assets"),
        "diagnostics": str(project_dir / "diagnostics"),
        "exports": str(project_dir / "exports"),
        "sources": copied_sources,
        "manifest": str(project_dir / "manifest.json"),
        "output": str(project_dir / "output.pptx"),
        "summary": str(project_dir / "summary.json"),
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
