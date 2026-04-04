# Skills Collection

A curated collection of agent skills for Claude Code and compatible agent platforms.

---

## Quick Reference

| Skill | Purpose | Use When |
|-------|---------|----------|
| [agent-browser](#agent-browser) | Browser automation | Interacting with websites, scraping, testing web apps |
| [brainstorming](#brainstorming) | Design exploration | Starting any creative work, building features, components |
| [find-skills](#find-skills) | Skill discovery | Looking for tools, asking "how do I do X" |
| [frontend-design](#frontend-design) | Visual interface creation | Building web components, pages, landing pages |
| [frontend-skill](#frontend-skill) | Premium UI development | Art direction, award-level composition |
| [pua](#pua) | Performance coaching | User frustration, repeated failures, debugging |
| [skill-creator](#skill-creator) | Skill development | Creating or improving skills |
| [ui-ux-pro-max](#ui-ux-pro-max) | Professional design | High-end UI/UX work |
| [vercel-react-best-practices](#vercel-react-best-practices) | React optimization | Writing, reviewing React/Next.js code |
| [web-design-guidelines](#web-design-guidelines) | UI code review | Reviewing UI for accessibility and best practices |
| [writing-plans](#writing-plans) | Implementation planning | Multi-step tasks with clear specs |

---

## Skill Details

### agent-browser

**Purpose:** Browser automation CLI for AI agents.

**Use Cases:**
- Navigate websites and interact with pages
- Fill forms, click buttons, take screenshots
- Scrape data, test web applications
- Handle authentication flows

**Key Commands:**
- `agent-browser open <url>` — Navigate to URL
- `agent-browser snapshot -i` — Get interactive elements with refs
- `agent-browser click @e1` / `agent-browser fill @e2 "text"` — Interact via refs
- `agent-browser screenshot` — Capture page images

---

### brainstorming

**Purpose:** Collaborative design exploration before implementation.

**When to Use:** Before any creative work — creating features, building components, adding functionality, or modifying behavior.

**Process:**
1. Explore project context
2. Ask clarifying questions (one at a time)
3. Propose 2-3 approaches with trade-offs
4. Present design sections for approval
5. Write design doc to `docs/superpowers/specs/`
6. Invoke `writing-plans` for implementation

**Hard Gate:** Do NOT write code until design is approved.

---

### find-skills

**Purpose:** Discover and install skills from the open agent skills ecosystem.

**When to Use:**
- "How do I do X?"
- "Find a skill for X"
- "Is there a skill that can..."

**Key Commands:**
- `npx skills find [query]` — Search for skills
- `npx skills add <package>` — Install a skill
- Browse: https://skills.sh/

---

### frontend-design

**Purpose:** Create distinctive, production-grade frontend interfaces.

**When to Use:** Building web components, pages, artifacts, posters, or applications.

**Design Principles:**
- **Bold aesthetics** — Choose an extreme direction (minimalist, maximalist, retro-futuristic, etc.)
- **Typography** — Distinctive, characterful font choices
- **Motion** — Well-orchestrated animations, scroll-triggered effects
- **Spatial composition** — Unexpected layouts, asymmetry, overlap

**Avoid:** Generic AI aesthetics (Inter font, purple gradients, predictable layouts)

---

### frontend-skill

**Purpose:** Ship premium, award-level interfaces with art direction and restraint.

**When to Use:** When quality depends on art direction, hierarchy, imagery, and motion.

**Defaults:**
- Start with composition, not components
- Full-bleed hero, no card grids
- Brand-first hierarchy
- Two typefaces max, one accent color
- Cardless layouts preferred

**Litmus Checks:**
- Is the brand unmistakable in the first screen?
- Can the page be understood by scanning headlines only?
- Would the design feel premium without decorative shadows?

---

### pua

**Purpose:** High-agency problem-solving with performance coaching.

**When to Use:**
- User frustration or repeated failures (2+)
- Passive behavior or quality complaints
- Triggers: "try harder", "figure it out", "stop giving up", "you broke it"

**Methodology Routing:**
| Task Type | Flavor | Core Method |
|-----------|--------|-------------|
| Debug/Bug | Huawei | RCA root cause + Blue Team self-attack |
| New Feature | Musk | The Algorithm: question → delete → simplify → accelerate → automate |
| Code Review | Jobs | Subtraction priority + pixel-perfect + DRI |
| Research | Baidu | Search is primary productivity |
| Architecture | Amazon | Working Backwards + 6-Pager |
| Performance | ByteDance | A/B Test + data-driven |

---

### skill-creator

**Purpose:** Create, modify, and measure skill performance.

**When to Use:**
- Creating a skill from scratch
- Editing or optimizing existing skills
- Running evals to test skills
- Benchmarking skill performance

**Workflow:**
1. Capture intent (what, when, output format)
2. Interview and research edge cases
3. Write `SKILL.md` with YAML frontmatter
4. Create test cases in `evals/evals.json`
5. Run with-skill vs baseline subagents
6. Review, iterate, package

---

### ui-ux-pro-max

**Purpose:** Professional UI/UX design for high-end work.

**When to Use:** Complex UI/UX projects requiring professional design expertise.

---

### vercel-react-best-practices

**Purpose:** React and Next.js performance optimization from Vercel Engineering.

**When to Use:**
- Writing new React components or Next.js pages
- Implementing data fetching
- Reviewing code for performance
- Optimizing bundle size or load times

**Rule Categories (65 rules):**

| Priority | Category | Prefix |
|----------|----------|--------|
| 1 | Eliminating Waterfalls | `async-` |
| 2 | Bundle Size Optimization | `bundle-` |
| 3 | Server-Side Performance | `server-` |
| 4 | Client-Side Data Fetching | `client-` |
| 5 | Re-render Optimization | `rerender-` |
| 6 | Rendering Performance | `rendering-` |
| 7 | JavaScript Performance | `js-` |
| 8 | Advanced Patterns | `advanced-` |

---

### web-design-guidelines

**Purpose:** Review UI code for Web Interface Guidelines compliance.

**When to Use:**
- "Review my UI"
- "Check accessibility"
- "Audit design"
- "Review UX"

**Process:**
1. Fetch latest guidelines from source
2. Read specified files
3. Check against all rules
4. Output findings in `file:line` format

---

### writing-plans

**Purpose:** Write comprehensive implementation plans for multi-step tasks.

**When to Use:** You have a spec or requirements for a multi-step task, before touching code.

**Output:** Plans saved to `docs/superpowers/plans/YYYY-MM-DD-<feature-name>.md`

**Structure:**
- Bite-sized tasks (2-5 minutes each)
- Exact file paths
- Complete code in every step
- Exact commands with expected output
- DRY, YAGNI, TDD, frequent commits

---

## Installation

Install individual skills using the Skills CLI:

```bash
npx skills add owner/repo@skill-name
```

Or install from this repository:

```bash
# Clone the repository
git clone https://github.com/ninehills/skills.git
cd skills

# Install a specific skill
npx skills add ./agent-browser -g
```

---

## Skill Structure

Each skill follows this structure:

```
skill-name/
├── SKILL.md              # Required: skill definition with YAML frontmatter
├── references/           # Optional: documentation loaded as needed
├── scripts/              # Optional: executable code for deterministic tasks
└── assets/               # Optional: templates, icons, fonts
```

---

## License

See individual skill directories for license information.
