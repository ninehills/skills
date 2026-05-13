# bggg-creator-image2ppt

中文 | [English](./README_EN.md)

`bggg-creator-image2ppt` 是一个 Codex skill，用来把图片、截图、HTML 或 SVG 设计稿转换成可编辑的 PowerPoint `.pptx` 文件。它适合 PPT 截图还原、信息图转幻灯片、AI 生图转可编辑页面、HTML/SVG 代码式页面转 PPTX，以及“把平面图片拆成组件图片 + 文本框 + 原生形状再重建”的工作流。

这个 skill 的核心是结构化 `manifest.json` 中间层和纯 Python PPTX 生成器。二进制图片输入时，它在 Codex 中默认配合 `imagegen`：先用 Codex 视觉理解页面，再用内置生图/编辑能力生成或清理背景、图标、照片、装饰等组件，最后由脚本拼成真正的 PPTX。

## 能做什么

- 把 PNG/JPEG/WebP 等二进制图片重建为可编辑 PPTX。
- 把标题、正文、标签、公式等文字还原为 PowerPoint 文本框。
- 把矩形、圆角矩形、圆、线条、箭头等还原为原生 PowerPoint 形状。
- 把照片、图标、复杂插画、纹理、复杂图表作为独立图片组件。
- 把 HTML/SVG 中的简单文本、图片和基础图形解析成 manifest，再生成 PPTX。
- 每次任务自动使用 `projects/YYYYMMDD_slug/` 保存源文件、组件图、imagegen 资产、manifest、PPTX、摘要和诊断文件。

## 安装

把本目录复制到 Codex skills 目录：

```bash
mkdir -p ~/.codex/skills
cp -R bggg-creator-image2ppt ~/.codex/skills/
```

也可以克隆整个 `bggg-skills` 仓库后复制或软链接：

```bash
git clone https://github.com/binggandata/bggg-skills.git
mkdir -p ~/.codex/skills
ln -s "$PWD/bggg-skills/bggg-creator-image2ppt" ~/.codex/skills/bggg-creator-image2ppt
```

安装运行依赖：

```bash
python3 -m pip install -r ~/.codex/skills/bggg-creator-image2ppt/scripts/requirements.txt
```

主要依赖是 `python-pptx`、`Pillow`、`beautifulsoup4` 和 `lxml`。

## 在 Codex 中使用

可以直接这样对 Codex 说：

```text
使用 bggg-creator-image2ppt 把这张 PPT 图片转成真正的 pptx 文件。
文本要尽量变成可编辑文本框，形状尽量用 PPT 原生形状，
复杂图标和背景可以用 imagegen 生成组件后拼进去。
```

HTML/SVG 输入：

```text
使用 bggg-creator-image2ppt 把这个 HTML/SVG 页面转成可编辑 PPTX。
优先把文本、矩形、圆形和线条转换为原生 PPT 元素。
```

## 命令行用法

初始化一个项目目录：

```bash
python3 bggg-creator-image2ppt/scripts/init_project.py cross_border_formula \
  --source input.png
```

从 manifest 生成 PPTX：

```bash
python3 bggg-creator-image2ppt/scripts/image2pptx.py build \
  --manifest bggg-creator-image2ppt/projects/20260505_cross_border_formula/manifest.json \
  --output bggg-creator-image2ppt/projects/20260505_cross_border_formula/output.pptx \
  --summary bggg-creator-image2ppt/projects/20260505_cross_border_formula/summary.json
```

把 SVG 或 HTML 转成 manifest：

```bash
python3 bggg-creator-image2ppt/scripts/html_svg_to_manifest.py input.svg \
  --output bggg-creator-image2ppt/projects/20260505_demo/manifest.json

python3 bggg-creator-image2ppt/scripts/html_svg_to_manifest.py input.html \
  --output bggg-creator-image2ppt/projects/20260505_demo/manifest.json
```

## Manifest 示例

元素顺序是从底到顶。

```json
{
  "deck": {
    "name": "Example Deck",
    "canvas_width": 1600,
    "canvas_height": 900,
    "slide_width_in": 13.333
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
          "kind": "shape",
          "shape": "roundRect",
          "x": 120,
          "y": 160,
          "w": 620,
          "h": 240,
          "fill": "#ffffff",
          "stroke": "#ffffff"
        },
        {
          "kind": "text",
          "text": "Image to Editable PPTX",
          "x": 160,
          "y": 210,
          "w": 760,
          "h": 110,
          "font_size_px": 64,
          "font_family": "PingFang SC",
          "bold": true,
          "color": "#17120d"
        },
        {
          "kind": "image",
          "file": "component_images/icon.png",
          "x": 1120,
          "y": 260,
          "w": 180,
          "h": 180,
          "fit": "contain"
        }
      ]
    }
  ]
}
```

## 项目输出结构

每次真实任务都会放在：

```text
bggg-creator-image2ppt/projects/YYYYMMDD_slug/
├── original_inputs/
├── component_images/
├── imagegen_assets/
├── diagnostics/
├── exports/
├── manifest.json
├── output.pptx
├── summary.json
└── process_notes.md
```

运行产物默认被 Git 忽略。开源仓库只保留 `projects/.gitkeep`。

## 注意事项

- 从单张图片还原可编辑 PPT 是近似重建，不是无损反编译。
- 二进制图片输入时，Codex 负责识别版式和调用 `imagegen` 生成/清理组件；脚本本身不直接调用模型。
- HTML/SVG 解析器覆盖常见绝对定位页面和基础 SVG 节点；复杂 path、滤镜、渐变、mask、clip-path 等建议作为图片组件 fallback。
- `python-pptx` 不能直接渲染预览；可以用 PowerPoint、Keynote、LibreOffice 或 macOS Quick Look 检查结果。

## License

MIT
