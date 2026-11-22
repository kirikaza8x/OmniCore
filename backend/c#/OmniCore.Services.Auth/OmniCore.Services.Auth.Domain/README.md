# OmniCore.Services.Auth.Domain

## Purpose
The business model of the Auth service, free of frameworks: the `Account` aggregate with its refresh tokens and roles, the `Role` aggregate, supporting entities (external logins, MFA, security tokens, audit log, sessions), value objects with their validation, domain events, and the repository / specification **interfaces**. Rules such as "a revoked refresh token that is presented again revokes every session" or "at most N active sessions" live here, so they are unit-tested without a database.

## What is inside

### Aggregates and entities (`Entities/`)
| Type | Kind | What it models | Status |
|---|---|---|---|
| `Account` (3 partial files) | aggregate root, `IAuditableEntity`, `ISoftDeletable` | username, optional e-mail, password hash, `IsEmailConfirmed`, `IsActive`, soft-delete fields; owns refresh tokens and account roles | **in use** |
| `RefreshToken` | entity | SHA-256 hash of the token (`TokenHash`), expiry, `IsRevoked`, `ReplacedByTokenHash`, `xmin` concurrency in the mapping | **in use** |
| `AccountRole` | junction | account to role, `AssignedAtUtc` | **in use** |
| `Role` | aggregate root | `Name` (unique), `Description`; no audit fields, no events | **in use** |
| `ExternalLogin` | entity | provider + provider key for Google-style login | table only, no feature |
| `MfaMethod`, `MfaRecoveryCode` | entities | TOTP/SMS/passkey method (`Secret` is documented as "encrypted" but nothing encrypts), hashed recovery codes | table only |
| `SecurityToken` | entity | hashed one-time code for e-mail verification, password reset, login OTP, e-mail change; `MarkAsUsed` | table only |
| `SecurityAuditLog` | entity | audit rows (`Log(...)` factory) | table only, never written |
| `UserSession` | aggregate root | device/IP/user-agent session linked to a refresh token | table only, duplicates refresh-token sessions (AI2) |

### `Account` behaviour
| Method | Rule |
|---|---|
| `Account.Create(username, email?, passwordHash?, firstName?, lastName?)` | validates the value objects, `IsActive = true`, raises `AccountCreatedDomainEvent` |
| `AddRefreshToken(token, duration, maxActiveSessions?)` | when the cap is reached the **oldest active** tokens are revoked to make room |
| `RotateRefreshToken(current, new, duration)` | unknown token: 401; **already revoked: reuse detected, all tokens revoked**, failure `Auth.TokenReuseDetected`; expired: failure; otherwise revoke the old (remember the new hash) and add the new one |
| `RevokeRefreshToken(token)` | revokes one token; already revoked is success, unknown is `NotFound` |
| `RevokeAllRefreshTokens()` | revokes every active token |
| `AssignRole(roleId)` / `RemoveRole(roleId)` | conflict if already assigned, not found if absent; raise `AccountRoleAssigned/RemovedDomainEvent` |
| `AttachEmail(email)` | sets the address and resets `IsEmailConfirmed` |
| `ConfirmEmail()` | needs an address, raises `EmailConfirmedDomainEvent` |
| `UpdatePassword(hash)` | sets the hash, **revokes all refresh tokens**, raises `PasswordChangedDomainEvent` |
| `Deactivate()` / `SoftDelete()` | `IsActive = false` and revoke all tokens; `SoftDelete` also sets `IsDeleted` and `DeletedAtUtc`. A deleted account therefore cannot log in, although repositories still return it (AI3) |

### Value objects (`ValueObjects/`)
| Type | Rule |
|---|---|
| `Username` | trimmed, `^[a-zA-Z0-9_.-]{3,30}$`, **case-sensitive** (AI1) |
| `EmailAddress` (`Email.cs`) | trimmed and lower-cased, format check |
| `PasswordHash` | non-empty string, no format check |
| `IpAddress`, `UserAgent` | validated strings for sessions |
| `AccountId`, `RoleId`, `RefreshTokenId`, `UserSessionId`, `SecurityTokenId`, `ExternalLoginId`, `MfaMethodId`, `MfaRecoveryCodeId`, `SecurityAuditLogId` (`Identifiers.cs`) | strongly typed Guid ids with `New()` |

### Other
| Item | What it is |
|---|---|
| `Events/AccountEvents.cs` | `AccountCreated`, `EmailConfirmed`, `PasswordChanged`, `AccountRoleAssigned`, `AccountRoleRemoved` (all `DomainEvent`). Dispatched by the EF interceptor before the commit. |
| `Enums/MfaType` (`Totp`, `Sms`, `Passkey`), `SecurityTokenType` (`EmailVerification`, `PasswordReset`, `LoginOtp`, `ChangeEmail`) | note the file `MfaType.cs.cs` (double extension) |
| `Repositories/IAccountRepository`, `IRoleRepository`, `ISessionRepository` | contracts implemented in `Auth.Infrastructure` |
| `Specifications/AccountWithRolesSpecification` | account (by id or by e-mail/username) with roles, no tracking; only the by-id form is used |

