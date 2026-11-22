import "server-only";
import { NextResponse } from "next/server";
import { backendFetch, readTokens } from "@/shared/server/backend";
import { isSameOrigin } from "@/shared/server/origin";
import { setSession } from "@/shared/server/session";
import type { ZodType } from "zod";

/**
 * Shared by login and register: validate the body, call the backend, keep the tokens in httpOnly cookies and
 * return nothing to the browser but success or the backend's problem details.
 */
export async function handleCredentials(request: Request, backendPath: string, schema: ZodType): Promise<NextResponse> {
  if (!isSameOrigin(request)) return NextResponse.json({ title: "Forbidden", status: 403 }, { status: 403 });

  const parsed = schema.safeParse(await request.json().catch(() => null));
  if (!parsed.success) {
    return NextResponse.json(
      { title: "Validation failed", status: 400, errors: parsed.error.issues.map((i) => i.message) },
      { status: 400 },
    );
  }

  const upstream = await backendFetch(backendPath, {
    method: "POST",
    contentType: "application/json",
    body: JSON.stringify(parsed.data),
  }).catch(() => null);

  if (!upstream) return NextResponse.json({ title: "The service is unreachable.", status: 502 }, { status: 502 });
  if (!upstream.ok) {
    return new NextResponse(await upstream.text(), {
      status: upstream.status,
      headers: { "Content-Type": upstream.headers.get("content-type") ?? "application/json" },
    });
  }

  const tokens = await readTokens(upstream);
  if (!tokens) return NextResponse.json({ title: "Unexpected answer from the service.", status: 502 }, { status: 502 });

  const response = NextResponse.json({ isSuccess: true });
  setSession(response, tokens);
  return response;
}
