# OmniCore.Shared.Api

## Purpose
The web layer every service host reuses: one call to wire Carter endpoints, JWT authentication, CORS, rate limiting, Swagger and error handling (`AddApi` / `UseApi`), plus the helpers that turn a domain `Result` into an HTTP response, the base class for route names and the authorization helper for admin-only endpoints. A service's own `Api` project then only contains its endpoints and routes.

## What is inside

### `DependencyInjection.cs` (`ApiExtensions`)
| Method | What it does |
|---|---|
| `AddApi(moduleAssemblies, configuration, apiTitle)` | In order: Carter modules from the given assemblies, `JwtConfig` validation, JWT authentication + authorization, SignalR, Swagger services, ProblemDetails + `GlobalExceptionHandler`, rate limiting, CORS. |
| `UseApi(apiTitle, enableSwagger?)` | Pipeline order: `UseExceptionHandler` -> `UseCors` -> `UseRateLimiter` -> `UseAuthentication` -> `UseAuthorization` -> Swagger (only when enabled) -> `MapHub<LogHub>("api/logHub").RequireAuthorization()`. The service calls `app.MapCarter()` itself afterwards. Swagger is on when the argument is true, or `EnableSwagger` config is true, or (default) in Development. |
| Rate limiting (private) | Reads `RateLimiting` (`GlobalPolicy`, `Policies`), throws at startup when the section, the global policy name or the policy list is missing. Registers every policy as a fixed-window limiter and sets the **global limiter** to `GlobalPolicy`. The partition key is the user id claim, else the remote IP. |
| CORS (private) | `Cors:AllowAnyOrigin=true` -> any origin with credentials; else `Cors:AllowedOrigins` with credentials; else any origin without credentials (the fallback when nothing is configured). |

### `Extensions/`
| File | What it does |
|---|---|
| `JwtAuthenticationExtensions.AddJwtAuthentication` | JwtBearer with issuer, audience, signing key and lifetime validation (`ClockSkew = 0`), keys and allowed algorithms from `JwtKeySet` (RS256 via JWKS, HS256 only while `Jwt__Secret` is set: see [docs/auth-tokens.md](../../../../docs/auth-tokens.md)). 401 and 403 are written as `ProblemDetails` through the `Result` pattern (`Authentication.Failed`, `Authorization.Failed`). Registers `JwtKeySet` as a singleton. |
| `AuthorizationExtensions.RequireRoles(params string[])` | `builder.RequireRoles("Admin")` = `RequireAuthorization` with a role policy. |
| `CarterExtensions.AddCarterWithAssemblies` | Finds every non-abstract `ICarterModule` in the given assemblies and registers them. |
| `SwaggerExtensions` | Swashbuckle document `v1` with a Bearer security scheme; in `UseSwaggerDocumentation` also `UseForwardedHeaders(XForwardedFor | XForwardedProto)` (only when Swagger is on), Swagger UI at `/swagger`. |

### `Results/` (Result to HTTP)
| Type | What it does |
|---|---|
| `ResultExtensions` | `ToOk`, `ToCreated` (by uri or by route name), `ToNoContent`, `ToAccepted`, `ToProblem` on `Result` / `Result<T>`. Success: 200/201/204/202 with an `ApiResult` body; failure: `CustomResults.Problem`. |
| `ApiResult`, `ApiResult<T>` | Success envelope: `IsSuccess`, `Message` (default "Success"), `Timestamp`, `Data`. |
| `CustomResults.Problem` | RFC 9457 problem details. Status by `ErrorType`: Validation 400, NotFound 404, Conflict 409, Unauthorized 401, Forbidden 403, anything else 500 with a generic detail (the real description is not leaked). Title is the error **code**; a validation failure adds an `errors` list; `traceId` is always added. |

### Other
| Type | What it does |
|---|---|
| `Exceptions/GlobalExceptionHandler` | Last-resort `IExceptionHandler`: unique-index violation (Postgres `23505`, found anywhere in the inner exceptions) -> 409 "already exists"; `DbUpdateConcurrencyException` -> 409 "changed by another request, retry"; everything else -> logged and 500 with a generic body plus `traceId`. |
| `Routing/BaseRouteModule` | Builds `GroupPrefix` as `api/v1/<ServiceName>/<ResourceName>` (empty segments skipped), conventional sub-paths (`GetById` = `{id:guid}`, `Update`, `Delete`, ...) and a Swagger `Tag`. Each service has `...Routes` classes deriving from it. |
| `File/FormFileUpload` | Adapts ASP.NET `IFormFile` to the Application's `IFileUpload`. |

## Depends on / used by
- Packages: `Carter` 9.0.0, `Swashbuckle.AspNetCore` 10.2.3, `Microsoft.EntityFrameworkCore.Design` (design-time only, `PrivateAssets=all`), ASP.NET Core shared framework.
- Projects: `Shared.Application`, `Shared.Infrastructure` (and through it `Shared.Domain`).
- Used by: the Api project of each service (today `OmniCore.Services.Auth.Api` calls `AddApi`; the User service has no Api yet).

