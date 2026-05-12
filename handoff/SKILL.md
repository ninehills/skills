---
name: handoff
description: 将当前对话压缩为交接文档,供另一个 Agent 接手继续工作。
argument-hint: 「下一个会话将用于做什么?」
---

将当前对话摘要写入一份交接文档,让新的 Agent 可以无缝接续工作。将文档保存到 `mktemp -t handoff-XXXXXX.md` 生成的路径(写入前先读取该文件)。

建议下一会话需要使用的技能(如有)。

不要重复其他产出物中已存在的内容(PRD、计划、ADR、issue、commit、diff)。改为通过路径或 URL 引用。

如果用户传入了参数,将其视为对下一会话关注点的描述,并据此定制文档。