## Depends on / used by
- Projects: `Shared.Domain` only (DDD base types, `Result`, `Error`, `IRepository`, `BaseSpecification`).
- Used by: `Auth.Application`, `Auth.Infrastructure` (mappings, repositories), `Auth.Domain.Tests` (17 unit tests).

## Rules for this layer
- Change state only through aggregate methods that return `Result`; setters are private. Do not add EF, ASP.NET or MassTransit types.
- Validate input in value objects (`Create` returns `Result<T>`), never accept a raw string where a value object exists.
- Raise a domain event for anything another part of the system must react to; handlers run in the same transaction.
- Anything that ends sessions (password change, deactivate, delete, reuse detected) calls `RevokeAllRefreshTokens()`.
- Keep secrets out of entities in readable form: tokens and codes are stored as hashes only.

## How to ...
**Add a rule to Account:** add a method in the right partial file (`Account.Roles.cs`, `Account.RefreshTokens.cs`, or `Account.cs`), return `Result`, raise an event if needed, add a unit test in `tests/OmniCore.Services.Auth.Domain.Tests` (copy a refresh-token test), and update the handler in `Auth.Application`.

**Add an entity or a column:** entity here, its `IEntityTypeConfiguration` and migration in `Auth.Infrastructure` in the same commit (`CLAUDE.md`).

**Add a value object:** `public sealed record X { public static Result<X> Create(string? v) {...} }`, plus a converter in the entity configuration.

## Known gaps
Ids from `docs/repo-check.md`; new ones (`AD1`..`AD8`) are added there in the same commit. The wider entity review is in `docs/entities.md`.
- **AD1 (new, medium)** System roles are ordinary rows identified by **string literals** (`"Admin"`, `"SuperAdmin"` in the endpoints, `"User"` in Application and the seeder). `UpdateRole` lets an admin rename them, which silently breaks authorization (`RequireRoles("Admin")`) and default-role assignment. Add `RoleNames` constants in the Domain and an `IsSystem` flag (rename/delete refused), with a migration.
- **AD2 (new)** `Deactivate`, `SoftDelete` and `AttachEmail` raise **no** domain event, so nothing (no integration event, no cache eviction) tells other services that an account was disabled, deleted or changed its e-mail (AC1). They also ignore their actor parameters (`reasonBy`, `deletedBy`, `updatedBy` are never used), so the audit trail does not say who did it.
- **AD3 (new)** Soft delete is half done: a deleted account keeps its username and e-mail reserved forever, there is no restore, no purge and no query filter (AI3). Decide the policy with D1 (one flag or two) before building account deletion.
- **AD4 (new, low)** Entities call `DateTime.UtcNow` directly (`RefreshToken`, `AccountRole`, `SecurityToken`, `UserSession`, `Account`), so time cannot be faked in tests and expiry tests would need to sleep. Pass a clock value into the factory methods.
- **AD5 (new, low)** Collections are inconsistent: `RefreshTokens` and `AccountRoles` are encapsulated, but `ExternalLogins`, `SecurityTokens`, `MfaMethods`, `MfaRecoveryCodes`, `SecurityAuditLogs` and `Role.AccountRoles` are public `ICollection` with setters, so outside code can bypass the aggregate. `Role` has no audit fields although `Account` has.
- **AD6 (new, low)** Unused or duplicate pieces: `UserSession` (see AI2) with two overloaded `Create` methods, the by-identifier constructor of `AccountWithRolesSpecification` (exact-case compare, never used), `ISessionRepository`, `MfaMethod.Secret` documented as encrypted with no encryption service anywhere.
- **AD7 (new, low)** Value objects accept too much or too little: `Username` allows names such as `admin` / `root` (no reserved list) and is case-sensitive (AI1); `PasswordHash` accepts any non-empty string (it could check the BCrypt prefix); `AttachEmail` is not unique-checked in the domain (the unique index decides).
- **AD8 (new, low)** `AccountCreatedDomainEvent` carries `Email` as an empty string when there is none, and first/last name that no one collects (AC2). Use a nullable e-mail and drop the names.
- **D1** `ISoftDeletable` only has `IsActive`, while `Account` has `IsDeleted` / `DeletedAtUtc` as well (shared kernel finding, decide together with AD3).
- No rule stops removing the last admin, removing the `User` role from an account, or assigning a role to a deactivated account.
