# 0006: Observability slice

- Status: code written 2026-11-24 on `develop`, unbuilt (CI result awaited)
- Date: 2026-11-24
- Related: ADR 0011, services-plan item 2

## Steps
1. Shared.Api: `AddObservability(configuration)` (called from `AddApi`), OpenTelemetry packages, enabled only when an OTLP endpoint is configured.
2. Compose: `docker-compose.observability.yml` (profile `observability`: seq, prometheus, grafana), `observability/` config
   (prometheus.yml, Grafana provisioning + one dashboard), `OTEL_*` variables for the three services (default off), `.env.example`.
3. Docs: `docs/observability.md` (how to start, what to look at, the cloud-sim drills), infra README, services-plan, CLAUDE.md.

## Acceptance
- Default `docker compose up` unchanged (no exporter, no new containers). With `--profile observability` and the OTLP variables,
  logs and traces show in Seq (http://localhost:5341), metrics in Prometheus (9090) and the Grafana dashboard (3001).
- No secrets in the compose files; Grafana password from the environment.

## Out of scope
Collector, sampling, alerting rules, database/broker exporters, log-based alerts, dashboards beyond one starter.
