# Copilot Instructions

## Project Context

- This repository is a brownfield refactor project for the existing BiliBiliToolPro codebase.
- The goal is to make the codebase safer to change through clearer boundaries, lower coupling, and stronger test coverage on critical flows.
- Preserve existing Console, scheduling, and integration behavior while refactoring.

## Planning Artifacts

- Read `.planning/PROJECT.md` for project intent, constraints, and decisions.
- Read `.planning/REQUIREMENTS.md` for scoped v1 refactor requirements and phase traceability.
- Read `.planning/ROADMAP.md` for the approved phase structure.
- Read `.planning/STATE.md` for current project position.
- Use `.planning/codebase/` and `.planning/research/` as supporting context during planning and execution.

## Current Approved Direction

- Phase 1: Boundary Guardrails
- Phase 2: Host Safety Nets
- Phase 3: Login Refactor Slice
- Phase 4: DailyTask Refactor Slice
- Phase 5: Scheduler Shell Cleanup
- Phase 6: Integration Boundary And Failure Model

## Execution Guidance

- Favor incremental, rollback-safe changes over broad rewrites.
- Add validation before moving critical behavior.
- Keep hosts thin and move orchestration into module or application boundaries.
- Treat architecture rules, characterization tests, and integration tests as core deliverables.
- Avoid framework churn unless a later phase explicitly justifies it.

## Next Step

- The next workflow step is `/gsd-discuss-phase 1`.