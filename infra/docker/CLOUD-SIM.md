# Cloud simulation

A way to run OmniCore on your own machine **the way a small cloud deployment would run it**, to practise what the dev stack hides: one public entry point, private networks, several replicas, resource limits, and things failing. Nothing here is a cloud emulator (it does not imitate AWS APIs); it is the prod override plus a second override and a helper script. Status: renders in CI, **never started** (no Docker daemon in the agent sandbox), so expect a first-run fix or two.

## Run it
```bash
cd infra/docker
cp .env.example .env        # set ALLOWED_HOSTS=localhost and CORS_ALLOWED_ORIGIN=http://localhost:3000, replace CHANGE_ME values
./scripts/cloud-sim.sh up --build
./scripts/cloud-sim.sh smoke      # register -> login -> profile created from the broker event, all through the gateway
./scripts/cloud-sim.sh status
```
Layering: `docker-compose.yml` (dev base) + `docker-compose.prod.yml` (Production settings, data-store ports closed) + `docker-compose.cloud-sim.yml`.

## What it changes
| Cloud idea | In the simulation |
|---|---|
| Load balancer / public edge | Only the gateway (`GATEWAY_PORT`, default 8080) is published for the APIs |
| Private subnet | `backend` network is `internal: true`: no published ports, no route to the internet |
| Horizontal scaling | `SIM_API_REPLICAS` (default 2) replicas of `auth-api` and `user-api`; nginx resolves names per request (`resolver 127.0.0.11`), so requests spread |
| Instance size | CPU and memory limits per container |
| Hardening | `no-new-privileges`, all capabilities dropped for the APIs, log rotation |
| Object storage endpoint | MinIO stays on both networks and published (clients download directly) |

## Failure drills (the useful part)
| Drill | Command | What you should see |
|---|---|---|
| Lose an API instance | `cloud-sim.sh kill auth-api` | requests keep working through the other replica; the container comes back (`restart: always`) |
| Scale up under load | `cloud-sim.sh scale user-api 4` | more containers behind the same gateway |
| Database outage | `cloud-sim.sh outage postgres 30` | 5xx while down, recovery after; EF retry (`Database__MaxRetryCount`) and `/health` behaviour (A18: health does not probe the database today, so it still says healthy: a real finding to fix) |
| Broker outage | `cloud-sim.sh outage rabbitmq 30` | register still works (outbox); the profile appears after the broker returns: proves the outbox and consumer retry |
| Cache outage | `cloud-sim.sh outage redis 30` | see which paths depend on Redis |

## Known limits
- Replicas all run migrations at startup (A18); with two replicas the first one wins and the other waits or retries. Fix is in the roadmap.
- Published MinIO ports and the 5001/5002 ports (dev only) are the "holes" to close in a real deployment; here 5001/5002 are already closed.
- No TLS, no real DNS, no autoscaling, one host. For TLS practice put Caddy or Traefik in front of the gateway (see [services-plan](../../docs/services-plan.md)).
- `read_only` root filesystems are not enabled: the .NET images were not checked for write needs.

## AWS emulator (optional)
`docker-compose.aws.yml` adds a local AWS API on port 4566 (S3, SQS, SNS, SES, Secrets Manager) under a compose profile, so you can practise the AWS SDK/CLI without an account:
```bash
docker compose --profile aws up -d aws-emulator          # Floci: MIT, no account or token
docker compose --profile aws-localstack up -d aws-emulator-localstack   # LocalStack: needs LOCALSTACK_AUTH_TOKEN (free hobby plan)
AWS_ENDPOINT_URL=http://localhost:4566 AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test aws s3 mb s3://demo
```
Ideas that use it: point `Storage__Endpoint` at it instead of MinIO (same S3 API); switch MassTransit to the Amazon SQS/SNS transport as an experiment (not built); keep the JWT private key in Secrets Manager instead of an env var. Nothing in the code depends on it. In the cloud simulation the emulator sits on the private network only. Why Floci first: LocalStack's current image requires an account since 2026-03-23 ([LocalStack announcement](https://blog.localstack.cloud/localstack-single-image-next-steps/)); `localstack/localstack:4.12` is the last tag without it and is the fallback profile. Both image tags were chosen from documentation and are checked for existence by CI.
