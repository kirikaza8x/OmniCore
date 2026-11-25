# 0003: Claude handoff documentation

- Status: done
- Date: 2026-11-23
- Owner: agent session
- Related: `CLAUDE.md`, `docs/opencode.md`, `docs/opencode.md`, `docs/ci-cd.md`

## Problem

A future Claude session needs one current place to learn what changed and how to delegate to the junior agents. The Copilot
docs do not yet record the Claude-to-agent task handoff, and `CLAUDE.md` still says the progress list reflects an older
branch and date.

## Scope

- Add a Claude-to-junior-agent handoff section to `docs/opencode.md`.
- Point `docs/opencode.md` and `docs/ci-cd.md` to that handoff.
- Update `CLAUDE.md` read-first pointers, current progress header, last commit and next steps.

## Non-goals

- No new automation, API client, secret, token or workflow.
- No start of OpenCode or Copilot from this change.
- No runtime application code.

## Plan

1. Add this plan and index it.
2. Document the issue-based handoff: plan first, one issue, one agent, draft PR, human review, no polling.
3. Update Claude's catch-up file and cross-links.
4. Check Markdown links, then commit and push.

## Risks

- Handoff instructions can drift from the agent config. The docs point to the config files instead of restating their
  internals.
- Copilot's agent task API is public preview. The docs mark that and require a user-to-server token.
- No CI result is checked by this change. The developer reports CI.

## Progress

- [x] plan docs
- [x] docs updated
- [x] links checked
- [ ] committed and pushed
