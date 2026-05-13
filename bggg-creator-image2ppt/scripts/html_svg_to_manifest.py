#!/usr/bin/env python3
"""Convert simple HTML/SVG slide-like files into a bggg-creator-image2ppt manifest."""

from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from html import unescape
from pathlib import Path
from typing import Any, Sequence

from bs4 import BeautifulSoup


DEFAULT_CANVAS_WIDTH = 1600
DEFAULT_CANVAS_HEIGHT = 900
CSS_NAMED_COLORS = {
    "black",
    "white",
    "red",
    "green",
    "blue",
    "yellow",
    "gray",
    "grey",
    "silver",
    "navy",
    "orange",
    "purple",
}
COLOR_TOKEN_RE = re.compile(r"(#[0-9a-fA-F]{3,8}|rgba?\([^)]+\)|\b[a-zA-Z]+\b)")


def strip_namespace(tag: str) -> str:
    return tag.rsplit("}", 1)[-1].lower()


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)


def parse_style(value: str | None) -> dict[str, str]:
    if not value:
        return {}
    result: dict[str, str] = {}
    for chunk in value.split(";"):
        if ":" not in chunk:
            continue
        key, raw = chunk.split(":", 1)
        result[key.strip().lower()] = raw.strip()
    return result


def merge_svg_style(attrs: dict[str, str]) -> dict[str, str]:
    style = parse_style(attrs.get("style"))
    for key in [
        "fill",
        "stroke",
        "stroke-width",
        "font-size",
        "font-family",
        "font-weight",
        "font-style",
        "text-anchor",
        "opacity",
    ]:
        if attrs.get(key) is not None:
            style[key] = attrs[key]
    return style


def number(value: Any, default: float = 0.0) -> float:
    if value is None:
        return default
    text = str(value).strip()
    if not text:
        return default
    if text.endswith("%"):
        return default
    match = re.match(r"[-+]?\d*\.?\d+", text)
    if not match:
        return default
    return float(match.group(0))


def css_length(style: dict[str, str], key: str, default: float = 0.0) -> float:
    return number(style.get(key), default)


def css_color(value: str | None, default: str | None = None) -> str | None:
    if not value:
        return default
    text = value.strip()
    lowered = text.lower()
    if lowered in {"none", "transparent"}:
        return None
    if lowered in CSS_NAMED_COLORS or lowered.startswith("rgb") or text.startswith("#"):
        return text
    match = COLOR_TOKEN_RE.search(text)
    if match:
        token = match.group(1)
        if token.lower() in CSS_NAMED_COLORS or token.lower().startswith("rgb") or token.startswith("#"):
            return token
    return default


def asset_reference(ref: str | None, input_dir: Path) -> str | None:
    if not ref:
        return None
    if ref.startswith(("data:", "http://", "https://", "#")):
        return ref
    path = Path(ref).expanduser()
    if not path.is_absolute():
        path = input_dir / path
    return str(path.resolve())


def infer_svg_canvas(root: ET.Element) -> tuple[float, float]:
    width = number(root.attrib.get("width"), 0)
    height = number(root.attrib.get("height"), 0)
    viewbox = root.attrib.get("viewBox") or root.attrib.get("viewbox")
    if (width <= 0 or height <= 0) and viewbox:
        parts = [number(part) for part in re.split(r"[\s,]+", viewbox.strip()) if part]
        if len(parts) == 4:
            width = width or parts[2]
            height = height or parts[3]
    return width or DEFAULT_CANVAS_WIDTH, height or DEFAULT_CANVAS_HEIGHT


def svg_text_content(element: ET.Element) -> str:
    pieces: list[str] = []
    if element.text:
        pieces.append(element.text)
    for child in element:
        if child.text:
            pieces.append(child.text)
        if child.tail:
            pieces.append(child.tail)
    return unescape("".join(pieces).strip())


def svg_href(attrs: dict[str, str]) -> str | None:
    return attrs.get("href") or attrs.get("{http://www.w3.org/1999/xlink}href") or attrs.get("xlink:href")


