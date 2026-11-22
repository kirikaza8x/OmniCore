# OmniCore.Services.Auth (master overview)

The accounts service: registration, login, logout, refresh-token rotation, the current user, roles, and the JWTs (RS256, with a public JWKS) that every other service trusts. It owns its own PostgreSQL schema (`auth`) and publishes `AccountRegisteredIntegrationEvent`. It is built from five projects with the same layering as every OmniCore service; each project has its own `README.md`, this page is the map.

## The five projects

| Project | One-line role | Details |
|---|---|---|
| `OmniCore.Services.Auth.Domain` | The model: `Account` (refresh tokens, roles), `Role`, value objects, domain events, repository interfaces | [README](OmniCore.Services.Auth.Domain/README.md) |
| `OmniCore.Services.Auth.Contracts` | Public integration events other services may consume (today one) | [README](OmniCore.Services.Auth.Contracts/README.md) |
| `OmniCore.Services.Auth.Application` | Use cases (register, login, refresh, logout, roles, queries), validators, event handlers, security-service interfaces | [README](OmniCore.Services.Auth.Application/README.md) |
| `OmniCore.Services.Auth.Infrastructure` | EF Core + migrations, repositories, BCrypt, JWT/refresh-token services, purge job, seeding, MassTransit outbox wiring | [README](OmniCore.Services.Auth.Infrastructure/README.md) |
| `OmniCore.Services.Auth.Api` | Carter endpoints, routes, `Program.cs` (the process entry point) | [README](OmniCore.Services.Auth.Api/README.md) |

Tests: `backend/c#/tests/OmniCore.Services.Auth.Domain.Tests` (17), `...Application.Tests` (14 handler tests), `...IntegrationTests` (23, Testcontainers, need Docker). All green in CI on 2026-11-20; not yet a required check.

## How they depend on each other
```
Auth.Api --> Auth.Infrastructure --> Auth.Application --> Auth.Domain --> Shared.Domain
   |               |                       |                  
   |               |                       +--> Auth.Contracts --> Shared.Contracts
   |               +--> Shared.Infrastructure
   +--> Shared.Api
```
Arrows point inward. `Auth.Api` is the only project nothing else references. Auth never references the User service, and other services may reference only `Auth.Contracts`.

## What the service offers (HTTP, base `/api/v1/auth`)
| Area | Endpoints | Access |
|---|---|---|
| Credentials | `POST register`, `POST login`, `POST refresh-token`, `POST logout` | anonymous (the refresh token is the credential); gateway rate-limited |
| Caller | `GET me` | signed in |
| Accounts | `GET {id}` | `Admin`, `SuperAdmin` |
| Roles | `GET/POST roles`, `GET/PUT roles/{id}`, `POST roles/assign`, `POST roles/remove` | `Admin`, `SuperAdmin` |
| Keys | `GET /.well-known/jwks.json`, `GET /health` | anonymous |

## What happens in the two important flows
**Register**
1. Validator (username 3-30, e-mail, strong password). 2. Handler checks uniqueness, hashes the password (BCrypt, cost 12), `Account.Create` raises `AccountCreatedDomainEvent`, adds the first refresh token. 3. The unit-of-work behaviour saves. During the save the interceptor dispatches the event: the default role `User` is attached and `AccountRegisteredIntegrationEvent` goes to the MassTransit outbox, all in one transaction. 4. Response: access token (JWT) + refresh token.

**Refresh (the security core, ADR 0003)**
1. The presented token is hashed (SHA-256) and looked up by hash; the account is loaded with its tokens and roles. 2. `Account.RotateRefreshToken`: an unknown or expired token fails; a **revoked** token being presented again means theft, so every session is revoked and saved explicitly even though the result is a failure. 3. Otherwise the old token is revoked, a new one issued and a new JWT signed with the current roles. 4. Two parallel refreshes of the same token cannot both win: Postgres `xmin` is the concurrency token, the loser gets 409/401. Details and the reasons for each choice: [docs/auth-tokens.md](../../../docs/auth-tokens.md).

## Tokens and keys at a glance
- Access token: JWT, RS256 (key from `Jwt__PrivateKeyPem`, public keys at the JWKS endpoint, validators fetch them); HS256 only while the legacy `Jwt__Secret` is set (cut-over steps in `infra/docker/.env.example`). Short lived; roles are baked in, so role changes apply at the next refresh.
- Refresh token: 64 random bytes, only its SHA-256 is stored; rotation on every use; at most 10 active sessions per account; rows expired for more than 30 days are purged every 6 hours by a Quartz job.

## Configuration at a glance
All from environment variables (compose), validated at startup. Shared sections: `Database`, `MessageBroker`, `Redis`, `MemoryCache`, `Storage`, `Jwt`, `Cors`, `RateLimiting`. Auth-specific: `GoogleAuth__ServerClientId` / `__ClientId` (required at startup, AI4), `RefreshTokenCleanup__RetentionDays|IntervalHours|Enabled`, and `Jwt__MaxActiveSessions`. Values for local runs: `infra/docker/.env.example`.

## Rules that hold across the service
- Layers point inward; Domain knows no framework; expected failures are `Result`/`Error`, not exceptions.
- State is changed only through the `Account` / `Role` aggregates; the handler orchestrates.
- Caller identity comes from the token, never from a request body.
- Every endpoint states its access rule explicitly; the default is currently open (AA1).
- A schema or token-format change needs an ADR first; an entity change and its migration go in one commit.
- No secrets in code, logs, events or the cache. No password hash in Redis (fixed), refresh tokens only hashed.

## Known problems, by project (full text in `docs/repo-check.md`)
| Project | Ids | Headlines |
|---|---|---|
| Api | AA1-AA5, A9, A15, A18 | no authorization fallback policy; `Location` header bug on role creation; most account features have no endpoint (password reset, MFA, Google login, sessions, deactivate); no first admin |
| Application | AN1-AN8 | register token claims `User` even if the role row is missing; dead cache code; no audit trail; account enumeration on login |
| Contracts | AC1-AC2 | one event only; invented first/last name fields |
| Infrastructure | AI1-AI8 | usernames are case-sensitive; unused `UserSession`; Google config required but unused; plain-token columns still present |
| Domain | AD1-AD8 | system roles renamable (breaks authorization); deactivate/delete raise no events; half-done soft delete |

## Suggested order for fixing these
1. **AD1** (role constants + `IsSystem`, refuse renaming system roles), because it can lock everyone out of admin features.
2. **AA1** (fallback authorization policy) and **A15** (first-admin bootstrap, ADR needed), then **AN1** (register/default role).
3. **AI1** (case-insensitive username; migration first checks duplicates, ADR needed).
4. **AA2**, **AI4**, **AN6**, **AN8** (small clean-ups and an audit trail).
5. The entity review (`docs/entities.md`): `UserSession` (AI2), soft delete (D1/AD3/AI3), event contracts (AC1/AD2), then new features (AA4).
