# ADR 0003: Asymmetric JWT signing with JWKS, hashed refresh tokens, safer rotation

- Status: **proposed. Nothing in this ADR is implemented. Waiting for the owner** (token format and schema change,
  see the rule in `CLAUDE.md`). The questions at the end need an answer before step 2.
- Date: 2026-11-20
- Scope: `OmniCore.Services.Auth`, `OmniCore.Shared` (JWT bearer setup, `JwtConfig`), every service that validates tokens,
  compose and `.env.example`
- Related findings: A6, A7, A10 (and A11, already fixed) in `docs/repo-check.md`; roadmap phase 4
- Basis: a read of the code, CI green on 2026-11-20. Not tried against a running stack.

## Context

- Access tokens are **HS256** with one shared secret (`JWT_SECRET`). Every service that validates a token holds the same
  secret that can also **mint** tokens, so one compromised service can forge any identity, and a service in another
  language needs the signing secret just to verify. (The algorithm is pinned and the secret length is checked at startup.)
- **Refresh tokens are stored in plain text** in `refresh_tokens.token` (unique). Anyone who can read the table has live
  sessions (A6). They are 64 random bytes, so they are high entropy.
- **Rotation has no concurrency control**: two parallel refreshes with the same token can both succeed and fork the
  session (A7).
- Every login adds a refresh token row, no session cap is applied, and expired or revoked rows are never purged (A10).

## Proposal, in the order I would ship it (each step is its own commit, built and tested by CI)

### 1. Refresh tokens stored as a hash (A6), two migrations so nothing is lost

- Add `token_hash` (SHA-256 of the token's UTF-8 bytes, lowercase hex, unique). **Backfill existing rows in the same
  migration** with `encode(sha256(convert_to(token, 'UTF8')), 'hex')`, so **existing sessions keep working** (the finding
  assumed they would be invalidated; they do not have to be). Plain SHA-256 without a salt is right here because the
  token is random and long, not a password.
- Code looks tokens up by hash only. The client still sends the original string, and the server never stores it again.
  `ReplacedByToken` stores the hash too.
- A second, later migration drops the plain `token` column once the first has run in production.
- Rollback of step one: the plain column is still there until the second migration.

### 2. Rotation is safe under parallel requests (A7)

- Use an optimistic concurrency token on the refresh token row (Postgres `xmin`). The second of two parallel refreshes
  fails on save; the handler answers it like a reuse of an already rotated token (401, see question 3), not a 500.
- Add a test that fires two refreshes in parallel against the Testcontainers database and expects exactly one success.

### 3. Session cap and purge (A10)

- Pass a `maxActiveSessions` (config, default 10) when adding a token, so the oldest active token is revoked past the cap.
- A Quartz job (Quartz is already a dependency) deletes tokens that expired or were revoked more than N days ago (default 30,
  config). Keep a short tail, because reuse detection needs recently revoked tokens to still exist.

### 4. RS256 with a JWKS endpoint

- Auth signs with an **RSA 2048-bit** private key supplied as a secret (`JWT_PRIVATE_KEY_PEM`, never committed, `CHANGE_ME`
  in `.env.example`), with a `kid` header. RS256 rather than ES256 because every language and gateway verifies it.
- Auth serves `GET /.well-known/jwks.json` with the public key(s). It is public by design and rate limited by the gateway.
- Other services validate with the JWKS URL (`JWT_JWKS_URL`, default the Auth container on the compose network), with
  the keys cached and refreshed on an unknown `kid`. They hold **no signing secret** at all.
- **Rotation:** Auth can publish the current key plus the previous public key (`JWT_PREVIOUS_PUBLIC_KEY_PEM`) for one token
  lifetime, then the old one is removed. No restart of the other services is needed.
- **Cut-over:** access tokens last 15 minutes and refresh tokens do not depend on the signature, so the switch costs
  users nothing but one silent refresh. Optionally accept both HS256 and RS256 for one release (`JWT_SECRET` kept only for
  that window), then remove `JWT_SECRET` from the config, compose and docs.
- Startup validation: Auth refuses to start without a parseable key of at least 2048 bits; other services fail readiness
  (not startup) while no key set has ever been fetched, and keep the last good set if Auth goes down later.

## What this does not do

- No access token revocation (A17, still accepted): a stolen access token works until it expires, 15 minutes.
- No lockout or MFA (A8, A12), and no change to the account-enumeration answers (A9, owner decision).
- JWKS makes **Auth a runtime dependency of token validation** for a cold start of any other service. The cache covers
  Auth being down later, not Auth being down at the very first request.

## Alternatives considered

- **Public key in an environment variable on each service** (no runtime dependency, no JWKS): simpler, but rotation needs
  a redeploy of every service and a mismatch gives 401s until it is done. Reasonable if there will only ever be two or
  three services; JWKS wins once there are more or other languages.
- **ES256**: smaller and faster, equally standard, a few older libraries lack it. Fine if all consumers are known.
- **Validate at the gateway** (`auth_request`): rejected in ADR 0002.
- **Keep HS256 and just split secrets per service**: does not fix forging, since validation and signing share a key.

## Questions for the owner

1. RS256 (my proposal) or ES256?
2. JWKS fetched from Auth (my proposal) or the public key distributed through environment variables?
3. A rotated token used a second time in parallel: answer 401 like a normal reuse (my proposal, and it revokes the
   session family), or a quiet 409 "retry" that does not revoke? The first is safer, the second is friendlier to a
   client that double-fires a refresh.
4. Session cap (default 10) and purge tail (default 30 days): fine, or other numbers?
5. Is it acceptable that the migration in step 1 backfills hashes of the existing plain tokens (keeps sessions, but runs a
   one-off update on the whole table)?

## Verification plan

Each step ships with CI evidence: the existing register -> login -> refresh -> reuse integration test (updated for hashed
storage), a new parallel-refresh test, a migration test against a database holding plain-text rows (existing session
still refreshes after the migration), and for step 4 a test that a token signed with another key, or with `alg=none` or
HS256 after the cut-over, is rejected.
