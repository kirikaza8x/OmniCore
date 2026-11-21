# Repo check: findings

Written from a read-through of the repo and verified against the code. **Nothing was built or run** (no .NET SDK in the
review environment), and there are no tests. Each finding names the [roadmap](roadmap.md) phase that fixes it.
Findings from the code review of each service are appended below as they are done.

**Update 2026-11-20:** the solution now builds and the Auth unit and integration tests pass in GitHub Actions, so the
"fixed, not built" notes below are now built (compile and CI only: only the register / login / refresh / reuse path is
tested). Building found three things the read-through missed (A19 below, a syntax error in `RegisterCommandValidator`,
missing xunit usings in the test projects), which is why review without a build is not enough. Docker-related items
(F3, F4, F7) are still only checked with `docker compose config`.

| # | Finding | Why it matters | Phase |
|---|---|---|---|
| 1 | **No automated tests and no CI**: no `tests/`, no `.github/`, and `OmniCore.slnx` has no test project | A broken build or a regression is only found by hand | 1 |
| 2 | **User service is not reachable through the gateway**: `nginx.conf` routes only `/api/v1/auth/` and `/specs/auth.json`, although `SWAGGER_URLS` already lists `/specs/user.json` | The "single entry point" covers only Auth, and the Swagger UI entry for User is dead | 3 |
| 3 | **Unsafe compose defaults** in `docker-compose.services.yml`: `Database__EnableSensitiveDataLogging` and `EnableDetailedErrors` default to `true`, `Cors__AllowAnyOrigin` to `true`, `AllowedHosts` to `*` | Fine for local dev, dangerous if the file is reused for a server | 2 (**fixed, not started**: the three switches now default to `false` and `.env.example` enables them for dev; `docker-compose.prod.yml` requires `ALLOWED_HOSTS` and an explicit CORS origin. `AllowedHosts` still defaults to `*` in the base file) |
| 4 | **Guessable example secrets** in `infra/docker/.env.example` (`POSTGRES_PASSWORD`, `guest/guest`, `minioadmin`, a readable `JWT_SECRET`); the **root `.env.example` is empty** | Copying the example gives a working but insecure setup; the empty root file misleads | 2 (**fixed**: `CHANGE_ME` placeholders, root file removed. The `JWT_SECRET` placeholder is 32+ chars so the stack still starts, which also means a forgotten replacement is not caught at startup) |
| 5 | **One shared symmetric JWT secret** (HS256) in every service (`JwtTokenService`, `JwtAuthenticationExtensions`) | Any service that can validate a token can also forge one; non-.NET services would need the secret too | 4 (**code done, green in CI 2026-11-20**: RS256 + JWKS; the owner still has to set the key and remove `JWT_SECRET`) |
| 6 | **Gateway has no rate limiting and no auth check**; rate limiting exists only inside the services (`RateLimiterExtensions`) | Abuse protection depends on every service being configured | 3 |
| 7 | **Unpinned images**: `swaggerapi/swagger-ui:latest`, `provectuslabs/kafka-ui:latest`, `apache/kafka:latest`, `minio/minio:latest` | Builds are not reproducible; a `latest` bump can break the stack | 2 (**fixed and verified in CI 2026-11-20**: all eight images pinned and each tag exists. **New finding:** the official `minio/minio` image was removed from Docker Hub and Quay (even old release tags 404), so the old `latest` could no longer be pulled. Stopgap: `coollabsio/minio:RELEASE.2025-10-15T17-29-55Z`, a third-party rebuild with the same CLI that starts healthy in CI. **Owner decision:** keep that fork, build MinIO from source yourself, or move to another S3-compatible store (SeaweedFS, Garage, RustFS were all pullable); the code only needs an S3 API) |
| 8 | **Both brokers on by default in compose** (`MessageBroker__Provider=Both`) while the code default is `RabbitMQ`, and the outbox is documented as RabbitMQ-only | Two systems to run and keep consistent; unclear which events are outbox-protected | 6 |
| 9 | No published contracts for non-.NET consumers (no OpenAPI export, no event schemas) | Other-language services cannot generate clients | 5 |
| 10 | Single auth + single user model: `UserTenantProfile` exists in User, `Tenant` exists as a base type in `Shared.Domain`, but there is no tenant concept in Auth, tokens or queries | Blocks several services sharing one identity across tenants | 8 |

## 2a. OmniCore.Shared review

Read-through of `Shared.Api`, `Shared.Application` behaviours, and the messaging / config / storage parts of
`Shared.Infrastructure`. **Nothing was built or run** (no .NET SDK, no Docker daemon): every "fixed" below is a source
change that still needs `dotnet build` and a smoke test by the owner. Status: **fixed** / **open** (with the phase).

