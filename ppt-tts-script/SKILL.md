---
name: ppt-tts-script
description: |
  将 PPT/PPTX/PDF 演示文档转换为拟人化逐字稿（Markdown + JSON 双格式输出），并将逐字稿回写到 PPTX 演讲者备注中。使用五阶段流水线：素材提取、大纲生成、逐字稿撰写（通过 Gemini CLI）、合并输出、备注回写。当用户需要将演示文档转为演讲稿、朗读稿、解说词时使用此技能。触发短语包括但不限于：「PPT 转语音」「生成逐字稿」「演讲稿生成」「帮我把 PPT 做成配音稿」「生成演示文档的解说词」「presentation to speech script」「PPT 转文字稿」「从 PPT 生成演讲词」。即使用户没有明确提到逐字稿，只要意图是从演示文档生成可朗读的文字稿，也应触发此技能。
---

# PPT 演示文档 → 拟人化逐字稿

将演示文档（PPT/PPTX/PDF）通过五阶段流水线转换为口语化逐字稿，同时输出 Markdown、JSON 和带备注的 PPTX 三种产物，适用于语音合成、演讲练习和内容二次加工。

## 第零步：依赖检查

在做任何事之前，先运行依赖检查脚本。这个脚本会验证所有必需工具是否已安装——缺少任何一个都会导致后续步骤失败，所以必须在开头就拦住。

```bash
python3 scripts/check_deps.py
```

如果脚本报告缺失依赖，将错误信息展示给用户并停止。不要尝试自动安装——让用户决定如何处理。

> **关于 Gemini CLI**：manuscript-generator 步骤优先使用 `gemini` 命令行工具生成高质量口语化内容。如果 Gemini CLI 未安装，第三步会自动检测当前模型是否具备图片输入能力：
> - **具备图片能力** → 用 subagent 直接读取幻灯片图片 + 文字生成逐字稿（质量接近 Gemini 方案）
> - **不具备图片能力** → 发出警告并中止执行，提示用户安装 Gemini CLI 或切换到支持视觉的模型

## 工作目录

让用户指定一个输出目录 `<output_dir>`（默认为当前目录下的 `output/`），按以下结构组织：

```
<output_dir>/
├── 00_input/           # 用户提供的原始演示文档
├── 01_materials/       # 提取的 PNG 图片 + Markdown 文字
├── 02_outline/         # 演讲大纲 outline.json
├── 03_manuscript/      # 分节逐字稿 + manuscript.json
└── 04_final/           # 最终合并输出
    ├── output.md       # Markdown 格式
    ├── output.json     # JSON 格式
    └── presentation.pptx  # 带逐字稿备注的 PPTX
```

将用户的演示文档复制到 `00_input/`。

## 流水线总览

```
[输入 PPT] → ① 素材提取 → ② 大纲生成 → ③ 逐字稿 → ④ 合并输出 → ⑤ 备注回写
                  ↓              ↓            ↓            ↓            ↓
              验证素材        验证大纲      验证稿件      验证终稿    验证 PPTX
```

每一步完成后都有验证环节。验证通过才进入下一步——这不是走形式，而是因为下游步骤严重依赖上游输出的正确性（比如缺失的 PAGE 标记会导致合并输出混乱）。

## 第一步：素材提取（material-splitter）

**目标**：将演示文档拆分为每页一张 PNG 图片和一个 Markdown 文字文件。

**执行**：

```bash
# 1. 提取文字和演讲者备注
python3 scripts/extract_ppt.py <pptx_path> <output_dir>/01_materials/

# 2. PPTX → PDF
soffice --headless --convert-to pdf <pptx_path> --outdir <output_dir>/01_materials/

# 3. PDF → PNG（每页一张）
pdftoppm -png -r 150 <output_dir>/01_materials/*.pdf <output_dir>/01_materials/slide
```

注意 `pdftoppm` 输出的文件名格式是 `slide-01.png`、`slide-02.png` 等。如果与 extract_ppt.py 的编号不一致（比如 `slide-1.png` vs `slide-01.png`），需要重命名以统一为两位数零填充格式。

完成后生成 `01_materials/index.json` 素材索引。详细格式见 `references/material-splitter.md`。

**验证**：

```bash
python3 scripts/validate_output.py <output_dir> --step 1
```

验证要点：
- PNG 文件数量 = MD 文件数量 = 演示文档总页数
- index.json 存在且 JSON 合法
- 每页都有对应的 PNG 和 MD 文件

## 第二步：大纲生成（outline-generator）

**目标**：分析素材内容，生成层次化的演讲大纲（节划分、时间线、风格提示）。

**执行**：读取所有 `slide-XX.md` 文件，分析内容结构，生成 `02_outline/outline.json`。

这一步由模型自身完成，不需要外部工具。阅读 `references/outline-generator.md` 获取详细的大纲结构规范和分析方法。

核心规则：
- 每节 3-15 页，边界通常在过渡页/章节页之后
- 根据内容类型选择风格提示（产品发布→热情自信，学术报告→严谨清晰，等等）
- 按 150-200 字/分钟估算时长

**验证**：

```bash
python3 scripts/validate_output.py <output_dir> --step 2
```

验证要点：
- outline.json 存在且 JSON 合法
- 所有页面都已被分配到某个节（无遗漏）
- 页码范围连续无重叠
- 每节都有 style_hint

## 第三步：逐字稿生成（manuscript-generator）

**目标**：按节生成口语化的逐字稿，每页以 `[PAGE:N]` 标记分隔。

