# Balance overrides

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

A followed debt can keep the person's own balance. The card labels it "Your value", shows the synced amount beside it, and offers "Use synced value". The edit form leaves that balance locked until they choose "Enter my own value". A card whose connection is not current can record today's balance with "Update balance". Sync does not clear the override.

## Decision

Setting and clearing the balance are their own actions: `PUT /api/debts/{id}/overrides/Balance` and `DELETE /api/debts/{id}/overrides/Balance`. The debt save still ignores the balance and the account while following. An amount equal to the synced balance is still an override.

A missing date on the put means today in the household time zone. "Update balance" omits the date. "Enter my own value" sends the date the person chose. Credit limit stays editable. It is not followed yet, so locking it would block a field the person still owns.

"Update balance" is on a stale card, a card whose sync failed, and a card whose bank link was removed. A current card uses the form. A missing account does not offer it. "Use synced value" is hidden when the snapshot cannot be used. Clearing the override leaves the stored amount in place. Stopping a follow still copies the snapshot only when the balance was not an override.

The line under the amount stays "Your value" and the balance date. That is the date the amount was true.

## Changes

- `DebtSyncedField` with `Balance`. `PUT` and `DELETE /api/debts/{id}/overrides/{field}`.
- The form locks the balance and its date while following. "Enter my own value" opens them. "Save my balance" writes the override. "Save changes" still writes the other terms only.
- A followed card with the person's balance shows the synced amount and "Use synced value".
- "Update balance" asks for an amount and dates it today.

## Data changes

No migration. The model matches `20261007162523_AddDebtAccountFollow`. Using the screen sets `Balance`, `BalanceAsOf`, and `BalanceOverriddenAt`. "Use synced value" clears `BalanceOverriddenAt` and leaves the stored amount. Account balances stay as they are. The local API process was already running, so it needs a restart before these routes answer.

## Agent verification

- `dotnet ef migrations has-pending-model-changes --project .\api --startup-project .\api --configuration Release`: no model changes since the last migration. The Debug build could not copy `api.exe` because that process is running.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~Debt"`: 42 passed. That covers keeping an amount that matches the snapshot, the debt save ignoring a new balance, clearing the override without copying the snapshot onto the stored date, today's date when the date is omitted, and a debt that is not following being rejected.
- `node ./scripts/run-debt-form-tests.mjs` in `frontend`: 7 passed. That covers a dated override, an update that omits the date, and which freshness states offer "Update balance".
- `eslint` on the changed frontend files passed.
- Did not click through the app.

## Manual verification

Restart the API so the new routes load. Following can change a debt's balance, its date, and `BalanceOverriddenAt`. Account balances stay as they are.

1. Follow a connected card, then choose "Keep mine as my own value" if the amounts differ. If they already match, edit the debt, choose "Enter my own value", and save the same amount.
   Expected: the card shows your amount, with "Your value" and the date on one line, and the synced amount under that. "Use synced value" is there. A toast names your balance and its date.
2. Choose "Use synced value".
   Expected: a toast says the debt is using the synced balance again. The card shows the account balance, with "Synced" and the snapshot date. "Use synced value" is gone.
3. Edit that debt.
   Expected: balance and its date are locked, with "Enter my own value". APR and the other terms still save. The followed balance does not change when you save those terms.
4. Choose "Enter my own value", enter a different amount and date, and choose "Save my balance".
   Expected: a toast names that amount and date. The fields lock again and show your amount. "Save changes" is not required for the balance. The card says "Your value" and still shows the synced amount.
5. On a followed card that says the balance is old, sync failed, or the bank link was removed, choose "Update balance", enter an amount, and save.
   Expected: the card uses that amount, dated today, with "Your value" and "Use synced value". Refresh or Reconnect is still there. Skip this step if every follow says Synced. A current card has no "Update balance". It uses the form.
6. If a followed account is missing, so the card says the bank no longer returns it.
   Expected: there is no "Update balance". Stop following is still there. Skip this step if no follow is in that state.

## Approval

Zach approved this increment on October 7, 2026, as built.

## Pending decision

None in this slice. After approval, the next roadmap item is linked-debt item 4, suggested matches.
