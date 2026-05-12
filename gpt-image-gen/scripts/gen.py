#!/usr/bin/env python3
"""GPT Image CLI — unified interface for OpenAI gpt-image-2.

Endpoints (auto-routed):
  - Prompt only          → POST /v1/images/generations  (text-to-image)
  - Prompt + -i          → POST /v1/images/edits         (reference edit)
  - Prompt + -i + -m     → POST /v1/images/edits         (inpainting)

Auth resolution (first hit wins):
  1. OPENAI_API_KEY env var
  2. .env file (./.env, then ~/.env)
  3. Codex/ChatGPT OAuth token (via Hermes auth store)
  4. config.yaml → image_gen.openai.api_key

Usage:
  gpt-image -p "a cat astronaut"                                      # basic gen
  gpt-image -p "cyberpunk version" -i photo.jpg                       # img2img
  gpt-image -p "replace sky" -i photo.jpg -m sky_mask.png             # inpainting
  gpt-image -p "collab poster" -i cat.png -i logo.png -f out.png      # multi-ref
  gpt-image -p "..." --quality high --size 2k --format webp           # hi-res
"""

from __future__ import annotations

import argparse
import base64
import datetime
import json
import logging
import os
import re
import sys
import uuid
from pathlib import Path
from typing import Any, Dict, List, Optional
from urllib import request as url_request

# ──────────────────────────────────────────────
# Logging — quiet by default unless --verbose
# ──────────────────────────────────────────────
logger = logging.getLogger("gpt-image")


# ──────────────────────────────────────────────
# Constants
# ──────────────────────────────────────────────
API_MODEL = "gpt-image-2"

QUALITIES = {"auto", "low", "medium", "high"}
DEFAULT_QUALITY = "high"

MODERATION_LEVELS = {"auto", "low"}
FORMATS = {"png", "jpeg", "webp"}

SIZE_SHORTCUTS: Dict[str, str] = {
    "1k": "1024x1024",
    "square": "1024x1024",
    "2k": "2048x2048",
    "4k": "3840x2160",
    "landscape": "1536x1024",
    "portrait": "1024x1536",
    "wide": "2048x1152",
    "tall": "2160x3840",
}
DEFAULT_SIZE = "1024x1024"

MAX_SINGLE_EDGE = 3840
MIN_TOTAL_PX = 655_360
MAX_TOTAL_PX = 8_294_400
MAX_RATIO = 3.0

HERMES_HOME = Path(os.environ.get("HERMES_HOME", Path.home() / ".hermes"))
CACHE_DIR = HERMES_HOME / "cache" / "images"

# ──────────────────────────────────────────────
# Codex backend (ChatGPT/Codex OAuth → Responses API)
# ──────────────────────────────────────────────

_CODEX_CHAT_MODEL = "gpt-5.4"
_CODEX_BASE_URL = "https://chatgpt.com/backend-api/codex"
_CODEX_INSTRUCTIONS = (
    "You are an assistant that must fulfill image generation requests by "
    "using the image_generation tool when provided."
)


def _is_codex_token(key: str) -> bool:
    """Detect if an auth string is a Codex JWT (starts with eyJ)."""
    return key.startswith("eyJ")


def _codex_cloudflare_headers(access_token: str) -> Dict[str, str]:
    """Headers required to avoid Cloudflare 403s on Codex endpoint."""
    try:
        import json as _json
    except ImportError:
        return {
            "User-Agent": "codex_cli_rs/0.0.0 (Hermes Agent)",
            "originator": "codex_cli_rs",
        }
    headers = {
        "User-Agent": "codex_cli_rs/0.0.0 (Hermes Agent)",
        "originator": "codex_cli_rs",
    }
    if not access_token or not access_token.strip():
        return headers
    try:
        parts = access_token.split(".")
        if len(parts) < 2:
            return headers
        payload_b64 = parts[1] + "=" * (-len(parts[1]) % 4)
        payload = _json.loads(base64.b64decode(payload_b64))
        account_id = payload.get("chatgpt_account_id")
        if account_id:
            headers["ChatGPT-Account-ID"] = str(account_id)
    except Exception:
        pass
    return headers