| # | Severity | Finding | Status |
|---|---|---|---|
| S1 | high | Compose defaulted `MessageBroker__Provider` to `Both`, but `MessageBrokerConfig.Validate` only accepts `RabbitMQ` or `Kafka`, so a stack started from `.env.example` fails at startup. `MESSAGE_BROKER_*` were also missing from `.env.example` | **fixed**: default is `RabbitMQ`; both keys added to `.env.example`. "Both" was never a working mode (code treats anything but `Kafka` as RabbitMQ), so F8 is now about dropping Kafka from the default stack (phase 6) |
| S2 | high | Per-client rate limiting collapses behind the gateway: `UseForwardedHeaders` is only called inside `UseSwaggerDocumentation` (so not when Swagger is off) and trusts only loopback proxies by default, so `RemoteIpAddress` is the nginx container for every anonymous caller. All anonymous callers share one `ip:` partition: one client can exhaust the quota for everyone, and a single IP is not actually limited | open (mitigated 2026-11-20: the gateway now does the per-IP limiting, so this limiter is a coarse backstop; the code fix still needs trusted-proxy config) |
| S3 | medium | Only the global limiter applies (100/min per user or IP, configured by compose). `RequireRateLimiting` is never used and compose defines a single policy `fixed`, so login and refresh have no stricter limit | **mitigated at the gateway** 2026-11-20 (credential endpoints 10/min per IP, see ADR 0002); the in-service policies are still unused |
| S4 | low | `AddCustomRateLimiting` and `RateLimitPolicies` were dead code with policies from another project (Payment, Order, AiGenerate, an unregistered `Webhook`) | **fixed**: removed |
| S5 | medium | JWT bearer validation did not pin the algorithm; `JwtConfig` (32-char secret, issuer, audience) was bound with plain `Configure`, so a short secret only failed at the first token | **fixed**: `ValidAlgorithms = HS256`; `AddConfig<JwtConfig>()` validates on start. `RequireHttpsMetadata=false` and `SaveToken=true` left as they are (revisit with JWKS, phase 4) |
| S6 | medium | `CachingBehavior` is `internal` and never registered in `AddSharedApplication`, so `ICacheableQuery` does nothing (GetRoles, GetRoleById, GetAccountById, GetUserProfileById are all uncached). It also passes no cancellation token to `next()` and the key is whatever the query supplies | open: do not just register it, cached reads need an invalidation review first (phase 7, with Auth/User) |
| S7 | medium | `MapHub<LogHub>("api/logHub")` was anonymous. Nothing publishes to it today (`AddSignalRLogger` is never called), but compose turns on `EnableSensitiveDataLogging`, so enabling the logger would stream sensitive logs to anyone | **fixed**: `RequireAuthorization()` |
| S8 | medium | CORS with `Cors__AllowAnyOrigin=true` uses `SetIsOriginAllowed(_ => true)` + `AllowCredentials()`, i.e. any origin with credentials; with no config the fallback is `AllowAnyOrigin` | open, phase 2 (compose default of `true` is F3) |
| S9 | low | `GlobalExceptionHandler` logs client cancellations as errors and maps everything to 500 (no 409 for concurrency, no 499 for aborted requests) | open |
| S10 | low | `DispatchDomainEventsInterceptor.SavingChanges` (sync) blocks on async dispatch (`GetAwaiter().GetResult()`). The async path used by the unit of work is fine; domain events do run before the commit, in the same transaction, with a loop guard of 10 | open |
| S11 | medium | `appsettings*.json` are git-ignored and absent, yet `AddApi` throws if `RateLimiting` is missing (and `Jwt`, `MessageBroker` are required too). Services only start with the compose environment variables | docs corrected (README); a documented `appsettings.Development.json.example` is a phase 2 item |
| S12 | low | `UnitOfWorkBehavior` calls `SaveChangesAsync` after every command; `ITransactionalCommand` exists but nothing uses it, so there is no explicit transaction around handlers that save more than once | open, phase 7 |
| S13 | low | `MinioStorageService` trusts the client's `ContentType` and file name (sanitised only against invalid path characters); the object key gets a GUID prefix, so there is no overwrite or traversal | noted; content-sniffing is in the User review |

