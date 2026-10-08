# Implementation reviews

One report per increment. What is happening now, what comes next, and open
decisions are in [`docs/README.md`](../README.md), not in these reports.

## Conventions

- A new report goes in this folder as `YYYY-MM-DD-NNN-short-name.md`. `NNN`
  is unique for its date. It is not reused, and it does not have to match
  the order the reports were written in. This index gives the order.
- A report starts with `Date`, `Status`, and `PR` lines. Status is
  `Awaiting review`, `Approved YYYY-MM-DD`, `Waived`, or `Superseded by …`.
  Approval goes in the report's `## Approval` section.
- A report's "Pending decision" is true when written. After that, read the
  status block in `docs/README.md`. Do not rewrite an old report to update
  it. A wrong fact gets a dated correction note at the end of the report.
- Update this index, newest first, and the status block in
  `docs/README.md` in the same change as the report.
- When a roadmap phase closes, its reports may move to
  `archive/<phase>/` with every link updated. Phases 0 and 1 moved on
  October 7, 2026.
- On October 7, 2026, five reports that shared a number with another
  report on the same date took that date's next free number. Their
  contents did not change.
- The next-chat prompt follows
  [`.cursor/rules/handoff.mdc`](../../.cursor/rules/handoff.mdc).

## UI/UX implementation reviews

For a new screen or substantial UI change, follow section 9 of
[`.cursor/rules/ui-governance.mdc`](../../.cursor/rules/ui-governance.mdc).

## UX assessments

- [2026-10-08 — Phase 2 product and UX audit](2026-10-08-006-phase-2-ux-audit.md) — Approved 2026-10-08
- [2026-10-08 — Planning UX review](2026-10-08-003-planning-ux-review.md) — Approved 2026-10-08. The assessment is approved. Unimplemented proposals stay unscheduled

## Rules and handoff

- [2026-10-08 — Documentation audit](2026-10-08-010-documentation-audit.md) — Approved 2026-10-08
- [2026-10-08 — Responsive UI/UX governance](2026-10-08-009-responsive-ui-governance.md) — Approved 2026-10-08
- [2026-10-08 — Handoff roadmap order](2026-10-08-001-handoff-roadmap-order.md) — Approved 2026-10-08

## Phase 3 — Recovery calculations and forecasts

- [2026-10-08 — Plan page refinement](2026-10-08-008-plan-page-refinement.md) — Approved 2026-10-08
- [2026-10-07 — Plan extra payment](2026-10-07-016-plan-extra-payment.md) — Approved 2026-10-08
- [2026-10-07 — Plan cash outlook](2026-10-07-015-plan-cash-outlook.md) — Approved 2026-10-07
- [2026-10-07 — Plan recovery screen](2026-10-07-014-plan-recovery.md) — Approved; reworked as Plan screen item 1 on October 7, with correction notes
- [2026-10-07 — Plan in the main nav](2026-10-07-013-plan-nav.md) — Approved 2026-10-07
- [2026-10-07 — Cash-flow recovery](2026-10-07-012-cash-flow-recovery.md) — Approved 2026-10-07; correction note added October 7
- [2026-10-07 — Payoff rollover](2026-10-07-011-payoff-rollover.md) — Approved 2026-10-07
- [2026-10-07 — Payoff priority](2026-10-07-010-payoff-priority.md) — Approved 2026-10-07
- [2026-10-07 — Cash forecast](2026-10-07-009-cash-forecast.md) — Approved 2026-10-07
- [2026-10-07 — Recovery calculations](2026-10-07-008-recovery-calculations.md) — Approved 2026-10-07

## Phase 2, linked debts, and the UI plan

Newest first.

