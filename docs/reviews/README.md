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

## Phase 2, linked debts, and the UI plan

Newest first.

- [2026-10-07 — Domain folders](2026-10-07-002-domain-folders.md) — Awaiting review
- [2026-10-07 — Documentation cleanup](2026-10-07-001-documentation-cleanup.md) — Awaiting review
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

Archived in [`archive/phase-1/`](archive/phase-1/). Newest first.

- [2026-10-04 — Backend enums](archive/phase-1/2026-10-04-015-backend-enums.md) — Approved
- [2026-10-04 — Frontend types](archive/phase-1/2026-10-04-014-frontend-types.md) — Approved
- [2026-10-04 — Frontend layers](archive/phase-1/2026-10-04-013-frontend-layers.md) — Approved
- [2026-10-04 — Frontend comment rule](archive/phase-1/2026-10-04-012-frontend-comment-rule.md) — Approved
- [2026-10-04 — Frontend method comments](archive/phase-1/2026-10-04-011-frontend-method-comments.md) — Approved
- [2026-10-04 — CSV import UX](archive/phase-1/2026-10-04-010-csv-import-ux.md) — Approved
- [2026-10-04 — Optional Plaid startup](archive/phase-1/2026-10-04-009-optional-plaid.md) — Approved
- [2026-10-04 — DotRush hover documentation](archive/phase-1/2026-10-04-018-dotrush-hover-docs.md) — Merged in PR #11; approval not recorded (was `2026-10-04-009-dotrush-hover-docs`)
- [2026-10-04 — CSV import](archive/phase-1/2026-10-04-008-csv-import.md) — Approved
- [2026-10-04 — DotRush solution pin](archive/phase-1/2026-10-04-017-dotrush-solution-pin.md) — Merged in PR #9; approval not recorded (was `2026-10-04-008-dotrush-solution-pin`)
- [2026-10-04 — Security hardening](archive/phase-1/2026-10-04-007-security-hardening.md) — Merged in PR #10; approval not recorded. Its proposed rule was adopted October 7 as `.cursor/rules/security.mdc`
- [2026-10-04 — Clerk page protection](archive/phase-1/2026-10-04-006-clerk-page-protection.md) — Merged in PR #8; approval not recorded
- [2026-10-04 — Frontend conventions](archive/phase-1/2026-10-04-005-frontend-conventions.md) — Merged in PR #8; approval not recorded
- [2026-10-04 — Frontend cleanup](archive/phase-1/2026-10-04-004-frontend-cleanup.md) — Merged in PR #8; approval not recorded. Its pending decision was resolved by 2026-10-04-005
- [2026-10-04 — Backend query access](archive/phase-1/2026-10-04-003-backend-query-access.md) — Merged in PR #7; approval not recorded
- [2026-10-04 — Query and index access](archive/phase-1/2026-10-04-002-query-and-index-access.md) — Merged in PR #7; approval not recorded
- [2026-10-04 — Clean-code follow-up](archive/phase-1/2026-10-04-001-clean-code-follow-up.md) — Merged in PR #7; approval not recorded
- [2026-10-03 — Financial profile preferences](archive/phase-1/2026-10-03-018-financial-profile.md) — Approved (was `2026-10-03-013-financial-profile`)
- [2026-10-03 — Method responsibility](archive/phase-1/2026-10-03-017-method-responsibility.md) — Merged in PR #7; approval not recorded
- [2026-10-03 — Domain classifiers](archive/phase-1/2026-10-03-016-domain-classifiers.md) — Merged in PR #7; approval not recorded
- [2026-10-03 — Backend type files](archive/phase-1/2026-10-03-015-backend-type-files.md) — Merged in PR #7; approval not recorded
- [2026-10-03 — Private method regions](archive/phase-1/2026-10-03-014-private-method-regions.md) — Approved
- [2026-10-03 — Backend method comments](archive/phase-1/2026-10-03-013-backend-method-comments.md) — Approved
- [2026-10-03 — Manual accounts, transactions, and balance reconciliation](archive/phase-1/2026-10-03-012-manual-accounts-and-transactions.md) — Approved
- [2026-10-03 — Accounts and transactions independent of Plaid](archive/phase-1/2026-10-03-011-independent-financial-records.md) — Approved
- [2026-10-03 — Assign existing rows to the household](archive/phase-1/2026-10-03-010-assign-household-rows.md) — Approved
- [2026-10-03 — Household scope for API reads and writes](archive/phase-1/2026-10-03-009-household-scope.md) — Approved
- [2026-10-03 — Clerk sign-in and household owner](archive/phase-1/2026-10-03-008-clerk-household-owner.md) — Approved
- [2026-10-03 — Phase 1 sign-in decision](archive/phase-1/2026-10-03-007-phase-1-sign-in-decision.md) — Approved

## Phase 0 — Baseline

Archived in [`archive/phase-0/`](archive/phase-0/). Newest first.

- [2026-10-03 — Phase 0 baseline close](archive/phase-0/2026-10-03-006-phase-0-baseline-close.md) — Approved
- [2026-10-03 — CI production builds](archive/phase-0/2026-10-03-005-ci-production-builds.md) — Approved
- [2026-10-03 — CI for API tests, frontend tests, and lint](archive/phase-0/2026-10-03-004-ci-tests-and-lint.md) — Approved
- [2026-10-03 — Balance history carries the last known balance](archive/phase-0/2026-10-03-003-balance-history-carry-forward.md) — Approved
- [2026-10-03 — Preserve transaction user edits during Plaid sync](archive/phase-0/2026-10-03-002-preserve-transaction-user-edits.md) — Approved
- [2026-10-03 — Plaid transaction-page retrieval boundary](archive/phase-0/2026-10-03-001-plaid-transaction-page-boundary.md) — Approved
- [2026-10-02 — Transaction activity conventions and dashboard accuracy](archive/phase-0/2026-10-02-004-transaction-activity-conventions.md) — Approved (Zach confirmed the checklist)
- [2026-10-02 — Dashboard spending and consistent financial totals](archive/phase-0/2026-10-02-003-dashboard-spending-and-financial-totals.md) — Approved (Zach confirmed)
- [2026-10-02 — Development setup and original-MVP acceptance](archive/phase-0/2026-10-02-002-development-setup-and-mvp-acceptance.md) — Approved (Zach reviewed)
- [2026-10-02 — Recovery roadmap direction](archive/phase-0/2026-10-02-001-recovery-roadmap-direction.md) — Approved (Zach confirmed)
