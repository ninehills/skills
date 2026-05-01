// Reference output for agent/human review — not executed by tests.
// After the Shape 1 (shared-helper) CSP patch is applied, apps/web/next.config.ts
// should look like this.

import {
  buildSupabaseRemotePatterns,
  createBaseNextConfig,
} from "@app/shared/next-config";
import type { NextConfig } from "next";

const posthogHost =
  process.env.NEXT_PUBLIC_POSTHOG_HOST || "https://us.i.posthog.com";

// Dev-only allowance so impeccable live mode can load. Guarded by NODE_ENV;
// empty array in any non-development environment.
const __impeccableLiveDev =
  process.env.NODE_ENV === "development" ? ["http://localhost:8400"] : [];

const baseConfig = createBaseNextConfig({
  appName: "web",
  enableMapbox: true,
  additionalImgSrc: ["https:", "https://*.googleusercontent.com"],
  additionalScriptSrc: [posthogHost, ...__impeccableLiveDev],
  additionalConnectSrc: [posthogHost, ...__impeccableLiveDev],
});

const nextConfig: NextConfig = {
  ...baseConfig,

  devIndicators: {
    position: "bottom-right",
  },

  experimental: {
    ...baseConfig.experimental,
  },

  typescript: {
    ignoreBuildErrors: true,
  },

  images: {
    remotePatterns: buildSupabaseRemotePatterns(),
    dangerouslyAllowLocalIP: process.env.NODE_ENV === "development",
  },
};

export default nextConfig;
