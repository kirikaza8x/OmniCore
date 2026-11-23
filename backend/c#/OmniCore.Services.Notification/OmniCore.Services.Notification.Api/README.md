# OmniCore.Services.Notification.Api

## Purpose

The HTTP host: Carter endpoints, admin authorization, Swagger, health check, and startup calls that apply migrations and
start the broker. Contains no business rules.

## What is inside

| Type | Role |
|---|---|
| `Program.cs` | `AddNotificationApplication()` → `AddNotificationInfrastructure()` → `AddNotificationApi()`; `UseApi()`, `/health`, `MapCarter()`, `UseNotificationInfrastructureAsync()`; `public partial class Program` for `WebApplicationFactory` |
| `DependencyInjection.cs` | `AddNotificationApi()` → shared `AddApi()`; registers `IHttpContextAccessor` |
| `Routing/NotificationRoutes.cs` | base route `api/v1/notifications`, Swagger tag `Notifications` |
| `Endpoints/NotificationEndpoints.cs` | the three admin endpoints |
| `GlobalUsings.cs`, `AssemblyReference.cs` | usings and assembly marker |
| `Dockerfile` | .NET 9 SDK build, non-root `appuser`, port 8080 |
| `Properties/launchSettings.json` | local HTTP `http://localhost:5003` |

## Endpoints

| Method and route | Auth | Request | Response |
|---|---|---|---|
| `GET /api/v1/notifications` | `Admin`, `SuperAdmin` | `page`, `pageSize`, `status`, `channel`, `recipient` | paged delivery log |
| `GET /api/v1/notifications/{id:guid}` | `Admin`, `SuperAdmin` | id | one delivery, 404 when absent |
| `POST /api/v1/notifications/test` | `Admin`, `SuperAdmin` | `{channel, recipient}` | sends the `test` template; no dedup key, so every call sends |
| `GET /health` | anonymous | — | liveness |

`RequireRoles` builds a policy for `Admin`/`SuperAdmin`. `Result.ToOk()` returns `ApiResult<T>` on success and
`ProblemDetails` on failure. Swagger documents 400/401/403/404.

## Depends on / used by

- Projects: `Notification.Application`, `Notification.Infrastructure`, `Shared.Api`; EF Core Design for `dotnet ef`.
- Used by: `tests/OmniCore.Services.Notification.IntegrationTests` (`WebApplicationFactory<Program>`), compose and nginx.

## Rules for this layer

- Endpoints validate transport concerns and delegate to MediatR; no domain logic here.
- State the access rule on every endpoint: there is no fallback authorization policy (AA1).
- Use exact role names as defined in the shared identity model (W6: role checks are case-sensitive).
- Secrets come from environment variables; `appsettings*.json` are git-ignored (S11).

## How to ...

**Add an endpoint:** add a method in `NotificationEndpoints` (or a new `ICarterModule`), use the shared route base,
state the auth rule, send a MediatR request and document the response codes.

**Debug a 500:** run the integration tests first — they start PostgreSQL and RabbitMQ in Testcontainers. Check that
the initial migration was applied before looking at handler code (NT1).

## Known gaps

- **AA1** no global fallback authorization policy; every endpoint opts in.
- **A18** `/health` is liveness only; a broken database or broker does not affect it.
- **W1** the shared rate limiter runs before authentication, so per-user partitioning does not apply.
- **W6** `RequireRoles` compares role names case-sensitively.
