# OmniCore.Services.Notification.Infrastructure

## Purpose

Adapters: SMTP and HTTP delivery, EF Core persistence, MassTransit outbox/inbox, configuration binding and startup
migrations. Nothing here decides *which* notification to send.

## What is inside

### Delivery channels

| Type | Role |
|---|---|
| `Channels/EmailChannel` | maps to `Email`; delegates to shared `IEmailSender`; 5xx and parse errors → Permanent, other errors → Transient |
| `Channels/SmtpEmailSender` | MailKit SMTP client; optional authentication; security mode from `Smtp__Security` (default `Auto`) |
| `Channels/WebhookChannel` | maps to `Webhook`; resolves a named URL from `Webhooks__Endpoints__<name>`; POSTs `{subject, text, content}`; 408/429/5xx → Transient, other non-success → Permanent |
| `Configs/SmtpConfig` | `Smtp` section: Host, Port, Security, Username, Password, FromAddress, FromName, TimeoutSeconds |
| `Configs/WebhooksConfig` | `Webhooks` section: named `Endpoints` and TimeoutSeconds (default 10) |

Webhook URLs are never logged; only the endpoint name and the response status are. Production rejects local Mailpit
hosts for `Smtp__Host`.

### Persistence

| Type | Role |
|---|---|
| `Persistence/Contexts/NotificationDbContext` | schema `notification`, `DbSet<NotificationMessage>`, `AddTransactionalOutboxEntities()` |
| `Persistence/Contexts/NotificationDbContextFactory` | design-time factory for `dotnet ef` (startup project `Notification.Api`) |
| `Persistence/Configurations/NotificationMessageConfiguration` | table `notification_messages`; snake_case columns; enums stored as strings; unique index on `deduplication_key`; indexes on `created_at`, `(status, created_at)` and `recipient` |
| `Persistence/Repositories/NotificationMessageRepository` | `RepositoryBase` plus dedup lookup and no-tracking paged search |
| `Persistence/Migrations/20261123075657_InitialNotification` | creates schema `notification`, `notification_messages`, `inbox_state`, `outbox_state`, `outbox_message`, history table |

The initial migration is required: without it `MigrateAsync()` reports success while creating no tables (NT1).

### Wiring

`DependencyInjection.cs` registers shared infrastructure, the DbContext (Npgsql, retry, snake case, interceptors,
migration history in `notification`), `IUnitOfWork`, MassTransit with the EF outbox, repositories and seeders by
Scrutor scan, both config sections, `IEmailSender → SmtpEmailSender`, a named webhook `HttpClient`, both channels.
`UseNotificationInfrastructureAsync()` applies migrations and seeders before the host starts.

Broker retries are 3 × 2 s with prefetch 16. Kafka has no retry/outbox support in the shared wiring (I6).

## Depends on / used by

- Projects: `Notification.Application` and `Shared.Infrastructure` (`MailKit` package for SMTP).
- Used by: `Notification.Api` and the integration tests.

## Rules for this layer

- A channel returns `DeliveryResult`, never throws for an expected delivery failure; cancellation is rethrown.
- Map provider exceptions here: a transient SMTP/HTTP error must be distinguishable from a permanent one.
- Every new column needs a configuration change and a migration, generated with `dotnet ef`, in the same commit.
- Keep credentials in environment variables, never in code, logs or events.

## How to ...

**Add a new channel:** implement `INotificationChannel`, register it in `DependencyInjection.cs`, add an enum value in the
Domain and a template case. See ADR 0006.

**Add a migration:**
```
cd backend/c#
$env:Database__ConnectionString = 'Host=localhost;...'
dotnet ef migrations add <Name> --project OmniCore.Services.Notification/OmniCore.Services.Notification.Infrastructure --startup-project OmniCore.Services.Notification/OmniCore.Services.Notification.Api --output-dir Persistence/Migrations
```

## Known gaps

- **I6** Kafka has no retry/outbox; handlers of one event retry together.
- **IF5** base images in the Dockerfile are not pinned by digest.
- **AP4** the old finding "`IEmailSender` has no implementation" is stale: `SmtpEmailSender` implements it.
