---
name: bggg-creator-image2psd
description: >
  把一张或多张图片整理成 PSD 图层文件的创作与转换 skill。当用户需要 image2psd、图片转 PSD、
  多张图片拼成 PSD、海报/设计稿拆成多个图层、白底转透明、颜色聚类拆层、把 Codex/AI 生图结果拆成元素图再合成 PSD、
  或希望输出 layered PSD、可在 Photoshop/Photopea 中编辑的分层栅格文件时，应该使用此 skill。
  在 Codex 中使用时，默认结合 imagegen skill：先用 Codex 视觉/生图能力理解、补齐或重建元素，再用本 skill 的脚本落地 PSD。
---

# BGGG Creator Image2PSD

用这个 skill 把图片资产组织成可编辑的 PSD 栅格图层。核心脚本是 `scripts/image2psd.py`，它不依赖 Photoshop 或 ImageMagick，能独立写出带图层名、透明通道和合成预览的 PSD。

## Codex 适配默认策略

在 Codex 里处理图片转 PSD 时，默认配合 `imagegen` skill 的能力：

- 先用 Codex 的视觉理解能力判断图层结构、主体/背景/文字/装饰的边界、以及哪些区域需要重建背景。
- 如果用户要求从描述生成、补齐缺失元素、移除对象后补背景、或拆出更干净的独立商品/主体图，优先使用 `imagegen` skill 的内置生图/编辑能力生成项目内资产，再交给本 skill 组装 PSD。
- 如果用户明确要求“不改变相对位置”“直接在 Photoshop 拼接”，优先保留原图像素：每个图层输出为同尺寸全画布透明 PNG，只改变 alpha，不重绘内容。只有背景擦除/补洞可使用 inpaint 或 imagegen 辅助。
- 不要把 imagegen 的默认输出留在 `$CODEX_HOME`。项目要用的图片必须复制到本 skill 的项目目录。

## 项目目录约定

每次处理图片都创建独立项目目录，放在本 skill 文件夹下：

```text
bggg-creator-image2psd/
└── projects/
    └── YYYYMMDD_slug/
        ├── original_reference.png
        ├── manifest.json
        ├── layer_sources/
        ├── psd_full_canvas_layers/
        ├── output.psd
        ├── output.preview.png
        ├── psd_full_canvas_layers.zip
        └── process_notes.md
```

命名规则：

- `YYYYMMDD_slug` 使用当前日期和简短英文/拼音任务名，例如 `20260503_lifestyle_product`.
- `original_reference.png` 是本次处理的源图副本。
- `layer_sources/` 保存用于组装的透明图层源。
- `psd_full_canvas_layers/` 保存 Photoshop 可直接按原位叠放的全画布 PNG 图层。
- `process_notes.md` 记录图层划分、是否使用 imagegen、验证结果和已知限制。

## 工作流

1. 判断任务类型：
   - 多张元素图已经存在：复制到项目目录，用 `assemble`，按图层顺序写 manifest。
   - 只有一张平面图且要保留相对位置：做语义/区域拆层，每层保持原图同尺寸；背景层用 inpaint 或 imagegen 辅助清理被拆出的区域。
   - 只有一张平面图且只要粗拆：用 `split-colors` 按颜色聚类拆层，并说明它不是语义对象拆层。
   - 用户要从描述生成 PSD：先用 Codex imagegen 分别生成背景、主体、文字/装饰等项目资产，再用 `assemble` 合成 PSD。
2. 创建 `projects/YYYYMMDD_slug/`，把源图和所有输出放进去。默认用脚本初始化：

   ```bash
   python3 bggg-creator-image2psd/scripts/init_project.py lifestyle_product \
     --source 用户附件.png \
     --date 20260503
   ```

   如果使用了 imagegen，先把 `$CODEX_HOME` 下的生成结果复制到该项目的 `imagegen_assets/`，再进入拆层或合成流程。
3. 确定画布尺寸。优先沿用源图尺寸；海报类任务需要明确宽高。
4. 处理透明：
   - 背景层用 `remove_background: none`。
   - Logo、文字、装饰图常用 `remove_background: white`。
   - 白帆、白色产品、浅色主体这类容易被白底吞掉的元素用 `remove_background: white-preserve`。
   - 非白色纯底用 `corner` 或 `color`。
