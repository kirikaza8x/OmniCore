# 0005: Require a verified e-mail at login (optional)

- Status: done 2026-11-24 on `develop`, CI green (developer reported)
- Date: 2026-11-24
- Related: ADR 0010, ADR 0009

## Steps
1. Auth: `IAuthPolicy` (Application) + `AuthPolicyConfig`/`AuthPolicyService` (Infrastructure, `Auth__RequireVerifiedEmail`); login
   check after the password; `RequestEmailVerificationByEmail` command + `POST /email/verify/resend`; admin seeder confirms the e-mail.
2. Gateway: `email/verify/resend` in the credential rate-limit regex. Compose and `.env.example`: `REQUIRE_VERIFIED_EMAIL`.
3. Web: `/resend-verification` page + BFF route; login form links to it on `Auth.EmailNotVerified`.
4. Tests: login handler (switch off/on, wrong password stays generic), resend handler; web schema test. Docs.

5. Auth: `AdminConfirmEmails` command + `POST /auth/admin/emails/confirm` (SuperAdmin), `GetByIdsAsync`, 4 tests.

## Acceptance
- Switch off: nothing changes. Switch on: unverified account with the right password gets 403 `Auth.EmailNotVerified`; wrong
  password still 401 generic; verified account signs in; resend always 200 and mails only active unverified accounts.

## Out of scope
A bulk-verify UI (the endpoint exists), enforcing on refresh, verified-at-creation for external sign-in.