def _generate_via_codex(
    api_key: str,
    *,
    prompt: str,
    size: str,
    quality: str,
    images: Optional[List[str]] = None,
    n: int = 1,
) -> List[Path]:
    """Generate images via Codex Responses API (chatgpt.com/backend-api/codex).

    Supports txt2img and reference-image edits by passing reference images
    as ``input_image`` content items alongside the text prompt.
    """
    try:
        import openai as _openai
    except ImportError:
        raise RuntimeError("openai package not installed (pip install openai)")

    if n != 1:
        logger.warning("Codex backend only supports n=1; ignoring n=%d", n)

    client = _openai.OpenAI(
        api_key=api_key,
        base_url=_CODEX_BASE_URL,
        default_headers=_codex_cloudflare_headers(api_key),
    )

    # Build input content: text + optional reference images
    content: List[Dict[str, Any]] = [
        {"type": "input_text", "text": prompt},
    ]
    if images:
        for img_path in images:
            img_bytes = _read_image_bytes(img_path)
            ext = Path(img_path).suffix.lower()
            mime_map = {
                ".jpg": "image/jpeg", ".jpeg": "image/jpeg",
                ".png": "image/png", ".webp": "image/webp",
                ".gif": "image/gif",
            }
            mime = mime_map.get(ext, "image/png")
            content.append({
                "type": "input_image",
                "image_url": (
                    f"data:{mime};base64,"
                    f"{base64.b64encode(img_bytes).decode()}"
                ),
            })

    image_b64: Optional[str] = None
    ref_count = len(images) if images else 0
    logger.info(
        "codex (%d refs) | model=%s size=%s quality=%s prompt=%.80s...",
        ref_count, API_MODEL, size, quality, prompt,
    )

    with client.responses.stream(
        model=_CODEX_CHAT_MODEL,
        store=False,
        instructions=_CODEX_INSTRUCTIONS,
        input=[{
            "type": "message",
            "role": "user",
            "content": content,
        }],
        tools=[{
            "type": "image_generation",
            "model": API_MODEL,
            "size": size,
            "quality": quality,
            "output_format": "png",
            "background": "opaque",
            "partial_images": 1,
        }],
        tool_choice={
            "type": "allowed_tools",
            "mode": "required",
            "tools": [{"type": "image_generation"}],
        },
    ) as stream:
        for event in stream:
            event_type = getattr(event, "type", "")
            if event_type == "response.output_item.done":
                item = getattr(event, "item", None)
                if getattr(item, "type", None) == "image_generation_call":
                    result = getattr(item, "result", None)
                    if isinstance(result, str) and result:
                        image_b64 = result
            elif event_type == "response.image_generation_call.partial_image":
                partial = getattr(event, "partial_image_b64", None)
                if isinstance(partial, str) and partial:
                    image_b64 = partial
        final = stream.get_final_response()

    # Final-response sweep
    for item in getattr(final, "output", None) or []:
        if getattr(item, "type", None) == "image_generation_call":
            result = getattr(item, "result", None)
            if isinstance(result, str) and result:
                image_b64 = result

    if not image_b64:
        raise ValueError(
            "Codex response contained no image_generation_call result"
        )

    raw = base64.b64decode(image_b64)
    out = make_output_path(prompt, fmt="png")
    _save_response_image(raw, out)
    _save_prompt_sidecar(
        out,
        prompt=prompt,
        model=API_MODEL,
        size=size,
        quality=quality,
        images=images,
        n=1,
        fmt="png",
        moderation="low",
    )
    return [out]


# ──────────────────────────────────────────────
# .env loading
# ──────────────────────────────────────────────


def _load_dotenv() -> None:
    """Load .env files if python-dotenv is available."""
    try:
        from dotenv import load_dotenv

        for env_path in (Path.cwd() / ".env", Path.home() / ".env"):
            if env_path.exists():
                load_dotenv(env_path, override=False)
    except ImportError:
        pass


