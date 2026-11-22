# infra/docker

Compose files for OmniCore. Settings come from `.env` (copy `.env.example`, replace every `CHANGE_ME`).

## Files
| File | What it holds |
|---|---|
| `docker-compose.yml` | master: includes the parts below |
| `docker-compose.database.yml` | PostgreSQL; `scripts/init-dbs.sql` creates one database per service |
| `docker-compose.infra.yml` | RabbitMQ, Redis, MinIO, Kafka, Kafka UI |
| `docker-compose.services.yml` | `auth-api`, `user-api`. Shared settings are defined once (`x-service-env`, `x-service-base`); a service lists only its differences |
| `docker-compose.gateway.yml` | nginx gateway (`nginx/nginx.conf`) and Swagger UI (profile `dev`/`swagger`) |
| `docker-compose.web.yml` | the Next.js web client `web` (profile `web`; source in `frontend/web`) |
| `docker-compose.aws.yml` | optional AWS emulator (profile `aws` = Floci, `aws-localstack` = LocalStack) |
| `docker-compose.prod.yml` | override: Production, Swagger off, requires `ALLOWED_HOSTS` and `CORS_ALLOWED_ORIGIN`, data-store ports closed |
| `docker-compose.cloud-sim.yml` | override: simulated cloud, see [CLOUD-SIM.md](CLOUD-SIM.md) |

## Commands
```bash
cp .env.example .env
docker compose up -d --build                                   # local development
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build   # server
./scripts/cloud-sim.sh up --build                              # simulated cloud (prod + cloud-sim)
docker compose --profile web up -d --build web                 # web client on :3000 (needs the gateway and services)
docker compose --profile aws up -d aws-emulator                # local AWS API on :4566
docker compose config --quiet                                  # check that everything renders
```

## Adding a service
1. Copy the `user-api` block in `docker-compose.services.yml`; change build path, container name, port, database name and Redis instance name.
2. Add its database to `scripts/init-dbs.sql` (and `*_DB_NAME` to `.env.example`).
3. Add a route (and a spec route) in `nginx/nginx.conf`, and its spec to `SWAGGER_URLS`.
4. If the prod or cloud-sim override should treat it specially, add it there too.

CI (`infra-config` job) renders dev, prod and cloud-sim, checks that every pinned image tag exists, runs `nginx -t` and starts the data stores.