5. 运行脚本输出 PSD、预览 PNG、可选的单层 PNG 目录/zip。
6. 验证输出：检查脚本 JSON 摘要里的 `layer_count`、预览 PNG、PSD 文件大小；用 Pillow 或 Photoshop/Photopea 检查 PSD 可读性。
7. 写 `process_notes.md`，记录图层、路径、验证和限制。

## 常用命令

从 manifest 合成：

```bash
python3 bggg-creator-image2psd/scripts/image2psd.py assemble \
  --manifest work/manifest.json \
  --output work/output.psd \
  --preview work/output.preview.png \
  --save-layers work/layers \
  --zip-layers work/layers.zip
```

直接把多张图合成，第一张为背景：

```bash
python3 bggg-creator-image2psd/scripts/image2psd.py assemble bg.png title.png logo.png \
  --first-is-background \
  --names "Background,Title,Logo" \
  --output output.psd \
  --save-layers layers
```

把单张平面图按颜色拆成 PSD 图层：

```bash
python3 bggg-creator-image2psd/scripts/image2psd.py split-colors poster.png \
  --output poster-color-layers.psd \
  --num-colors 10 \
  --ignore-color white \
  --save-layers poster-color-layers
```

## Manifest 格式

用 manifest 管理复杂 PSD。图层数组按从底到顶排列。

```json
{
  "canvas": {
    "width": 1122,
    "height": 1402,
    "composite_background": "#ffffff"
  },
  "output": "poster.psd",
  "preview": "poster.preview.png",
  "save_layers_dir": "layers",
  "layers": [
    {
      "name": "Background",
      "file": "assets/background.png",
      "fit": "cover",
      "remove_background": "none"
    },
    {
      "name": "Ship and Waves",
      "file": "assets/ship.png",
      "remove_background": "white-preserve"
    },
    {
      "name": "Title",
      "type": "text",
      "text": "AI Commerce Summit",
      "x": 80,
      "y": 120,
      "font_size": 72,
      "color": "#41270d",
      "max_width": 900
    }
  ]
}
```

图层字段要点：

- `file`/`path`/`src`: 图片路径，manifest 相对路径从 manifest 所在目录解析。
- `type: "text"`: 用 PIL 渲染成独立栅格文字层，不是 Photoshop 可编辑文字对象。
- `x`, `y`: 图片或文字层左上角偏移。
- `fit`: `none`、`contain`、`cover`、`stretch`。
- `remove_background`: `none`、`white`、`white-preserve`、`corner`、`color`。
- `opacity`: 0 到 1。

## 生图到 PSD

在 Codex 中，默认把 imagegen 当成补强工具，而不是唯一处理方式。把设计拆成独立元素来生成或编辑，而不是只生成一张完整海报：

- 背景：完整画布，通常不去底。
- 主体/产品/人物/船/道具：白底或透明背景，合成时用 `white` 或 `white-preserve` 去底。
- Logo、标题、日程、装饰：尽量单独生成或用 manifest 的 text 层重建。
- 每个元素的提示词要包含画布尺寸、视角、边缘干净、不要阴影污染背景等约束。

如果生图只得到一张完整图，先用 `split-colors` 做可编辑性最低限度拆层，再按用户需求补生关键元素。

## 单图语义拆层经验

复盘成功案例后，单张图转 PSD 优先采用这个顺序：

1. 复制源图到项目目录，保持原始尺寸。
2. 先列出图层清单，按“背景/主体/装饰/文字/阴影或光效”分组。
3. 每个可移动对象输出全画布透明 PNG，位置不裁切，便于 Photoshop 直接叠放。
4. 背景层用被拆出图层的 union mask 做 inpaint，必要时二次扩大遮罩清理残影。
5. 预览图与原图做像素差异或肉眼对比；发现文字/主体边缘被裁时，优先扩大 mask 框而不是移动图层。
6. 如果源图没有本地文件，先要求用户提供源文件路径；不要用 imagegen 重绘图冒充原始拆层。

## 何时读参考

- 修改核心脚本前，读 `references/implementation-notes.md`。
- 需要追溯外部项目启发时，读 `references/source-projects.md`。
- 不要把 `reference/` 下克隆的外部仓库作为运行依赖；它们只是开发参考。

## 输出要求

交付时至少说明：

- PSD 路径。
- 预览 PNG 路径。
- 图层数量和主要图层名。
- 是否生成了单层 PNG/zip。
- 项目目录路径。
- 如果没有验证 Photoshop 打开效果，要明确说验证限于脚本和预览。