# ──────────────────────────────────────────────
# Auth resolution
# ──────────────────────────────────────────────


def _read_codex_token() -> Optional[str]:
    """Try to read Codex OAuth token from Hermes auth store."""
    try:
        sys.path.insert(0, str(HERMES_HOME / "hermes-agent"))
        from agent.auxiliary_client import _read_codex_access_token

        token = _read_codex_access_token()
        if isinstance(token, str) and token.strip():
            return token.strip()
    except Exception:
        pass
    return None


def _read_config_api_key() -> Optional[str]:
    """Try config.yaml → image_gen.openai.api_key."""
    try:
        from hermes_cli.config import load_config

        cfg = load_config()
        section = cfg.get("image_gen") if isinstance(cfg, dict) else None
        if isinstance(section, dict):
            openai_cfg = section.get("openai")
            if isinstance(openai_cfg, dict):
                key = openai_cfg.get("api_key")
                if isinstance(key, str) and key.strip():
                    return key.strip()
    except Exception:
        pass
    return None


def resolve_api_key() -> Optional[str]:
    """Resolve API key: env → .env files → Codex token → config."""
    _load_dotenv()
    env_key = os.environ.get("OPENAI_API_KEY", "").strip()
    if env_key:
        return env_key
    codex_token = _read_codex_token()
    if codex_token:
        return codex_token
    return _read_config_api_key()


# ──────────────────────────────────────────────
# Size parsing & validation
# ──────────────────────────────────────────────


def parse_size(raw: str) -> str:
    """Normalize a size string to WxH.

    Accepts shortcuts (1k, square, 2k, 4k, landscape, portrait, wide, tall)
    or literal WxH strings.
    """
    raw = raw.strip().lower()
    if raw in SIZE_SHORTCUTS:
        return SIZE_SHORTCUTS[raw]
    if "x" in raw:
        parts = raw.split("x")
        if len(parts) == 2:
            try:
                w, h = int(parts[0]), int(parts[1])
                _validate_dimensions(w, h)
                return f"{w}x{h}"
            except ValueError:
                pass
    logger.warning("Invalid size '%s', falling back to %s", raw, DEFAULT_SIZE)
    return DEFAULT_SIZE


def _validate_dimensions(w: int, h: int) -> None:
    """Validate against gpt-image-2 constraints."""
    total = w * h
    if total < MIN_TOTAL_PX:
        raise ValueError(f"Total pixels {total:,} below minimum {MIN_TOTAL_PX:,}")
    if total > MAX_TOTAL_PX:
        raise ValueError(f"Total pixels {total:,} exceeds maximum {MAX_TOTAL_PX:,}")
    if w > MAX_SINGLE_EDGE or h > MAX_SINGLE_EDGE:
        raise ValueError(f"Single edge exceeds max {MAX_SINGLE_EDGE}px")
    if w % 16 != 0 or h % 16 != 0:
        raise ValueError("Dimensions must be multiples of 16")
    if max(w, h) / min(w, h) > MAX_RATIO:
        raise ValueError(f"Aspect ratio exceeds {MAX_RATIO}:1")


# ──────────────────────────────────────────────
# Smart output naming
# ──────────────────────────────────────────────


def _slugify(text: str, max_len: int = 60) -> str:
    """Turn prompt text into a filename-safe slug."""
    slug = re.sub(r"[^\w\s-]", "", text.lower())
    slug = re.sub(r"[-\s]+", "-", slug).strip("-")
    return slug[:max_len]


def make_output_path(
    prompt: str,
    output: Optional[str] = None,
    fmt: str = "png",
) -> Path:
    """Determine output file path.

    Priority: explicit --output → prompt-slug + timestamp → UUID fallback.
    """
    if output:
        p = Path(output).expanduser().resolve()
        p.parent.mkdir(parents=True, exist_ok=True)
        return p

    CACHE_DIR.mkdir(parents=True, exist_ok=True)
    ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    slug = _slugify(prompt) or "image"
    short = uuid.uuid4().hex[:6]
    return CACHE_DIR / f"{ts}_{slug}_{short}.{fmt}"


