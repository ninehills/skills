# Architecture: Why templates.py and gen.py are separate

## The mistake (v2.0)

Originally, template search, optimization, and prompt rendering were all in gen.py
as `--template`, `--optimize`, `--list-templates` flags. This was rejected because:

1. **Coupling**: one script did too many unrelated things
2. **No intelligence layer**: the script did mechanical keyword matching instead
   of letting the agent (LLM) understand the user's intent and select the right
   template slots
3. **Rigid**: hard to extend template logic without touching generation code

## The corrected design (v2.1+)

```
templates.py          gen.py
(search & show)       (generate only)
       \                /
        Agent (LLM)
        - reads template structure
        - understands user intent
        - fills template slots intelligently
        - crafts final prompt
        - calls gen.py
```

**Principles:**
- Scripts are thin tools — they don't make decisions
- The agent is the brain — it reads template structure, understands context,
  assembles the final prompt
- gen.py never imports templates.py and vice versa
- templates.py outputs JSON for agent consumption, not for piping to gen.py

## What NOT to do

Do NOT re-add `--template` or `--optimize` flags to gen.py. Do NOT add
auto-selection logic to either script. The agent handles all of that.
