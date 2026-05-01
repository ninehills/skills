// Nuxt 3 config with CSP applied via routeRules headers.
// Representative of the "append-string" shape: CSP is a literal value string.
export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  devtools: { enabled: true },
  routeRules: {
    '/**': {
      headers: {
        'Content-Security-Policy':
          "default-src 'self'; " +
          "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
          "style-src 'self' 'unsafe-inline'; " +
          "img-src 'self' data: blob:; " +
          "connect-src 'self'; " +
          "frame-ancestors 'self';",
        'X-Frame-Options': 'SAMEORIGIN',
      },
    },
  },
});
