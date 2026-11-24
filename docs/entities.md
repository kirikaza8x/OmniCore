# Entity design review

Scope: the domain models of Auth and User (Shared has only base types, see its README). The Notification aggregate is not reviewed here; see the [Notification.Domain README](../backend/c%23/OmniCore.Services.Notification/OmniCore.Services.Notification.Domain/README.md). Written 2026-11-22 against the code on `docs/readme-and-roadmap`. Finding ids (`AD#`, `UD#`) are in [repo-check.md](repo-check.md). Verdict per entity: **keep**, **change**, **remove**, **defer**.

## 1. The big picture
```
 Auth database                          User database (schema users)
 Account (aggregate)                    UserProfile (aggregate)  id = Account.Id, no FK across databases
  |- RefreshToken[]                      |- UserSettings (1:1)
  |- AccountRole[] --> Role              |- UserAddress[] (max 10, one primary)
 SecurityToken, MfaMethod,               (UserTenantProfile: defined, not mapped, ADR 0001)
 MfaRecoveryCode, ExternalLogin,
 SecurityAuditLog, UserSession  <- tables exist, no feature uses them
```
The split is sound: Auth owns **credentials and access**, User owns **who the person is**. The link is the id plus one event (`AccountRegisteredIntegrationEvent`). Design rules that follow: never copy a secret across, never join across databases, Auth is the source of truth for username and e-mail.

## 2. Auth
| Entity | Verdict | Why / change |
|---|---|---|
| `Account` | **keep, small changes** | Good aggregate: owns its tokens and roles, rules inside methods. Change: raise events from `Deactivate`/`SoftDelete`/`AttachEmail` (AD2) so User can react (see 4); make username case-insensitive unique (AD7/AI1); inject a clock instead of `DateTime.UtcNow` (AD4). |
| `RefreshToken` | **keep** | Hashed at rest, rotation with reuse detection, `xmin` guard, session cap: the most mature part. |
| `Role` + `AccountRole` | **change** | Roles are identified by string and `UpdateRole` can rename `Admin`/`User` and break authorization (AD1). Add `IsSystem` (rename/delete blocked) with a migration, and a seeded first admin (A15). Add audit fields (AD5). |
| `UserSession` | **remove** | Duplicates what `RefreshToken` already gives (one token = one device session) (AI2/AD6). If "list my devices" is wanted, add `DeviceName`, `IpAddress`, `UserAgent`, `LastSeenAtUtc` to `RefreshToken` instead of a second aggregate. |
| `SecurityToken` | **keep, build the feature** | The right shape for e-mail verification, password reset and change-e-mail (hashed one-time code, `MarkAsUsed`). Needed before the service is usable by real people. |
| `MfaMethod`, `MfaRecoveryCode` | **defer** | Fine design, but `Secret` is called "encrypted" and nothing encrypts it (AD6). Do not build MFA before a data-protection key story exists. Until then drop the claim from the code comment. |
| `ExternalLogin` | **keep, build later** | Needed for Google login (a validator already exists). Unique index on (provider, provider key). |
| `SecurityAuditLog` | **keep, start writing** | Never written today. Cheapest valuable addition: log login success/failure, password change, role change, token reuse. |
| Value objects | **keep** | Add reserved usernames and a `PasswordHash` format check (AD7). |

## 3. User
| Entity | Verdict | Why / change |
|---|---|---|
| `UserProfile` | **keep (done)** | Rebuilt per ADR 0004: validated updates, owned children, soft delete event. |
| `UserSettings` | **keep (done)** | Value-validated theme and language; owned by the aggregate. Consider storing future preferences as a JSON column instead of a column per switch. |
| `UserAddress` | **keep (done)** | One primary, max 10. Later: country code (ISO 3166) instead of free text. |
| `UserTenantProfile` | **defer** | Overlaps `UserProfile` (U7); `GamerTag` is game-specific naming (UD6). Decide with ADR 0001; likely becomes a separate table keyed by (user, tenant). |
| `Username` (User copy) | **remove later** | Duplicate of Auth's (U9). Move the rule to `Shared.Domain` once a second consumer needs it, or just store a plain validated string in User. |

## 4. Cross-service gaps worth fixing
1. **Account lifecycle events.** Auth publishes only `AccountRegistered`. User cannot learn about deactivation, deletion, e-mail change or username change. Add `AccountDeactivated`, `AccountDeleted`, `AccountEmailChanged` to `Auth.Contracts` (raised by the domain events from AD2), consumed by User to deactivate/anonymise the profile and refresh the display copies (UD2).
2. **No shared "who is the caller" contract beyond the token.** Roles are in the token; fine for now. When roles change, tokens stay valid until expiry (60 min). Acceptable; document it.
3. **Value objects duplicated** (`Username`, `Email`): decide the rule once, in one place.
4. **Soft delete is half done on both sides** (AD3): no query filter, reserved username/e-mail, no purge job. Pick: a global query filter on `IsDeleted` plus a Quartz purge after N days.

## 5. Suggested order (feeds the roadmap)
1. AD2 events + AD7 username rules (small, unlocks 4.1)
2. `IsSystem` roles + first admin (AD1, A15), migration
3. Remove `UserSession`, add device columns to `RefreshToken`
4. Start writing `SecurityAuditLog`
5. Account lifecycle events and the User consumer
6. `SecurityToken` features (verify e-mail, reset password): endpoints written, ADR 0009 (CI result awaited)
7. After ADR 0001: tenant entities
