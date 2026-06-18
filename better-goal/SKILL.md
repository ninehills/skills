---
name: better-goal
description: 为任意 coding agent 编写高质量任务目标的指南。当用户提到「设定目标」「goal」「任务目标」「持久目标」「完成标准」、或需要为复杂多轮任务（性能优化、调试、迁移、研究复现等）定义可验证的完成条件和约束时触发。
version: 1.2.0
source: https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex
metadata:
  hermes:
    tags: [goal, prompt-engineering, agent, 中文]
    related_skills: [writing-plans, code-review]
---

# 编写高质量任务目标（better-goal）

**Goal 不是 prompt。Goal 是验收契约。**

以前我们写 prompt，告诉模型「做什么」就够了。现在 agent 能干活了，要求更高——你得告诉它「做完了怎么验证」「什么情况下停」「边界在哪里」。没有验收标准的 goal 就是猜谜游戏；没有暂停条件的 goal 会让 agent 在死循环里烧 token。

一个好的任务目标让 agent 在多轮探索中持续朝着既定结果工作，证据达标时停止，遇到决策点时暂停等人工判断。

## 一次性任务 vs 持久目标

| | 一次性任务 | 持久目标 |
|---|---|---|
| 模式 | 请求 → 工作 → 结果 → 等待 | 工作 → 检查 → 继续或完成 |
| 适用 | 单次明确任务 | 路径不确定的多轮探索任务 |
| 完成 | 任务执行完毕 | 证据验证目标达成 |

## 何时使用持久目标

**应该用：** 性能优化、flaky test 排查、依赖迁移、需要复现的 bug 调查、多步重构、基准驱动调优、需要产出最终产物的研究任务。

**不要用：** 单行修改、简短解释、代码审查、一次性问答。标语式目标（「让代码更好」「重构一下」）也不适合。

适用条件：**持久目标 + 可验证的完成线 + 需多轮探索的路径**。

## 七个要素

1. **Outcome（结果）**：工作完成时应该为真的状态
2. **Verification（验证）**：证明结果的测试 / 基准 / 产物 / 命令输出
3. **Constraints（约束）**：工作过程中不能退化的东西
4. **Boundaries（边界）**：允许写哪里、禁止碰哪里、可用的资源范围
5. **Iteration policy（迭代策略）**：每次改多少，改完跑什么检查，如何选择下一步
6. **Stop when（完成条件）**：什么证据出现时宣告目标达成
7. **Pause if（暂停条件）**：什么情况下暂停等人工决策（而非继续盲猜或宣告失败）

## 编写模板

```
/goal [期望结果]

Verification: [用什么证明完成了]
Constraints: [不能改什么 / 不能退化什么]
Boundaries: [允许写哪里 / 禁止碰哪里]
Iteration policy: [每次改多少，改完跑什么检查，如何选下一步]
Stop when: [证据证明完成]
Pause if: [什么情况下暂停等人工决策]
```

**Stop when 和 Pause if 的区别**：Stop when 是正向的完成信号（测试全绿、benchmark 达标），Pause if 是需要人介入的决策点（发现两种方案各有利弊、需要确认是否接受 trade-off、外部依赖阻塞需人类操作）。不要一遇到困难就宣告完成——该暂停时暂停，该完成时完成，该报告阻塞时报告阻塞。

## 从弱到强

### 性能优化

弱：`/goal Improve performance`

强：
```
/goal Reduce p95 checkout latency below 120 ms

Verification: checkout benchmark 报告 p95 低于 120ms，且 correctness suite 全绿
Constraints: 不退化 correctness suite 任何测试，不修改公开 API
Boundaries: 仅修改 checkout service、benchmark fixtures 和相关测试
Iteration policy: 每次迭代记录改了什么、benchmark 结果、下一最佳实验方向
Stop when: p95 连续 3 次 benchmark 低于 120ms 且 correctness suite 全绿
Pause if: benchmark 无法运行、或连续 5 次实验无改善、或需要动到公开 API
```

关键差异：强目标命名了**结果（<120ms）、验证方式（benchmark）、约束（correctness suite、API）、完成信号（连续3次达标）和暂停点（5次无改善）**。即使延迟从 180ms 降到 135ms，目标仍未完成；延迟达标但测试失败，目标也未完成；连续无改善时暂停而非盲目继续。

### 研究复现

弱：`/goal Reproduce Buehler et al., "Deep Hedging"`

强：
```
/goal Produce the strongest evidence-backed reproduction of Buehler et al., "Deep Hedging"

Verification: 产出分级报告——confirmed（精确复现）/ approximate（近似结果）/ blocked（阻塞项）/ uncertain（不确定项）四层分类
Constraints: 仅使用论文公开材料和本地资源，不做未经证实的结论宣称
Boundaries: 允许使用论文材料、公开数据集、本地计算资源
Iteration policy: 逐一尝试每个 headline 结果，验证输出，记录证据等级
Stop when: 所有 headline 结果分类完毕，报告覆盖四层分级
Pause if: 关键数据不可获取、计算资源不足跑完整实验、论文方法有歧义需判断
```

关键差异：强目标定义了证据标准（四层分级），防止「看似可行」的产物变成过度宣称的结论。

## 核心纪律

1. **先定义验收标准，再开始工作。** 什么算完成？什么证据能证明？什么信号表示该停？
2. **目标要窄到能审计，宽到能探索。** 「修好这个测试」可能太窄（问题在上游依赖），「改善整个系统」太宽（没有审计面）。
3. **区分三种停止信号，不要混淆。** Stop when 是完成，Pause if 是等人，彻底无路可走才报告阻塞。不要用暂停冒充完成，也不要用完成冒充阻塞。
4. **预算耗尽≠目标完成。** 达到 token 限制时应总结进展和阻塞，标识下一步，而非标记完成。

## 反向访谈：让 AI 帮你收敛需求

当你不确定怎么写 goal 时，最有用的技巧是**让 AI 反向访谈你**——你只知道要做什么，AI 帮你把验收标准、边界条件、暂停规则全部问出来。

**Step 1：提交模糊任务，让 AI 反问**

```
/plan Help me turn this vague task into a strong coding agent goal. 
Interview me for missing success criteria, verification commands, 
constraints, boundaries, iteration policy, stop conditions, and pause 
triggers. Then draft a final /goal ... command.
```

或者中文版：

```
/plan 帮我把这个模糊任务转成高质量的 coding agent 目标。
请反问我：验收标准是什么、用什么命令验证、有什么约束不能碰、
边界在哪、迭代策略、什么证据算完成、什么情况要暂停等人。
最后生成一份完整的 /goal 命令。
```

**Step 2：回答 AI 的反问**

AI 会逐一追问：
- 「你说的『快』具体是多少毫秒？」
- 「用什么测试来验证？」
- 「性能优化过程中有什么不能改？」
- 「如果连续失败几次后要暂停？」

**Step 3：审查生成的 goal**

检查收紧张力：成功条件是否可测量？暂停条件是否覆盖常见卡点？边界是否清晰？然后复制执行。

这就是 Agent 时代的工程规范——需求不再是「你猜我要什么」，而是「我们一起把验收契约写清楚」。
