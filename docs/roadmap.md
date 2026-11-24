# Roadmap

The coding path, in order. Findings that motivate each phase are in [repo-check.md](repo-check.md) (`F#`). Tick items
off as they land; the status here is also mirrored in `CLAUDE.md`.

## Phase 0: documentation
- [x] README, architecture, repo-check, roadmap, owl-editor integration split and checked against the code (F5 of the old list)
- [x] Junior-agent docs and config: OpenCode ([ADR 0007](decisions/0007-opencode-junior-agent.md), [opencode.md](opencode.md)) and GitHub Copilot ([ADR 0008](decisions/0008-copilot-junior-agent.md), [copilot.md](copilot.md)); both configured and linted locally, first GitHub runs still unverified

## Phase 1: safety net (F1)
- [x] GitHub Actions `backend.yml`: path-filtered restore, build and selective unit/integration tests (**green 2026-11-20**; scoped by changed area 2026-11-23, see [ci-cd.md](ci-cd.md))
- [x] `tests/` projects in `OmniCore.slnx`: Auth domain unit tests (**compile and pass in CI**)
- [x] One integration test (Testcontainers: Postgres + RabbitMQ): register -> login -> refresh -> reuse rejected (**passes in CI, 2026-11-20**; found the unregistered password hasher; `continue-on-error` removed)
- [x] Both CI jobs pass on GitHub
- [x] Handler tests for register, login and refresh (`tests/OmniCore.Services.Auth.Application.Tests`, 13 tests, NSubstitute); they found A20
- [ ] Make `ci-gate` the required check in branch protection when `backend.yml` runs (repo setting, owner); docs-only PRs do not produce it and need an exemption or an always-on docs check

## Phase 2: dev vs prod configuration (F3, F4, F7)
- [x] `docker-compose.prod.yml` override: Production, Swagger off, sensitive logging / detailed errors / any-origin CORS off, `ALLOWED_HOSTS` and `CORS_ALLOWED_ORIGIN` required, data-store ports not published; compose defaults for the three dev switches are now safe (`false`), and `.env.example` turns them on for local dev (**validated with `docker compose config` only, never started**)
- [x] `.env.example` placeholders (`CHANGE_ME`, RabbitMQ user no longer `guest`); empty root `.env.example` removed
- [x] Pin every image tag (**checked by the CI job `infra-config`: every tag exists; Postgres, Redis, RabbitMQ and MinIO also start healthy in CI**). `minio/minio` no longer exists on Docker Hub or Quay, so MinIO is pinned to the third-party rebuild `coollabsio/minio`: owner decision, see repo-check F7
- [ ] Still open from this phase: `appsettings.Development.json.example` (S11), health check with DB/broker probes and migrations not run in every replica (A18), TLS in front of the gateway

## Phase 3: gateway (F2, F6)
- [x] Route the User service and its spec (`/api/v1/users/...` with a 6 MB body limit, `/specs/user.json`); U1 fixed, the service has endpoints (full compose stack still not smoke-tested, UP1)
- [x] Rate limiting in the gateway (`limit_req`: credential endpoints 10/min burst 5, rest of `/api/` 20/s burst 20, JSON 429); JWTs stay validated by each service: [ADR 0002](decisions/0002-gateway-auth-and-rate-limits.md)
- [x] CI job `infra-config` (runs only for infra or workflow changes): compose renders, pinned image tags exist in their registries, `nginx -t` with the real image
- [ ] Still open from this phase: close the published service ports 5001/5002 in the prod override (they bypass the gateway), real client IP behind a TLS proxy (`set_real_ip_from`), per-account lockout (A8)

## Phase 4: auth hardening (F5)
- [x] Asymmetric signing (RS256) with a JWKS endpoint, so any language can verify without minting (ADR 0003 step 4)
- [x] Hashed refresh tokens (A6, backfilling migration) and safe rotation (A7, xmin): [ADR 0003](decisions/0003-auth-token-hardening.md) steps 1-2, **green in CI 2026-11-20**; reasons in [auth-tokens.md](auth-tokens.md)
- [x] Session cap 10 and Quartz purge (A10); RS256 + JWKS with validators fetching the keys (step 3 and 4, **green in CI 2026-11-20**)
- [x] 2026-11-23: prod override requires `JWT_PRIVATE_KEY_PEM`; CI prod/cloud-sim render updated with a placeholder key; `docs/auth-tokens.md` status corrected
- [ ] Still open: migration dropping the plain `token` columns, owner does the HS256 -> RS256 cut-over (`.env.example`), real RS256 compose-stack start
- [x] A11: unique-index race on register answers 409 instead of 500 (built by CI, no test yet)

