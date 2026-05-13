# 用 nbformat 构造 .ipynb 文件

当用户选择传统 `.ipynb` 格式时，可以用 Python 的 `nbformat` 库程序化生成 notebook。

## 基本用法

```python
import nbformat

nb = nbformat.v4.new_notebook()

# 添加 Markdown cell
nb.cells.append(nbformat.v4.new_markdown_cell("# 标题\n\n这是说明文字"))

# 添加代码 cell
nb.cells.append(nbformat.v4.new_code_cell("import numpy as np\nnp.arange(10)"))

# 设置 kernel
nb.metadata.kernelspec = {
    "display_name": "Python 3",
    "language": "python",
    "name": "python3",
}

# 写入文件
with open("notebook.ipynb", "w") as f:
    nbformat.write(nb, f)
```

## 常用 cell 类型

| 函数 | 用途 |
|------|------|
| `new_markdown_cell(source)` | Markdown cell |
| `new_code_cell(source)` | 代码 cell |
| `new_raw_cell(source)` | 原始文本 cell |

## cell metadata

```python
cell = nbformat.v4.new_code_cell("print('hello')")
cell.metadata.tags = ["framework"]  # 添加标签
cell.metadata.execution = {"iopub.execute_input": "..."}  # 执行信息
```

## 完整带输出的代码 cell

```python
cell = nbformat.v4.new_code_cell("1 + 1")
cell.outputs.append(nbformat.v4.new_execute_result(
    execution_count=1,
    data={"text/plain": "2"},
    metadata={},
))
cell.execution_count = 1
```

## 安装

```bash
pip install nbformat
```