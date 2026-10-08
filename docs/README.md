# Cardui documentation

Status: Current
Updated: 2026-10-07

## Status

This block is the single source for current work. Update it in the same
change as each review report. Reviews and phase status paragraphs record
history. They do not say what is next.

**Now**

- Plan screen item 1 in [`roadmap.md`](roadmap.md) is approved in
  [`reviews/2026-10-07-014-plan-recovery.md`](reviews/2026-10-07-014-plan-recovery.md):
  the debt-free summary, Finish your plan with warnings, the What you owe
  today donut, the stacked balance chart, the minimums and breathing room
  chart, the payoff order, and the Rollover or Keep freed payments switch.
  Design: [`design/plan-page.md`](design/plan-page.md). More UI polish on
  Plan comes as later work brings it up.

**Next**

1. Plan screen item 2: the cash outlook on Plan, following the switch.
   Settle the stored due date before today first; see "To settle when
   item 2 starts" in the design.
2. Phase 3 item 6: reproducible scenarios for changed extra payments,
  income loss, bonuses and windfalls, spending changes, and protected-cash
  targets. Detail panel keyboard focus stays tracked and is not scheduled.

**Tracked, not scheduled**

- Detail panel keyboard focus. See "Tracked UI follow-ups" in
  [`roadmap.md`](roadmap.md).

**Open decisions**

- Hosting and production configuration. Until a public host exists,
  `Plaid:WebhookUrl` stays empty, so Plaid cannot report a revoked bank
  connection.

## Map

| Path | What it is |
| --- | --- |
| [`roadmap.md`](roadmap.md) | Product direction, the phase backlog, and what each phase built |
| [`decisions/`](decisions/) | Short decision records for product and architecture choices (listed below) |
| [`design/linked-manual-debts.md`](design/linked-manual-debts.md) | Linked manual debts design. Follow a balance, balance overrides, suggested matches, credit limit, and reconnect are approved |
| [`design/plan-page.md`](design/plan-page.md) | Plan page design: the answer, Finish your plan, debt charts, the path switch, and the cash outlook. Approved; item 1 is built and awaiting review |
| [`design/ui-direction.md`](design/ui-direction.md) | UI direction, implemented; the enforceable rules are in `.cursor/rules/ui-governance.mdc` |
| [`reference/transaction-activity-conventions.md`](reference/transaction-activity-conventions.md) | Income, spending, refund, transfer, and pending rules |
| [`checklists/original-mvp-acceptance.md`](checklists/original-mvp-acceptance.md) | Manual walkthrough template and the Phase 0 record |
| [`reviews/`](reviews/README.md) | One report per increment, with an index |
| [`archive/`](archive/) | Superseded documents kept for reference |

Decision records:

- [0001. Clerk Hobby sign-in and one household per owner](decisions/0001-clerk-sign-in-and-household-owner.md)
- [0002. Records independent of Plaid, and Plaid optional](decisions/0002-records-independent-of-plaid.md)
- [0003. Single planning currency](decisions/0003-single-planning-currency.md)
- [0004. A debt stays separate from a connected account](decisions/0004-debt-separate-from-connected-account.md)
- [0005. One light shell and three primary destinations](decisions/0005-light-shell-and-three-destinations.md)
- [0006. One-shot sync worker on a cron schedule](decisions/0006-one-shot-cron-worker.md)
- [0007. Plan is first in the main nav](decisions/0007-plan-first-in-main-nav.md)
- [0008. Forward-looking charts live on Plan](decisions/0008-forward-charts-on-plan.md)

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
