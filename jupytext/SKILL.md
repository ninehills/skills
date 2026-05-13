---
name: jupytext
description: "Jupyter Notebook 创建与格式转换技能。当用户提到以下任何请求时必须使用：新建/创建 notebook、Jupyter notebook、.ipynb、jupytext、py:percent、# %% cell 格式、notebook 版本控制、paired notebook、notebook 转 Python、Python 转 notebook、用纯文本写 notebook。即使用户只说「新建一个 notebook」也应触发此技能——先询问用户选用 .py percent format 还是传统 .ipynb，再按选定格式生成。不用于调试已有 notebook 或写普通 Python 脚本。"
---

# Jupytext：用纯 Python 写 Jupyter Notebook

本技能帮助用户用纯 Python 脚本编写 Jupyter Notebook，借助 [Jupytext](https://github.com/mwouts/jupytext) 实现 `.py` ↔ `.ipynb` 双向转换。

## 什么时候用这个技能

当用户提出以下请求时触发：

- 创建新的 Jupyter notebook
- 要求用 Python 写 notebook
- 提到 jupytext、paired notebook、notebook 版本控制
- 想把现有 notebook 转成纯 Python 脚本
- 提到 `# %%` cell 分隔符（percent format）

## 核心流程

### 第一步：确认格式偏好

**必须先问用户**想要哪种格式：

| 格式 | 适用场景 |
|------|----------|
| **`.py`（percent format）** | 需要版本控制、纯编辑器开发、code review 友好 |
| **`.ipynb`** | 需要 Jupyter 原生体验、包含大量输出/图表 |

如果用户只说「新建一个 notebook」，默认推荐 `.py`（percent format），因为：

1. Git diff 友好，方便 code review
2. 编辑器支持好（VS Code、PyCharm、Spyder 原生识别 `# %%`）
3. 无需额外工具即可阅读和编辑

### 第二步：生成文件

#### A. 创建 `.py` percent format 文件

一个完整的 percent format Python 文件长这样：

```python
# ---
# jupytext:
#   formats: ipynb,py:percent
#   notebook_metadata_filter: -all
# ---

# %% [markdown]
# # 标题
# 这是一段 Markdown 说明

# %% [markdown]
"""
也可以用三引号写多行 Markdown，
这在 Python 中更易读。
"""

# %%
import numpy as np
import pandas as pd

# %%
data = pd.DataFrame({
    "x": np.random.randn(100),
    "y": np.random.randn(100),
})
data.head()
```

关键语法：

| 语法 | 含义 |
|------|------|
| `# %%` | 代码 cell 开始 |
| `# %% [markdown]` | Markdown cell 开始 |
| `# %% 标题文字 [markdown]` | 带标题的 Markdown cell |
| `# %% [markdown]` + `"""..."""` | 用三引号包裹多行 Markdown（推荐） |
| `# %% key="value"` | 带 cell metadata |

#### B. 创建 `.ipynb` 文件

如果用户选择传统 notebook，使用 `nbformat` 或直接构造 JSON。参考 `references/nbformat-guide.md` 了解如何用 Python 构造 `.ipynb`。

### 第三步：提供配套操作

根据用户需求，告知或执行：

1. **安装 Jupytext**：`pip install jupytext`
2. **转换为 notebook**：`jupytext --to notebook script.py`
3. **双向同步**：先在文件头部加 YAML front matter 声明 `formats: ipynb,py:percent`，之后 `jupytext --sync script.py` 即可同步两端
4. **在 JupyterLab 中打开 .py 为 notebook**：右键 → Open With → Notebook（需安装 jupytext 扩展）

## YAML Front Matter 详解

YAML front matter 是 `.py` 文件头部用 `# ---` 包围的元数据块：

```python
# ---
# jupytext:
#   formats: ipynb,py:percent
#   notebook_metadata_filter: -all
#   cell_metadata_filter: -all
# ---

# %% [markdown]
# # My Notebook
```

常用配置：

| 配置 | 作用 |
|------|------|
| `formats: ipynb,py:percent` | 启用 paired notebook（保存时双向更新） |
| `notebook_metadata_filter: -all` | 不在 .py 中输出 notebook 级元数据 |
| `cell_metadata_filter: -all` | 不在 .py 中输出 cell 级元数据 |
| `cell_markers: '"""'` | Markdown cell 用三引号而非 `#` 注释 |

## 配置文件

在项目根目录创建 `jupytext.toml` 可设置全局默认：

```toml
# 所有 notebook 自动 paired
formats = "ipynb,py:percent"

# Markdown cell 用三引号
cell_markers = '"""'
```

或 `pyproject.toml`：

```toml
[tool.jupytext]
formats = "ipynb,py:percent"
cell_markers = '"""'
```

## 常用 CLI 命令

```bash
# .py → .ipynb
jupytext --to notebook script.py

# .ipynb → .py (percent format)
jupytext --to py:percent notebook.ipynb

# 启用 paired notebook
jupytext --set-formats ipynb,py:percent notebook.ipynb

# 同步 paired notebook（以较新文件为准）
jupytext --sync notebook.ipynb

# 测试 round-trip 一致性
jupytext --test notebook.ipynb --to py:percent

# 用 black 格式化
jupytext --sync --pipe black notebook.ipynb

# 设置 kernel 信息
jupytext --set-kernel - notebook.py
```

## light format 补充

除了 percent format (`# %%`)，Jupytext 还支持 `light` format，用 `# +` / `# -` 标记 cell 边界：

```python
# +
import numpy as np

# + [markdown]
# 这是一段说明
```

由于 light format 不如 percent format 通用（仅 Jupytext 原生支持），**一般不推荐**，除非用户明确要求。

## 注意事项

1. **永远先问用户要哪种格式**，不要默认帮用户做决定
2. `.py` 文件中不要混用 percent format 和 light format 的分隔符
3. 如果用户已有 `.ipynb` 文件想版本控制，推荐 `jupytext --set-formats ipynb,py:percent` 转为 paired notebook
4. 三引号 Markdown 写法更易读，但需要在 front matter 或配置中声明 `cell_markers: '"""'`
5. `.py` percent format 文件可以在 VS Code 中直接作为 Interactive Window cell 运行，无需启动 Jupyter

## 参考资源

- [Jupytext GitHub](https://github.com/mwouts/jupytext)
- [formats-scripts.md](https://github.com/mwouts/jupytext/blob/main/docs/formats-scripts.md) — percent / light / sphinx 格式详解
- [paired-notebooks.md](https://github.com/mwouts/jupytext/blob/main/docs/paired-notebooks.md) — paired notebook 机制