# Integration events

Machine-readable JSON Schemas for these events are in [`shared/contracts/`](../shared/contracts/README.md).

Every record that implements `IIntegrationEvent` in a `*.Contracts` project, checked against the code on `develop`
(2026-11-24). The contract file is the source of truth; publisher and consumer paths are given so each claim can be
checked. The messaging wiring itself is described in [architecture.md](architecture.md#messaging).

## Contents

- [Overview](#overview)
- [Shared base types](#shared-base-types)
- [AccountRegisteredIntegrationEvent](#accountregisteredintegrationevent)
- [PasswordResetRequestedIntegrationEvent](#passwordresetrequestedintegrationevent)
- [EmailVerificationRequestedIntegrationEvent](#emailverificationrequestedintegrationevent)
- [NotificationRequestedIntegrationEvent](#notificationrequestedintegrationevent)
- [Outbox and inbox](#outbox-and-inbox)

## Overview

| Event | Contract file | Published by | Consumed by | Carries a secret |
|---|---|---|---|---|
| `AccountRegisteredIntegrationEvent` | `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/AccountRegisteredIntegrationEvent.cs` | Auth | User, Notification | no |
| `PasswordResetRequestedIntegrationEvent` | `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/PasswordResetRequestedIntegrationEvent.cs` | Auth | Notification | **yes** (raw one-time token) |
| `EmailVerificationRequestedIntegrationEvent` | `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/EmailVerificationRequestedIntegrationEvent.cs` | Auth | Notification | **yes** (raw one-time token) |
| `NotificationRequestedIntegrationEvent` | `backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Contracts/Events/NotificationRequestedIntegrationEvent.cs` | any service (no production publisher yet) | Notification | no dedicated secret field |

`OmniCore.Services.User.Contracts` and `OmniCore.Shared.Contracts` contain no concrete event record; User has only an
`AssemblyReference`, and Shared has the base types listed below.

## Shared base types

These are in `backend/c#/OmniCore.Shared/OmniCore.Shared.Contracts/Events/`:

| Type | File | What it is |
|---|---|---|
| `IIntegrationEvent` | `IIntegrationEvent.cs` | `Guid Id`, `DateTime OccurredOnUtc`. Every event has these two fields. |
| `IntegrationEvent` | `IntegrationEvent.cs` | Abstract record implementing `IIntegrationEvent`; assigns a new `Id` and `OccurredOnUtc` in its constructor. All four events derive from it. |
| `IPartitionedIntegrationEvent` | `IPartitionedIntegrationEvent.cs` | Optional `string PartitionKey` for the Kafka partition key; when absent `KafkaEventBus` uses the event `Id` (`KafkaEventBus.cs:56-58`). |
| `ITenantIntegrationEvent` | `ITenantIntegrationEvent.cs` | Optional `Guid TenantId` (ADR 0001); consumers set the tenant context from it. Also holds `TenantPartitionKey.For(tenantId, aggregateId)`. |
| `IIntegrationEventHandler<T>` | `IIntegrationEventHandler.cs` | `Task HandleAsync(T, CancellationToken)`. |
| `IntegrationEventHandler<T>` | `IntegrationEventHandler.cs` | Abstract base class for handlers; implements `IIntegrationEventHandler<T>`. |

None of the four events below implements `IPartitionedIntegrationEvent` or `ITenantIntegrationEvent` today, so they have
no partition key and no tenant id of their own.

## AccountRegisteredIntegrationEvent

- Contract: `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/AccountRegisteredIntegrationEvent.cs`
- Publishing service: **Auth**.
  `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Application/Features/Auth/EventHandlers/PublishAccountRegisteredIntegrationEventHandler.cs`
  maps the `AccountCreatedDomainEvent` (raised by `Account.Create`, `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Domain/Entities/Account.cs:61`)
  to this event and publishes it through `IEventBus`.
- Consuming services:
  - **User**:
    `backend/c#/OmniCore.Services.User/OmniCore.Services.User.Application/Features/Profiles/IntegrationEvents/AccountRegisteredIntegrationEventHandler.cs`
    creates the profile through `ProvisionUserProfileCommand`.
  - **Notification**:
    `backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Application/Features/Notifications/IntegrationEvents/AccountRegisteredIntegrationEventHandler.cs`
    sends the `welcome` e-mail; dedup key `welcome:{AccountId}`.

| Field | Type | Notes |
|---|---|---|
| `AccountId` | `Guid` | Auth account id; the User profile id equals it (ADR 0004). |
| `Username` | `string` | |
| `Email` | `string` | |
| `FirstName` | `string` | Auth does not collect names: it sends the username here (`PublishAccountRegisteredIntegrationEventHandler.cs:26`). |
| `LastName` | `string` | Empty when unknown. |
| `Id` | `Guid` | Inherited from `IntegrationEvent`. |
| `OccurredOnUtc` | `DateTime` | Inherited from `IntegrationEvent`. |

- Secret: **no**.
- Partition key: **none**; Kafka falls back to the event `Id`.
- Outbox/inbox: see [Outbox and inbox](#outbox-and-inbox).

## PasswordResetRequestedIntegrationEvent

- Contract: `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/PasswordResetRequestedIntegrationEvent.cs`
- Publishing service: **Auth**.
  `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Application/Features/Auth/Commands/RequestPasswordReset/RequestPasswordResetCommand.cs:51-58`
  publishes it after storing only the token hash. The command always succeeds, whether or not the e-mail exists
  (ADR 0009).
- Consuming service: **Notification**:
  `backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Application/Features/Notifications/IntegrationEvents/PasswordResetRequestedIntegrationEventHandler.cs`
  sends the `password-reset` e-mail; dedup key `password-reset:{event Id}`.

| Field | Type | Notes |
|---|---|---|
| `AccountId` | `Guid` | |
| `Username` | `string` | Used as the display name in the mail. |
| `Email` | `string` | |
| `Token` | `string` | **Secret**: the raw single-use reset code (32 random bytes, URL-safe base64). Only its SHA-256 is stored in `security_tokens`; consumers must not log it. |
| `ExpiresInMinutes` | `int` | Reset links live 60 minutes. |
| `Id` | `Guid` | Inherited from `IntegrationEvent`. |
| `OccurredOnUtc` | `DateTime` | Inherited from `IntegrationEvent`. |

- Secret: **yes**. The raw `Token` travels in the event, so it sits in the Auth outbox row until cleanup and in the
  broker; Notification puts it only into the e-mailed link and does not store the model (ADR 0009,
  `docs/decisions/0009-password-reset-and-email-verification.md`).
- Partition key: **none**; Kafka falls back to the event `Id`.
- Outbox/inbox: see [Outbox and inbox](#outbox-and-inbox).

## EmailVerificationRequestedIntegrationEvent

- Contract: `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/EmailVerificationRequestedIntegrationEvent.cs`
- Publishing service: **Auth**, from two commands:
  - `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Application/Features/Auth/Commands/RequestEmailVerification/RequestEmailVerificationCommand.cs:55-62`
    (signed-in user's own address; does nothing if already verified).
  - `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Application/Features/Auth/Commands/ResendEmailVerification/ResendEmailVerificationCommand.cs:49-56`
    (anonymous resend by address; always succeeds, ADR 0010).
- Consuming service: **Notification**:
  `backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Application/Features/Notifications/IntegrationEvents/EmailVerificationRequestedIntegrationEventHandler.cs`
  sends the `verify-email` e-mail; dedup key `verify-email:{event Id}`.

| Field | Type | Notes |
|---|---|---|
| `AccountId` | `Guid` | |
| `Username` | `string` | Used as the display name in the mail. |
| `Email` | `string` | |
| `Token` | `string` | **Secret**: the raw single-use verification code. Only its SHA-256 is stored; consumers must not log it. |
| `ExpiresInMinutes` | `int` | Verification links live 1440 minutes (24 hours). |
| `Id` | `Guid` | Inherited from `IntegrationEvent`. |
| `OccurredOnUtc` | `DateTime` | Inherited from `IntegrationEvent`. |

- Secret: **yes**, same handling as the password-reset event (ADR 0009).
- Partition key: **none**; Kafka falls back to the event `Id`.
- Outbox/inbox: see [Outbox and inbox](#outbox-and-inbox).

## NotificationRequestedIntegrationEvent

- Contract: `backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Contracts/Events/NotificationRequestedIntegrationEvent.cs`
- Publishing service: **any service** that references `Notification.Contracts`, so it does not need to know about SMTP
  or webhooks (XML doc on the record). There is **no production publisher in this repository yet**: the only callers
  that publish it are the Notification integration tests
  (`backend/c#/tests/OmniCore.Services.Notification.IntegrationTests/NotificationApiTests.cs:158`). Adding real
  publishers is open work (ADR 0006, roadmap AC1).
- Consuming service: **Notification**:
  `backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Application/Features/Notifications/IntegrationEvents/NotificationRequestedIntegrationEventHandler.cs`
  resolves the channel and sends through `SendNotificationCommand`.

| Field | Type | Notes |
|---|---|---|
| `Channel` | `string` | `"Email"` or `"Webhook"`, parsed case-insensitively; an unknown value is logged and dropped (retrying cannot fix it). |
| `Recipient` | `string` | An e-mail address, or the configured webhook endpoint name for `Webhook`. |
| `TemplateKey` | `string` | Fills the `{{placeholders}}` of the named built-in template. |
| `Model` | `IReadOnlyDictionary<string, string>` | Values for the template placeholders. |
| `DeduplicationKey` | `string?` | Optional; when null the handler uses `event:{Id}`. Makes a broker redelivery harmless. |
| `Id` | `Guid` | Inherited from `IntegrationEvent`. |
| `OccurredOnUtc` | `DateTime` | Inherited from `IntegrationEvent`. |

- Secret: **no dedicated secret field**. Whatever a publisher puts into `Model` ends up in the rendered message, so a
  publisher must not put secrets there.
- Partition key: **none**; Kafka falls back to the event `Id`.
- Outbox/inbox: see [Outbox and inbox](#outbox-and-inbox).

## Outbox and inbox

- **Publishing** goes through `IEventBus`: `PublishEndpointEventBus` (RabbitMQ, `IPublishEndpoint.Publish`) or
  `KafkaEventBus` (Kafka `ITopicProducer`), both in
  `backend/c#/OmniCore.Shared/OmniCore.Shared.Infrastructure/EventBus/`.
- **RabbitMQ uses the MassTransit EF Core outbox**: `AddEntityFrameworkOutbox<TDbContext>` + `UseBusOutbox`, switched by
  `MessageBroker__EnableOutbox` (default on), in
  `backend/c#/OmniCore.Shared/OmniCore.Shared.Infrastructure/EventBus/OutboxExtensions.cs:15-29`. The message is written
  with the same database transaction as the business change and delivered later, so a broker outage does not lose it.
  Each service maps the outbox/inbox tables in its `DbContext` with `AddTransactionalOutboxEntities` (`AuthDbContext.cs:35`,
  `UserDbContext.cs:27`, `NotificationDbContext.cs:23`).
- **Kafka does not use the outbox**: `EnableOutbox=true` with `Provider=Kafka` is rejected at startup
  (`backend/c#/OmniCore.Shared/OmniCore.Shared.Infrastructure/Configs/MessageBroker/MessageBrokerConfig.cs:64-71`), and
  the Kafka producer path has no retry/outbox (repo-check I6).
- **Consuming** is one `IntegrationEventConsumer<T>` per event type
  (`backend/c#/OmniCore.Shared/OmniCore.Shared.Infrastructure/EventBus/IntegrationEventConsumer.cs`). It calls every
  `IIntegrationEventHandler<T>` in parallel with `Task.WhenAll` (`:21-22`), so handlers must be idempotent and all of them
  are retried when one fails (repo-check I6). The MassTransit EF inbox tables (`inbox_state`) are what deduplicate a
  redelivered message per consumer; the `DbContext` comments state this (`UserDbContext.cs:26`,
  `NotificationDbContext.cs:22-23`). Notification additionally deduplicates at the application level with
  `DeduplicationKey` (see above).
- **Queue/consumer-group naming**: every service prefixes its queues and Kafka consumer groups with the service name, so
  User and Notification each receive their own copy of `AccountRegisteredIntegrationEvent`
  (`backend/c#/OmniCore.Shared/OmniCore.Shared.Infrastructure/Extensions/MassTransitExtensions.cs:59-61`).
