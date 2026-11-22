# OmniCore.Services.User.Domain

## Purpose
The business model of a user profile and the rules that keep it valid. No EF, no HTTP, no MediatR. Design reasons: [ADR 0004](../../../../docs/decisions/0004-user-service-design.md). Covered by `tests/OmniCore.Services.User.Domain.Tests` (25 tests, green in CI).

## What is inside
| Type | What it is |
|---|---|
| `UserProfile` (aggregate root) | Id = Auth account id. `Create(accountId, username, email?, first?, last?)` returns a `Result`. Methods: `UpdatePersonalInformation` (full replace, length/phone rules), `UpdateAvatar`, `UpdateSettings`, `AddAddress`, `UpdateAddress`, `RemoveAddress`, `SoftDelete` (idempotent, raises an event). Limits: 10 addresses, name 100, bio 500, phone 32. |
| `UserSettings` | Owned by the profile. Theme `Light`/`Dark`/`System` (case-normalised), language like `en` or `vi-VN`. Changed only through the aggregate (`internal`). |
| `UserAddress` | Owned by the profile; first address is primary, setting a new primary clears the others, removing the primary promotes the next. |
| `UserTenantProfile` | Kept for ADR 0001, **not mapped or used**. |
| Value objects | `UserId`, `UserSettingsId`, `UserAddressId`, `UserTenantProfileId`, `Username` (copy of the Auth rules, U9). |
| Events | `UserProfileCreated` (with e-mail), `Updated`, `AvatarUpdated`, `Deleted`. |
| `UserProfileErrors` | NotFound, AddressNotFound, TooManyAddresses, Unauthenticated, TooLong, PhoneInvalid, ThemeInvalid, LanguageInvalid, AvatarUrlEmpty. |
| `IUserProfileRepository` | `IRepository<UserProfile, UserId>` plus `GetReadOnlyAsync` (no tracking, for queries). |

## Depends on / used by
`Shared.Domain` only. Used by Application and Infrastructure.

## Rules for this layer
- Children (settings, addresses) change only through the aggregate; nothing outside calls `SetPrimary` or edits an address directly.
- Every rule returns a `Result`; no exceptions for bad input.
- E-mail and username are display copies; Auth is the source of truth (no uniqueness here).

## How to ...
**Add a profile field:** property on `UserProfile`, validation inside the method that sets it, a test, then the EF mapping and a migration in the same commit (`scripts/shells/add-migration.ps1 -Service User -Name AddX`).

## Known gaps
- **UD2** No sync with Auth when username or e-mail change, and no reaction to account deactivation.
- **UD6** `GamerTag` naming on `UserTenantProfile` is game-specific; decide with ADR 0001.
- **U7/U9** Tenant overlap and the duplicated `Username` type remain.
