# Clean-code follow-up

Date: October 4, 2026

## Increment

Aligned "today" on the local financial date, loaded system category ids
once per batch, filtered group sub-groups in the query, and turned merchant
keywords into a lookup list.

## Changes

- Merchant history and the transfer lookback use `FinancialDate.Today`,
  the same local date as balances, the dashboard, and manual transactions.
- Transfer repair and the Plaid uncategorized pass load system category
  ids in one query, then classify each row in memory.
- `GroupsService.GetGroupByIdAsync` loads only sub-groups visible to the
  household. The visibility rule stays in `VisibleToHousehold`.
- `TransactionCategoryClassifier` matches merchant text against an ordered
  keyword list. Bank transfers and income hints still win first.
- New rows inserted during Plaid reconcile still look up one category at
  a time. That path is one insert, not a batch already in memory.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`:
  100 passed, 0 failed, 0 skipped.
- That includes the new keyword cases, the system-category map tests, and
  the existing merchant history, transfer pairing, and household scope tests.
- Release was used so the build would not need the debug `api.exe` lock.
- Did not click through the app.

## Manual verification

Behavior should match what you already have, except merchant history and
the transfer window now follow the local date. Please run this, or waive it.

1. Open merchant history for a transaction and compare the selected period
   with the dashboard month.
   Expected: both use today's local date.
2. Sync a linked bank, if you have one.
   Expected: uncategorized transactions still receive a keyword category,
   and transfers between your accounts still pair.
3. Open a group that contains a custom sub-group.
   Expected: that sub-group is listed. A custom sub-group from another
   household is not.

## Remaining considerations

- Financial profile preferences are the next Phase 1 item. That work
  should replace `FinancialDate.Today` with the household time zone.
- Mixed-currency totals are still unhandled.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
