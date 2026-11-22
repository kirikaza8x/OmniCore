import "server-only";
import { env } from "@/shared/config/env";
import type { Tokens } from "./session";

export type BackendInit = {
  method?: string;
  body?: BodyInit | null;
  contentType?: string | null;
  accessToken?: string;
};

/** One call to the backend through the gateway. */
export function backendFetch(path: string, init: BackendInit = {}): Promise<Response> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (init.contentType) headers["Content-Type"] = init.contentType;
  if (init.accessToken) headers.Authorization = `Bearer ${init.accessToken}`;
  return fetch(`${env.apiBaseUrl}${path}`, {
    method: init.method ?? "GET",
    headers,
    body: init.body ?? null,
    cache: "no-store",
    signal: AbortSignal.timeout(15_000),
  });
}

/** Reads AuthResponse whether or not it is wrapped in { data }. */
export async function readTokens(response: Response): Promise<Tokens | null> {
  try {
    const raw = (await response.json()) as { data?: Partial<Tokens> } & Partial<Tokens>;
    const t = raw.data ?? raw;
    if (t.accessToken && t.refreshToken) {
      return { accessToken: t.accessToken, refreshToken: t.refreshToken, expiresInMinutes: t.expiresInMinutes ?? 60 };
    }
  } catch {
    /* not JSON */
  }
  return null;
}
