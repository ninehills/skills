---
name: patent-draft-agent
description: >
  生成符合中国专利法规范的专利交底书。接收 Markdown 格式的技术描述作为输入，通过多阶段流水线（标题生成、专利检索、发明目的撰写、发明内容撰写、图表生成、报告合并）自动生成完整的专利交底书。
---

# 专利交底书生成 Skill

你负责协调多个子代理，按阶段产出完整、可提交审阅的专利交底书。

## 初始化

API Key 要求：
- 必需：`SERPAPI_API_KEY`
  - 用途：`google-patents-mcp.search_patents`，这是 Google Patents 检索的必需凭据
- 可选：`EXA_API_KEY`
  - 用途：`exa.web_search_exa` / `exa.crawling_exa`
  - 未配置时：不影响主流程，回退到 coding agent 内置搜索/浏览能力处理非专利资料检索

开始前先运行：

```bash
bash <SKILL_DIR>/scripts/init.sh
```

如果失败，停止执行，并明确缺失的环境变量或命令。

从输出中记录 `SKILL_DIR=...` 的绝对路径。后续所有 skill 内文件都基于该路径引用。

## MCP 工具调用约定

本 skill 的检索兼容两层能力：
- Google Patents 检索必须通过 `mcporter` CLI 执行
- Exa 检索为可选增强；如果 Exa 不可用，则改用 coding agent 内置搜索/浏览能力补足产品、论文和网页资料检索

固定格式：

```bash
mcporter call <server>.<tool> --config <SKILL_DIR>/references/mcporter.json <key=value>...
```

mcporter 可用服务：

| 服务 | 工具 | 典型用途 |
|------|------|----------|
| `google-patents-mcp` | `search_patents` | Google Patents 专利检索 |
| `exa` | `web_search_exa` | 可选的网页 / 论文 / 产品语义检索 |
| `exa` | `crawling_exa` | 可选的重点页面正文抓取 |

调用规则：
- `search_patents` 至少传 `query` 与 `num_results`
- `web_search_exa` 至少传 `query` 与 `numResults`
- `crawling_exa` 传 JSON 数组格式的 `urls`
- 所有命令都由子代理通过 Bash 工具执行
- 检索结果先落盘，再用于分析，避免只在上下文中口头总结
- 若 Exa 不可用或未配置，不阻塞流程；保持 Google Patents 检索不变，并改用内置搜索/浏览能力完成非专利资料检索

示例：

```bash
mcporter call google-patents-mcp.search_patents \
  --config <SKILL_DIR>/references/mcporter.json \
  query="大规模文件分发" \
  num_results=8

mcporter call exa.web_search_exa \
  --config <SKILL_DIR>/references/mcporter.json \
  query="中国专利 文件分发系统" \
  numResults=5
```

## 工作目录管理

收到请求后创建工作目录：

```bash
uuid=$(python3 -c "import uuid;print(uuid.uuid4())")
WORK_DIR=output/temp_${uuid}
mkdir -p ${WORK_DIR}/{00_input,01_title,02_search,03_sections,04_final,metadata}
```

如果用户提供 UUID，则复用该 UUID。

将用户输入保存为 `${WORK_DIR}/00_input/input.md`。

## 流程

严格按以下 6 个阶段执行。每个阶段结束后先验收文件，再进入下一阶段。

对子代理的统一要求：
1. 先读取对应 prompt 文件。
2. 在附加指令中给出工作目录绝对路径。
3. 如涉及检索，给出 `mcporter.json` 的绝对路径。
4. 明确输入文件、输出文件、验收标准。
5. 要求子代理只写指定文件，不改其他阶段产物。

### 阶段 1：标题生成

Prompt：`<SKILL_DIR>/references/prompts/title-generator.md`

输入：
- `${WORK_DIR}/00_input/input.md`

输出：
- `${WORK_DIR}/01_title/title.txt`

验收：
- 文件存在
- 单行纯文本
- 标题不超过 25 字

### 阶段 2：专利检索与查重

Prompt：`<SKILL_DIR>/references/prompts/patent-searcher.md`

输入：
- `${WORK_DIR}/00_input/input.md`
- `${WORK_DIR}/01_title/title.txt`

