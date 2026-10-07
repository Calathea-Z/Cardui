# Cardui documentation

Status: Current
Updated: 2026-10-07

## Status

This block is the single source for current work. Update it in the same
change as each review report. Reviews and phase status paragraphs record
history. They do not say what is next.

**Now**

- Documentation cleanup, in progress.

**Next**

1. Sync correctness, item 1 of "Sync correctness and linked debts" in
   [`roadmap.md`](roadmap.md): `PlaidAccountSyncService` tests, a guard for
   overlapping worker and manual sync, and interrupted-sync timestamps. The
   Plaid transaction reconciliation tests are already done
   ([`reviews/2026-10-05-008-plaid-sync-reconciliation-tests.md`](reviews/2026-10-05-008-plaid-sync-reconciliation-tests.md)).
2. Linked-debt items 2–6 in the same roadmap section.
3. Phase 2 item 6: monthly category targets.

**Tracked, not scheduled**

- Detail panel keyboard focus. See "Tracked UI follow-ups" in
  [`roadmap.md`](roadmap.md).

**Open decisions**

- Where Plan (`/budgets`) goes when it has a real screen.
  `.cursor/rules/ui-governance.mdc` keeps three primary destinations and no
  fourth tab until Zach decides otherwise.
- Hosting and production configuration. Until a public host exists,
  `Plaid:WebhookUrl` stays empty, so Plaid cannot report a revoked bank
  connection.

## Map

| Path | What it is |
| --- | --- |
| [`roadmap.md`](roadmap.md) | Product direction, the phase backlog, and what each phase built |
| [`design/linked-manual-debts.md`](design/linked-manual-debts.md) | Linked manual debts design, approved and not built |
| [`design/ui-direction.md`](design/ui-direction.md) | UI direction, implemented; the enforceable rules are in `.cursor/rules/ui-governance.mdc` |
| [`reference/transaction-activity-conventions.md`](reference/transaction-activity-conventions.md) | Income, spending, refund, transfer, and pending rules |
| [`checklists/original-mvp-acceptance.md`](checklists/original-mvp-acceptance.md) | Manual walkthrough template and the Phase 0 record |
| [`reviews/`](reviews/README.md) | One report per increment, with an index |
| [`archive/`](archive/) | Superseded documents kept for reference |

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
