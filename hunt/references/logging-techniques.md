# Logging Techniques for Debugging

Use logs as a scalpel, not as noise. The goal is to answer a yes/no question about a hypothesis, not to dump state.

## Binary Search Instrumentation

Place the first log at the midpoint of the execution path, not at the symptom. If it fires correctly, move downstream. If it does not fire, move upstream. This is binary search over the call graph.

```
Entry → [midpoint log] → ... → symptom
         ^
         First log here, not at the symptom
```

Repeat: each log cuts the remaining unknown path in half.

## Log What Discriminates, Not What Is Convenient

Before adding a log, write the question it must answer:

> "If this prints X before Y, hypothesis A holds. If it prints Y first, A is wrong."

Discriminating log content:
- Sequence number or timestamp (ordering)
- Input identity key (which request/item)
- Branch taken (which `if` arm)
- Old vs. new state transition (not just new value)
- Error code plus context string (not just the exception message)

Never log: full request/response bodies, credentials, PII, or huge JSON blobs.

## Log the Boundary, Not the Interior

Log at system boundaries where behavior should be predictable:

- Request handler entry/exit
- Cache read (hit or miss) with key
- State setter (old value, new value, caller)
- Async callback entry
- External API call result
- Build step start/end

Interior logs (inside tight loops or low-level helpers) are noise unless the hypothesis is specifically about that interior.

## Prefix Discipline

Use a consistent log prefix to filter by context:

```
[hunt:auth] token validate: user=42 result=expired
[hunt:cache] miss: key=user:42 latency=12ms
[hunt:render] phase=layout height=842px overflow=yes
```

Gate verbose logging behind a debug flag when the project already has one. Remove temporary logs before finishing.

## Timing Bug Logging

For race conditions, flicker, or intermittent failures, log:
- Event identity (which event source)
- Timestamp (or monotonic counter)
- Start and end (not just "it ran")
- Thread/task/queue identity when concurrency is involved

If adding a log changes the behavior, treat that as evidence of a timing, lifecycle, or concurrency problem. Do not dismiss it as "just logging side effects."

## Removing Logs

After the root cause is confirmed:
1. Remove all temporary logs.
2. If a log is genuinely useful in production, move it behind the project's debug flag or logger level.
3. Do not leave `console.log`, `print`, or `fmt.Println` in shipped code paths unless the project keeps debug instrumentation there.
