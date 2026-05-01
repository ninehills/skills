/**
 * Tests for live-inject.mjs — script-tag insert/remove round-trip.
 * Run with: node --test tests/live-inject.test.mjs
 */

import { describe, it, beforeEach, afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync, readFileSync, rmSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const __dirname = dirname(fileURLToPath(import.meta.url));
const INJECT = resolve(__dirname, '..', 'source/skills/impeccable/scripts/live-inject.mjs');

function runInject(cwd, configPath, args) {
  try {
    const out = execFileSync('node', [INJECT, ...args], {
      cwd,
      encoding: 'utf-8',
      env: { ...process.env, IMPECCABLE_LIVE_CONFIG: configPath },
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    return JSON.parse(out.trim());
  } catch (err) {
    const body = err.stdout?.toString().trim() || err.stderr?.toString().trim() || '';
    return JSON.parse(body || '{}');
  }
}

describe('live-inject — insert/remove round-trip preserves file bytes', () => {
  let tmp;
  beforeEach(() => { tmp = mkdtempSync(join(tmpdir(), 'impeccable-inject-test-')); });
  afterEach(() => { rmSync(tmp, { recursive: true, force: true }); });

  it('round-trips an HTML file without mangling indentation', () => {
    const original = `<!DOCTYPE html>
<html>
  <head><title>Test</title></head>
  <body>
    <main>
      <h1>Hello</h1>
    </main>
  </body>
</html>
`;
    const file = join(tmp, 'index.html');
    writeFileSync(file, original);

    const config = {
      files: ['index.html'],
      insertBefore: '</body>',
      commentSyntax: 'html',
    };
    const cfgPath = join(tmp, 'config.json');
    writeFileSync(cfgPath, JSON.stringify(config));

    runInject(tmp, cfgPath, ['--port', '8400']);
    runInject(tmp, cfgPath, ['--remove']);

    const after = readFileSync(file, 'utf-8');
    assert.equal(after, original, 'file should match original byte-for-byte after insert/remove');
  });

  it('round-trips a JSX layout without mangling indentation', () => {
    // Matches the EAC shape: indented </body> inside a typed RootLayout return.
    const original = `export default async function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        {children}
        <SpeedInsights />
      </body>
    </html>
  );
}
`;
    const file = join(tmp, 'layout.tsx');
    writeFileSync(file, original);

    const config = {
      files: ['layout.tsx'],
      insertBefore: '</body>',
      commentSyntax: 'jsx',
    };
    const cfgPath = join(tmp, 'config.json');
    writeFileSync(cfgPath, JSON.stringify(config));

    runInject(tmp, cfgPath, ['--port', '8400']);
    runInject(tmp, cfgPath, ['--remove']);

    const after = readFileSync(file, 'utf-8');
    assert.equal(after, original, 'JSX file should match original byte-for-byte after insert/remove');
  });

  it('round-trips multiple files at once', () => {
    const originals = {
      'a.html': `<html>
  <body>
    <p>A</p>
  </body>
</html>
`,
      'b.html': `<html>
  <body>
    <p>B</p>
  </body>
</html>
`,
    };
    for (const [name, body] of Object.entries(originals)) {
      writeFileSync(join(tmp, name), body);
    }
    const cfgPath = join(tmp, 'config.json');
    writeFileSync(cfgPath, JSON.stringify({
      files: ['a.html', 'b.html'],
      insertBefore: '</body>',
      commentSyntax: 'html',
    }));

    runInject(tmp, cfgPath, ['--port', '8400']);
    runInject(tmp, cfgPath, ['--remove']);

    for (const [name, body] of Object.entries(originals)) {
      const after = readFileSync(join(tmp, name), 'utf-8');
      assert.equal(after, body, `${name} should match original byte-for-byte after insert/remove`);
    }
  });

  it('round-trips with insertAfter — preserves indented opener line below it', () => {
    const original = `<!DOCTYPE html>
<html>
  <head>
    <title>Test</title>
  </head>
  <body>
    <main>
      <h1>Hello</h1>
    </main>
  </body>
</html>
`;
    const file = join(tmp, 'index.html');
    writeFileSync(file, original);

    const cfgPath = join(tmp, 'config.json');
    writeFileSync(cfgPath, JSON.stringify({
      files: ['index.html'],
      insertAfter: '<head>',
      commentSyntax: 'html',
    }));

    runInject(tmp, cfgPath, ['--port', '8400']);
    runInject(tmp, cfgPath, ['--remove']);

    const after = readFileSync(file, 'utf-8');
    assert.equal(after, original, 'insertAfter round-trip must restore original byte-for-byte');
  });

  it('round-trips through CSP-meta patch and revert (insert mutates the meta tag, remove restores it)', () => {
    // Mirrors a Vite app that ships a CSP meta tag in index.html. live-inject
    // appends `http://localhost:PORT` to script-src / connect-src on insert
    // and stashes the original directives in `data-impeccable-csp-original`.
    // --remove must restore the meta tag's original `content` exactly.
    const original = `<!DOCTYPE html>
<html>
  <head>
    <meta http-equiv="Content-Security-Policy" content="default-src 'self'; script-src 'self'; connect-src 'self';" />
    <title>CSP test</title>
  </head>
  <body>
    <main>
      <h1>Hello</h1>
    </main>
  </body>
</html>
`;
    const file = join(tmp, 'index.html');
    writeFileSync(file, original);

    const cfgPath = join(tmp, 'config.json');
    writeFileSync(cfgPath, JSON.stringify({
      files: ['index.html'],
      insertBefore: '</body>',
      commentSyntax: 'html',
    }));

    runInject(tmp, cfgPath, ['--port', '8400']);
    runInject(tmp, cfgPath, ['--remove']);

    const after = readFileSync(file, 'utf-8');
    assert.equal(after, original, 'CSP meta tag must round-trip exactly through insert+remove');
  });

  it('round-trips when the insert anchor has no leading indent (column-0 </body>)', () => {
    const original = `<html>
<body>
<p>Content</p>
</body>
</html>
`;
    const file = join(tmp, 'flat.html');
    writeFileSync(file, original);

    const cfgPath = join(tmp, 'config.json');
    writeFileSync(cfgPath, JSON.stringify({
      files: ['flat.html'],
      insertBefore: '</body>',
      commentSyntax: 'html',
    }));

    runInject(tmp, cfgPath, ['--port', '8400']);
    runInject(tmp, cfgPath, ['--remove']);

    const after = readFileSync(file, 'utf-8');
    assert.equal(after, original, 'column-0 anchor should round-trip cleanly too');
  });
});
