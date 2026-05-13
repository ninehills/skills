#!/usr/bin/env python3
"""Build layered PSD files from image elements or color-split a flat image.

This script intentionally avoids Photoshop and ImageMagick. It writes a basic
8-bit RGB PSD with raster layers, layer names, alpha channels, and a flattened
composite preview. The resulting PSD opens in Photoshop, Photopea, Affinity,
and most PSD readers that support normal raster layers.
"""

from __future__ import annotations

import argparse
import json
import math
import re
import struct
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable, Sequence

import numpy as np
from PIL import Image, ImageDraw, ImageFont


BLEND_KEYS = {
    "normal": b"norm",
    "norm": b"norm",
    "multiply": b"mul ",
    "mul": b"mul ",
    "screen": b"scrn",
    "scrn": b"scrn",
    "overlay": b"over",
    "over": b"over",
}


@dataclass
class Layer:
    name: str
    image: Image.Image
    blend_mode: str = "normal"
    opacity: float = 1.0


def fail(message: str) -> None:
    raise SystemExit(f"error: {message}")


def parse_color(value: str | Sequence[int] | None, default: tuple[int, int, int] = (255, 255, 255)) -> tuple[int, int, int]:
    if value is None:
        return default
    if isinstance(value, (list, tuple)):
        if len(value) < 3:
            fail(f"color sequence needs at least 3 values: {value}")
        return tuple(max(0, min(255, int(v))) for v in value[:3])  # type: ignore[return-value]
    text = str(value).strip()
    named = {
        "white": "#ffffff",
        "black": "#000000",
        "transparent": "#ffffff",
    }
    text = named.get(text.lower(), text)
    if text.startswith("#"):
        text = text[1:]
    if len(text) == 3:
        text = "".join(ch * 2 for ch in text)
    if not re.fullmatch(r"[0-9a-fA-F]{6}", text):
        fail(f"invalid color: {value!r}")
    return int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16)


def color_hex(rgb: Sequence[int]) -> str:
    return "#{:02x}{:02x}{:02x}".format(int(rgb[0]), int(rgb[1]), int(rgb[2]))


def ensure_rgba(image: Image.Image) -> Image.Image:
    if image.mode == "RGBA":
        return image
    return image.convert("RGBA")


def image_from_path(path: Path) -> Image.Image:
    if not path.exists():
        fail(f"image not found: {path}")
    try:
        return Image.open(path).convert("RGBA")
    except Exception as exc:  # pragma: no cover - keeps CLI errors readable
        fail(f"cannot read image {path}: {exc}")


