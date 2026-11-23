# OmniCore.Services.Auth.Infrastructure

## Purpose
Everything in the Auth service that touches the outside world: the PostgreSQL database (EF Core `AuthDbContext`, entity mappings, migrations, repositories, seeding), password hashing, JWT and refresh-token generation, the Google token validator, the Quartz job that purges old refresh tokens, and the wiring that plugs the Shared infrastructure (broker, outbox, cache, interceptors) into this service. It implements the interfaces declared in `Auth.Domain` (repositories) and `Auth.Application` (security services).

## What is inside

### Wiring (`DependencyInjection.cs`)
`AddAuthInfrastructure(config)` does, in order:
1. `AddSharedInfrastructure` (validated config classes, interceptors, cache, Quartz, SignalR).
2. `AddDbContext<AuthDbContext>`: Npgsql from the validated `DatabaseConfig`, history table `auth.__EFMigrationsHistory`, snake_case names, retry and timeout from config, Shared `ISaveChangesInterceptor`s (audit, domain events).
3. `IUnitOfWork` = `UnitOfWorkBase<AuthDbContext>`.
4. `AddMassTransitWithBroker<AuthDbContext>` (RabbitMQ or Kafka, EF outbox in this context).
5. Scrutor scan of this assembly: classes named `*Repository` and `*Service` are registered **scoped** under their interfaces (no registration to write for new ones).
6. Quartz purge job `PurgeRefreshTokensJob` (first run 2 minutes after start, then every `RefreshTokenCleanup__IntervalHours`, default 6).
7. Data seeders (`AddDataSeeders`).

`UseAuthInfrastructureAsync(seedData)` runs `ApplyMigrationsAsync<AuthDbContext>` and then the seeders at startup.

### Persistence
| Item | Detail |
|---|---|
| `AuthDbContext` | schema `auth`; sets for Accounts, AccountRoles, ExternalLogins, MfaMethods, MfaRecoveryCodes, RefreshTokens, Roles, SecurityAuditLogs, SecurityTokens, UserSessions, plus the MassTransit outbox tables. The soft-delete query filters are commented out (see AI3). |
| `AuthDbContextFactory` | design-time factory for `dotnet ef` / `add-migration.ps1`. |
| `Configurations/*` | one `IEntityTypeConfiguration` per entity: table names (`accounts`, `refresh_tokens`, ...), value-object converters (`AccountId`, `Username`, `EmailAddress`, `PasswordHash`...), indexes (unique username, unique e-mail where not null, unique role name, unique `token_hash`), cascade deletes from account. `RefreshTokenConfiguration` also maps Postgres `xmin` as the concurrency token (ADR 0003). |
| `Repositories/AccountRepository` | `GetById`, `ByUsername`, `ByEmail`, `ByIdentifier` (e-mail or username), `GetByRefreshTokenAsync` (hashes the token with SHA-256 and looks up `token_hash`), uniqueness checks, `GetLocalById` (change tracker, used by the default-role event handler). Every `GetBy...` includes refresh tokens, account roles and roles. |
| `Repositories/RoleRepository` | `GetByName`, `IsNameUnique` (case-insensitive via `ToLower()`). |
| `Repositories/SessionRepository` | `UserSession` queries and a bulk revoke; **nothing calls it** (AI2). |
| `Seeds/RoleSeeder` | creates `User`, `Admin`, `SuperAdmin` when missing (order 1). Creates no admin account (A15). |
| `Migrations/` | 9 migrations, the latest `20261120080000_hash_refresh_tokens` (hashes stored tokens, backfills). The plain `token` / `replaced_by_token` columns still exist until a follow-up migration drops them (A6). |

### Security services (`Services/Security`)
| Class | Implements | Notes |
|---|---|---|
| `PasswordHasherService` | `IPasswordHasherService` | BCrypt.Net, work factor 12 hard-coded. |
| `JwtTokenService` | `IJwtTokenService` | Signs access tokens with RS256 (key from `JwtKeySet`, `kid` header) or HS256 while `Jwt__Secret` is set; claims: id, e-mail, name, roles, jti. See [docs/auth-tokens.md](../../../../docs/auth-tokens.md). |
| `RefreshTokenService` | `IRefreshTokenService` | 64 random bytes (Base64), lifetime and `MaxActiveSessions` (session cap, default 10) from `JwtConfig`. |
| `GooglePayloadValidatorService` | `IGooglePayloadValidatorService` | Validates a Google ID token against `GoogleAuth__ServerClientId`. No caller yet. |

