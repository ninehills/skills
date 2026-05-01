/*
  Shared security helpers for CORS/CSP across apps.
  Configure once via env: ALLOWED_ORIGINS (CSV), ALLOW_VERCEL_PREVIEWS (1/0),
  VERCEL_TEAM_SLUG, ALLOW_LOCALHOST_ORIGINS (1/0).

  Derived from a real monorepo shape — sanitized of company-specific identifiers
  but structurally identical so patch mechanics get tested against realistic
  CSP/rewrite layering.
*/

export interface CorsPolicyOptions {
  allowVercelPreviews?: boolean;
  vercelTeamSlug?: string;
  allowLocalhost?: boolean;
}

export function parseCsvEnv(value?: string | null): string[] {
  return (value || "")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
}

export function getBaseAllowedOrigins(): string[] {
  const fromCsv = parseCsvEnv(process.env.ALLOWED_ORIGINS);
  return Array.from(new Set([...fromCsv]));
}

const LOCALHOST_REGEX = /^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/;

export function localhostRegex(): RegExp {
  return LOCALHOST_REGEX;
}

export function vercelPreviewRegex(teamSlug: string): RegExp {
  const safeSlug = teamSlug.replace(/[^a-z0-9-]/gi, "");
  return new RegExp(`^https:\\/\\/.*-${safeSlug}\\.vercel\\.app$`, "i");
}

export const PERMISSIONS_POLICY_VALUE =
  "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(self), payment=(), usb=()";

export const HSTS_VALUE = "max-age=31536000; includeSubDomains; preload";

export function getSupabaseOrigin(): string {
  const url = process.env.NEXT_PUBLIC_SUPABASE_URL || process.env.SUPABASE_URL;
  if (!url) return "";
  try {
    return new URL(url).origin;
  } catch {
    return "";
  }
}

export function buildConnectSrc(apiUrl?: string): string[] {
  const apiOrigin = apiUrl ? new URL(apiUrl).origin : "";
  const base: string[] = ["'self'", "https://*.supabase.co"];

  // Only include localhost allowances outside production
  if (process.env.NODE_ENV !== "production") {
    base.push(
      "http://localhost:*",
      "http://127.0.0.1:*",
      "ws://localhost:*",
      "ws://127.0.0.1:*"
    );
  }

  const allowPreviews = (process.env.ALLOW_VERCEL_PREVIEWS || "1") !== "0";
  if (allowPreviews) {
    base.push("https://*.vercel.app");
  }

  if (apiOrigin) base.push(apiOrigin);

  const supabaseOrigin = getSupabaseOrigin();
  if (supabaseOrigin && !base.includes(supabaseOrigin)) {
    base.push(supabaseOrigin);
  }

  return base;
}