def fit_to_canvas(image: Image.Image, canvas: tuple[int, int], mode: str) -> Image.Image:
    mode = (mode or "none").lower()
    width, height = canvas
    if mode == "none":
        return image
    if mode == "stretch":
        return image.resize((width, height), Image.Resampling.LANCZOS)
    scale_x = width / image.width
    scale_y = height / image.height
    if mode == "contain":
        scale = min(scale_x, scale_y)
    elif mode == "cover":
        scale = max(scale_x, scale_y)
    else:
        fail(f"unknown fit mode {mode!r}; use none, contain, cover, or stretch")
    resized = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.LANCZOS)
    if mode == "cover":
        left = max(0, (resized.width - width) // 2)
        top = max(0, (resized.height - height) // 2)
        return resized.crop((left, top, left + width, top + height))
    return resized


def place_on_canvas(image: Image.Image, canvas: tuple[int, int], x: int = 0, y: int = 0) -> Image.Image:
    out = Image.new("RGBA", canvas, (0, 0, 0, 0))
    out.alpha_composite(ensure_rgba(image), (int(x), int(y)))
    return out


def apply_opacity(image: Image.Image, opacity: float) -> Image.Image:
    opacity = max(0.0, min(1.0, float(opacity)))
    if opacity >= 0.999:
        return image
    rgba = np.asarray(image.convert("RGBA")).copy()
    rgba[:, :, 3] = np.clip(rgba[:, :, 3].astype(np.float32) * opacity, 0, 255).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def background_to_alpha(
    image: Image.Image,
    bg_color: tuple[int, int, int] = (255, 255, 255),
    tolerance: float = 8.0,
    feather: float = 45.0,
    strength: float = 1.0,
    min_alpha: int = 2,
) -> Image.Image:
    """Turn a flat background color into transparency.

    For a white background, use the standard white-to-alpha recovery so colored
    text and antialiased edges stay crisp. For other colors, use distance from
    the sampled background color.
    """
    rgba = np.asarray(image.convert("RGBA")).astype(np.float32)
    rgb = rgba[:, :, :3] / 255.0
    existing_alpha = rgba[:, :, 3] / 255.0
    bg = np.array(bg_color, dtype=np.float32) / 255.0

    if max(bg_color) >= 245 and min(bg_color) >= 245:
        alpha = (1.0 - np.min(rgb, axis=2)) * float(strength)
        if tolerance > 0:
            dist = np.linalg.norm((1.0 - rgb) * 255.0, axis=2)
            gate = np.clip((dist - float(tolerance)) / max(1.0, float(feather) * 0.25), 0.0, 1.0)
            alpha *= gate
    else:
        dist = np.linalg.norm((rgb - bg) * 255.0, axis=2)
        denom = max(1.0, float(feather))
        alpha = np.clip((dist - float(tolerance)) / denom, 0.0, 1.0) * float(strength)

    alpha = np.clip(alpha, 0.0, 1.0) * existing_alpha
    alpha[alpha < (float(min_alpha) / 255.0)] = 0.0

    out_rgb = rgb.copy()
    mask = alpha > 1e-6
    # Recover foreground colors from alpha-composited background.
    out_rgb[mask] = (rgb[mask] - bg * (1.0 - alpha[mask, None])) / alpha[mask, None]
    out_rgb = np.clip(out_rgb, 0.0, 1.0)

    out = np.dstack([(out_rgb * 255.0).astype(np.uint8), (alpha * 255.0).astype(np.uint8)])
    return Image.fromarray(out, "RGBA")


def corner_color(image: Image.Image, sample: int = 12) -> tuple[int, int, int]:
    rgb = np.asarray(image.convert("RGB"))
    h, w = rgb.shape[:2]
    sample = max(1, min(sample, h, w))
    patches = [
        rgb[:sample, :sample],
        rgb[:sample, w - sample :],
        rgb[h - sample :, :sample],
        rgb[h - sample :, w - sample :],
    ]
    merged = np.concatenate([p.reshape(-1, 3) for p in patches], axis=0)
    return tuple(np.median(merged, axis=0).astype(int))  # type: ignore[return-value]


def preserve_light_foreground_to_alpha(
    image: Image.Image,
    tolerance: float = 10.0,
    preserve_opacity: float = 0.72,
    min_area_ratio: float = 0.00025,
) -> Image.Image:
    """White-to-alpha plus a soft structure mask for light foreground objects.

    This helps with objects such as pale sails, paper, white product packaging,
    or low-contrast illustrations whose interior is close to the background.
    It is deliberately conservative: if OpenCV is unavailable, it falls back to
    plain white-to-alpha.
    """
    base = background_to_alpha(image, (255, 255, 255), tolerance=tolerance)
    try:
        import cv2  # type: ignore
    except Exception:
        return base

    rgb_u8 = np.asarray(image.convert("RGB"))
    h, w = rgb_u8.shape[:2]
    dist = np.sqrt(np.sum((255.0 - rgb_u8.astype(np.float32)) ** 2, axis=2))
    rough = (dist > float(tolerance)).astype(np.uint8) * 255

    kernel_open = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    kernel_close = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (17, 17))
    rough = cv2.morphologyEx(rough, cv2.MORPH_OPEN, kernel_open, iterations=1)
    rough = cv2.morphologyEx(rough, cv2.MORPH_CLOSE, kernel_close, iterations=2)

    count, labels, stats, _ = cv2.connectedComponentsWithStats(rough, 8)
    keep = np.zeros_like(rough)
    min_area = max(24, int(w * h * float(min_area_ratio)))
    for idx in range(1, count):
        if stats[idx, cv2.CC_STAT_AREA] >= min_area:
            keep[labels == idx] = 255

    flood = keep.copy()
    ff_mask = np.zeros((h + 2, w + 2), dtype=np.uint8)
    cv2.floodFill(flood, ff_mask, (0, 0), 255)
    filled = cv2.bitwise_or(keep, cv2.bitwise_not(flood))
    soft = cv2.GaussianBlur(filled, (0, 0), 5).astype(np.float32) / 255.0

    near_kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (29, 29))
    near = cv2.dilate((dist > (float(tolerance) * 0.65)).astype(np.uint8) * 255, near_kernel, iterations=1)
    near = cv2.GaussianBlur(near, (0, 0), 8).astype(np.float32) / 255.0
    lift = np.minimum(soft, near) * float(preserve_opacity)

    arr = np.asarray(base.convert("RGBA")).copy()
    alpha = arr[:, :, 3].astype(np.float32) / 255.0
    alpha = np.maximum(alpha, lift)
    alpha[alpha < (2.0 / 255.0)] = 0.0

    # Preserve original light pixels in lifted regions to avoid harsh unpremul artifacts.
    original = np.asarray(image.convert("RGB"))
    very_light = (np.mean(original, axis=2) > 224) & (lift > 0.12)
    arr[:, :, :3][very_light] = original[very_light]
    arr[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


def remove_background(image: Image.Image, mode: str, spec: dict[str, Any] | None = None) -> Image.Image:
    mode = (mode or "none").lower().replace("_", "-")
    spec = spec or {}
    if mode in {"none", "false", "0"}:
        return image.convert("RGBA")
    if mode in {"white", "white-to-alpha", "auto"}:
        return background_to_alpha(
            image,
            (255, 255, 255),
            tolerance=float(spec.get("tolerance", 8)),
            feather=float(spec.get("feather", 45)),
            strength=float(spec.get("strength", 1.0)),
            min_alpha=int(spec.get("min_alpha", 2)),
        )
    if mode in {"white-preserve", "preserve-light", "subject"}:
        return preserve_light_foreground_to_alpha(
            image,
            tolerance=float(spec.get("tolerance", 10)),
            preserve_opacity=float(spec.get("preserve_opacity", 0.72)),
            min_area_ratio=float(spec.get("min_area_ratio", 0.00025)),
        )
    if mode == "corner":
        bg = corner_color(image, int(spec.get("sample", 12)))
        return background_to_alpha(
            image,
            bg,
            tolerance=float(spec.get("tolerance", 8)),
            feather=float(spec.get("feather", 45)),
            strength=float(spec.get("strength", 1.0)),
        )
    if mode == "color":
        bg = parse_color(spec.get("color", "#ffffff"))
        return background_to_alpha(
            image,
            bg,
            tolerance=float(spec.get("tolerance", 8)),
            feather=float(spec.get("feather", 45)),
            strength=float(spec.get("strength", 1.0)),
        )
    fail(f"unknown remove_background mode: {mode}")
    return image


def resolve_path(base: Path, value: str | None) -> Path | None:
    if not value:
        return None
    path = Path(value).expanduser()
    if not path.is_absolute():
        path = base / path
    return path


def parse_canvas(text: str | None) -> tuple[int, int] | None:
    if not text:
        return None
    match = re.fullmatch(r"\s*(\d+)\s*[xX,]\s*(\d+)\s*", text)
    if not match:
        fail(f"canvas must look like WIDTHxHEIGHT, got {text!r}")
    return int(match.group(1)), int(match.group(2))


def common_font_path() -> str | None:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Helvetica.ttf",
        "/System/Library/Fonts/PingFang.ttc",
        "/Library/Fonts/Arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ]
    for item in candidates:
        if Path(item).exists():
            return item
    return None


def load_font(font_path: Path | None, size: int) -> ImageFont.ImageFont:
    if font_path and font_path.exists():
        return ImageFont.truetype(str(font_path), size)
    fallback = common_font_path()
    if fallback:
        return ImageFont.truetype(fallback, size)
    return ImageFont.load_default()


def text_width(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.ImageFont) -> int:
    box = draw.textbbox((0, 0), text, font=font)
    return box[2] - box[0]


def wrap_text(text: str, font: ImageFont.ImageFont, max_width: int | None) -> list[str]:
    if not max_width:
        return text.splitlines() or [text]
    probe = Image.new("RGBA", (8, 8))
    draw = ImageDraw.Draw(probe)
    lines: list[str] = []
    for raw_line in text.splitlines() or [text]:
        words = raw_line.split(" ")
        current = ""
        for word in words:
            trial = word if not current else f"{current} {word}"
            if text_width(draw, trial, font) <= max_width:
                current = trial
                continue
            if current:
                lines.append(current)
            if text_width(draw, word, font) <= max_width:
                current = word
            else:
                chunk = ""
                for char in word:
                    trial_chunk = chunk + char
                    if text_width(draw, trial_chunk, font) <= max_width or not chunk:
                        chunk = trial_chunk
                    else:
                        lines.append(chunk)
                        chunk = char
                current = chunk
        lines.append(current)
    return lines


def render_text_layer(spec: dict[str, Any], canvas: tuple[int, int], base_dir: Path) -> Image.Image:
    text = str(spec.get("text", ""))
    font_size = int(spec.get("font_size", spec.get("size", 48)))
    font = load_font(resolve_path(base_dir, spec.get("font_path") or spec.get("font")), font_size)
    fill = parse_color(spec.get("color", "#000000"), (0, 0, 0))
    alpha = int(max(0.0, min(1.0, float(spec.get("opacity", 1.0)))) * 255)
    x = int(spec.get("x", 0))
    y = int(spec.get("y", 0))
    max_width = spec.get("max_width")
    max_width_int = int(max_width) if max_width else None
    line_spacing = float(spec.get("line_spacing", 1.18))
    align = str(spec.get("align", "left")).lower()

    out = Image.new("RGBA", canvas, (0, 0, 0, 0))
    draw = ImageDraw.Draw(out)
    lines = wrap_text(text, font, max_width_int)
    line_height = max(1, round(font_size * line_spacing))
    for idx, line in enumerate(lines):
        tx = x
        if max_width_int and align in {"center", "right"}:
            width = text_width(draw, line, font)
            if align == "center":
                tx = x + (max_width_int - width) // 2
            elif align == "right":
                tx = x + max_width_int - width
        draw.text((tx, y + idx * line_height), line, font=font, fill=(*fill, alpha))
    return out


def load_layer_from_spec(spec: dict[str, Any], canvas: tuple[int, int], base_dir: Path) -> Layer | None:
    if spec.get("visible", True) is False:
        return None
    layer_type = str(spec.get("type", "image")).lower()
    name = str(spec.get("name") or spec.get("file") or spec.get("path") or layer_type)
    opacity = float(spec.get("opacity", 1.0))
    blend_mode = str(spec.get("blend_mode", spec.get("blend", "normal"))).lower()

    if layer_type == "text":
        image = render_text_layer(spec, canvas, base_dir)
        return Layer(name=name, image=apply_opacity(image, opacity), blend_mode=blend_mode, opacity=opacity)

    source = spec.get("file") or spec.get("path") or spec.get("src")
    path = resolve_path(base_dir, source)
    if not path:
        fail(f"image layer {name!r} is missing file/path/src")
    image = image_from_path(path)
    fit = str(spec.get("fit", "none"))
    image = fit_to_canvas(image, canvas, fit)
    remove_spec = spec.get("background") if isinstance(spec.get("background"), dict) else spec
    image = remove_background(image, str(spec.get("remove_background", spec.get("remove_bg", "none"))), remove_spec)
    x = int(spec.get("x", 0))
    y = int(spec.get("y", 0))
    image = place_on_canvas(image, canvas, x, y)
    image = apply_opacity(image, opacity)
    return Layer(name=name, image=image, blend_mode=blend_mode, opacity=opacity)


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int] | None:
    alpha = image.convert("RGBA").getchannel("A")
    return alpha.getbbox()


