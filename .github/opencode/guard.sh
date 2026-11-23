#!/usr/bin/env bash
# Screens what the OpenCode agent left in the working tree before anything is published.
#   guard.sh <role>          env: OC_OUT (output dir), MAX_FILES (40), MAX_LINES (1500)
# Writes $OC_OUT/opencode.patch, changed.txt, reason.txt.   Exit: 0 patch ok, 2 rejected, 3 nothing changed.
set -u -o pipefail
role="${1:?role}"
out="${OC_OUT:?OC_OUT}"
max_files="${MAX_FILES:-40}"
max_lines="${MAX_LINES:-1500}"
mkdir -p "$out"
: > "$out/reason.txt"

reject() { echo "$1" > "$out/reason.txt"; echo "guard: rejected - $1" >&2; exit 2; }

git add -A -- . ':(exclude).opencode' >/dev/null 2>&1   # .opencode/ is the agent's own scratch dir
git diff --cached --name-status > "$out/status.txt"
git diff --cached --name-only   > "$out/changed.txt"

if [ ! -s "$out/changed.txt" ]; then
  echo "the agent changed no files" > "$out/reason.txt"
  case "$role" in review|repo-scan) exit 0 ;; esac   # read-only roles are expected to change nothing
  exit 3
fi

case "$role" in
  review|repo-scan) reject "read-only role '$role' modified files: $(head -3 "$out/changed.txt" | tr '\n' ' ')" ;;
esac

n=$(wc -l < "$out/changed.txt")
[ "$n" -le "$max_files" ] || reject "too many files changed ($n > $max_files)"

lines=$(git diff --cached --numstat | awk '$1 ~ /^[0-9]+$/ {s+=$1+$2} $1 == "-" {s+=1000} END {print s+0}')
[ "$lines" -le "$max_lines" ] || reject "diff too large ($lines changed lines > $max_lines)"

# Paths the agent may never change (workflows, its own rules/config, secrets, prod compose).
denied='^\.github/|^opencode\.json$|^CLAUDE\.md$|^AGENTS\.md$|(^|/)\.env|\.(pem|key|pfx|p12)$|docker-compose\.prod\.yml$|^\.git/'
if bad=$(grep -E "$denied" "$out/changed.txt" | head -3 | tr '\n' ' ') && [ -n "$bad" ]; then
  reject "touches protected paths: $bad"
fi

# Unusual file names are rejected instead of handled.
if bad=$(grep -vE '^[A-Za-z0-9._/@+#-]+$' "$out/changed.txt" | head -3 | tr '\n' ' ') && [ -n "$bad" ]; then
  reject "unusual file names: $bad"
fi

# Tests may be added or edited, not removed or switched off.
if del=$(awk '$1 ~ /^D/ {print $2}' "$out/status.txt" | grep -Ei '(^|/)tests?/|\.test\.|\.spec\.|Tests?\.cs$' | head -3 | tr '\n' ' ') && [ -n "$del" ]; then
  reject "deletes test files: $del"
fi
added="$(git diff --cached -U0 | grep -E '^\+[^+]' || true)"
if echo "$added" | grep -Eq '\[(Fact|Theory)\(Skip|\.skip\(|\bxit\(|\bxdescribe\(|\[Ignore\]'; then
  reject "adds a skipped or disabled test"
fi

# Secret-looking additions.
if echo "$added" | grep -Eq 'BEGIN [A-Z ]*PRIVATE KEY|AKIA[0-9A-Z]{16}|gh[pousr]_[A-Za-z0-9]{30,}|sk-[A-Za-z0-9]{32,}'; then
  reject "the diff contains something that looks like a secret"
fi

git diff --cached --binary > "$out/opencode.patch"
echo "ok: $n files, $lines changed lines"