def convert_svg(path: Path, *, slide_name: str | None = None) -> dict[str, Any]:
    tree = ET.parse(path)
    root = tree.getroot()
    canvas_width, canvas_height = infer_svg_canvas(root)
    elements: list[dict[str, Any]] = []
    diagnostics: list[str] = []

    for index, node in enumerate(root.iter()):
        tag = strip_namespace(node.tag)
        if tag == "svg":
            continue
        attrs = dict(node.attrib)
        style = merge_svg_style(attrs)
        if attrs.get("transform"):
            diagnostics.append(f"element #{index} <{tag}> has transform and may need manual adjustment")

        base = {"z": index, "name": attrs.get("id") or f"{tag}_{index}"}
        if tag == "rect":
            rx = number(attrs.get("rx"), 0)
            elements.append(
                {
                    **base,
                    "kind": "shape",
                    "shape": "roundRect" if rx > 0 else "rect",
                    "x": number(attrs.get("x")),
                    "y": number(attrs.get("y")),
                    "w": number(attrs.get("width")),
                    "h": number(attrs.get("height")),
                    "fill": css_color(style.get("fill")),
                    "stroke": css_color(style.get("stroke")),
                    "stroke_width_px": number(style.get("stroke-width"), 1),
                }
            )
        elif tag == "circle":
            radius = number(attrs.get("r"))
            elements.append(
                {
                    **base,
                    "kind": "shape",
                    "shape": "ellipse",
                    "x": number(attrs.get("cx")) - radius,
                    "y": number(attrs.get("cy")) - radius,
                    "w": radius * 2,
                    "h": radius * 2,
                    "fill": css_color(style.get("fill")),
                    "stroke": css_color(style.get("stroke")),
                    "stroke_width_px": number(style.get("stroke-width"), 1),
                }
            )
        elif tag == "ellipse":
            rx = number(attrs.get("rx"))
            ry = number(attrs.get("ry"))
            elements.append(
                {
                    **base,
                    "kind": "shape",
                    "shape": "ellipse",
                    "x": number(attrs.get("cx")) - rx,
                    "y": number(attrs.get("cy")) - ry,
                    "w": rx * 2,
                    "h": ry * 2,
                    "fill": css_color(style.get("fill")),
                    "stroke": css_color(style.get("stroke")),
                    "stroke_width_px": number(style.get("stroke-width"), 1),
                }
            )
        elif tag == "line":
            elements.append(
                {
                    **base,
                    "kind": "shape",
                    "shape": "line",
                    "x1": number(attrs.get("x1")),
                    "y1": number(attrs.get("y1")),
                    "x2": number(attrs.get("x2")),
                    "y2": number(attrs.get("y2")),
                    "stroke": css_color(style.get("stroke"), "#111111"),
                    "stroke_width_px": number(style.get("stroke-width"), 1),
                }
            )
        elif tag == "text":
            text = svg_text_content(node)
            if text:
                font_size = number(style.get("font-size"), 32)
                anchor = style.get("text-anchor", "start")
                x = number(attrs.get("x"))
                estimated_width = max(font_size * 0.62 * len(text), font_size * 2)
                if anchor == "middle":
                    x -= estimated_width / 2
                    align = "center"
                elif anchor == "end":
                    x -= estimated_width
                    align = "right"
                else:
                    align = "left"
                elements.append(
                    {
                        **base,
                        "kind": "text",
                        "text": text,
                        "x": x,
                        "y": number(attrs.get("y")) - font_size,
                        "w": estimated_width,
                        "h": font_size * 1.35,
                        "font_size_px": font_size,
                        "font_family": style.get("font-family", "Arial").strip("\"'"),
                        "bold": style.get("font-weight") in {"bold", "700", "800", "900"},
                        "italic": style.get("font-style") == "italic",
                        "color": css_color(style.get("fill"), "#111111"),
                        "align": align,
                    }
                )
        elif tag == "image":
            href = asset_reference(svg_href(attrs), path.parent)
            if href:
                elements.append(
                    {
                        **base,
                        "kind": "image",
                        "file": href,
                        "x": number(attrs.get("x")),
                        "y": number(attrs.get("y")),
                        "w": number(attrs.get("width")),
                        "h": number(attrs.get("height")),
                        "fit": "stretch",
                    }
                )
        elif tag in {"path", "polygon", "polyline", "g", "defs", "lineargradient", "radialgradient", "filter", "mask", "clippath"}:
            if tag not in {"g", "defs"}:
                diagnostics.append(f"unsupported SVG <{tag}> at element #{index}; use imagegen/raster fallback if visually important")

    return {
        "deck": {
            "name": slide_name or path.stem,
            "canvas_width": canvas_width,
            "canvas_height": canvas_height,
            "slide_width_in": 13.333,
        },
        "slides": [{"name": slide_name or path.stem, "elements": elements}],
        "diagnostics": diagnostics,
    }


def find_slide_root(soup: BeautifulSoup) -> Any:
    return soup.select_one(".slide") or soup.select_one("[data-slide]") or soup.body or soup


def html_canvas(root: Any) -> tuple[float, float]:
    style = parse_style(root.get("style") if hasattr(root, "get") else None)
    width = css_length(style, "width", 0)
    height = css_length(style, "height", 0)
    return width or DEFAULT_CANVAS_WIDTH, height or DEFAULT_CANVAS_HEIGHT


def text_of_tag(tag: Any) -> str:
    own_text = tag.get_text(" ", strip=True)
    return re.sub(r"\s+", " ", own_text)


