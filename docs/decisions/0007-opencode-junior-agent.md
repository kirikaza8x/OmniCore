# ADR 0007: OpenCode (free model) as a junior developer in GitHub Actions

Status: accepted by delegation (2026-11-23). Not yet run on GitHub: see "Unverified" below.

## Context
Routine work (small fixes, tests for existing code, doc updates, first-pass reviews) should not use up the senior session
(Claude + the owner), which is better spent on design decisions. OpenCode is an open-source coding agent CLI; a set of
free hosted models can be used with it. The repo's CI is the only build machine for .NET, so the agent has to live there.

## Decision
A workflow `.github/workflows/opencode.yml` with three jobs and a hard separation of powers:

| Job | Token | Can do |
|---|---|---|
| `agent` | `contents: read`, no secrets, checkout without credentials | install pinned OpenCode, run the model, screen the result (`guard.sh`), build and unit-test it (`validate.sh`). Output: a patch and a report |
| `publish` | `contents`, `pull-requests`, `actions` write | apply the screened patch, push `opencode/<run>`, open a **draft** PR, start backend/frontend CI on it. Never merges |
| `notify` | `issues: write` | job summary, and a comment on the issue it was started from |

The model never holds a write token and never runs in the job that does. Its edits reach the repository only as a patch
that passed the guard.

Roles (one prompt file each in `.github/opencode/roles/`): `implement`, `tests`, `docs` (may edit) and `review`,
`repo-scan` (read-only: edit permission denied in `ci-readonly.json`, and the guard rejects any change).

Triggers: manual (`workflow_dispatch`) and `/oc [role] task` in an **issue** comment by an owner, member or collaborator.
Not on pull requests and not on fork or contributor text, so a stranger cannot hand the agent instructions.

Model choice: a list of free `opencode/*` models tried in order; ids missing from the pinned version's catalogue are
skipped. Order prefers models documented as zero-retention, because the repo content is sent to the provider. Override with
the repo variable `OPENCODE_MODELS` or the `models` input.

## Guard rules (`.github/opencode/guard.sh`, tested)
Reject: changes to `.github/`, `opencode.json`, `CLAUDE.md`, `AGENTS.md`, env files, keys, `docker-compose.prod.yml`;
more than 40 files or 1500 changed lines; deleted test files; added skipped tests; secret-looking additions; unusual file
names; any change by a read-only role. "Nothing changed" is reported, not treated as success.

## Fallback
Every failure (install, no model answered, guard, build, tests) ends the same way: nothing is published, the reason goes to the
job summary (and the issue), the other workflows are not touched. The work is then done by hand. The workflow is not a
required check and nothing depends on it.

## Review before shipping
Draft PRs only, labelled `opencode` and `needs-review`, with a checklist. The agent cannot approve or merge. The unit
tests it ran are a floor: the integration tests run through `backend.yml` on the branch, and every opencode PR is read
line by line by the senior reviewer (Claude or the owner) before merge. Green CI alone is not a reason to merge.

## Alternatives rejected
- The official `anomalyco/opencode/github` action: it needs the OpenCode GitHub App (OIDC) and lets the agent push and comment
  with a write token in the same job as the model. The split above is stricter and keeps the guard in the path.
- `@latest`: unreviewed code on every run. The version is pinned (1.19.6) and bumped on purpose.

## Consequences
- Free models are weaker and may be rate-limited or removed: runs will sometimes produce nothing. That is acceptable by design.
- Pushes made with `GITHUB_TOKEN` do not start workflows, so `publish` starts CI with `workflow_dispatch`
  (`backend.yml` already has it; `frontend.yml` gained it).
- Repo setting needed for PR creation: Settings > Actions > General > "Allow GitHub Actions to create and approve pull
  requests". Without it the branch is still pushed and the run summary links the compare page.

## Unverified (2026-11-23)
Checked here: pinned CLI installs, flags and both config files parse, all default model ids exist in the catalogue,
`guard.sh` (11 cases), model fallback in `run-agent.sh` (stubbed CLI), workflow passes actionlint.
Not checked: a real run. The sandbox cannot reach the OpenCode API and the workflow has not run on GitHub. Open questions:
whether free models need no login in CI, exit codes of `opencode run` on provider errors, `--auto` with the deny rules,
PR creation and CI dispatch with `GITHUB_TOKEN`, and model quality.
