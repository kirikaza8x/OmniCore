# ADR 0008: GitHub Copilot as a second junior agent

Status: accepted by delegation (2026-11-23). Config written and linted locally; not yet run on GitHub.

## Context

[ADR 0007](0007-opencode-junior-agent.md) added a free-model OpenCode agent for routine work, but its model availability
and quality vary, and a real GitHub run is still unverified. The repository also has users with GitHub Copilot plans.
They should be able to delegate the same small tasks to Copilot cloud agent without teaching it the repo conventions on
every run.

Copilot cloud agent has a different security shape from the OpenCode pipeline. Copilot holds its own token, can commit,
push and open a pull request as part of a session, and does not pass through `.github/opencode/guard.sh`. A second junior
agent must therefore be constrained by repo instructions, a narrow custom agent profile, branch protection, and human
review, not by pretending the two agents have identical isolation.

## Decision

Add three repository files:

- `.github/copilot-instructions.md`: repository-wide rules for Copilot chat, code review and cloud agent sessions. It
  points to `CLAUDE.md` and `docs/workflow.md` as the sources of truth and states the junior-agent limits.
- `.github/agents/omnicore-junior.agent.md`: a custom agent profile named `OmniCore Junior`. It is selected in the
  Copilot Agents tab or when assigning an issue. Its tools are `read`, `search`, `edit` and `terminal`; its prompt requires
  the plan-first workflow, small diffs, tests with code, docs in the same change, and one draft pull request per task.
- `.github/workflows/copilot-setup-steps.yml`: setup steps for GitHub Actions-based sessions. It installs .NET 9 and
  Node 22, restores the solution and `frontend/web` dependencies, and runs only for its own file or manual dispatch.

Copilot is started by a human from the Agents tab or by assigning an issue to it. It does not run on every push or pull
request. It opens a draft pull request; a human reads the diff, the developer reports CI, and nothing merges on green CI
alone.

The same exclusions as ADR 0007 apply to normal tasks: `.github/`, `CLAUDE.md`, AGENTS files, secrets, env files,
`docker-compose.prod.yml`, auth/token design, schema changes and new services stay with a senior session unless the
issue explicitly authorises them.

## Consequences

- Copilot needs an eligible paid plan and cloud agent enabled. The custom agent profile and setup steps must be present
  on the default branch before GitHub shows or applies them.
- Setup steps apply only to GitHub Actions-based Copilot sessions. Cloud sandbox sessions use their own environment.
- The setup workflow is not a CI gate. `backend.yml` and `frontend.yml` remain the build authorities.
- Copilot's write capability is stronger than OpenCode's read-only model job. The safeguard is a small scope plus a
  human review of every draft PR; this is weaker isolation than the OpenCode patch guard and is stated openly in the
  docs.
- Agent instructions are read from the head branch for Copilot code review, so changes to them in a draft PR affect that
  review. Review those files first.

## Alternatives rejected

- Duplicating all of `CLAUDE.md` into `copilot-instructions.md`: two sources of truth would drift. The instructions
  point to the existing files.
- A workflow that runs Copilot on every push: wasted runs and untrusted task text. A human starts the agent.
- A separate GitHub App or secret for Copilot: not needed for repository custom instructions, custom agents, or setup
  steps.

## Verification before use

Local: YAML parses, shell blocks pass `bash -n`, frontmatter parses, and Markdown links resolve.
On GitHub, after merge to `main`: the agent appears in the Copilot Agents dropdown, setup steps pass when manually run,
and a first task produces a draft PR with the expected labels and review checklist. Until then, describe Copilot as
configured but unrun, as with OpenCode.
