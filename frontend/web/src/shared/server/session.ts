import "server-only";
import type { NextResponse } from "next/server";
import { COOKIES, env } from "@/shared/config/env";

export type Tokens = { accessToken: string; refreshToken: string; expiresInMinutes: number };

const REFRESH_DAYS = 30; // the backend decides when a refresh token really expires; this only bounds the cookie

const base = { httpOnly: true, sameSite: "lax" as const, secure: env.cookieSecure, path: "/" };

export function setSession(response: NextResponse, tokens: Tokens): void {
  response.cookies.set(COOKIES.access, tokens.accessToken, { ...base, maxAge: Math.max(60, tokens.expiresInMinutes * 60 - 30) });
  response.cookies.set(COOKIES.refresh, tokens.refreshToken, { ...base, maxAge: REFRESH_DAYS * 24 * 3600 });
}

/** Replaces only the access token (switching tenant does not touch the refresh token) and remembers the tenant. */
export function setAccessToken(response: NextResponse, accessToken: string, expiresInMinutes: number, tenantId: string): void {
  response.cookies.set(COOKIES.access, accessToken, { ...base, maxAge: Math.max(60, expiresInMinutes * 60 - 30) });
  response.cookies.set(COOKIES.tenant, tenantId, { ...base, maxAge: REFRESH_DAYS * 24 * 3600 });
}

export function clearSession(response: NextResponse): void {
  response.cookies.set(COOKIES.tenant, "", { ...base, maxAge: 0 });
  response.cookies.set(COOKIES.access, "", { ...base, maxAge: 0 });
  response.cookies.set(COOKIES.refresh, "", { ...base, maxAge: 0 });
}
