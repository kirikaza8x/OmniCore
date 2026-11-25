# Plans

One Markdown plan per non-trivial change. The workflow is in [../workflow.md](../workflow.md).

## Naming and index

`NNNN-short-title.md`, numbered in creation order. Add a row here when the plan is created.

| Plan | Status | Scope |
|---|---|---|
| [0001-ci-scoped-runs.md](0001-ci-scoped-runs.md) | done | path-filtered backend CI, selective jobs, plan-first workflow docs |
| [0003-claude-handoff-docs.md](0003-claude-handoff-docs.md) | done, CI green | Claude catch-up and task handoff to OpenCode or Copilot |
| [0004-password-reset-email-verification.md](0004-password-reset-email-verification.md) | done, CI green | password reset and e-mail verification across Auth and Notification (ADR 0009) |
| [0005-require-verified-email.md](0005-require-verified-email.md) | done, CI green | optional verified-e-mail enforcement at login (ADR 0010) |
| [0006-observability.md](0006-observability.md) | done, CI green | OpenTelemetry + Seq + Prometheus + Grafana profile (ADR 0011) |
| [0007-multitenancy.md](0007-multitenancy.md) | in progress (steps 1-2b green, 3 next) | multi-tenancy rollout (ADR 0001) |
| [0008-junior-batch-1.md](0008-junior-batch-1.md) | A, B done; C waiting | first OpenCode tasks: event catalogue, reserved usernames, web theme |

Statuses: `planned`, `in progress`, `blocked`, `done`, `abandoned`.

## Template

```markdown
# NNNN: Short title

- Status: planned
- Date: YYYY-MM-DD
- Owner: name or agent session
- Related: issue, ADR, roadmap phase, repo-check ids

## Problem

What is wrong or missing. One short paragraph.

## Scope

What this change does.

## Non-goals

What stays out, so the change cannot grow silently.

## Plan

1. Files and behaviour to change.
2. Tests to add or update.
3. Docs to update in the same change.
4. CI jobs this change should trigger.

## Risks

Schema, compatibility, security, rollout, rollback.

## Progress

- [ ] code
- [ ] tests
- [ ] docs
- [ ] CI reported by the developer
```
