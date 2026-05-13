---
name: bggg-creator-image2ppt
description: >
  把图片、截图、海报、PPT 页面截图、HTML 或 SVG 设计稿转换成可编辑 PPTX 的 Codex skill。
  当用户需要 image2ppt、image2pptx、图片转 PPT、截图转 PPT、PNG/JPEG 转可编辑幻灯片、
  HTML/SVG 转 PPTX、把设计图拆成图片组件和文本框再拼成 PowerPoint、
  或希望复用 Codex 内置生图能力把平面图重建为多元素可编辑 PPT 时，应该使用此 skill。
  在 Codex 中处理二进制图片时，默认结合 imagegen skill：先用 Codex 视觉理解版式，
  再用内置图片生成/编辑能力生成或清理多个组件图片，最后用本 skill 的脚本拼装 PPTX。
---

# BGGG Creator Image2PPT

用这个 skill 把平面视觉稿转换成可编辑的 `.pptx`。核心设计是“先形成结构化 manifest，再由脚本生成 PPTX”。

- 二进制图片输入：Codex 负责识别版式、文字和组件；默认调用 `imagegen` skill 生成或清理背景、装饰、产品、图表等组件图片；文本尽量还原为 PowerPoint 文本框。
- HTML/SVG 输入：优先把代码结构解析为原生 PPT 元素；复杂节点再降级为图片组件。
- 所有中间图片、manifest、诊断和输出都必须放在本 skill 的 `projects/YYYYMMDD_slug/` 目录里。

## Codex 默认策略

在 Codex 中处理 PNG/JPEG/WebP 等二进制图片时，默认执行这个顺序：

1. 使用 Codex 视觉能力观察源图，列出幻灯片尺寸、背景、标题、正文、图标、照片、图表、装饰、阴影、遮罩等对象。
2. 对于可编辑文字，直接写入 manifest 的 `text` 元素，不要把文字只做成图片。
3. 对于背景、照片、图标、复杂插画、图表、纹理、遮挡后需要补全的背景，使用 `imagegen` skill 生成、清理或重建独立组件图片。
4. 把 `imagegen` 生成的图片复制到当前项目目录的 `imagegen_assets/` 或 `component_images/`，不要让项目依赖 `$CODEX_HOME` 的临时输出。
5. 写 `manifest.json`，用 `scripts/image2pptx.py build` 生成 PPTX。
6. 验证 PPTX 能被 `python-pptx` 重新打开，并记录图层/文本/图片数量和已知限制。

如果用户明确要求“不重绘”“保持原图像素”，可以用原图裁切或全画布透明 PNG 作为组件图片；但默认仍要用 Codex imagegen 能力辅助背景清理、缺失区域补全和组件干净化。

## 项目目录约定

每次转换都创建独立项目目录：

```text
bggg-creator-image2ppt/
└── projects/
    └── YYYYMMDD_slug/
        ├── original_inputs/
        ├── component_images/
        ├── imagegen_assets/
        ├── diagnostics/
        ├── manifest.json
        ├── output.pptx
        ├── summary.json
        └── process_notes.md
```

初始化：

```bash
python3 bggg-creator-image2ppt/scripts/init_project.py pitch_deck \
  --source /path/to/reference.png \
  --date 20260504
```

## 二进制图片转 PPTX 工作流

1. 初始化项目，把源图复制到 `original_inputs/`。
2. 识别页面结构：
   - 画布比例和大致尺寸。
   - 背景是纯色、渐变、照片还是复杂插画。
   - 每段文字的内容、位置、字号、颜色、粗细、对齐方式。
   - 组件图片的边界、层级和是否需要透明背景。
3. 默认用 `imagegen` 生成或编辑组件：
   - 背景：完整画布、无文字、无前景组件。
   - 照片/产品/人物/图标/复杂装饰：干净边缘，必要时透明背景。
   - 图表：能原生重建就用形状和文本；复杂图表可先做成图片组件。