### Config classes (`Configs`)
| Section | Class | Keys |
|---|---|---|
| `GoogleAuth` | `GoogleAuthConfig` | `ServerClientId` (required), `ClientId` |
| `RefreshTokenCleanup` | `RefreshTokenCleanupConfig` | `RetentionDays` (30), `IntervalHours` (6), `Enabled` (true) |

Shared sections (`Database`, `MessageBroker`, `Redis`, `Jwt`, ...) are documented in `Shared.Infrastructure/README.md`; values for compose are in `infra/docker/.env.example`.

### Jobs
`PurgeRefreshTokensJob`: one bulk `DELETE` of refresh-token rows expired for more than `RetentionDays`. `[DisallowConcurrentExecution]`, safe on several replicas.

## Depends on / used by
- Projects: `Auth.Application` (and through it Domain, Contracts), `Shared.Infrastructure`. Package: `BCrypt.Net-Next` 4.2.0.
- Used by: `Auth.Api` (calls `AddAuthInfrastructure` and `UseAuthInfrastructureAsync`) and the integration tests.

## Rules for this layer
- Implement interfaces from Domain/Application here; no business rules (they belong in the aggregate).
- Repositories return **aggregates**, not DTOs, and expose only what a use case needs. Name them `XRepository` / `XService` so the scan finds them.
- An entity change and its migration go in the **same commit** (`CLAUDE.md`). A schema or token-format change needs an ADR first.
- Never log or return secrets: refresh tokens are stored only as hashes, the password only as a BCrypt hash.
- A bulk operation (`ExecuteUpdate/Delete`) bypasses the change tracker and the save interceptors (no audit columns, no domain events); use it only for maintenance like the purge job.

## How to ...
**Add a migration:** change the entity and its configuration, then `pwsh backend/c#/scripts/shells/add-migration.ps1` (uses `AuthDbContextFactory`); commit the migration, its `.Designer.cs` and the updated snapshot together. Give it a meaningful name (AI8).

**Add a repository:** declare the interface in `Auth.Domain/Repositories`, implement `class XRepository(AuthDbContext db) : RepositoryBase<X, XId>(db), IXRepository` here. It is registered automatically.

**Add a seeder:** implement `IDataSeeder<T>` (`Order`, `SeedAllAsync`); it runs at startup after the migration.

**Change the session cap or purge:** `Jwt__MaxActiveSessions`, `RefreshTokenCleanup__RetentionDays`, `__IntervalHours`, `__Enabled`.

## Known gaps
Ids from `docs/repo-check.md`; new ones (`AI1`..`AI8`) are added there in the same commit.
- **AI1 (new, medium)** Usernames are case-sensitive: `Alice` and `alice` are two accounts (the unique index and the lookups compare the exact string), while e-mail is lower-cased by the value object. Impersonation risk and confusing login. Normalise in the `Username` value object (or index on `lower(username)`) with a migration that first checks for existing duplicates: needs a short ADR.
- **AI2 (new)** `SessionRepository`, `UserSession` and the `user_sessions` table are unused: refresh tokens already act as sessions (cap, rotation, reuse detection). Decide in the entity review (`docs/entities.md`): drop it or make it the single session record.
- **AI3 (new, low)** Soft delete is not enforced by the data layer: the `HasQueryFilter` lines in `AuthDbContext` are commented out, so repositories return soft-deleted accounts. Login is still refused because `Account.SoftDelete()` sets `IsActive = false`, but the username and e-mail stay reserved (AD3, D1).
- **AI4 (new, low)** `GoogleAuthConfig.ServerClientId` is `[Required]`, so the service refuses to start without a Google client id even though no endpoint uses Google login (the integration test factory has to set it too). Make it optional until the feature exists.
- **AI5 (new, low)** BCrypt work factor is hard-coded to 12 and passwords are never re-hashed on login when the cost changes. Move the cost to config and add `NeedsRehash` on login.
- **AI6 (new, low)** Every account lookup loads the refresh tokens, roles and role rows (also for the refresh-token lookup). Fine at the current size (tokens capped at 10 + rows awaiting purge); revisit with projections when it shows up in a profile.
- **AI7 (new, low)** Role lookups use `ToLower()` in SQL, which cannot use the (case-sensitive) unique index on `name`; two parallel creates of `Admin` / `admin` pass the check and are separated only by the exact-case index. Harmless with three roles.
- **AI8 (new, low)** Migration names are timestamps (`migrate_auto_...`), which hides what each one does; the old plain-token columns remain (A6).
- **A18** Migrations and seeding run in every replica at startup (a race on first start of several replicas); `/health` does not check the database.
- **A10** closed by ADR 0003 (cap 10 + purge). **A7** closed (xmin). **A6** open until the plain-token columns are dropped.
- **A15** No first admin account is created by the seeder.
