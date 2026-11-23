# OmniCore.Services.Notification.Application

## Purpose

The use cases: validate a request, choose a channel, render a template, persist the delivery attempt and send it.
It also consumes integration events and translates channel failures into retryable or final results.

## What is inside

### Send command

| Type | Role |
|---|---|
| `Features/Notifications/Commands/SendNotification/SendNotificationCommand` | `ICommand<NotificationResponse>` with channel, recipient, template key, model and optional dedup key |
| `.../SendNotificationCommandHandler` | finds an existing delivery, renders, creates the message, claims the dedup key, sends, records the outcome |
| `.../SendNotificationCommandValidator` | channel in enum, recipient ≤ 256, template key ≤ 64, dedup key ≤ 128, model ≤ 50 entries and values ≤ 2000 chars |

Delivery sequence: `GetByDeduplicationKeyAsync` → render → `NotificationMessage.Create` → `SaveChangesAsync` for a new
deduplicated message (claim the unique key before the side effect) → `INotificationChannel.SendAsync` →
`MarkSent` / `MarkAttemptFailed`. The shared `UnitOfWorkBehavior` saves successful commands; the handler saves
explicitly for the dedup claim and for failed attempts.

- Transient failure returns `Notification.DeliveryFailed`, so the broker retries.
- Permanent failure or 5 attempts returns success with status `Failed`, so the broker acknowledges and drops it.

### Queries

| Type | Role |
|---|---|
| `Features/Notifications/Queries/GetNotification/` | get one delivery, `Notification.NotFound` when absent |
| `Features/Notifications/Queries/GetNotifications/` | paged list (page 1, size 20 by default; size clamped to 1–100), filters for status, channel and recipient |

### Integration events

| Type | Role |
|---|---|
| `Features/Notifications/IntegrationEvents/AccountRegisteredIntegrationEventHandler` | consumes Auth's event, sends `welcome` to the e-mail, dedup key `welcome:{AccountId}`; name comes from `FirstName` or `Username` |
| `.../NotificationRequestedIntegrationEventHandler` | consumes the public event, dedup key from the event or `event:{event.Id}` |
| `.../NotificationEventOutcome.ThrowIfRetryable` | validation/not-found/conflict → log and drop; other failures → throw for broker retry |

### Support

| Type | Role |
|---|---|
| `Abstractions/INotificationChannel` | `Channel` plus `SendAsync(OutgoingNotification)`; results are `Ok`, `Transient(error)` or `Permanent(error)` |
| `Abstractions/ITemplateRenderer` | `Render(templateKey, channel, model)` |
| `Templates/BuiltInTemplateRenderer` | `welcome` and `test` templates; `{{placeholder}}` substitution, HTML encoding for e-mail, CRLF stripped from subjects |
| `DTOs/NotificationResponse` | response record and `NotificationMappings.ToResponse()` |
| `DependencyInjection.cs` | `AddNotificationApplication()` → shared application wiring; registers the renderer as a singleton |

## Depends on / used by

- Projects: `Notification.Domain`, `Notification.Contracts`, `Auth.Contracts` (welcome event only), `Shared.Application`.
- Used by: `Notification.Infrastructure`, `Notification.Api`, `tests/OmniCore.Services.Notification.Application.Tests`.

## Rules for this layer

- Handlers orchestrate; validation and state transitions stay in the Domain or the validators.
- Never return raw exceptions for expected failures; return `Result`/`Error`.
- Never put secrets or rendered bodies in logs or in `NotificationMessage`.
- A deduplicated message is saved before the send; removing that save brings back the double-send race (NT2).
- A new template needs renderer code and unit tests; a new channel also needs an Infrastructure adapter (ADR 0006).

## How to ...

**Add a use case:** command/query, handler, validator and DTO under `Features/`, plus a handler test. The assembly is
scanned by MediatR through `AddNotificationApplication()`, so no manual registration is needed.

**Add a template:** add a case to `BuiltInTemplateRenderer` and tests for HTML/plain output, encoding and missing
placeholders.

## Known gaps

- **I6** all handlers of one event run in parallel and all retry when one fails; keep handlers idempotent.
- **AC1/AC2** no confirmation/reset events and no real name in the Auth event, so the welcome greeting falls back to username.
- **S12** attempts that roll back with the broker transaction may not be logged.
