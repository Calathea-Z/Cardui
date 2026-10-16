# Cardui documentation

Status: Current
Updated: 2026-10-16

## Status

This block is the single source for current work. Update it in the same
change as each review report. Reviews and phase status paragraphs record
history. They do not say what is next.

**Now**

[Fact basis](reviews/2026-10-16-013-fact-basis.md) was approved on October
16, 2026. It is Phase 4 item 5. Planning inputs are labeled as a user
statement, an imported fact, an estimate, or an unverified suggestion. An
inferred recurring expense stays a suggestion until the person saves the
bill. No model or price is chosen. Items 6–9 have not started. Detail panel
keyboard focus stays tracked. The
[October 15 UX assessment](reviews/2026-10-15-006-phase-3-ux-review.md)
stays as written.

**Next**

1. Phase 4 item 6, display proposed changes for review before accepting a
   plan revision. Handle edits made outside chat and stale model context
   with version checks. Do not start it in the review that approved item 5.
   Leave the planning-model settings blank until a review asks for a live
   call.

**Tracked, not scheduled**

- Detail panel keyboard focus. See "Tracked UI follow-ups" in
  [`roadmap.md`](roadmap.md).

**Open decisions**

- Hosting and production configuration. Until a public host exists,
  `Plaid:WebhookUrl` stays empty, so Plaid cannot report a revoked bank
  connection.
- Which planning model to use, and what it costs. Item 1 calls the model
  id from configuration and does not choose a price.

## Map

- [Fact basis](reviews/2026-10-16-013-fact-basis.md): Phase 4 item 5,
  approved October 16, 2026.
- [Useful questions](reviews/2026-10-16-012-useful-questions.md): Phase 4
  item 4, approved October 16, 2026.
- [Narrow tools](reviews/2026-10-16-011-narrow-tools.md): Phase 4 item 3,
  approved October 16, 2026.
- [Persisted conversations](reviews/2026-10-16-010-persisted-conversations.md):
  Phase 4 item 2, approved October 16, 2026. `20261016190413_AddConversations`
  is applied.
- [Planning model](reviews/2026-10-16-009-planning-model.md): Phase 4 item 1,
  approved October 16, 2026.
- [Phase 3 close](reviews/2026-10-16-008-phase-3-close.md): closed October 16,
  2026. The [Phase 3 UX reassessment](reviews/2026-10-16-003-phase-3-ux-reassessment.md)
  sequence is approved. Increment 1 is approved in
  [Plan next month first](reviews/2026-10-16-004-plan-next-month-first.md).
  Increment 2 is approved in
  [One way to apply a change](reviews/2026-10-16-005-one-way-to-apply.md).
  Increment 3 is approved in
  [Remaining Plan edges](reviews/2026-10-16-006-remaining-plan-edges.md).
  The [October 15 handoff](design/phase-3-ux-handoff.md)
  stays as the approved increments 1–3.

| Path                                                                                             | What it is                                                                                                                                                 |
| ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`roadmap.md`](roadmap.md)                                                                       | Product direction, the phase backlog, and what each phase built                                                                                            |
| [`decisions/`](decisions/)                                                                       | Short decision records for product and architecture choices (listed below)                                                                                 |
| [`design/linked-manual-debts.md`](design/linked-manual-debts.md)                                 | Linked manual debts design. Follow a balance, balance overrides, suggested matches, credit limit, and reconnect are approved                               |
| [`design/plan-page.md`](design/plan-page.md)                                                     | Plan page design: Overview, Try changes, and Your plan, with cash, debt, and offer as supporting views. The next-month layout is approved                     |
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
