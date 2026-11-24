# OmniCore web client

Next.js 16 (App Router), React 19, TypeScript, Tailwind 4, zod. Decisions: [ADR 0005](../../docs/decisions/0005-web-client.md).

## What it does today
Sign in, register, sign out, forgot/reset password and e-mail confirmation (ADR 0009), and a profile page (personal information, avatar upload, settings, up to 10 addresses) that talks to the Auth and User services through the gateway.

## Run it
```bash
cd frontend/web
cp .env.example .env.local        # API_BASE_URL=http://localhost:8080 (the compose gateway)
npm install
npm run dev                       # http://localhost:3000
```
The backend must be running: `cd infra/docker && docker compose up -d --build` (see its README). With Docker for the web client too: `docker compose --profile web up -d --build web`.

| Command | What |
|---|---|
| `npm run lint` | ESLint, including the layer rules |
| `npm run typecheck` | `tsc --noEmit` |
| `npm test` | Vitest (schemas, error parsing) |
| `npm run build` | production build (standalone output) |

CI: `.github/workflows/frontend.yml` runs all four and builds the Docker image.

## Structure (same idea as the backend layers)
```
src/
  app/                  routes only (like the Api layer): pages, layouts, route handlers, nothing else
    (auth)/login, register, forgot-password, reset-password?token=, verify-email?token=     public pages
    (app)/profile              private page
    api/auth/*                 login, register, logout (BFF: set/clear httpOnly cookies); forgot-password, reset-password, verify-email (anonymous, forwarded without tokens)
    api/proxy/[...path]        adds the bearer token, refreshes it once, forwards to the gateway
  features/             one folder per business area (like a service)
    auth/    schemas.ts  api.ts  components/  server/
    profile/ types.ts  schemas.ts  api.ts  useProfile.ts  components/
  shared/               generic code (like Shared): config, api client + problem details, server helpers, ui
  proxy.ts              redirects signed-out visitors away from private pages (Next 16 name for middleware)
```
Layers, enforced by ESLint (`no-restricted-imports`): `app` may import `features` and `shared`; `features` may import `shared`; `shared` imports neither; a feature never imports another feature (put shared bits in `shared`, or compose features in `app`).

Inside a feature: `schemas.ts` (zod, mirrors backend validators) -> `api.ts` (calls, typed) -> `components/` (UI). `server/` holds code that runs only on the server (marked `server-only`).

## Rules
- The browser never calls the backend and never sees a token. Client code calls `/api/...` only (`shared/api/http.ts`).
- The backend is the authority. Zod schemas only give quick feedback; show the backend's message when it answers 400/401/409.
- Ids that identify the caller are never sent: the backend takes them from the token.
- New backend area reachable from the browser: add it to the allow list in `app/api/proxy/[...path]/route.ts` deliberately.

## How to ...
**Add a feature (for example `notes`):** create `src/features/notes/{types.ts,schemas.ts,api.ts,components/}`; add the route under `src/app/(app)/notes/page.tsx`; add `^notes(/|$)` to the proxy allow list; add the feature to `eslint.config.mjs` boundary rules; add a test.
**Add a page:** a folder with `page.tsx` in `app`; if it is private, add its path to the matcher in `proxy.ts`.

## Known gaps
- No browser tests yet (Playwright is the next step); the BFF was smoke-tested against a fake gateway, not yet against the real stack.
- Account-level screens: password change and sessions wait for backend endpoints (AA4). The verify-email page confirms on a button press, not on load, so mail scanners cannot burn the single-use token. The profile page offers "Send verification e-mail" but does not show whether the address is verified (the profile API does not return it).
- Edit/promote an address is not in the UI yet (the API supports it).
- The theme setting is saved but not applied to the UI.
