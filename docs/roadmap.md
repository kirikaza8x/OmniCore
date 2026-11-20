# Roadmap

The coding path, in order. Findings that motivate each phase are in [repo-check.md](repo-check.md) (`F#`). Tick items
off as they land; the status here is also mirrored in `CLAUDE.md`.

## Phase 0: documentation
- [x] README, architecture, repo-check, roadmap, owl-editor integration split and checked against the code (F5 of the old list)

## Phase 1: safety net (F1)
- [x] GitHub Actions `backend.yml`: restore, build, unit tests on every PR and on `develop`/`main`/`docs/**`/`fix/**` pushes (**green 2026-11-20**)
- [x] `tests/` projects in `OmniCore.slnx`: Auth domain unit tests (**compile and pass in CI**)
- [x] One integration test (Testcontainers: Postgres + RabbitMQ): register -> login -> refresh -> reuse rejected (**passes in CI, 2026-11-20**; found the unregistered password hasher; `continue-on-error` removed)
- [x] Both CI jobs pass on GitHub
- [x] Handler tests for register, login and refresh (`tests/OmniCore.Services.Auth.Application.Tests`, 13 tests, NSubstitute); they found A20
- [ ] Make `build-and-unit-test` (and integration) a required check in branch protection (repo setting, owner)

## Phase 2: dev vs prod configuration (F3, F4, F7)
- [x] `docker-compose.prod.yml` override: Production, Swagger off, sensitive logging / detailed errors / any-origin CORS off, `ALLOWED_HOSTS` and `CORS_ALLOWED_ORIGIN` required, data-store ports not published; compose defaults for the three dev switches are now safe (`false`), and `.env.example` turns them on for local dev (**validated with `docker compose config` only, never started**)
- [x] `.env.example` placeholders (`CHANGE_ME`, RabbitMQ user no longer `guest`); empty root `.env.example` removed
- [x] Pin every image tag (**checked by the CI job `infra-config`: every tag exists; Postgres, Redis, RabbitMQ and MinIO also start healthy in CI**). `minio/minio` no longer exists on Docker Hub or Quay, so MinIO is pinned to the third-party rebuild `coollabsio/minio`: owner decision, see repo-check F7
- [ ] Still open from this phase: `appsettings.Development.json.example` (S11), health check with DB/broker probes and migrations not run in every replica (A18), TLS in front of the gateway

## Phase 3: gateway (F2, F6)
- [x] Route the User service and its spec (`/api/v1/users/...` with a 6 MB body limit, `/specs/user.json`); they answer 502 until the User service runs (U1)
- [x] Rate limiting in the gateway (`limit_req`: credential endpoints 10/min burst 5, rest of `/api/` 20/s burst 20, JSON 429); JWTs stay validated by each service: [ADR 0002](decisions/0002-gateway-auth-and-rate-limits.md)
- [x] CI job `infra-config`: compose renders, pinned image tags exist in their registries, `nginx -t` with the real image
- [ ] Still open from this phase: close the published service ports 5001/5002 in the prod override (they bypass the gateway), real client IP behind a TLS proxy (`set_real_ip_from`), per-account lockout (A8)

## Phase 4: auth hardening (F5)
- [x] Asymmetric signing (RS256) with a JWKS endpoint, so any language can verify without minting (ADR 0003 step 4)
- [x] Hashed refresh tokens (A6, backfilling migration) and safe rotation (A7, xmin): [ADR 0003](decisions/0003-auth-token-hardening.md) steps 1-2, **green in CI 2026-11-20**; reasons in [auth-tokens.md](auth-tokens.md)
- [x] Session cap 10 and Quartz purge (A10); RS256 + JWKS with validators fetching the keys (step 3 and 4, **green in CI 2026-11-20**)
- [ ] Still open: migration dropping the plain `token` columns, owner does the HS256 -> RS256 cut-over (`.env.example`), prod override requires the private key
- [x] A11: unique-index race on register answers 409 instead of 500 (built by CI, no test yet)

## Phase 5: contracts (F9)
- [ ] OpenAPI per service; AsyncAPI / JSON schemas for integration events in `shared/contracts/`, versioned

## Phase 6: messaging simplification (F8)
- [x] Compose default is `RabbitMQ` (S1: `Both` failed startup validation)
- [ ] Take Kafka and Kafka UI out of the default stack; keep Kafka for a real streaming need

## Phase 7: code review of the three code projects
- [x] 2a `OmniCore.Shared` reviewed (S1-S13 in repo-check; S1, S4, S5, S7 fixed, not built)
- [x] 2b `OmniCore.Services.Auth` reviewed (A1-A18 in repo-check; A1-A5 fixed, not built; A9 and A15 need an owner decision)
- [x] 2c `OmniCore.Services.User` reviewed (U1-U10 in repo-check; U4 and U10 fixed, not built; U1 and U2 are the real gaps)
Findings go to `repo-check.md`; safe fixes are committed one by one; docs and `CLAUDE.md` updated after each.

## Phase 8: multi-tenancy (F10)
- [x] ADR `docs/decisions/0001-multitenancy.md` written and committed (status: proposed)
- [ ] Owner confirms or amends the ADR (open questions in section 7)
- [ ] Implementation order after confirmation: Shared -> Auth -> User -> gateway
