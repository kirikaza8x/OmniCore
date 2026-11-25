import "server-only";

/** Server-side settings, read once. Everything comes from environment variables (see .env.example). */
export const env = {
  /** Where the gateway is, as seen from this server (inside Docker: http://gateway:80). */
  apiBaseUrl: (process.env.API_BASE_URL ?? "http://localhost:8080").replace(/\/+$/, ""),
  /** Set true when the site is served over HTTPS. Cookies are then marked Secure. */
  cookieSecure: process.env.COOKIE_SECURE === "true",
} as const;

export const COOKIES = {
  access: "omni_at",
  refresh: "omni_rt",
  /** The tenant the person picked; sent as a hint when the access token is renewed (never a secret). */
  tenant: "omni_tenant",
} as const;
