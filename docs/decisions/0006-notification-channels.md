# ADR 0006: Notification delivery channels

- Status: **accepted (retroactive)** — code already follows this shape; the ADR was missing and is added 2026-11-23.
- Date: 2026-11-23
- Scope: `backend/c#/OmniCore.Services.Notification/**`
- Related findings: NT1, NT2, AC1 in `docs/repo-check.md`

## Context

Notification must deliver through more than one transport. E-mail goes through SMTP in production and Mailpit in
development; webhooks post to a named URL. Later transports (push, Telegram) are planned. The service also needs to
tell the caller and the broker whether a failure is temporary, so a mail outage retries while a bad address does not.

## Decision

1. **One enum value plus one adapter per transport.** `NotificationChannel` (`Email`, `Webhook`) is the public choice;
   each adapter implements `INotificationChannel` in `Notification.Infrastructure` and is registered in DI. Adding a
   transport means an enum value, an adapter, a registration and a renderer case.
2. **The application layer is transport-agnostic.** `SendNotificationCommandHandler` renders a `NotificationMessage`
   through `ITemplateRenderer`, then calls the selected `INotificationChannel.SendAsync(OutgoingNotification)`. It
   depends on the interface, never on MailKit or `HttpClient`.
3. **Failures are classified by the adapter.** `DeliveryResult.Ok`, `DeliveryResult.Transient(error)` or
   `DeliveryResult.Permanent(error)`. Cancellation is rethrown. For e-mail, SMTP 5xx and parse errors are permanent;
   other errors are transient. For webhooks, 408/429/5xx are transient and other non-success statuses are permanent.
4. **Transient failures return a retryable error; permanent failures are acknowledged.** The broker retries transient
   failures and drops permanent ones after recording them in the delivery log.
5. **Webhook recipients are endpoint names, not URLs.** `Webhooks__Endpoints__<name>` holds the URL. The URL is never
   logged; only the endpoint name and status are.
6. **Templates are built in code and rendered before persistence.** `welcome` and `test` are the current templates.
   Placeholder values are HTML-encoded for e-mail; subjects have CR/LF stripped. The rendered body and template model
   are not stored, because they can carry reset links and tokens.
7. **A deduplicated message is saved before the send.** The unique index on `deduplication_key` claims the key first;
   a concurrent consumer retries and reads the committed row instead of sending a duplicate (NT2).

## Consequences

- A new transport does not change the command, the queries or the other adapters.
- A new enum value without an adapter makes the send fail with `Notification.ChannelNotAvailable`; tests should cover
  that case.
- SMTP and webhook timeouts, endpoint names and credentials come from `Smtp__*` and `Webhooks__*` environment variables.
  Production rejects local Mailpit hosts.
- Only committed send attempts are guaranteed in the delivery log: an attempt that rolls back with the broker
  transaction is not recorded (S12).
- No event in this service publishes a notification request yet; producers must reference
  `Notification.Contracts` and publish `NotificationRequestedIntegrationEvent` (AC1).
