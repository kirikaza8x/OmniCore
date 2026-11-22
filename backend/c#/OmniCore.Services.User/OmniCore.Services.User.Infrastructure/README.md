# OmniCore.Services.User.Infrastructure

> **Status: empty shell.** The project contains only `InfrastructureAssemblyReference`. There is no `UserDbContext`, no entity mapping, no repository, no migration, no `AddUserInfrastructure()` and no seeder. Finding U1/U2.

## Purpose
Reserved for everything the User service needs from the outside world: its own PostgreSQL schema/database (`USER_DB_NAME` in compose), the `IUserProfileRepository` implementation, EF configurations and migrations, the MassTransit outbox and consumers, and storage for avatars (the Shared MinIO service already exists).

## What is inside
| Type | What it is |
|---|---|
| `InfrastructureAssemblyReference` | assembly handle for scanning |

Package reference: `Microsoft.EntityFrameworkCore.Tools` **10.0.11** (design-time).

## Depends on / used by
- Projects: `User.Application` (not `User.Domain` directly, it arrives through Application), `Shared.Infrastructure`.
- Used by: `User.Api` (reference only).

## What has to be built here (the checklist)
Copy the structure of [Auth.Infrastructure](../../OmniCore.Services.Auth/OmniCore.Services.Auth.Infrastructure/README.md):
1. `Persistence/Contexts/UserDbContext` (schema `user`, `AddTransactionalOutboxEntities`, `ApplyConfigurationsFromAssembly`) and a design-time `UserDbContextFactory`.
2. `Persistence/Configurations/*`: `UserProfileConfiguration` (id = account id, no generated key; value-object converters for `UserId`, `Username`, `EmailAddress`; unique indexes on username and e-mail; owns or links `Settings`, `Addresses`, `TenantProfiles` with the cascade rules), plus `UserAddress`, `UserSettings`, `UserTenantProfile` (tenant filter only after ADR 0001).
3. `Persistence/Repositories/UserProfileRepository : RepositoryBase<UserProfile, UserId>, IUserProfileRepository` (scoped by the Scrutor scan convention). Writes must load **with tracking** (UA1).
4. `DependencyInjection.AddUserInfrastructure(config)`: `AddSharedInfrastructure`, `AddDbContext`, `IUnitOfWork`, `AddMassTransitWithBroker<UserDbContext>`, scan, seeders; `UseUserInfrastructureAsync` for migrations.
5. First migration (`pwsh backend/c#/scripts/shells/add-migration.ps1`), reviewed by hand, committed with the entities.
6. An integration test project with Testcontainers (copy `Auth.IntegrationTests`, including the shared collection fixture for the fixed RabbitMQ port).

## Rules for this layer
Same as Auth.Infrastructure: implement interfaces only, no business rules, entity change and migration in one commit, secrets never in code, `CHANGE_ME` placeholders in examples.

## Known gaps
New ids (`UI1`, `UI2`) are added to `docs/repo-check.md` in the same commit.
- **UI1 (new, medium)** Package version clash: this project references EF Core Tools **10.0.11**, while `Shared.Infrastructure` and the rest of the backend use EF Core **9.0.9** (Npgsql 9.0.4) on .NET 9. Mixing major versions can produce restore warnings or runtime mismatches. Align to 9.0.9 (or move the whole backend together).
- **UI2 (new, low)** The tooling package is set to `PrivateAssets=all` here, but the project has no design-time context factory, so `dotnet ef` could not run even if it had entities.
- **U1 / U2** nothing is registered or persisted; the checklist above is the work.
- **I3** queue and consumer names are not service-scoped; fix before this service consumes `AccountRegisteredIntegrationEvent` (U6).
