# ADR 0010: Optional enforcement of verified e-mail at login

- Status: **accepted by delegation** (owner said "continue with the plan"; reasons below)
- Date: 2026-11-24
- Scope: `OmniCore.Services.Auth.*`, `frontend/web`, compose
- Related: ADR 0009, plan `docs/plans/0005-require-verified-email.md`

## Context
ADR 0009 records `IsEmailConfirmed` but nothing uses it, so an account with a typo or someone else's address can sign in
and receive mail meant for another person. Turning enforcement on for everyone at once would lock out every existing
unverified account and anyone who registered before the mail pipeline was reachable.

## Decision
1. **A switch, off by default.** `Auth__RequireVerifiedEmail` (compose `REQUIRE_VERIFIED_EMAIL`, default `false`). When it is
   `true`, login succeeds only for accounts with `IsEmailConfirmed`. Registration and token refresh are unchanged: a
   session issued before the switch keeps working until its refresh token ends (stated risk; revoke sessions if you need a hard cut).
2. **Checked after the password.** The handler answers `Auth.EmailNotVerified` (403) only when the identifier and password are
   right and the account is active. A wrong password or unknown account still gets the one generic answer (A9), so the new
   error cannot be used to find out which e-mails or usernames exist.
3. **A way out.** A blocked user has no token, so verification cannot depend on being signed in. New anonymous endpoint
   `POST /api/v1/auth/email/verify/resend` takes an e-mail and always answers 200; it sends a link only to an active,
   unverified account (same token rules and `limit_req` zone as password reset). The web login page links to it when it gets
   `Auth.EmailNotVerified`.
4. **The first admin is verified.** The configured admin (A15) is created with `ConfirmEmail()`, because the operator owns the
   address and an unverified admin could otherwise lock themselves out of a fresh stack with the switch on.

## Consequences
- Turning the switch on for an existing system blocks unverified users until they use the resend link. Roll out by setting it
  after announcing it, or verify the important accounts first. There is no bulk-verify tool yet.
- The switch needs working outbound mail. Production already guards the SMTP host; with the switch on, a broken relay means
  nobody new can sign in. That is the intended trade.
- External (Google) sign-in is not built; when it is, an account created from a verified Google e-mail should start verified.
