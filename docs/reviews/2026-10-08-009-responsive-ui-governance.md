Date: October 8, 2026
Status: Awaiting review
PR:

# Responsive UI/UX governance

## Increment

This documentation-only increment updates Cardui's shared UI/UX governance so
new screens and substantial changes lead with the user's question and next
action, control visual noise, and define intentional desktop and mobile
arrangements before implementation.

## Changes

- `.cursor/rules/ui-governance.mdc` now governs purpose and hierarchy,
  whole-page composition, task-based width and density, intentional responsive
  behavior, local tabs and progressive disclosure, warning priority, charts
  and metrics, contextual actions, state handling, accessibility, routine
  agent autonomy, and lightweight UX review evidence.
- `docs/design/ui-direction.md` now explains the rationale and examples without
  becoming a second rulebook. It marks the old no-budget, three-jobs,
  Plan-placeholder, and desktop-then-stack statements as historical.
- `docs/design/plan-page.md` and the Plan status in `docs/roadmap.md`
  distinguish the historical long one-page stack from the currently
  implemented three-view refinement, which is still awaiting review. The
  separate Planning UX recommendations remain unapproved proposals.
- `AGENTS.md` and `frontend/AGENTS.md` carry the same concise pointer to the
  hierarchy, responsive, and decision-autonomy rules. The generated Next.js
  instruction block remains intact.
- `docs/reviews/README.md` adds a concise UI/UX implementation-review
  expectation: first content and action, secondary access, mobile adaptation,
  and visual or interaction checks awaiting Zach.

## Data changes

None. No application code, dependency, route, API, schema, migration,
financial record, roadmap order, or runtime behavior changed. The Plan
redesign was not implemented.

## Agent verification

- Targeted Prettier checks passed for every changed Markdown and rule file.
- `git diff --check` passed for the documentation increment.
- Changed-document links were inspected against the repository and resolve to
  existing rule, design, decision, and review files.
- Focused searches found the retired no-budget, three-jobs, Plan-placeholder,
  and desktop-first stacking language only in the historical cleanup note.
- Application builds, lint, tests, and browser QA were not run because this
  increment changes documentation only.

## Manual verification

Zach waived manual verification for this documentation-only increment on
October 8, 2026. No visual, usability, end-to-end, or application-data check
was required.

## Scope retained

- The Plan page refinement remains awaiting review in
  `2026-10-08-008-plan-page-refinement.md`.
- The Phase 2 UX closure remains awaiting its separate approval.
- The broader Planning UX assessment remains a proposal. This increment does
  not approve its recommendations or start a further Plan redesign.
- Existing financial qualifications, navigation decisions, design tokens,
  accessibility requirements, primitives, and component conventions remain in
  force.

## Approval

Awaiting Zach's review.

## Pending decision

Approve the updated UI/UX governance and synchronized documentation. No new
product behavior or dependency decision is required in this increment.
