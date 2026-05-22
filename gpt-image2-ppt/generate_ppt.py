#!/usr/bin/env python3
"""
GPT Image 2 PPT Generator

使用 GPT Vision Image 2 模型生成高质量 PPT 幻灯片图片，
支持多种设计风格，自动生成 HTML 播放器。

环境变量（兼容 OpenAI 标准格式）：
- OPENAI_BASE_URL: API 地址（官方或中转均可）
- OPENAI_API_KEY: API 密钥
"""

import argparse
import base64
import json
import os
import re
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional

import requests
from dotenv import load_dotenv


# =============================================================================
# Constants
# =============================================================================

DEFAULT_SIZE = "1536x864"  # 16:9 landscape
MODEL = "gpt-vision-image-2"
OUTPUT_BASE_DIR = "outputs"
TEMPLATE_PATH = "templates/viewer.html"


# =============================================================================
# Environment
# =============================================================================

def find_and_load_env() -> bool:
    """
    Find and load .env file from multiple locations.

    Search priority:
    1. Current script directory
    2. Parent directories up to project root
    3. video_workflow project .env
    """
    search_paths = [
        Path(__file__).parent / ".env",
        Path.home() / ".claude" / "skills" / "gpt-image2-ppt-skills" / ".env",
    ]

    for env_path in search_paths:
        if env_path.exists():
            load_dotenv(env_path, override=True)
            print(f"Loaded environment from: {env_path}")
            return True

    load_dotenv(override=True)
    print("Warning: No .env file found, using system environment variables")
    return False


# =============================================================================
# Style Template
# =============================================================================

def load_style_template(style_path: str) -> Dict[str, str]:
    """
    Load and parse style template file.

    Returns dict with keys: base_prompt, cover_template, content_template, data_template
    """
    with open(style_path, "r", encoding="utf-8") as f:
        content = f.read()

    result = {
        "base_prompt": "",
        "cover_template": "",
        "content_template": "",
        "data_template": "",
    }

    # Extract base prompt
    base_match = re.search(
        r"## 基础提示词模板\s*\n(.*?)(?=\n## (?:页面类型|使用示例))",
        content, re.DOTALL
    )
    if base_match:
        result["base_prompt"] = base_match.group(1).strip()

    # Extract page type templates
    cover_match = re.search(
        r"### 封面页模板\s*\n(.*?)(?=\n### |$)",
        content, re.DOTALL
    )
    if cover_match:
        result["cover_template"] = cover_match.group(1).strip()

    content_match = re.search(
        r"### 内容页模板\s*\n(.*?)(?=\n### |$)",
        content, re.DOTALL
    )
    if content_match:
        result["content_template"] = content_match.group(1).strip()

    data_match = re.search(
        r"### 数据页模板\s*\n(.*?)(?=\n### |$)",
        content, re.DOTALL
    )
    if data_match:
        result["data_template"] = data_match.group(1).strip()

    # Fallback: use full content
    if not result["base_prompt"]:
        result["base_prompt"] = content

    return result


# =============================================================================
# Prompt Generation
# =============================================================================

def generate_prompt(
    style: Dict[str, str],
    page_type: str,
    content_text: str,
    slide_number: int,
    total_slides: int,
) -> str:
    """Generate prompt for a single slide."""
    base = style["base_prompt"]

    # Determine page type
    is_cover = page_type == "cover" or slide_number == 1
    is_data = page_type == "data"
    is_summary = page_type == "summary" or slide_number == total_slides

    # Build the prompt
    prompt_parts = [base, "\n\n"]

    # Add design constraints
    prompt_parts.append(
        "重要渲染约束：这是一张16:9横屏的演示文稿幻灯片图片。"
        "所有文字必须清晰可读、排版美观。"
        "画面要有足够的留白空间。"
        "整体设计感要像专业设计师制作的keynote一样精美。\n\n"
    )

    if is_cover:
        template = style.get("cover_template", "")
        if template:
            prompt_parts.append(f"{template}\n\n")
        prompt_parts.append(
            f"请生成封面页。内容如下：\n\n{content_text}\n\n"
            "这是演示文稿的第一页，需要有强烈的视觉冲击力。"
        )
    elif is_data or is_summary:
        template = style.get("data_template", "")
        if template:
            prompt_parts.append(f"{template}\n\n")
        prompt_parts.append(
            f"请生成数据/总结页。内容如下：\n\n{content_text}\n\n"
            "需要用数据可视化或总结形式呈现信息。"
        )
    else:
        template = style.get("content_template", "")
        if template:
            prompt_parts.append(f"{template}\n\n")
        prompt_parts.append(
            f"请生成内容页。内容如下：\n\n{content_text}\n\n"
            "需要清晰展示要点，保持美观的排版。"
        )

    return "".join(prompt_parts)


