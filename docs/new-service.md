# Adding a new service

Follow the Auth, User and Notification pattern. The scaffold writes the skeleton; you write the domain.

## 1. Plan first
Copy `docs/plans/README.md` into `docs/plans/NNNN-<service>.md` (entities, endpoints, events in and out). Write an ADR
first if the service adds a dependency, changes a token or schema convention, or touches multi-tenancy (ADR 0001).

## 2. Scaffold
```
python3 backend/c#/scripts/scaffold.py Billing --route invoices --port 5004 --consumes Auth
# or: pwsh backend/c#/scripts/shells/scaffold_service.ps1   (thin wrapper, unrun)
```
Add `--dry-run` first. It creates the five projects (Api, Application, Domain, Infrastructure, Contracts) and three test
projects (Domain, Application, Integration), registers them in `OmniCore.slnx`, adds the database to
`infra/docker/scripts/init-dbs.sql` and `.env.example`, and adds the tests to the CI loops. It never overwrites files.
It prints the compose and nginx snippets that still need a human: add the service to `docker-compose.services.yml`,
the prod override (`ASPNETCORE_ENVIRONMENT`, hosts, CORS), the gateway route and spec in `nginx.conf`.

## 3. Fixed conventions
- DI entry points: `AddXApplication()`, `AddXInfrastructure(config)`, `AddXApi(config)`, `UseXInfrastructureAsync()`.
- Config classes derive from `ConfigBase` (a `SectionName`) and register with `AddConfig<T>()`; settings come from env vars only.
- Own database and schema. EF migrations in the same commit as entity changes (generate in CI, see `docs/ci-cd.md`).
- MassTransit: set `MessageBroker:ServiceName`, so queue names get a per-service prefix (without it two services
  consuming the same event collide on one queue).
- Handlers go through MediatR; `UnitOfWorkBehavior` saves only on success. A handler that must persist on a failure
  result saves explicitly, and remember that save can roll back with the broker inbox transaction if the handler then throws.
- Consumers must be idempotent (dedup key) and must treat transient errors as exceptions (broker retries) and
  permanent ones as logged drops.
- Endpoints: Carter modules under `/api/v1/<route>/`, `RequireAuthorization()` and `RequireRoles(...)` on every one.
  Caller ids come from the token.
- Never log secrets (tokens, webhook URLs, SMTP passwords).

## 4. Tests and docs
Domain unit tests, handler tests with substitutes, and one Testcontainers integration project (runs alone: RabbitMQ
binds port 5672). Add layer READMEs, a row in `docs/layers.md`, `docs/architecture.md`, `README.md`, the roadmap, and
`CLAUDE.md` progress. Push once, then wait for the developer's CI report (`CLAUDE.md`, "Building and CI hand-off").

Worked example: `OmniCore.Services.Notification` (channels abstraction, templates, dedup, delivery log).
