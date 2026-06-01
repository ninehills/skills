# PPT 生成与编辑工作流

本文档说明 `generate_ppt.py` 的完整逻辑——从生成、编辑、回滚到外部 PPTX 摄取。

---

## 一、CLI 入口分发

```text
generate_ppt.py
│
├── --list-sessions ──→ 扫描 outputs/ 下含 metadata.json 的目录，输出列表
│
├── --ingest-pptx ────→ 渲染外部 PPTX → 创建 session → 写入占位 metadata.json
│                        (Agent 后续 Read PNG 填 slide_spec)
│
├── --edit ───────────→ cmd_edit_slide()      ← 需 --session
│
├── --rollback ───────→ cmd_rollback_slide()  ← 需 --session + --to-version
│
└── (默认) ───────────→ 生成模式               ← 需 --plan + (--style | --template-pptx)
```

---

## 二、生成流程

### 2.1 内置风格 (`--plan` + `--style`)

```text
slides_plan.md                      styles/<id>.md
      │                                    │
      │  md_to_plan.py                     │
      ▼                                    │
slides_plan.json                           │
      │                                    │
      │  Agent 综合两者，为每页构造 slide_spec  │
      │  (type / content / position /       │
      │   style / color)                    │
      │  写入每页 .slide_spec 字段            │
      ▼                                    ▼
┌────────────────────────────────────────────────────┐
│                  generate_ppt.py                    │
│                                                    │
│  slide_spec 有 elements?                            │
│   ├── YES → generate_prompt_from_spec()             │
│   │        逐元素描述位置、内容、样式 → 精确 prompt    │
│   └── NO  → generate_prompt()                       │
│             自由格式 content → 基础 prompt            │
│                                                    │
│  并发派发 → gpt-image-2 出图                          │
│       │                                            │
│       ▼                                            │
│  outputs/<ts>/images/slide-NN.png                  │
│       │                                            │
│       ├── prompts.json      (兼容旧格式)              │
│       ├── metadata.json     (slide_spec 版本历史)     │
│       └── <title>.pptx      (16:9 打包)              │
└────────────────────────────────────────────────────┘
```

### 2.2 模板克隆 (`--template-pptx`)

```text
template.pptx
      │
      │  render_template.py (LibreOffice / Keynote / PowerPoint COM)
      ▼
template_renders/<stem>/page-NN.png
      │
      │  template_analyzer.py (vision 分析, 结果缓存)
      ▼
template_cache/<sha256>.json  (TemplateProfile: layouts + global_style)
      │
      │  match_layout() → coerce_fields() → render_prompt_from_template()
      ▼
  每页 prompt ──→ gpt-image-2
                  (可选 --template-strict → 模板页作 reference image)
```

### 2.3 增量生成 (`--slides` + `--output`)

当 `--output` 指向已有 session 时，不会覆盖已有 metadata，而是**合并**新页到现有 session：

```text
outputs/20240523_143052/        ← 已有 slide-01, slide-03
      │
      │  --output outputs/20240523_143052 --slides 2,4
      ▼
outputs/20240523_143052/        ← 新增 slide-02, slide-04
                                  metadata.json 合并
                                  prompts.json 重建
                                  .pptx 更新
```

---

## 三、编辑流程 (`--edit`)

```text
用户: "改第 3 页的副标题"
      │
      ▼
┌─ Agent ───────────────────────────────────────────────┐
│  1. 读 metadata.json                                  │
│     → slides["3"].versions[current_version]           │
│     → spec.elements.subtitle                          │
│       content = "健康管理的两类割裂"                     │
│       position = "标题下方"                             │
│                                                       │
│  2. 问用户新内容 → "医疗数据碎片化"                       │
│                                                       │
│  3. 构造 CLI 参数:                                      │
│     --element-updates                                 │
│       '{"subtitle":{"content":"医疗数据碎片化"}}'        │
│     --edit-instruction                                │
│       "将副标题从X改为Y"                                 │
└───────────────────────────────────────────────────────┘
      │
      ▼
┌─ cmd_edit_slide() ────────────────────────────────────┐
│                                                       │
│  1. 加载 metadata.json                                │
│  2. 获取当前 slide_spec (latest version)               │
│  3. apply_spec_updates() → updated_spec               │
│  4. construct_edit_prompt(old_spec, updates):          │
│     "在参考图基础上，只修改标题下方subheading的文字       │
│      从「健康管理的两类割裂」改为「医疗数据碎片化」         │
│      保持其他所有元素不变"                               │
│                                                       │
│  5. 备份原图: slide-03.png → slide-03_v0001.png       │
│  6. gpt-image-2 (edit_prompt + 原图 reference) → 新图  │
│  7. 新图 → slide-03.png (覆盖)                         │
│  8. 新图备份: slide-03.png → slide-03_v0002.png       │
│  9. metadata.json 追加新 version:                      │
│     { action:"edit", spec:<updated>,                   │
│       reference_version:2, edit_instruction:"..." }    │
│  10. 重建 .pptx                                       │
└───────────────────────────────────────────────────────┘
```