def composite_layers(layers_bottom_to_top: Sequence[Layer], background: tuple[int, int, int] = (255, 255, 255)) -> Image.Image:
    if not layers_bottom_to_top:
        fail("no layers to composite")
    canvas = layers_bottom_to_top[0].image.size
    comp = Image.new("RGBA", canvas, (*background, 255))
    for layer in layers_bottom_to_top:
        comp.alpha_composite(layer.image.convert("RGBA"))
    return comp.convert("RGB")


def pad_even(data: bytes) -> bytes:
    if len(data) % 2:
        return data + b"\x00"
    return data


def pad4(data: bytes) -> bytes:
    return data + (b"\x00" * ((4 - (len(data) % 4)) % 4))


def pascal_name(name: str) -> bytes:
    raw = name.encode("macroman", errors="replace")[:255]
    data = bytes([len(raw)]) + raw
    return pad4(data)


def layer_resource_block(key: bytes, payload: bytes) -> bytes:
    if len(key) != 4:
        fail("PSD layer resource key must be 4 bytes")
    return b"8BIM" + key + struct.pack(">I", len(payload)) + pad_even(payload)


def unicode_name_block(name: str) -> bytes:
    payload = struct.pack(">I", len(name)) + name.encode("utf-16be")
    return layer_resource_block(b"luni", payload)


