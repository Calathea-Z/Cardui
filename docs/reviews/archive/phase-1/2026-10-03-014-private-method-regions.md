# Private method regions

Date: October 3, 2026

## Increment

Grouped every backend private method into a `#region Private Methods`, and
added a standing rule so new private methods go in that region.

## Changes

- Twenty types in `api/` now end with `#region Private Methods`. The region
  contains only private methods. Fields, constructors, and public methods
  stay above it.
- `TransactionsService` had merchant-history helpers between public methods.
  Those helpers now sit with the other private methods.
- `GetPeriodLabels` and `NormalizePagination` were missing summaries. Both
  now have one.
- `TransactionActivityCalculator` keeps its private `CategoryBucket` record
  outside the region. It is a type, not a method.
- Types with no private methods were left unchanged, including controllers,
  `worker/`, and generated migrations.
- The rule is in `AGENTS.md`,
  `.cursor/rules/backend-private-methods.mdc`, and a pointer in
  `.cursor/rules/backend-method-comments.mdc`.

## Agent verification

- Checked that every private method in `api/` and `worker/` is inside one
  `Private Methods` region, and that no public member is inside a region.
- `dotnet build .\api\api.csproj -c Release`: succeeded, 0 warnings.
- Did not run the test suite. The regions do not change behavior.
- Did not click through the app.

## Manual verification

No screen or data change. Spot-check the regions, or waive this list.

1. Open `api/Services/Implementations/AccountsService.cs`.
   Expected: public methods come first. Every private helper is inside
   `#region Private Methods`, and the region closes before the class ends.
2. Open `api/Services/Implementations/TransactionsService.cs`.
   Expected: `UpdateTransactionCategoryAsync` follows
   `GetMerchantHistoryAsync`. Merchant-history helpers, pagination, and
   the other private methods are together in the region.
3. Open `api/Domain/Transactions/TransactionActivityCalculator.cs`.
   Expected: the four private methods are in the region. `CategoryBucket`
   stays after `#endregion`.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- Financial profile preferences are the next Phase 1 item. Mixed-currency
  totals are still unhandled.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
