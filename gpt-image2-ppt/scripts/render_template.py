#!/usr/bin/env python3
"""Render a .pptx template to per-page PNGs.

Backends, in priority order:
  PPTX -> PDF: native LibreOffice CLI
  PDF -> PNG:  pymupdf > pdf2image (needs poppler)

Default output: <cwd>/template_renders/<pptx_stem>/page-NN.png
Intermediate PDF goes to <out_dir>/_source.pdf and is left in place
for inspection (gitignored under template_renders/).
"""
from __future__ import annotations

import argparse
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Optional


DEFAULT_RENDERS_DIR_NAME = "template_renders"


def _safe_stem(name: str) -> str:
    cleaned = re.sub(r"[^\w\u4e00-\u9fff\-]+", "_", name).strip("_")
    return cleaned[:80] or "template"


def default_out_dir(pptx_path: Path) -> Path:
    return Path.cwd() / DEFAULT_RENDERS_DIR_NAME / _safe_stem(pptx_path.stem)


def render_pptx_to_pngs(
    pptx_path: str | Path,
    out_dir: Optional[Path] = None,
    dpi: int = 144,
    force: bool = False,
) -> Path:
    """Render every slide of pptx to PNGs. Returns the directory containing PNGs."""
    pptx_path = Path(pptx_path).resolve()
    if not pptx_path.exists():
        raise FileNotFoundError(f"PPTX not found: {pptx_path}")

    if out_dir is None:
        out_dir = default_out_dir(pptx_path)
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    if not force:
        existing = sorted(out_dir.glob("page-*.png"))
        if existing:
            print(f"📦 已渲染 {len(existing)} 页 -> {out_dir}（用 --force 强制重渲）")
            return out_dir

    # Windows: try PowerPoint COM first (direct PNG export, no PDF step)
    count = _try_powerpoint_render(pptx_path, out_dir)
    if count is not None:
        print(f"[OK] 渲染 {count} 页 -> {out_dir}")
        return out_dir

    pdf_path = out_dir / "_source.pdf"
    print(f"🖨️  PPTX -> PDF：{pptx_path.name}")
    _convert_pptx_to_pdf(pptx_path, pdf_path)

    print(f"🖼️  PDF -> PNG（dpi={dpi}）...")
    n = _rasterize_pdf(pdf_path, out_dir, dpi=dpi)
    print(f"[OK] 渲染 {n} 页 -> {out_dir}")
    return out_dir


def _find_libreoffice() -> Optional[str]:
    cli = shutil.which("libreoffice") or shutil.which("soffice")
    if cli:
        return cli
    if sys.platform == "win32":
        for base in (
            os.environ.get("ProgramFiles", r"C:\Program Files"),
            os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)"),
        ):
            for lo_dir in ("LibreOffice", "LibreOffice Fresh", "LibreOffice Still"):
                candidate = os.path.join(base, lo_dir, "program", "soffice.exe")
                if os.path.isfile(candidate):
                    return candidate
    return None


def _try_powerpoint_render(pptx_path: Path, out_dir: Path) -> Optional[int]:
    """Windows only: use PowerPoint COM to export slides as PNGs.

    Returns page count, or None if unavailable / failed (caller should fall back to LO).
    """
    if sys.platform != "win32":
        return None

    try:
        import pythoncom  # type: ignore
        from win32com import client as _win32  # type: ignore
    except ImportError:
        print("(!) pywin32 未安装，正在自动安装 …")
        result = subprocess.run(
            [sys.executable, "-m", "pip", "install", "pywin32"],
            check=False, capture_output=True, text=True,
        )
        if result.returncode != 0:
            print("(!) pywin32 安装失败，回退到 LibreOffice")
            return None
        try:
            import pythoncom  # type: ignore
            from win32com import client as _win32  # type: ignore
        except ImportError:
            print("(!) pywin32 安装后仍无法导入，回退到 LibreOffice")
            return None

    pptx_path = pptx_path.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    print(f"\U0001f5a5️  尝试 PowerPoint 渲染：{pptx_path.name}")

    app = None
    pres = None
    try:
        pythoncom.CoInitialize()
        app = _win32.Dispatch("PowerPoint.Application")
        app.Visible = True
        pres = app.Presentations.Open(str(pptx_path), WithWindow=False)
        pres.Export(str(out_dir), "PNG", 1920)  # 1920px wide (~144dpi for 16:9)
        count = len(pres.Slides)

        # Rename Slide1.PNG -> page-01.png
        for i in range(1, count + 1):
            src = out_dir / f"Slide{i}.PNG"
            dst = out_dir / f"page-{i:02d}.png"
            if src.exists():
                src.replace(dst)

        print(f"[OK] PowerPoint 导出 {count} 页 -> {out_dir}")
        return count
    except Exception as e:
        print(f"(!) PowerPoint 失败 ({e})，回退到 LibreOffice")
        return None
    finally:
        if pres is not None:
            try:
                pres.Close()
            except Exception:
                pass
        if app is not None:
            try:
                app.Quit()
            except Exception:
                pass
        try:
            pythoncom.CoUninitialize()
        except Exception:
            pass


