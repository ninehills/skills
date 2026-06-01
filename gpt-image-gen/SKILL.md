---
name: gpt-image-gen
description: "生图 / 生成图片 / 画图 — 用 OpenAI gpt-image-2 生成图像。支持文生图、参考图生图 (img2img)、蒙版修补 (inpainting)。当用户要求用 GPT 画图、OpenAI 生图、gpt-image-2、文+图生图、参考图片生成、img2img、inpainting 时必加载此技能。Auth 自动继承 OPENAI_API_KEY / Codex OAuth (Pi/Codex) / .env / config.yaml。"
version: 2.0.0
author: Hermes Agent
license: MIT
metadata:
  hermes:
    tags:
      - image-generation
      - gpt-image-2
      - openai
      - img2img
      - inpainting
    category: creative
---

# GPT Image Gen

调用 OpenAI `gpt-image-2` 模型生图。单一脚本覆盖三种模式：

| 模式 | 触发条件 | 后端 |
|------|---------|---------|
| 文生图 | 仅 `-p` | API key → `images.generate` / Codex OAuth → Responses API |
| 参考图编辑 | `-p` + `-i` | API key → `images.edit` / Codex OAuth → Responses API (input_image) |
| 蒙版修补 | `-p` + `-i` + `-m` | 仅 API key → `images.edit`；Codex 不支持 |

**Auth 自动选择：** 检测到 JWT token (eyJ...) 时走 Codex Responses API (`chatgpt.com/backend-api/codex`)，否则走标准 OpenAI REST API (`api.openai.com`)。

## 触发条件（几乎任何生图请求都应加载此技能）

**只要用户提到以下任一关键词/场景，立即加载：**

- 生图、生成图片、画图、文生图、图片生成、AI 画图
- gpt-image、gpt-image-2、GPT 画图、OpenAI 生图、GPT 图片
- img2img、图生图、参考图生图、文+图、传图生图、图片编辑
- inpainting、蒙版修补、局部重绘、涂抹重绘
- 用户发送了图片并要求基于它生成/修改（这是 img2img 场景）

**特别注意：** 如果用户发送了图片附件并说「用这个图生成」「基于这张图」「把这张图改成」等，这就是 img2img 场景——必须加载此技能，因为内置 `image_generate` 不支持图片输入。

## 依赖

```bash
pip install openai python-dotenv
```

- `openai`：API 调用
- `python-dotenv`：自动加载 `./.env` 和 `~/.env`（可选，没有也能用）

## Auth 自动解析

脚本按以下优先级找 API key：

1. `OPENAI_API_KEY` 环境变量
2. `./.env` 文件（当前目录）
3. `~/.env` 文件（家目录）
4. Pi agent auth (`~/.pi/agent/auth.json` → `openai-codex.access`)
5. Codex OAuth token（`hermes auth codex` 登录过的）
6. `config.yaml` → `image_gen.openai.api_key`
7. `--api-key` 手动传入

**⚠️ Codex token 限制：** Codex OAuth token/pi auth token 只能认证 `chatgpt.com/backend-api/codex`，**不能**直连 `api.openai.com`。如果 auth 解析落到 Codex token（优先级 4-5），脚本会拿它当标准 API key 去打 `api.openai.com/v1/images/*`，返回 **401 Missing scopes: api.model.images.request**。文生图请用 Hermes 内置 `image_generate` 工具（走 Codex backend）；img2img / inpainting（`images.edit`）必须有真正的 `OPENAI_API_KEY`。详见 `references/auth-pitfalls.md`。

## 用法

### 文生图

```bash
python3 scripts/gen.py -p "a cat astronaut on the moon" --quality high

# 多张
python3 scripts/gen.py -p "..." -n 4
```

### 参考图编辑（img2img）

```bash
# 单参考图
python3 scripts/gen.py -p "make it cyberpunk style" -i photo.jpg

# 多参考图
python3 scripts/gen.py -p "collab poster" -i cat.png -i logo.png -f out.png
```

### 蒙版修补（inpainting）

