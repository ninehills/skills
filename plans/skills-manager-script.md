# Skills Manager 脚本方案

## 背景

`.skills-manager` 目录包含一个技能/场景管理系统：
- **技能定义** (`.skills-manager/skills/*.json`)：每个包含 `skill_id`、`path`（仓库根目录下的文件夹名）
- **场景定义** (`.skills-manager/scenarios/*.json`)：每个包含 `scenario_id`、`name`
- **场景-技能映射** (`.skills-manager/scenario-skills/<scenario_id>/*.json`)：每个将技能关联到场景

需要一个纯 Python 脚本（无第三方依赖），支持：
1. `scenarios list` — 列出所有场景名称
2. `scenarios install <name>` — 通过软连接将场景的技能安装到目标目录，并清理无效软连接

## 方案

单文件 Python 脚本，仅使用标准库（`json`、`os`、`pathlib`、`sys`）。

### 数据模型

- 仓库根目录 = 脚本所在目录
- `.skills-manager/` 相对于仓库根目录
- 四个默认目标目录：
  - `~/.codex/skills`
  - `~/.pi/agent/skills`
  - `~/.claude/skills`
  - `~/.hermes/skills`

### 子命令

#### `scenarios list`
1. 读取 `.skills-manager/scenarios/` 下所有 `.json` 文件
2. 打印每个场景的 `name` 字段

#### `scenarios install <name>`
1. 在 `.skills-manager/scenarios/` 中按 `name` 查找场景
2. 收集 `.skills-manager/scenario-skills/<scenario_id>/` 下所有技能映射
3. 对每个映射：
   - 从 `.skills-manager/skills/<skill_id>.json` 读取技能定义
   - 获取 `path`（仓库根目录下的文件夹名）
   - **忽略 `tools` 字段，统一安装到全部四个目标目录**
   - 对每个目标目录：
     - 必要时创建父目录
     - 移除目标位置已存在的软连接/文件
     - 创建软连接：`<目标目录>/<path>` → `<仓库根目录>/<path>`
4. 清理：遍历每个已存在的目标目录：
   - 检查所有软连接
   - 如果软连接指向当前仓库内、但该技能目录名不在当前场景的技能路径集合中 → 删除该软连接

## 需修改的文件

- **新建**：`skills-manager`（仓库根目录下的 Python 脚本，无扩展名）
- **修改**：`README.md` — 添加 CLI 工具的简要使用说明

## 可复用的现有结构

- `.skills-manager/scenarios/*.json` — 场景定义
- `.skills-manager/skills/*.json` — 技能定义，`path` 字段指向仓库根目录下的文件夹
- `.skills-manager/scenario-skills/<scenario_id>/*.json` — 场景-技能映射

## 实施步骤

- [ ] 创建 `skills-manager` 脚本，包含数据加载辅助函数
- [ ] 实现 `scenarios list` 子命令
- [ ] 实现 `scenarios install <name>` 子命令（含软连接创建）
- [ ] 实现无效软连接清理逻辑
- [ ] 修改 `README.md`，添加 CLI 工具使用说明
- [ ] 测试：`python3 skills-manager scenarios list` 和 `python3 skills-manager scenarios install Common`

## 验证方式

1. 运行 `python3 skills-manager scenarios list` — 应打印 "Common"（及其他场景）
2. 运行 `python3 skills-manager scenarios install Common` — 应在目标目录中创建软连接
3. 验证软连接：`ls -la ~/.codex/skills/`、`ls -la ~/.claude/skills/` 等
4. 验证清理：在目标目录中手动创建一个指向仓库但不在 Common 场景中的软连接，重新运行 install，确认被删除
