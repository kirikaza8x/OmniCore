# Change workflow: plan, code, docs, CI

Every non-trivial change starts with a Markdown plan. One plan keeps agents and humans on the same scope instead of
each session inventing its own. Trivial fixes (typo, broken link) can skip the plan file but still update affected docs.

## 1. Plan first

Before changing code:

1. Read `CLAUDE.md`, `docs/architecture.md`, the relevant service/layer `README.md`, `docs/roadmap.md` and
   `docs/repo-check.md`.
2. Add `docs/plans/NNNN-short-title.md` from the template in [plans/README.md](plans/README.md). State the problem,
   scope, non-goals, files, tests, docs, risks and the CI jobs the change should trigger.
3. Link the plan from the roadmap item or the issue. One plan per task; do not run parallel plans for the same area.
4. If the change is risky (schema, token format, rename, new service, new dependency), write the ADR first, as
   `CLAUDE.md` requires. The plan links the ADR; the ADR holds the decision.
5. Get owner or senior approval for the scope before implementation when the change is not routine.

## 2. Implement

- Work in small commits and keep the plan status current (`planned` → `in progress` → `done`).
- Add or update tests with the code. Do not weaken a test to make the change pass.
- If the scope changes, update the plan first, then the code. Do not leave the plan describing an older design.

## 3. Update docs in the same change

Docs are part of the change, not a later cleanup:

- affected `README.md` files (service, layer, contracts);
- `docs/architecture.md`, `docs/roadmap.md`, `docs/repo-check.md`;
- `CLAUDE.md` progress/status lines;
- an ADR if a decision changed;
- the plan status and the final commit links.

A change is not done while its docs still describe the old behaviour.

## 4. Hand off CI

Push, then stop. The developer reports the result; inspect only a reported failure. Full rule and failure commands:
[ci-cd.md](ci-cd.md).