```bash
# opaque 区域保留，transparent 区域重绘
python3 scripts/gen.py -p "replace sky with aurora" -i photo.jpg -m sky_mask.png
```

### 高分辨率 / 自定义格式

```bash
python3 scripts/gen.py -p "..." --size 2k --quality high --format webp --compression 85
python3 scripts/gen.py -p "..." --size 3840x2160 --quality high
```

### 提示词模板

参考文件：`references/templates.md`（833 行，16 类工业级模板 + 防坑指南）。

**工作流：Agent 读 templates.md → 匹配合适的模板类别 → 用模板结构组装完整 prompt → 调 gen.py 生图。**

Agent 加载 skill 后，需按需读取 `references/templates.md` 全文或定位到相关章节。模板覆盖 UI、信息图、海报、电商、品牌、摄影、角色、历史、工业设计等 16 个类别，每类有标准模板 + 防坑指南。

**Agent 的典型流程：**
1. 用户说「画一碗拉面」→ Agent 读 templates.md，定位到「E-commerce & Products」或「Photography & Realism」类别
2. Agent 参考模板结构和防坑指南，组装完整 prompt（补全布光、材质、构图等参数）
3. **Agent 用 `clarify` 展示优化后的 prompt，等用户确认**
4. 确认后调 `gen.py -p "<prompt>" --quality high`

如果用户给的 prompt 已经很详细，则跳过优化，直接调 gen.py。

**⚠️ 强制确认规则：**
- 用户明确说「优化并生成」「帮我优化 prompt」「先优化再画」等 → Agent 优化后**直接生成**，无需确认
- 用户只是给了模糊提示词但**未要求优化** → Agent 若修改了 prompt，**必须**用 `clarify` 展示修改后的完整 prompt 让用户确认，确认后才能调 gen.py
  - 确认时必须说明「我优化了提示词，确认后生成：」让用户知道 prompt 被改过
- 未修改则直接生成

## 参数表

| 参数 | 简写 | 类型 | 默认值 | 说明 |
|------|------|------|--------|------|
| `--prompt` | `-p` | str | **必填** | 提示词 |
| `--image` | `-i` | path | — | 参考图，可重复传多个 |
| `--mask` | `-m` | path | — | Alpha 通道蒙版 PNG（需配合 `-i`） |
| `--output` | `-f` | path | 自动命名 | 输出文件路径 |
| `--n` | `-n` | int | 1 | 生成张数 |
| `--model` | | str | `gpt-image-2` | 模型 ID |
| `--quality` | `-q` | literal | `high` | 见下方质量策略 |
| `--size` | `-s` | literal | `1024x1024` | 见下方尺寸表 |
| `--format` | | literal | `png` | `png` / `jpeg` / `webp` |
| `--compression` | | int | — | 压缩级别 0-100（jpeg/webp） |
| `--moderation` | | literal | `low` | `low` / `auto` |
| `--background` | | literal | — | `opaque` / `auto` |
| `--input-fidelity` | | literal | — | `low` / `high`（gpt-image-2 自动剔除） |
| `--api-key` | | str | 自动解析 | 手动指定 API key |
| `--json` | | flag | — | JSON 格式输出 |
| `--verbose` | `-v` | flag | — | 详细日志 |

## 尺寸快捷表

| 快捷名 | 分辨率 | 适用场景 |
|--------|--------|---------|
| `1k` / `square` | 1024×1024 | 正方形，社交头像 |
| `2k` | 2048×2048 | 高清印刷 |
| `4k` | 3840×2160 | 宽屏电影级 |
| `landscape` | 1536×1024 | 横版照片/游戏截图 |
| `portrait` | 1024×1536 | 竖版海报/手机壁纸 |
| `wide` | 2048×1152 | 宽幅横版 |
| `tall` | 2160×3840 | 超长竖版 |

也支持自定义 WxH：`--size 1536x1536`。约束：16px 倍数，总像素 655,360 ~ 8,294,400，宽高比 ≤ 3:1。

## 质量策略

