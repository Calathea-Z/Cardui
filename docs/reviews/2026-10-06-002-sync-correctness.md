# Sync correctness

Date: October 6, 2026
Status: Approved 2026-10-07
PR:

## Increment

Account sync now has tests. One Plaid item syncs at a time, whether the daily worker or Sync now starts it. An interrupted sync keeps the last success time and records the failure. No schema change.

## Decision

One sync holds an item from the start until it completes, fails, or 30 minutes pass. A second sync does not fetch accounts, advance the transaction cursor, or replace today's snapshot. It reports that a sync is already running. The worker logs that skip and does not count it as a failure. On Connections, a toast says "Already syncing" and does not mark the bank as just synced. The Sync now button shows a spinner while the request runs.

Starting a sync no longer clears `LastSyncCompletedAt`. A cancel or a Plaid error sets `LastSyncFailedAt` and leaves the last success. A start that is still open after 30 minutes is stored as "Sync was interrupted." before the next sync begins. A later success clears that failure.

Thirty minutes is long enough for one item's account and transaction sync, and short enough that a dead process does not block the rest of the day.

On PostgreSQL the claim is a conditional update, so the worker and Sync now cannot both win. The test database cannot run that update, so the tests apply the same rule to the loaded row.

`POST /api/plaid/{id}/sync-accounts` and `POST /api/plaid/{id}/sync-transactions` are not the worker or Sync now path. They can still run beside a full sync.

## Changes

- `PlaidAccountSyncService` tests: balances are copied, a blank Plaid account id is skipped, today's snapshot is replaced in the household time zone, an account the bank stops returning is marked inactive, and an archived account stays archived.
- Account fetch goes through `IPlaidAccountsClient`, so those tests do not call Plaid.
- `SyncPlaidItemAsync` claims the item, keeps the last success when the sync does not finish, and returns `alreadyRunning` when another sync holds it.
- Connections reports sync results in a toast. The Sync now button keeps its place and shows a spinner while the request runs. A finished sync says what was added, modified, and removed. A click that finds a sync already running says "Already syncing." Accounts refresh leaves a bank that is already syncing to that sync, then reloads the page. Sync all, beside Connect account, syncs each bank in order and toasts one total. Disconnect confirms in the shared dialog before it removes the bank login.
- The worker skips an item that is already syncing.

## Data changes

None.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlaidAccountSyncService|FullyQualifiedName~PlaidItemSync"`: 14 passed. That covers the account-sync cases above, a second sync leaving the snapshot and timestamps alone, a cancel keeping the last success and recording the interruption, an abandoned start recorded before the new fetch and cleared by the later success, and a Plaid error keeping the last success.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~Plaid|FullyQualifiedName~HouseholdFinancialScope"`: 54 passed.
- `dotnet build .\worker\worker.csproj -c Release` succeeded.
- Prettier was applied to the Connections page, the Accounts page, and the Plaid types.
- `pnpm exec tsc --noEmit` in `frontend` failed on stale generated routes in `.next` for `budgets`, `institutions`, and `transactions`. It reported no error in the sync files.
- Did not click through the app. The conditional update itself was not run on PostgreSQL.

## Manual verification

No data changes. The daily worker does not need to be running for step 1.

1. Open Connections and choose Sync now on one bank.
   Expected: The button shows a spinning circle in the same place and the banks do not move. A toast then says "Sync finished" and what was added, modified, and removed. That bank shows a success time.
2. Start two syncs of the same bank that overlap, such as Sync now in two browser tabs.
   Expected: One toast says "Sync finished." The other says "Already syncing" and that the click did nothing. Skip the worker for this. It finishes too quickly to catch by hand.
3. Choose Disconnect, then cancel. Choose Disconnect again and confirm Remove bank link.
   Expected: The first click opens the confirm dialog and the card stays as it was. Cancel leaves the bank. Confirm removes the login, the card leaves the list, and a toast says the accounts and transactions stay.
4. With more than one bank, choose Sync all.
   Expected: The header button and the current bank's Sync now button show a spinning circle. The other banks stay put and their buttons wait. One toast then says "Sync finished" and the added, modified, and removed totals.

## Approval

Zach approved this increment on October 7, 2026, as built.

## Pending decision

None. The next roadmap item is linked-debt item 2, follow a balance. Documentation cleanup in `docs/reviews/2026-10-07-001-documentation-cleanup.md` is still awaiting review.
