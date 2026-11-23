# GitHub Copilot junior agent

Design and reasons: [ADR 0008](decisions/0008-copilot-junior-agent.md). Status: **configured and linted locally, not yet run
on GitHub.**

Copilot is a second junior agent beside [OpenCode](opencode.md). Both follow the same plan-first workflow in
[workflow.md](workflow.md). Copilot is not a replacement for OpenCode and does not remove the human review gate.

## Use it

1. One-time owner setup: use an eligible paid Copilot plan with cloud agent enabled and write access to this repository.
   Merge these files to `main` first, because GitHub reads the custom agent and setup steps from the default branch.
   Keep "Use custom instructions when reviewing pull requests" enabled in Settings > Copilot > Code review.
2. Go to `github.com/copilot/agents`, select this repository, choose the **OmniCore Junior** custom agent, and give it a
   task or issue URL. You can also assign an issue to Copilot and select the same agent. For non-trivial work, link the
   plan in `docs/plans/`. The agent reports a missing plan instead of inventing scope.
3. The result is one **draft pull request** for review. Read the diff line by line. The developer reports CI when it
   finishes; the agent does not poll. Never merge on green CI alone. Agent pull requests still need the normal
   `backend.yml` / `frontend.yml` checks from [ci-cd.md](ci-cd.md).

## What the agent follows

| Source | Purpose |
|---|---|
| `CLAUDE.md` | architecture, commands, conventions and CI hand-off |
| `docs/workflow.md` | plan first, then code, then docs in the same change |
| `docs/plans/` | the scope and progress for the task |
| `.github/copilot-instructions.md` | repository-wide Copilot rules |
| `.github/agents/omnicore-junior.agent.md` | the selected custom agent's limits and final-report format |
| `.github/workflows/copilot-setup-steps.yml` | .NET 9, Node 22 and dependency restore for GitHub Actions-based sessions |

The custom agent has `read`, `search`, `edit` and `terminal` tools. It works on one small task, adds tests, updates docs
in the same change, opens one draft PR, and never merges or approves. Its normal scope excludes `.github/`, `CLAUDE.md`,
AGENTS files, secrets, `.env*`, `docker-compose.prod.yml`, auth or token design, schema changes and new services.

## Copilot versus OpenCode

| | OpenCode | Copilot |
|---|---|---|
| Start | `Actions > opencode` or `/oc` on an issue | Copilot Agents tab or assigned issue |
| Model | free OpenCode models | Copilot account model |
| Isolation | read-only model job; separate publish job; patch guard | agent holds its own token; no patch guard |
| Guard | `.github/opencode/guard.sh` | repo instructions and custom agent profile |
| Output | draft PR `opencode/<run>` with artifact report | one draft PR with the agent report |
| CI | publish job starts `backend.yml` / `frontend.yml` | normal checks run on the draft PR |

Copilot's instructions are guidance, not the hard guard that OpenCode uses. Treat every Copilot draft as unreviewed code.
Branch protection and a human reader are the safeguards.

## Environment limits

`.github/workflows/copilot-setup-steps.yml` applies only when Copilot runs on GitHub Actions. Cloud sandbox sessions use
their own environment and ignore the setup steps. The setup workflow installs tools and dependencies; it is not a build
gate. A human can run it from the Actions tab to confirm the toolchain.

## First-run check

After merge to `main`, confirm that **OmniCore Junior** appears in the Copilot Agents dropdown and that the setup
workflow passes when run manually. Give the first real run a small docs task with a named file and an explicit
definition of done. Until a run has produced a draft PR, describe Copilot as configured but unrun.

## Local checks

After editing the config:

- parse `.github/workflows/copilot-setup-steps.yml` as YAML;
- check its `run` blocks with `bash -n`;
- parse the YAML frontmatter in `.github/agents/omnicore-junior.agent.md`;
- check Markdown links from this page.
