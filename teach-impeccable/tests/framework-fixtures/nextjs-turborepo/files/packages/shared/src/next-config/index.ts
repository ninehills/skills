/**
 * Shared Next.js configuration utilities
 *
 * Reusable configuration for Next.js apps in a monorepo: CSP headers,
 * webpack tweaks, remote image patterns, rewrites.
 *
 * Derived from a real monorepo shape — sanitized of company-specific identifiers
 * but structurally identical so patch mechanics get tested against realistic
 * CSP/rewrite layering.
 */

import type { NextConfig } from "next";
import {
  buildConnectSrc,
  getSupabaseOrigin,
  HSTS_VALUE,
  PERMISSIONS_POLICY_VALUE,
} from "../security/origins";

export interface SharedNextConfigOptions {
  appName?: string;
  additionalImgSrc?: string[];
  additionalConnectSrc?: string[];
  additionalScriptSrc?: string[];
  enableMapbox?: boolean;
  serverExternalPackages?: string[];
  optimizePackageImports?: string[];
  transpilePackages?: string[];
}

const DEFAULT_WORKSPACE_TRANSPILE_PACKAGES = [
  "@app/backend",
  "@app/database",
  "@app/mcp-ui",
  "@app/shared",
  "@app/supabase-client",
  "@app/ui",
];

const KNOWN_SUPABASE_CUSTOM_DOMAINS = [
  "db.example.com",
  "db-staging.example.com",
] as const;

const KNOWN_SUPABASE_CUSTOM_ORIGINS = KNOWN_SUPABASE_CUSTOM_DOMAINS.map(
  (h) => `https://${h}`
);

interface CSPConfig {
  scriptSrc: string;
  styleSrc: string;
  imgSrc: string;
  fontSrc: string;
  connectSrc: string;
  frameSrc: string;
  workerSrc: string;
  childSrc: string;
}

function buildCSPConfig(options: SharedNextConfigOptions = {}): CSPConfig {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "";
  const supabaseOrigin = getSupabaseOrigin();
  const connectSrcBase = buildConnectSrc(apiUrl);

  const isPreview = true;

  const scriptSrc = [
    "'self'",
    "'unsafe-eval'",
    "'unsafe-inline'",
    "https://va.vercel-scripts.com",
    ...(isPreview ? ["https://vercel.live"] : []),
    ...(options.additionalScriptSrc || []),
  ].join(" ");

  const styleSrc = [
    "'self'",
    "'unsafe-inline'",
    ...(isPreview ? ["https://fonts.googleapis.com"] : []),
  ].join(" ");

  const imgSrc = [
    "'self'",
    "data:",
    "blob:",
    "http://localhost:54321",
    "http://127.0.0.1:54321",
    "https://*.supabase.co",
    ...KNOWN_SUPABASE_CUSTOM_ORIGINS,
    ...(supabaseOrigin &&
    !KNOWN_SUPABASE_CUSTOM_ORIGINS.includes(
      supabaseOrigin as (typeof KNOWN_SUPABASE_CUSTOM_ORIGINS)[number]
    )
      ? [supabaseOrigin]
      : []),
    ...(options.enableMapbox
      ? ["https://api.mapbox.com", "https://*.tiles.mapbox.com"]
      : []),
    ...(isPreview ? ["https://vercel.com", "https://vercel.live"] : []),
    ...(options.additionalImgSrc || []),
  ].join(" ");

  const fontSrc = [
    "'self'",
    "data:",
    ...(options.enableMapbox ? ["https://api.mapbox.com"] : []),
    ...(isPreview ? ["https://fonts.gstatic.com", "https://vercel.live"] : []),
  ].join(" ");

  const connectExtras = [
    ...(options.enableMapbox
      ? [
          "https://api.mapbox.com",
          "https://*.tiles.mapbox.com",
          "https://events.mapbox.com",
        ]
      : []),
    ...(isPreview
      ? ["https://vercel.live", "wss://*.pusher.com", "https://*.pusher.com"]
      : []),
    ...(options.additionalConnectSrc || []),
  ];

  const connectSrc = [...connectSrcBase, ...connectExtras].join(" ");
  const frameSrc = isPreview ? "'self' https://vercel.live" : "'self'";
  const workerSrc = "'self' blob:";
  const childSrc = "'self' blob:";

  return {
    scriptSrc,
    styleSrc,
    imgSrc,
    fontSrc,
    connectSrc,
    frameSrc,
    workerSrc,
    childSrc,
  };
}

