# Layer index

Every project has its own README (purpose, contents, dependencies, rules, how-tos, known gaps). Start with the master page of a service, then the layer you touch.

| Area | Master | Domain | Contracts | Application | Infrastructure | Api |
|---|---|---|---|---|---|---|
| Shared | [README](../backend/c%23/OmniCore.Shared/README.md) | [d](../backend/c%23/OmniCore.Shared/OmniCore.Shared.Domain/README.md) | [c](../backend/c%23/OmniCore.Shared/OmniCore.Shared.Contracts/README.md) | [a](../backend/c%23/OmniCore.Shared/OmniCore.Shared.Application/README.md) | [i](../backend/c%23/OmniCore.Shared/OmniCore.Shared.Infrastructure/README.md) | [api](../backend/c%23/OmniCore.Shared/OmniCore.Shared.Api/README.md) |
| Auth | [README](../backend/c%23/OmniCore.Services.Auth/README.md) | [d](../backend/c%23/OmniCore.Services.Auth/OmniCore.Services.Auth.Domain/README.md) | [c](../backend/c%23/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/README.md) | [a](../backend/c%23/OmniCore.Services.Auth/OmniCore.Services.Auth.Application/README.md) | [i](../backend/c%23/OmniCore.Services.Auth/OmniCore.Services.Auth.Infrastructure/README.md) | [api](../backend/c%23/OmniCore.Services.Auth/OmniCore.Services.Auth.Api/README.md) |
| User | [README](../backend/c%23/OmniCore.Services.User/README.md) | [d](../backend/c%23/OmniCore.Services.User/OmniCore.Services.User.Domain/README.md) | [c](../backend/c%23/OmniCore.Services.User/OmniCore.Services.User.Contracts/README.md) | [a](../backend/c%23/OmniCore.Services.User/OmniCore.Services.User.Application/README.md) | [i](../backend/c%23/OmniCore.Services.User/OmniCore.Services.User.Infrastructure/README.md) | [api](../backend/c%23/OmniCore.Services.User/OmniCore.Services.User.Api/README.md) |

Web client: [frontend/web](../frontend/web/README.md) (structure and layer rules) · [ADR 0005](decisions/0005-web-client.md)

Related: [architecture](architecture.md) · [entity review](entities.md) · [findings](repo-check.md) · [roadmap](roadmap.md) · [ADRs](decisions/) · [service ideas](services-plan.md) · [cloud simulation](../infra/docker/CLOUD-SIM.md)

## Dependency rule in one picture
```
Api -> Infrastructure -> Application -> Domain -> Shared.Domain
 |          |                 |
 |          |                 +-> own Contracts (+ other services' Contracts only)
 |          +-> Shared.Infrastructure
 +-> Shared.Api
```
