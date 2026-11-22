#!/usr/bin/env bash
# Helper for the cloud simulation (docker-compose.cloud-sim.yml). See CLOUD-SIM.md.
# Written without a Docker daemon: only `bash -n` and `docker compose config` were run.
set -euo pipefail
cd "$(dirname "$0")/.."

ENV_FILE="${ENV_FILE:-.env}"
GATEWAY="${GATEWAY_URL:-http://localhost:${GATEWAY_PORT:-8080}}"
DC=(docker compose -f docker-compose.yml -f docker-compose.prod.yml -f docker-compose.cloud-sim.yml --env-file "$ENV_FILE")

usage() {
  cat <<USAGE
Usage: $0 <command> [args]
  up [--build]         start the simulated cloud (builds images with --build)
  down [-v]            stop it (-v also deletes the data volumes)
  status               containers, health and replica counts
  logs [service]       follow logs
  scale <svc> <n>      change replicas, e.g. scale user-api 4
  kill <svc>           kill one random replica (the gateway and the others must keep answering)
  outage <svc> [secs]  pause a dependency (postgres, redis, rabbitmq, minio) for N seconds (default 20), then resume
  smoke                call the gateway: health, register, login (needs the stack up)
  config               print the merged compose file
Environment: ENV_FILE (default .env), GATEWAY_URL, SIM_API_REPLICAS (default 2)
USAGE
}

ensure_env() {
  [[ -f "$ENV_FILE" ]] || { echo "No $ENV_FILE. Run: cp .env.example .env and fill ALLOWED_HOSTS / CORS_ALLOWED_ORIGIN" >&2; exit 1; }
  for k in ALLOWED_HOSTS CORS_ALLOWED_ORIGIN; do
    grep -Eq "^$k=.+" "$ENV_FILE" || { echo "Set $k in $ENV_FILE (the prod override requires it; localhost / http://localhost:3000 is fine here)" >&2; exit 1; }
  done
}

pick_replica() {  # random running container of a compose service
  "${DC[@]}" ps -q "$1" | shuf -n1
}

cmd="${1:-}"; shift || true
case "$cmd" in
  up)      ensure_env; "${DC[@]}" up -d --wait --wait-timeout 240 ${1:+"$@"} ;;
  down)    "${DC[@]}" down "$@" ;;
  status)  "${DC[@]}" ps --format 'table {{.Service}}\t{{.Name}}\t{{.State}}\t{{.Health}}' ;;
  logs)    "${DC[@]}" logs -f --tail 100 "$@" ;;
  scale)   [[ $# -eq 2 ]] || { usage; exit 1; }; "${DC[@]}" up -d --no-recreate --scale "$1=$2" "$1" ;;
  kill)
    [[ $# -eq 1 ]] || { usage; exit 1; }
    id=$(pick_replica "$1"); [[ -n "$id" ]] || { echo "no running container for $1" >&2; exit 1; }
    echo "killing $(docker inspect -f '{{.Name}}' "$id")"; docker kill "$id" >/dev/null
    echo "restart policy brings it back; watch with: $0 status"
    ;;
  outage)
    [[ $# -ge 1 ]] || { usage; exit 1; }
    secs="${2:-20}"; ids=$("${DC[@]}" ps -q "$1")
    echo "pausing $1 for ${secs}s"; docker pause $ids >/dev/null
    trap 'docker unpause $ids >/dev/null 2>&1 || true' EXIT
    sleep "$secs"; docker unpause $ids >/dev/null; trap - EXIT; echo "resumed"
    ;;
  smoke)
    u="sim_$(date +%s)"; p="Sup3r!Secret-sim1"
    echo "health:   $(curl -s -o /dev/null -w '%{http_code}' "$GATEWAY/api/v1/auth/me")  (401 = gateway + auth reachable)"
    echo "register: $(curl -s -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' -d "{\"username\":\"$u\",\"email\":\"$u@example.com\",\"password\":\"$p\"}" "$GATEWAY/api/v1/auth/register")"
    tok=$(curl -s -H 'Content-Type: application/json' -d "{\"identifier\":\"$u\",\"password\":\"$p\"}" "$GATEWAY/api/v1/auth/login" | grep -o '"accessToken":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "login:    ${tok:+ok}${tok:-FAILED}"
    for i in 1 2 3 4 5 6 7 8; do
      code=$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $tok" "$GATEWAY/api/v1/users/me"); echo "profile (try $i, created by the broker event): $code"
      [[ "$code" == 200 ]] && break; sleep 2
    done
    ;;
  config)  "${DC[@]}" config ;;
  *)       usage; [[ -z "$cmd" ]] || exit 1 ;;
esac
