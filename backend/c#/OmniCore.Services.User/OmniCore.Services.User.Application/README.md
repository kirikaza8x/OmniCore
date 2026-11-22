# OmniCore.Services.User.Application

## Purpose
The use cases of the User service as MediatR commands and queries (pipeline from Shared: logging, validation, unit of work, performance). Every "me" use case takes the caller from the token through `RequireUserId()`; a request body never carries the caller's id (U3). Covered by 22 handler tests.

## What is inside
| Use case | Type | Notes |
|---|---|---|
| `ProvisionUserProfile` | command, **internal only** | Idempotent (second delivery = success, nothing added). Called by the event handler. |
| `AccountRegisteredIntegrationEventHandler` | broker consumer | From `Auth.Contracts`. Drops a first name equal to the username (AC2). Throws on failure so MassTransit retries and dead-letters. |
| `GetMyProfile`, `UpdateMyProfile`, `UpdateMySettings` | query / commands | |
| `UploadMyAvatar` | command | Reads the file header: JPEG/PNG/WebP only (`ImageSignature`), 5 MB, uploads with the detected content type, saves, **then** deletes the old file (failure only logged). |
| `GetPublicProfile` | query | Public fields only; missing or inactive = 404. |
| `GetProfileDetails` | query | Admin view (endpoint restricted to Admin/SuperAdmin). |
| `AddMyAddress`, `UpdateMyAddress`, `RemoveMyAddress`, `GetMyAddresses` | commands / query | Load the tracked profile and call the aggregate; no `Update()` call (UA1). |
| `DTOs`, `Mappings`, `CurrentUserExtensions` | helpers | |

## Depends on / used by
`User.Domain`, `Shared.Application`, `Auth.Contracts` (only for the event). Used by Infrastructure (scanning) and Api.

## Rules for this layer
- Load through `GetByIdAsync` (tracked) to change, `GetReadOnlyAsync` to read.
- `UnitOfWorkBehavior` saves only on success; a handler that must save before another step saves explicitly (avatar).
- No caching behaviour is registered (S6).

## How to ...
**Add a use case:** folder `Features/<Area>/<Name>/` with Command, Handler, Validator; test with NSubstitute fakes (`tests/OmniCore.Services.User.Application.Tests/TestSupport.cs`); endpoint in Api.

## Known gaps
- Missing: delete/deactivate profile (UA6), reaction to Auth account events other than registration.
- Tenant use cases removed until ADR 0001.
