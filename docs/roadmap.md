# Roadmap and repo-check findings

Written after a read-through of the repo (no build, no tests run - there are no tests yet). Ordered by value.

## Findings
| # | Finding | Why it matters |
|---|---|---|
| 1 | **No automated tests and no CI** (`tests/` was only in the README; no `.github/`) | Nothing catches a broken build or a regression before merge |
| 2 | **User service is not reachable through the gateway**: `nginx.conf` only routes `/api/v1/auth/` and `/specs/auth.json` | The "single entry point" only covers Auth |
| 3 | **Unsafe defaults in `docker-compose.services.yml`**: `EnableSensitiveDataLogging` and `EnableDetailedErrors` default to `true`, `Cors__AllowAnyOrigin` defaults to `true`, `AllowedHosts` to `*` | Fine for local dev, dangerous if the compose file is reused for a server. `.env.example` also ships guessable passwords and a `JWT_SECRET` |
| 4 | **JWT uses one shared symmetric secret** in every service | Any service that can validate tokens can also forge them |
| 5 | **Docs were stale**: README listed folders that do not exist and no `docs/` content | Fixed in this change (README, `architecture.md`) |
| 6 | **Gateway has no rate limiting or auth check** (rate limiting exists only inside each service); `swagger-ui:latest` is unpinned | Abuse protection depends on every service; builds are not reproducible |
| 7 | Both brokers on by default (`Provider=Both`) | Two systems to run and keep consistent; pick one until a real need appears |

## Plan
1. **Safety net first:** a GitHub Actions workflow (restore, build, test) plus the first `tests/` projects: domain unit tests for Auth, and one integration test (Testcontainers: Postgres + RabbitMQ) for register -> login -> refresh.
2. **Dev vs prod config:** a `docker-compose.prod.yml` override (or `ASPNETCORE_ENVIRONMENT`-driven defaults) that turns sensitive logging, detailed errors and open CORS off; `.env.example` gets placeholders (`CHANGE_ME`) instead of working-looking secrets; pin image tags.
3. **Gateway:** add the User route and spec, rate limiting (`limit_req`), and decide whether the gateway or each service validates JWTs.
4. **Auth:** move to asymmetric signing (RS256/ES256) with a JWKS endpoint so other services - in any language - can verify tokens without being able to mint them.
5. **Contracts:** publish OpenAPI per service (already via Swagger) and AsyncAPI/JSON schemas for integration events into `shared/contracts/`, versioned, so non-.NET services can generate clients.
6. **Pick one broker** for the first release (RabbitMQ is enough for service-to-service events), keep Kafka for a real streaming need such as the `StreamProcessor` idea.

## Where `owl_editor` fits (Python service)
`owl_editor` (Otherworld Legends save/hotfix toolkit) now ships an optional HTTP service, `owl_api/` in that repo
(FastAPI, file-in / file-out async jobs: hotfix decrypt/encrypt, PlayerPrefs decrypt/encode; see its `docs/api.md`).
It deliberately uses a **flat Python service layer** (routers -> `JobService` -> storage / toolkit adapter) instead of this
repo's five C# layers; what has to match is the contract:

- **OpenAPI** per service (FastAPI generates it) so the gateway can aggregate specs like it does for Auth.
- **JWT**: today one shared `X-API-Key`; target is OmniCore JWTs verified through a JWKS (roadmap step 4).
- **Events**: `JobFinished` through an `on_finished` hook to RabbitMQ once the contracts in `shared/contracts/` exist.
- **Container + gateway**: Dockerfile and a compose entry under `infra/docker`, a `/api/v1/owl/` route in nginx.
- **Storage**: results move from local disk to MinIO (the bucket already exists); jobs from memory to a database.

Caveats: the toolkit has never been run against the real game, and hosting other people's saves or APKs has privacy and
terms-of-service implications. Decide that before exposing it.