- [2026-10-08 — Phase 2 standards review](2026-10-08-011-phase-2-standards-review.md) — Approved 2026-10-08
- [2026-10-08 — Phase 2 UX closure](2026-10-08-007-phase-2-ux-closure.md) — Approved 2026-10-08
- [2026-10-08 — Simplified Living](2026-10-08-005-simplified-living.md) — Approved 2026-10-08
- [2026-10-08 — Living](2026-10-08-004-living.md) — Superseded by the simplified Living review
- [2026-10-08 — Savings](2026-10-08-002-savings.md) — Approved 2026-10-08
- [2026-10-07 — Reconnect](2026-10-07-007-reconnect.md) — Approved 2026-10-07
- [2026-10-07 — Credit limit](2026-10-07-006-credit-limit.md) — Approved 2026-10-07
- [2026-10-07 — Suggested matches](2026-10-07-005-suggested-matches.md) — Approved 2026-10-07
- [2026-10-07 — Domain folders](2026-10-07-004-domain-folders.md) — Approved 2026-10-07
- [2026-10-07 — Balance overrides](2026-10-07-003-balance-overrides.md) — Approved 2026-10-07
- [2026-10-07 — Follow a balance](2026-10-07-002-follow-a-balance.md) — Approved 2026-10-07
- [2026-10-07 — Documentation cleanup](2026-10-07-001-documentation-cleanup.md) — Approved 2026-10-07
- [2026-10-06 — Sync correctness](2026-10-06-002-sync-correctness.md) — Approved 2026-10-07
- [2026-10-06 — Linked manual debts design and plan](2026-10-06-001-linked-manual-debts-design.md) — Approved; correction note added October 7
- [2026-10-05 — Category targets](2026-10-05-019-category-targets.md) — Approved 2026-10-06
- [2026-10-05 — Debt summary](2026-10-05-016-debt-summary.md) — Approved; its pending decision was superseded by 2026-10-06-001
- [2026-10-05 — Accounts action menu](2026-10-05-015-accounts-action-menu.md) — Approved; recorded October 7 (PR #20)
- [2026-10-05 — Debts](2026-10-05-014-debts.md) — Approved
- [2026-10-05 — Recurring bill suggestions](2026-10-05-013-recurring-suggestions.md) — Approved
- [2026-10-05 — Bills](2026-10-05-012-bills.md) — Approved
- [2026-10-05 — Paycheck schedules and gross pay](2026-10-05-011-paycheck-schedules.md) — Approved
- [2026-10-05 — Income form feedback](2026-10-05-010-income-form-feedback.md) — Approved
- [2026-10-05 — UI governance](2026-10-05-018-ui-governance.md) — Approved (was `2026-10-05-009-ui-governance`)
- [2026-10-05 — Income scenarios and expected raises](2026-10-05-009-income-scenarios-and-raises.md) — Approved
- [2026-10-05 — Plaid sync reconciliation tests](2026-10-05-008-plaid-sync-reconciliation-tests.md) — Approved
- [2026-10-05 — Signed-in layout](2026-10-05-007-signed-in-layout.md) — Approved
- [2026-10-05 — Signed-in UI review](2026-10-05-006-signed-in-ui-review.md) — Review notes, superseded by 2026-10-05-007
- [2026-10-05 — UI review notes](2026-10-05-017-ui-review-notes.md) — Review notes, superseded by 2026-10-05-007; detail panel focus is tracked in the roadmap (was `2026-10-05-005-ui-review-notes`)
- [2026-10-05 — Accounts chart header](2026-10-05-005-accounts-chart-header.md) — Approved
- [2026-10-05 — Picker popovers](2026-10-05-004-picker-popovers.md) — Approved
- [2026-10-05 — Detail surface](2026-10-05-003-detail-surface.md) — Approved
- [2026-10-05 — Light shell plus Home](2026-10-05-002-light-shell-home.md) — Approved
- [2026-10-05 — UI direction](2026-10-05-001-ui-direction.md) — Approved
- [2026-10-04 — Income sources](2026-10-04-016-income-sources.md) — Approved

## Phase 1 — Ownership, manual data, and conventions

Closed October 4, 2026. The reports are in
[`archive/phase-1/`](archive/phase-1/). Several October 4 reports were merged
without a recorded approval. The security rule proposed in
`2026-10-04-007-security-hardening.md` was adopted on October 7 as
`.cursor/rules/security.mdc`.

## Phase 0 — Baseline

Closed October 3, 2026. The reports are in
[`archive/phase-0/`](archive/phase-0/).
