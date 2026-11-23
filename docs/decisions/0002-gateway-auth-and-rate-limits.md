# ADR 0002: Gateway rate limiting, and who validates JWTs

- Status: **accepted as the default for now** (low risk, easy to reverse). The owner can overturn it.
- Date: 2026-11-20
- Scope: `infra/docker/nginx/nginx.conf`
- Related findings: F2, F6, S2, S3, A8 in `docs/repo-check.md`

## Context

The gateway only routed `/api/v1/auth/`, had no limits, and the services' own limiter sees only the nginx address
behind it (S2). Login, register and refresh-token had no stricter limit than any other endpoint (S3, A8).
The open question in the roadmap was whether the gateway or each service validates JWTs.

## Decision

1. **Rate limiting lives in the gateway** (`limit_req`, per client IP):
   - credential endpoints (`login`, `register`, `refresh-token`): 10 requests/min, burst 5
   - everything else under `/api/`: 20 requests/s, burst 20
   - rejected requests get `429` with a JSON body and `Retry-After`
   - the credential pattern is case-insensitive and accepts a trailing slash, because ASP.NET Core routing does;
     a stricter pattern would let `/Login` or `/login/` skip the limit (found while testing)
2. **JWTs stay validated by each service**, not by the gateway. The gateway only routes and limits.
3. The gateway routes the User service prefix `/api/v1/users/` (with a 6 MB body limit for avatars) and
   `/specs/user.json`. The User service now has endpoints and a Swagger spec (U1 fixed); the full compose stack is
   still not smoke-tested (UP1). The gateway also routes `/api/v1/notifications/` and `/specs/notification.json` to
   the Notification service.

## Why

- Validating tokens in nginx needs either `auth_request` (an extra hop to Auth on every call) or nginx Plus / Lua,
  and it would still leave the services open to anything that reaches them directly: ports 5001 and 5002 are
  published in the base compose file.
- Services already validate the token, the issuer, the audience and the algorithm (S-series fixes), so
  authorization stays next to the code that knows the roles.
- IP limits in the gateway protect all services at once and do not depend on each service's configuration.

## Consequences and limits

- This slows password guessing (about 10 tries per minute per IP) but is **not** an account lockout: someone with many
  IPs, or many users behind one NAT, behaves differently. A per-account failed-attempt counter is still open (A8).
- Behind another proxy or load balancer every client shares one bucket until `set_real_ip_from` / `real_ip_header`
  is configured (documented in `nginx.conf`).
- The in-service limiter still keys on the nginx address (S2); it is a coarse backstop now, not the main control.
- Published service ports (5001, 5002) bypass the gateway and its limits; the prod override does not close them yet.
- If we ever want one place to reject unauthenticated calls, revisit with `auth_request` or asymmetric JWT + JWKS
  (phase 4), where the gateway can verify the signature without calling Auth.

## Verification

Run with nginx 1.24 and fake backends in a sandbox: routing per prefix, 413 above 6 MB and 200 at 3 MB, six
credential requests pass and the seventh gets 429, `/Login/`, `/LOGIN/`, `/register/` and `/Refresh-Token/` share the
strict bucket, `/me` is not affected by it. CI also runs `nginx -t` with the real image.
