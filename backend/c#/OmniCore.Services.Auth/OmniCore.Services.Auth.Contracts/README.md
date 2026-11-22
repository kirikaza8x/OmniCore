# OmniCore.Services.Auth.Contracts

## Purpose
The public message contract of the Auth service: the integration events other services may consume. It is the only Auth project another service is allowed to reference. It holds plain records and nothing else (no logic, no Auth domain types), so a consumer never depends on how Auth is built.

## What is inside
| Type | What it is |
|---|---|
| `Events/AccountRegisteredIntegrationEvent` | Published once when an account is created: `AccountId`, `Username`, `Email`, `FirstName`, `LastName`. Derives from `Shared.Contracts.Events.IntegrationEvent`, so it also carries the base `Id` and `OccurredOn`. |
| `ContractsAssemblyReference` | Assembly handle (used for scanning). |

How the event is produced: `Account.Create` raises `AccountCreatedDomainEvent`; `PublishAccountRegisteredIntegrationEventHandler` (Auth.Application) maps it and calls `IEventBus.PublishAsync`, which stores it in the MassTransit EF outbox in the same transaction as the account. `FirstName` falls back to the username and `LastName` to an empty string, because registration collects neither.

## Depends on / used by
- Projects: `Shared.Contracts` only.
- Used by: `Auth.Application` (publisher). **No consumer exists yet**: `Auth.Application` is the only project referencing it, and the User service has no handler for it (U1/U2).

## Rules for this layer
- Only immutable `record`s that derive from `IntegrationEvent`; primitive types, Guids, strings, enums owned by this project. No value objects or entities from the domain.
- Name events in the past tense with the suffix `IntegrationEvent` (`AccountRegisteredIntegrationEvent`).
- A published event is a promise to other services: add fields at the end with defaults, never rename or remove one. For a breaking change create a `...V2` record and publish both for a while (see C1).
- Put no secrets or sensitive data in an event (no password hash, no tokens). The e-mail is already personal data: publish it only to services that need it.

## How to ...
**Add an event:** create `Events/<Name>IntegrationEvent.cs` (`public record X(Guid AccountId, ...) : IntegrationEvent;`), raise a domain event in the aggregate, publish it from a `IDomainEventHandler` in `Auth.Application` (copy the publisher above).

**Consume it in another service:** reference this project, add `public sealed class OnAccountRegistered : IntegrationEventHandler<AccountRegisteredIntegrationEvent>` in that service's Application layer (the Shared handler scan registers it), and make the queue/consumer group name service-specific first (I3).

## Known gaps
Ids from `docs/repo-check.md`; new ones (`AC1`, `AC2`) are added there in the same commit.
- **C1** There is no event versioning or schema check (shared kernel finding).
- **I3** Queue and consumer-group names are not service-scoped, which matters as soon as a second service subscribes.
- **AC1 (new, low)** Only one event exists. Account changes other services will care about (e-mail changed, account deactivated or deleted, role changed) have no contract, so a future User/Tenant service cannot stay in sync. Add them with the matching Account features (AA4), before the User service is built.
- **AC2 (new, low)** The event names `FirstName` / `LastName` although Auth stores neither: they are invented (`FirstName = Username`) so that the User service can create a profile. Better: send only what Auth owns (`AccountId`, `Username`, `Email`) and let the profile owner collect names.
