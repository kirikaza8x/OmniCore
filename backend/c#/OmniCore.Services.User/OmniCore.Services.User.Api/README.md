# OmniCore.Services.User.Api

## Purpose
HTTP face and host of the User service. Endpoints only bind, send one MediatR request and convert the `Result`. Tested by `tests/OmniCore.Services.User.IntegrationTests` (Postgres + RabbitMQ in Docker).

## What is inside
All routes are under `/api/v1/users` and require a signed-in user unless noted.

| Method and route | Access | Sends |
|---|---|---|
| `GET /me`, `PUT /me` | signed in | `GetMyProfile`, `UpdateMyProfile` |
| `PUT /me/settings` | signed in | `UpdateMySettings` (invalid theme/language: 400) |
| `POST /me/avatar` | signed in, multipart | `UploadMyAvatar` (anti-forgery disabled for the API) |
| `GET /me/addresses`, `POST /me/addresses` (201) | signed in | `GetMyAddresses`, `AddMyAddress` |
| `PUT /me/addresses/{addressId}`, `DELETE ...` (204) | signed in | `UpdateMyAddress`, `RemoveMyAddress` |
| `GET /{id}` | signed in | `GetPublicProfile` (no e-mail, phone, addresses, settings) |
| `GET /{id}/details` | `Admin`, `SuperAdmin` | `GetProfileDetails` |

There is no create endpoint on purpose: profiles come from the Auth registration event. `Program.cs` wires `AddUserApplication().AddUserInfrastructure().AddUserApi()`, `/health`, Carter and migrations; `Dockerfile` copies the `Auth.Contracts` sources it needs (UP2).

## Depends on / used by
`User.Infrastructure`, `User.Application`, `Shared.Api`, EF Core Design (design time). Used by the integration tests only.

## Rules for this layer
Access rule on every endpoint; the caller's id from the token; route strings in `Routing/UserRoutes`.

## How to ...
**Add an endpoint:** route in `UserRoutes`, class in `Endpoints/`, use case in Application, integration test with 401/403 and the happy path. **Run:** `cd infra/docker && docker compose up -d --build user-api postgres rabbitmq redis minio` (not yet verified as a stack).

## Known gaps
- **AA1-style** default is open (no fallback authorization policy); every endpoint here has `RequireAuthorization()`.
- **UP1** `user-api` not yet started in the real compose stack; its published port bypasses the gateway.
- No profile deletion endpoint.
