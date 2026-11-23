# Auth tokens: how they work and why they are built this way

Plain-language companion to [ADR 0003](decisions/0003-auth-token-hardening.md). Written because the owner delegated
the decisions ("your call") and asked for the reasons. Status of each part is marked **done** (built and green in CI,
2026-11-20) or **planned**. Updated 2026-11-23.

All four steps below are done in code. Still open: the owner must generate `JWT_PRIVATE_KEY_PEM` and remove
`JWT_SECRET` to finish the cut-over; a follow-up migration must drop the old plain `token` columns (A6); and the full
compose stack has not been started with RS256. The production override now **requires** `JWT_PRIVATE_KEY_PEM` (added
2026-11-23), so production cannot start in the legacy HS256-only mode.

## The two tokens
- **Access token** (a JWT): short-lived proof of who you are. Sent with every API call. Services check its signature.
- **Refresh token**: a long random string (64 random bytes) that can be traded once for a new access token plus a new
  refresh token ("rotation"). It is what keeps a user logged in.

## 1. Refresh tokens are stored as a hash (A6) - done
**Problem:** the database held the refresh token itself. Whoever could read the table (a backup, a SQL injection, a
curious admin) could use those sessions.
**Decision:** store only `SHA-256(token)` in `refresh_tokens.token_hash`. The client still sends the original string;
the server hashes it and looks the hash up. A stolen table is now useless for logging in.
**Why plain SHA-256 and no salt/bcrypt:** salting and slow hashes protect weak, guessable secrets such as passwords.
This token is 64 random bytes, so it cannot be guessed or brute-forced; a fast hash is enough and keeps lookups cheap.
**Why nobody is logged out:** the migration (`20261120080000_hash_refresh_tokens`) computes the hash of every existing
token inside Postgres (`encode(sha256(convert_to(token,'UTF8')),'hex')`), so existing sessions keep working.
**Rollback:** the old plain `token` column is kept (made nullable) so the migration's `Down` can restore it; rows
created after the migration have no plain token and are deleted on rollback (those users log in again). A later,
separate migration will drop the plain columns once the owner is happy (**planned**). Until then the old plain values
still sit in the table for old rows: that migration is what finally removes the exposure, so do not postpone it long.
**Tests:** unit tests for hashing, an integration test that the database holds no plain token, and one that runs the
real migration over a pre-existing row.

## 2. Parallel refresh cannot fork a session (A7) - done
**Problem:** two requests carrying the same refresh token at the same instant could both pass the "is it still valid?"
check and both get new tokens, creating two live sessions from one.
**Decision:** use Postgres' built-in row version (`xmin`) as an optimistic concurrency token on `refresh_tokens`. Both
requests try to revoke the same row; the second UPDATE sees the row changed and fails. Exactly one wins.
**What the loser sees:** 409 (lost the race) or 401 (arrived after, treated as reuse of a rotated token, which also
revokes the session family - the existing A1 behaviour). Never a 500. 409 is mapped in `GlobalExceptionHandler`.
**Why this way:** no schema change (`xmin` exists on every table), no locks held across requests, no Redis dependency.
**Why not revoke everything on a lost race:** a double-click or a client retry would log the user out everywhere.
Only a genuine reuse of an already-rotated token revokes the family.
**Test:** 8 parallel refreshes with one token -> exactly one 200, all others 401/409.

## 3. Session cap and purge (A10) - done
- Cap of **10** active refresh tokens per account (`Jwt__MaxActiveSessions`, 0 = no cap). On a new login or register the
  oldest active tokens are revoked. Why 10: enough for phone, laptop, tablet and browsers; low enough that someone with a
  leaked password cannot pile up unlimited sessions. Rotation does not change the count (one revoked, one added).
- A Quartz job (`PurgeRefreshTokensJob`, every 6 h, first run 2 min after start; `RefreshTokenCleanup__RetentionDays`,
  `__IntervalHours`, `__Enabled`) deletes rows whose **expiry** is more than **30 days** in the past. Active tokens are
  never touched. Why keyed on expiry: a rotated (revoked) token must stay while it could still be replayed, and once it
  has expired anyway the row is useless; 30 days of tail keeps room for support questions without unbounded growth.
  One bulk DELETE, so several replicas running it is harmless.
- **Tests:** handler test that the cap reaches the aggregate; integration test of the purge (old expired and old revoked
  rows go; recent expired, recent revoked and active rows stay).

## 4. RS256 access tokens with a JWKS endpoint - done
**Problem:** all services shared one HS256 secret that could also create tokens. A compromise of any service, or any
non-C# service that only wants to verify, could forge any identity.
**What it does now:**
- Auth signs with an RSA private key (`Jwt__PrivateKeyPem`, 2048 bits or more, checked at startup) and puts a `kid` in the
  token header. The public key is at `GET /.well-known/jwks.json` (public keys only, `Cache-Control: max-age=300`; also
  routed through the gateway).
- Every other service sets `Jwt__JwksUrl` (compose default `http://auth-api:8080/.well-known/jwks.json`). It fetches the
  keys lazily, caches them 10 minutes, refetches when a token names an unknown `kid` (at most every 30 s, so bad tokens
  cannot flood Auth) and keeps the cached keys if Auth is briefly down. These services hold no private key.
- Accepted algorithms are explicit: RS256 when keys exist, HS256 only while `Jwt__Secret` is set. `none` and any other
  algorithm are rejected; the public key is never offered as an HMAC secret (the algorithm-confusion attack).
**Cut-over (nobody is logged out):** (1) deploy all services (they already read the JWKS; HS256 still works),
(2) set `JWT_PRIVATE_KEY_PEM` on Auth and redeploy it, new tokens are RS256, old HS256 ones still validate,
(3) after `JWT_EXPIRY_MINUTES` remove `JWT_SECRET`: HS256 is rejected from then on. Steps are also in `.env.example`.
**Key rotation:** set the new private key, put the old public key in `JWT_PREVIOUS_PUBLIC_KEY_PEM` (published in the JWKS
and accepted), remove it after `JWT_EXPIRY_MINUTES`.
**Why RS256 and not ES256:** every JWT library in every language supports RS256; other languages are planned, so
compatibility wins over the smaller ES256 keys.
**Why JWKS from Auth rather than a public key copied into each service:** rotation needs no redeploy of the others.
**Tests (CI):** the API runs RS256-only; token header is RS256 with `kid`; protected endpoint accepts it; JWKS has no
private parameter and verifies the token; tokens from another RSA key, HS256 with the old secret, HS256 with the public key
as secret, and `alg=none` all get 401. Unit-level: modes (legacy / cut-over / RS256 only), rotation publishes two kids,
`\n` PEMs, remote fetch from a local server, unreachable Auth does not throw, startup validation (short secret, public key
as private, 1024-bit key, junk PEM).
**Owner action:** generate the key and put it in your secret store as `JWT_PRIVATE_KEY_PEM`; never commit it. The repo
defaults still start in the legacy HS256 mode until you do, but `docker-compose.prod.yml` now *requires* the private
key (2026-11-23). **Not done:** the owner cut-over and a real compose-stack start with RS256; CI covers the API
in-process with Testcontainers only.

## Not covered here
Account lockout (A8) is still open. Generic login errors (A9) and the first admin account (A15) were fixed on
2026-11-23; see [repo-check.md](repo-check.md).
