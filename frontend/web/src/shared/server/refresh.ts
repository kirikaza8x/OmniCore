import "server-only";
import { backendFetch, readTokens } from "./backend";
import type { Tokens } from "./session";

// Two requests that find an expired access token at the same moment must not both use the same refresh token:
// the backend rotates it and treats the second use as theft (revokes everything). So one refresh runs at a time per
// refresh token and the others wait for its result.
const inflight = new Map<string, Promise<Tokens | null>>();

export function refreshTokens(refreshToken: string): Promise<Tokens | null> {
  const existing = inflight.get(refreshToken);
  if (existing) return existing;

  const run = (async () => {
    const response = await backendFetch("/api/v1/auth/refresh-token", {
      method: "POST",
      contentType: "application/json",
      body: JSON.stringify({ refreshToken }),
    });
    return response.ok ? readTokens(response) : null;
  })().finally(() => setTimeout(() => inflight.delete(refreshToken), 5_000));

  inflight.set(refreshToken, run);
  return run;
}
