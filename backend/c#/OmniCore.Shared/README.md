# OmniCore.Shared (master overview)

The shared kernel of the backend: five small projects that every service (Auth today, User and others later) builds on. A service never re-implements these pieces; it plugs its own entities, commands and endpoints into them. Each project has its own `README.md` with the details; this page is the map.

## The five projects

| Project | One-line role | Details |
|---|---|---|
| `OmniCore.Shared.Domain` | The vocabulary of the domain layer: entity/aggregate/event base types, `Result`/`Error`, value objects, repository, unit-of-work and specification **interfaces**, paging types | [README](OmniCore.Shared.Domain/README.md) |
| `OmniCore.Shared.Contracts` | The base types for integration events (messages between services) and their handlers | [README](OmniCore.Shared.Contracts/README.md) |
| `OmniCore.Shared.Application` | CQRS interfaces, the MediatR pipeline (logging, validation, save), and interfaces for cross-cutting services (current user, cache, event bus, storage, time) | [README](OmniCore.Shared.Application/README.md) |
| `OmniCore.Shared.Infrastructure` | The implementations: EF repositories and interceptors, MassTransit bus + outbox, cache, storage, configuration classes, Quartz, JWT key handling | [README](OmniCore.Shared.Infrastructure/README.md) |
| `OmniCore.Shared.Api` | The web layer: `AddApi`/`UseApi`, JWT bearer, CORS, rate limiting, Swagger, `Result` to HTTP mapping, route base class | [README](OmniCore.Shared.Api/README.md) |

## How they depend on each other
```
                    Shared.Api
                    /        \
     Shared.Infrastructure     \
          /          \          \
   Shared.Application  \         \
      /        \        \         \
Shared.Domain  Shared.Contracts   (Domain also reached through Application)
```
Arrows point inward: Api -> Infrastructure -> Application -> Domain / Contracts. `Shared.Domain` references only MediatR; `Shared.Contracts` references nothing. A service mirrors this: its `Api`, `Infrastructure`, `Application`, `Domain` and `Contracts` projects reference the matching Shared project.

## What happens to one request (Auth login as the example)
1. **Gateway** (nginx): per-IP rate limit, routes `/api/v1/auth/...` to the service.
2. **`UseApi` pipeline** (Shared.Api): exception handler, CORS, rate limiter, JWT authentication, authorization.
3. **Carter endpoint** (service Api) deserialises the body and sends a MediatR command with `ISender`.
4. **Pipeline** (Shared.Application), outermost first: logging, FluentValidation (a failure returns a `ValidationResult` and stops here), unit of work (saves only when the result is a success), performance warning.
5. **Handler** (service Application) uses the aggregate (service Domain) and repositories (interfaces from Shared.Domain, implemented in the service's Infrastructure on top of `RepositoryBase`). It returns a `Result`.
6. **Save** (Shared.Infrastructure interceptors): audit columns are filled, domain events are dispatched **before** the commit so their handlers take part in the same save, and an integration event queued through the MassTransit outbox is stored in the same transaction.
7. **Response**: `ToOk()` / `ToCreated()` turn a success into an `ApiResult`, a failure into RFC 9457 problem details with the status taken from the `ErrorType`.

## How a service hosts the kernel (startup order)
1. `AddXApplication()` calls `AddSharedApplication(assemblies)`: MediatR, behaviours, validators, AutoMapper.
2. `AddXInfrastructure(config)` calls `AddSharedInfrastructure(config, assemblies)` (configs validated at startup, interceptors, caching, Quartz, SignalR), then adds the service's DbContext, `AddMassTransitWithBroker<TDbContext>`, repositories/services by Scrutor scan, seeders.
3. `AddApi(moduleAssemblies, config, title)` then `UseApi(...)`, `MapCarter()`, `UseXInfrastructureAsync()` (migrations + seeding).

## Configuration at a glance
Everything comes from environment variables (`Section__Key`), validated at startup; there are no `appsettings*.json` files in the repo. Sections: `Database`, `MessageBroker`, `Redis`, `MemoryCache`, `Storage`, `Jwt`, `Cors`, `RateLimiting`, `Qdrant` (+ service-specific ones). Full table in the Infrastructure README; the values for local runs are in `infra/docker/.env.example`.

## Rules that hold across the kernel
- Domain and Application know no framework detail except MediatR/FluentValidation; EF, ASP.NET, MassTransit, Redis and MinIO live only in Infrastructure and Api.
- Expected failures are `Result`/`Error` values, not exceptions.
- The caller's identity comes from the token (`ICurrentUserService`), never from a request body.
- Shared holds generic code only; anything about accounts, profiles or any one service's rules belongs in that service.

## Known problems, by project (full text in `docs/repo-check.md`)
| Project | Ids | Headlines |
|---|---|---|
| Domain | D1-D6 | `ISoftDeletable` mixes inactive and deleted; namespaces do not match folders; unused types |
| Contracts | C1 | no event versioning; Kafka key falls back to a random GUID |
| Application | AP1-AP5, S6, S12 | `CachingBehavior` cannot work as written and is not registered; invalidation keys differ from query keys |
| Infrastructure | I1-I8, S2, S10, S13 | dynamic filter/sort accepts client field names (latent); queue/group names not service-scoped; correlation-id middleware not wired |
| Api | W1-W7, S2, S8, S9, S11 | rate limiter runs before authentication; named limit policies never applied; two response shapes |

## Suggested order for fixing these (cheap and safe first)
1. W1 + S2 (rate-limiter order and real client IP), because it affects every endpoint.
2. I1 (allow-list for dynamic filter/sort) before any endpoint exposes paging.
3. I3 (per-service queue names) before the User service consumes events.
4. AP1/AP2/S6 together, only when a real query needs caching.
5. D1 with the entity review (`docs/entities.md`), then the clean-ups (D4, D5, D6, I4, I5, I7, W7).
