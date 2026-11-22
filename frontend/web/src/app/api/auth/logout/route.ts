import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { COOKIES } from "@/shared/config/env";
import { backendFetch } from "@/shared/server/backend";
import { isSameOrigin } from "@/shared/server/origin";
import { clearSession } from "@/shared/server/session";

export async function POST(request: Request) {
  if (!isSameOrigin(request)) return NextResponse.json({ title: "Forbidden", status: 403 }, { status: 403 });

  const refreshToken = (await cookies()).get(COOKIES.refresh)?.value;
  if (refreshToken) {
    // Revoke the session on the backend; the cookies are cleared even if the backend is down.
    await backendFetch("/api/v1/auth/logout", {
      method: "POST",
      contentType: "application/json",
      body: JSON.stringify({ refreshToken }),
    }).catch(() => undefined);
  }

  const response = NextResponse.json({ isSuccess: true });
  clearSession(response);
  return response;
}
