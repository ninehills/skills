# LLM Skills Collection

Claude/Alma 技能集，覆盖开发、设计、部署、信息获取等场景。

## 安装

```bash
# 克隆仓库
git clone <repo-url> ~/skills

# 运行安装脚本（支持 Alma / Claude / Codex / Gemini）
bash ~/skills/install.sh
```

> 安装脚本会自动创建技能目录软链接，并在检测到 `~/.openclaw/openclaw.json` 时更新 `skills.load.extraDirs`。

## 技能目录

### 前端与 UI
| 技能 | 功能 |
|------|------|
| [frontend-design](./skills/frontend-design) | 生成生产级前端界面，避免 AI 审美 |
| [ui-ux-pro-max](./skills/ui-ux-pro-max) | UI/UX 设计系统（50+ 风格、21 配色、50 字体组合） |
| [ui-skills](./skills/ui-skills) | 界面构建的最佳实践约束 |
| [react-best-practices](./skills/react-best-practices) | React/Next.js 性能优化指南 |
| [pretty-mermaid](./skills/pretty-mermaid) | Mermaid 图表渲染（15+ 主题、SVG/ASCII） |
| [diagram-generator](./skills/diagram-generator) | Draw.io 图表生成 |

### 部署与运维
| 技能 | 功能 |
|------|------|
| [vercel-deploy](./skills/vercel-deploy) | 一键部署到 Vercel，无需认证 |

### 网络与信息获取
| 技能 | 功能 |
|------|------|
| [firecrawl](./skills/firecrawl) | 网络爬虫与搜索，替代所有内置 web 工具 |
| [web-search](./skills/web-search) | DuckDuckGo 搜索 |
| [web-fetch](./skills/web-fetch) | 网页内容获取 |
| [news-aggregator-skill](./skills/news-aggregator-skill) | 聚合 HN/GitHub Trending/36Kr/V2EX 等 8 个源 |
| [qmd-skill](./skills/qmd-skill) | Markdown 笔记本地混合搜索 |

### 浏览器自动化
| 技能 | 功能 |
|------|------|
| [browser](./skills/browser) | 控制真实 Chrome 浏览器（需 Chrome Relay） |
| [agent-browser](./skills/agent-browser) | Playwright 自动化浏览器测试与数据提取 |
| [screenshot](./skills/screenshot) | macOS 屏幕截图 |

### 任务与规划
| 技能 | 功能 |
|------|------|
| [planning-with-files](./skills/planning-with-files) | Manus 式文件规划 |
| [plan-mode](./skills/plan-mode) | 结构化规划模式 |
| [todo](./skills/todo) | Markdown 任务列表管理 |
| [tasks](./skills/tasks) | 全局多步任务追踪（跨线程持久化） |
| [scheduler](./skills/scheduler) | Cron 任务与定时提醒 |

### 内容处理
| 技能 | 功能 |
|------|------|
| [human](./skills/human) | 去除中文文本 AI 写作痕迹 |
| [ai-avoid](./skills/ai-avoid) | 去除英文文本 AI 写作痕迹 |
| [pua-debugging](./skills/pua-debugging) | PUA 激励调试引擎 |

### 代码开发
| 技能 | 功能 |
|------|------|
| [coding-agent](./skills/coding-agent) | 委托复杂编码任务到 Claude Code |
| [notebook](./skills/notebook) | Jupyter Notebook 编辑 |

### 文件与办公
| 技能 | 功能 |
|------|------|
| [spreadsheet](./skills/spreadsheet) | Excel/CSV 处理与格式化 |
| [pdf](./skills/pdf) | PDF 读取与生成 |
| [doc](./skills/doc) | Word 文档处理 |
| [file-manager](./skills/file-manager) | 文件搜索、整理、压缩 |
| [send-file](./skills/send-file) | 发送文件到当前聊天 |

### 社交与通讯
| 技能 | 功能 |
|------|------|
| [telegram](./skills/telegram) | Telegram Bot API 完整操作 |
| [discord](./skills/discord) | Discord 消息与群组管理 |
| [bird](./skills/bird) | X/Twitter 读写（推荐只读） |
| [reactions](./skills/reactions) | 消息表情回复 |

### 媒体与生成
| 技能 | 功能 |
|------|------|
| [image-gen](./skills/image-gen) | AI 图像生成与编辑 |
| [selfie](./skills/selfie) | 一致风格自拍生成 |
| [video-reader](./skills/video-reader) | 视频内容理解与转录 |
| [music-gen](./skills/music-gen) | AI 音乐生成（ACE-Step 本地） |
| [music-listener](./skills/music-listener) | 音乐文件分析与鉴赏 |
| [voice](./skills/voice) | Qwen3-TTS 本地语音合成 |

### 数据与金融
| 技能 | 功能 |
|------|------|
| [tvscreener](./skills/tvscreener) | TradingView 股票筛选器 |
| [stock_analysis](./skills/stock_analysis) | A股/基金/港股实时行情与技术分析 |

### 系统与工具
| 技能 | 功能 |
|------|------|
| [system-info](./skills/system-info) | 系统信息获取 |
| [skill-hub](./skills/skill-hub) | Alma 技能生态搜索与安装 |
| [memory-management](./skills/memory-management) | 记忆与对话历史管理 |
| [thread-management](./skills/thread-management) | 聊天线程管理 |
| [self-management](./skills/self-management) | Alma 配置管理 |
| [self-reflection](./skills/self-reflection) | 每日自我反思与成长 |
| [travel](./skills/travel) | 虚拟旅行系统 |

### Google Workspace
| 技能 | 功能 |
|------|------|
| [gogcli](./skills/gogcli) | Gmail/日历/云端硬盘/联系人/表格/文档 |

## 技能来源

| 技能 | 来源 |
|------|------|
| agent-browser | @vercel-labs/agent-browser |
| diagram-generator | @GBSOSS/ai-drawio |
| planning-with-files | @OthmanAdi/planning-with-files |
| ui-ux-pro-max | @nextlevelbuilder/ui-ux-pro-max-skill |
| ui-skills | @ibelick/ui-skills |
| vercel-deploy | @vercel-labs/agent-skills |
| react-best-practices | @vercel-labs/agent-skills |
| news-aggregator-skill | @cclank/news-aggregator-skill |
| qmd-skill | @levineam/qmd-skill |
| pretty-mermaid | @imxv/Pretty-mermaid-skills |
| firecrawl | @firecrawl/cli |
| frontend-design | @anthropics/skills |
| pua-debugging | @tanweai/pua |
| human | @op7418/Humanizer-zh |
| 其他 | 本地维护 |

## 添加新技能

1. 在 `skills/` 下创建目录，包含 `SKILL.md`：
```yaml
---
name: example
description: 简短描述，说明触发条件和功能
---
```

2. 更新本 README 的技能表格和来源表格

3. 提交时记录来源仓库地址
