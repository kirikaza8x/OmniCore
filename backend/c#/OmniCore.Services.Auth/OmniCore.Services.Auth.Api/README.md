# OmniCore.Services.Auth.Api

## Purpose
The HTTP face of the Auth service and the program that hosts it. It contains the Carter endpoints (one class per endpoint), the route name classes, and `Program.cs` that wires the Application, Infrastructure and Shared layers together. It holds no business logic: each endpoint reads the request, sends one MediatR command or query, and converts the `Result` into a response with the helpers from `Shared.Api`.

## What is inside

### Endpoints
All routes are under `/api/v1/auth` (built by `AuthRoutes` / `RoleRoutes` from `Shared.Api`'s `BaseRouteModule`). "Anonymous" means the endpoint has no authorization requirement.

| Method and route | Endpoint class | Access | Sends | Success |
|---|---|---|---|---|
| `POST /register` | `RegisterEndpoint` | anonymous | `RegisterCommand` (body) | 200 `ApiResult<AuthResponse>`; 400 validation; 409 username/email taken |
| `POST /login` | `LoginEndpoint` | anonymous | `LoginCommand` | 200 `AuthResponse` (access + refresh token); 401/404/400 |
| `POST /refresh-token` | `RefreshTokenEndpoint` | anonymous (the refresh token is the credential) | `RefreshTokenCommand` | 200 `AuthResponse`; 401 on invalid, expired or reused token |
| `POST /logout` | `LogoutEndpoint` | anonymous (the refresh token is the credential) | `LogoutCommand` | 200 `ApiResult` |
| `GET /me` | `GetCurrentUserEndpoint` | any signed-in user | `GetCurrentUserQuery` (no id: taken from the token) | 200 `AccountResponse` |
| `GET /{id:guid}` | `GetAccountByIdEndpoint` | roles `Admin`, `SuperAdmin` | `GetAccountByIdQuery` | 200 `AccountResponse`; 404 |
| `GET /roles` | `GetRolesEndpoint` | `Admin`, `SuperAdmin` | `GetRolesQuery` | 200 list of `RoleResponse` |
| `GET /roles/{id:guid}` | `GetRoleByIdEndpoint` | `Admin`, `SuperAdmin` | `GetRoleByIdQuery` | 200 / 404 |
| `POST /roles` | `CreateRoleEndpoint` | `Admin`, `SuperAdmin` | `CreateRoleCommand` | 201 / 409 |
| `PUT /roles/{id:guid}` | `UpdateRoleEndpoint` | `Admin`, `SuperAdmin` | `UpdateRoleCommand` | 200 / 404 / 409 |
| `POST /roles/assign` | `AssignRoleToAccountEndpoint` | `Admin`, `SuperAdmin` | `AssignRoleToAccountCommand` | 200 / 404 |
| `POST /roles/remove` | `RemoveRoleFromAccountEndpoint` | `Admin`, `SuperAdmin` | `RemoveRoleFromAccountCommand` | 200 / 404 |
| `GET /.well-known/jwks.json` (no prefix) | `JwksEndpoint` | anonymous, hidden from Swagger | reads `JwtKeySet` | public signing keys, cacheable 5 min (see [docs/auth-tokens.md](../../../../docs/auth-tokens.md)) |
| `GET /health` | mapped in `Program.cs` | anonymous | | 200 when the process is up (no database or broker probe, A18) |

`DeleteRoleEndpoint.cs` exists but is fully commented out, so roles cannot be deleted.

### Other files
| File | What it does |
|---|---|
| `Program.cs` | `AddAuthApplication()`, `AddAuthInfrastructure(config)`, `AddAuthApi(config)`, health checks; then `UseApi(...)`, `MapHealthChecks("/health")`, `MapCarter()`, `UseAuthInfrastructureAsync()` (runs migrations and seeds roles) and `Run()`. `public partial class Program` lets the integration tests host the app. |
| `DependencyInjection.AddAuthApi` | Calls `Shared.Api`'s `AddApi(...)` with this assembly as the Carter module source and the title "OmniCore Auth API", plus `AddHttpContextAccessor`. |
| `Routing/AuthRoutes`, `Routing/RoleRoutes` | Route strings and tags (`/api/v1/auth`, `/api/v1/auth/roles`, sub-paths `register`, `login`, `refresh-token`, `logout`, `me`, `assign`, `remove`). |
| `GlobalUsings.cs`, `AssemblyReference.cs` | Global usings (Carter, MediatR, `ApiResult` helpers); assembly handle. |
| `Dockerfile` | Container image build for the service (listens on 8080 in compose; published on `AUTH_SERVICE_PORT`, default 5001). |

## Depends on / used by
- Projects: `Auth.Infrastructure`, `Auth.Application` (and through them `Auth.Domain`, `Auth.Contracts`), `Shared.Api`. Package: EF Core Design (design-time).
- Used by: nothing references it except the integration tests (`WebApplicationFactory<Program>`). It is the process entry point.

## Rules for this layer
- An endpoint is: bind request, send one command/query, return `result.ToOk(...)` / `ToCreated(...)`. No repositories, no domain types, no business checks.
- State the access rule on every endpoint. Admin-only actions use `RequireRoles("Admin", "SuperAdmin")`; anything that identifies the caller reads the id from the token (`/me`), never from the body.
- Request DTOs for admin actions live in `Auth.Application/Features/*/DTOs` (for example `CreateRoleRequest`); the public credential endpoints bind the command itself.
- Public endpoints must stay few and deliberate: register, login, refresh-token, logout and the JWKS. They are protected by the gateway's `limit_req` (ADR 0002), not yet by per-account lockout (A8).

## How to ...
**Add an endpoint**
1. Add the route string to `AuthRoutes` / `RoleRoutes` if it is new.
2. Create `Endpoints/<Area>/<Name>Endpoint.cs` implementing `ICarterModule` (copy `GetRoleByIdEndpoint`): `app.MapGroup(Routes.GroupPrefix).WithTags(Routes.Tag).MapX(path, handler).WithName(...).RequireRoles(...).Produces<...>(...)`.
3. The command/query and its handler and validator go in `Auth.Application`. Carter finds the module by itself (no registration).
4. Add an integration test in `tests/OmniCore.Services.Auth.IntegrationTests` that checks the expected 401/403 as well as the happy path.

**Make an endpoint admin-only:** `.RequireRoles("Admin", "SuperAdmin")` (role names are case-sensitive and are the ones seeded by `RoleSeeder`).

**Run the API on its own:** `cd infra/docker && cp .env.example .env && docker compose up -d --build auth-api postgres rabbitmq redis minio` (full stack still unverified, see `CLAUDE.md`).

## Known gaps
Ids from `docs/repo-check.md`; new ones (`AA1`..`AA5`) are added there in the same commit.
- **AA1 (new, medium)** The default is open: there is no authorization fallback policy, so an endpoint that forgets `RequireAuthorization()` is public. Today every non-public endpoint has it, but nothing enforces it. Set a `FallbackPolicy` that requires an authenticated user and mark the public endpoints with `AllowAnonymous()` explicitly.
- **AA2 (new)** `CreateRoleEndpoint` calls `result.ToCreated("Role created successfully")`, which binds the text to the `uri` parameter of the overload, so the 201 response's `Location` header is the message, not a URL. Use the route-name overload (`ToCreated("GetRoleById", r => new { id = r.Id }, "...")`) and name the route.
- **AA3 (new)** `LogoutEndpoint` is anonymous: whoever holds a refresh token can end that session (acceptable), but nothing limits calls to it and it is not tied to the caller's access token. Compare with the logout handler in `Auth.Application/README.md`.
- **AA4 (new)** No endpoints for password change or reset, e-mail confirmation, MFA, external (Google) login, session list/revoke, account deactivation, or role deletion, although the domain has entities for most of them (`SecurityToken`, `MfaMethod`, `ExternalLogin`, `UserSession`). See `docs/entities.md` and the roadmap.
- **AA5 (new)** Inconsistent request binding: public endpoints bind the MediatR command from the body, admin endpoints bind a separate request DTO and build the command. Choose one style.
- **A15** There is no first admin account: nothing creates an `Admin`/`SuperAdmin` user, so every admin endpoint is unreachable until a role is assigned by hand in the database.
- **A9** The login errors tell "account not found", "inactive" and "wrong password" apart, which allows account enumeration (owner decision pending).
- **A18** `/health` does no database or broker check, and migrations plus seeding run in every replica at startup.
- Published port 5001 bypasses the gateway's rate limits (known, `CLAUDE.md`).
