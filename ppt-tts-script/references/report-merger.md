# 合并输出（Step 4）详细指南

## 任务

将分节逐字稿合并为完整文档，同时输出 Markdown 和 JSON 两种格式。

## 输入

- `02_outline/outline.json` — 演讲大纲
- `03_manuscript/section-XX.md` — 各节逐字稿
- `03_manuscript/manuscript.json` — 逐字稿索引

## 输出

- `04_final/output.md` — Markdown 格式（人类可读）
- `04_final/output.json` — JSON 格式（机器可读）

---

## Markdown 格式规范（output.md）

```markdown
# 《演示文档标题》逐字稿

---

## 元信息

- **总页数**：30 页
- **总节数**：4 节
- **总字数**：约 8000 字
- **预计时长**：约 40 分钟

---

## 第一节：开场与公司介绍

**页面范围**：1-5 页
**风格提示**：专业、热情、自信

**[PAGE:1]** `slide-01.png`

各位来宾，大家好。今天非常高兴能在这里和大家分享...

**[PAGE:2]** `slide-02.png`

我们今天的议程有三个部分...

---

## 第二节：产品介绍

（继续...）

---

## 页面索引

| 页码 | 图片文件 | 所属节 |
|-----|---------|-------|
| 1 | slide-01.png | 第一节：开场与公司介绍 |
| 2 | slide-02.png | 第一节：开场与公司介绍 |
| ... | ... | ... |
```

---

## JSON 格式规范（output.json）

JSON 格式将逐字稿结构化为可程序化消费的数据，方便 TTS 引擎、字幕生成器、内容编辑器等下游工具直接使用。

```json
{
  "metadata": {
    "title": "演示文档标题",
    "description": "演示文档一句话描述",
    "total_pages": 30,
    "total_sections": 4,
    "total_words": 8000,
    "estimated_duration": "40分钟",
    "generated_at": "2026-01-07T10:00:00Z"
  },
  "sections": [
    {
      "id": "sec-01",
      "title": "开场与公司介绍",
      "page_start": 1,
      "page_end": 5,
      "style_hint": "专业、热情、自信",
      "estimated_duration": "5分钟",
      "word_count": 1500,
      "pages": [
        {
          "page": 1,
          "image": "slide-01.png",
          "content": "各位来宾，大家好。今天非常高兴能在这里和大家分享..."
        },
        {
          "page": 2,
          "image": "slide-02.png",
          "content": "我们今天的议程有三个部分..."
        }
      ]
    }
  ]
}
```

### JSON 字段说明

| 字段路径 | 类型 | 说明 |
|---------|------|------|
| `metadata.title` | string | 演示文档标题 |
| `metadata.description` | string | 一句话描述 |
| `metadata.total_pages` | number | 总页数 |
| `metadata.total_sections` | number | 总节数 |
| `metadata.total_words` | number | 逐字稿总字数（纯文本） |
| `metadata.estimated_duration` | string | 预估朗读总时长 |
| `metadata.generated_at` | string | ISO 8601 生成时间 |
| `sections[].id` | string | 节 ID，如 `sec-01` |
| `sections[].title` | string | 节标题 |
| `sections[].page_start` | number | 起始页码 |
| `sections[].page_end` | number | 结束页码 |
| `sections[].style_hint` | string | 风格提示 |
| `sections[].estimated_duration` | string | 本节预估时长 |
| `sections[].word_count` | number | 本节字数 |
| `sections[].pages[].page` | number | 页码 |
| `sections[].pages[].image` | string | 对应图片文件名 |
| `sections[].pages[].content` | string | 该页逐字稿纯文本 |

---

## 处理步骤

### 1. 读取大纲获取结构

从 `outline.json` 获取演示文档标题、节列表（标题、页码范围、风格提示）、时间线。

### 2. 逐节解析

读取每个 `section-XX.md`，按 `[PAGE:N]` 标记切分为每页内容。每页内容去除标记行本身，保留纯文本。

解析逻辑：
1. 找到所有 `[PAGE:N]` 标记的位置
2. 两个标记之间的文本即为该页内容
3. 去除首尾空行，保留段落结构

### 3. 统计元信息

- **总字数**：统计所有页面 content 的字符数之和
- **各节字数**：按节统计
- **预计时长**：基于字数 ÷ 语速计算

### 4. 写入双格式

同时生成 `output.md` 和 `output.json`，确保两者内容一致——同一页的逐字稿文本在两个文件中应相同。

## 一致性要求

Markdown 和 JSON 是同一份数据的两种视图，必须保证：
- 页面数量一致
- 每页文本内容一致
- 节划分一致
- 元信息（总字数、总页数）一致
