# Specialist Reviewer Activation Catalog

The orchestrator reads the full diff and uses judgment (not keyword matching) to decide which specialists to activate. This catalog defines the signals to reason about.

## Always-On (no condition required)

The base /check skill runs as always-on. Specialist reviewers are additive.

## Conditional Specialists

### Security Reviewer

**Agent file:** `agents/reviewer-security.md`
**Activate at:** Standard or Deep depth

Activate when the diff touches:
- Authentication or authorization logic (middleware, guards, JWT handling, session management)
- Cryptographic operations (hashing, signing, encryption)
- Input handling at trust boundaries (form fields, API request bodies, URL parameters)
- File system operations on user-controlled paths
- Shell or subprocess execution
- Third-party credential or API key handling
- SQL queries or raw database access

**Do not activate** for: pure UI changes, config file updates, test-only changes, documentation.

### Architecture Reviewer

**Agent file:** `agents/reviewer-architecture.md`
**Activate at:** Standard or Deep depth

Activate when the diff:
- Adds a new module, package, or service boundary
- Changes a public API, exported type, or function signature
- Introduces a cross-module import that did not exist before
- Modifies more than 10 files across different directories
- Adds or removes a major dependency
- Restructures how components call each other

**Do not activate** for: single-file bug fixes, test additions, style changes, documentation updates.

## Adversarial Pass (Deep only)

No separate agent. The orchestrator runs this as an extra reasoning pass after all findings are collected.

**Activate at:** Deep depth only (500+ lines changed, or explicit high-risk signals: auth, payments, data mutation, external API integration).

Adversarial pass asks: "If I wanted to break this system through this specific diff, what would I do?"

Four attack angles:
1. **Assumption violation** -- What does this code assume is always true? (format, ordering, range) What happens when it is not?
2. **Composition failures** -- What breaks when this new code interacts with the existing system under concurrent load or partial failure?
3. **Cascade construction** -- What sequence of valid operations leads to an invalid state?
4. **Abuse cases** -- What happens on the 1000th request, during a deployment, with two users editing the same resource simultaneously?

Report adversarial findings with confidence score. Suppress findings below 0.60.
