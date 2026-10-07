# Method responsibility

Date: October 3, 2026

## Increment

Split backend methods that were doing more than one job, moved merchant
history periods into `Domain`, and added a standing rule so each new
method stays one job.

## Changes

- `PlaidTransactionReconciler` now loads existing rows, inserts, updates,
  and removes through separate methods. `UpsertAsync` only chooses which
  of those to run. `TransactionUpsertResult` is in its own file.
- Merchant history periods and the merchant match key live in
  `api/Domain`. `TransactionsService` loads the rows and maps the result.
- `TransferPairingService` loads categories, accounts, and candidates in
  their own methods, then runs the existing pair, promote, and repair steps.
- `AccountsService.ReconcileBalanceAsync` builds the adjustment
  transaction in `CreateAdjustmentTransaction`.
- Create and update share the future-date and opening-date checks.
- `PlaidAccountSyncService` fetches, loads, applies, and deactivates
  through separate methods.
- The rule is in `AGENTS.md` and
  `.cursor/rules/backend-method-responsibility.mdc`. Each method in
  `api/` and `worker/` does one job. A pure calculation stays in `Domain`.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`:
  93 passed, 0 failed, 0 skipped.
- That includes the new `MerchantHistoryPeriods` tests and the existing
  merchant history, reconciler, transfer pairing, and manual account tests.
- Release was used so the build would not need the debug `api.exe` lock.
- There is no automated test for `PlaidAccountSyncService`. That sync
  path was split without a behavior change, and it was not clicked
  through.

## Manual verification

Behavior should match what you already have. Please run this, or waive it.

1. Open a transaction with merchant history and switch monthly, quarterly,
   and yearly.
   Expected: gap periods still appear, the current period stays selected,
   and totals match the transactions you see.
2. On a manual account, add or edit a transaction with a future date, then
   with a date before the opening date.
   Expected: the same two date errors as before.
3. Reconcile a manual account to a statement balance that does not match.
   Expected: one balance-reconciliation transaction, and the balance
   matches the statement.
4. If a bank is linked, sync that item.
   Expected: accounts and transactions still update. An account the bank
   no longer returns becomes inactive.

## Remaining considerations

- Financial profile preferences are the next Phase 1 item. Mixed-currency
  totals are still unhandled.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
- `TransactionCategoryClassifier.GetCategoryKey` is still one decision
  written as a chain of keyword checks. A keyword table can wait.
