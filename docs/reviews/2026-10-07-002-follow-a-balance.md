# Follow a balance

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

A debt can follow one connected credit card or loan. The Debts page shows that account's latest balance, and the summary counts it. Sync writes the account and its snapshots. The debt reads the latest snapshot when it is shown. Stopping the follow copies that balance onto the debt once and leaves the account linked as a reference. Existing links stay reference links.

## Decision

Follow is a second link mode. A reference link still names an account and still offers "Two balances" and "Use this balance." A follow sets `AccountFollowedSince`. One account backs at most one followed debt.

The balance in use is the latest snapshot when it is usable: dated, same currency, and small enough to store. A snapshot older than two days in the household time zone (`America/Denver`) is stale. A missing account, a removed bank link, or a failed sync is named ahead of a stale date. A negative snapshot on a followed revolving debt counts as $0, with a note for the credit. That negative amount is not stored on the debt. An installment balance below zero stays blocked, and the recorded balance stays in use.

When the balances differ, confirm offers "Use connected value" and "Keep mine as my own value." Keeping the recorded amount sets `BalanceOverriddenAt`. The card then says "Your value" and shows the synced amount beside it. "Use synced value" and "Update balance" are the next item.

On a reference link, "Use this balance" starts following when that account is eligible. The confirm says the plan will use the account balance from now on. An account that cannot be followed, such as a manual account or one another debt already follows, is still copied once.

Refresh on a stale card runs the existing bank sync. Reconnect opens Connections. A repair flow is a later item. Suggested matches and the credit limit are later items.

## Changes

- `AccountFollowedSince` and `BalanceOverriddenAt` on `Debt`. A unique index on `AccountId` covers rows that are following.
- `GET /api/debts/{id}/follow-accounts`, `POST /api/debts/{id}/follow`, and `POST /api/debts/{id}/stop-following`.
- The debt list returns the balance in use, its source, the synced amount, a credit when the card is paid ahead, and freshness. The summary totals use that balance. A followed debt does not offer the side-by-side choice. A non-current follow is counted and still included in the totals.
- Saving a followed debt ignores a new balance, date, or account. The form disables those fields. APR, minimum, and due date still save.
- Debts offers "Follow this account" when the debt already names an eligible account, and "Follow a connected account" otherwise. The list is connected cards and loans in the same currency. Confirm names both balances when they differ. Stop following stays on the card.
- A missing count in the summary names the debts and the blank field on hover, focus, or a tap. "2 missing inputs" can say "Store card and Visa need an APR."

## Data changes

`20261007162523_AddDebtAccountFollow` is applied. It adds the two timestamps and replaces `IX_Debts_AccountId` with the filtered unique index `IX_Debts_AccountId_Followed`. Existing links stay reference links because `AccountFollowedSince` is null. The model matches that migration.

Using the screen sets those timestamps when a debt starts following. Stop following may copy the last synced balance onto the debt, including $0 when a card is paid ahead. It does not change the account or its snapshots.

## Agent verification

- `dotnet ef migrations has-pending-model-changes --project .\api --startup-project .\api`: no model changes since the last migration.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~Debt"`: 39 passed. That covers a follow that reads the snapshot and leaves the stored balance, keeping the recorded balance, a second follow rejected, a card credit counted as $0 until stop following stores zero, freshness order, a summary that uses the followed balance, and choosing an eligible account balance starting a follow. A manual account is still copied once.
- `node ./scripts/run-debt-form-tests.mjs` and `node ./scripts/run-debt-summary-tests.mjs` in `frontend`: 5 and 7 passed.
- Did not click through the app.

## Manual verification

The migration is already applied. Following and stopping can change a debt's timestamps and, on stop, its stored balance. Account balances stay as they are.

1. Open Debts. On a debt that names a connected card or loan, look for "Two balances" when the amounts differ.
   Expected: the note says choosing the account balance follows that account from now on. Choose "Use this balance," then Follow. Follow uses the primary button. A toast says the debt now follows that account. The card shows the account balance, with "Synced" and the snapshot date on one line. APR, Minimum, Due, and Limit are labeled facts. A blank one says Unknown. On a manual account, the same button still copies the balance once and does not follow.
2. Choose "Follow this account" if the debt already names a connected card or loan. Otherwise choose "Follow a connected account."
   Expected: a named account opens confirm. A debt with no account opens a list of connected cards and loans in that currency. A manual account is absent. Back returns to the list, or closes when the list was skipped. Escape closes the sheet.
3. When the two amounts differ, choose "Use connected value."
   Expected: a toast says the debt now follows that account. The card shows the account balance, with "Synced" and the snapshot date on one line. The summary total uses that amount. APR, Minimum, Due, and Limit are labeled under the name. A blank one says Unknown.
4. Edit that debt, change the APR, and save.
   Expected: the APR saves. Balance, balance date, and account stay disabled. The followed balance is unchanged.
5. Stop following.
   Expected: a toast says the debt is manual again and names the balance and date that were kept. The account stays on the debt. "Two balances" can show again later if the amounts differ.
6. Follow again and choose "Keep mine as my own value."
   Expected: the card shows your amount, with "Your value" and the date on one line, and the synced amount under that. There is no "Use synced value" button yet.
7. From a second debt, try to follow the same account.
   Expected: that account is not in the list.
8. If a followed card is paid ahead, so the bank balance is negative, look at the card.
   Expected: the amount is $0 and the note says the card shows a credit and it is counted as $0. Stop following stores $0. Skip this step if no card is paid ahead.
9. If a followed card says the balance is old, choose Refresh. If it says sync failed or the bank link was removed, choose Reconnect.
   Expected: Refresh runs a sync and reloads Debts. Reconnect opens Connections. A current card has neither button. Skip this step if every follow says Synced.
10. Hover or focus a missing count in the summary, such as "2 missing inputs", "2 unknown", "2 missing limits", or "2 due dates". On a phone, tap it.
    Expected: a note names the debts and the blank field. Another tap, or a tap outside, closes it. A count with nothing to name stays plain text.

## Approval

Zach approved this increment on October 7, 2026, as built.

## Pending decision

None in this slice. After approval, the next roadmap item is linked-debt item 3, overrides. Documentation cleanup in `docs/reviews/2026-10-07-001-documentation-cleanup.md` is still awaiting review.
