// Reference output for agent/human review — not executed by tests.
// After the Shape 2 (inline-headers) CSP patch is applied, next.config.js
// should look like this.

/** @type {import('next').NextConfig} */

// Dev-only allowance so impeccable live mode can load. Empty string in any
// non-development environment.
const __impeccableLiveDev =
  process.env.NODE_ENV === "development" ? " http://localhost:8400" : "";

module.exports = {
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          {
            key: "Content-Security-Policy",
            value:
              "default-src 'self'; " +
              `script-src 'self' 'unsafe-inline' 'unsafe-eval'${__impeccableLiveDev}; ` +
              "style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: blob:; " +
              `connect-src 'self'${__impeccableLiveDev}; ` +
              "frame-ancestors 'self';",
          },
          { key: "X-Frame-Options", value: "SAMEORIGIN" },
        ],
      },
    ];
  },
};