## Phase 5: contracts (F9)
- [ ] OpenAPI per service; AsyncAPI / JSON schemas for integration events in `shared/contracts/`, versioned

## Phase 6: messaging simplification (F8)
- [x] Compose default is `RabbitMQ` (S1: `Both` failed startup validation)
- [ ] Take Kafka and Kafka UI out of the default stack; keep Kafka for a real streaming need

## Phase 7: code review of the three code projects
- [x] 2a `OmniCore.Shared` reviewed (S1-S13 in repo-check; S1, S4, S5, S7 fixed, not built)
- [x] 2b `OmniCore.Services.Auth` reviewed (A1-A18 in repo-check; A1-A5 fixed, not built; A9 and A15 need an owner decision)
- [x] 2c `OmniCore.Services.User` reviewed (U1-U10 in repo-check)
Findings go to `repo-check.md`; safe fixes are committed one by one; docs and `CLAUDE.md` updated after each.

## Phase 8: multi-tenancy (F10)
- [x] ADR `docs/decisions/0001-multitenancy.md` written and committed (status: proposed)
- [ ] Owner confirms or amends the ADR (open questions in section 7)
- [ ] Implementation order after confirmation: Shared -> Auth -> User -> gateway

## Phase 9: User service (ADR 0004)
- [x] Rebuilt: aggregate rules, 10 endpoints, Auth-event consumer, CI-generated migration, 53 tests, **green in CI 2026-11-22**
- [ ] Start `user-api` in the real compose stack and add it to the smoke check
- [ ] Profile deletion/deactivation (needs the Auth lifecycle events, Phase 10)

## Phase 10: entity fixes (order from [entities.md](entities.md))
- [ ] AD2 events from `Deactivate`/`SoftDelete`/`AttachEmail`, AD7 username rules (case-insensitive, reserved names)
- [ ] `IsSystem` roles + first admin account (AD1, A15), migration
- [ ] Remove `UserSession`; device columns on `RefreshToken` (AI2)
- [ ] Write `SecurityAuditLog` (login, password change, role change, token reuse)
- [ ] `AccountDeactivated` / `AccountDeleted` / `AccountEmailChanged` in `Auth.Contracts` + User consumer (UD2)
- [x] E-mail verification and password reset on `SecurityToken` (AA4, ADR 0009, plan 0004): 4 Auth endpoints, 2 events, Notification mails and links; **written 2026-11-24, unbuilt, CI result awaited**. Not done: enforce verified e-mail at login

## Phase 11: shared-layer and auth fixes from the master READMEs
Order: W1+S2 (real client IP / rate limit), I1, I3 (service-scoped queue names), AP1/AP2/S6, D1, AD1, AA1 (fallback authorization policy), A15, AN1, AI1.

## Phase 12: cloud simulation and observability
- [x] [Cloud simulation override](../infra/docker/CLOUD-SIM.md): prod-like local run (one public port, isolated networks, replicas, limits, chaos helper). Renders in CI; **not started yet**
- [ ] First observability slice (logs, metrics, traces): see [services-plan.md](services-plan.md)

## Phase 13: new services
Candidates, effort and order: [services-plan.md](services-plan.md). Each new service starts from `scaffold_service.ps1`, an ADR and an integration test.

## Phase 14: web client (ADR 0005)
- [x] `frontend/web`: Next.js, BFF cookies, sign in / register / profile, Dockerfile, compose profile `web`, CI workflow
- [ ] Run it against the real stack (compose `--profile web`), then add Playwright smoke tests
- [ ] Apply the theme setting, edit/promote addresses, show other users' public profiles
- [ ] Password reset and e-mail verification screens: pages `/forgot-password`, `/reset-password?token=`, `/verify-email?token=` and BFF routes (Auth endpoints exist, ADR 0009)
- [ ] Admin screens (roles, accounts) once the admin endpoints are settled

## Phase 15: Notification service (2026-11-23)
- [x] Delivery through e-mail and named webhooks, built-in `welcome`/`test` templates, admin delivery log, initial EF migration, dedup claim before send (NT1/NT2). Junior reported local application and integration tests green; CI result awaited
- [x] Documentation: service and five layer READMEs, [ADR 0006](decisions/0006-notification-channels.md), layer index and architecture update
- [ ] Add production publishers for confirmation and reset mails (AC1, after Phase 10 AA4)
- [ ] Start `notification-api` in the real compose stack and add it to the smoke check
- [ ] Pin Dockerfile base images by digest (IF5)
