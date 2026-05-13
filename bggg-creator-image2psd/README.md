# bggg-creator-image2psd

中文 | [English](./README_EN.md)

`bggg-creator-image2psd` 是一个 Codex skill，用来把一张或多张栅格图片整理成可编辑的分层 PSD。它适合海报拆层、产品场景拆图、AI 生图元素组装、白底转透明、颜色聚类拆层，以及“把图片拆成若干个 Photoshop 可移动图层”的工作流。

这个 skill 内置纯 Python PSD writer，不依赖 Photoshop、ImageMagick、Wand 或 `psd-tools`。

## 能做什么

- 把多张图片或栅格文字层组装成 PSD。
- 保留图层名和透明通道。
- 导出合成预览 PNG。
- 导出全画布透明 PNG 图层，方便 Photoshop 直接按 `(0, 0)` 叠放。
- 把单张平面图按颜色聚类拆成多个图层。
- 每次任务自动使用 `projects/YYYYMMDD_slug/` 保存源图、过程图、PSD、预览和诊断文件。
- 在 Codex 中默认配合 `imagegen` skill：需要生成、编辑、补齐、重建元素时，先产出项目内资产，再组装 PSD。

## 安装

把本目录复制到 Codex skills 目录：

```bash
mkdir -p ~/.codex/skills
cp -R bggg-creator-image2psd ~/.codex/skills/
```

也可以克隆整个 `bggg-skills` 仓库后复制或软链接：

```bash
git clone https://github.com/binggandata/bggg-skills.git
mkdir -p ~/.codex/skills
ln -s "$PWD/bggg-skills/bggg-creator-image2psd" ~/.codex/skills/bggg-creator-image2psd
```

安装运行依赖：

```bash
python3 -m pip install -r ~/.codex/skills/bggg-creator-image2psd/scripts/requirements.txt
```

必需依赖是 `Pillow` 和 `numpy`。`opencv-python` 用于更好的主体遮罩和浅色主体保护；`scikit-learn` 可增强 `split-colors --method kmeans`。

## 在 Codex 中使用

可以直接这样对 Codex 说：

```text
使用 bggg-creator-image2psd 把这张图转成 PSD。
保持元素相对位置不变，把主体、文字、背景拆成独立图层，
所有过程图片都放到 skill 的 projects 文件夹下。
```

如果需要先生成元素再组装：

```text
先用 imagegen 生成背景、主体、标题和装饰元素，
再用 bggg-creator-image2psd 把它们组装成 PSD。
```

## 命令行用法

初始化一个项目目录：

```bash
python3 bggg-creator-image2psd/scripts/init_project.py lifestyle_product \
  --source input.png
```

从 manifest 组装 PSD：

```bash
python3 bggg-creator-image2psd/scripts/image2psd.py assemble \
  --manifest bggg-creator-image2psd/projects/20260503_lifestyle_product/manifest.json
```

直接组装多张图片，第一张作为背景：

```bash
python3 bggg-creator-image2psd/scripts/image2psd.py assemble bg.png logo.png title.png \
  --first-is-background \
  --names "Background,Logo,Title" \
  --output output.psd \
  --save-layers layers
```

把单张平面图按颜色拆层：

```bash
python3 bggg-creator-image2psd/scripts/image2psd.py split-colors poster.png \
  --output poster-color-layers.psd \
  --num-colors 10 \
  --ignore-color white \
  --save-layers poster-color-layers
```

## Manifest 示例

图层顺序是从底到顶。

```json
{
  "canvas": {
    "width": 1200,
    "height": 1600,
    "composite_background": "#ffffff"
  },
  "output": "output.psd",
  "preview": "output.preview.png",
  "save_layers_dir": "psd_full_canvas_layers",
  "zip_layers": "psd_full_canvas_layers.zip",
  "layers": [
    {
      "name": "Background",
      "file": "layer_sources/background.png",
      "fit": "cover",
      "remove_background": "none"
    },
    {
      "name": "Subject",
      "file": "layer_sources/subject.png",
      "remove_background": "white-preserve"
    },
    {
      "name": "Title",
      "type": "text",
      "text": "Event Title",
      "x": 80,
      "y": 120,
      "font_size": 76,
      "color": "#1c1712",
      "max_width": 960
    }
  ]
}
```

## 去底模式

- `none`：保持原图。
- `white`：把白色背景转透明。
- `white-preserve`：白底转透明，同时保留白色/浅色主体结构。
- `corner`：采样四角作为背景色。
- `color`：使用图层配置里的指定背景色。

## 项目输出结构

每次真实任务都会放在：

```text
bggg-creator-image2psd/projects/YYYYMMDD_slug/
├── original_reference.png
├── manifest.json
├── layer_sources/
├── psd_full_canvas_layers/
├── imagegen_assets/
├── diagnostics/
├── output.psd
├── output.preview.png
├── psd_full_canvas_layers.zip
└── process_notes.md
```

运行产物默认被 Git 忽略。开源仓库只保留 `projects/.gitkeep`。

## 注意事项

- Manifest 创建的文字层是栅格图层，不是 Photoshop 可编辑文字对象。
- 从单张平面图做语义拆层时，结果一定是近似的；如果要高度可编辑，最好提供或生成独立元素图。
- 如果用户要求相对位置不变，优先输出全画布透明 PNG 图层，Photoshop 中每层放在 `(0, 0)` 即可对齐。

## License

MIT
