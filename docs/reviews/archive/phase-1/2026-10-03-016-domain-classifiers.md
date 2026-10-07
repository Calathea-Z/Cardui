# Domain classifiers

Date: October 3, 2026

## Increment

Moved the category and transfer text rules into `Domain`, and added a
standing rule for that split.

## Changes

- `TransactionCategoryClassifier` and `TransferTextClassifier` now live in
  `api/Domain`. Their tests moved to `tests/Cardui.Tests/Domain`.
- `TransactionCategorizationService` calls the category classifier through
  the `Domain` namespace.
- `ManualAccountBalance` stays in `api/Services` because it loads
  transactions and writes the account balance and snapshot.
- The rule is in `AGENTS.md` and
  `.cursor/rules/backend-domain-rules.mdc`. Pure rules go in `Domain`.
  Database work stays in `Services`. There is no `Helpers` folder.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release` filtered
  to the classifier tests and `TransferPairing`: 13 passed, 0 failed.
- Release was used because the running debug API had `api.exe` locked.
- Did not click through the app. The move does not change behavior.

## Manual verification

No screen or data change. Spot-check the folders, or waive this list.

1. Open `api/Domain/Transactions/TransactionCategoryClassifier.cs` and
   `api/Domain/Transactions/TransferTextClassifier.cs`.
   Expected: both types are in `Cardui.Api.Domain`.
2. Open `api/Services`.
   Expected: `ManualAccountBalance.cs` is the only file in that folder.
   The two classifiers are gone.
3. Open `.cursor/rules/backend-domain-rules.mdc`.
   Expected: pure rules go in `Domain`, database work stays in `Services`,
   and a `Helpers` folder is not used.

## Remaining considerations

- Financial profile preferences are the next Phase 1 item. Mixed-currency
  totals are still unhandled.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
