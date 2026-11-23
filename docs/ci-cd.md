# CI/CD and the agent hand-off

GitHub Actions is the build machine. Agent sessions often have no .NET SDK or NuGet access, so they push and let CI
compile, test and render the stack. This page records what runs, and the rule that keeps an agent from wasting tokens
while it waits.

## Workflows

| Workflow | Runs on | Jobs |
|---|---|---|
| `backend.yml` | PRs; pushes to `develop`, `main`, `docs/**`, `fix/**` | `build-and-unit-test`, `integration-test`, `infra-config` |
| `frontend.yml` | PRs and pushes touching `frontend/**` or its workflow | `web` (lint, typecheck, test, build, Docker image) |
| `opencode.yml` | manual dispatch or `/oc` issue comment | `agent` (read-only), `publish` (draft PR + start CI), `notify` |

`backend.yml` integration tests use Testcontainers (Postgres + RabbitMQ) and run one project after the other because
RabbitMQ binds port 5672. `infra-config` renders the dev, prod and cloud-sim compose files, checks every pinned image
tag, runs `nginx -t`, and smoke-starts the data stores.

## Agent rule: push, then hand off

After a push the agent **stops**. It does not wait, sleep, poll or re-check the run.

1. Push the branch and end the turn with the commit id and a request for the result.
2. The developer (or reviewer) reports whether CI passed or failed.
3. If it failed, the agent reads the failure, fixes it and pushes again. If it passed, the pipeline is accepted and the
   work continues. A silent pipeline is not a reason to poll.
4. Never call an unreported pipeline "verified". Say "pushed; CI result awaited from the developer".

A developer saying "CI passed" counts as the run having shown it. The agent does not need to see the run itself.

## Reading a reported failure

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
