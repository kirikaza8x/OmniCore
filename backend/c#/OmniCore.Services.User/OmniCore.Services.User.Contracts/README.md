# OmniCore.Services.User.Contracts

> **Status: empty placeholder.** The project holds only `ContractsAssemblyReference`. It has no package or project references (not even `Shared.Contracts`), and the User service publishes no integration events yet.

## Purpose
Reserved for the public message contract of the User service: the integration events other services may consume (for example "profile updated" or "avatar changed"), written as plain records. It follows the same rules as `Auth.Contracts`: it is the only User project another service may reference.

## What is inside
| Type | What it is |
|---|---|
| `ContractsAssemblyReference` | assembly handle for scanning |

Nothing else. `User.Application` references this project but uses none of it.

## Depends on / used by
- Projects: none (it should reference `Shared.Contracts` as soon as the first event is added, so events derive from `IntegrationEvent`).
- Used by: `User.Application` (reference only).

## Rules for this layer
Same as [Auth.Contracts](../../OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/README.md): immutable `record`s deriving from `IntegrationEvent`, past-tense names ending in `IntegrationEvent`, no domain types, additive changes only, no secrets.

## How to ...
**Add the first event:** add a project reference to `Shared.Contracts`, create `Events/UserProfileUpdatedIntegrationEvent.cs`, and publish it from a domain-event handler in `User.Application` through `IEventBus` (the existing `UserProfileCacheInvalidationEventHandler` shows where such a handler goes). Check queue/consumer names first (I3).

## Known gaps
New ids (`UC1`) are added to `docs/repo-check.md` in the same commit.
- **UC1 (new, low)** No contracts yet. Besides outgoing events, the User service must **consume** Auth's `AccountRegisteredIntegrationEvent` to create profiles (U6), which means referencing `Auth.Contracts`, not the other way round. Decide which profile changes other services need (display name, avatar) before inventing events.
- **U6** nothing consumes the Auth event, so no profile is ever created automatically.
