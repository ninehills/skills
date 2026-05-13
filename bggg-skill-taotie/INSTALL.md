# 饕餮.skill 安装说明

---

## 选择你的平台

### A. Claude Code（推荐）

本项目遵循官方 [AgentSkills](https://agentskills.io) 标准，整个 repo 就是 skill 目录。克隆到 Claude skills 目录即可：

```bash
# ⚠️ 必须在 git 仓库根目录执行！
cd $(git rev-parse --show-toplevel)

# 方式 1：安装到当前项目
mkdir -p .claude/skills
git clone https://github.com/binggandata/bggg-skill-taotie .claude/skills/bggg-skill-taotie

# 方式 2：安装到全局（所有项目都能用）
git clone https://github.com/binggandata/bggg-skill-taotie ~/.claude/skills/bggg-skill-taotie
```

然后在 Claude Code 中直接说"把 B 喂给 A"等触发意图即可启动。

---

### B. OpenClaw

```bash
git clone https://github.com/binggandata/bggg-skill-taotie ~/.openclaw/workspace/skills/bggg-skill-taotie
```

重启 OpenClaw session 后可用。

---

## 无需额外依赖

饕餮.skill 是纯 Prompt 驱动的 Skill，不依赖任何外部 Python 包或 Node 模块。它通过 Claude Code 的内置工具（Read、Write、Edit、Bash、Agent）完成所有工作。

---

## 目录结构说明

```
bggg-skill-taotie/           ← clone 到 .claude/skills/bggg-skill-taotie/
├── SKILL.md                  # skill 入口（官方 frontmatter + 完整指令）
├── references/               # 参考文档（分析框架 + 模式库）
│   ├── analysis-guide.md     # 六维分析指南
│   ├── patterns.md           # 模式库文档
│   └── pattern-library.json  # 模式库数据（随使用自动积累）
└── evals/                    # 测试用例
    └── evals.json
```

---

## 工作产物

饕餮的所有工作产物保存在 skill 所在项目目录下的 `bggg-skill-taotie-workspace/`：

```
bggg-skill-taotie-workspace/
├── session-YYYYMMDD-HHMMSS/   # 每次进化一个目录
│   ├── task-N/                 # 测试任务
│   │   ├── agent-a/            # A 的执行记录
│   │   └── agent-b/            # B 的执行记录
│   ├── comparison-report.md    # 对比报告
│   ├── reverse-engineering.md  # 反向工程报告
│   ├── snapshots/              # A 的版本快照
│   └── evolution-log.md        # 进化日志
```

---

## 快速验证

安装后，在 Claude Code 中输入以下任意内容验证 Skill 是否正确加载：

```
帮我对比一下 skill-A 和 skill-B
```

如果饕餮正确触发，会进入 Phase 1 开始读取你指定的两个 Skill。

---

## 更新

```bash
cd .claude/skills/bggg-skill-taotie   # 或你的安装路径
git pull
```