# =============================================================================
# Image Generation (GPT Vision Image 2)
# =============================================================================

class GptImage2Generator:
    """GPT Vision Image 2 PPT slide generator."""

    def __init__(self):
        self.base_url = os.getenv("OPENAI_BASE_URL")
        self.api_key = os.getenv("OPENAI_API_KEY")

        if not self.base_url or not self.api_key:
            raise ValueError(
                "请设置环境变量 OPENAI_BASE_URL 和 OPENAI_API_KEY\n"
                "可以在 .env 文件中设置，支持官方 API 或任何 OpenAI 兼容的中转服务"
            )

        self.base_url = self.base_url.rstrip("/")
        self.headers = {
            "Accept": "application/json",
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
        }

    def generate_slide(
        self,
        prompt: str,
        slide_number: int,
        output_dir: str,
        quality: str = "auto",
    ) -> Optional[str]:
        """Generate a single PPT slide image."""
        url = f"{self.base_url}/v1/images/generations"

        payload = {
            "model": MODEL,
            "prompt": prompt,
            "n": 1,
            "size": DEFAULT_SIZE,
        }
        if quality != "auto":
            payload["quality"] = quality

        print(f"\nGenerating slide {slide_number}...")
        print(f"  POST {url}")
        print(f"  Prompt: {prompt[:120]}...")

        start_time = time.time()

        try:
            resp = requests.post(
                url,
                json=payload,
                headers=self.headers,
                timeout=600,
            )

            if resp.status_code != 200:
                print(f"  ERROR: API returned {resp.status_code}: {resp.text[:200]}")
                return None

            data = resp.json()
            image_data = self._extract_image_data(data)

            # Save image
            image_path = os.path.join(output_dir, "images", f"slide-{slide_number:02d}.png")
            self._save_image(image_data, image_path)

            elapsed = time.time() - start_time
            print(f"  Slide {slide_number} saved: {image_path} ({elapsed:.1f}s)")
            return image_path

        except Exception as e:
            print(f"  ERROR generating slide {slide_number}: {e}")
            return None

    def _extract_image_data(self, data: dict) -> str:
        """Extract image data from API response."""
        # Standard images API format
        if "data" in data and isinstance(data["data"], list) and data["data"]:
            item = data["data"][0]
            if "b64_json" in item and item["b64_json"]:
                return f"data:image/png;base64,{item['b64_json']}"
            if "url" in item and item["url"]:
                return item["url"]

        # chat.completion compatible format
        if "choices" in data and data["choices"]:
            content = data["choices"][0].get("message", {}).get("content", "")
            if content:
                content = content.strip()
                if content.startswith("http"):
                    return content.split()[0]
                if content.startswith("data:image"):
                    return content
                if len(content) > 200:
                    try:
                        base64.b64decode(content[:100])
                        return f"data:image/png;base64,{content}"
                    except Exception:
                        pass
                urls = re.findall(r'(https?://[^\s\)]+)', content)
                if urls:
                    return urls[0]

        raise ValueError(f"Failed to extract image from API response: {str(data)[:300]}")

    def _save_image(self, image_url_or_data: str, output_path: str) -> str:
        """Save image from URL or base64 data."""
        os.makedirs(os.path.dirname(output_path), exist_ok=True)

        if image_url_or_data.startswith("data:image/"):
            base64_data = image_url_or_data.split(",", 1)[1]
            image_bytes = base64.b64decode(base64_data)
            with open(output_path, "wb") as f:
                f.write(image_bytes)
        elif image_url_or_data.startswith("http"):
            resp = requests.get(image_url_or_data, timeout=120)
            resp.raise_for_status()
            with open(output_path, "wb") as f:
                f.write(resp.content)
        else:
            raise ValueError(f"Unknown image format: {image_url_or_data[:100]}")

        return output_path


