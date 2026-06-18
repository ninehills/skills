#!/usr/bin/env python3
"""Render a .pptx template to per-page PNGs.

Backends, in priority order (platform-dependent):
  macOS:   Keynote (AppleScript) > LibreOffice + pymupdf/pdf2image
  Windows: PowerPoint COM > LibreOffice + pymupdf/pdf2image
  Linux:   executable LibreOffice + pymupdf/pdf2image

Default output: <cwd>/template_renders/<pptx_stem>/page-NN.png
Intermediate PDF (LibreOffice path only) goes to <out_dir>/_source.pdf
and is left in place for inspection (gitignored under template_renders/).
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import platform
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Optional


DEFAULT_RENDERS_DIR_NAME = "template_renders"
RENDER_MANIFEST = "_render_manifest.json"
_LIBREOFFICE_PROBE_ERRORS: list[str] = []


def _safe_stem(name: str) -> str:
    cleaned = re.sub(r"[^\w\u4e00-\u9fff\-]+", "_", name).strip("_")
    return cleaned[:80] or "template"


def default_out_dir(pptx_path: Path) -> Path:
    return Path.cwd() / DEFAULT_RENDERS_DIR_NAME / _safe_stem(pptx_path.stem)


def _file_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def _render_manifest_path(out_dir: Path) -> Path:
    return out_dir / RENDER_MANIFEST


def _render_cache_valid(pptx_path: Path, out_dir: Path, existing: list[Path]) -> bool:
    manifest_path = _render_manifest_path(out_dir)
    if not manifest_path.is_file():
        return False
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception:
        return False
    return (
        manifest.get("source_path") == str(pptx_path)
        and manifest.get("source_sha256") == _file_sha256(pptx_path)
        and manifest.get("page_count") == len(existing)
    )


def _write_render_manifest(pptx_path: Path, out_dir: Path, page_count: int) -> None:
    manifest = {
        "source_path": str(pptx_path),
        "source_sha256": _file_sha256(pptx_path),
        "page_count": page_count,
    }
    _render_manifest_path(out_dir).write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def _cleanup_extra_pages(out_dir: Path, page_count: int) -> None:
    """Remove stale page-NN.png files only after a successful fresh render."""
    for page in out_dir.glob("page-*.png"):
        m = re.search(r"page-(\d+)\.png$", page.name)
        if not m:
            continue
        if int(m.group(1)) > page_count:
            page.unlink(missing_ok=True)


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
            if _render_cache_valid(pptx_path, out_dir, existing):
                print(f"📦 已渲染 {len(existing)} 页 -> {out_dir}（用 --force 强制重渲）")
                return out_dir
            print(f"(!)  模板渲染缓存已过期，重新渲染 -> {out_dir}")

    # Windows: try PowerPoint COM first (direct PNG export, no PDF step)
    count = _try_powerpoint_render(pptx_path, out_dir)
    if count is not None:
        _cleanup_extra_pages(out_dir, count)
        _write_render_manifest(pptx_path, out_dir, count)
        print(f"[OK] 渲染 {count} 页 -> {out_dir}")
        return out_dir

    # macOS: try Keynote AppleScript (direct PNG export, no PDF step)
    count = _try_keynote_render(pptx_path, out_dir)
    if count is not None:
        _cleanup_extra_pages(out_dir, count)
        _write_render_manifest(pptx_path, out_dir, count)
        print(f"[OK] 渲染 {count} 页 -> {out_dir}")
        return out_dir

    pdf_path = out_dir / "_source.pdf"
    print(f"🖨️  PPTX -> PDF：{pptx_path.name}")
    _convert_pptx_to_pdf(pptx_path, pdf_path)

    print(f"🖼️  PDF -> PNG（dpi={dpi}）...")
    n = _rasterize_pdf(pdf_path, out_dir, dpi=dpi)
    _cleanup_extra_pages(out_dir, n)
    _write_render_manifest(pptx_path, out_dir, n)
    print(f"[OK] 渲染 {n} 页 -> {out_dir}")
    return out_dir


def _probe_executable(path: str, args: list[str], timeout: int = 8) -> tuple[bool, str]:
    try:
        result = subprocess.run(
            [path, *args],
            check=False,
            capture_output=True,
            text=True,
            timeout=timeout,
        )
    except FileNotFoundError:
        return False, "file not found"
    except PermissionError:
        return False, "permission denied"
    except subprocess.TimeoutExpired:
        return False, f"timeout after {timeout}s"
    except OSError as e:
        return False, str(e)

    output = (result.stdout or result.stderr or "").strip().splitlines()
    detail = output[0] if output else f"exit code {result.returncode}"
    return result.returncode == 0, detail


def _libreoffice_candidates() -> list[str]:
    seen: set[str] = set()
    candidates: list[str] = []

    for env_name in ("LIBREOFFICE", "SOFFICE", "LIBREOFFICE_PATH"):
        value = os.environ.get(env_name)
        if value:
            candidates.append(value)

    for name in ("libreoffice", "soffice"):
        cli = shutil.which(name)
        if cli:
            candidates.append(cli)

    if sys.platform == "win32":
        for base in (
            os.environ.get("ProgramFiles", r"C:\Program Files"),
            os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)"),
        ):
            for lo_dir in ("LibreOffice", "LibreOffice Fresh", "LibreOffice Still"):
                candidate = os.path.join(base, lo_dir, "program", "soffice.exe")
                candidates.append(candidate)

    unique: list[str] = []
    for candidate in candidates:
        candidate = os.path.expanduser(candidate)
        if candidate in seen:
            continue
        seen.add(candidate)
        unique.append(candidate)
    return unique


def _find_libreoffice() -> Optional[str]:
    global _LIBREOFFICE_PROBE_ERRORS
    _LIBREOFFICE_PROBE_ERRORS = []

    for candidate in _libreoffice_candidates():
        if not os.path.isfile(candidate):
            _LIBREOFFICE_PROBE_ERRORS.append(f"{candidate}: not a file")
            continue
        ok, detail = _probe_executable(candidate, ["--version"])
        if ok:
            return candidate
        _LIBREOFFICE_PROBE_ERRORS.append(f"{candidate}: {detail}")

    return None


def _runtime_label() -> str:
    return f"{platform.system() or sys.platform} {platform.machine()} ({sys.platform})"


def check_render_backend() -> tuple[bool, list[str]]:
    """Return whether any local PPTX renderer is currently usable."""
    messages: list[str] = [f"Runtime: {_runtime_label()}"]

    if sys.platform == "win32":
        app = None
        try:
            from win32com import client as _win32  # type: ignore
            app = _win32.Dispatch("PowerPoint.Application")
            messages.append("PowerPoint COM: OK")
            return True, messages
        except Exception as e:
            messages.append(f"PowerPoint COM: unavailable ({e})")
        finally:
            if app is not None:
                try:
                    app.Quit()
                except Exception:
                    pass

    if sys.platform == "darwin":
        keynote_app = "/Applications/Keynote.app"
        if os.path.isdir(keynote_app):
            ok, detail = _probe_executable("/usr/bin/osascript", ["-e", 'tell application "Keynote" to version'])
            if ok:
                messages.append(f"Keynote: OK ({detail})")
                return True, messages
            messages.append(f"Keynote: found but AppleScript probe failed ({detail})")
        else:
            messages.append("Keynote: not found")

    cli = _find_libreoffice()
    if cli:
        messages.append(f"LibreOffice: OK ({cli})")
        return True, messages

    messages.append("LibreOffice: unavailable")
    if _LIBREOFFICE_PROBE_ERRORS:
        messages.extend(f"  - {err}" for err in _LIBREOFFICE_PROBE_ERRORS)
    messages.append(
        "Fallback: manually export template slides as page-01.png, page-02.png, ... "
        "and pass that directory with --template-images."
    )
    return False, messages


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


def _try_keynote_render(pptx_path: Path, out_dir: Path) -> Optional[int]:
    """macOS only: use Keynote AppleScript to export slides as PNGs.

    Returns page count, or None if unavailable / failed (caller should fall back to LO).
    """
    if sys.platform != "darwin":
        return None

    keynote_app = "/Applications/Keynote.app"
    if not os.path.isdir(keynote_app):
        return None

    pptx_path = pptx_path.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    dest_prefix = out_dir / "_keynote_export.png"

    script = f'''
tell application "Keynote"
    with timeout of 90 seconds
        try
            open POSIX file "{pptx_path}"
            set theDoc to front document
            set slideCount to count of slides of theDoc
            export theDoc to POSIX file "{dest_prefix}" as slide images ¬
                with properties {{image format:PNG, compression factor:1.0}}
            close theDoc without saving
            return slideCount
        on error errMsg number errNum
            -- Keynote may show a conversion dialog that blocks Apple Events;
            -- surface the error so Python can fall back to LibreOffice.
            return "ERR:" & errNum & ":" & errMsg
        end try
    end timeout
end tell
'''

    print(f"\U0001f5a5\ufe0f  尝试 Keynote 渲染：{pptx_path.name}")

    try:
        result = subprocess.run(
            ["osascript", "-e", script],
            check=True, capture_output=True, text=True, timeout=120,
        )
        stdout = result.stdout.strip()
        if stdout.startswith("ERR:"):
            print(f"(!) Keynote 失败 ({stdout[4:]})，回退到 LibreOffice")
            return None
        slide_count = int(result.stdout.strip())
    except subprocess.TimeoutExpired:
        print("(!) Keynote 导出超时，回退到 LibreOffice")
        return None
    except (subprocess.CalledProcessError, ValueError) as e:
        print(f"(!) Keynote 失败 ({e})，回退到 LibreOffice")
        return None

    # Rename _keynote_export.001.png, _keynote_export.002.png, ... -> page-01.png, ...
    existing = sorted(out_dir.glob("_keynote_export.*.png"))
    if len(existing) != slide_count:
        print(f"(!) Keynote 导出文件数 ({len(existing)}) 与页数 ({slide_count}) 不符，回退到 LibreOffice")
        # Clean up partial output
        for f in out_dir.glob("_keynote_export.*.png"):
            f.unlink(missing_ok=True)
        return None

    for f in existing:
        try:
            page_num = int(f.suffix.lstrip("."))
        except ValueError:
            continue
        f.rename(out_dir / f"page-{page_num:02d}.png")

    print(f"[OK] Keynote 导出 {slide_count} 页 -> {out_dir}")
    return slide_count


def _convert_pptx_to_pdf(pptx_path: Path, out_pdf: Path) -> None:
    cli = _find_libreoffice()
    if cli:
        try:
            subprocess.run(
                [cli, "--headless", "--convert-to", "pdf",
                 "--outdir", str(out_pdf.parent), str(pptx_path)],
                check=True, capture_output=True, text=True,
            )
        except (PermissionError, OSError, subprocess.CalledProcessError) as e:
            raise RuntimeError(_render_backend_error(f"LibreOffice 转 PDF 失败：{e}")) from e
        produced = out_pdf.parent / f"{pptx_path.stem}.pdf"
        if not produced.exists():
            raise RuntimeError(f"LibreOffice 未产出 PDF：{produced}")
        produced.replace(out_pdf)
        return

    raise RuntimeError(_render_backend_error("没找到可用的 PPTX 渲染后端。"))


def _render_backend_error(headline: str) -> str:
    lines = [
        headline,
        f"当前运行环境：{_runtime_label()}",
    ]
    if _LIBREOFFICE_PROBE_ERRORS:
        lines.append("LibreOffice 探测结果：")
        lines.extend(f"  - {err}" for err in _LIBREOFFICE_PROBE_ERRORS)

    lines.extend([
        "可选处理方式：",
        "  Windows: winget install LibreOffice.LibreOffice",
        "  macOS:   brew install --cask libreoffice，或安装 Keynote",
        "  Linux:   sudo apt-get install -y libreoffice",
        "  鸿蒙 / Termux / 容器 / 特殊架构：不要假设 Linux aarch64 LibreOffice 二进制可运行；"
        "请在桌面端把模板每页导出为 PNG，命名 page-01.png、page-02.png 后传 --template-images。",
        "示例：python3 scripts/generate_ppt.py --plan slides_plan.json "
        "--template-pptx template.pptx --template-images template_renders/template_manual --template-strict",
    ])
    return "\n".join(lines)


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
    p.add_argument("pptx", nargs="?", help="path to .pptx file")
    p.add_argument("-o", "--out", help="output directory (default: <cwd>/template_renders/<stem>/)")
    p.add_argument("--dpi", type=int, default=144, help="PNG dpi (default: 144)")
    p.add_argument("--force", action="store_true", help="re-render even if PNGs exist")
    p.add_argument("--check", action="store_true", help="check whether a local PPTX renderer is usable")
    args = p.parse_args()
    if args.check:
        ok, messages = check_render_backend()
        print("\n".join(messages))
        raise SystemExit(0 if ok else 1)
    if not args.pptx:
        p.error("the following arguments are required: pptx")
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
