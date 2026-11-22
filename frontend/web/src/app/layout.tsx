import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = { title: "OmniCore", description: "OmniCore web client" };

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
