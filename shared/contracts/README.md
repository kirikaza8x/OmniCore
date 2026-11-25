# Integration event contracts

Machine-readable [JSON Schema (draft 2020-12)](https://json-schema.org/draft/2020-12/schema) files for the concrete
integration events catalogued in [docs/events.md](../../docs/events.md). The C# records in `*.Contracts` remain the
source of truth; these files describe the wire shape so other languages and tooling can validate a message without
reading the .NET code. Every field name and type here was copied from the record.

## Naming and versioning

- One file per concrete event: `<EventName>.vN.schema.json`. The `$id` is the relative file name.
- The version is part of the file name, not the payload. A **breaking change** (removing or renaming a field, changing
  its type, making an optional field required, or narrowing `additionalProperties`) is published as a **new**
  `<EventName>.v2.schema.json`. The existing `v1` file is never edited.
- Because the schemas set `additionalProperties: false`, adding a field is also a breaking change for a strict
  validator and follows the same rule.
- A property with `"x-secret": true` carries a secret (currently the raw one-time `Token` in the password-reset and
  verification events); it must not be logged or stored.

## Schemas

| Event | Schema file | Contract source |
|---|---|---|
| `AccountRegisteredIntegrationEvent` | [AccountRegisteredIntegrationEvent.v1.schema.json](AccountRegisteredIntegrationEvent.v1.schema.json) | `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/AccountRegisteredIntegrationEvent.cs` |
| `PasswordResetRequestedIntegrationEvent` | [PasswordResetRequestedIntegrationEvent.v1.schema.json](PasswordResetRequestedIntegrationEvent.v1.schema.json) | `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/PasswordResetRequestedIntegrationEvent.cs` |
| `EmailVerificationRequestedIntegrationEvent` | [EmailVerificationRequestedIntegrationEvent.v1.schema.json](EmailVerificationRequestedIntegrationEvent.v1.schema.json) | `backend/c#/OmniCore.Services.Auth/OmniCore.Services.Auth.Contracts/Events/EmailVerificationRequestedIntegrationEvent.cs` |
| `NotificationRequestedIntegrationEvent` | [NotificationRequestedIntegrationEvent.v1.schema.json](NotificationRequestedIntegrationEvent.v1.schema.json) | `backend/c#/OmniCore.Services.Notification/OmniCore.Services.Notification.Contracts/Events/NotificationRequestedIntegrationEvent.cs` |
