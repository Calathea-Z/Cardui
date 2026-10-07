# Backend type files

Date: October 3, 2026

## Increment

Moved domain and DTO data types into their own files, and added a standing
rule so those types stay out of services and other behavior classes.

## Changes

- `TransactionActivityValue`, `TransactionActivityCategoryTotal`, and
  `TransactionActivityTotals` each have a file under `api/Domain`.
- `CategoryBucket` is now `internal` in `api/Domain/Transactions/CategoryBucket.cs`.
  Only the activity calculator uses it.
- `LedgerTransaction` is in `api/Domain/Accounts/LedgerTransaction.cs`.
- `MerchantHistoryPeriodDto` is in its own file next to
  `MerchantHistoryDto`.
- The rule is in `AGENTS.md`, `.cursor/rules/backend-type-files.mdc`, and
  short pointers in the method-comment and private-method rules.
- Test doubles stay nested in the test class that uses them.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release` filtered
  to `TransactionActivityCalculatorTests` and `AccountLedgerTests`: 8
  passed, 0 failed.
- Release was used because the running debug API had `api.exe` locked.
- Did not click through the app. The move does not change behavior.

## Manual verification

No screen or data change. Spot-check the files, or waive this list.

1. Open `api/Domain/Transactions/TransactionActivityCalculator.cs`.
   Expected: the file contains only the calculator. The activity records
   and `CategoryBucket` are gone.
2. Open `api/Domain/Transactions/CategoryBucket.cs`.
   Expected: the record is `internal` and sits in its own file.
3. Open `api/Domain/Accounts/AccountLedger.cs` and `api/Domain/Accounts/LedgerTransaction.cs`.
   Expected: the ledger file contains only `AccountLedger`. The transaction
   value is in its own file.
4. Open `.cursor/rules/backend-type-files.mdc`.
   Expected: models, DTOs, domain values, and options types each get their
   own file, and they are not nested in a service or calculator.

## Remaining considerations

- Financial profile preferences are the next Phase 1 item. Mixed-currency
  totals are still unhandled.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
