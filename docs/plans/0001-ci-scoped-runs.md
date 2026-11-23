# 0001: Scoped CI runs and the plan-first workflow

- Status: done
- Date: 2026-11-23
- Owner: agent session
- Related: `docs/ci-cd.md`, `docs/workflow.md`, `CLAUDE.md`

## Problem

`backend.yml` runs every job on every push and PR, including docs-only changes. The three jobs also run the full test
set even when one service changed. Agents also had no shared plan file, so each session could choose a different scope
and different docs.

## Scope

- Path-filter `backend.yml` to backend, infra/docker and its own workflow.
- Add a `changes` job that classifies the diff: shared, auth, user, notification, infra, workflow.
- Build the full solution for any backend change, but run only the affected unit and integration test projects.
- Run `infra-config` only for infra or workflow changes.
- Add a stable `ci-gate` job for required checks.
- Document the plan-first workflow and add this plan as the example.

## Non-goals

- No change to `frontend.yml`; it is already path-filtered.
- No new CI provider or self-hosted runner.
- No test project split or new test framework.

## Plan

1. Add `docs/workflow.md`, `docs/plans/README.md` and this plan.
2. Rewrite `.github/workflows/backend.yml`: path filters, `changes`, selective test lists, `ci-gate`.
3. Update `docs/ci-cd.md`, `README.md`, `CLAUDE.md` and the OpenCode role prompt.
4. Verify the workflow locally where possible (`actionlint` if available, YAML parse, shell syntax) and push.
5. Developer reports CI; inspect only a failure.

## Risks

- A missed path filter can skip a needed run. Shared and solution/workflow changes force all backend jobs.
- Skipped jobs and required checks: `ci-gate` is the single stable check to require.
- `git diff` needs a valid base. A new branch or manual dispatch runs all jobs.

## Progress

- [x] plan docs
- [x] workflow (YAML parses, shell blocks pass `bash -n`, change classifier tested on real diffs)
- [x] docs updated with the change
- [ ] CI reported by the developer (handed off; no polling)
