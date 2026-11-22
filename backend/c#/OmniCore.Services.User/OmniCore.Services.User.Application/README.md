# OmniCore.Services.User.Application

> **Status: design only, and not wired.** There is no `AddUserApplication()` (the project has no `DependencyInjection.cs`), no endpoint sends these requests, and no repository implementation exists, so none of this has ever run. There are no tests. Treat the code as a first draft to review, not as working behaviour.

## Purpose
The use cases of the User service: create and update a profile, upload an avatar, update settings, add an address, create a per-tenant profile, and read a profile. Same pattern as `Auth.Application`: a MediatR command or query with a handler and a FluentValidation validator per use case, and a domain-event handler for cache invalidation.

## What is inside

### Use cases
| Feature | Request | Handler does | Problems |
|---|---|---|---|
| UserProfiles | `CreateUserProfileCommand(UserId, Username, Email, First?, Last?)` | uniqueness check, `UserProfile.Create`, `Add` | ids and e-mail come from the **body** (U3); nothing calls it (U6) |
| UserProfiles | `UpdateUserProfileCommand(UserId, First, Last, Bio, Phone)` | `GetByIdAsync`, `UpdatePersonalInformation`, `Update` | `UserId` from the body (U3); validator only checks bio length |
| UserProfiles | `UploadAvatarCommand(UserId, IFileUpload)` | uploads to folder `avatars`, `UpdateAvatar`, then deletes the previous file best-effort (order fixed, U4) | trusts client content type (U5); no null check on the upload result |
| UserProfiles | `GetUserProfileByIdQuery(UserId)` (cacheable, 30 min) | full specification (settings, addresses, tenant profiles) -> `UserProfileResponse` | cache never used (S6/AP1) |
| UserSettings | `UpdateUserSettingsCommand(UserId, Theme, Language, E-mail, Push)` | loads with settings, `Settings.UpdatePreferences`, `Update` | settings changed outside the aggregate (UD1) |
| UserAddresses | `AddUserAddressCommand(UserId, address fields, IsPrimary)` | loads with addresses, `UserAddress.Create`, `AddAddress`, `Update` | see UA1 |
| UserTenantProfiles | `CreateTenantProfileCommand(TenantId, UserId, GamerTag, CustomAvatarUrl?)` | builds a `UserTenantProfile` and returns it | **never attached or saved** (U2); tenant and user ids from the body (U3); ADR 0001 pending |

Validators sit next to each request (lengths: username 3-30, bio <= 500, address lines <= 150, city/state/country <= 50, zip <= 20, theme <= 20, language <= 10, avatar <= 5 MB and jpeg/png/webp, gamer tag 3-30).

### Other
| Item | What it is |
|---|---|
| `EventHandlers/UserProfileCacheInvalidationEventHandler` | removes the cache key `GetUserProfileByIdQuery:<id>` on created/updated/avatar-updated. The key is built with the same helper as the query, so unlike Auth it matches (U8). |
| `Mappings/UserProfileMappingExtensions` | hand-written `ToResponse()` for profile, settings, address |
| `DTOs` | `UserProfileResponse`, `UserSettingsResponse`, `UserAddressResponse`, `UserTenantProfileResponse`, and request DTOs `CreateUserProfileRequest`, `UpdateUserProfileRequest` (the latter has no `UserId`, which is the right shape) |
| `AssemblyReference` (`ApplicationAssemblyReference`) | assembly handle for scanning |

## Depends on / used by
- Projects: `User.Domain`, `User.Contracts` (unused), `Shared.Application`.
- Used by: `User.Api` references the Application project but `Program.cs` registers nothing (U1). `User.Infrastructure` references it for the future interface implementations.

## Rules for this layer
Same as [Auth.Application](../../OmniCore.Services.Auth/OmniCore.Services.Auth.Application/README.md): handlers return `Result`, change state through the aggregate, no EF/ASP.NET types, validation in validators, and **the caller's id comes from `ICurrentUserService`** (the token), never from the command. The commands here break that rule today, so they must be changed before an endpoint exposes them (U3).

## How to ...
**Fix the id rule for a command:** remove `UserId` from the command and set it in the handler from `ICurrentUserService.UserId` (return 401 when null), as `GetCurrentUserQueryHandler` does in Auth; keep a separate admin command if an admin must act on another user.

**Wire the layer (when the Api is built):** add `DependencyInjection.AddUserApplication()` that calls `AddSharedApplication(ApplicationAssemblyReference.Assembly)` (copy `AddAuthApplication`).

**Create profiles automatically (U6):** add `OnAccountRegistered : IntegrationEventHandler<AccountRegisteredIntegrationEvent>` that sends `CreateUserProfileCommand` with the event's `AccountId`, username and e-mail; this needs `Auth.Contracts` referenced and service-scoped queue names (I3).

## Known gaps
Ids from `docs/repo-check.md`; existing: U2, U3, U4, U5, U6, U8. New ones (`UA1`..`UA6`) are added there in the same commit.
- **UA1 (new, high until verified)** `AddUserAddress` and `UpdateUserSettings` load the profile with a specification that calls `ApplyNoTracking()`, modify it, then call `Update(profile)`. For a no-tracking entity EF `Update` marks every object with a non-empty key as *modified*, so the new address (its Guid is created client-side) becomes an `UPDATE` of a row that does not exist and the save fails with `DbUpdateConcurrencyException`. Load with tracking for writes (use `GetByIdAsync` or a tracked specification) and drop `Update(...)`. Verify with an integration test once Infrastructure exists.
- **UA2 (new, medium)** No `DependencyInjection`, no `Program` wiring, no tests (U1): the whole layer is unproven, including MediatR registration and validator discovery.
- **UA3 (new, low)** `UploadAvatarCommandHandler` uploads before saving: if the save fails, the new file is orphaned in storage; the deletion of the old file swallows every exception without logging. Acceptable as best effort, but log it and add a periodic orphan sweep.
- **UA4 (new, low)** Validation is thin: no length or format rules for first/last name or phone (`UpdateUserProfileCommandValidator` checks only the bio), language is not checked against real culture codes, theme is a free string (UD4), the custom tenant avatar URL is not validated at all.
- **UA5 (new, low)** Namespaces do not match folders (`Features/UserAddresses/AddUserAddress/` holds namespace `...UserAddresses.Commands.AddUserAddress`; tenant profile files sit directly in `Commands/`), and the tenant feature is the only one without a `Queries` side.
- **UA6 (new, low)** No use case to list, update, delete or set the primary address, delete the profile, deactivate it, or read other users' public profiles; `GetUserProfileByIdQuery` returns phone and e-mail, so it must be restricted to the owner or an admin when exposed.
- **UD2** profile e-mail/username are copies of Auth data with no sync.
