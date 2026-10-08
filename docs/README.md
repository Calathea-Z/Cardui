# Cardui documentation

Status: Current
Updated: 2026-10-08

## Status

This block is the single source for current work. Update it in the same
change as each review report. Reviews and phase status paragraphs record
history. They do not say what is next.

**Now**

- Responsive UI/UX governance is updated and awaiting review in
  [`reviews/2026-10-08-009-responsive-ui-governance.md`](reviews/2026-10-08-009-responsive-ui-governance.md).
  New and substantially changed screens now lead with the user's question and
  relevant action, define desktop and mobile arrangements intentionally, and
  reduce repeated surfaces and warning noise without hiding material financial
  qualifications. This is documentation only; no Plan redesign was
  implemented.
- The Plan page refinement is implemented, manually checked, and awaiting
  review in
  [`reviews/2026-10-08-008-plan-page-refinement.md`](reviews/2026-10-08-008-plan-page-refinement.md).
  Overview, Cash outlook, and Debt payoff now separate the existing report;
  the deferred extra-payment layout is included without adding calculations
  or saved scenarios.
- The bounded Phase 2 UX closure is implemented and awaiting review in
  [`reviews/2026-10-08-007-phase-2-ux-closure.md`](reviews/2026-10-08-007-phase-2-ux-closure.md).
  Plan now leads with cash affordability and a next action, explains material
  source freshness, and reconciles zero-balance debt terms. Home, Accounts,
  Activity, Plan remains the approved primary order.
- The source Phase 2 product and UX audit is approved in
  [`reviews/2026-10-08-006-phase-2-ux-audit.md`](reviews/2026-10-08-006-phase-2-ux-audit.md).
- Phase 2 item 8, simplified Living, is approved in
  [`reviews/2026-10-08-005-simplified-living.md`](reviews/2026-10-08-005-simplified-living.md).
  The contribution and cleanup migrations are applied. Phase 2 is complete.
- A signed-in UX assessment of Plan, Debts, and unfinished Savings is
  awaiting review in
  [`reviews/2026-10-08-003-planning-ux-review.md`](reviews/2026-10-08-003-planning-ux-review.md).
  It proposes improvements; no application changes or roadmap reorder are approved.
- Phase 2 item 7, savings, is approved in
  [`reviews/2026-10-08-002-savings.md`](reviews/2026-10-08-002-savings.md).
  `20261008143017_AddEverydaySpendingAndCashToKeep` is applied.
- The handoff rule is still awaiting review in
  [`reviews/2026-10-08-001-handoff-roadmap-order.md`](reviews/2026-10-08-001-handoff-roadmap-order.md).

**Next**

1. Review and approve the responsive UI/UX governance, the Plan page
   refinement, and separately the bounded Phase 2 UX closure. None of those
   reviews is marked approved yet.
2. After those approvals, resume the rest of Phase 3 item 6: income loss,
   windfalls, spending changes, and protected-cash targets. Detail panel
   keyboard focus stays tracked and is not scheduled. The separate Planning
   UX review remains a proposal until Zach approves a change from it.

**Tracked, not scheduled**

- Detail panel keyboard focus. See "Tracked UI follow-ups" in
  [`roadmap.md`](roadmap.md).

**Open decisions**

- Hosting and production configuration. Until a public host exists,
  `Plaid:WebhookUrl` stays empty, so Plaid cannot report a revoked bank
  connection.

## Map

| Path                                                                                             | What it is                                                                                                                                                 |
| ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`roadmap.md`](roadmap.md)                                                                       | Product direction, the phase backlog, and what each phase built                                                                                            |
| [`decisions/`](decisions/)                                                                       | Short decision records for product and architecture choices (listed below)                                                                                 |
| [`design/linked-manual-debts.md`](design/linked-manual-debts.md)                                 | Linked manual debts design. Follow a balance, balance overrides, suggested matches, credit limit, and reconnect are approved                               |
| [`design/plan-page.md`](design/plan-page.md)                                                     | Plan page design: Overview, focused Cash outlook, chart-led Debt payoff, and the temporary scenario controls. The three-view refinement is awaiting review |
| [`design/savings.md`](design/savings.md)                                                         | Savings design: cash to keep, an emergency goal, and named goals under Saving for. Monthly living spending is edited on Living                             |
| [`design/living.md`](design/living.md)                                                           | Plan budget design at `/living`: unequal contribution shares, one monthly flexible-spending amount, and a monthly affordability check                      |
| [`design/ui-direction.md`](design/ui-direction.md)                                               | Shared UI rationale and examples; the light shell is implemented and the enforceable rules are in `.cursor/rules/ui-governance.mdc`                        |
| [`reference/transaction-activity-conventions.md`](reference/transaction-activity-conventions.md) | Income, spending, refund, transfer, and pending rules                                                                                                      |
| [`checklists/original-mvp-acceptance.md`](checklists/original-mvp-acceptance.md)                 | Manual walkthrough template and the Phase 0 record                                                                                                         |
| [`reviews/`](reviews/README.md)                                                                  | One report per increment, with an index                                                                                                                    |
| [`archive/`](archive/)                                                                           | Superseded documents kept for reference                                                                                                                    |

Decision records:

- [0001. Clerk Hobby sign-in and one household per owner](decisions/0001-clerk-sign-in-and-household-owner.md)
- [0002. Records independent of Plaid, and Plaid optional](decisions/0002-records-independent-of-plaid.md)
- [0003. Single planning currency](decisions/0003-single-planning-currency.md)
- [0004. A debt stays separate from a connected account](decisions/0004-debt-separate-from-connected-account.md)
- [0005. One light shell and three primary destinations](decisions/0005-light-shell-and-three-destinations.md)
- [0006. One-shot sync worker on a cron schedule](decisions/0006-one-shot-cron-worker.md)
- [0007. Historical Plan-first navigation](decisions/0007-plan-first-in-main-nav.md) — superseded by 0009
- [0008. Forward-looking charts live on Plan](decisions/0008-forward-charts-on-plan.md)
- [0009. Home, Accounts, Activity, then Plan](decisions/0009-home-accounts-activity-plan-order.md)

Setup is in the root [`README.md`](../README.md). Agent rules are in the
root [`AGENTS.md`](../AGENTS.md) and `.cursor/rules/`.

## Document conventions

- File and folder names are lowercase kebab-case.
- Every document outside `reviews/` starts with `Status:` (`Current`,
  `Draft`, `Approved`, `Implemented`, `Template`, `Historical`, or
  `Superseded by …`) and `Updated: YYYY-MM-DD`. Add `Supersedes:` when it
  replaces another document.
- A superseded document moves to `archive/` or is deleted. Git keeps the
  history.
- A decision record in `decisions/` is `NNNN-short-name.md`, 10–25 lines,
  and links the review that holds the detail. Code conventions stay in
  `.cursor/rules`, not in decision records.
