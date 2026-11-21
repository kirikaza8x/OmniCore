# OmniCore.Shared.Contracts

## Purpose
The tiny base layer for integration events: messages that cross service boundaries through the message broker (RabbitMQ or Kafka). Service-specific event classes live in each service's own `...Contracts` project and build on the types here, so services can share messages without referencing each other.

## What is inside

### Events/
| Type | What it does |
|---|---|
| `IIntegrationEvent` | Marker contract: `Guid Id`, `DateTime OccurredOnUtc`. |
| `IntegrationEvent` | Abstract record implementing it. The parameterless protected constructor sets `Id = Guid.NewGuid()` and `OccurredOnUtc = DateTime.UtcNow`; a second protected constructor takes both values (for rebuilding an event). Both properties are `init`. |
| `IPartitionedIntegrationEvent` | Optional: `string PartitionKey`. Implement it on an event to keep related events on one Kafka partition (e.g. the aggregate id). Read by `KafkaEventBus.PublishAsync`; without it the event `Id` (a fresh GUID) is the key, so there is no ordering between events of one aggregate. |
| `IIntegrationEventHandler<in TIntegrationEvent>` | `Task HandleAsync(event, cancellationToken)`. |
| `IntegrationEventHandler<TIntegrationEvent>` | Abstract base class with one abstract `HandleAsync`. Inherit this in a consuming service. |

## Depends on / used by
- Depends on: nothing (the .csproj has no package or project references; `net9.0`).
- Used by (verified from .csproj files): `OmniCore.Shared.Application` and `OmniCore.Services.Auth.Contracts`. `Shared.Infrastructure` code (`KafkaEventBus`, `IntegrationEventConsumer`, `MassTransitExtensions`, `EventBusReflectionExtensions`) uses these types through the Application reference.

## Rules for this layer
- Only message shapes and handler contracts. No logic, no EF, no MediatR, no references to any other project (CLAUDE.md: `Shared` holds only generic code).
- Events are records with plain, serializable properties (primitives, ids, strings, dates). Do not put domain entities or value objects in an event, so consumers never need the publisher's Domain project.
- Do not rename or remove a property on an event that is already published without a plan for messages in flight; this is a wire format, not just a C# type.
- Service-specific events go in that service's `...Contracts` project, not here.

## How to ...

**Add a new integration event** (in the service's Contracts project, e.g. Auth):
```csharp
public record AccountRegisteredIntegrationEvent(Guid AccountId, string Username, string Email,
    string FirstName, string LastName) : IntegrationEvent;   // the real one in Auth.Contracts
```
Add `IPartitionedIntegrationEvent` and a `PartitionKey` if ordering per account matters on Kafka.

**Handle an event in a service.** Create `class AccountRegisteredHandler : IntegrationEventHandler<AccountRegisteredIntegrationEvent>` and override `HandleAsync`. `EventBusReflectionExtensions` scans the service's assemblies for non-abstract classes implementing `IIntegrationEventHandler<>` (`GetHandledEventTypes`) and for event types (`GetIntegrationEventTypes`); the Kafka topic name is the event type name in kebab-case (`ToKebabCase`). See `OmniCore.Shared.Infrastructure/README.md` for the full wiring.

**Add a field to an existing event.** Add it as a new optional/nullable property so old messages still deserialize.

## Known gaps
- new: `IntegrationEvent` has no `EventType` or schema/version field, unlike the domain-side `DomainEvent.EventType`. Versioning has to be managed by hand.
- new: `IPartitionedIntegrationEvent` does not extend `IIntegrationEvent`, so nothing at compile time stops it being put on a non-event type.
- No repo-check finding (S/A/U ids) covers this project. Otherwise none found.
