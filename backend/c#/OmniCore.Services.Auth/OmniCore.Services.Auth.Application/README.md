# OmniCore.Services.Auth.Application

## Purpose
The use cases of the Auth service. Each use case is a MediatR command or query with its handler and FluentValidation validator, grouped by feature (`Features/Auth`, `Features/Roles`). The layer also declares the **interfaces** for security services (password hashing, JWT, refresh tokens, Google token validation) that `Auth.Infrastructure` implements. It orchestrates the `Account` / `Role` aggregates from `Auth.Domain`; business rules live in the aggregates, not here.

## What is inside

### Use cases
| Feature | Request | Handler does | Failures |
|---|---|---|---|
| Auth | `RegisterCommand(Username, Email, Password)` | checks e-mail/username uniqueness, hashes the password, `Account.Create` (raises `AccountCreatedDomainEvent`), adds the first refresh token, returns access + refresh token | 409 `Account.EmailAlreadyInUse` / `UsernameAlreadyInUse`, domain errors |
| Auth | `LoginCommand(Identifier, Password)` | loads account by e-mail or username, checks active + password, adds a refresh token (session cap from `IRefreshTokenService.MaxActiveSessions`), builds the JWT from the account's roles | 404 `Auth.AccountNotFound`, 401 `Auth.AccountInactive` / `PasswordNotConfigured` / `InvalidPassword` |
| Auth | `RefreshTokenCommand(RefreshToken)` | finds the account by token, `RotateRefreshToken`; on `Auth.TokenReuseDetected` saves explicitly (so the "revoke all sessions" is kept) and fails | 401 `Auth.InvalidRefreshToken`, `Auth.TokenReuseDetected` |
| Auth | `LogoutCommand(RefreshToken)` | revokes that refresh token; unknown token is still success (idempotent) | none |
| Auth | `GetAccountByIdQuery(AccountId)` | `AccountWithRolesSpecification` -> `AccountResponse` | 404 |
| Auth | `GetCurrentUserQuery` | takes the id from `ICurrentUserService` (token), sends `GetAccountByIdQuery` | 401 |
| Roles | `CreateRoleCommand`, `UpdateRoleCommand` | unique-name check, `Role.Create` / `role.Update`, evicts role cache keys | 409 `Role.NameAlreadyExists`, 404 |
| Roles | `AssignRoleToAccountCommand`, `RemoveRoleFromAccountCommand` | checks role/account exist, calls `account.AssignRole` / `RemoveRole` (raises role events) | 404 `Role.NotFound` / `Account.NotFound` |
| Roles | `GetRoleByIdQuery`, `GetRolesQuery` | read role(s), no paging | 404 |

Validators (`...Validator`) sit next to each request. Rules worth knowing: register username 3-30, e-mail <= 256 and valid, password 8-128 with upper, lower, digit and special; login identifier <= 256, password <= 128; role name <= 50.

### Domain-event handlers (run inside the same save, dispatched by the EF interceptor)
| Handler | Event | Does |
|---|---|---|
| `AssignDefaultRoleOnAccountCreatedEventHandler` | `AccountCreatedDomainEvent` | gives the new account the role `User` (logs an error and does nothing if the role is missing) |
| `PublishAccountRegisteredIntegrationEventHandler` | `AccountCreatedDomainEvent` | publishes `AccountRegisteredIntegrationEvent` (via the MassTransit outbox, so it commits with the account) |
| `AccountCacheInvalidationEventHandler` | `PasswordChanged`, `EmailConfirmed` | removes cache key `auth:account:id:<id>` |
| `AccountRoleCacheInvalidationEventHandler` | `AccountRoleAssigned`, `AccountRoleRemoved` | removes cache key `auth:account:id:<id>` |

### Abstractions (`Abstractions/Security`)
| Interface | Implemented by (Infrastructure) | Used by |
|---|---|---|
| `IPasswordHasherService` | bcrypt hasher | Register, Login |
| `IJwtTokenService` | `JwtTokenService` (RS256 / HS256 via `JwtKeySet`) | Register, Login, Refresh (`GenerateToken`, `ExpiryMinutes`) |
| `IRefreshTokenService` | `RefreshTokenService` | Register, Login, Refresh (`GenerateRefreshToken`, `MaxActiveSessions`) |
| `IGooglePayloadValidatorService` | `GooglePayloadValidatorService` | **nothing** (no endpoint, see A5/AN5) |

### DTOs and mapping
`AuthResponse(AccessToken, RefreshToken, ExpiresInMinutes)`, `AccountResponse(Id, Email, Username, IsActive, Roles)`, `RoleResponse`, request DTOs for the admin endpoints (`CreateRoleRequest`, `UpdateRoleRequest`, `AssignRoleToAccountRequest`, `RemoveRoleFromAccountRequest`). Mapping is by hand-written extension methods (`AccountMappingExtensions`, `RoleMappingExtensions`); AutoMapper is registered by Shared but not used here.

### Wiring
`AddAuthApplication()` calls `AddSharedApplication(ApplicationAssemblyReference.Assembly)`: MediatR handlers, the pipeline (logging, validation, save-on-success, performance) and all validators.

