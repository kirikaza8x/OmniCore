# 0002: Add GitHub Copilot as a second junior agent

- Status: done
- Date: 2026-11-23
- Owner: agent session
- Related: ADR 0008, `docs/opencode.md`, `docs/workflow.md`, `docs/ci-cd.md`

## Problem

OpenCode is the only delegated junior agent. The repo has no Copilot instructions, custom agent profile, or setup steps,
so Copilot starts without the plan-first workflow, the agent guardrails, or the .NET and Node toolchain. Another junior
agent must follow the same plan and review rules instead of inventing its own scope.

## Scope

- Add a short ADR that records why Copilot is a second junior agent and what it may do.
- Add `.github/copilot-instructions.md` with repo-wide rules for Copilot chat, code review, and cloud agent sessions.
- Add `.github/agents/omnicore-junior.agent.md` as a custom agent profile with the same junior limits as OpenCode.
- Add `.github/workflows/copilot-setup-steps.yml` to preinstall .NET 9 and Node 22 and restore dependencies for GitHub
  Actions-based Copilot sessions.
- Document how to start Copilot, which files control it, and the gap between local validation and CI.

## Non-goals

- No Copilot plan purchase, GitHub App, secret, or org-level setting.
- No replacing OpenCode or its guard; Copilot is additive.
- No automatic Copilot run on push or pull request. A human starts it from the Agents tab or an issue.
- No auto-merge, no direct push to `main`, no approval of its own pull request.
- No change to runtime application code.

## Plan

1. Add this plan and the ADR.
2. Add the Copilot instructions, custom agent profile, and setup-steps workflow.
3. Update `docs/copilot.md`, `docs/opencode.md`, `docs/ci-cd.md`, `README.md`, `CLAUDE.md`, `docs/roadmap.md` and the plan
   index.
4. Validate YAML, shell syntax, frontmatter, and Markdown links locally.
5. Push. The developer reports CI; inspect only a reported failure.

## Risks

- Copilot custom agents and setup steps are GitHub features that need an eligible plan and default-branch presence. Bad
  config can be ignored silently, so the docs state the one-time owner checks.
- GitHub Actions-based setup steps do not apply to cloud sandbox sessions. The docs state this.
- A write-capable coding agent can overrun scope. Instructions and the custom profile narrow it; a human still reads the
  diff. No repo rule claims the guardrails are as strict as the OpenCode guard.
- Setup steps are not a code gate. They install dependencies; `backend.yml` and `frontend.yml` still decide CI status.

## Progress

- [x] plan docs and ADR
- [x] code/config (workflow YAML parses, setup-steps job name verified, shell blocks pass `bash -n`, agent frontmatter parses)
- [x] docs updated with the change (Markdown links resolve)
- [ ] CI reported by the developer (handed off; no polling)