# =============================================================================
# Output Generation
# =============================================================================

def generate_viewer_html(output_dir: str, slide_count: int, template_path: str) -> str:
    """Generate HTML viewer for slides playback."""
    with open(template_path, "r", encoding="utf-8") as f:
        html_template = f.read()

    slides_list = [f"'images/slide-{i:02d}.png'" for i in range(1, slide_count + 1)]
    html_content = html_template.replace(
        "/* IMAGE_LIST_PLACEHOLDER */",
        ",\n            ".join(slides_list),
    )

    html_path = os.path.join(output_dir, "index.html")
    with open(html_path, "w", encoding="utf-8") as f:
        f.write(html_content)

    print(f"  Viewer HTML generated: {html_path}")
    return html_path


def save_prompts(output_dir: str, prompts_data: Dict[str, Any]) -> str:
    """Save all prompts to JSON file."""
    prompts_path = os.path.join(output_dir, "prompts.json")
    with open(prompts_path, "w", encoding="utf-8") as f:
        json.dump(prompts_data, f, ensure_ascii=False, indent=2)
    print(f"  Prompts saved: {prompts_path}")
    return prompts_path


# =============================================================================
# Main
# =============================================================================

def main() -> None:
    """Main entry point."""
    find_and_load_env()

    parser = argparse.ArgumentParser(
        description="GPT Image 2 PPT Generator",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("--plan", required=True, help="Path to slides plan JSON file")
    parser.add_argument("--style", required=True, help="Path to style template file")
    parser.add_argument("--output", help="Output directory (default: outputs/TIMESTAMP)")
    parser.add_argument("--quality", default="auto", choices=["auto", "high", "medium", "low"])
    parser.add_argument("--template", default=TEMPLATE_PATH, help="HTML template path")

    args = parser.parse_args()

    # Load slides plan
    with open(args.plan, "r", encoding="utf-8") as f:
        slides_plan = json.load(f)

    # Load style template
    style = load_style_template(args.style)

    # Create output directory
    if args.output:
        output_dir = args.output
    else:
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        output_dir = os.path.join(OUTPUT_BASE_DIR, timestamp)

    os.makedirs(os.path.join(output_dir, "images"), exist_ok=True)

    slides = slides_plan["slides"]
    total_slides = len(slides)

    print("=" * 60)
    print("GPT Image 2 PPT Generator Started")
    print("=" * 60)
    print(f"Style: {args.style}")
    print(f"Quality: {args.quality}")
    print(f"Slides: {total_slides}")
    print(f"Output: {output_dir}")
    print("=" * 60)

    # Initialize generator
    generator = GptImage2Generator()

    # Prompts record
    prompts_data: Dict[str, Any] = {
        "metadata": {
            "title": slides_plan.get("title", "Untitled"),
            "total_slides": total_slides,
            "style": args.style,
            "quality": args.quality,
            "model": MODEL,
            "size": DEFAULT_SIZE,
            "generated_at": datetime.now().isoformat(),
        },
        "slides": [],
    }

    success_count = 0

    for slide_info in slides:
        slide_number = slide_info["slide_number"]
        page_type = slide_info.get("page_type", "content")
        content_text = slide_info["content"]

        prompt = generate_prompt(style, page_type, content_text, slide_number, total_slides)
        image_path = generator.generate_slide(prompt, slide_number, output_dir, args.quality)

        if image_path:
            success_count += 1

        prompts_data["slides"].append({
            "slide_number": slide_number,
            "page_type": page_type,
            "content": content_text,
            "prompt": prompt,
            "image_path": image_path,
        })

    # Save prompts
    save_prompts(output_dir, prompts_data)

    # Generate viewer HTML
    script_dir = Path(__file__).parent
    template_full_path = script_dir / args.template
    if template_full_path.exists():
        generate_viewer_html(output_dir, total_slides, str(template_full_path))
    else:
        print(f"  Warning: Template not found at {template_full_path}, skipping viewer")

    print()
    print("=" * 60)
    print(f"Generation Complete! ({success_count}/{total_slides} slides)")
    print("=" * 60)
    print(f"Output directory: {output_dir}")
    open_cmd = "start" if sys.platform == "win32" else "open"
    print(f"Open viewer: {open_cmd} {os.path.join(output_dir, 'index.html')}")


if __name__ == "__main__":
    main()
