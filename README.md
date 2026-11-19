# OmniCore

OmniCore is a modular, service-oriented backend with Docker infrastructure. **Only the C# (.NET 9) backend exists
today**; services in other languages (for example the Python `owl_editor` service) are planned, see
[docs/integrations/owl-editor.md](docs/integrations/owl-editor.md). There is no frontend, no shared contracts folder
and no test project in the repo yet.

**Status:** working prototype, not production-ready. Nothing has been built or run in the last documentation review
(no .NET SDK available), and there are no automated tests. See [docs/repo-check.md](docs/repo-check.md) for the
numbered findings and [docs/roadmap.md](docs/roadmap.md) for the plan.

## Documentation
| Doc | What is in it |
|---|---|
| [docs/architecture.md](docs/architecture.md) | services, layers, request path, messaging, runtime layout (with a diagram) |
| [docs/repo-check.md](docs/repo-check.md) | numbered findings from a repo check, each tagged with the roadmap phase that fixes it |
| [docs/roadmap.md](docs/roadmap.md) | the phased checklist (the coding path) |
| [docs/integrations/owl-editor.md](docs/integrations/owl-editor.md) | how the Python `owl_editor` service would plug in |
| `docs/decisions/` | architecture decision records (ADRs), added when a risky change needs one |

## Repository structure
```
backend/c#/
  OmniCore.slnx                      solution (open in Visual Studio / Rider)
  OmniCore.Shared/                   shared building blocks referenced by every service
    .Domain  .Application  .Infrastructure  .Api  .Contracts
  OmniCore.Services.Auth/            accounts, roles, JWT issuing, Google sign-in
    .Api  .Application  .Domain  .Infrastructure  .Contracts
  OmniCore.Services.User/            user profiles (+ avatar), settings, tenant profiles
    .Api  .Application  .Domain  .Infrastructure  .Contracts
  scripts/shells/                    scaffold_service.ps1, add-migration.ps1, remove_service.ps1, run.ps1
infra/docker/                        docker-compose.{database,infra,services,gateway}.yml, nginx/, scripts/init-dbs.sql
docs/                                documentation (see the table above)
```
Planned, **not in the repo yet** (git does not track empty folders): `frontend/`, `shared/contracts/`, `tests/`, and
language folders for Python / Rust / Go / Java / C++.

## Tech stack
.NET 9 / ASP.NET Core, Carter (endpoints), MediatR, FluentValidation, AutoMapper, Entity Framework Core with
PostgreSQL (Npgsql), MassTransit over RabbitMQ and/or Kafka, Redis (cache), MinIO / AWS S3 SDK (object storage),
Quartz (scheduling), Serilog, nginx (gateway), Swagger UI. `Shared.Infrastructure` also references the
`Microsoft.CodeAnalysis` packages.

## Quick start
Everything runs in Docker (PostgreSQL, RabbitMQ, Kafka, Redis, MinIO, the services, an nginx gateway):
```
cd infra/docker
cp .env.example .env        # change the passwords and JWT_SECRET: the example values are for local use only
docker compose up -d --build
```
The gateway listens on `GATEWAY_PORT` (default 8080); Swagger UI is at `/swagger` while the `dev` profile is on.
Or open `backend/c#/OmniCore.slnx` and run a service's `.Api` project against the infra containers.
Add a service with `backend/c#/scripts/shells/scaffold_service.ps1`.

**Note:** the `.env.example` at the repository root is **empty**. The real one is `infra/docker/.env.example`.
