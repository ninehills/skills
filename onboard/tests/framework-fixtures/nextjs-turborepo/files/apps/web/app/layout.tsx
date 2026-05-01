import type { Metadata } from "next";
import type React from "react";

export const metadata: Metadata = {
  title: "Turborepo Fixture",
  description: "Minimal monorepo app layout for live-mode tests.",
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
