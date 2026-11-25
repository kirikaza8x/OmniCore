# OpenCode junior developer

Design and reasons: [ADR 0007](decisions/0007-opencode-junior-agent.md). Status: **built, not yet run on GitHub.**
GitHub Copilot was tried and removed 2026-11-25 (never ran: assignment needs a UI click the sandbox cannot make); [ADR 0008](decisions/0008-copilot-junior-agent.md) is superseded.

## Use it
1. One-time: `.github/workflows/opencode.yml` must be on `main` (GitHub only runs `issue_comment` and lists "Run workflow" from the default branch). In Settings > Actions > General enable "Allow GitHub Actions to create and approve pull requests". No secrets are needed. **Branch:** an issue comment (`/oc ...`) checks out and opens its draft PR against `develop` (`OPENCODE_BASE_BRANCH` in the workflow); Run workflow uses the branch chosen in the dropdown. When the workflow file changes, push the same change to `main` too, since GitHub reads the trigger from there.
2. Actions > opencode > Run workflow: pick a role, write the task (name the files, say what "done" is). For non-trivial work, point it at the plan in `docs/plans/`; the roles refuse to invent scope without one. Or comment on an **issue**: `/oc tests: add handler tests for Logout in Auth.Application.Tests` (role optional, default `implement`). Claude can use the issue-comment route; see the shared handoff protocol in [copilot.md](copilot.md#claude-to-junior-agent-handoff).
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

## Claude-to-OpenCode handoff

Neither junior agent listens to Claude's chat session. Claude hands off work through GitHub:

1. Write and push the plan in `docs/plans/NNNN-short-title.md`. Both agents refuse non-trivial work without a plan.
2. Create one issue containing the plan link, role, scope, named files, definition of done, checks and protected paths.
3. Start exactly one agent. One agent owns one task; do not run both on the same diff.
4. Stop. The agent returns a draft PR. The human reviews it and the developer reports CI.

Start OpenCode from the issue with `/oc implement: task` or `/oc tests: task`. The comment author must be an OWNER,
MEMBER or COLLABORATOR. Manual `Actions > opencode > Run workflow` is the fallback. See [opencode.md](opencode.md).

OpenCode works on `develop` (`OPENCODE_BASE_BRANCH`): that is where the current code is; `main` only gets the agent files and releases. Start Copilot from the Agents tab, by assigning the issue to `copilot-swe-agent[bot]` with
`custom_agent: "OmniCore Junior"`, or through the agent tasks API:

```text
POST /agents/repos/{owner}/{repo}/tasks
{
  "prompt": "...",
  "base_ref": "develop",
  "create_pull_request": true
}
```

The agent tasks API is public preview. It needs a user-to-server token (PAT, OAuth or GitHub App user token);
`GITHUB_TOKEN` is not supported. Issue assignment is also available through REST and GraphQL and accepts
`custom_agent`, `custom_instructions`, `base_branch` and `target_repo`.

