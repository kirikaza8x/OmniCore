# OmniCore.Services.Notification (master overview)

The delivery service: it turns integration events into e-mails and webhook calls, and keeps a delivery log that an
administrator can query. It owns its own PostgreSQL database (`omnicore_notification_db`, schema `notification`).
It consumes events from Auth and from its own contracts; it publishes none today. It is built from five projects with
the same layering as every OmniCore service; each has its own `README.md`, this page is the map.

## The five projects

| Project | One-line role | Details |
|---|---|---|
| `OmniCore.Services.Notification.Domain` | `NotificationMessage` aggregate, channel/status enums, errors, repository contract | [README](OmniCore.Services.Notification.Domain/README.md) |
| `OmniCore.Services.Notification.Contracts` | Public `NotificationRequestedIntegrationEvent` for producers | [README](OmniCore.Services.Notification.Contracts/README.md) |
| `OmniCore.Services.Notification.Application` | Send command, queries, event handlers, templates, channel abstraction | [README](OmniCore.Services.Notification.Application/README.md) |
| `OmniCore.Services.Notification.Infrastructure` | SMTP/webhook channels, EF Core + migration, MassTransit wiring, configs | [README](OmniCore.Services.Notification.Infrastructure/README.md) |
| `OmniCore.Services.Notification.Api` | Carter endpoints, admin auth, `Program.cs` (the process entry point) | [README](OmniCore.Services.Notification.Api/README.md) |

Tests: `backend/c#/tests/OmniCore.Services.Notification.Domain.Tests`, `...Application.Tests`
(handler, event-handler and template-renderer tests) and `...IntegrationTests` (8 tests, Testcontainers with PostgreSQL
and RabbitMQ, need Docker). All are in CI. Local integration and application runs were green on 2026-11-23 (20/20
application, 8/8 integration).

## How they depend on each other

```
Notification.Api --> Notification.Infrastructure --> Notification.Application --> Notification.Domain --> Shared.Domain
      |                     |                              |
      |                     |                              +--> Notification.Contracts --> Shared.Contracts
      |                     |                              +--> Auth.Contracts (only for the welcome event)
      |                     +--> Shared.Infrastructure
      +--> Shared.Api
```

Arrows point inward. `Notification.Api` is the only project nothing else references. Notification never references
the Auth or User service projects; other services may reference only `Notification.Contracts`.

## What the service offers (HTTP, base `/api/v1/notifications`)

| Endpoint | Access | What it does |
|---|---|---|
| `GET /` (query: `page`, `pageSize`, `status`, `channel`, `recipient`) | `Admin`, `SuperAdmin` | paged delivery log, newest first |
| `GET /{id:guid}` | `Admin`, `SuperAdmin` | one delivery record, 404 when absent |
| `POST /test` (body: `channel`, `recipient`) | `Admin`, `SuperAdmin` | sends the built-in `test` template; no dedup key, so every call sends |
| `GET /health` | anonymous | liveness |

`/health` is liveness only; a failing database or broker does not turn it red (A18).

## What happens on an event

1. `AccountRegisteredIntegrationEvent` (Auth) → `AccountRegisteredIntegrationEventHandler` sends the `welcome` e-mail
   once per account with dedup key `welcome:{AccountId}`. The name comes from `FirstName` or `Username` (AC2).
2. `NotificationRequestedIntegrationEvent` → `NotificationRequestedIntegrationEventHandler` parses `Channel`, uses the
   event's dedup key or `event:{event.Id}` and sends through the requested channel. There is no production publisher yet.
3. `IntegrationEventConsumer` runs a message's handlers in parallel. Validation, not-found and conflict results are
   logged and acknowledged; other failures throw so MassTransit retries (3 × 2 s) and then moves the message to the
   error queue (I6: all handlers retry when one fails).

## Delivery and idempotency

- Templates are built in code: `welcome` (HTML e-mail) and `test`. `{{placeholder}}` values are HTML-encoded for e-mail;
  the subject has CR/LF stripped. Missing placeholders become empty strings.
- The rendered body and the template model are **not** stored: they can contain reset links and tokens.
- A new message with a dedup key is saved **before** the send. The unique index on `deduplication_key` claims the key;
  a concurrent loser retries, reads the committed row and does not send again (NT2).
- Success → `MarkSent`. Permanent failure or 5 attempts → `MarkFailed`, acknowledged. Transient failure → the command
  returns a retryable error, so the broker redelivers. Attempts that roll back with the broker transaction may not
  reach the log; only committed attempts are guaranteed (S12).
- Channel details: [ADR 0006](../../../docs/decisions/0006-notification-channels.md).

## Configuration at a glance

Environment variables (compose), validated at startup. Service-specific: `Smtp__*` (Host, Port, Security, Username,
Password, FromAddress, FromName, TimeoutSeconds), `Webhooks__Endpoints__<name>` and `Webhooks__TimeoutSeconds`.
Shared: `Database__*`, `MessageBroker__*`, `Jwt__*`, `Cors__*`, `RateLimiting__*`, `Redis__*`, `MemoryCache__*`,
`Storage__*`. Local values are in `infra/docker/.env.example`; Production rejects local Mailpit hosts for SMTP.
Settings live in environment variables; `appsettings*.json` are git-ignored (S11).

## Rules that hold across the service

- Layers point inward; Domain knows no framework. Expected failures are `Result`/`Error`, not exceptions.
- State changes go through `NotificationMessage`; the handler orchestrates.
- Every endpoint states its access rule explicitly; there is no fallback authorization policy yet (AA1).
- A new channel means an enum value, an `INotificationChannel` implementation and a registration (ADR 0006).
- A new template means a renderer case plus unit tests; never store rendered bodies or secrets in the log.
- Add a column and its migration in one commit, then regenerate the model snapshot with `dotnet ef`.

## Known problems, by project (full text in `docs/repo-check.md`)

| Project | Ids | Headlines |
|---|---|---|
| Api | AA1, A18, W1, W6 | no fallback policy; roles compared case-sensitively; liveness only; rate limiter runs before auth |
| Application | I6, AC1, AC2, S12 | all event handlers retry together; no confirmation/reset events; welcome name falls back to username |
| Infrastructure | A12, IF5, S11 | no reset/confirmation flow to use; base images not pinned; no committed appsettings |
| Whole service | NT1, NT2 | fixed: initial migration created the schema; dedup key is claimed before the send |