4. 把组件图片放入 `component_images/` 或 `imagegen_assets/`。
5. 写 manifest，元素按从底到顶排序。
6. 运行：

   ```bash
   python3 bggg-creator-image2ppt/scripts/image2pptx.py build \
     --manifest bggg-creator-image2ppt/projects/YYYYMMDD_slug/manifest.json \
     --output bggg-creator-image2ppt/projects/YYYYMMDD_slug/output.pptx \
     --summary bggg-creator-image2ppt/projects/YYYYMMDD_slug/summary.json
   ```

7. 写 `process_notes.md`，说明是否使用 imagegen、哪些对象是可编辑文本、哪些对象是图片 fallback。

## HTML/SVG 转 PPTX 工作流

HTML/SVG 是代码形式的 PPT 时，优先走解析器：

```bash
python3 bggg-creator-image2ppt/scripts/html_svg_to_manifest.py input.svg \
  --output bggg-creator-image2ppt/projects/YYYYMMDD_slug/manifest.json

python3 bggg-creator-image2ppt/scripts/html_svg_to_manifest.py input.html \
  --output bggg-creator-image2ppt/projects/YYYYMMDD_slug/manifest.json
```

解析原则：

- 原生还原 `text`、`rect`、`ellipse`、`line`、简单图片。
- HTML 里带绝对定位的 `.slide`/`body` 元素最容易被准确转换。
- SVG 的复杂 `path`、滤镜、渐变、mask、clip-path、foreignObject 可写入 diagnostics，并用 imagegen 或外部渲染结果作为图片组件 fallback。
- 解析脚本生成 manifest 后，仍用 `image2pptx.py build` 生成最终 PPTX。

## Manifest 格式

manifest 是跨来源的中间层。坐标默认以源画布像素为单位，脚本会映射到 PowerPoint 尺寸。

```json
{
  "deck": {
    "canvas_width": 1600,
    "canvas_height": 900,
    "slide_width_in": 13.333,
    "name": "Example Deck"
  },
  "slides": [
    {
      "name": "Cover",
      "elements": [
        {
          "kind": "background",
          "fill": "#f7f4ec"
        },
        {
          "kind": "image",
          "name": "Hero Product",
          "file": "component_images/product.png",
          "x": 910,
          "y": 170,
          "w": 520,
          "h": 520,
          "fit": "contain"
        },
        {
          "kind": "text",
          "name": "Title",
          "text": "Walk With Intention",
          "x": 120,
          "y": 180,
          "w": 680,
          "h": 150,
          "font_size_px": 68,
          "font_family": "Arial",
          "bold": true,
          "color": "#17120d",
          "align": "left"
        }
      ]
    }
  ]
}
```

常用元素：

- `background`: `fill` 纯色或 `file` 背景图。
- `image`: `file`/`path`/`src`、`x`、`y`、`w`、`h`、`fit`。`fit` 支持 `stretch`、`contain`、`cover`。
- `text`: `text`、`x`、`y`、`w`、`h`、`font_size_px` 或 `font_size_pt`、`font_family`、`color`、`bold`、`italic`、`align`。
- `shape`: `shape` 为 `rect`、`roundRect`、`ellipse` 或 `line`；支持 `fill`、`stroke`、`stroke_width_px`。
- `table`: 简单表格 fallback，用 `rows` 数组生成原生 PPT 表格。

## 可编辑性优先级

1. 文本框：标题、正文、页码、标签、按钮文字都优先转成原生 PPT 文本。
2. 基础形状：矩形、圆角矩形、圆、线条优先转成 PPT 形状。
3. 图片组件：照片、插画、图标、纹理、复杂图表作为独立图片层。
4. 整页背景 fallback：无法拆干净时可保留一张底图，再把关键文字和组件覆盖为可编辑对象，并明确说明限制。

## 输出要求

交付时至少说明：

- 项目目录路径。
- PPTX 路径。
- manifest 和 summary 路径。
- 可编辑文本框数量、图片组件数量、形状数量。
- 哪些组件由 Codex imagegen 生成或清理。
- 哪些复杂对象降级为图片 fallback。
