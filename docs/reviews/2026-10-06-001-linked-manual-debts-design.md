# Linked manual debts design

Date: October 6, 2026

## Increment

Wrote a design for letting a manually entered debt follow a connected card
or loan, so the person does not retype the balance after every payment.
Zach decided every design question the same day and approved adding the
work to the action plan. No screens, API, models, or migrations were
changed.

## Changes

- Added `docs/design/linked-manual-debts.md`. It describes the current debt
  record, the reference link, the debt summary's "Two balances" and "Use
  this balance", and the account sync, with file paths.
- It adds a second link mode, follow, next to today's reference link.
  Existing links stay reference links. Only a connected, active credit card
  or loan in the same currency can be followed.
- Sync keeps writing only account-side rows. A followed debt reads its
  synced values when shown, so a sync still cannot overwrite a debt column.
- Following syncs the balance and the credit limit only. APR, minimum,
  due date, promotional terms, months left, and the future priority, notes,
  and target payment stay the person's.
- An edit to a synced field is a labeled override, set and cleared by its
  own action, with "Use synced value". The form keeps synced fields
  read-only so a full save cannot create an override by accident.
- Freshness is one line per followed debt: current, stale, sync failing,
  disconnected, or account missing. The last balance stays visible.
- Stop following copies the last synced values onto the debt and keeps the
  account named as a reference link.
- The data model is a sketch: `AccountFollowedSince`, override timestamps
  for the balance and credit limit, a filtered unique index on the followed
  account, and `Account.CreditLimit`.
- "Questions for Zach" became "Decisions". Liabilities analysis, and Zach's
  answers on APRs and the statement balance, moved to "Future: if
  Liabilities is ever adopted". Nothing was deleted.
- `docs/roadmap.md` gains "Sync correctness and
  linked debts", between Phase 2 items 5 and 6, with six items. The
  current-work note, the Phase 2 status, the debt inventory traceability
  row, and Phase 6 item 1 point to it.
- The design does not name outside products.

## Decisions

Zach decided these on October 6, 2026.

1. Plaid Liabilities is not adopted. Following uses basic sync only. It is
   deferred, not planned.
2. A followed debt is stale after two days in the household time zone.
3. A negative synced balance on a followed revolving debt counts as $0,
   with a note.
4. When the bank link is removed, the debt stays followed and stale until
   the person chooses.
5. One account backs at most one debt.
6. If Liabilities is ever adopted, keep every APR, not only the purchase
   APR.
7. If Liabilities is ever adopted, a synced statement balance is display
   only.
8. Sync correctness (account sync tests and overlapping worker and manual
   sync) is the next engineering increment, together with the paused Plaid
   sync reconciliation tests. It does not wait for the linked-debt work.

Model changes in the follow-a-balance and credit-limit items need Zach to
generate and apply the EF Core migration.

## Data changes

None.

## Agent verification

- Documentation only. No application code, dependencies, database, or
  runtime behavior changed.
- Cited types, methods, and routes were checked against `main` at
  `485641f`.
- Application tests were not run.

## Manual verification

Waived. There is no screen to exercise.

## Pending decision

None for the design. The next engineering increment is item 1 of "Sync
correctness and linked debts" in the action plan: `PlaidAccountSyncService`
tests, overlapping worker and manual sync, and interrupted-sync timestamps,
together with the paused Plaid sync reconciliation tests.

## Correction (October 7, 2026)

The Plaid sync reconciliation tests were not paused. They were done and
approved on October 5 in
`docs/reviews/2026-10-05-008-plaid-sync-reconciliation-tests.md`. The next
increment is `PlaidAccountSyncService` tests, a guard for overlapping worker
and manual sync, and interrupted-sync timestamps.
