---
name: better-goal
description: 为任意 coding agent 编写高质量任务目标的指南。当用户提到「设定目标」「goal」「任务目标」「持久目标」「完成标准」、或需要为复杂多轮任务（性能优化、调试、迁移、研究复现等）定义可验证的完成条件和约束时触发。
version: 1.1.0
source: https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex
metadata:
  hermes:
    tags: [goal, prompt-engineering, agent, 中文]
    related_skills: [writing-plans, code-review]
---

# 编写高质量任务目标（better-goal）

一个好的任务目标是**可验证的完成合约**，而非更大的 prompt。它让 agent 在多轮探索中持续朝着既定结果工作，并在证据达标时停止。

## 一次性任务 vs 持久目标

| | 一次性任务 | 持久目标 |
|---|---|---|
| 模式 | 请求 → 工作 → 结果 → 等待 | 工作 → 检查 → 继续或完成 |
| 适用 | 单次明确任务 | 路径不确定的多轮探索任务 |
| 完成 | 任务执行完毕 | 证据验证目标达成 |

## 何时使用持久目标

**应该用：** 性能优化、flaky test 排查、依赖迁移、需要复现的 bug 调查、多步重构、基准驱动调优、需要产出最终产物的研究任务。

**不要用：** 单行修改、简短解释、代码审查、一次性问答。标语式目标（"让代码更好"、"重构一下"）也不适合。

适用条件：**持久目标 + 可验证的完成线 + 需多轮探索的路径**。

## 六个要素

1. **Outcome（结果）**：工作完成时应该为真的状态
2. **Verification surface（验证面）**：证明结果的测试/基准/产物/命令输出
3. **Constraints（约束）**：工作过程中不能退化的东西
4. **Boundaries（边界）**：允许使用的文件/工具/数据/资源
5. **Iteration policy（迭代策略）**：每次尝试后如何选择下一步
6. **Blocked stop condition（阻塞终止条件）**：何时停止并报告无可行路径

## 编写模板

```
/goal <期望最终状态> verified by <具体证据> while preserving <约束条件>。
Use <允许的工具或边界>。
Between iterations，<如何选择下一步>。
If blocked or no valid paths remain，<报告内容和解锁条件>。
```

## 从弱到强

### 性能优化

弱：`/goal Improve performance`

强：`/goal Reduce p95 checkout latency below 120 ms, verified by the checkout benchmark, while keeping the correctness suite green. Use only the checkout service, benchmark fixtures, and related tests. Between iterations, record what changed, what the benchmark showed, and the next best experiment to try. If the benchmark cannot run or no valid paths remain, stop with the attempted paths, the evidence gathered, the blocker, and the next input needed.`

关键差异：强目标命名了**结果（<120ms）、验证方式（checkout benchmark）、约束（correctness suite green）**。即使延迟从 180ms 降到 135ms，目标仍未完成；延迟达标但测试失败，目标也未完成；基准无法运行，必须报告阻塞而非宣告成功。

### 研究复现

弱：`/goal Reproduce Buehler et al., "Deep Hedging"`

强：`/goal Produce the strongest evidence-backed reproduction of Buehler et al., "Deep Hedging," using the available paper materials and local resources. Attempt every headline result, verify the outputs, and end with a report that separates reproduced mechanics, approximate trained results, blocked exact replay, and remaining uncertainty.`

关键差异：强目标定义了证据标准（confirmed / approximate / blocked / uncertain 四层分级），防止"看似可行"的产物变成过度宣称的结论。

## 核心纪律

1. **先定义证据标准，再开始工作。** 什么算精确复现？什么算近似？什么算阻塞？
2. **目标要窄到能审计，宽到能探索。** "修好这个测试"可能太窄（问题在上游依赖），"改善整个系统"太宽（没有审计面）。
3. **遇到阻塞不要宣告成功。** 如果验证面不可用，应报告阻塞而非声称完成。
4. **预算耗尽≠目标完成。** 达到 token 限制时应总结进展和阻塞，标识下一步，而非标记完成。

## 让 Agent 帮你写目标

如果不确定如何写，先描述任务，让 agent 生成草案：

```
Help me turn this into a task goal: I want to keep working on this flaky
checkout test until we either fix it with evidence or can clearly explain 
what is blocking progress.
```

然后审查草案，收紧成功条件、验证面、约束和阻塞终止条件，再开始执行。
