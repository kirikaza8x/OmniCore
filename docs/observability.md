# Observability (ADR 0011, plan 0006)

Logs, traces and metrics leave the services over OpenTelemetry (OTLP) and are only sent when you configure an endpoint.
Code: `OmniCore.Shared.Api/ObservabilityExtensions.cs` (called from `AddApi`). **CI green 2026-11-24 (developer reported); the stack itself has never been started.**

## Start it
```
cd infra/docker
# in .env: uncomment the three OTEL_* lines, set GRAFANA_ADMIN_PASSWORD
docker compose --profile observability up -d
docker compose up -d --build        # restart services so they pick up the endpoints
```
| What | Where |
|---|---|
| Logs + traces (Seq) | http://localhost:5341 (no login: local only) |
| Metrics (Prometheus) | http://localhost:9090 |
| Dashboard (Grafana) | http://localhost:3001, user `admin`, password from `GRAFANA_ADMIN_PASSWORD`; dashboard "OmniCore services" |

## What is recorded
HTTP server/client spans, ASP.NET Core + runtime metrics, `ILogger` output. `/health` is filtered. No SQL text, bodies or headers
(reset codes and passwords travel in POST bodies). Keep not logging secrets or e-mail addresses.

## Try it
Hit Swagger/web, then in Seq filter `@Resource.service.name = 'omnicore-auth'`; in Grafana watch requests and p95 per service.
Cloud-sim drills (`infra/docker/CLOUD-SIM.md`): kill a replica and watch the request graph.

## Not covered (yet)
Collector, sampling, alerting, Postgres/RabbitMQ/Redis exporters, gateway metrics (repo-check A18 stays open).
Metric names in the dashboard assume the .NET 9 built-in `http.server.request.duration` converted by Prometheus; check them in
Prometheus if a panel is empty.
