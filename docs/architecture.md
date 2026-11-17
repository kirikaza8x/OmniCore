# Architecture

What exists today (checked against the repo, not built or run - no .NET SDK was available for the check).

## Services
| Service | Does | Layers |
|---|---|---|
| `OmniCore.Services.Auth` | accounts (register, login, logout, refresh token, current user), roles, JWT issuing, Google sign-in | Api / Application / Domain / Infrastructure / Contracts |
| `OmniCore.Services.User` | user profiles (+ avatar upload), settings, tenant profiles | same five layers |
| `OmniCore.Shared` | domain abstractions (DDD base types, value objects, repository + query contracts), MediatR behaviours, EF Core / MassTransit / Redis / S3 wiring, integration-event contracts | Domain / Application / Infrastructure / Api / Contracts |

Each service owns its own PostgreSQL database (`AUTH_DB_NAME`, `USER_DB_NAME`, created by `infra/docker/scripts/init-dbs.sql`).
`scripts/shells/scaffold_service.ps1` generates a new service with the same five layers and links `OmniCore.Shared`.

## Request path
client -> nginx gateway (`infra/docker/nginx/nginx.conf`, prefix routing, one block per service) -> service `.Api` (Carter endpoints)
-> MediatR command/query -> domain -> EF Core. Tokens are issued by Auth; other services validate them with the same
`Jwt__Secret` / issuer / audience (shared symmetric secret, see roadmap).

## Messaging
Integration events (`Shared.Contracts/Events`) go through MassTransit to RabbitMQ and/or Kafka (`MessageBroker__Provider`,
default `Both`) with an EF outbox (`MessageBroker__EnableOutbox`). Redis is the distributed cache; MinIO is object storage.

## Runtime layout (`infra/docker`)
`docker-compose.yml` includes `database`, `infra` (RabbitMQ, Redis, MinIO, Kafka + UI), `services` (auth-api, user-api) and
`gateway` (nginx + Swagger UI). Services have `/health` and compose health checks; the images run as a non-root user.
Swagger specs are aggregated through the gateway (`/specs/<name>.json`, `SWAGGER_URLS`).
