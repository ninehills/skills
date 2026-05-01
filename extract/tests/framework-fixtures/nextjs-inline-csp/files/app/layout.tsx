import type { Metadata } from "next";
import type React from "react";

export const metadata: Metadata = {
  title: "Inline CSP Fixture",
  description: "Minimal app with a literal CSP header for live-mode tests.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
