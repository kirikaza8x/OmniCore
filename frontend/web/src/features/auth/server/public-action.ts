import "server-only";
import { NextResponse } from "next/server";
import { backendFetch } from "@/shared/server/backend";
import { isSameOrigin } from "@/shared/server/origin";
import type { ZodType } from "zod";

/**
 * For anonymous account actions (forgot password, reset password, confirm e-mail): validate the body, forward it to the
 * backend with no token and hand back the backend's answer. Nothing is stored in cookies, and the body (which may hold
 * a reset token or a new password) is never logged.
 */
export async function handlePublicAction(request: Request, backendPath: string, schema: ZodType): Promise<NextResponse> {
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

  return new NextResponse(upstream.status === 204 ? null : await upstream.text(), {
    status: upstream.status,
    headers: { "Content-Type": upstream.headers.get("content-type") ?? "application/json" },
  });
}
