# OmniCore.Services.User (master overview)

> **Status: design only, not running.** The User service has a domain model and a first set of use cases, but no endpoints, no database context, no repository, no migrations, no wiring and no tests (U1, U2). Nothing here has been built into a working service. This page describes the **intended** service and what is missing.

## What the service is for
Auth answers "who are you and may you in". User answers "what do we know about you": display name, bio, avatar, phone, preferences, addresses, and (after the multi-tenancy decision) a profile per tenant. A user profile has the **same id as the Auth account**; that id is the only link between the two services, and no database foreign key crosses the boundary.

## The five projects

| Project | One-line role | State | Details |
|---|---|---|---|
| `OmniCore.Services.User.Domain` | `UserProfile` aggregate with settings, addresses, tenant profiles; value objects; events; repository interface | model exists, incomplete, no tests | [README](OmniCore.Services.User.Domain/README.md) |
| `OmniCore.Services.User.Contracts` | public integration events | empty | [README](OmniCore.Services.User.Contracts/README.md) |
| `OmniCore.Services.User.Application` | 7 use cases, validators, cache-invalidation handler | drafted, unwired, unproven | [README](OmniCore.Services.User.Application/README.md) |
| `OmniCore.Services.User.Infrastructure` | EF context, repository, migrations, broker wiring | **empty shell** | [README](OmniCore.Services.User.Infrastructure/README.md) |
| `OmniCore.Services.User.Api` | endpoints and host | **skeleton, crashes at start** | [README](OmniCore.Services.User.Api/README.md) |

## How they depend on each other (as designed, same shape as Auth)
```
User.Api --> User.Infrastructure --> User.Application --> User.Domain --> Shared.Domain
   |               |                       |
   |               |                       +--> User.Contracts
   |               +--> Shared.Infrastructure
   +--> Shared.Api
```
User may reference only `Auth.Contracts` from the Auth service (to consume `AccountRegisteredIntegrationEvent`), never its other projects.

## Intended flow: a new account gets a profile
1. Auth registers an account and, in the same transaction, stores `AccountRegisteredIntegrationEvent` in its outbox; the broker delivers it.
2. A User consumer (not written, U6) sends `CreateUserProfileCommand` with the event's `AccountId` as the profile id, plus username and e-mail.
3. `UserProfile.Create` makes the profile with default settings; the cache-invalidation handler runs; the profile is saved.
4. From then on the signed-in user reads and updates **their own** profile through `/api/v1/users/me/...`, the id always from the token.

## What is missing before this can run (in order)
1. ADR 0001 answered by the owner (tenancy), because it decides whether `UserTenantProfile` stays.
2. Infrastructure: `UserDbContext`, mappings, repository, first migration, `AddUserInfrastructure` (checklist in its README).
3. Domain fixes UD1/UD2 and use-case fixes UA1 (tracking) and U3 (ids from the token), with new `User.Domain.Tests`.
4. Api: `Program` wiring, routes and endpoints (checklist in its README).
5. The Auth event consumer (U6) with service-scoped queue names (I3).
6. An integration test project, then add the service to CI and make the `user-api` compose service healthy.

## Rules that hold across the service
Layers point inward; Domain knows no framework; failures are `Result`/`Error`; the caller's id comes from the token; Auth is the source of truth for e-mail and username; tenancy is not built before ADR 0001; an entity change and its migration go in one commit.

## Known problems, by project (full text in `docs/repo-check.md`)
| Project | Ids | Headlines |
|---|---|---|
| Domain | UD1-UD8, U7, U9 | incomplete aggregate; copies of Auth data with no sync; duplicate `Username` type; game-specific `GamerTag` |
| Contracts | UC1 | empty; Auth event not consumed (U6) |
| Application | UA1-UA6, U2-U5, U8 | no-tracking `Update` bug (UA1); tenant profile never saved (U2); ids from the body (U3); unwired |
| Infrastructure | UI1-UI2, U1 | empty; EF Tools 10 vs EF 9 version clash |
| Api | UP1-UP2, U1 | skeleton crashes at start; compose service and gateway route point at it |

## Decision needed from the owner
Keep the User service in compose and CI now, or take it out until Infrastructure and Api exist (a failing container in `docker compose up`, UP1). Related: what the profile should store (UD2), and the tenancy question (ADR 0001).
