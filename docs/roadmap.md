# Roadmap

The coding path, in order. Findings that motivate each phase are in [repo-check.md](repo-check.md) (`F#`). Tick items
off as they land; the status here is also mirrored in `CLAUDE.md`.

## Phase 0: documentation
- [x] README, architecture, repo-check, roadmap, owl-editor integration split and checked against the code (F5 of the old list)

## Phase 1: safety net (F1)
- [ ] GitHub Actions: restore, build, test on every PR
- [ ] `tests/` projects in `OmniCore.slnx`, starting with Auth domain unit tests
- [ ] One integration test (Testcontainers: Postgres + RabbitMQ): register -> login -> refresh

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
- [ ] Pick one broker for the first release (RabbitMQ is enough); keep Kafka for a real streaming need

## Phase 7: code review of the three code projects
- [ ] 2a `OmniCore.Shared`
- [ ] 2b `OmniCore.Services.Auth`
- [ ] 2c `OmniCore.Services.User`
Findings go to `repo-check.md`; safe fixes are committed one by one; docs and `CLAUDE.md` updated after each.

## Phase 8: multi-tenancy (F10)
- [ ] ADR `docs/decisions/0001-multitenancy.md` committed and confirmed by the owner (design first)
- [ ] Implementation order after confirmation: Shared -> Auth -> User -> gateway
