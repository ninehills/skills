# PPT-TTS-Script

将 PPT/PPTX/PDF 演示文档转换为拟人化逐字稿。

## 安装

将 `ppt-tts-script/` 目录复制到以下任一位置：

- **全局**：`~/.pi/agent/skills/ppt-tts-script/`
- **项目级**：`<project>/.pi/skills/ppt-tts-script/`

### 依赖

| 依赖 | 用途 | 安装 (macOS) |
|------|------|-------------|
| python-pptx | 提取 PPT 文字/备注 | `pip install python-pptx` |
| LibreOffice | PPTX → PDF | `brew install --cask libreoffice` |
| poppler | PDF → PNG | `brew install poppler` |
| Gemini CLI（可选） | 高质量逐字稿生成 | `npm install -g @anthropic-ai/gemini-cli` |

## 使用

### 基本用法

```
转换 data/my-presentation.pptx 为逐字稿，时长在 30 分钟左右，输出目录为 output 目录。
```

### 指定语速

```
将 slides/quarterly-report.pptx 转换为逐字稿，语速慢速，输出到 output 目录。
```

| 语速 | 字/分钟 | 适用场景 |
|------|---------|----------|
| 快速 | 200 | 技术分享、产品演示 |
| 中等 | 180 | 商业汇报、培训 |
| 慢速 | 150 | 学术报告、正式演讲 |

### 指定时长

```
把 data/product-launch.pptx 生成演讲稿，控制在 20 分钟以内，输出到 output 目录。
```

时长通过调整每页讲解深度来控制，而非简单截断。

### PDF 输入

```
将 docs/report.pdf 转换为逐字稿，输出到 output 目录。
```

PDF 输入时跳过文字提取和备注回写步骤（无 PPTX 可操作）。

## 输出

```
output/
├── 00_input/              # 原始文件副本
├── 01_materials/          # 每页 PNG + Markdown
├── 02_outline/            # 大纲 outline.json
├── 03_manuscript/         # 分节逐字稿
└── 04_final/
    ├── output.md          # Markdown 格式逐字稿
    ├── output.json        # JSON 格式逐字稿
    └── presentation.pptx  # 带备注的 PPTX（仅 PPTX 输入）
```

- **output.md**：人类可读，适合直接朗读
- **output.json**：结构化数据，适合 TTS 引擎/字幕生成
- **presentation.pptx**：逐字稿写入演讲者备注（`<manuscript>` 标签包裹，不覆盖原有备注）
