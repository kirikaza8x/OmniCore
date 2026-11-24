import type { Metadata } from "next";
import { VerifyEmailPanel } from "@/features/auth/components/VerifyEmailPanel";

// The link carries a single-use token in the query string: do not leak it to other sites through the Referer header.
export const metadata: Metadata = { referrer: "no-referrer", robots: { index: false } };

export default async function VerifyEmailPage({ searchParams }: { searchParams: Promise<{ token?: string | string[] }> }) {
  const { token } = await searchParams;
  return <VerifyEmailPanel token={typeof token === "string" ? token : ""} />;
}
