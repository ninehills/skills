/**
 * Page template wrapper for generated sub-pages.
 *
 * Reads the shared site header partial once and wraps content bodies with
 * a minimal HTML scaffold that imports tokens.css + sub-pages.css.
 *
 * Used by scripts/build-sub-pages.js (wired up in commit 3).
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT_DIR = path.resolve(__dirname, '..', '..');
const HEADER_PARTIAL = path.join(ROOT_DIR, 'content', 'site', 'partials', 'header.html');

let cachedHeader = null;

/**
 * Read the shared site header partial.
 * Cached after first read.
 */
export function readHeaderPartial() {
  if (cachedHeader === null) {
    cachedHeader = fs.readFileSync(HEADER_PARTIAL, 'utf8').trim();
  }
  return cachedHeader;
}

/**
 * Mark a nav item as current by adding aria-current="page" and removing
 * the default nav href state. Matches on `data-nav="{activeNav}"`.
 *
 * @param {string} headerHtml
 * @param {string} activeNav - one of: home, designing, docs, slop, live, github
 * @returns {string}
 */
export function applyActiveNav(headerHtml, activeNav) {
  if (!activeNav) return headerHtml;
  return headerHtml.replace(
    new RegExp(`data-nav="${activeNav}"`, 'g'),
    `data-nav="${activeNav}" aria-current="page"`,
  );
}

/**
 * Wrap body HTML in a full page shell.
 *
 * @param {object} opts
 * @param {string}   opts.title         - <title> text
 * @param {string}   opts.description   - meta description
 * @param {string}   opts.bodyHtml      - main content HTML (will be placed inside <main>)
 * @param {string}   [opts.activeNav]   - which nav item to mark current
 * @param {string}   [opts.canonicalPath] - relative URL path for <link rel="canonical">
 * @param {string}   [opts.extraHead]   - raw HTML to inject into <head>
 * @param {string}   [opts.bodyClass]   - optional class on <body>
 * @param {number}   [opts.assetDepth]  - how many `..` to prepend for Bun's HTML loader to resolve on-disk paths. 1 = page is one dir deep under public/ (e.g. public/skills/polish.html). Defaults to 1.
 * @returns {string} full HTML document
 */
