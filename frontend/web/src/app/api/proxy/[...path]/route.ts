import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { COOKIES } from "@/shared/config/env";
import { backendFetch } from "@/shared/server/backend";
import { isSameOrigin } from "@/shared/server/origin";
import { refreshTokens } from "@/shared/server/refresh";
import { clearSession, setSession, type Tokens } from "@/shared/server/session";

// The browser calls /api/proxy/<path>; this handler adds the bearer token from the httpOnly cookie, refreshes it
// once when it has expired, and forwards to the gateway. Only these backend areas can be reached through it.
const ALLOWED = [/^users(\/|$)/, /^auth\/me$/];

type Context = { params: Promise<{ path: string[] }> };

async function forward(request: Request, { params }: Context): Promise<NextResponse> {
  const { path } = await params;
  const target = path.join("/");
  if (!ALLOWED.some((rule) => rule.test(target))) {
    return NextResponse.json({ title: "Not found", status: 404 }, { status: 404 });
  }
  if (request.method !== "GET" && !isSameOrigin(request)) {
    return NextResponse.json({ title: "Forbidden", status: 403 }, { status: 403 });
  }

  const jar = await cookies();
  let accessToken = jar.get(COOKIES.access)?.value;
  const refreshToken = jar.get(COOKIES.refresh)?.value;
  let renewed: Tokens | null = null;

  if (!accessToken && refreshToken) {
    renewed = await refreshTokens(refreshToken);
    accessToken = renewed?.accessToken;
  }
  if (!accessToken) return unauthorized();

  const search = new URL(request.url).search;
  const hasBody = request.method !== "GET" && request.method !== "DELETE";
  const body = hasBody ? await request.arrayBuffer() : null;
  const call = (token: string) =>
    backendFetch(`/api/v1/${target}${search}`, {
      method: request.method,
      body,
      contentType: request.headers.get("content-type"),
      accessToken: token,
    });

  let upstream = await call(accessToken).catch(() => null);
  if (!upstream) return NextResponse.json({ title: "The service is unreachable.", status: 502 }, { status: 502 });

  // The access token may have been revoked or expired early: try once with a fresh one.
  if (upstream.status === 401 && refreshToken && !renewed) {
    renewed = await refreshTokens(refreshToken);
    if (!renewed) return unauthorized();
    upstream = (await call(renewed.accessToken).catch(() => null)) ?? upstream;
  }

  const response = new NextResponse(upstream.status === 204 ? null : await upstream.arrayBuffer(), {
    status: upstream.status,
    headers: { "Content-Type": upstream.headers.get("content-type") ?? "application/json" },
  });
  if (renewed) setSession(response, renewed);
  return response;
}

function unauthorized(): NextResponse {
  const response = NextResponse.json({ title: "Unauthorized", status: 401 }, { status: 401 });
  clearSession(response);
  return response;
}

export { forward as GET, forward as POST, forward as PUT, forward as DELETE };
