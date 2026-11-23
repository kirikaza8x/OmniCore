#!/usr/bin/env bash
# Runs the OpenCode CLI once per candidate model until one completes. Never throws the workflow off: it records why it failed.
#   run-agent.sh <role>     env: OC_OUT, MODELS (comma list, first available wins), AGENT_TIMEOUT_MIN (20)
# Reads $OC_OUT/task.txt. Writes model.txt, agent-report.md, agent.err, reason.txt. Exit 0 = a model completed, 1 = none did.
set -u -o pipefail
role="${1:?role}"
out="${OC_OUT:?OC_OUT}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(git rev-parse --show-toplevel)"
mkdir -p "$out"

case "$role" in
  review|repo-scan) cfg="$here/ci-readonly.json" ;;
  implement|tests|docs) cfg="$here/ci.json" ;;
  *) echo "unknown role '$role'" > "$out/reason.txt"; exit 1 ;;
esac
export OPENCODE_CONFIG="$cfg"
export OPENCODE_DISABLE_AUTOUPDATE=1

{ cat "$here/roles/_common.md"; echo; cat "$here/roles/$role.md"; echo; echo "TASK:"; cat "$out/task.txt"; } > "$out/prompt.md"

available="$(opencode models opencode 2>/dev/null || true)"
if [ -z "$available" ]; then
  echo "the opencode model catalogue could not be read" > "$out/reason.txt"; exit 1
fi

tried=""
IFS=',' read -r -a candidates <<< "${MODELS:?MODELS}"
for m in "${candidates[@]}"; do
  m="$(echo "$m" | xargs)"
  [ -n "$m" ] || continue
  if ! grep -qxF "$m" <<< "$available"; then
    echo "skip $m: not in this OpenCode version's catalogue"; tried="$tried $m(missing)"; continue
  fi
  echo "::group::opencode run --model $m"
  timeout "${AGENT_TIMEOUT_MIN:-20}m" opencode run --model "$m" --auto --title "oc-$role" "$(cat "$out/prompt.md")" \
    > "$out/agent-report.raw" 2> "$out/agent.err"
  rc=$?
  echo "::endgroup::"
  if [ "$rc" -eq 0 ] && [ -s "$out/agent-report.raw" ]; then
    echo "$m" > "$out/model.txt"
    sed 's/\x1b\[[0-9;?]*[A-Za-z]//g' "$out/agent-report.raw" | head -c 20000 > "$out/agent-report.md"
    exit 0
  fi
  echo "attempt with $m failed (exit $rc); last stderr:"; tail -5 "$out/agent.err" | head -c 800
  tried="$tried $m(exit $rc)"
  # Start the next model from a clean tree: half-finished edits from a failed run must not leak into the next one.
  git -C "$repo" checkout -q -- . 2>/dev/null; git -C "$repo" clean -qfd -e .opencode 2>/dev/null
done

echo "no free model completed the task. Tried:${tried:- nothing (empty candidate list)}" > "$out/reason.txt"
exit 1
