# Cardui documentation

Status: Current
Updated: 2026-10-16

## Status

This block is the single source for current work. Update it in the same
change as each review report. Reviews and phase status paragraphs record
history. They do not say what is next.

**Now**

[Payoff priority and partial reclaim](reviews/2026-10-16-002-payoff-priority-and-reclaim.md)
was approved on October 16, 2026. [Working scenario and saved versions](reviews/2026-10-16-001-working-scenario-and-versions.md)
was approved on October 16, 2026. The [Phase 3 UX assessment](reviews/2026-10-15-006-phase-3-ux-review.md)
stays as written.

**Next**

1. Phase 3 behavior QA remains pending. Do not start Phase 4. Detail panel
   keyboard focus remains tracked. This does not close Phase 3.

**Tracked, not scheduled**

- Detail panel keyboard focus. See "Tracked UI follow-ups" in
  [`roadmap.md`](roadmap.md).

**Open decisions**

- Hosting and production configuration. Until a public host exists,
  `Plaid:WebhookUrl` stays empty, so Plaid cannot report a revoked bank
  connection.

## Map

- [Phase 3 UX handoff](design/phase-3-ux-handoff.md): proposed workflow,
  three reviewable increments, and the paste-ready Cursor prompt.

| Path                                                                                             | What it is                                                                                                                                                 |
| ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`roadmap.md`](roadmap.md)                                                                       | Product direction, the phase backlog, and what each phase built                                                                                            |
| [`decisions/`](decisions/)                                                                       | Short decision records for product and architecture choices (listed below)                                                                                 |
| [`design/linked-manual-debts.md`](design/linked-manual-debts.md)                                 | Linked manual debts design. Follow a balance, balance overrides, suggested matches, credit limit, and reconnect are approved                               |
| [`design/plan-page.md`](design/plan-page.md)                                                     | Plan page design: Overview, Try changes, and Your plan, with cash, debt, and offer as supporting views. The October 15 grouping is approved                    |
| [`design/savings.md`](design/savings.md)                                                         | Savings design: cash to keep, an emergency goal, and named goals under Saving for. Monthly living spending is edited on Living                             |
| [`design/living.md`](design/living.md)                                                           | Plan budget design at `/living`: unequal contribution shares, one monthly flexible-spending amount, and a monthly affordability check                      |
| [`design/allocation.md`](design/allocation.md)                                                   | Allocation setting: saved variable-income and windfall splits that can be reviewed and do not move money                                                    |
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
- [0005. One light shell](decisions/0005-one-light-shell.md)
- [0006. One-shot sync worker on a cron schedule](decisions/0006-one-shot-cron-worker.md)
- [0007. Historical Plan-first navigation](decisions/0007-plan-first-in-main-nav.md) — superseded by 0009
- [0008. Forward-looking charts live on Plan](decisions/0008-forward-charts-on-plan.md)
- [0009. Home, Accounts, Activity, then Plan](decisions/0009-home-accounts-activity-plan-order.md)
- [0010. Plan setup and Settings](decisions/0010-plan-setup-and-settings.md)

Setup is in the root [`README.md`](../README.md). Agent rules are in the
root [`AGENTS.md`](../AGENTS.md) and `.cursor/rules/`.

## Document conventions

- File and folder names are lowercase kebab-case.
- Every document outside `reviews/` starts with `Status:` (`Current`,
  `Draft`, `Approved`, `Accepted`, `Implemented`, `Template`, `Historical`,
  or `Superseded by …`) and `Updated: YYYY-MM-DD`. Add `Supersedes:` when it
  replaces another document. A decision record uses `Accepted`.
- A superseded document moves to `archive/` or is deleted. Git keeps the
  history. A numbered decision stays in `decisions/` so the sequence stays
  intact, and its status says what replaced it.
- A decision record in `decisions/` is `NNNN-short-name.md`, 10–25 lines,
  and links the review that holds the detail. Code conventions stay in
  `.cursor/rules`, not in decision records.
