# 0004: Password reset and e-mail verification

- Status: in progress
- Date: 2026-11-23
- Owner: Claude session
- Related: ADR 0009, repo-check AA4, roadmap Phase 10

## Problem
A user who forgets the password is locked out and e-mails are never verified. The domain pieces exist but nothing uses them.

## Steps (one push each, CI result from the developer)
1. Auth.Contracts: two events. Auth.Application: `RequestPasswordReset`, `ResetPassword`, `RequestEmailVerification`,
   `ConfirmEmail` commands, `ISecurityTokenService` (generate + hash), repository access for tokens, validators, tests.
2. Auth.Api: four endpoints per ADR 0009; gateway `limit_req` for the anonymous ones.
3. Notification: two consumers, templates `password-reset` and `verify-email`, `Notification:PublicBaseUrl` config, tests.
4. Docs: auth-tokens.md, repo-check AA4, roadmap, CLAUDE.md, front-end note.

## Acceptance
- Forgot always 202; reset works once, expires, wrong type rejected, old refresh tokens revoked.
- Verify sets `IsEmailConfirmed`; Notification sends one mail per request, no token in logs.

## Out of scope
Enforcing verified e-mail at login, MFA, change-e-mail flow.