def _convert_pptx_to_pdf(pptx_path: Path, out_pdf: Path) -> None:
    cli = _find_libreoffice()
    if cli:
        subprocess.run(
            [cli, "--headless", "--convert-to", "pdf",
             "--outdir", str(out_pdf.parent), str(pptx_path)],
            check=True, capture_output=True, text=True,
        )
        produced = out_pdf.parent / f"{pptx_path.stem}.pdf"
        if not produced.exists():
            raise RuntimeError(f"LibreOffice 未产出 PDF：{produced}")
        produced.replace(out_pdf)
        return

    raise RuntimeError(
        "没找到可用的 LibreOffice。请安装：\n"
        "  Windows: winget install LibreOffice.LibreOffice\n"
        "  macOS:   brew install --cask libreoffice\n"
        "  Linux:   sudo apt-get install -y libreoffice\n"
        "或者手动从 PowerPoint/Keynote 把每页导出 PNG，"
        "命名 page-01.png 起按字典序对应页码。"
    )


def _rasterize_pdf(pdf_path: Path, out_dir: Path, dpi: int = 144) -> int:
    pymupdf = None
    try:
        import pymupdf as _m  # type: ignore
        pymupdf = _m
    except ImportError:
        try:
            import fitz as _m  # type: ignore
            pymupdf = _m
        except ImportError:
            pass

    if pymupdf is not None:
        zoom = dpi / 72.0
        mat = pymupdf.Matrix(zoom, zoom)
        doc = pymupdf.open(str(pdf_path))
        n = len(doc)
        for i, page in enumerate(doc):
            pix = page.get_pixmap(matrix=mat)
            pix.save(str(out_dir / f"page-{i+1:02d}.png"))
        doc.close()
        return n

    try:
        from pdf2image import convert_from_path  # type: ignore
    except ImportError:
        raise RuntimeError(
            "PDF -> PNG 缺依赖。任选一种装：\n"
            "  - pip install pymupdf  （推荐，单装即可）\n"
            "  - pip install pdf2image && sudo apt-get install -y poppler-utils"
        )
    images = convert_from_path(str(pdf_path), dpi=dpi)
    for i, img in enumerate(images):
        img.save(str(out_dir / f"page-{i+1:02d}.png"), "PNG")
    return len(images)


def _cli() -> None:
    p = argparse.ArgumentParser(description="Render .pptx -> per-page PNGs")
    p.add_argument("pptx", help="path to .pptx file")
    p.add_argument("-o", "--out", help="output directory (default: <cwd>/template_renders/<stem>/)")
    p.add_argument("--dpi", type=int, default=144, help="PNG dpi (default: 144)")
    p.add_argument("--force", action="store_true", help="re-render even if PNGs exist")
    args = p.parse_args()
    out_dir = render_pptx_to_pngs(
        args.pptx, Path(args.out) if args.out else None,
        dpi=args.dpi, force=args.force,
    )
    print()
    print(f"模板渲染目录：{out_dir}")
    print(f"喂给 generate_ppt.py：")
    print(f"  --template-pptx {args.pptx} \\")
    print(f"  --template-images {out_dir}")


if __name__ == "__main__":
    _cli()
