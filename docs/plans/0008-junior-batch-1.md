# 0008: First junior batch (OpenCode and Copilot)

- Status: A and B done (OpenCode, reviewed, merged 2026-11-24); C waiting for the developer to assign issue #4 to Copilot in the GitHub UI (API assignment answered Forbidden)
- Date: 2026-11-24
- Owner: Claude writes the briefs and reviews; the developer opens the issues and starts the agents
- Related: ADR 0007, ADR 0008, `docs/copilot.md#claude-to-junior-agent-handoff`, roadmap phases 5, 10, 14

## Why
Take routine, self-contained work off Claude while it does the multi-tenancy steps (plan 0007), and run each junior agent
for the first time on a task that is small and checkable. None of these tasks touches tenancy, tokens, schema, `.github/`,
`CLAUDE.md`, secrets or compose files, so they cannot collide with Claude's branch.

## Preconditions
- The agents work on `develop` (OpenCode: `OPENCODE_BASE_BRANCH`; Copilot: `base_branch: develop`). Only the workflow and agent files must also exist on `main`; they do.
- Repo setting: Actions > General > "Allow GitHub Actions to create and approve pull requests" (OpenCode needs it).
- Start **one agent per task**, one task at a time per area. Draft PRs only; Claude reviews every line before merge.

## Task A (OpenCode, role `docs`): event catalogue
**Issue title:** docs: catalogue every integration event
**Scope:** create `docs/events.md`. Read every record that implements `IIntegrationEvent` under `backend/c#/**/*.Contracts`
(Auth: account registered, password reset requested, e-mail verification requested, and any others; Shared contracts).
For each event: name, publishing service, consuming service(s) (grep the consumers), fields with types, whether it carries a
secret (password-reset and verification events carry a raw one-time token: say so), partition key if any, and the outbox/inbox
note. Add a table of contents and link it from `docs/architecture.md` (one line) and `docs/roadmap.md` Phase 5.
**Files:** `docs/events.md`, one line each in `docs/architecture.md` and `docs/roadmap.md`. No code.
**Done when:** every event class in a Contracts project appears once; a reviewer can find each publisher and consumer by the
file path given; no claim without a file path. **Checks:** none (docs only). **Start:** comment `/oc docs: <body of this task>` on the issue.

## Task B (OpenCode, role `tests`, then `implement`): reserved usernames at registration (AD7, part 1)
**Issue title:** auth: reject reserved usernames at registration
**Scope:** in `OmniCore.Services.Auth.Application/Features/Auth/Commands/Register/RegisterCommandValidator.cs` reject, case-insensitively,
these usernames: admin, administrator, root, superadmin, system, support, moderator, owner, null, undefined, api, www, me,
anonymous, postmaster, noreply, no-reply. Error message: "This username is reserved." Add a `ReservedUsernames` static class in the
same folder holding the list. **Do not** change the `Username` value object (the first-admin seeder creates accounts through it
and may legitimately use `admin`), the database or any migration.
**Files:** the validator, the new list class, tests in `tests/OmniCore.Services.Auth.Application.Tests/` (new file `RegisterValidatorReservedNamesTests.cs`).
**Done when:** tests cover each listed name in two casings, one normal name passing, and the first-admin path is untouched.
**Checks:** `dotnet` is not available in the agent sandbox; CI (`backend.yml`) is the check. Mark the work unbuilt in the PR text.
**Start:** `/oc implement: <body>` (the tests are part of the task).

## Task C (Copilot, custom agent `OmniCore Junior`): apply the theme setting in the web client
**Issue title:** web: apply the saved theme setting (Light / Dark / System)
**Scope:** `frontend/web` only. The profile settings already store `theme` as `Light | Dark | System`
(`src/features/profile/schemas.ts`). Today the UI follows the OS (`globals.css`, Tailwind `dark:` classes) and ignores the setting.
Make the chosen theme apply: set the Tailwind dark mode to class-based (`@custom-variant dark` in Tailwind 4 / `darkMode: "class"`
in the config, whichever the repo uses), put a small client component in the `(app)` layout that reads the user's settings and
toggles the `dark` class on `<html>` (System follows `prefers-color-scheme` and reacts to changes), and use a short inline script
or cookie so there is no flash of the wrong theme on load for signed-in pages. Keep the `features/` layer rules (ESLint enforces
them); do not touch tokens or the BFF routes.
**Files:** `frontend/web/src/app/globals.css`, `src/app/(app)/layout.tsx`, a new component under `src/features/profile/components/`, tests under the existing test folder.
**Done when:** `npm run lint`, `typecheck`, `test` and `build` pass in `frontend/web`; a unit test covers the Light/Dark/System to class mapping.
**Start:** assign the issue to `copilot-swe-agent[bot]` with custom agent `OmniCore Junior`.

## Review checklist (Claude, per PR)
Scope matches the issue; no change outside the named files; no secrets; tests exist and would fail without the change;
docs updated where the change invalidates them; CI result reported by the developer before merging.

## After the batch
Record in `docs/opencode.md` / `docs/copilot.md` what the first real runs showed (what worked, what the guard blocked),
then change "not yet run on GitHub" in `CLAUDE.md`.