def layer_extra_data(name: str) -> bytes:
    data = b""
    data += struct.pack(">I", 0)  # layer mask data length
    data += struct.pack(">I", 0)  # layer blending ranges length
    data += pascal_name(name)
    data += unicode_name_block(name)
    return data


def channel_bytes(arr: np.ndarray, channel_index: int) -> bytes:
    return arr[:, :, channel_index].astype(np.uint8).tobytes(order="C")


def write_psd(
    output_path: Path,
    layers_bottom_to_top: Sequence[Layer],
    composite_background: tuple[int, int, int] = (255, 255, 255),
) -> dict[str, Any]:
    if not layers_bottom_to_top:
        fail("cannot write PSD without layers")
    width, height = layers_bottom_to_top[0].image.size
    for layer in layers_bottom_to_top:
        if layer.image.size != (width, height):
            fail(f"layer {layer.name!r} has size {layer.image.size}, expected {(width, height)}")

    records: list[bytes] = []
    channel_data_blocks: list[bytes] = []
    layer_summaries: list[dict[str, Any]] = []
    layers_top_to_bottom = list(reversed(layers_bottom_to_top))

    for layer in layers_top_to_bottom:
        image = layer.image.convert("RGBA")
        bbox = alpha_bbox(image)
        if not bbox:
            print(f"warning: skipping empty layer {layer.name!r}", file=sys.stderr)
            continue
        left, top, right, bottom = bbox
        cropped = image.crop((left, top, right, bottom))
        arr = np.asarray(cropped.convert("RGBA"), dtype=np.uint8)
        layer_w = right - left
        layer_h = bottom - top

        channels = [
            (0, channel_bytes(arr, 0)),
            (1, channel_bytes(arr, 1)),
            (2, channel_bytes(arr, 2)),
            (-1, channel_bytes(arr, 3)),
        ]
        channel_info = b""
        data_block = b""
        for channel_id, data in channels:
            channel_info += struct.pack(">hI", channel_id, 2 + len(data))
            data_block += struct.pack(">H", 0) + data  # raw channel data

        blend_key = BLEND_KEYS.get(layer.blend_mode, b"norm")
        extra = layer_extra_data(layer.name)
        record = b""
        record += struct.pack(">iiii", top, left, bottom, right)
        record += struct.pack(">H", len(channels))
        record += channel_info
        record += b"8BIM" + blend_key
        record += bytes([255, 0, 0, 0])  # opacity, clipping, flags, filler
        record += struct.pack(">I", len(extra)) + extra
        records.append(record)
        channel_data_blocks.append(data_block)
        layer_summaries.append(
            {
                "name": layer.name,
                "top": top,
                "left": left,
                "width": layer_w,
                "height": layer_h,
                "blend_mode": layer.blend_mode,
            }
        )

    if not records:
        fail("all layers were empty")

    layer_info = struct.pack(">h", len(records)) + b"".join(records) + b"".join(channel_data_blocks)
    layer_info = pad_even(layer_info)
    layer_info_block = struct.pack(">I", len(layer_info)) + layer_info
    global_layer_mask_block = struct.pack(">I", 0)
    layer_mask_payload = layer_info_block + global_layer_mask_block
    layer_and_mask = struct.pack(">I", len(layer_mask_payload)) + layer_mask_payload

    composite = composite_layers(layers_bottom_to_top, composite_background)
    comp_arr = np.asarray(composite, dtype=np.uint8)
    composite_data = (
        struct.pack(">H", 0)
        + comp_arr[:, :, 0].tobytes(order="C")
        + comp_arr[:, :, 1].tobytes(order="C")
        + comp_arr[:, :, 2].tobytes(order="C")
    )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("wb") as handle:
        handle.write(b"8BPS")
        handle.write(struct.pack(">H", 1))
        handle.write(b"\x00" * 6)
        handle.write(struct.pack(">HIIHH", 3, height, width, 8, 3))
        handle.write(struct.pack(">I", 0))  # color mode data length
        handle.write(struct.pack(">I", 0))  # image resources length
        handle.write(layer_and_mask)
        handle.write(composite_data)

    layer_summaries.reverse()  # return bottom-to-top order for humans
    return {
        "output": str(output_path),
        "width": width,
        "height": height,
        "layers": layer_summaries,
        "layer_count": len(layer_summaries),
        "bytes": output_path.stat().st_size,
    }


