# OmniCore.Services.Notification.Domain

## Purpose

The delivery model, free of frameworks. `NotificationMessage` is the aggregate and the delivery log: who received what,
through which channel, with which template, and what happened. It owns the validation of recipients, template keys and
dedup keys, and the state transitions for send attempts. Rendering, SMTP and the broker are not part of this layer.

## What is inside

| Type | Kind | What it models |
|---|---|---|
| `Entities/NotificationMessage` | aggregate root | channel, recipient, template key, subject, status, attempts, last error, dedup key, timestamps |
| `Enums/NotificationChannel` | enum | `Email = 1`, `Webhook = 2` |
| `Enums/NotificationStatus` | enum | `Pending = 0`, `Sent = 1`, `Failed = 2` |
| `Errors/NotificationErrors` | errors | not found, invalid recipient/template/dedup key, template not found, channel unavailable, already finished, delivery failed |
| `Repositories/INotificationMessageRepository` | interface | `IRepository` plus `GetByDeduplicationKeyAsync` and `SearchAsync` |
| `ValueObjects/NotificationMessageId` | id | typed `Guid` with `From` and `New` |
| `AssemblyReference.cs` | marker | assembly reference for scanning |

## `NotificationMessage` behaviour

| Method | Rule |
|---|---|
| `Create(channel, recipient, templateKey, subject, dedupKey?)` | validates all inputs; starts `Pending` with zero attempts |
| `MarkSent()` | increments attempts, sets `Sent`, clears `LastError` |
| `MarkAttemptFailed(error, permanent)` | increments attempts, truncates the error to 500 chars; a permanent error or 5 attempts moves to `Failed` |

Limits: `MaxAttempts = 5`, `MaxRecipientLength = 256`, `MaxErrorLength = 500`, `MaxDeduplicationKeyLength = 128`.

- E-mail recipients must equal the parsed address exactly: no display names, no header injection.
- Webhook recipients are endpoint **names**, matched by `^[a-z0-9][a-z0-9-]{0,49}$`. URLs are rejected.
- Template keys match `^[a-z0-9][a-z0-9.-]{0,63}$`.
- `Sent` and `Failed` messages are finished; they cannot be retried or resent.
- The rendered body and template model are deliberately not stored: they can contain reset links and tokens.

## Depends on / used by

- Projects: `Shared.Domain` only (DDD base types, `Result`, `Error`, `IRepository`).
- Used by: `Notification.Application`, `Notification.Infrastructure` (mapping and repository),
  `tests/OmniCore.Services.Notification.Domain.Tests`.

## Rules for this layer

- Change state only through aggregate methods that return `Result`; setters stay private.
- Do not add EF Core, ASP.NET, MassTransit or MailKit types here.
- A new channel means a new enum value plus an `INotificationChannel` implementation (ADR 0006).
- Add validation here, not only in the command validator.

## How to ...

**Add a rule:** add an aggregate method that returns `Result`, guard finished messages where relevant, and add a test in
`tests/OmniCore.Services.Notification.Domain.Tests`.

**Add a column:** change the entity here, its `IEntityTypeConfiguration` and migration in `Notification.Infrastructure`
in the same commit, then regenerate the EF snapshot.

## Known gaps

None raised specifically against this layer. Cross-service findings are in the master README and `docs/repo-check.md`.