## Rules for this layer
- Endpoints are Carter modules in the service's Api project; they send a MediatR command or query and turn the `Result` into a response with `ToOk` / `ToCreated` / `ToNoContent`. No business logic and no repository access in an endpoint.
- Every endpoint is explicit about its access: `RequireAuthorization()` for signed-in users, `RequireRoles("Admin")` for admin actions, `AllowAnonymous()` only for register, login, refresh and public keys. Check each new endpoint (`CLAUDE.md`).
- Take the caller's id from the token via `ICurrentUserService`, never from the body (finding U3).
- Failures are returned as `Result` errors, not thrown; `GlobalExceptionHandler` is the safety net, not the normal path.

## How to ...
**Add an endpoint** (Auth example, `Endpoints/Roles/GetRoleByIdEndpoint.cs` is the pattern)
```csharp
public sealed class GetThingEndpoint : ICarterModule
{
    private static readonly ThingRoutes Routes = new();
    public void AddRoutes(IEndpointRouteBuilder app) =>
        app.MapGroup(Routes.GroupPrefix).WithTags(Routes.Tag)
           .MapGet(Routes.GetById, async (Guid id, ISender sender, CancellationToken ct) =>
               (await sender.Send(new GetThingQuery(id), ct)).ToOk())
           .RequireAuthorization()          // or .RequireRoles("Admin")
           .WithName("GetThing")
           .Produces<ApiResult<ThingResponse>>()
           .ProducesProblem(StatusCodes.Status404NotFound);
}
```
**Add routes for a new service:** `public sealed class ThingRoutes : BaseRouteModule { protected override string? ServiceName => "orders"; protected override string? ResourceName => "things"; }` gives `/api/v1/orders/things`.

**Wire a new service host:** in `Program.cs` call the service's `AddXApplication()`, `AddXInfrastructure(config)`, `AddApi(new[] { typeof(XApi.AssemblyReference).Assembly }, config, "X API")`, then `app.UseApi(...)`, `app.MapCarter()`, `app.Run()`. Set `RateLimiting__GlobalPolicy` and `RateLimiting__Policies__<name>__*`, `Jwt__JwksUrl` and the other required settings in compose.

**Return a new error kind:** create `Error.Conflict("Thing.Exists", "...")` in the domain; the status code follows its `ErrorType` automatically.

## Known gaps
Ids from `docs/repo-check.md`; new ones (`W1`..`W7`) are added there in the same commit.
- **W1 (new, medium)** `UseRateLimiter` runs **before** `UseAuthentication`, so `BuildPartitionKey` never sees a user id and always falls back to the remote IP. Together with S2 (the IP is the gateway's) every client shares one bucket per policy. Move `UseRateLimiter` after `UseAuthentication` and fix the client IP first.
- **W2 (new)** The named policies are registered but nothing applies them (`RequireRateLimiting` is not used anywhere); only the global policy limits requests. Credential endpoints are protected only by the gateway's `limit_req` (ADR 0002).
- **S2** `UseForwardedHeaders` exists only inside `UseSwaggerDocumentation`, so with Swagger off (production) the service sees the nginx address; it would also need `KnownNetworks` for the Docker network, otherwise forwarded headers from the gateway are ignored.
- **S8 / F3** The CORS fallback (nothing configured) allows any origin; `AllowAnyOrigin=true` also allows credentials. The production compose override requires `CORS_ALLOWED_ORIGIN`.
- **S9** The handler logs client cancellations as errors and maps all other exceptions to 500.
- **S11** `AddApi` throws when `RateLimiting` is missing, and `appsettings*.json` are not in the repo, so every service must set the `RateLimiting__*` variables.
- **W3 (new)** Two response shapes: success is `ApiResult` (`isSuccess`, `message`, `timestamp`, `data`), failure is RFC 9457 `ProblemDetails`. Clients must handle both; `isSuccess` is redundant with the status code.
- **W4 (new)** Error `Code` is used as the problem `Title`, and for validation failures the code is a *property name*, not a stable error code (see AP notes in `Shared.Application/README.md`).
- **W5 (new)** `GlobalExceptionHandler` references Npgsql's `PostgresException`, tying the shared web layer to PostgreSQL (the whole backend uses it, but the dependency arrives only transitively through `Shared.Infrastructure`).
- **W6 (new)** `RequireRoles` uses ASP.NET's case-sensitive role check, while `CurrentUserService.IsInRole` compares case-insensitively; keep role names exactly as seeded (`RoleSeeder`).
- **W7 (new)** Smaller points: `JwtBearer` has `RequireHttpsMetadata = false` and `SaveToken = true` (the raw token is stored in the auth properties and not used); `AddSignalR()` is called here and again in `AddSharedInfrastructure`; `File/FIleUpload.cs` has a typo in its file name; `RequireRoles` is available only on `RouteHandlerBuilder`, not on endpoint groups.
