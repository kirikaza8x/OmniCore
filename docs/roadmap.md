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

## Where `owl_editor` could fit (Python service)
`owl_editor` (Otherworld Legends save/hotfix toolkit, Python) already has clean layers: `core` is pure logic, `services` does I/O,
only `app` prompts. That makes a service wrapper small. Suggested shape, not started:

- **`OmniCore.OwlTools`** under `backend/python/`: FastAPI (OpenAPI out of the box) over `owl.services` / `owl.core`; the menu/CLI stay as they are.
- **Jobs, not requests:** decrypting a hotfix, scanning an APK or encoding a save can take seconds to minutes; expose `POST /jobs`, `GET /jobs/{id}`, and publish `OwlJobCompleted` on the broker.
- **Files in MinIO:** uploads and results go to object storage (the bucket already exists); the service keeps no user data on local disk. Never store game saves or APKs in git.
- **Auth:** accept OmniCore JWTs (via JWKS once step 4 exists); per-user workspaces keyed by user id.
- **Needs first:** the `Workspace` in `owl_editor` takes a root path, so a per-job temp workspace already works; the interactive `Pipeline.prepare/finish` prompts would be replaced by explicit request parameters.
- **Caveats:** `owl_editor` has never been run against the real game; a hosted service that touches game accounts or downloads APKs has terms-of-service and legal exposure the local tool does not. Decide that before building it.
