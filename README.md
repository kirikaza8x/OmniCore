# OmniCore

OmniCore is a multi-language, modular platform repository built around a shared backend architecture and supporting infrastructure. It includes a C# backend solution, data and messaging services across multiple languages, frontend placeholders, infrastructure definitions, shared contracts and models, and test scaffolding.

## Repository structure

```
backend/c#/
  OmniCore.slnx                      solution (open this in Visual Studio / Rider)
  OmniCore.Shared/                   shared building blocks, referenced by every service
    .Domain  .Application  .Infrastructure  .Api  .Contracts
  OmniCore.Services.Auth/            accounts, roles, JWT issuing, Google sign-in   (Api / Application / Domain / Infrastructure / Contracts)
  OmniCore.Services.User/            user profiles, settings, tenant profiles       (same five layers)
  scripts/shells/                    scaffold_service.ps1, add-migration.ps1, remove_service.ps1, run.ps1
infra/docker/                        docker-compose.{database,infra,services,gateway}.yml, nginx/, scripts/init-dbs.sql
```

Planned but **not in the repo yet** (git does not track empty folders): `frontend/`, `shared/`, `tests/`, language
folders for Python / Rust / Go / Java / C++. See [docs/roadmap.md](docs/roadmap.md).

More detail: [docs/architecture.md](docs/architecture.md) (layers, services, gateway, messaging) and
[docs/roadmap.md](docs/roadmap.md) (findings from a repo check, plan, and how other-language services such as
`owl_editor` would plug in).

## Key technologies

- .NET 9.0
- ASP.NET Core
- Carter
- Entity Framework Core
- MassTransit with RabbitMQ
- PostgreSQL via Npgsql
- Redis caching
- AWS S3 SDK
- Quartz scheduling
- AutoMapper
- MediatR
- FluentValidation
- Serilog
- Microsoft.CodeAnalysis workspace packages (for code analysis scenarios)

## Backend solution

The main C# solution is located at `backend/c#/OmniCore.slnx` and includes the following projects:

- `OmniCore.Shared.Api` - API and web endpoints
- `OmniCore.Shared.Application` - application logic, CQRS behaviors, and validation
- `OmniCore.Shared.Domain` - domain models and abstractions
- `OmniCore.Shared.Infrastructure` - persistence, messaging, integration, and service wiring

## Getting started

Everything runs in Docker (PostgreSQL, RabbitMQ, Kafka, Redis, MinIO, the services and an nginx gateway):

```
cd infra/docker
cp .env.example .env        # then change the passwords and JWT_SECRET - the example values are for local use only
docker compose up -d --build
```

The gateway listens on `GATEWAY_PORT` (default 8080); Swagger UI is at `/swagger` when the `dev` profile is on.
Or open `backend/c#/OmniCore.slnx` in Visual Studio / Rider and run a service's `.Api` project against the infra
containers. Add a service with `backend/c#/scripts/shells/scaffold_service.ps1`.
