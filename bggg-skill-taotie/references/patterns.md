# 模式库文档

模式库（pattern-library.json）是饕餮的"经验记忆"，记录每次成功进化中提炼出的可复用优化模式。

## 模式分类

| 类别 | category 值 | 典型模式 |
|------|------------|---------|
| 性能优化 | `performance` | 并发、缓存、Prompt 精简 |
| 质量提升 | `quality` | 输出约束、Schema 校验、二次验证 |
| 鲁棒性 | `robustness` | 错误处理、降级方案、重试机制 |
| Prompt 策略 | `prompt` | CoT、Few-shot、角色设定、分步指引 |
| 架构优化 | `architecture` | 模块拆分、渐进式披露、工具复用 |

## 模式生命周期

1. **发现**: 对比分析中发现 B 的某个做法优于 A
2. **提炼**: 将具体做法抽象为通用模式
3. **验证**: 在沙盒测试中确认改进效果
4. **入库**: 用户确认后存入模式库
5. **累积**: 每次成功使用，增加 success_count
6. **淘汰**: 多次失败的模式降低优先级

## 模式字段说明

```json
{
  "id": "p001",                              // 唯一标识
  "name": "并发抓取优化",                     // 人类可读名称
  "category": "performance",                  // 分类
  "source_skill": "last30days",              // 最初从哪个 skill 发现的
  "applied_to": ["bggg-creator-research"],   // 成功应用到了哪些 skill
  "description": "将串行网络请求改为并发",     // 描述
  "when_to_apply": "skill 中有多个独立请求时", // 适用条件
  "when_not_to_apply": "请求之间有依赖关系时", // 不适用场景
  "implementation_hint": "asyncio.gather()",  // 实现提示
  "success_count": 3,                        // 成功应用次数
  "failure_count": 0,                        // 失败次数
  "user_satisfaction": "high",               // 用户满意度
  "created_at": "2026-04-06",
  "last_used": "2026-04-06"
}
```

## 查询策略

饕餮在分析新的 A/B 对比时，应先查阅模式库：
- 是否有与当前发现匹配的已知模式？
- 如果有，这个模式的成功率如何？
- 是否可以直接推荐，跳过部分分析步骤？