def save_layer_pngs(layers: Sequence[Layer], directory: Path) -> list[str]:
    directory.mkdir(parents=True, exist_ok=True)
    written: list[str] = []
    for idx, layer in enumerate(layers, start=1):
        safe = re.sub(r"[^A-Za-z0-9_.-]+", "_", layer.name).strip("_") or f"layer_{idx}"
        path = directory / f"{idx:02d}_{safe}.png"
        layer.image.save(path)
        written.append(str(path))
    return written


def zip_files(paths: Iterable[str], zip_path: Path) -> None:
    zip_path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as archive:
        for item in paths:
            path = Path(item)
            archive.write(path, arcname=path.name)


def resolve_output(path: str | None, fallback: Path, base_dir: Path) -> Path:
    if not path:
        return fallback
    candidate = Path(path).expanduser()
    if not candidate.is_absolute():
        candidate = base_dir / candidate
    return candidate


def layers_from_manifest(manifest_path: Path) -> tuple[list[Layer], dict[str, Any], Path]:
    with manifest_path.open("r", encoding="utf-8") as handle:
        manifest = json.load(handle)
    base_dir = manifest_path.parent
    layer_specs = manifest.get("layers")
    if not isinstance(layer_specs, list) or not layer_specs:
        fail("manifest must contain a non-empty layers array")

    canvas_spec = manifest.get("canvas", {})
    canvas: tuple[int, int] | None = None
    if isinstance(canvas_spec, dict) and canvas_spec.get("width") and canvas_spec.get("height"):
        canvas = (int(canvas_spec["width"]), int(canvas_spec["height"]))
    elif isinstance(canvas_spec, str):
        canvas = parse_canvas(canvas_spec)

    if canvas is None:
        first = layer_specs[0]
        if not isinstance(first, dict):
            fail("layer specs must be objects")
        first_path = resolve_path(base_dir, first.get("file") or first.get("path") or first.get("src"))
        if not first_path:
            fail("canvas is missing and first layer has no image size to infer from")
        first_image = image_from_path(first_path)
        canvas = first_image.size

    layers: list[Layer] = []
    for raw_spec in layer_specs:
        if not isinstance(raw_spec, dict):
            fail("each manifest layer must be an object")
        layer = load_layer_from_spec(raw_spec, canvas, base_dir)
        if layer is not None:
            layers.append(layer)
    if not layers:
        fail("manifest produced no visible layers")
    return layers, manifest, base_dir


