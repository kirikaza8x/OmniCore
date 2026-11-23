# OmniCore.Services.User (master overview)

The profile service: display name, bio, avatar, phone, preferences and addresses. Auth owns identity and credentials;
User owns "what we know about you". A profile has the **same id as the Auth account** — that id is the only link between
the two databases, with no cross-service foreign key. Profiles are created only from Auth's registration event.
Design decisions are in [ADR 0004](../../../docs/decisions/0004-user-service-design.md); tenancy waits for ADR 0001.

## The five projects

| Project | One-line role | Details |
|---|---|---|
| `OmniCore.Services.User.Domain` | `UserProfile` aggregate with settings and addresses, value objects, domain events, repository interface | [README](OmniCore.Services.User.Domain/README.md) |
| `OmniCore.Services.User.Contracts` | placeholder for future public integration events (empty today) | [README](OmniCore.Services.User.Contracts/README.md) |
| `OmniCore.Services.User.Application` | 11 use cases, validators, DTOs, mapping, Auth-event consumer | [README](OmniCore.Services.User.Application/README.md) |
| `OmniCore.Services.User.Infrastructure` | EF Core context, repository, `InitialUser` migration, MassTransit wiring | [README](OmniCore.Services.User.Infrastructure/README.md) |
| `OmniCore.Services.User.Api` | 10 Carter endpoints, host, health check, Dockerfile | [README](OmniCore.Services.User.Api/README.md) |

Tests: `backend/c#/tests/OmniCore.Services.User.Domain.Tests` (25), `...Application.Tests` (22) and
`...IntegrationTests` (6, Testcontainers with PostgreSQL and RabbitMQ, need Docker). All green in CI on 2026-11-22.
Not yet run as a full compose stack (UP1).

## How they depend on each other

```
User.Api --> User.Infrastructure --> User.Application --> User.Domain --> Shared.Domain
   |              |                       |
   |              |                       +--> User.Contracts, Auth.Contracts (event only)
   |              +--> Shared.Infrastructure
   +--> Shared.Api
```

Arrows point inward. User may reference only `Auth.Contracts` from Auth, never its other projects.

## How a new account gets a profile

1. Auth registers an account and writes `AccountRegisteredIntegrationEvent` to its outbox in the same transaction.
2. `AccountRegisteredIntegrationEventHandler` (User.Application) consumes it and sends
   `ProvisionUserProfileCommand`; a name equal to the username is dropped as a placeholder (AC2).
3. The idempotent handler creates `UserProfile` with default settings. Duplicate delivery is a success.
4. Failures throw, so MassTransit retries and then dead-letters. There is no public create endpoint.

## What the service offers (HTTP, base `/api/v1/users`)

| Area | Endpoints | Access |
|---|---|---|
| Caller | `GET /me`, `PUT /me`, `PUT /me/settings` | signed in |
| Avatar | `POST /me/avatar` (multipart, JPEG/PNG/WebP by signature, max 5 MB) | signed in |
| Addresses | `GET/POST /me/addresses`, `PUT/DELETE /me/addresses/{id}` | signed in |
| Other users | `GET /{id}` (public fields only) | signed in |
| Admin | `GET /{id}/details` | `Admin`, `SuperAdmin` |
| Health | `GET /health` | anonymous |

The caller's id always comes from the token, never from a request body.

## Storage at a glance

Schema `users` in its own database (`USER_DB_NAME`): `user_profiles`, `user_settings`, `user_addresses`, plus the
MassTransit outbox/inbox tables. The first migration, `InitialUser`, is generated with `dotnet ef`, not hand-written.
Username and e-mail are display copies, not unique; Auth stays the source of truth. `UserTenantProfile` exists in the
Domain only and is not mapped (ADR 0001 pending).

## Rules that hold across the service

- Layers point inward; Domain knows no framework; expected failures are `Result`/`Error`, not exceptions.
- Children change only through `UserProfile` methods: theme `Light|Dark|System`, language format, max 10 addresses,
  one primary, soft delete with a domain event.
- Caller identity comes from the token. Never trust an id in a request body.
- Auth owns username and e-mail; do not sync them here without a new Auth event.
- Tenancy is not built before ADR 0001. An entity change and its migration go in one commit.

## Known problems, by project (full text in `docs/repo-check.md`)

| Project | Ids | Headlines |
|---|---|---|
| Domain | UD1-UD8, U7, U9 | copied Auth data with no sync; duplicate `Username` type; game-specific `GamerTag`; tenant aggregate unused |
| Contracts | UC1 | still empty; the "Auth event not consumed" part of the finding is stale |
| Application | UA5, U2-U5 | namespace/folder mismatch; no tenant queries; fixed: wiring, no-tracking `Update`, ids from token |
| Infrastructure | UI1-UI2, U1 | fixed: EF Tools version aligned, design-time factory added, migrations applied |
| Api | UP1, AA1 | compose stack unverified; no fallback authorization policy; all endpoints opt in |

Other open items: ADR 0001 (tenancy), ADR 0004 follow-ups (profile deletion/deactivation and Auth lifecycle events),
I6 (all handlers of one event retry together), S11 (settings come from compose environment variables; no committed
`appsettings*.json`).