export function buildSecurityHeaders(
  options: SharedNextConfigOptions = {}
): NextConfig["headers"] {
  return () => {
    const csp = buildCSPConfig(options);
    const isPreview = true;

    return Promise.resolve([
      {
        source: "/(.*)",
        headers: [
          {
            key: "Content-Security-Policy",
            value: `default-src 'self'; script-src ${csp.scriptSrc}; style-src ${csp.styleSrc}; img-src ${csp.imgSrc}; font-src ${csp.fontSrc}; connect-src ${csp.connectSrc}; frame-src ${csp.frameSrc}; worker-src ${csp.workerSrc}; child-src ${csp.childSrc}; frame-ancestors 'self';`,
          },
          { key: "X-Frame-Options", value: "SAMEORIGIN" },
          { key: "Strict-Transport-Security", value: HSTS_VALUE },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "Permissions-Policy", value: PERMISSIONS_POLICY_VALUE },
          { key: "Cross-Origin-Embedder-Policy", value: "unsafe-none" },
          { key: "Cross-Origin-Resource-Policy", value: "cross-origin" },
          {
            key: "Cross-Origin-Opener-Policy",
            value: "same-origin-allow-popups",
          },
          ...(isPreview
            ? [
                {
                  key: "Access-Control-Allow-Origin",
                  value: "https://vercel.live",
                },
                {
                  key: "Access-Control-Allow-Methods",
                  value: "GET,POST,PUT,PATCH,DELETE,OPTIONS",
                },
                { key: "Access-Control-Allow-Headers", value: "*" },
                { key: "Access-Control-Allow-Credentials", value: "true" },
                { key: "Vary", value: "Origin" },
              ]
            : []),
        ],
      },
    ]);
  };
}

export function buildApiProxyRewrites(): NextConfig["rewrites"] {
  return () => {
    const rewrites: Array<{ source: string; destination: string }> = [];

    if (
      process.env.NEXT_PUBLIC_API_PROXY === "1" &&
      process.env.NEXT_PUBLIC_API_URL
    ) {
      const proxyPath = process.env.NEXT_PUBLIC_API_PROXY_PATH || "/backend";
      rewrites.push({
        source: `${proxyPath}/:path*`,
        destination: `${process.env.NEXT_PUBLIC_API_URL}/:path*`,
      });
    }

    return {
      beforeFiles: rewrites,
      afterFiles: [],
      fallback: [],
    };
  };
}

type WebpackConfig = Parameters<NonNullable<NextConfig["webpack"]>>[0];
type WebpackContext = Parameters<NonNullable<NextConfig["webpack"]>>[1];

export function buildWebpackConfig(
  options: SharedNextConfigOptions = {}
): NextConfig["webpack"] {
  return (config: WebpackConfig, context: WebpackContext) => {
    if (context.isServer && options.serverExternalPackages?.length) {
      config.externals = config.externals || [];
      if (Array.isArray(config.externals)) {
        for (const pkg of options.serverExternalPackages) {
          config.externals.push({ [pkg]: `commonjs ${pkg}` });
        }
      }
    }
    return config;
  };
}

export function buildSupabaseRemotePatterns(): Array<{
  protocol: "http" | "https";
  hostname: string;
  port?: string;
  pathname: string;
}> {
  return [
    {
      protocol: "http",
      hostname: "localhost",
      port: "54321",
      pathname: "/storage/v1/object/public/**",
    },
    {
      protocol: "https",
      hostname: "*.supabase.co",
      pathname: "/storage/v1/object/public/**",
    },
    ...KNOWN_SUPABASE_CUSTOM_DOMAINS.map((hostname) => ({
      protocol: "https" as const,
      hostname,
      pathname: "/storage/v1/object/public/**",
    })),
  ];
}

/**
 * Create a shared Next.js configuration base. Apps extend this via spread.
 */
export function createBaseNextConfig(
  options: SharedNextConfigOptions = {}
): NextConfig {
  const transpilePackages = Array.from(
    new Set([
      ...DEFAULT_WORKSPACE_TRANSPILE_PACKAGES,
      ...(options.transpilePackages || []),
    ])
  );

  return {
    experimental: {
      turbopackFileSystemCacheForDev: true,
      ...(options.optimizePackageImports && {
        optimizePackageImports: options.optimizePackageImports,
      }),
    },
    env: {
      VERCEL_RELATED_PROJECTS: process.env.VERCEL_RELATED_PROJECTS || "",
      VERCEL_ENV: process.env.VERCEL_ENV || "",
    },
    ...(options.serverExternalPackages && {
      serverExternalPackages: options.serverExternalPackages,
    }),
    transpilePackages,
    webpack: buildWebpackConfig(options),
    headers: buildSecurityHeaders(options),
    rewrites: buildApiProxyRewrites(),
  };
}
