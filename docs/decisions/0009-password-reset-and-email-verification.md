# ADR 0009: Password reset and e-mail verification

- Status: **accepted by delegation** (owner said "you pick"; reasons below)
- Date: 2026-11-23
- Scope: `OmniCore.Services.Auth.*`, `OmniCore.Services.Notification.*`, `Auth.Contracts`
- Related: repo-check AA4, plan `docs/plans/0004-password-reset-email-verification.md`

## Context

Auth has no way to reset a forgotten password or to verify an e-mail. The pieces exist but are unused: the
`SecurityToken` entity (hashed code, type, expiry, single use, table already migrated), `SecurityTokenType`,
`Account.ConfirmEmail()` and `Account.UpdatePassword()` (which already revokes every refresh token), and Notification
with templates and retries. No schema change is needed.

## Decision

1. **Opaque random token, hash at rest.** 32 random bytes, URL-safe base64; only its SHA-256 is stored in
   `SecurityToken.CodeHash`. Lifetimes: password reset 1 hour, e-mail verification 24 hours. A new request of the same
   type invalidates the older unused ones for that account.
2. **No account enumeration.** `POST /password/forgot` always answers 202 with the same body, whether or not the
   e-mail exists, and does the same amount of work as far as practical. Failures to send never reach the caller.
3. **Auth publishes its own events, Notification maps them.** `PasswordResetRequestedIntegrationEvent` and
   `EmailVerificationRequestedIntegrationEvent` live in `Auth.Contracts` (Notification already consumes Auth's
   contracts). Auth does not reference Notification. Notification renders the templates `password-reset` and
   `verify-email` and builds the link from a configured front-end base URL (`Notification:PublicBaseUrl`).
4. **Single-use and type-bound.** A token of one type is rejected for the other. Using it marks it used in the same
   transaction as the effect. A successful reset also revokes all refresh tokens (already in `UpdatePassword`) and
   keeps the `PasswordPolicy` validation of the new password.
5. **Endpoints.** `POST /api/v1/auth/password/forgot`, `POST /api/v1/auth/password/reset`,
   `POST /api/v1/auth/email/verify/request` (authorized), `POST /api/v1/auth/email/verify/confirm`. The two anonymous
   ones sit under the gateway credential `limit_req` zone.

## Consequences and accepted risks

- **The plaintext token travels in the integration event** and therefore sits in the Auth outbox row until the outbox
  cleanup removes it, and in the broker. Mitigations: 1 hour or 24 hours lifetime, single use, hash-only in the token
  table, and Notification never stores the model (only the delivery log). The alternative (Notification asks Auth
  for a link) adds a synchronous dependency and is rejected for now. Revisit if the outbox retention grows.
- E-mail is the only recovery channel, so a compromised mailbox is a compromised account. MFA is out of scope.
- Verification is not enforced at login yet: `IsEmailConfirmed` is recorded only. Enforcing it needs its own decision.
- The front-end must serve the pages that read the token from the link (`/reset-password?token=`, `/verify-email?token=`).
