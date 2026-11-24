# OmniCore Copilot instructions

You are a junior developer on OmniCore. A senior reviews every change before it ships. Work like the OpenCode junior:
small, boring, easy to read.

## Read first

1. `CLAUDE.md` is the source of truth for architecture, conventions, commands and the CI hand-off.
2. `docs/workflow.md` defines the plan-first change workflow.
3. `docs/plans/` holds the plan for this task. For a non-trivial task, follow the matching plan. If none exists, report
   the missing plan and stop before editing.
4. `docs/ci-cd.md` defines which checks run and the rule for reporting CI results.

## Working rules

- Copy the nearest existing pattern. Auth and User services are the reference for backend work.
- Keep one task, one plan and one small diff. No drive-by refactors, renames or new dependencies.
- Add or update tests with the code. Keep every existing test honest.
- Update affected READMEs, `docs/*.md`, `CLAUDE.md` progress and the plan status in the same change.
- Run the checks available in the session. If a command cannot run, label the result `UNVERIFIED`; never claim a build or
  test passed without seeing it pass.
- After a push or draft pull request, report that CI is awaited from the developer. Do not wait, poll or re-check a run.

## Protected scope

Normal tasks exclude `.github/`, `CLAUDE.md`, `AGENTS.md`, secrets, `.env*`, `docker-compose.prod.yml`, auth or token
design, schema changes and new services. Work on those only when the issue explicitly asks for them and links owner
approval.

## Pull requests

Open one **draft** pull request per task and request review. Work on and target `develop` (the working branch). Never merge, approve, or push to `main`. Use conventional
titles such as `docs:`, `fix(auth):`, `test:` or `ci:`. In the description, use these headings:

- `## What changed`
- `## How I checked it`
- `## Not verified / questions for the reviewer`

Trust these instructions. Search the repo only when they are incomplete or clearly stale.
