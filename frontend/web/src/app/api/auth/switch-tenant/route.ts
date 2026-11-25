import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { switchTenantSchema } from "@/features/auth/schemas";
import { COOKIES } from "@/shared/config/env";
import { backendFetch } from "@/shared/server/backend";
import { isSameOrigin } from "@/shared/server/origin";
import { refreshTokens } from "@/shared/server/refresh";
import { setAccessToken, setSession } from "@/shared/server/session";

/** Picks another tenant: the backend checks membership and answers with a new access token for it. */
export async function POST(request: Request) {
  if (!isSameOrigin(request)) return NextResponse.json({ title: "Forbidden", status: 403 }, { status: 403 });

  const parsed = switchTenantSchema.safeParse(await request.json().catch(() => null));
  if (!parsed.success) {
    return NextResponse.json(
      { title: "Validation failed", status: 400, errors: parsed.error.issues.map((i) => i.message) },
      { status: 400 },
    );
  }

  const jar = await cookies();
  const refreshToken = jar.get(COOKIES.refresh)?.value;
  let accessToken = jar.get(COOKIES.access)?.value;
  const renewed = !accessToken && refreshToken ? await refreshTokens(refreshToken, jar.get(COOKIES.tenant)?.value) : null;
  accessToken = accessToken ?? renewed?.accessToken;
  if (!accessToken) return NextResponse.json({ title: "Unauthorized", status: 401 }, { status: 401 });

  const upstream = await backendFetch("/api/v1/auth/switch-tenant", {
    method: "POST",
    contentType: "application/json",
    body: JSON.stringify(parsed.data),
    accessToken,
  }).catch(() => null);
  if (!upstream) return NextResponse.json({ title: "The service is unreachable.", status: 502 }, { status: 502 });
  if (!upstream.ok) {
    return new NextResponse(await upstream.text(), {
      status: upstream.status,
      headers: { "Content-Type": upstream.headers.get("content-type") ?? "application/json" },
    });
  }

  const raw = (await upstream.json().catch(() => null)) as { data?: Switched } & Switched | null;
  const result = raw?.data ?? raw;
  if (!result?.accessToken) return NextResponse.json({ title: "Unexpected answer.", status: 502 }, { status: 502 });

  const response = NextResponse.json({ isSuccess: true });
  if (renewed) setSession(response, renewed);
  setAccessToken(response, result.accessToken, result.expiresInMinutes ?? 60, parsed.data.tenantId);
  return response;
}

type Switched = { accessToken?: string; expiresInMinutes?: number };
