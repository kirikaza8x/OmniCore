# CI/CD and the agent hand-off

GitHub Actions is the build machine. Agent sessions often have no .NET SDK or NuGet access, so they push and let CI
compile, test and render the stack. This page records what runs, and the rule that keeps an agent from wasting tokens
while it waits.

## Workflows

| Workflow | Runs on | Jobs |
|---|---|---|
| `backend.yml` | PRs and pushes changing `backend/c#/**`, `infra/docker/**` or its own workflow | `changes`, `build-and-unit-test`, `integration-test`, `infra-config`, `ci-gate` |
| `frontend.yml` | PRs and pushes touching `frontend/**` or its workflow | `web` (lint, typecheck, test, build, Docker image) |
| `opencode.yml` | manual dispatch or `/oc` issue comment | `agent` (read-only), `publish` (draft PR + start CI), `notify` |
| `copilot-setup-steps.yml` | manual dispatch or a change to its own file | `copilot-setup-steps` (environment prep for Copilot cloud agent; not a code gate) |

## Scoped runs

A docs-only or frontend-only change does not start `backend.yml`. When it does run, a `changes` job classifies the diff
and the expensive jobs run only for the affected area:

| Changed files | Unit tests | Integration tests | `infra-config` |
|---|---|---|---|
| `backend/c#/OmniCore.Shared/**` | all | all | no |
| one service (`Auth`, `User` or `Notification`) | that service | that service | no |
| service tests only | that service | that service | no |
| solution/global build files | all | all | no |
| `infra/docker/**` | no | no | yes |
| `*.md` under `backend/c#/` or `infra/docker/` | no | no | no (no workflow run) |
| `.github/workflows/backend.yml` | all | all | yes |
| manual dispatch, new branch, empty diff | all | all | yes |

The solution is built in full for every backend change, so a compile break in another project is still caught. Tests
are selective because they are the slow part; integration tests are the most expensive and use Testcontainers
(Postgres + RabbitMQ), one project after the other because RabbitMQ binds port 5672.

`infra-config` renders the dev, prod and cloud-sim compose files, checks every pinned image tag, runs `nginx -t`, and
smoke-starts the data stores. It runs only when infra or the workflow changed.

`ci-gate` is the stable check when `backend.yml` runs: it passes when every selected job passed or was skipped, and
fails when a selected job failed or was cancelled. A docs-only change does not start the workflow, so `ci-gate` is not
produced for it. If branch protection requires a check on every PR, either exempt docs-only PRs or add a separate
always-on docs check; do not require individual backend jobs, because the scoped jobs are skipped on purpose.

## Plan first, then code and docs

Every non-trivial change starts with a Markdown plan under [`docs/plans/`](plans/README.md); the workflow is in
[workflow.md](workflow.md). Agents read the plan before editing, update it when scope changes, and update the affected
docs in the same change. This keeps parallel sessions from inventing different designs.

Claude hands junior work to OpenCode or Copilot through a task issue, not by chat polling. The shared protocol and agent
start commands are in [copilot.md](copilot.md#claude-to-junior-agent-handoff).

## Agent rule: push, then hand off

After a push the agent **stops**. It does not wait, sleep, poll or re-check the run.

1. Push the branch and end the turn with the commit id and a request for the result.
2. The developer (or reviewer) reports whether CI passed or failed.
3. If it failed, the agent reads the failure, fixes it and pushes again. If it passed, the pipeline is accepted and the
   work continues. A silent pipeline is not a reason to poll.
4. Never call an unreported pipeline "verified". Say "pushed; CI result awaited from the developer".

A developer saying "CI passed" counts as the run having shown it. The agent does not need to see the run itself.

## CI mode

`auto` (default since 2026-11-24): the agent checks the run itself, once per push, and fixes failures without being asked; `manual`: push, stop, the developer reports. The developer switches by saying so. Details and rules: `CLAUDE.md`, "Building and CI hand-off".

## Reading a reported failure

When the developer says CI failed, the agent finds the failure itself with the commands below and does not ask for a paste. Order: runs for the head sha -> jobs (find the failed one) -> annotations. Fix, push once, stop. Only if the annotations are empty or unreachable does the agent ask for the error text. (A Docker or local build error the developer pastes is used as given.)

Use the public API (no token needed):

```text
GET https://api.github.com/repos/kirikaza8x/OmniCore/actions/runs?head_sha=<sha>
GET https://api.github.com/repos/kirikaza8x/OmniCore/actions/runs/<run id>/jobs
GET https://api.github.com/repos/kirikaza8x/OmniCore/check-runs/<job id>/annotations
```

Compiler errors and integration-test failure text arrive as annotations. Job summaries and raw logs are **not**
readable without a token. Do not download logs; read the annotations first.

## Token cost

Every poll is a tool call. Its result is added to the conversation context and is billed again as input on later
turns. A long `sleep` also costs a call, and repeated "is it done yet" checks cost more than one. One push plus one
reported result is the intended path.

## OpenCode junior agent

`opencode.yml` screens and validates a patch before publishing it. The `publish` job pushes `opencode/<run>`, opens a
**draft** PR and starts `backend.yml` / `frontend.yml` on that branch. The agent job does not wait for those runs either.
The reviewer reads the diff and reports the CI result; green CI alone is not a reason to merge.
