# Roadmap

The coding path, in order. Findings that motivate each phase are in [repo-check.md](repo-check.md) (`F#`). Tick items
off as they land; the status here is also mirrored in `CLAUDE.md`.

## Phase 0: documentation
- [x] README, architecture, repo-check, roadmap, owl-editor integration split and checked against the code (F5 of the old list)

## Phase 1: safety net (F1)
- [x] GitHub Actions `backend.yml`: restore, build, unit tests on every PR (**written, not run yet**; first hosted run is the first check)
- [x] `tests/` projects in `OmniCore.slnx`: Auth domain unit tests (**written, not compiled or run**)
- [x] One integration test (Testcontainers: Postgres + RabbitMQ): register -> login -> refresh -> reuse rejected (**written, not compiled or run**; the CI job is `continue-on-error` until it passes once)
- [ ] Make both CI jobs pass on GitHub, then make `build-and-unit-test` a required check; add handler tests (login, refresh, register)

## Phase 2: dev vs prod configuration (F3, F4, F7)
- [ ] `docker-compose.prod.yml` override (or environment-driven defaults): sensitive logging, detailed errors, open CORS and `AllowedHosts=*` off
- [ ] `.env.example` placeholders (`CHANGE_ME`); fix or remove the empty root `.env.example`
- [ ] Pin every image tag

## Phase 3: gateway (F2, F6)
- [ ] Route the User service and its spec (`/api/v1/users/...`, `/specs/user.json`)
- [ ] Rate limiting (`limit_req`); decide whether the gateway or each service validates JWTs

## Phase 4: auth hardening (F5)
- [ ] Asymmetric signing (RS256/ES256) with a JWKS endpoint, so any language can verify without minting

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
