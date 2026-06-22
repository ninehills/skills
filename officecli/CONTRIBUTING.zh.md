# 为 OfficeCLI 贡献代码

> English / 英文主文件: [CONTRIBUTING.md](./CONTRIBUTING.md)

> 你必须遵守下面两条规则。代码风格、依赖、测试、文档由维护者在 merge 之后通过
> follow-up commit 处理 —— 不用操心。

## Rule 1: 一个 PR 只做一件不可再拆的事

一个 PR 必须包含且仅包含一个 feature 或一个 bug 修复,而且这个单元不能再被拆分。
如果你的改动可以被拆成多个每个都有独立价值的部分,就拆成多个 PR 分别提交。

### 自检

提交前,先让你的 AI 做一次拆分分析:

> "分析下面这一坨 diff,它能不能拆成多个独立的 PR,每个都可以独立 merge 或独立
> revert?如果可以,列出来。"

如果回答是"可以,N 个 PR",就先拆再提。

### Examples

**✅ 可以作为一个 PR 的 bug** —— 单一根因,单一修复
- `图片只指定 width 时 height fallback 错了`
- `body 级 find: 锚点抛 ArgumentException`
- `AddParagraph --index N 在 body 含 table 时偏移`

**✅ 可以作为一个 PR 的 feature** —— 单一 coherent 能力
- `query ole: 列出所有嵌入的 OLE 对象及其 ProgID 和尺寸`
- `set wrap/hposition/vposition on floating pictures`

**❌ 必须拆** —— 多个独立改动被打包
- `修图片索引 bug + 加 OLE 检测 + 加 HTML heading 编号`
  → 3 个 PR,零共享代码
- `加 OLE 对象检测 + 加 EMF→PNG 转换`
  → 2 个 PR,两个独立 layer
- `加自动宽高比 + 修索引 off-by-one + 修行距裁剪`
  → 3 个 PR,三个不相关的根因

**🤔 可拆可不拆** —— 默认选拆
- `加一个 helper 函数 + 第一处调用者`
  → 1 或 2 个 PR;helper 有独立复用价值就拆
- `加 read 支持 + 加 write 支持(同一属性)`
  → 1 或 2 个 PR;希望 read 先被 vet 就拆

## Rule 2: 每个 PR 必须附带可验证的验证方法

在 PR description 或关联 issue 里写清楚:reviewer 怎么才能验证你的改动真的有效。

### Bug 修复 PR —— 至少给出一种(按优先顺序)

1. **officecli 命令序列**,展示改动前的错误输出和改动后的正确输出
2. **shell 或 python 脚本**,能复现 bug、在修复后干净退出
3. **权威文档引用**,说明正确行为应该是什么样(OOXML spec、Microsoft / ECMA
   文档等)
4. **截图** —— 仅当 bug 纯粹是视觉问题时

### Feature PR —— 至少包含

- **一张截图**,展示 feature 实际效果(Word / Excel / PowerPoint 窗口、HTML
  预览、或终端输出)
- 可选:一段 shell 命令序列说明如何触发这个 feature

### Examples

**Bug 修复 —— 命令序列格式(最理想):**

```bash
# Before my fix:
officecli blank test.docx
officecli add test.docx picture --prop "path=photo-2x1.png" --prop "width=10cm"
officecli query test.docx picture
# → height: "10.2cm"  ❌ 错(硬编码 4 英寸 fallback)

# After my fix:
officecli blank test.docx
officecli add test.docx picture --prop "path=photo-2x1.png" --prop "width=10cm"
officecli query test.docx picture
# → height: "5.0cm"   ✓ 对(根据 2:1 像素比例自动计算)
```

**Feature —— 截图格式(最理想):**

> **标题自动编号(从 style chain 解析)**
>
> Before: ![heading-before.png] (纯 "Chapter One",无编号)
> After:  ![heading-after.png]  ("1. Chapter One",带自动编号 span)
>
> 如何触发:
> ```bash
> officecli blank demo.docx
> officecli add demo.docx paragraph --prop "style=Heading1" --prop "text=Chapter One"
> officecli watch demo.docx
> ```

## 如果你不遵守这两条规则

维护者保留以下两种处理方式。

### Option A —— 拒绝并要求重新提交(首选)

维护者关闭 PR,留一条指向本 guide 的 comment,请你按规则拆分后重新提交。

**你的 credit:** PR 完全归你,重新提交成功后仍然拿 **"Merged"** badge。

### Option B —— Cherry-pick 有价值的部分(最后手段)

如果你的 PR 里有一部分明显有价值、值得保留,维护者会用 `git cherry-pick` 直接把
这些 commit 摘到 `main`,然后关闭原 PR。

**你的 credit:**
- `git cherry-pick` 保留原作者,所以 `git log` 和 `git blame` 里那些代码行仍然
  显示你是作者。
- 维护者创建的 reconcile commit message 会附带
  `Co-authored-by: <you> <your-email>` trailer,GitHub 贡献图会把它算进你的
  contribution。
- **但原 PR 会显示为 "Closed" 而不是 "Merged"**。