### 编辑 prompt 的两种方式

| 方式 | 参数 | 说明 |
|------|------|------|
| 自动构造 | `--element-updates '{"subtitle":{"content":"新内容"}}'` | 从 old spec + updates 自动生成精确的编辑 prompt |
| 手动提供 | `--edit-prompt "在参考图基础上..."` | Agent/用户直接写完整 prompt，跳过自动构造 |

两种方式可以同时用：`--element-updates` 更新 metadata 中的 spec，`--edit-prompt` 覆盖自动生成的 prompt。

---

## 四、回滚流程 (`--rollback`)

```text
--rollback 3 --to-version 1 --session <ts>
      │
      ▼
┌─ cmd_rollback_slide() ───────────────────────────────┐
│                                                       │
│  1. 加载 metadata.json                                │
│  2. 找到 version=1 的 spec 和 image_snapshot           │
│  3. 有 elements?                                      │
│     ├── YES → generate_prompt_from_spec(v1_spec)      │
│     └── NO  → 读 v1 的 prompt_file 原文                │
│               ├── 有 → 用原文                           │
│               └── 无 → 用 v1 参考图构造视觉提示          │
│  4. 以 v1 的图片作 reference (可选)                     │
│  5. gpt-image-2 → 新图                                 │
│  6. 备份当前图 → slide-03_v{old}.png                   │
│  7. 新图 → slide-03.png                               │
│  8. metadata.json 追加新 version:                      │
│     { action:"rollback", spec:<v1_spec>,               │
│       reference_version:1 }                           │
│  9. 重建 .pptx                                       │
└───────────────────────────────────────────────────────┘
```

**注意**：回滚不是直接恢复旧文件,而是用旧版本的 spec 重新生成一张新图，这样既保留了完整的版本历史，又确保了图片质量（旧图可能被压缩或损坏）。

---

## 五、摄取外部 PPTX (`--ingest-pptx`)

```text
外部 deck.pptx
      │
      │  --ingest-pptx
      ▼
┌─ cmd_ingest_pptx() ──────────────────────────────────┐
│                                                       │
│  1. render_template.py → template_renders/<stem>/     │
│  2. 创建 outputs/<ts>_<stem>/images/                  │
│  3. 复制 PNG: slide-NN_v0001.png + slide-NN.png       │
│  4. 写入 metadata.json (placeholder spec):            │
│     { elements: {}, layout: "(待 Agent 分析填充)" }    │
└───────────────────────────────────────────────────────┘
      │
      │  Agent 后续操作
      ▼
┌─ Agent ───────────────────────────────────────────────┐
│  Read 每页 slide-NN.png (多模态看懂内容)                 │
│                                                       │
│  对每页构建 slide_spec:                                 │
│  {                                                    │
│    "elements": {                                      │
│      "title": {"type":"heading", "content":"...",     │
│                "position":"左上角", "style":"32pt",    │
│                "color":"#ffffff"},                     │
│      "body": {...}                                    │
│    }                                                  │
│  }                                                    │
│                                                       │
│  更新 metadata.json → 之后走正常 --edit 流程             │
└───────────────────────────────────────────────────────┘
```

---

## 六、核心数据结构

### slide_spec

每页幻灯片的结构化描述，精确到每个可编辑元素：

```json
{
  "slide_number": 3,
  "page_type": "content",
  "layout": "两个卡片纵向排列",
  "elements": {
    "title": {
      "type": "heading",
      "content": "市场痛点",
      "style": "48pt Bold 思源黑体",
      "position": "左上角",
      "color": "#00ff88"
    },
    "subtitle": {
      "type": "subheading",
      "content": "健康管理的两类割裂",
      "style": "24pt Regular 思源黑体",
      "position": "标题下方",
      "color": "#ffffff"
    },
    "card_1": {
      "type": "card",
      "heading": "痛点一：高频无深度",
      "body": "用户日均使用健康 App 3.2 次…",
      "position": "中部偏左"
    }
  }
}
```

**元素字段说明：**