# ──────────────────────────────────────────────
# Image encode / decode
# ──────────────────────────────────────────────


def _read_image_bytes(path: str) -> bytes:
    p = Path(path).expanduser().resolve()
    if not p.exists():
        raise FileNotFoundError(f"Image not found: {p}")
    return p.read_bytes()


def _save_response_image(raw: bytes, output_path: Path) -> Path:
    output_path.write_bytes(raw)
    return output_path


def _save_prompt_sidecar(
    image_path: Path,
    *,
    prompt: str,
    model: str,
    size: str,
    quality: str,
    images: Optional[List[str]] = None,
    mask: Optional[str] = None,
    n: int = 1,
    fmt: str = "png",
    moderation: str = "low",
    background: Optional[str] = None,
    compression: Optional[int] = None,
    input_fidelity: Optional[str] = None,
    revised_prompt: Optional[str] = None,
) -> Path:
    """Save a .prompt.txt sidecar alongside the generated image."""
    sidecar_path = image_path.with_suffix(".prompt.txt")
    lines = [
        f"# GPT Image 2 — Prompt Sidecar",
        f"# Generated: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
        f"",
        f"prompt: {prompt}",
        f"model: {model}",
        f"size: {size}",
        f"quality: {quality}",
        f"format: {fmt}",
        f"n: {n}",
        f"moderation: {moderation}",
    ]
    if background:
        lines.append(f"background: {background}")
    if compression is not None:
        lines.append(f"compression: {compression}")
    if input_fidelity:
        lines.append(f"input_fidelity: {input_fidelity}")
    if images:
        lines.append(f"reference_images: {', '.join(images)}")
    if mask:
        lines.append(f"mask: {mask}")
    if revised_prompt:
        lines.append(f"revised_prompt: {revised_prompt}")

    sidecar_path.write_text("\n".join(lines) + "\n")
    return sidecar_path


# ──────────────────────────────────────────────
# Model-specific safety
# ──────────────────────────────────────────────


def _model_rejects_input_fidelity(model: str) -> bool:
    """gpt-image-2 rejects ``input_fidelity`` — strip it."""
    return model.strip().lower().startswith("gpt-image-2")


# ──────────────────────────────────────────────
# Generation
# ──────────────────────────────────────────────


