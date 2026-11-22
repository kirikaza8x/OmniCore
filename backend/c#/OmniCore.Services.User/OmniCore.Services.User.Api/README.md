# OmniCore.Services.User.Api

> **Status: skeleton that cannot serve requests.** `Program.cs` builds an app with only health checks and then calls `UseApi(...)`, which needs services that nothing registered. There are no endpoints, no routes classes and no Carter wiring (U1). The compose service `user-api` exists but would fail at startup.

## Purpose
The HTTP face and the host process of the User service: Carter endpoints for profiles, settings, addresses and tenant profiles, and `Program.cs` that wires the layers (the role `Auth.Api` plays for Auth).

## What is inside
| File | What it does today |
|---|---|
| `Program.cs` | `AddHealthChecks()`, `Build()`, `app.UseApi("OmniCore User API")`, `MapHealthChecks("/health")`, `Run()`. Missing: `AddUserApplication`, `AddUserInfrastructure`, `AddApi`, `MapCarter`, `UseUserInfrastructureAsync`, `public partial class Program`. |
| `ApiAssemblyReference` | assembly handle (for `AddApi` module scanning) |
| `Dockerfile` | builds the User projects + Shared (copies Auth.Contracts project files too; the matching source `COPY` is commented out), non-root user, `curl` for health checks, port 8080 |
| `Properties/launchSettings.json` | local run profile |
| csproj | `Microsoft.NET.Sdk.Web`; references `User.Infrastructure`, `User.Application`, `Shared.Api` |

## Depends on / used by
- Projects: `User.Infrastructure`, `User.Application`, `Shared.Api`. Used by: nothing; it is the entry point.
- Gateway: the nginx config routes `/api/v1/users/` here (spec in `infra/docker`); published port `USER_SERVICE_PORT` (5002) bypasses the gateway limits.

## What has to be built here (the checklist)
1. `Program.cs` like `Auth.Api`: `AddUserApplication()`, `AddUserInfrastructure(config)`, `AddApi([ApiAssemblyReference.Assembly], config, "OmniCore User API")`, then `UseApi`, `MapCarter`, `UseUserInfrastructureAsync`, `Run`, and `public partial class Program`.
2. `Routing/UserRoutes : BaseRouteModule` (`ServiceName = "users"`) giving `/api/v1/users/...`.
3. Endpoints, every one with an explicit access rule and the caller's id **from the token**, not the body (U3):
   | Planned route | Request | Access |
   |---|---|---|
   | `GET /me` | `GetUserProfileByIdQuery` with the token's id | signed in |
   | `PUT /me` | `UpdateUserProfileCommand` | signed in |
   | `POST /me/avatar` (multipart, `FormFileUpload`) | `UploadAvatarCommand` | signed in |
   | `PUT /me/settings` | `UpdateUserSettingsCommand` | signed in |
   | `POST /me/addresses` | `AddUserAddressCommand` | signed in |
   | `GET /{id}` | profile by id (public fields only) | signed in; full data for owner/admin |
   | tenant profile | after ADR 0001 | to be decided |
4. Integration tests for 401/403 and the happy path per endpoint.

## Rules for this layer
Same as [Auth.Api](../../OmniCore.Services.Auth/OmniCore.Services.Auth.Api/README.md): one command/query per endpoint, `RequireAuthorization()` or `RequireRoles(...)` stated explicitly, `ToOk` / `ToCreated` from `Shared.Api`, no business logic.

## Known gaps
New ids (`UP1`, `UP2`) are added to `docs/repo-check.md` in the same commit.
- **U1** nothing is registered: `UseApi` calls `UseCors`, `UseRateLimiter`, `UseAuthentication`, `UseAuthorization` without the matching `AddApi`, so the container would crash at start (and `RateLimiting`/`Jwt` settings are required anyway, S11).
- **UP1 (new, medium)** The compose service `user-api` and the gateway route exist for a service that cannot run; either keep it out of the default compose profile until it works or accept a failing container in `docker compose up` (the full stack has never been started, see `CLAUDE.md`).
- **UP2 (new, low)** The Dockerfile restores `Auth.Contracts` project files but does not copy its sources (line commented out): the build will fail as soon as the User service references `Auth.Contracts` for U6. Uncomment the `COPY` together with that reference.
- **U3** the existing commands take ids from the body; change them before building endpoints.
