# IME / Unicode Debugging Reference

Recurring patterns in Tauri and native macOS apps. Check these before forming a hypothesis.

## IME State Desync

**Symptom**: Latin characters appear correctly but CJK input is dropped, doubled, or committed at the wrong time.

**Cause candidates**:
- Input method switch mid-composition: the IME commits the preedit with a stale target, then the new mode processes the same keystrokes again.
- `keydown` handler consuming events during active composition: check `event.isComposing` before acting on `keydown`/`keyup`. If `isComposing` is true, defer the action until `compositionend`.
- Webview + native frame split focus: in Tauri, the webview and the native window title bar can hold focus simultaneously. A click on a native control during IME composition triggers a focus-out, committing incomplete preedit text.

**Instruments**:
- Log `compositionstart`, `compositionupdate`, `compositionend` sequence; confirm they fire in order without gaps.
- Log the `data` field of each `compositionupdate`; a sudden empty string signals a forced commit.

## Cursor Position Drift After IME Commit

**Symptom**: After confirming a CJK word, the cursor jumps to the wrong position or the selection collapses.

**Cause candidates**:
- DOM mutation during composition: React/Svelte/Vue re-rendering while `isComposing` is true will reset the selection. Batch state updates and flush only on `compositionend`.
- Counting bytes instead of code points in position math: CJK characters are multi-byte in UTF-8. Use `Array.from(str).length` or `[...str].length`, not `str.length`, for character-level offsets in positions.

## Emoji ZWJ Sequence Splitting

**Symptom**: Multi-person or profession emoji (e.g. `👩‍🚒`) renders as two or three separate emoji, or the ZWJ (`U+200D`) appears as a visible character.

**Cause candidates**:
- String sliced at byte offset: `str.slice(0, n)` splits a ZWJ sequence if `n` falls inside the sequence. Use `Intl.Segmenter` with `granularity: 'grapheme'` to split at grapheme cluster boundaries.
- Font does not support the sequence: the font renders each code point individually. Verify with `canvas.measureText` or by checking which font is actually used via `document.fonts`.
- Serialization strips ZWJ: some JSON encoders normalize or escape `U+200D`. Verify the raw bytes of the stored string.

**Test**: `[...'👩‍🚒'].length` should be 1 (one grapheme cluster). If it returns 3, the runtime is iterating code points, not grapheme clusters.

## `compositionend` / `keydown` Event Ordering

**Symptom**: The action bound to Enter or Tab fires during IME confirmation, submitting incomplete input.

**Cause**: On macOS + some IMEs, the sequence is `compositionend` → `keydown(Enter)`. On Windows + other IMEs it can be `keydown(Enter)` → `compositionend`. Code that blocks Enter only when `isComposing` is true will break on the macOS ordering because `isComposing` is already false when `keydown` fires.

**Fix**: Track composition state with a boolean flag set on `compositionstart`, cleared on `compositionend`. Guard the Enter handler with that flag rather than `event.isComposing`.

## macOS Text System vs Webview Conflict

**Symptom**: Undo (`Cmd+Z`) reverts individual IME preedit characters instead of committed words, or system text shortcuts (Cmd+Shift+Left for word selection) behave differently inside vs outside the webview.

**Cause**: WKWebView has its own text system that partially overlaps with NSTextView conventions. Tauri's `preventDefaultFor` config can suppress system shortcuts; check `tauri.conf.json` (v1) or `app.json` (v2) for any `preventDefault` rules that may be too broad.

## Quick Checklist

- [ ] `isComposing` checked before acting on keyboard events?
- [ ] No DOM mutation while `isComposing` is true?
- [ ] String position math uses grapheme clusters, not bytes or code points?
- [ ] ZWJ sequences verified with `Intl.Segmenter`?
- [ ] Enter/Tab guard uses a flag set by `compositionstart`, not `event.isComposing`?
- [ ] `tauri.conf.json` `preventDefaultFor` not too broad?