## Depends on / used by
- Projects: `Auth.Domain`, `Auth.Contracts`, `Shared.Application`. Package: `Google.Apis.Auth` 1.75.0 (only for the `Payload` type in the Google validator interface).
- Used by: `Auth.Infrastructure` (implements the interfaces), `Auth.Api` (sends the requests), tests (`Auth.Application.Tests`, 14 handler tests).

## Rules for this layer
- A handler returns `Result`; expected failures are `Error.*` values (NotFound, Conflict, Unauthorized, Validation), never exceptions. The status code follows the `ErrorType` (see Shared.Api).
- Commands change state through the aggregate (`account.AddRefreshToken`, `RotateRefreshToken`, `AssignRole`...). Do not edit entity properties from a handler.
- `UnitOfWorkBehavior` saves only on success. A handler that must persist something on a failure (reuse detection) calls `IUnitOfWork.SaveChangesAsync` itself.
- No EF, ASP.NET or HttpContext types. Caller identity comes from `ICurrentUserService`.
- Validate shape in the validator, business rules in the aggregate, lookups in the handler.

## How to ...
**Add a use case** (example: change password)
1. `Features/Auth/Commands/ChangePassword/ChangePasswordCommand.cs` with `record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;` (the account id comes from `ICurrentUserService`, not the command).
2. Handler: load account, `passwordHasher.VerifyPassword`, `account.ChangePassword(newHash)` (raises `PasswordChangedDomainEvent`), revoke other sessions.
3. Validator in the same folder (copy the register password rules; ideally share them, see AN7).
4. Add the endpoint in `Auth.Api`, a handler test in `tests/OmniCore.Services.Auth.Application.Tests` (copy `LoginCommandHandlerTests`) and an integration test.

**React to a domain event:** implement `IDomainEventHandler<TEvent>` in `Features/<Area>/EventHandlers`; it runs in the same transaction, so a failure rolls the save back.

**Use a new security service:** add the interface in `Abstractions/Security`, implement it in `Auth.Infrastructure/Services/Security`, it is registered by the Scrutor scan.

## Known gaps
Ids from `docs/repo-check.md`; new ones (`AN1`..`AN8`) are added there in the same commit.
- **AN1 (new, medium)** Register returns an access token with the role `"User"` hard-coded, while the real role row is attached by the event handler, which only logs when the `User` role does not exist. On a database without the seeded role, registration succeeds, the token claims `User`, and the account has no role. Make a missing default role a failure (or have the aggregate take the role id), and take the roles for the token from the saved account.
- **AN2 (new)** Role changes are not instant: roles are baked into the access token, so an assign/remove takes effect at the next refresh (up to the access-token lifetime). Removing a role does not revoke sessions. Acceptable for now; state it in the API docs or revoke sessions on role removal.
- **AN3 (new)** Dead cache code. `PasswordChanged` / `EmailConfirmed` are never raised by any command (no change-password or confirm-e-mail feature), nothing ever writes `auth:roles:all`, `auth:role:id:<id>` or `auth:account:id:<id>`, and `CachingBehavior` is not registered (AP1), so every eviction is a no-op. The key mismatch AP2 still applies (`GetAccountByIdQuery:<id>` vs `auth:account:id:<id>`). Delete the evictions or build caching properly, together.
- **AN4 (new, low)** Eviction runs before the save in `CreateRole` / `UpdateRole`; if the save fails the cache was cleared for nothing (harmless today, wrong once caching exists).
- **AN5 (new)** `IGooglePayloadValidatorService` has no caller, and the interface exposes `GoogleJsonWebSignature.Payload`, so the Application layer depends on `Google.Apis.Auth`. Return an own `ExternalIdentity` record instead when the feature is built (A5).
- **AN6 (new)** `IJwtTokenService` declares `ValidateToken`, `GenerateRefreshToken`, `IsTokenExpired`, `GetMinutesUntilExpiry` and `RefreshTokenExpiryDays` that no handler uses; refresh-token creation is already `IRefreshTokenService`. Trim the interface (tests use `ValidateToken`, keep that or move it).
- **AN7 (new, low)** Password rules are only in `RegisterCommandValidator`; a future change/reset password needs the same rules in one shared place. Login also compares e-mail/username after `Trim()` but register does not trim.
- **AN8 (new)** No audit trail: login success/failure, token reuse, role changes write no `SecurityAuditLog` row (the entity and table exist, nothing writes to it); `AccountDto` carries `PasswordHash` and is built on every `GetAccountById` only to be mapped away. Use `ToResponse(Account)` directly and drop the field.
- **A9** `Login` answers differently for unknown account, inactive account and wrong password (account enumeration); owner decision pending.
- **A8** No lockout / attempt counter; only the gateway's IP limit.
- **A15** Nothing assigns the first admin; `AssignRole` is admin-only.
- Missing use cases: change/reset password, confirm e-mail, MFA, Google login, session list/revoke, deactivate account, delete role (AA4).
