// Pretend generator: reads source content, writes HTML into dist/.
// Not executed by the test — it only exists so the fixture has a "source"
// that the agent could reason about when picking where to write variants.
export function render(title, body) {
  return `<!DOCTYPE html>
<html><head><title>${title}</title></head>
<body>${body}</body></html>`;
}
