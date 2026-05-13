#!/usr/bin/env python3
"""Create a bggg-creator-image2psd project folder for one conversion run."""

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
    return text or "image2psd"


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
        description="Initialize a project folder under bggg-creator-image2psd/projects.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument("slug", help="short project name, e.g. lifestyle_product")
    parser.add_argument("--source", help="optional source image to copy as original_reference.png")
    parser.add_argument("--date", help="YYYYMMDD override; defaults to local current date")
    parser.add_argument("--force-dir", help="explicit project directory name under projects/")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    date_prefix = args.date or datetime.now().strftime("%Y%m%d")
    project_name = args.force_dir or f"{date_prefix}_{slugify(args.slug)}"
    project_dir = PROJECTS_ROOT / project_name if args.force_dir else unique_project_dir(date_prefix, slugify(args.slug))
    subdirs = [
        "layer_sources",
        "psd_full_canvas_layers",
        "imagegen_assets",
        "diagnostics",
    ]
    project_dir.mkdir(parents=True, exist_ok=True)
    for item in subdirs:
        (project_dir / item).mkdir(parents=True, exist_ok=True)

    source_out = None
    if args.source:
        source = Path(args.source).expanduser().resolve()
        if not source.exists():
            raise SystemExit(f"source not found: {source}")
        source_out = project_dir / "original_reference.png"
        shutil.copy2(source, source_out)

    result = {
        "project_dir": str(project_dir),
        "source": str(source_out) if source_out else None,
        "layer_sources": str(project_dir / "layer_sources"),
        "psd_full_canvas_layers": str(project_dir / "psd_full_canvas_layers"),
        "imagegen_assets": str(project_dir / "imagegen_assets"),
        "diagnostics": str(project_dir / "diagnostics"),
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
