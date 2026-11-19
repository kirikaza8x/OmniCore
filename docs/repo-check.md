# Repo check: findings

Written from a read-through of the repo and verified against the code. **Nothing was built or run** (no .NET SDK in the
review environment), and there are no tests. Each finding names the [roadmap](roadmap.md) phase that fixes it.
Findings from the code review of each service are appended below as they are done.

| # | Finding | Why it matters | Phase |
|---|---|---|---|
| 1 | **No automated tests and no CI**: no `tests/`, no `.github/`, and `OmniCore.slnx` has no test project | A broken build or a regression is only found by hand | 1 |
| 2 | **User service is not reachable through the gateway**: `nginx.conf` routes only `/api/v1/auth/` and `/specs/auth.json`, although `SWAGGER_URLS` already lists `/specs/user.json` | The "single entry point" covers only Auth, and the Swagger UI entry for User is dead | 3 |
| 3 | **Unsafe compose defaults** in `docker-compose.services.yml`: `Database__EnableSensitiveDataLogging` and `EnableDetailedErrors` default to `true`, `Cors__AllowAnyOrigin` to `true`, `AllowedHosts` to `*` | Fine for local dev, dangerous if the file is reused for a server | 2 |
| 4 | **Guessable example secrets** in `infra/docker/.env.example` (`POSTGRES_PASSWORD`, `guest/guest`, `minioadmin`, a readable `JWT_SECRET`); the **root `.env.example` is empty** | Copying the example gives a working but insecure setup; the empty root file misleads | 2 |
| 5 | **One shared symmetric JWT secret** (HS256) in every service (`JwtTokenService`, `JwtAuthenticationExtensions`) | Any service that can validate a token can also forge one; non-.NET services would need the secret too | 4 |
| 6 | **Gateway has no rate limiting and no auth check**; rate limiting exists only inside the services (`RateLimiterExtensions`) | Abuse protection depends on every service being configured | 3 |
| 7 | **Unpinned images**: `swaggerapi/swagger-ui:latest`, `provectuslabs/kafka-ui:latest`, `apache/kafka:latest`, `minio/minio:latest` | Builds are not reproducible; a `latest` bump can break the stack | 2 |
| 8 | **Both brokers on by default in compose** (`MessageBroker__Provider=Both`) while the code default is `RabbitMQ`, and the outbox is documented as RabbitMQ-only | Two systems to run and keep consistent; unclear which events are outbox-protected | 6 |
| 9 | No published contracts for non-.NET consumers (no OpenAPI export, no event schemas) | Other-language services cannot generate clients | 5 |
| 10 | Single auth + single user model: `UserTenantProfile` exists in User, `Tenant` exists as a base type in `Shared.Domain`, but there is no tenant concept in Auth, tokens or queries | Blocks several services sharing one identity across tenants | 8 |
