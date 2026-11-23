#!/usr/bin/env bash
# Build/test what the agent changed, with the same commands as CI. Exit 0 = all green or nothing to build, 1 = failed.
#   validate.sh     env: OC_OUT. Reads $OC_OUT/changed.txt. Appends a human summary to $OC_OUT/validation.md.
set -u -o pipefail
out="${OC_OUT:?OC_OUT}"
repo="$(git rev-parse --show-toplevel)"
: > "$out/validation.md"
fail() { echo "$1" > "$out/reason.txt"; echo "- FAILED: $1" >> "$out/validation.md"; exit 1; }
ok()   { echo "- ok: $1" >> "$out/validation.md"; }

if ! git -C "$repo" diff --cached --check; then fail "whitespace errors in the diff (git diff --check)"; fi
ok "git diff --check"

if grep -q '^backend/' "$out/changed.txt"; then
  cd "$repo/backend/c#" || fail "backend/c# missing"
  dotnet restore OmniCore.slnx >"$out/restore.log" 2>&1 || { tail -20 "$out/restore.log"; fail "dotnet restore failed"; }
  dotnet build OmniCore.slnx --no-restore -c Release >"$out/build.log" 2>&1 || { grep -E "error " "$out/build.log" | sort -u | head -20; fail "dotnet build failed"; }
  ok "dotnet build (Release)"
  # Unit tests only (no Docker); the integration tests run in backend.yml on the pushed branch.
  for p in tests/*Domain.Tests tests/*Application.Tests tests/OmniCore.Shared.Tests; do
    [ -d "$p" ] || continue
    dotnet test "$p" -c Release --no-build >"$out/test.log" 2>&1 || { tail -30 "$out/test.log"; fail "unit tests failed in $p"; }
    ok "dotnet test $p"
  done
fi

if grep -q '^frontend/web/' "$out/changed.txt"; then
  cd "$repo/frontend/web" || fail "frontend/web missing"
  npm ci >"$out/npm.log" 2>&1 || { tail -20 "$out/npm.log"; fail "npm ci failed"; }
  for s in lint typecheck test; do
    npm run "$s" >"$out/npm-$s.log" 2>&1 || { tail -30 "$out/npm-$s.log"; fail "npm run $s failed"; }
    ok "npm run $s"
  done
fi
exit 0
