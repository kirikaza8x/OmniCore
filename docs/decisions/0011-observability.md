# ADR 0011: Observability with OpenTelemetry, Seq, Prometheus and Grafana

- Status: **accepted by delegation** (owner said "continue with the plan"; `docs/services-plan.md` item 2)
- Date: 2026-11-24
- Scope: `OmniCore.Shared.Api`, `infra/docker` (new opt-in profile), docs
- Related: plan `docs/plans/0006-observability.md`, repo-check A18

## Context
Four moving parts (gateway, three services, broker) and no way to see what they do: logs go to the console only, there are no
metrics and no traces. The cloud simulation drills (`CLOUD-SIM.md`) teach little without that. The earlier docs mention Serilog,
but no service configures it.

## Decision
1. **One standard, no vendor SDK in the services.** The services use OpenTelemetry (logs, traces, metrics) and export over OTLP
   (HTTP/protobuf). Packages: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`,
   `OpenTelemetry.Instrumentation.AspNetCore`, `.Http`, `.Runtime`. Serilog is not added.
2. **Off unless configured.** Nothing is exported unless an `OTEL_EXPORTER_OTLP_*ENDPOINT` variable is set, so tests, local `dotnet run`
   and production without the stack behave exactly as before. The service name comes from `OTEL_SERVICE_NAME` (set per service in compose).
3. **Backends as an opt-in compose profile `observability`** (`docker-compose.observability.yml`): **Seq** receives logs and traces
   (OTLP ingest), **Prometheus** receives metrics through its native OTLP receiver, **Grafana** reads Prometheus (provisioned
   data source and one small dashboard). No collector: fewer moving parts to learn first; add one when fan-out or sampling is needed.
4. **What is and is not recorded.** HTTP server and client spans, ASP.NET Core and runtime metrics, `ILogger` output with scopes.
   `/health` is filtered out. No SQL text (no EF instrumentation), no request or response bodies, no headers, so passwords, tokens and
   reset codes (which travel in POST bodies) are not captured. Log statements must keep following the rule of never logging secrets
   or e-mail addresses.
5. **Local only by default.** Seq, Prometheus and Grafana publish on `127.0.0.1`, Grafana needs `GRAFANA_ADMIN_PASSWORD`
   (`CHANGE_ME` in `.env.example`; the profile refuses to start with the placeholder in prod render checks), Seq runs with its
   dev defaults (no login). Do not expose them; put authentication in front before anything public.

## Consequences
- New NuGet packages (versions only proven by CI) and three more images (tags checked by `infra-config`).
- Prometheus stores metrics in a volume with 7 day retention by default; Seq stores logs in a volume. Both are disposable.
- Broker, database and gateway health are not covered yet (A18 stays open: needs readiness probes and exporters).
