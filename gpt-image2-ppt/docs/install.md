# gpt-image2-ppt-skills 安装指南（给 AI agent 读）

> 本文档是**让 AI 助手自动完成安装**的可执行指引。Claude Code / OpenClaw / Codex / Cursor / Trae / Hermes Agent 等任一 agent 按以下步骤执行即可。
>
> 人类用户请**不要**手动照抄下面的步骤，直接把这份 URL 扔给你的 AI 助手，它自己会跑通。

## 项目一句话介绍

`gpt-image2-ppt-skills` 是一个 Claude Code / Codex / OpenClaw Skill，用 OpenAI `gpt-image-2` 生成视觉强烈的 PPT（10 套内置风格 + 支持仿任意 `.pptx` 模板），产出高清逐页 PNG + 16:9 `.pptx`。

仓库：https://github.com/JuneYaooo/gpt-image2-ppt-skills

## 前置依赖（agent 请先检查）

- **必需**：`git`、`python3`（3.8+）、`pip`
- **可选**：`libreoffice` 本机命令（仅模板克隆模式需要，用来把 `.pptx` 渲染成 PNG）

如果缺 `git` / `python3`，先用系统包管理器装好；`libreoffice` 可以留到用户要用模板克隆模式时再装。

## 安装步骤

### 1. 克隆仓库到临时目录

```bash
git clone https://github.com/JuneYaooo/gpt-image2-ppt-skills.git /tmp/gpt-image2-ppt-skills
cd /tmp/gpt-image2-ppt-skills
```

### 2. 跑安装脚本

```bash
# Claude Code
bash install_as_skill.sh --target claude

# Codex
bash install_as_skill.sh --target codex
```

这一步会：
- 把项目文件拷贝到对应 agent 的 skill 目录
- `pip install -r requirements.txt` 装齐 Python 依赖
- 保留 `.env.example`；安装目录 `.env` 只作为 standalone CLI fallback

安装脚本是**交互式**的，如果目标目录已存在会问是否覆盖；agent 可以用 `yes | bash install_as_skill.sh` 自动选"是"，或先检查目录是否存在再决定。

### 3. 只有在走 API 直连时才注入环境变量

如果当前 agent 就是带原生图片生成能力的 Codex，可以跳过这一步，直接重启后走 `SKILL.md` 里的原生出图路径。

否则，agent 需要主动问用户要 API key：

> 请提供你的 OpenAI API key（或任意 OpenAI 兼容中转的 base_url + key）。

优先通过当前 agent 框架或运行环境注入，不要写进调用者业务项目根目录 `.env`：

- Claude Code：用户级 `~/.claude/settings.json`，或项目级 `.claude/settings.local.json`
- OpenClaw / 自定义 Agent：用 `apiKey` / env reference 引用系统环境变量，避免明文写进项目配置
- CI / 服务器：用系统环境变量、Docker Compose、Kubernetes Secret 或 CI Secret
- standalone CLI fallback：设置 `GPT_IMAGE2_PPT_ENV=/path/to/private.env`，或使用 skill 安装目录下的 `.env`

需要注入的变量如下：

```bash
OPENAI_BASE_URL=https://api.openai.com    # 或用户提供的兼容中转 URL
OPENAI_API_KEY=sk-...                     # 用户提供的 key（必需）
GPT_IMAGE_MODEL_NAME=gpt-image-2
GPT_IMAGE_QUALITY=high                    # low / medium / high / auto
```

> 如果用户没有 OpenAI 官方 key，agent 可以告诉用户：本 skill 也支持 `--backend codex` 复用本地 codex CLI 登录，不需要在本 skill 配 key；或者用任意 OpenAI 兼容中转站（把 `OPENAI_BASE_URL` 改成中转站地址即可）。

### 3.1 模板克隆的视觉分析

模板克隆需要先看懂 `.pptx` 渲染出的页面截图：

- 当前 agent 是多模态模型（例如多模态 Claude / GPT / 原生 Codex）时，不需要配置 `VISION_*`；agent 直接读取 `template_renders/<stem>/page-*.png`，生成 `template_profile.json`，再用 `--template-profile` 传给 CLI。
- 当前 agent 是纯文本模型（例如 DeepSeek 文本模型）时，需要额外配置 `VISION_BASE_URL` / `VISION_API_KEY` / `VISION_MODEL_NAME`，由 `template_analyzer.py` 调独立多模态模型分析模板。

### 4. 提示用户重启 agent

装完之后，告诉用户：

> 已安装完成。请**重启当前 agent**（Claude Code / Codex / 其它）让 skill 生效。

### 5.（可选）清理临时目录

```bash
rm -rf /tmp/gpt-image2-ppt-skills
```

## 冒烟测试（用户重启 agent 后）

告诉用户直接跟 agent 说：

> 帮我用 **gpt-image2-ppt** 生成一份关于「猫为什么是液体」的 3 页 PPT，风格用 `gradient-glass`。

正常的话 agent 会自己写 `slides_plan.md`、转成 `slides_plan.json`、跑 `scripts/generate_ppt.py --slides 1` 先出封面、再跑全量，最后给出输出目录和 `.pptx` 的路径。

## 常见问题（给 agent 参考）

- **`ModuleNotFoundError: pymupdf`** → 在实际安装目录里重跑 `pip install -r requirements.txt`
- **`libreoffice: command not found`**（仅模板克隆模式）→ Linux: `apt install libreoffice`；macOS: `brew install --cask libreoffice`；或让用户切换到内置 10 套风格之一，不走模板克隆
- **`OPENAI_API_KEY 未设置`** → 如果你不是走 Codex 原生路径，回到步骤 3，检查 agent / 系统环境变量是否已注入；standalone CLI 再检查 `GPT_IMAGE2_PPT_ENV` 或 skill 安装目录 `.env`
- **agent 识别不到 skill** → 确认目录装到了对应 agent 的技能目录，并且完全重启过当前 agent

## 完成标志

以下三条都满足即视为安装成功：

1. 对应安装目录下的 `SKILL.md` 存在
2. 如果走 API 直连模式，agent / 系统环境变量中能提供可用 `OPENAI_API_KEY`
3. agent 重启后，用户用自然语言要求生成 PPT 时能触发本 skill

装完不用逐字读 `SKILL.md`，但需要告诉用户："你可以直接用自然语言要 PPT，也可以把任意 `.pptx` 模板丢给我做克隆。"