def command_assemble(args: argparse.Namespace) -> None:
    if args.manifest:
        manifest_path = Path(args.manifest).expanduser().resolve()
        layers, manifest, base_dir = layers_from_manifest(manifest_path)
        canvas_spec = manifest.get("canvas", {})
        composite_bg = parse_color(
            canvas_spec.get("composite_background") if isinstance(canvas_spec, dict) else None,
            parse_color(args.composite_background),
        )
        output = resolve_output(args.output or manifest.get("output"), manifest_path.with_suffix(".psd"), base_dir)
        preview = resolve_output(args.preview or manifest.get("preview"), output.with_suffix(".preview.png"), base_dir)
        save_layers_dir_value = args.save_layers or manifest.get("save_layers_dir")
        save_layers_dir = resolve_output(save_layers_dir_value, output.with_suffix("").parent / "layers", base_dir) if save_layers_dir_value else None
        zip_layers_path = resolve_output(args.zip_layers or manifest.get("zip_layers"), output.with_suffix(".layers.zip"), base_dir) if (args.zip_layers or manifest.get("zip_layers")) else None
    else:
        if not args.images:
            fail("assemble needs either --manifest or image paths")
        paths = [Path(item).expanduser().resolve() for item in args.images]
        names = split_names(args.names)
        canvas = parse_canvas(args.canvas)
        if canvas is None:
            canvas = image_from_path(paths[0]).size
        layers = []
        for idx, path in enumerate(paths):
            spec = {
                "file": str(path),
                "name": names[idx] if idx < len(names) else path.stem,
                "fit": args.fit if idx == 0 and args.first_is_background else "none",
                "remove_background": "none" if idx == 0 and args.first_is_background else args.remove_background,
                "x": 0,
                "y": 0,
                "tolerance": args.tolerance,
                "feather": args.feather,
            }
            layer = load_layer_from_spec(spec, canvas, Path.cwd())
            if layer:
                layers.append(layer)
        composite_bg = parse_color(args.composite_background)
        output = Path(args.output).expanduser().resolve() if args.output else Path("image2psd-output.psd").resolve()
        preview = Path(args.preview).expanduser().resolve() if args.preview else output.with_suffix(".preview.png")
        save_layers_dir = Path(args.save_layers).expanduser().resolve() if args.save_layers else None
        zip_layers_path = Path(args.zip_layers).expanduser().resolve() if args.zip_layers else None

    summary = write_psd(output, layers, composite_bg)
    preview.parent.mkdir(parents=True, exist_ok=True)
    composite_layers(layers, composite_bg).save(preview)
    summary["preview"] = str(preview)

    if save_layers_dir:
        written = save_layer_pngs(layers, save_layers_dir)
        summary["layer_pngs"] = written
        if zip_layers_path:
            zip_files(written, zip_layers_path)
            summary["zip_layers"] = str(zip_layers_path)

    print(json.dumps(summary, ensure_ascii=False, indent=2))


