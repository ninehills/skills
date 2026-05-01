/**
 * Markdown → HTML rendering for sub-pages.
 *
 * Wraps `marked` with a custom link renderer that resolves cross-references
 * between skill bodies and their references, and emits stable heading slugs
 * so anti-pattern → skill section anchors work.
 *
 * Skeleton in commit 1. Link resolution and heading slugger are wired up in
 * commit 3 (skills generator) when the data model lands.
 */

import { marked } from 'marked';

/**
 * Slugify a heading text into a stable anchor id.
 * Matches the convention: lowercase, strip non-alphanum, spaces → dashes.
 *
 * @param {string} text
 * @returns {string}
 */
export function slugify(text) {
  return String(text)
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, '')
    .replace(/[\s_]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

/**
 * Build a marked renderer configured for impeccable's skill/tutorial bodies.
 *
 * @param {object} opts
 * @param {Set<string>} [opts.knownSkillIds] - slugs of skills the site knows about; unknown /name mentions render as plain text
 * @param {string}      [opts.currentSkillId] - when rendering a skill body, resolve `reference/foo.md` to `#reference-foo` on the current page
 * @returns {import('marked').Renderer}
 */
export function createRenderer({ knownSkillIds = new Set(), currentSkillId = null } = {}) {
  const renderer = new marked.Renderer();

  // Heading slugger — stable ids so we can anchor-link from elsewhere.
  // Supports {#custom-id} suffix (kramdown/pandoc style) for explicit anchors.
  renderer.heading = ({ tokens, depth }) => {
    const raw = tokens.map((t) => t.raw || '').join('');
    const customIdMatch = raw.match(/\s*\{#([a-z0-9_-]+)\}\s*$/i);
    let id, text;
    if (customIdMatch) {
      id = customIdMatch[1];
      // Strip the {#id} suffix from the rendered text
      const cleanRaw = raw.slice(0, customIdMatch.index);
      text = renderer.parser.parseInline(marked.lexer(cleanRaw, { gfm: true })[0]?.tokens || tokens);
    } else {
      id = slugify(raw);
      text = renderer.parser.parseInline(tokens);
    }
    return `<h${depth} id="${id}">${text}</h${depth}>\n`;
  };

  // Link resolver.
  renderer.link = ({ href, title, tokens }) => {
    const text = renderer.parser.parseInline(tokens);
    const resolved = resolveHref(href, { knownSkillIds, currentSkillId });
    const titleAttr = title ? ` title="${escapeAttr(title)}"` : '';
    const relAttr = resolved.external ? ' target="_blank" rel="noopener"' : '';
    return `<a href="${escapeAttr(resolved.href)}"${titleAttr}${relAttr}>${text}</a>`;
  };

  // Fenced code blocks — minimal glass-terminal styling, no syntax highlighter in v1.
  // Wrapped in a container with a copy button; click handling lives in the
  // page-level inline script added by render-page.js.
  renderer.code = ({ text, lang }) => {
    const langClass = lang ? ` code-block--${escapeAttr(lang)}` : '';
    const copyValue = escapeAttr(text);
    return `<div class="code-block-wrap"><pre class="code-block${langClass}"><code>${escapeHtml(text)}</code></pre><button class="code-block-copy" type="button" data-copy="${copyValue}" aria-label="Copy to clipboard"></button></div>\n`;
  };

  return renderer;
}

/**
 * Resolve a markdown link href against the site's URL scheme.
 *
 * - `http(s)://…` → unchanged, external
 * - `reference/foo.md` → `#reference-foo` on current skill page
 * - `/skill-id` (known) → `/docs/skill-id`
 * - `#anchor` → unchanged (in-page anchor)
 * - anything else → unchanged (will be caught by build warnings later)
 *
 * @param {string} href
 * @param {{ knownSkillIds: Set<string>, currentSkillId: string|null }} ctx
 * @returns {{ href: string, external: boolean }}
 */
function resolveHref(href, { knownSkillIds, currentSkillId }) {
  if (!href) return { href: '', external: false };

  // External links
  if (/^https?:\/\//i.test(href) || /^mailto:/i.test(href)) {
    return { href, external: true };
  }

  // In-page anchor
  if (href.startsWith('#')) {
    return { href, external: false };
  }

  // reference/foo.md → #reference-foo on the current skill page
  const refMatch = href.match(/^reference\/([a-z0-9-]+)\.md$/i);
  if (refMatch && currentSkillId) {
    return { href: `#reference-${refMatch[1].toLowerCase()}`, external: false };
  }

  // /skill-id mentioned in prose (e.g. "run /polish")
  const slashMatch = href.match(/^\/([a-z0-9-]+)$/i);
  if (slashMatch && knownSkillIds.has(slashMatch[1])) {
    return { href: `/docs/${slashMatch[1]}`, external: false };
  }

  // [text](other-skill) → /docs/other-skill
  if (/^[a-z0-9-]+$/i.test(href) && knownSkillIds.has(href)) {
    return { href: `/docs/${href}`, external: false };
  }

  // Unknown — pass through. Generator can warn separately.
  return { href, external: false };
}

/**
 * Render a markdown string to HTML.
 *
 * @param {string} markdown
 * @param {object} [opts]
 * @param {Set<string>} [opts.knownSkillIds]
 * @param {string}      [opts.currentSkillId]
 * @returns {string} HTML
 */
export function renderMarkdown(markdown, opts = {}) {
  const renderer = createRenderer(opts);
  return marked.parse(markdown, {
    renderer,
    gfm: true,
    breaks: false,
  });
}

function escapeHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function escapeAttr(str) {
  return String(str).replace(/"/g, '&quot;');
}