| 质量 | 速度 | 成本(1024²) | 何时用 |
|------|------|------------|--------|
| `auto` | 自适应 | 自适应 | 让 API 自行判断 |
| `low` | ~15s | ~$0.006 | 快速草稿、批量探索、构图检查 |
| `medium` | ~40s | ~$0.05 | 风格测试、日常浏览 |
| `high` | ~2min | ~$0.21 | **中文字体、海报、信息图、正式交付** |

默认 `high`。Agent 应根据场景自动选档：探索用 low、风格尝试用 medium、最终交付用 high。

## 输出

- 默认输出格式：`── PROMPT ──` + prompt + `── IMAGE ──` + 图片路径 + `── PARAMS ──` + 参数
- **Agent 收到输出后，必须同时展示图片和 prompt**，用 `![desc](path)` + prompt 文本
- 保存到 `~/.hermes/cache/images/`（`--output` 可覆盖）
- **每次生成同时保存 `.prompt.txt` 副文件**，与图片同名、同路径，记录完整 prompt 和所有参数，方便复用
- `--json` 模式输出结构化 JSON

## 退出码

| 码 | 含义 | 处理 |
|----|------|------|
| 0 | 成功 | 输出路径到 stdout |
| 1 | API 错误 | 检查 moderation/rate-limit/content-policy 返回信息 |
| 2 | 参数错误 | 缺少 API key、mask 无 image、n<1 等 |

## 注意事项

- gpt-image-2 不支持透明背景（需要透明用 `gpt-image-1.5`）
- `--input-fidelity` 在 gpt-image-2 上会自动剔除（模型拒绝该参数）
- `--moderation` 仅适用于 `images.generate`，`images.edit` 路径自动剔除
- Codex backend 不支持 `--mask` 蒙版修补、不支持 `-n` 多张
- 编辑端点对真人照片有严格内容策略，可能返回 400
- 复杂 prompt 可能耗时 2 分钟以上
- 需要 OpenAI Organization Verification（标准 API key）；Codex OAuth 无需
- 参考图直接以 binary 上传，注意文件大小
- `--mask` 必须是带 alpha 通道的 PNG，opaque=保留，transparent=重绘
- Codex backend 架构详见 `references/codex-backend.md`
- 图片附件路径：Discord/Telegram 缓存通常在 `~/.hermes/image_cache/img_*.jpeg`
- 端到端测试报告：`references/test-report.md`
- **img2img 不依赖聊天模型的 vision 能力：** 参考图作为 base64 直接发送到 Codex/OpenAI 图像后端，不经过聊天模型。即使当前模型（如 DeepSeek）不支持 vision、`vision_analyze` 失败，`gen.py -i` 的 img2img 仍然正常工作。Agent 无需先「看懂」图片即可做参考图编辑——图像模型独立处理视觉理解。若确实需要了解原图内容来写 prompt，可用 PIL 做颜色/边缘分析辅助判断构图。<｜end▁of▁thinking｜>

<｜｜DSML｜｜parameter name="old_string" string="true">## 注意事项

- gpt-image-2 不支持透明背景（需要透明用 `gpt-image-1.5`）
- `--input-fidelity` 在 gpt-image-2 上会自动剔除（模型拒绝该参数）
- `--moderation` 仅适用于 `images.generate`，`images.edit` 路径自动剔除
- Codex backend 不支持 `--mask` 蒙版修补、不支持 `-n` 多张
- 编辑端点对真人照片有严格内容策略，可能返回 400
- 复杂 prompt 可能耗时 2 分钟以上
- 需要 OpenAI Organization Verification（标准 API key）；Codex OAuth 无需
- 参考图直接以 binary 上传，注意文件大小
- `--mask` 必须是带 alpha 通道的 PNG，opaque=保留，transparent=重绘
- Codex backend 架构详见 `references/codex-backend.md`
- 图片附件路径：Discord/Telegram 缓存通常在 `~/.hermes/image_cache/img_*.jpeg`
- 端到端测试报告：`references/test-report.md`