输出：
- `${WORK_DIR}/02_search/search-report.md`
- `${WORK_DIR}/02_search/similar-patents-reference.md`

验收：
- 两个文件都存在
- `search-report.md` 包含检索关键词、相关专利、区别点总结
- `similar-patents-reference.md` 包含术语规范和写作建议

### 阶段 3：发明目的撰写

Prompt：`<SKILL_DIR>/references/prompts/purpose-writer.md`

输入：
- `${WORK_DIR}/00_input/input.md`
- `${WORK_DIR}/01_title/title.txt`
- `${WORK_DIR}/02_search/similar-patents-reference.md`

输出：
- `${WORK_DIR}/03_sections/purpose-section.md`

验收：
- 文件存在
- 约 1000 到 1500 字

### 阶段 4：发明内容撰写

Prompt：`<SKILL_DIR>/references/prompts/content-writer.md`

输入：
- `${WORK_DIR}/00_input/input.md`
- `${WORK_DIR}/01_title/title.txt`
- `${WORK_DIR}/02_search/similar-patents-reference.md`
- `${WORK_DIR}/03_sections/purpose-section.md`

输出：
- `${WORK_DIR}/03_sections/content-section.md`

验收：
- 文件存在
- 约 2000 到 2500 字

### 阶段 5：图表生成

Prompt：`<SKILL_DIR>/references/prompts/diagram-generator.md`

输入：
- `${WORK_DIR}/03_sections/content-section.md`

输出：
- `${WORK_DIR}/03_sections/diagrams.md`

验收：
- 文件存在
- 包含 Mermaid 代码块

### 阶段 6：报告合并

Prompt：`<SKILL_DIR>/references/prompts/report-merger.md`

输入：
- `${WORK_DIR}/01_title/title.txt`
- `${WORK_DIR}/02_search/search-report.md`
- `${WORK_DIR}/03_sections/purpose-section.md`
- `${WORK_DIR}/03_sections/content-section.md`
- `${WORK_DIR}/03_sections/diagrams.md`

输出：
- `${WORK_DIR}/04_final/output.md`
- `${WORK_DIR}/metadata/quality-check.json`

验收：
- `output.md` 存在
- 总篇幅不少于 3000 字
- `quality-check.json` 存在

## 子代理调用模板

对子代理不要只给“请处理”这类模糊指令，必须给出完整执行框架。推荐模板：

```text
你现在执行 skill 子任务：<agent-name>

先完整阅读以下 prompt 文件，再开始工作：
<PROMPT_FILE_ABS_PATH>

执行约束：
- 工作目录：<WORK_DIR_ABS_PATH>
- 仅允许读取以下输入文件：
  - <INPUT_FILE_1_ABS_PATH>
  - <INPUT_FILE_2_ABS_PATH>
- 仅允许写入以下输出文件：
  - <OUTPUT_FILE_1_ABS_PATH>
  - <OUTPUT_FILE_2_ABS_PATH>
- 除非任务明确要求，不得修改其他文件

如任务涉及检索：
- Google Patents 必须使用 mcporter：
  - mcporter 配置：<SKILL_DIR>/references/mcporter.json
  - 命令格式：mcporter call google-patents-mcp.search_patents --config <CONFIG_ABS_PATH> key=value ...
- 产品 / 论文 / 网页资料优先使用 Exa；若 Exa 不可用，则回退到 coding agent 内置搜索/浏览能力
- 先执行检索，再整理成 Markdown 输出

完成条件：
- 所有指定输出文件都已写入
- 内容满足 prompt 中的结构和字数要求
- 最终回复中仅总结完成情况与输出路径
```

## 创造性扩展原则

允许：
- 补充合理的技术细节、参数范围、实施步骤、技术效果、实施例
- 基于检索结果统一术语和表达方式

禁止：
- 虚构改变核心方案的内容
- 抄袭现有专利文本
- 无依据夸大效果

## 质量标准

- 总篇幅不少于 3000 字
- 术语统一
- 逻辑完整：技术问题、现有方案、缺陷、本发明方案、技术效果
- Markdown 结构规范
- 不直接复用检索到的专利原文

## 注意事项

- 主代理负责调度与验收，不直接替代子代理完成各阶段长文
- 子代理之间只通过文件传递数据
- 只使用当前输入文件和当前工作目录中的产物
