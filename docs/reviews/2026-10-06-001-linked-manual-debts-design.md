# Linked manual debts design

Date: October 6, 2026

## Increment

Wrote a design note for letting a manually entered debt follow a connected
card or loan, so the person does not retype the balance after every payment.
The note is for review. No screens, API, models, or migrations were changed.

## Changes

- Added `docs/design/linked-manual-debts.md`. It describes the current debt
  record, the reference link, the debt summary's "Two balances" and "Use
  this balance", and the account sync, with file paths.
- It proposes a second link mode, follow, next to today's reference link.
  Existing links stay reference links. Only a connected, active credit card
  or loan in the same currency can be followed, by one debt at most.
- Sync keeps writing only account-side rows. A followed debt reads its
  synced values when shown, so a sync still cannot overwrite a debt column.
- Ownership is per field. The balance and credit limit can follow without
  Liabilities. Minimum, due date, statement balance, and APR need it.
  Promotional terms, months left, and the future priority, notes, and target
  payment stay the person's.
- An edit to a synced field is a labeled override, set and cleared by its
  own action, with "Use synced value". The form keeps synced fields
  read-only so a full save cannot create an override by accident.
- Freshness is one line per followed debt: current, stale, sync failing,
  disconnected, or account missing. The last balance stays visible.
- Stop following copies the last synced values onto the debt and keeps the
  account named as a reference link.
- The data model is a sketch for Zach: `AccountFollowedSince`, one override
  timestamp per synced field, a filtered unique index on the followed
  account, `Account.CreditLimit`, and an account-side Liabilities row only if
  approved.
- Plaid Liabilities coverage, cost, consent, and freshness are listed as
  things to confirm before deciding, not as assumptions.
- Slice 0 is sync correctness: `PlaidAccountSyncService` tests, overlapping
  worker and manual sync, and interrupted-sync timestamps. Six further
  slices follow.
- The note does not name outside products. The action plan is unchanged
  until Zach reviews.

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

Review `docs/design/linked-manual-debts.md`. The questions at its end need
answers, first whether Liabilities is in scope and whether slice 0 lands
next regardless. Overlapping worker and manual sync remains open. Phase 2
item 6 stays next in the action plan until Zach decides.
