# Auth tokens: how they work and why they are built this way

Plain-language companion to [ADR 0003](decisions/0003-auth-token-hardening.md). Written because the owner delegated
the decisions ("your call") and asked for the reasons. Status of each part is marked **done** (built and green in CI,
2026-11-20) or **planned**.

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

## 3. Session cap and purge (A10) - planned
- Cap of **10** active refresh tokens per account (oldest revoked first). `Account.AddRefreshToken` already supports it;
  it only needs a config value. Why 10: enough for phone, laptop, tablet and browsers; low enough that a leaked-password
  attacker cannot pile up unlimited sessions.
- A Quartz job deletes revoked/expired rows older than **30 days**. Why keep a tail: reuse detection needs recently
  rotated tokens to still exist, and 30 days leaves room for support/audit questions without letting the table grow forever.

## 4. RS256 access tokens with a JWKS endpoint - planned
**Problem:** today all services share one HS256 secret that can also create tokens. A compromise of any service, or any
non-C# service that only wants to verify, can forge any identity.
**Decision:** Auth signs with an RSA private key (2048-bit, RS256, `kid` in the header) and publishes the public key at
`/.well-known/jwks.json`. Other services only fetch and cache the public key; they cannot mint tokens.
**Why RS256 and not ES256:** every JWT library in every language supports RS256; ES256 is smaller but library support
and key handling are less uniform. Since other languages are planned, compatibility wins.
**Why JWKS from Auth rather than copying a public key into each service's env:** key rotation then needs no redeploy
of the other services. Rotation: Auth publishes the new and previous public key together for a window.
**Cut-over:** briefly accept both HS256 and RS256, then remove `JWT_SECRET`. Tests will cover wrong key, `alg=none`,
and HS256 rejected after cut-over.
**Needs from the owner at deploy time:** generate the private key and put it in the secret store as `JWT_PRIVATE_KEY_PEM`.
It must never be committed.

## Not covered here
Account lockout (A8), generic login error (A9) and the first admin account (A15) are separate findings.
