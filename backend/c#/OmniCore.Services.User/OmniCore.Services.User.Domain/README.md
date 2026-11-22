# OmniCore.Services.User.Domain

> **Status: design only.** The User service does not run yet (no endpoints, DbContext, repository implementation or migrations, finding U1). This project compiles and holds the intended model, but nothing has exercised it and it has **no unit tests**.

## Purpose
The business model of the User service: who a person is *beyond* their login. Auth owns credentials; User owns the profile (names, bio, avatar, phone), preferences, addresses and per-tenant profiles. `UserProfile.Id` is the **same Guid as the Auth `AccountId`** (`UserId.From(accountId)`), which is the only link between the two services: there is no foreign key across the databases.

## What is inside

### Entities (`Entities/`)
| Type | Kind | What it models |
|---|---|---|
| `UserProfile` | aggregate root, `IAuditableEntity`, `ISoftDeletable` | username, e-mail, first/last/display name, bio, avatar URL, phone, `IsActive`, soft-delete fields; owns one `UserSettings`, many `UserAddress`, many `UserTenantProfile` |
| `UserSettings` | entity (1:1) | `Theme` (default "Dark"), `Language` ("en-US"), e-mail and push notification flags |
| `UserAddress` | entity | address lines, city, state, country, zip, `IsPrimary` |
| `UserTenantProfile` | `TenantAggregateRoot` | per-tenant `GamerTag`, custom avatar, `JoinedAtUtc` |

### `UserProfile` behaviour
| Method | Rule |
|---|---|
| `UserProfile.Create(rawUserId, username, email, first?, last?)` | validates username and e-mail, display name = "first last" or the username, `IsActive = true`, creates default `UserSettings`, raises `UserProfileCreatedDomainEvent` |
| `UpdatePersonalInformation(first, last, bio, phone)` | **overwrites** all four fields (null clears), recomputes display name, raises `UserProfileUpdatedDomainEvent` |
| `UpdateAvatar(url)` | rejects empty, raises `UserAvatarUpdatedDomainEvent` |
| `AddAddress(address)` | a new primary address clears the flag on the others |
| `SoftDelete()` | `IsDeleted`, `IsActive = false`, `DeletedAtUtc` |

### Value objects, events, errors, contracts
| Item | What it is |
|---|---|
| `ValueObjects/UserId`, `UserSettingsId`, `UserAddressId`, `UserTenantProfileId` | typed Guid ids (class-based `ValueObject` style from Shared.Domain) |
| `ValueObjects/Username` | 3-30 characters after trim, **no character rule** (Auth's `Username` allows only letters, digits, `_ . -`): duplicate type, different rules (U9) |
| E-mail | uses `EmailAddress` from `Shared.Domain` (lower-cased, validated) |
| `Events/` | `UserProfileCreated`, `UserProfileUpdated`, `UserAvatarUpdated` domain events |
| `Errors/UserProfileErrors` | ready-made `Error`s: `NotFound`, `UsernameAlreadyExists`, `EmailAlreadyExists`, `AddressNotFound`, `AvatarUrlEmpty` |
| `Repositories/IUserProfileRepository` | by username/e-mail, uniqueness checks, local-tracker lookup, `GetWithSpecificationAsync`; **no implementation** |
| `Specifications/` | `UserProfileWithSettings`, `WithAddresses`, `Full` (settings + addresses + tenant profiles); all exclude deleted profiles except the by-identifier form |

## Depends on / used by
- Projects: `Shared.Domain` only.
- Used by: `User.Application` (handlers). `User.Infrastructure` does not reference it yet because it holds no mappings.

## Rules for this layer
- Same as Auth.Domain: private setters, state changes through aggregate methods returning `Result`, validation in value objects, no framework types.
- The profile id is the account id: never generate a new `UserId` for an existing account (`UserId.New()` is only for tests).
- Do not duplicate Auth data without a sync plan. E-mail and username are copies; Auth is the source of truth.
- Anything tenant-specific belongs in a `TenantAggregateRoot`, and tenancy is **not to be built before the owner confirms ADR 0001**.

## How to ...
**Add a field to the profile:** property with a private setter, validation in `UpdatePersonalInformation` (or a new method), an event if others care; Application handler/DTO; the EF mapping and migration come with the Infrastructure layer (U1) in the same commit.

**Add an aggregate method with a test:** there is no User test project yet; create `tests/OmniCore.Services.User.Domain.Tests` by copying the Auth domain test project, then add it to `OmniCore.slnx` and to `backend.yml`.

## Known gaps
Ids from `docs/repo-check.md`; existing: U1, U2, U7, U9. New ones (`UD1`..`UD8`) are added there in the same commit.
- **UD1 (new, medium)** The aggregate is incomplete: no method to update or remove an address, to change settings through the aggregate (`UserSettings.UpdatePreferences` is public and unvalidated, so the aggregate cannot guard it), to add a tenant profile (`_tenantProfiles` is never filled, U2/U7), to change e-mail or username, or to deactivate. Several Application handlers therefore bypass the aggregate.
- **UD2 (new, medium)** No sync with Auth: e-mail and username are stored twice and nothing updates them when Auth changes them, and nothing deactivates a profile when the account is deactivated or deleted (Auth raises no such events, AC1/AD2). Decide: drop e-mail/username from the profile (read them from Auth by id) or consume Auth events for every change.
- **UD3 (new, low)** `UpdatePersonalInformation` overwrites every field, so a partial update from a client erases the omitted ones; no length or format rules for names, bio or phone (the validators in Application are the only guard).
- **UD4 (new, low)** `UserSettings.Theme` and `Language` are free strings; use an enum / validated culture code. A separate table for four columns could be an owned type of `UserProfile`.
- **UD5 (new, low)** `UserAddress.SetPrimary` is public, so code outside the aggregate can create two primaries; `AddAddress` has no limit and no duplicate check, and nothing guarantees one primary exists.
- **UD6 (new, low)** `UserTenantProfile.GamerTag` is a gaming-specific term inside a generic backend, and the entity derives from `TenantAggregateRoot` while `UserProfile` (its parent) does not (U7). Rename (for example `Nickname`) in the entity review.
- **UD7 (new, low)** `SoftDelete` raises no event and ignores `deletedBy`; the by-identifier specification does not exclude deleted profiles and compares the exact string (an upper-case e-mail does not match the lower-cased stored one).
- **UD8 (new, low)** `IUserProfileRepository.GetWithSpecificationAsync` duplicates the `FirstOrDefaultAsync(spec)` already in the Shared repository interface.
- **D1** soft delete shape is the shared-kernel question again (`ISoftDeletable` has only `IsActive`).
- **U9** duplicate `Username` value object.
- No unit tests at all (Auth.Domain has 17).
