# CLAUDE.md - orientation for Claude / agent sessions in this repo

**Read first:** [README.md](README.md), [docs/architecture.md](docs/architecture.md), [docs/repo-check.md](docs/repo-check.md)
(numbered findings), [docs/roadmap.md](docs/roadmap.md) (the plan), [docs/decisions/](docs/decisions/) (ADRs).

## What this is
**OmniCore** is a .NET 9 service-oriented backend (`backend/c#/`) plus Docker infrastructure (`infra/docker/`). Only the
C# backend exists today; other languages are planned, not in the repo.

| Project | Responsibility |
|---|---|
| `OmniCore.Services.Auth` | Accounts (register, login, logout, refresh-token rotation, current user), roles, JWT issuing. Own database. A Google token validator exists but no endpoint uses it. |
| `OmniCore.Services.User` | **Design only, not running** (no endpoints, DbContext, repository or migrations): profiles, settings, addresses, tenant profiles. See U1/U2 in repo-check. |
| `OmniCore.Shared` | Shared library: Domain (DDD base types, `Result`, `Error`), Application (MediatR behaviours), Infrastructure (EF, MassTransit + outbox, Redis, MinIO, config), Api (Carter, JWT bearer, CORS, rate limiting, Swagger), Contracts (integration events). |
| nginx gateway | `infra/docker/` : routes `/api/v1/auth/` only today. |

Each service has layers `Api / Application / Domain / Infrastructure / Contracts`. Stack: Carter, MediatR,
FluentValidation, EF Core + PostgreSQL (snake_case), MassTransit (RabbitMQ; Kafka also supported), Redis, MinIO, Quartz, Serilog.

## Repo map
- Solution: `backend/c#/OmniCore.slnx` (services, Shared, `tests/`)
- Tests: `backend/c#/tests/` (`...Auth.Domain.Tests`, `...Auth.IntegrationTests` with Testcontainers)
- CI: `.github/workflows/backend.yml`
- Compose + gateway: `infra/docker/` (`docker-compose*.yml`, `nginx.conf`, `scripts/init-dbs.sql`, **`.env.example` here**)
- Developer scripts (PowerShell): `backend/c#/scripts/shells/` (`scaffold_service.ps1`, `add-migration.ps1`, `run.ps1`, `remove_service.ps1`)
- Settings come **only from environment variables** (compose). `appsettings*.json` are git-ignored and absent.
  The only `.env.example` is `infra/docker/.env.example` (placeholders are `CHANGE_ME`). Server runs add
  `-f docker-compose.prod.yml` (needs `ALLOWED_HOSTS` and `CORS_ALLOWED_ORIGIN`).

## Commands (the sandbox used for agent sessions has no NuGet access, so .NET is built by GitHub Actions: see "Building" below)
```
dotnet restore backend/c#/OmniCore.slnx                      # verified by CI 2026-11-20
dotnet build   backend/c#/OmniCore.slnx -c Release           # verified by CI 2026-11-20
dotnet test    backend/c#/tests/OmniCore.Services.Auth.Domain.Tests          # verified by CI 2026-11-20 (unit)
dotnet test    backend/c#/tests/OmniCore.Services.Auth.IntegrationTests      # verified by CI 2026-11-20, needs Docker (Testcontainers)
cd infra/docker && cp .env.example .env && docker compose up -d --build      # UNVERIFIED (never started)
cd infra/docker && docker compose --env-file .env.example config             # verified 2026-11-20 (dev and prod override render)
pwsh backend/c#/scripts/shells/add-migration.ps1                             # new EF migration (UNVERIFIED)
pwsh backend/c#/scripts/shells/scaffold_service.ps1                          # new service (UNVERIFIED)
```
Do **not** state that the build or tests pass until a run (local or the GitHub Actions run) has shown it.