def split_names(value: str | None) -> list[str]:
    if not value:
        return []
    if "|" in value:
        return [item.strip() for item in value.split("|")]
    return [item.strip() for item in value.split(",")]


def quantized_labels(rgb: Image.Image, colors: int) -> tuple[np.ndarray, list[tuple[int, int, int]]]:
    quantized = rgb.quantize(colors=max(2, int(colors)), method=Image.Quantize.MEDIANCUT)
    labels = np.asarray(quantized, dtype=np.uint8)
    palette_raw = quantized.getpalette() or []
    used = sorted(int(x) for x in np.unique(labels))
    palette: list[tuple[int, int, int]] = []
    for idx in used:
        offset = idx * 3
        palette.append(tuple(palette_raw[offset : offset + 3]))  # type: ignore[arg-type]
    remap = {old: new for new, old in enumerate(used)}
    remapped = np.vectorize(remap.get)(labels).astype(np.uint8)
    return remapped, palette


def kmeans_labels(rgb_arr: np.ndarray, colors: int, max_pixels: int = 180_000) -> tuple[np.ndarray, list[tuple[int, int, int]]]:
    h, w = rgb_arr.shape[:2]
    pixels = rgb_arr.reshape(-1, 3).astype(np.float32)
    sample = pixels
    if len(sample) > max_pixels:
        rng = np.random.default_rng(42)
        sample = pixels[rng.choice(len(pixels), size=max_pixels, replace=False)]
    try:
        from sklearn.cluster import MiniBatchKMeans  # type: ignore

        model = MiniBatchKMeans(n_clusters=int(colors), random_state=42, n_init=3, batch_size=4096)
        model.fit(sample)
        centers = np.clip(model.cluster_centers_, 0, 255).astype(np.uint8)
    except Exception:
        centers = numpy_kmeans(sample, int(colors), iterations=12)

    distances = np.sum((pixels[:, None, :] - centers[None, :, :].astype(np.float32)) ** 2, axis=2)
    labels = np.argmin(distances, axis=1).reshape(h, w).astype(np.uint8)
    palette = [tuple(int(v) for v in center) for center in centers]
    return labels, palette


def numpy_kmeans(sample: np.ndarray, colors: int, iterations: int = 10) -> np.ndarray:
    rng = np.random.default_rng(42)
    if len(sample) < colors:
        colors = len(sample)
    centers = sample[rng.choice(len(sample), size=colors, replace=False)].astype(np.float32)
    for _ in range(iterations):
        distances = np.sum((sample[:, None, :] - centers[None, :, :]) ** 2, axis=2)
        labels = np.argmin(distances, axis=1)
        for idx in range(colors):
            members = sample[labels == idx]
            if len(members):
                centers[idx] = members.mean(axis=0)
    return np.clip(centers, 0, 255).astype(np.uint8)


def should_ignore_color(color: tuple[int, int, int], ignore: tuple[int, int, int] | None, tolerance: float) -> bool:
    if ignore is None:
        return False
    return math.sqrt(sum((int(color[i]) - int(ignore[i])) ** 2 for i in range(3))) <= tolerance


