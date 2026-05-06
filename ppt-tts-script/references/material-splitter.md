# 素材提取（Step 1）详细指南

## 任务

将演示文档拆分为标准化的每页 PNG 图片 + Markdown 文字文件，并生成素材索引。

## 执行流程

### 1. 提取文字内容

```bash
python3 scripts/extract_ppt.py <pptx_path> <output_dir>/01_materials/
```

脚本会提取每页的：
- 标题（如有）
- 正文文字（所有文本框）
- 演讲者备注（如有）

输出格式为 `slide-XX.md`：

```markdown
# 页面标题

页面正文内容...

**演讲者备注:**
演讲者备注内容...
```

### 2. 转换为 PDF

```bash
soffice --headless --convert-to pdf <pptx_path> --outdir <output_dir>/01_materials/
```

### 3. PDF 转 PNG

```bash
pdftoppm -png -r 150 <output_dir>/01_materials/<filename>.pdf <output_dir>/01_materials/slide
```

`pdftoppm` 的输出文件名格式取决于总页数：
- 少于 10 页：`slide-1.png`, `slide-2.png`
- 10-99 页：`slide-01.png`, `slide-02.png`
- 100+ 页：`slide-001.png`, `slide-002.png`

需要统一重命名为两位数零填充格式：`slide-01.png`, `slide-02.png`, ...

### 4. 生成索引

创建 `01_materials/index.json`：

```json
{
  "source_file": "presentation.pptx",
  "total_pages": 30,
  "materials": [
    {
      "page": 1,
      "image": "slide-01.png",
      "markdown": "slide-01.md",
      "has_notes": true
    }
  ],
  "extracted_at": "2026-01-07T10:00:00Z"
}
```

### 5. 清理临时文件

删除中间 PDF 文件（PNG 已提取完毕，不再需要）。

## 特殊情况处理

- **PDF 输入**：跳过步骤 1（无法提取文字）和步骤 2（已经是 PDF），直接从步骤 3 开始。MD 文件留空或标注"PDF 输入，无可提取文字"。
- **Keynote 输入**：先用 soffice 转 PPTX，再走标准流程。
- **提取失败**：如果 extract_ppt.py 报错，检查文件格式是否正确，提示用户。