## Building (agent sessions)
No .NET SDK or NuGet access in the agent sandbox (the egress policy answers 403 for nuget.org and dot.net; do not route
around it). Use GitHub Actions as the build machine: push the branch (`backend.yml` also runs on `docs/**` and `fix/**`),
then read the result without a token from the public API: `GET https://api.github.com/repos/kirikaza8x/OmniCore/actions/runs?head_sha=<sha>`,
then `/actions/runs/<id>/jobs` and `/check-runs/<job id>/annotations`. Compiler errors and the integration-test failure
text arrive as annotations (job summaries and raw logs are **not** readable without a token). A push-and-poll cycle
takes about a minute.

## Conventions
- Layers point inward: Api -> Application -> Domain; Infrastructure implements Domain/Application interfaces.
  Domain must not reference EF or infrastructure. `Shared` holds only generic code, not service logic.
- Commands/queries go through MediatR; `UnitOfWorkBehavior` saves **only when the result is a success**. A handler that
  must persist a change on a failure result has to save explicitly (see the refresh-token reuse fix, A1).
- Endpoints are Carter modules; routes are `/api/v1/<service>/...` built by a `BaseRouteModule`. Protect them with
  `RequireAuthorization()` and, for admin things, `RequireRoles(...)`; check every new endpoint.
- Ids that identify the caller or the tenant come from the token, never from the request body (U3).
- Keep EF migrations in sync with entity changes in the same commit.
- Commits: small and reviewable, `docs:` / `fix(auth):` / `refactor(shared):` / `ci:` / `test:`. Write an ADR in
  `docs/decisions/NNNN-title.md` **before** a risky change (rename, schema or token-format change, removing a layer).
  Update the docs and the progress section below after each piece of work.

## Do not
- Do not claim tests or builds pass without running them. Mark code written without a build as unbuilt.
- Do not commit secrets. Example values must be obviously fake (`CHANGE_ME`).
- Do not start implementing multi-tenancy before the owner confirms ADR 0001.
- Do not re-enable `Provider=Both` for the message broker (it is rejected at startup); do not register `CachingBehavior`
  without reviewing cache invalidation (S6).

## Known issues (full list in docs/repo-check.md)
`AllowedHosts` still defaults to `*` in the base compose file (the prod override requires it) (F3);
gateway routes only auth and rate limiting collapses behind it (S2, S3); refresh tokens stored in plain text (A6);
no brute-force protection (A8); no first admin account (A15); User service not running (U1); the Auth and Shared fixes
below now compile and pass CI, but only the register/login/refresh/reuse path is exercised by a test.

## Current progress
Reflects branch `docs/readme-and-roadmap`, 2026-11-20, last code/doc commit before this section: see `git log -1`.

- [x] Phase 0 docs refactor (README, architecture, repo-check, roadmap, integrations/owl-editor, decisions)
- [x] Review 2a Shared, 2b Auth, 2c User (findings S1-S13, A1-A18, U1-U10)
- [x] Safe fixes committed, **built by CI 2026-11-20** (first CI run also found and fixed a validator syntax error, missing xunit usings and an unregistered password hasher): broker default, JWT algorithm pin + startup validation, LogHub auth, dead rate-limit code,
      refresh-reuse persistence, admin-only UpdateRole / GetAccountById, no password hash in Redis, input limits, avatar order
- [x] Phase 1: `backend.yml`, Auth domain unit tests and the Testcontainers integration test are **green on GitHub** (2026-11-20). Not yet a required check; no handler tests yet
- [x] ADR 0001 multi-tenancy written (**proposed, waiting for the owner**)
- [x] Phase 2 infra, 2026-11-20: `docker-compose.prod.yml`, safe compose defaults, `CHANGE_ME` `.env.example`, root `.env.example`
      removed, all images pinned. Only checked with `docker compose config`; **never started, image tags unverified**
      (the sandbox had Docker CLI but no daemon, no Docker Hub and no NuGet, so no .NET build either)
- [ ] Next: make `build-and-unit-test` a required check and add handler tests (login, refresh, register), then phase 3 (gateway: route User, `limit_req`), phase 4 (asymmetric JWT + JWKS, A6/A7)
- [ ] Owner decisions pending: ADR 0001 questions, A9 (generic login error), A15 (first admin), keep or drop the User service in compose