**执行**：这是最核心也最耗时的步骤。用 Gemini CLI 为每个节生成逐字稿——Gemini 能同时理解幻灯片图片和文字内容，生成更贴合视觉内容的口语化表述。

读取 `references/manuscript-generator.md` 获取完整的 Gemini 调用模板和并发处理方法。

如果有多个节，可以通过 subagent 并发生成各节内容。每个 subagent：
1. 读取该节对应的 slide-XX.md 和 slide-XX.png
2. 调用 Gemini CLI 生成逐字稿
3. 保存为 `03_manuscript/section-XX.md`

全部完成后，生成 `03_manuscript/manuscript.json` 索引。

> **降级策略**：如果 Gemini CLI 不可用，先检测当前模型的图片输入能力（尝试用 read 工具读取一张 slide PNG 并描述内容）。如果模型可以理解图片，则用 subagent 直接读图 + 读文生成逐字稿；如果不能，则警告用户并中止。详见 `references/manuscript-generator.md` 中的「模型自身生成」章节。

**验证**：

```bash
python3 scripts/validate_output.py <output_dir> --step 3
```

验证要点：
- 每个节都有对应的 section-XX.md 文件
- 每个 section 文件中包含正确的 `[PAGE:N]` 标记
- 页码范围与 outline.json 一致
- manuscript.json 索引完整
- **不包含控制指令**：无（语速放慢）、（停顿）、[强调]等非朗读内容

## 第四步：合并输出（report-merger）

**目标**：将分节逐字稿合并为完整文档，同时输出 Markdown 和 JSON 两种格式。

**执行**：读取 `references/report-merger.md` 获取两种格式的完整规范。

两种输出格式各有用途：
- **Markdown** (`output.md`)：人类可读，适合直接朗读、打印和审阅
- **JSON** (`output.json`)：机器可读，适合 TTS 引擎、字幕生成、二次编辑等程序化消费

合并步骤：
1. 读取大纲获取结构
2. 逐节读取 `section-XX.md`，按 `[PAGE:N]` 标记解析出每页内容
3. 统计元信息（总页数、总字数、预计时长）
4. 分别写入 `04_final/output.md` 和 `04_final/output.json`

**验证**：

```bash
python3 scripts/validate_output.py <output_dir> --step 4
```

验证要点：
- output.md 和 output.json 均存在且非空
- 包含所有页面的内容
- JSON 结构合法且字段完整
- Markdown 和 JSON 的页面数、字数一致

## 第五步：备注回写（notes-injector）

**目标**：将每页逐字稿内容写入原始 PPTX 文件的演讲者备注，方便演讲者在放映时直接参考。

这一步只对 PPTX 输入有效。如果输入是 PDF，跳过此步。

**执行**：

```bash
python3 scripts/inject_notes.py \
  <output_dir>/00_input/<原始文件>.pptx \
  <output_dir>/04_final/output.json \
  <output_dir>/04_final/presentation.pptx
```

脚本的工作方式：
1. 用 `python-pptx` 打开原始 PPTX
2. 读取 `output.json` 获取每页的逐字稿内容
3. 对每个幻灯片，将逐字稿**追加**到现有演讲者备注末尾，格式为 `<manuscript>内容</manuscript>`
4. 如果该页已有 `<manuscript>` 块则跳过（幂等性）
5. 保存为新文件到 `04_final/presentation.pptx`

不修改原始文件。不覆盖已有备注——始终追加。

**验证**：

```bash
python3 scripts/validate_output.py <output_dir> --step 5
```

验证要点：
- `presentation.pptx` 存在且可打开
- 有逐字稿内容的页面，其备注中包含 `<manuscript>` 标记
- `<manuscript>` 块数量与 output.json 中的页面数一致
- 原有备注内容未被覆盖

## 用户交互点

在以下节点暂停，等待用户确认后再继续：

1. **依赖检查后**：报告环境状态
2. **大纲生成后**（第二步完成）：展示节结构给用户审核，这决定了后续所有步骤的组织方式
3. **最终输出后**：展示结果摘要（含 PPTX 备注回写状态）

用户可以在任意步骤要求调整。常见需求：
- 调整节的划分方式
- 修改语速（快速 200字/分、中等 180字/分、慢速 150字/分）
- 指定整体风格偏好
- 对特定页面的逐字稿提出修改意见

## 脚本说明

所有脚本路径相对于本 skill 目录：

| 脚本 | 用途 |
|------|------|
| `scripts/check_deps.py` | 检查所有依赖是否已安装 |
| `scripts/extract_ppt.py` | 从 PPTX 提取文字和演讲者备注 |
| `scripts/gemini-task.sh` | 带超时的 Gemini CLI 调用封装 |
| `scripts/validate_output.py` | 各步骤输出验证（步骤 1-5） |
| `scripts/inject_notes.py` | 将逐字稿注入 PPTX 演讲者备注 |

## 参考文档

详细的子代理指令和格式规范在 `references/` 目录：

| 文件 | 内容 | 何时读取 |
|------|------|----------|
| `references/material-splitter.md` | 素材提取详细流程和 index.json 格式 | 执行第一步时 |
| `references/outline-generator.md` | 大纲分析方法和 outline.json 完整 schema | 执行第二步时 |
| `references/manuscript-generator.md` | Gemini CLI 调用模板、并发策略、降级方案 | 执行第三步时 |
| `references/report-merger.md` | 最终输出的 Markdown 和 JSON 双格式规范 | 执行第四步时 |

第五步（备注回写）无需单独参考文档——`scripts/inject_notes.py` 脚本处理全部逻辑，SKILL.md 中的说明已足够。
