# OmniCore

OmniCore is a modular, service-oriented backend with Docker infrastructure and a Next.js web client
(`frontend/web`, see its README). **Only the C# (.NET 9) backend exists today**; services in other languages (for example the Python `owl_editor` service) are planned, see
[docs/integrations/owl-editor.md](docs/integrations/owl-editor.md).

**Status:** working prototype, not production-ready. All three backend services (Auth, User, Notification) build in CI and have automated unit and integration tests; integration tests use Postgres and RabbitMQ testcontainers. The full compose stack has not been smoke-tested with real service containers. See [docs/repo-check.md](docs/repo-check.md) for the numbered findings and [docs/roadmap.md](docs/roadmap.md) for the plan.

## Documentation
| Doc | What is in it |
|---|---|
| [docs/architecture.md](docs/architecture.md) | services, layers, request path, messaging, runtime layout (with a diagram) |
| [docs/repo-check.md](docs/repo-check.md) | numbered findings from a repo check, each tagged with the roadmap phase that fixes it |
| [docs/roadmap.md](docs/roadmap.md) | the phased checklist (the coding path) |
| [docs/ci-cd.md](docs/ci-cd.md) | workflows, jobs, and the agent push-and-hand-off rule (no CI polling) |
| [docs/integrations/owl-editor.md](docs/integrations/owl-editor.md) | how the Python `owl_editor` service would plug in |
| `docs/decisions/` | architecture decision records (ADRs), added when a risky change needs one |

## Repository structure
```
backend/c#/
  OmniCore.slnx                      solution (open in Visual Studio / Rider)
  OmniCore.Shared/                   shared building blocks referenced by every service
    .Domain  .Application  .Infrastructure  .Api  .Contracts
  OmniCore.Services.Auth/            accounts, roles, JWT issuing
    .Api  .Application  .Domain  .Infrastructure  .Contracts
  OmniCore.Services.User/            user profiles (+ avatar), settings, tenant profiles
    .Api  .Application  .Domain  .Infrastructure  .Contracts
  OmniCore.Services.Notification/    e-mail delivery, templates, retries, deduplication
    .Api  .Application  .Domain  .Infrastructure  .Contracts
  scripts/shells/                    scaffold_service.ps1, add-migration.ps1, remove_service.ps1, run.ps1
infra/docker/                        docker-compose.{database,infra,services,gateway}.yml, nginx/, scripts/init-dbs.sql
docs/                                documentation (see the table above)
```
Planned, **not in the repo yet** (git does not track empty folders): language folders for
Python / Rust / Go / Java / C++. Shared contracts live in `backend/c#/OmniCore.Shared/OmniCore.Shared.Contracts`.

## Tech stack
.NET 9 / ASP.NET Core, Carter (endpoints), MediatR, FluentValidation, AutoMapper, Entity Framework Core with
PostgreSQL (Npgsql), MassTransit over RabbitMQ and/or Kafka, Redis (cache), MinIO / AWS S3 SDK (object storage),
Quartz (scheduling), Serilog, nginx (gateway), Swagger UI. `Shared.Infrastructure` also references the
`Microsoft.CodeAnalysis` packages.

## Quick start
Everything runs in Docker (PostgreSQL, RabbitMQ, Kafka, Redis, MinIO, the services, an nginx gateway):
```
cd infra/docker
cp .env.example .env        # replace every CHANGE_ME (passwords, JWT_SECRET): the example values are placeholders
docker compose up -d --build
```
On a server, add the production override (it needs `ALLOWED_HOSTS` and `CORS_ALLOWED_ORIGIN` in `.env`):
```
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```
It forces Production, turns Swagger, sensitive logging, detailed DB errors and any-origin CORS off, and stops publishing
the data stores on the host. TLS is not handled here; put a TLS-terminating proxy in front of the gateway.
The gateway listens on `GATEWAY_PORT` (default 8080); Swagger UI is at `/swagger` while the `dev` profile is on.
Or open `backend/c#/OmniCore.slnx` and run a service's `.Api` project against the infra containers. Note that
`appsettings*.json` are git-ignored and not in the repo: settings come from environment variables (the keys are in
`infra/docker/docker-compose.services.yml` (see `infra/docker/README.md`)), so a `.Api` project run outside Docker needs your own
`appsettings.Development.json` or those variables set.
Add a service with `backend/c#/scripts/shells/scaffold_service.ps1`.

**Note:** the only `.env.example` is `infra/docker/.env.example` (the empty one at the repository root was removed).
