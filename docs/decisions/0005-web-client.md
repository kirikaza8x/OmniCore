# ADR 0005: Web client stack and token handling

Status: **accepted by delegation** (2026-11-23). Owner asked for a Node.js front end (Next, React or Angular), organised like the backend, with a Docker image.

## Decision
1. **Next.js (App Router) with TypeScript and Tailwind**, in `frontend/web`.
2. **Backend-for-frontend (BFF):** the browser only talks to Next. Route handlers call the gateway from the server side and keep the access and refresh tokens in **httpOnly cookies**. No token is ever readable by JavaScript.
3. **Feature folders with layer rules**, enforced by ESLint: `app` (routes) -> `features/<name>` -> `shared`; shared never imports features, a feature never imports another feature.
4. **The backend stays the authority:** the web client validates input only to save round trips (zod schemas mirror the backend validators); authorization is the backend's 401/403.
5. Docker: multi-stage image, `output: "standalone"`, non-root user, health check; compose service `web` behind the `web` profile.

## Why Next.js
- You already know React and Next; Next is the least new thing to learn while learning the backend.
- A server part comes with it, which is exactly what the BFF needs. A plain React SPA would have to keep tokens in JavaScript-reachable storage, or run a separate Node server just for cookies.
- Angular is a good fit for large enterprise teams and brings DI and modules that feel like the .NET side, but it would double the number of new concepts at once and has no server part for the cookie pattern.
- Server rendering stays available later for public pages (blog, landing) without a second project.

## Token handling in detail
- Login/register: browser -> `POST /api/auth/login` (Next) -> gateway -> Auth. Next stores `omni_at` (access) and `omni_rt` (refresh) as `HttpOnly; SameSite=Lax` (`Secure` when `COOKIE_SECURE=true`) and returns no token.
- Data calls: browser -> `/api/proxy/<path>` -> Next adds `Authorization: Bearer` -> gateway. Only `users/**` and `auth/me` can be reached through it (allow list).
- Expired access token: Next refreshes once and retries. **One refresh runs at a time per refresh token** (single flight): the backend rotates refresh tokens and treats a second use as theft, so parallel refreshes would log the user out.
- CSRF: SameSite=Lax cookies, JSON bodies, and a same-origin check on every state-changing route handler.
- Logout: refresh token revoked on the backend, cookies cleared even if the backend is down.

## Consequences
- The gateway must stay reachable from the web container (`API_BASE_URL`).
- Cookies are not shared with other sites; a mobile app would use the gateway directly with its own token storage.
- ADR 0002 (JWTs are validated in the services, not the gateway) is unchanged.
- Not built: password reset and e-mail verification (needs the Notification service), social login, i18n, tests in a browser (Playwright is the next step).
