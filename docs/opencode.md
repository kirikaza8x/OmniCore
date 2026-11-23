# OpenCode junior developer

Design and reasons: [ADR 0007](decisions/0007-opencode-junior-agent.md). Status: **built, not yet run on GitHub.**
GitHub Copilot is a second junior agent with the same plan-first rules; see [copilot.md](copilot.md) and
[ADR 0008](decisions/0008-copilot-junior-agent.md).

## Use it
1. One-time: merge `.github/workflows/opencode.yml` to `main` (GitHub only runs `issue_comment` and lists "Run workflow" from the default branch). In Settings > Actions > General enable "Allow GitHub Actions to create and approve pull requests". No secrets are needed.
2. Actions > opencode > Run workflow: pick a role, write the task (name the files, say what "done" is). For non-trivial work, point it at the plan in `docs/plans/`; the roles refuse to invent scope without one. Or comment on an **issue**: `/oc tests: add handler tests for Logout in Auth.Application.Tests` (role optional, default `implement`).
3. Result: a **draft PR** `opencode/<run>` (labels `opencode`, `needs-review`) with the agent's report, the automatic check results and a review checklist; `backend.yml` (and `frontend.yml` if the web client changed) start on the branch. Read-only roles post their findings in the run summary / issue instead.
4. Review the diff line by line. The reviewer reports the CI result when it finishes; the agent does not poll. Merge or close after that, and never merge on green CI alone. See [ci-cd.md](ci-cd.md).

## Roles
| Role | Edits? | Good for |
|---|---|---|
| `implement` | yes | small, well-specified fixes that copy an existing pattern |
| `tests` | yes (tests only) | tests for existing behaviour; real bugs it finds are reported, not fixed |
| `docs` | yes (docs only) | bringing READMEs/`docs/*.md` in line with the code |
| `review` | no | first-pass review of named code; findings with file and failing input |
| `repo-scan` | no | hygiene sweep (missing `RequireAuthorization`, TODOs, stray secrets), compared with `docs/repo-check.md` |

Not for: design decisions, schema or token changes, new services, anything touching auth or secrets. Those stay with the senior session.

## What it can and cannot do
- The agent job has a read-only token and no secrets, and OpenCode runs with `.github/opencode/ci.json`: web fetch denied, shell limited to read commands plus `dotnet restore|build|test` and `npm ci|run lint|run typecheck|test`, no sharing, no auto-update.
- `guard.sh` rejects patches that touch `.github/`, `opencode.json`, `CLAUDE.md`, env files/keys, the prod compose file; that exceed 40 files / 1500 lines; delete or skip tests; add secret-looking text.
- `validate.sh` builds the solution and runs the unit tests (and lint/typecheck/test for `frontend/web`) before anything is published.
- Failure of any step publishes nothing and says why (run summary, and the issue). Other workflows are unaffected.

## Configuration
| What | Where |
|---|---|
| OpenCode version | `OPENCODE_VERSION` in the workflow (pinned; bump deliberately) |
| Models | repo variable `OPENCODE_MODELS` (comma list, first usable wins), else `DEFAULT_MODELS` in the workflow. `opencode models opencode` lists the current free ids; the free list changes, so expect to edit it |
| Role prompts | `.github/opencode/roles/*.md` (`_common.md` applies to all) |
| Permissions | `.github/opencode/ci.json` (edit roles), `ci-readonly.json` (read-only roles) |
| Limits and protected paths | top of `.github/opencode/guard.sh` |

Free models may retain or train on what they see (see the provider notes for each model); this repo is public, but do not
put anything private in a task.

## Local checks
`bash .github/opencode/guard.sh <role>` works in any git checkout (set `OC_OUT` to a directory outside the repo). `actionlint` the workflow after edits.

## Not verified yet
A real run (see ADR 0007). The first run is a test: start with `repo-scan` (read-only) to see whether a free model completes at all, then a trivial `docs` task.
