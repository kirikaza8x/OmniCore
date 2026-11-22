import { ApiError, toProblem } from "./problem";

/** The backend wraps successful results as { isSuccess, message, data }. */
type Envelope<T> = { data?: T };

/**
 * Browser-side client. It only talks to this site (`/api/...`), never to the backend directly:
 * the server part holds the tokens in httpOnly cookies, so no token is ever visible to JavaScript.
 */
export async function http<T>(path: string, init: RequestInit & { json?: unknown } = {}): Promise<T> {
  const { json, headers, ...rest } = init;
  const response = await fetch(path, {
    ...rest,
    headers: {
      ...(json !== undefined ? { "Content-Type": "application/json" } : {}),
      ...headers,
    },
    body: json !== undefined ? JSON.stringify(json) : rest.body,
    credentials: "same-origin",
  });

  if (!response.ok) throw new ApiError(await toProblem(response));
  if (response.status === 204) return undefined as T;

  const body = (await response.json()) as Envelope<T> | T;
  return body && typeof body === "object" && "data" in body ? ((body as Envelope<T>).data as T) : (body as T);
}