def looks_like_text_node(tag: Any, text: str) -> bool:
    if not text:
        return False
    if tag.name in {"script", "style", "svg"}:
        return False
    return tag.name in {"p", "span", "h1", "h2", "h3", "h4", "h5", "h6", "button", "a", "label", "li"} or bool(
        parse_style(tag.get("style")).get("font-size")
    )


def convert_html(path: Path, *, slide_name: str | None = None) -> dict[str, Any]:
    soup = BeautifulSoup(path.read_text(encoding="utf-8"), "html.parser")
    root = find_slide_root(soup)
    canvas_width, canvas_height = html_canvas(root)
    elements: list[dict[str, Any]] = []
    diagnostics: list[str] = []

    for index, tag in enumerate(root.find_all(True)):
        if tag.name in {"script", "style", "meta", "link"}:
            continue
        style = parse_style(tag.get("style"))
        position = style.get("position")
        left = css_length(style, "left", None) if position == "absolute" else css_length(style, "left", 0)
        top = css_length(style, "top", None) if position == "absolute" else css_length(style, "top", 0)
        width = css_length(style, "width", 0)
        height = css_length(style, "height", 0)
        z_index = int(number(style.get("z-index"), index))
        base = {"z": z_index, "name": tag.get("id") or " ".join(tag.get("class", [])) or f"{tag.name}_{index}"}

        if tag.name == "img":
            src = asset_reference(tag.get("src"), path.parent)
            if not src:
                continue
            elements.append(
                {
                    **base,
                    "kind": "image",
                    "file": src,
                    "x": left or number(tag.get("x"), 0),
                    "y": top or number(tag.get("y"), 0),
                    "w": width or number(tag.get("width"), 0),
                    "h": height or number(tag.get("height"), 0),
                    "fit": style.get("object-fit", "stretch"),
                }
            )
            continue

        background = css_color(style.get("background-color") or style.get("background"))
        border_color = None
        border_match = re.search(r"(#[0-9a-fA-F]{3,6}|rgb\([^)]+\))", style.get("border", ""))
        if border_match:
            border_color = border_match.group(1)
        text = text_of_tag(tag)

        if background and not text and width and height:
            radius = css_length(style, "border-radius", 0)
            elements.append(
                {
                    **base,
                    "kind": "shape",
                    "shape": "roundRect" if radius > 0 else "rect",
                    "x": left or 0,
                    "y": top or 0,
                    "w": width,
                    "h": height,
                    "fill": background,
                    "stroke": border_color,
                }
            )
            continue

        if looks_like_text_node(tag, text):
            font_size = css_length(style, "font-size", 28)
            inferred_width = width or min(canvas_width - (left or 0), max(font_size * 0.6 * len(text), font_size * 4))
            inferred_height = height or font_size * 1.4 * max(1, text.count(" ") // 8 + 1)
            elements.append(
                {
                    **base,
                    "kind": "text",
                    "text": text,
                    "x": left or 0,
                    "y": top or 0,
                    "w": inferred_width,
                    "h": inferred_height,
                    "font_size_px": font_size,
                    "font_family": style.get("font-family", "Arial").split(",")[0].strip("\"' "),
                    "bold": style.get("font-weight") in {"bold", "700", "800", "900"},
                    "italic": style.get("font-style") == "italic",
                    "color": css_color(style.get("color"), "#111111"),
                    "align": style.get("text-align", "left"),
                }
            )
        elif style and position != "absolute":
            diagnostics.append(f"HTML element <{tag.name}> #{index} is not absolutely positioned; layout may need manual review")

    return {
        "deck": {
            "name": slide_name or path.stem,
            "canvas_width": canvas_width,
            "canvas_height": canvas_height,
            "slide_width_in": 13.333,
        },
        "slides": [{"name": slide_name or path.stem, "elements": elements}],
        "diagnostics": diagnostics,
    }


def convert(path: Path, *, slide_name: str | None = None) -> dict[str, Any]:
    suffix = path.suffix.lower()
    if suffix == ".svg":
        return convert_svg(path, slide_name=slide_name)
    if suffix in {".html", ".htm"}:
        return convert_html(path, slide_name=slide_name)
    raise ValueError(f"unsupported input type: {path.suffix}")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Convert simple HTML/SVG files to a bggg-creator-image2ppt manifest.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument("input", help="input .html/.htm/.svg file")
    parser.add_argument("--output", "-o", help="manifest JSON path; defaults to stdout")
    parser.add_argument("--slide-name", help="slide name override")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    input_path = Path(args.input).expanduser().resolve()
    if not input_path.exists():
        raise SystemExit(f"input not found: {input_path}")
    manifest = convert(input_path, slide_name=args.slide_name)
    if args.output:
        write_json(Path(args.output).expanduser().resolve(), manifest)
    else:
        json.dump(manifest, sys.stdout, ensure_ascii=False, indent=2)
        sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
