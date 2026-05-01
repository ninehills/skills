import {
  buildSupabaseRemotePatterns,
  createBaseNextConfig,
} from "@app/shared/next-config";
import type { NextConfig } from "next";

const posthogHost =
  process.env.NEXT_PUBLIC_POSTHOG_HOST || "https://us.i.posthog.com";

const baseConfig = createBaseNextConfig({
  appName: "web",
  enableMapbox: true,
  additionalImgSrc: ["https:", "https://*.googleusercontent.com"],
  additionalScriptSrc: [posthogHost],
  additionalConnectSrc: [posthogHost],
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
