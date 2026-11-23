# OmniCore.Services.Notification.Contracts

## Purpose

Public event contracts that other services may reference to request a notification. Producers reference **only** this
project, not the Notification service implementation.

## What is inside

| Type | What it is |
|---|---|
| `Events/NotificationRequestedIntegrationEvent` | `record(Channel, Recipient, TemplateKey, Model, DeduplicationKey?) : IntegrationEvent` |
| `AssemblyReference.cs` | assembly marker |

- `Channel` is a string: `Email` or `Webhook`, parsed case-insensitively by the consumer.
- `Recipient` is an e-mail address or the name of a configured webhook endpoint.
- `TemplateKey` selects a built-in renderer template (`welcome` or `test`).
- `Model` maps placeholder names to values; the renderer HTML-encodes values for e-mail.
- `DeduplicationKey` makes redelivery harmless. When null, the consumer uses `event:{event.Id}`.

## Depends on / used by

- Projects: `Shared.Contracts` only, for `IntegrationEvent` (`Id` and `OccurredOnUtc`).
- Used by: `Notification.Application` (consumer) and the integration tests (publisher).
- No production service publishes this event yet.

## Rules for this layer

- Contracts are immutable records. No behaviour, no validation logic beyond the record shape.
- A breaking change needs a new event name or an explicit version; consumers cannot be updated in lockstep.
- Do not reference Auth, User or Notification implementation projects from here.

## How to ...

**Add an event:** create the record in `Events/`, derive from `IntegrationEvent`, add a consumer in
`Notification.Application`, and reference this project from the producer.

**Change an event field:** treat it as breaking. Add a new event type or a version field instead of reusing the name.

## Known gaps

- **AC1** No production publisher exists: confirmation and reset mails have no contracts yet.
- **C1** `IntegrationEvent` carries no version or explicit event-type field.
