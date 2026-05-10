#!/usr/bin/env python3
"""Check all dependencies required by the ppt-tts-script skill.

Checks: python-pptx, soffice (LibreOffice), pdftoppm (poppler), gemini CLI.
Exits 0 if all required deps are present, 1 otherwise.
Gemini CLI is optional — the skill can degrade gracefully without it.
"""

import shutil
import subprocess
import sys
import json


def check_python_package(package_name: str, import_name: str) -> dict:
    """Check if a Python package is importable."""
    try:
        __import__(import_name)
        return {"name": package_name, "status": "ok", "required": True}
    except ImportError:
        return {
            "name": package_name,
            "status": "missing",
            "required": True,
            "install_hint": f"pip install {package_name}  # 或 uv pip install {package_name}",
        }


def check_command(cmd: str, required: bool, install_hint: str) -> dict:
    """Check if a CLI command is available on PATH."""
    path = shutil.which(cmd)
    if path:
        # Try to get version
        version = ""
        try:
            # Some tools use --version, others use -v or -h
            for flag in ["--version", "-v"]:
                result = subprocess.run(
                    [cmd, flag], capture_output=True, text=True, timeout=5
                )
                out = (result.stdout or result.stderr).strip()
                if out and "error" not in out.lower()[:20]:
                    version = out.split("\n")[0]
                    break
        except Exception:
            pass
        return {
            "name": cmd,
            "status": "ok",
            "required": required,
            "path": path,
            "version": version,
        }
    else:
        return {
            "name": cmd,
            "status": "missing",
            "required": required,
            "install_hint": install_hint,
        }


def main():
    results = []

    # Python packages
    results.append(check_python_package("python-pptx", "pptx"))

    # CLI tools
    results.append(
        check_command(
            "soffice",
            required=True,
            install_hint="macOS: brew install --cask libreoffice\nLinux: apt-get install libreoffice",
        )
    )
    results.append(
        check_command(
            "pdftoppm",
            required=True,
            install_hint="macOS: brew install poppler\nLinux: apt-get install poppler-utils",
        )
    )
    results.append(
        check_command(
            "gemini",
            required=False,  # Optional — skill degrades gracefully
            install_hint="npm install -g @anthropic-ai/gemini-cli  # 或参考 https://github.com/google-gemini/gemini-cli",
        )
    )

    # Report
    missing_required = [r for r in results if r["status"] == "missing" and r["required"]]
    missing_optional = [r for r in results if r["status"] == "missing" and not r["required"]]
    all_ok = [r for r in results if r["status"] == "ok"]

    print("=" * 60)
    print("PPT-TTS-Script 依赖检查")
    print("=" * 60)

    if all_ok:
        print("\n✅ 已安装：")
        for r in all_ok:
            version_str = f" ({r.get('version', '')})" if r.get("version") else ""
            print(f"   {r['name']}{version_str}")

    if missing_optional:
        print("\n⚠️  可选依赖未安装（功能降级但不影响运行）：")
        for r in missing_optional:
            print(f"   {r['name']}")
            print(f"      安装方法: {r['install_hint']}")

    if missing_required:
        print("\n❌ 必需依赖未安装（无法运行）：")
        for r in missing_required:
            print(f"   {r['name']}")
            print(f"      安装方法: {r['install_hint']}")
        print()
        sys.exit(1)

    print("\n✅ 所有必需依赖已就绪。")

    if missing_optional:
        print("⚠️  Gemini CLI 未安装，第三步将使用模型自身生成逐字稿（质量可能下降）。")

    # Output structured result for programmatic consumption
    print("\n--- JSON ---")
    print(json.dumps({"checks": results, "all_required_met": len(missing_required) == 0}, ensure_ascii=False, indent=2))

    sys.exit(0)


if __name__ == "__main__":
    main()