export function renderPage({
  title,
  description,
  bodyHtml,
  activeNav,
  canonicalPath,
  extraHead = '',
  bodyClass = 'sub-page',
  assetDepth = 1,
}) {
  const header = applyActiveNav(readHeaderPartial(), activeNav);
  const safeTitle = escapeHtml(title);
  const safeDesc = escapeAttr(description || '');
  const canonical = canonicalPath
    ? `<link rel="canonical" href="https://impeccable.style${canonicalPath}">`
    : '';

  // Relative prefix for on-disk resolution by Bun's HTML loader.
  // Bun rewrites these to hashed absolute URLs at build time, so runtime
  // serving works regardless of the request path.
  const rel = assetDepth > 0 ? '../'.repeat(assetDepth) : './';

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>${safeTitle}</title>
  <meta name="description" content="${safeDesc}">
  <meta name="theme-color" content="#fafafa">
  ${canonical}
  <link rel="icon" type="image/svg+xml" href="${rel}favicon.svg">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Cormorant+Garamond:ital,wght@0,400;0,600;1,400&family=Instrument+Sans:wght@400;500;600;700&family=Space+Grotesk:wght@400;500;600&display=swap" rel="stylesheet">
  <link rel="stylesheet" href="${rel}css/sub-pages.css">
  ${extraHead}
</head>
<body class="${bodyClass}">
  <a href="#main" class="skip-link">Skip to content</a>
  ${header}
  <main id="main">
${bodyHtml}
  </main>
  <script>
    // Copy buttons on rendered code blocks
    document.addEventListener('click', (e) => {
      const btn = e.target.closest('[data-copy]');
      if (!btn) return;
      const text = btn.getAttribute('data-copy');
      if (!text) return;
      navigator.clipboard.writeText(text).then(() => {
        btn.classList.add('is-copied');
        setTimeout(() => btn.classList.remove('is-copied'), 1500);
      }).catch(() => {});
    });

    // Mobile sidebar toggle (shown on narrow viewports, hidden on desktop).
    document.addEventListener('click', (e) => {
      const toggle = e.target.closest('.skills-sidebar-toggle');
      if (!toggle) return;
      const expanded = toggle.getAttribute('aria-expanded') === 'true';
      toggle.setAttribute('aria-expanded', String(!expanded));
    });

    // Before/after split-compare: drag on touch, hover OR drag on mouse.
    // Pointer events attach to the padded .split-comparison wrapper so
    // there is a ~20px invisible buffer around the visible box. The
    // divider only snaps back when the pointer leaves that outer buffer.
    (function initSplitCompare() {
      const wrappers = document.querySelectorAll('.split-comparison');
      if (wrappers.length === 0) return;
      const hasHover = matchMedia('(hover: hover)').matches;
      const DEFAULT_POSITION = 50;

      for (const wrapper of wrappers) {
        const container = wrapper.querySelector('.split-container');
        const splitAfter = wrapper.querySelector('.split-after');
        const splitDivider = wrapper.querySelector('.split-divider');
        if (!container || !splitAfter || !splitDivider) continue;

        const tanAngle = Math.tan(10 * Math.PI / 180);
        let skewOffset = 8;
        const recalcSkew = () => {
          const r = container.getBoundingClientRect();
          if (r.width > 0 && r.height > 0) {
            skewOffset = 50 * r.height * tanAngle / r.width;
          }
        };
        recalcSkew();
        window.addEventListener('resize', recalcSkew, { passive: true });

        let targetX = DEFAULT_POSITION;
        let currentX = DEFAULT_POSITION;
        let rafId = null;

        const paint = (pct) => {
          const x = Math.max(-skewOffset, Math.min(100 + skewOffset, pct));
          splitAfter.style.clipPath =
            \`polygon(\${x + skewOffset}% 0%, 100% 0%, 100% 100%, \${x - skewOffset}% 100%)\`;
          splitDivider.style.left = \`\${x}%\`;
        };

        const step = () => {
          currentX += (targetX - currentX) * 0.2;
          if (Math.abs(targetX - currentX) < 0.1) {
            currentX = targetX;
            rafId = null;
          } else {
            rafId = requestAnimationFrame(step);
          }
          paint(currentX);
        };

        const setTarget = (pct) => {
          targetX = pct;
          if (rafId === null) rafId = requestAnimationFrame(step);
        };

        paint(DEFAULT_POSITION);

        // Percentage is always relative to the VISIBLE .split-container,
        // not the padded .split-comparison wrapper. The pointer event
        // target is the wrapper but the clip-path math uses the inner box.
        const pctFromClientX = (clientX) => {
          const rect = container.getBoundingClientRect();
          return ((clientX - rect.left) / rect.width) * 100;
        };

        let hovering = false;
        let dragging = false;

        wrapper.addEventListener('pointerenter', (e) => {
          if (hasHover && e.pointerType === 'mouse') {
            hovering = true;
          }
        });

        wrapper.addEventListener('pointerdown', (e) => {
          dragging = true;
          wrapper.setPointerCapture(e.pointerId);
          setTarget(pctFromClientX(e.clientX));
        });

        wrapper.addEventListener('pointermove', (e) => {
          if (dragging || hovering) {
            setTarget(pctFromClientX(e.clientX));
          }
        });

        const endDrag = (e) => {
          if (dragging) {
            dragging = false;
            try { wrapper.releasePointerCapture(e.pointerId); } catch {}
          }
        };

        wrapper.addEventListener('pointerup', endDrag);
        wrapper.addEventListener('pointercancel', endDrag);

        wrapper.addEventListener('pointerleave', (e) => {
          endDrag(e);
          if (hovering) {
            hovering = false;
            setTarget(DEFAULT_POSITION);
          }
        });
      }
    })();
  </script>
</body>
</html>
`;
}

function escapeHtml(str) {
  return String(str || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function escapeAttr(str) {
  return String(str || '').replace(/"/g, '&quot;');
}