### Findings from the per-layer READMEs (`README.md` in each project; started 2026-11-21)
| # | Sev | Finding | Status |
|---|---|---|---|
| D1 | medium | `ISoftDeletable` is only `IsActive`; `AuditableEntityInterceptor` turns a hard delete into `IsActive = false`, while `Account` also has `IsDeleted` / `DeletedAtUtc` that nothing sets through the interceptor. Two meanings (inactive vs deleted) share one flag | open, see `docs/entities.md` |
| D2 | low | `PagedQuery.SortColumn` defaults to `"CreatedAt"`; entities that are not `IAuditableEntity` (e.g. `Role`) have no such column | open |
| D3 | low | `IDomainEvent : INotification` ties Shared.Domain to MediatR | open, accepted for now |
| D4 | low | Namespaces do not match folders (`Data/Repositories` -> `...Domain.Repositories`, `ValueObject` vs `ValueObjects`, `OmniCoreApplicationException` is in Domain's `Abstractions/` but declares `...Application.Exceptions`) | open |
| D5 | low | Two value-object styles (abstract `ValueObject` class and sealed records) | open, use records for new code |
| D6 | low | Unused: `Guard`, `DomainException`, `OmniCoreApplicationException`, `Address`, `Money`, `DateRange`, `ITenantScoped`, `TenantEntity` | open, decide with ADR 0001 |
| C1 | low | `IntegrationEvent` has no version / event-type field; `IPartitionedIntegrationEvent` does not extend `IIntegrationEvent`; without a partition key Kafka uses a random GUID, so no per-aggregate ordering | open |

## 2b. OmniCore.Services.Auth review

Read: register / login / refresh / logout handlers, `Account` refresh-token logic, `RefreshToken`, the repository,
JWT / refresh / password / Google services, all endpoints, the event handlers, role seeder, DI, and the account and
refresh-token EF configurations. **Not reviewed:** the EF migrations against the model, the Role/MFA/ExternalLogin/
SecurityToken entities beyond their existence, and the other Role handlers. **Nothing was built or run**; every
"fixed" is a source change that needs `dotnet build` plus a smoke test.

| # | Severity | Finding | Status |
|---|---|---|---|
| A1 | high | Refresh-token **reuse detection never persisted**: `RotateRefreshToken` revokes all tokens on a reused one but returns a failure, and `UnitOfWorkBehavior` saves only on success, so the revocation was dropped | **fixed**: the handler saves explicitly on `Auth.TokenReuseDetected` |
| A2 | high | `PUT` update-role needed only a signed-in user (any account could edit roles); `GetAccountById` let any signed-in user read any account (email, roles). Create/assign/remove/get roles were already admin-only | **fixed**: both require `Admin`/`SuperAdmin`; `/me` is the self endpoint (breaking for non-admin callers of `GET /accounts/{id}`) |
| A3 | medium | Login cached an account DTO **including the password hash** in Redis for 10 min (stale after a password change; the invalidation handler used a different key, `auth:account:id:`, so it never matched) | **fixed**: login reads the aggregate from the database; the unused invalidation handler stays (cleanup with F-cache work, phase 7) |
| A4 | medium | Register allowed usernames of 31-50 chars (validator 50, value object and column 30) so they failed late; no max length on password (bcrypt reads 72 bytes, large bodies were hashed) or email | **fixed**: limits 30 / 128 / 256. Login already had `NotEmpty` rules (kept) plus the same maxima |
| A5 | low | `JwtTokenService.ValidateToken` did not pin the algorithm (used by nothing on the request path today) | **fixed** |
| A6 | high | **Refresh tokens are stored in plain text** (`refresh_tokens.token`, unique). A database read gives live sessions | **fixed in code (ADR 0003 step 1, CI green)**: SHA-256 stored, backfilled so sessions survive. Remaining: a later migration drops the old plain `token` column (still holds old values) |
| A7 | medium | No concurrency control on rotation: two parallel refreshes with the same token both succeed and fork the session (no row version / `xmin`) | **fixed (ADR 0003 step 2, CI green)**: xmin concurrency token, test with 8 parallel refreshes |
| A8 | medium | No brute-force protection: no lockout or failed-attempt counter, and no endpoint-specific rate limit (see S3). bcrypt cost 12 is fine | **partly mitigated** 2026-11-20: gateway limit of 10 credential requests/min per IP slows guessing (ADR 0002); still no lockout or failed-attempt counter, so open |
| A9 | medium | Account enumeration: login answers `NotFound` ("No account found matching ...") vs `Unauthorized` for a bad password; register answers `Conflict` for a taken email | **needs owner decision**: one generic 401 is the usual answer but changes the API contract |
| A10 | low | `AddRefreshToken` is called with no `maxActiveSessions`, so every login adds a row, and expired or revoked tokens are never purged (Quartz is available) | **fixed (ADR 0003 step 3, CI green)**: cap 10 per account, Quartz purge of rows expired > 30 days |
| A11 | low | Register uniqueness is check-then-insert: a race hits the unique index and surfaces as a 500 instead of a 409 | **fixed** 2026-11-20, untested: `GlobalExceptionHandler` now maps a Postgres unique violation (23505) to a 409 problem response instead of a 500 |
| A12 | medium | Register returns tokens immediately; `IsEmailConfirmed` exists, and `SecurityToken` / `MfaMethod` / `MfaRecoveryCode` entities exist, but there are no confirmation, reset or MFA endpoints | open, product decision |
| A13 | low | `GooglePayloadValidatorService` and `ExternalLogin` exist but nothing calls them (no Google endpoint), so the README / docs claim of "Google sign-in" was wrong; the validator echoes exception text into its error | docs corrected; open |
| A14 | low | Register hard-codes `roles: ["User"]` in the token while the role is assigned by a domain-event handler that silently skips when the role is missing | open, phase 7 |
| A15 | medium | **No way to get the first admin**: `RoleSeeder` seeds roles only, and role endpoints need `Admin` | open, needs owner decision (seed from env, or a one-off CLI) |
| A16 | low | `RefreshToken` uses `DateTime.UtcNow` directly (not `IDateTimeProvider`), which makes expiry hard to unit-test; `AccountDto` still carries `PasswordHash` | open, phase 1 (with tests) |
| A17 | low | Access tokens cannot be revoked (logout only revokes one refresh token; 15 min default lifetime); no logout-all | accepted for now |
| A18 | low | Migrations run at startup in every replica (`UseAuthInfrastructureAsync`); `/health` is a bare liveness check with no database or broker probe | open, phase 2 |
| A19 | high | `PasswordHasher` did not end in `Service`, so the Scrutor scan in `AddAuthInfrastructure` never registered `IPasswordHasherService`: Register and Login handlers could not be constructed (the app would fail at the first register/login request in Production, at startup with validation on) | **fixed**: class and file renamed `PasswordHasherService`; found by the first CI integration run. Convention to keep: the scan registers only `*Service` and `*Repository` classes |
| A20 | medium | `Account.IsActive` was never set to `true` in the domain; it relied on the column `HasDefaultValue(true)`, so a new account was inactive in memory until reloaded (any code or test that checks `IsActive` before a save would reject it) | **fixed**: set in the constructor, no schema change; found by the first handler tests |

## 2c. OmniCore.Services.User review

Read: the whole Domain and Application projects, `Program.cs`, the csproj references and the compose service.
**Nothing was built or run.** The headline: **User is a design sketch, not a running service yet.**

| # | Severity | Finding | Status |
|---|---|---|---|
| U1 | high | `Program.cs` registers none of the layers (no `AddApi`, no application or infrastructure registration), and `Infrastructure` holds only an `AssemblyReference`: no DbContext, no repository implementation (`IUserProfileRepository` has none), no migration, no Carter endpoints. The compose `user-api` container therefore serves nothing; it probably fails at startup, because `UseApi` calls `UseAuthorization`, `UseExceptionHandler` and `MapHub` without the services `AddApi` registers (inferred from the code, not run) | open, phase 2/7: decide whether to finish it or drop it from compose |
| U2 | high | `CreateTenantProfileCommandHandler` builds a `UserTenantProfile` and returns it as a success but **never attaches or saves it** (the aggregate has no `AddTenantProfile`, `_tenantProfiles` is private and never written): silent data loss | open: left for the multi-tenancy work (ADR 0001), because the model is likely to change |
| U3 | high | Tenant and user ids come from the **request body** (`CreateTenantProfileCommand.TenantId` / `UserId`, `CreateUserProfileCommand.UserId`, `UploadAvatarCommand.UserId`), not from the token, so a caller could act on another user or tenant once endpoints exist | open: rule for the ADR (ids from `ICurrentUserService` / `ITenantContext` only) |
| U4 | medium | `UploadAvatarCommandHandler` deleted the old avatar before uploading the new one, so a failed upload left the profile pointing at a missing file | **fixed**: old file is removed after the upload and attach |
| U5 | medium | Avatar validation trusts the client `ContentType` (5 MB cap, jpeg/png/webp list) and does not check the file signature | open, with S13 |
| U6 | medium | No consumer exists for `AccountRegisteredIntegrationEvent` (Auth publishes it): nothing creates a `UserProfile` when an account registers, and `UserProfile` copies `Username` and `Email` from Auth with no update path | open, phase 5/7 |
| U7 | medium | Two aggregate roots overlap: `UserProfile` (global) contains `TenantProfiles`, while `UserTenantProfile` is itself a `TenantAggregateRoot` with a `UserProfile` navigation. The tenant-aware part is not isolated by any filter yet | open: ADR 0001 |
| U8 | low | `GetUserProfileByIdQuery` is `ICacheableQuery` but `CachingBehavior` is not registered (S6), so it is never cached; the invalidation handler is correct | open (S6) |
| U9 | low | `Username` value object is duplicated between Auth and User (and email uses the Shared one) | open, phase 7 |
| U10 | low | Swagger title said "Auth API" | **fixed** |