def command_split_colors(args: argparse.Namespace) -> None:
    input_path = Path(args.input).expanduser().resolve()
    source = image_from_path(input_path)
    rgba = np.asarray(source.convert("RGBA"), dtype=np.uint8)
    rgb = source.convert("RGB")
    rgb_arr = np.asarray(rgb, dtype=np.uint8)

    if args.method == "kmeans":
        labels, palette = kmeans_labels(rgb_arr, args.num_colors, args.max_pixels)
    elif args.method == "quantize":
        labels, palette = quantized_labels(rgb, args.num_colors)
    else:
        # Quantize is fast and dependency-free; kmeans can improve gradients.
        labels, palette = quantized_labels(rgb, args.num_colors)

    ignore = parse_color(args.ignore_color) if args.ignore_color else None
    layers: list[Layer] = []
    alpha_source = rgba[:, :, 3]
    for idx, color in enumerate(palette):
        if should_ignore_color(color, ignore, float(args.ignore_tolerance)):
            continue
        mask = labels == idx
        if not np.any(mask):
            continue
        layer_arr = np.zeros_like(rgba)
        layer_arr[:, :, :3][mask] = rgba[:, :, :3][mask]
        layer_arr[:, :, 3][mask] = alpha_source[mask]
        layers.append(Layer(name=f"Color {idx + 1} {color_hex(color)}", image=Image.fromarray(layer_arr, "RGBA")))

    if not layers:
        fail("color split produced no layers")
    if args.order == "small-on-top":
        layers.sort(key=lambda item: np.count_nonzero(np.asarray(item.image.getchannel("A"))), reverse=True)
    elif args.order == "large-on-top":
        layers.sort(key=lambda item: np.count_nonzero(np.asarray(item.image.getchannel("A"))))

    output = Path(args.output).expanduser().resolve()
    composite_bg = parse_color(args.composite_background)
    summary = write_psd(output, layers, composite_bg)
    if args.preview:
        preview = Path(args.preview).expanduser().resolve()
    else:
        preview = output.with_suffix(".preview.png")
    preview.parent.mkdir(parents=True, exist_ok=True)
    composite_layers(layers, composite_bg).save(preview)
    summary["preview"] = str(preview)
    if args.save_layers:
        written = save_layer_pngs(layers, Path(args.save_layers).expanduser().resolve())
        summary["layer_pngs"] = written
    print(json.dumps(summary, ensure_ascii=False, indent=2))


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Create layered PSD files from images, manifests, text specs, or color clusters.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    sub = parser.add_subparsers(dest="command", required=True)

    assemble = sub.add_parser("assemble", help="assemble image/text elements into a layered PSD")
    assemble.add_argument("images", nargs="*", help="image files, used when --manifest is omitted")
    assemble.add_argument("-m", "--manifest", help="JSON manifest describing canvas and layers")
    assemble.add_argument("-o", "--output", help="output PSD path")
    assemble.add_argument("--preview", help="flattened preview PNG path")
    assemble.add_argument("--save-layers", help="directory to write full-canvas transparent PNG layers")
    assemble.add_argument("--zip-layers", help="zip file for saved PNG layers")
    assemble.add_argument("--canvas", help="canvas size for positional image input, e.g. 1200x1600")
    assemble.add_argument("--names", help="comma-separated or pipe-separated layer names for positional input")
    assemble.add_argument("--first-is-background", action="store_true", help="treat the first positional image as opaque background")
    assemble.add_argument("--fit", default="cover", choices=["none", "contain", "cover", "stretch"], help="fit mode for first background image")
    assemble.add_argument("--remove-background", default="white", choices=["none", "white", "corner", "color", "white-preserve"], help="background removal mode for positional foreground images")
    assemble.add_argument("--tolerance", type=float, default=8.0, help="background removal tolerance")
    assemble.add_argument("--feather", type=float, default=45.0, help="background alpha feather")
    assemble.add_argument("--composite-background", default="#ffffff", help="background color used only for flattened preview/composite")
    assemble.set_defaults(func=command_assemble)

    split = sub.add_parser("split-colors", help="split one flat image into color-cluster raster layers")
    split.add_argument("input", help="input image")
    split.add_argument("-o", "--output", required=True, help="output PSD path")
    split.add_argument("--num-colors", type=int, default=8, help="number of color layers")
    split.add_argument("--method", choices=["auto", "quantize", "kmeans"], default="auto", help="color clustering method")
    split.add_argument("--max-pixels", type=int, default=180_000, help="sample size for kmeans")
    split.add_argument("--ignore-color", help="skip a background-like cluster, e.g. white or #ffffff")
    split.add_argument("--ignore-tolerance", type=float, default=22.0, help="RGB distance tolerance for --ignore-color")
    split.add_argument("--order", choices=["as-found", "small-on-top", "large-on-top"], default="small-on-top", help="layer stacking heuristic")
    split.add_argument("--preview", help="flattened preview PNG path")
    split.add_argument("--save-layers", help="directory to write full-canvas transparent PNG layers")
    split.add_argument("--composite-background", default="#ffffff", help="background color used only for flattened preview/composite")
    split.set_defaults(func=command_split_colors)
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    args.func(args)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