def generate(
    client: Any,
    *,
    prompt: str,
    model: str = API_MODEL,
    size: str,
    quality: str,
    images: Optional[List[str]] = None,
    mask: Optional[str] = None,
    n: int = 1,
    fmt: str = "png",
    moderation: str = "low",
    background: Optional[str] = None,
    compression: Optional[int] = None,
    input_fidelity: Optional[str] = None,
) -> List[Path]:
    """Run generation or edit, return list of saved file paths."""

    # Shared base payload
    payload: Dict[str, Any] = {
        "model": model,
        "prompt": prompt,
        "size": size,
        "n": n,
        "quality": quality,
        "moderation": moderation,
    }

    # Output format
    payload["response_format"] = fmt

    # Background (opaque vs auto)
    if background:
        payload["background"] = background

    # Compression (jpeg/webp only)
    if compression is not None and fmt in ("jpeg", "webp"):
        payload["compression"] = compression

    # Input fidelity — strip for gpt-image-2
    if input_fidelity and not _model_rejects_input_fidelity(model):
        payload["input_fidelity"] = input_fidelity

    # --- Endpoint routing ---
    has_images = bool(images)

    # images.edit does not accept 'moderation'
    if has_images:
        payload.pop("moderation", None)

    if not has_images:
        # Text-to-image
        logger.info(
            "generate | model=%s size=%s quality=%s n=%d prompt=%.80s...",
            model, size, quality, n, prompt,
        )
        response = client.images.generate(**payload)

    elif mask:
        # Inpainting: image + mask → images.edit
        if len(images) > 1:
            raise ValueError("Inpainting supports exactly one reference image (got %d)" % len(images))
        logger.info(
            "edit (inpaint) | ref=%s mask=%s prompt=%.80s...",
            images[0], mask, prompt,
        )
        image_data = _read_image_bytes(images[0])
        mask_data = _read_image_bytes(mask)
        response = client.images.edit(
            image=image_data,
            mask=mask_data,
            **payload,
        )

    else:
        # Reference edit (single or multi)
        logger.info(
            "edit (%d refs) | prompt=%.80s...",
            len(images), prompt,
        )
        if len(images) == 1:
            response = client.images.edit(
                image=_read_image_bytes(images[0]),
                **payload,
            )
        else:
            # Multi-reference: pack as tuple of bytes
            refs = tuple(_read_image_bytes(p) for p in images)
            response = client.images.edit(
                image=refs,
                **payload,
            )

    # --- Parse response ---
    data = getattr(response, "data", None) or []
    if not data:
        raise ValueError("API returned no image data")

    saved: List[Path] = []
    for i, item in enumerate(data):
        b64 = getattr(item, "b64_json", None)
        url = getattr(item, "url", None)
        revised_prompt = getattr(item, "revised_prompt", None)

        raw: Optional[bytes] = None
        if b64:
            raw = base64.b64decode(b64)
        elif url:
            with url_request.urlopen(url, timeout=300) as r:
                raw = r.read()
        else:
            raise ValueError("Response item has neither b64_json nor url")

        suffix = f"_{i + 1}" if n > 1 else ""
        out = make_output_path(prompt + suffix, fmt=fmt)
        _save_response_image(raw, out)
        _save_prompt_sidecar(
            out,
            prompt=prompt,
            model=model,
            size=size,
            quality=quality,
            images=images,
            mask=mask,
            n=n,
            fmt=fmt,
            moderation=moderation,
            background=background,
            compression=compression if compression and fmt in ("jpeg", "webp") else None,
            input_fidelity=input_fidelity,
            revised_prompt=revised_prompt,
        )
        saved.append(out)

    return saved


# ──────────────────────────────────────────────
# CLI
# ──────────────────────────────────────────────


def _build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        prog="gpt-image",
        description="GPT Image 2 — text-to-image, reference edit, and inpainting via OpenAI API.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""\
Exit codes: 0=success, 1=API error, 2=bad arguments

Examples:
  %(prog)s -p "a cat astronaut on the moon"
  %(prog)s -p "cyberpunk style" -i photo.jpg --quality high
  %(prog)s -p "replace sky with aurora" -i photo.jpg -m sky_mask.png
  %(prog)s -p "collab poster" -i cat.png -i logo.png -f out.png
  %(prog)s -p "..." --size 2k --quality high --format jpeg --compression 85
