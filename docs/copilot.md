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

## Claude-to-junior-agent handoff

Neither junior agent listens to Claude's chat session. Claude hands off work through GitHub:

1. Write and push the plan in `docs/plans/NNNN-short-title.md`. Both agents refuse non-trivial work without a plan.
2. Create one issue containing the plan link, role, scope, named files, definition of done, checks and protected paths.
3. Start exactly one agent. One agent owns one task; do not run both on the same diff.
4. Stop. The agent returns a draft PR. The human reviews it and the developer reports CI.

Start OpenCode from the issue with `/oc implement: task` or `/oc tests: task`. The comment author must be an OWNER,
MEMBER or COLLABORATOR. Manual `Actions > opencode > Run workflow` is the fallback. See [opencode.md](opencode.md).

Always point Copilot at `develop` (`base_branch` / `base_ref`): that is where the current code is; `main` only gets the agent files and releases. Start Copilot from the Agents tab, by assigning the issue to `copilot-swe-agent[bot]` with
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