| 字段 | 说明 | 示例 |
|------|------|------|
| `type` | 元素语义类型 | `heading`, `subheading`, `card`, `metric`, `decoration` |
| `content` | 元素内容文字 | `"市场痛点"` |
| `position` | 页面位置 | `"左上角"`, `"标题下方"`, `"中部偏左"` |
| `style` | 字体/样式描述 | `"48pt Bold 思源黑体"` |
| `color` | 颜色 | `"#00ff88"` |
| `description` | 装饰类元素的描述 | `"深色渐变，左侧极光纹理"` |

**元素 ID 命名规范：**

| 语义 | ID | 说明 |
|------|-----|------|
| 主标题 | `title` | 每页唯一 |
| 副标题 | `subtitle` | 每页唯一 |
| 卡片 | `card_1`, `card_2`, … | 编号从 1 开始 |
| 数据指标 | `metric_1`, `metric_2`, … | 编号从 1 开始 |
| 背景装饰 | `background` | 装饰类，用 `description` 而非 `content` |

### metadata.json

版本化的 slide_spec 存储：

```json
{
  "version": 1,
  "title": "MediWise 商业计划书",
  "style": "styles/dark-aurora.md",
  "generated_at": "2024-05-23T14:30:52",
  "slide_order": [1, 2, 3],
  "slides": {
    "3": {
      "slide_number": 3,
      "page_type": "content",
      "current_version": 2,
      "image_snapshot": "images/slide-03.png",
      "versions": [
        {
          "version": 1,
          "action": "generate",
          "spec": { "elements": { "title": {...}, "subtitle": {...} } },
          "prompt_file": "images/slide-03_v0001.txt",
          "image_snapshot": "images/slide-03_v0001.png"
        },
        {
          "version": 2,
          "action": "edit",
          "spec": { "elements": { "title": {...}, "subtitle": {"content": "医疗数据碎片化"} } },
          "edit_instruction": "将副标题从'健康管理的两类割裂'改为'医疗数据碎片化'",
          "prompt_file": "images/slide-03_v0002.txt",
          "reference_version": 1,
          "image_snapshot": "images/slide-03_v0002.png"
        }
      ]
    }
  }
}
```

版本链关系：

```text
Slide 3:
  v1 (generate) ──── spec₀ ──── slide-03_v0001.png
       │
       │  --edit: subtitle 被改了
       ▼
  v2 (edit)    ──── spec₁ ──── slide-03_v0002.png  ← 当前
       │
       │  --rollback --to-version 1
       ▼
  v3 (rollback) ─── spec₀ ──── slide-03_v0003.png  ← 新当前
```

`images/slide-03.png` 始终指向 `current_version`。

---

## 七、文件布局

```text
outputs/<timestamp>/
├── images/
│   ├── slide-01.png           # 当前（最新版本）
│   ├── slide-01_v0001.png     # v1 快照
│   ├── slide-01_v0001.txt     # v1 的 prompt
│   ├── slide-01_v0002.png     # v2 快照
│   ├── slide-01_v0002.txt     # v2 的 prompt
│   ├── slide-02.png
│   └── ...
├── metadata.json              # 版本历史 + slide_spec
├── prompts.json               # 兼容旧格式
└── <title>.pptx               # 16:9 打包
```

---

## 八、数据安全

- **原子写入**：`metadata.json` 先写 temp 再 rename，不会因进程崩溃产生半截文件
- **版本快照**：每次编辑前自动备份当前图为 `_vXXXX.png`，永不丢失
- **合并不覆盖**：`--output` 指向已有 session 时合并新页，不覆盖已有 metadata
- **prompts.json 兼容**：始终同步输出，保持与旧版脚本的兼容性

---

## 九、CLI 命令汇总

| 命令 | 作用 |
|------|------|
| `python3 scripts/generate_ppt.py --plan slides_plan.json --style styles/xx.md` | 生成（内置风格） |
| `python3 scripts/generate_ppt.py --plan slides_plan.json --template-pptx xx.pptx --template-strict` | 生成（模板克隆） |
| `python3 scripts/generate_ppt.py --plan slides_plan.json --style xx.md --slides 1,3 --output path/existing` | 生成指定页，合并到已有 session |
| `python3 scripts/generate_ppt.py --edit 3 --session <ts> --element-updates '{"subtitle":{"content":"新"}}'` | 修改第 3 页 |
| `python3 scripts/generate_ppt.py --edit 3 --session <ts> --edit-prompt "…" --element-updates '{…}'` | 修改（手动 prompt + spec 更新） |
| `python3 scripts/generate_ppt.py --rollback 3 --to-version 1 --session <ts>` | 回滚第 3 页到 v1 |
| `python3 scripts/generate_ppt.py --ingest-pptx path/to/deck.pptx` | 摄取外部 PPTX |
| `python3 scripts/generate_ppt.py --list-sessions` | 列出所有 session |