""",
    )

    # ── Required ──
    p.add_argument("-p", "--prompt", required=True, help="Text prompt or edit instruction")

    # ── Images ──
    p.add_argument("-i", "--image", action="append", dest="images",
                   help="Reference image(s). Repeat for multi-ref. Triggers images.edit endpoint.")
    p.add_argument("-m", "--mask", help="Alpha-channel PNG mask for inpainting (requires -i)")

    # ── Output ──
    p.add_argument("-f", "--output", help="Output file path (default: auto-named under cache dir)")
    p.add_argument("-n", "--n", type=int, default=1, help="Number of images to generate (default: 1)")

    # ── Quality & size ──
    p.add_argument("--model", default=API_MODEL,
                   help=f"Model ID (default: {API_MODEL})")
    p.add_argument("--quality", "-q", choices=sorted(QUALITIES), default=DEFAULT_QUALITY,
                   help=f"Output quality (default: {DEFAULT_QUALITY})")
    p.add_argument("--size", "-s", default=DEFAULT_SIZE,
                   help=f"Size: shortcut ({', '.join(SIZE_SHORTCUTS)}) or WxH. Default: {DEFAULT_SIZE}")

    # ── Format ──
    p.add_argument("--format", choices=sorted(FORMATS), default="png",
                   help="Output format (default: png)")
    p.add_argument("--compression", type=int, metavar="0-100",
                   help="Compression level for jpeg/webp output")

    # ── Moderation & background ──
    p.add_argument("--moderation", choices=sorted(MODERATION_LEVELS), default="low",
                   help="Content moderation level (default: low)")
    p.add_argument("--background", choices=["opaque", "auto"],
                   help="Background handling (opaque disables transparency)")

    # ── Advanced ──
    p.add_argument("--input-fidelity", choices=["low", "high"],
                   help="Input fidelity for edits (auto-stripped for gpt-image-2)")
    p.add_argument("--api-key", help="Override OpenAI API key")

    # ── Output control ──
    p.add_argument("--json", action="store_true", help="Output results as JSON")
    p.add_argument("--verbose", "-v", action="store_true", help="Verbose logging")

    return p


def main() -> None:
    parser = _build_parser()
    args = parser.parse_args()

    if args.verbose:
        logging.basicConfig(level=logging.DEBUG, format="%(levelname)s %(message)s")
    else:
        logging.basicConfig(level=logging.WARNING, format="%(message)s")

    # ── Validate args ──
    if args.mask and not args.images:
        logger.error("--mask requires at least one --image (-i)")
        sys.exit(2)

    if args.n < 1:
        logger.error("--n must be >= 1")
        sys.exit(2)

    # ── Resolve auth ──
    api_key = args.api_key or resolve_api_key()
    if not api_key:
        logger.error(
            "No API key found. Options:\n"
            "  • export OPENAI_API_KEY=...\n"
            "  • Create ~/.env with OPENAI_API_KEY=...\n"
            "  • Run `hermes auth codex`\n"
            "  • Pass --api-key"
        )
        sys.exit(2)

    # ── Parse size ──
    try:
        size = parse_size(args.size)
    except ValueError as e:
        logger.error("Invalid size: %s", e)
        sys.exit(2)

    # ── Generate ──
    use_codex = _is_codex_token(api_key)
    # Codex backend does not support mask inpainting
    if use_codex and args.mask:
        logger.error("Codex backend does not support --mask inpainting")
        sys.exit(2)

    # Format handling (non-Codex path only)
    if args.compression is not None and args.format not in ("jpeg", "webp"):
        logger.warning("--compression is only meaningful for jpeg/webp; ignoring")

    # Strip input_fidelity for gpt-image-2 (non-Codex path only)
    input_fidelity = args.input_fidelity
    if input_fidelity and _model_rejects_input_fidelity(args.model):
        logger.debug("input_fidelity stripped (gpt-image-2 rejects it)")
        input_fidelity = None

    try:
        if use_codex:
            saved = _generate_via_codex(
                api_key,
                prompt=args.prompt,
                size=size,
                quality=args.quality,
                images=args.images,
                n=args.n,
            )
        else:
            import openai as _openai_check  # noqa: F401
            client = _openai_check.OpenAI(api_key=api_key)
            saved = generate(
                client,
                prompt=args.prompt,
                model=args.model,
                size=size,
                quality=args.quality,
                images=args.images,
                mask=args.mask,
                n=args.n,
                fmt=args.format,
                moderation=args.moderation,
                background=args.background,
                compression=args.compression,
                input_fidelity=input_fidelity,
            )
    except Exception as e:
        logger.error("Generation failed: %s", e)
        sys.exit(1)

    # ── Output ──
    if args.json:
        result = {
            "success": True,
            "model": args.model,
            "prompt": args.prompt,
            "size": size,
            "quality": args.quality,
            "format": args.format,
            "images": [str(p) for p in saved],
        }
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        # Always output prompt + image path together
        print("── PROMPT ──")
        print(args.prompt)
        print("── IMAGE ──")
        for p in saved:
            print(p)
        print("── PARAMS ──")
        print(f"model={args.model} size={size} quality={args.quality} format={args.format}")

    sys.exit(0)


if __name__ == "__main__":
    main()
