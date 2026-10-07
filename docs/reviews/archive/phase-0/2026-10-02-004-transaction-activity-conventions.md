# Transaction activity conventions and dashboard accuracy

Date: October 2, 2026

## Increment

Defined and implemented consistent posted-transaction conventions for dashboard
income, spending, refunds, transfers, and pending activity. Updated automatic
categorization so a negative amount alone is no longer treated as income.

## Changes

- Added a centralized transaction-activity calculator used for monthly income,
  monthly spending, and spending-by-category totals.
- Income now requires an Income group/category assignment. Income reversals
  reduce income, with the displayed monthly total floored at zero.
- Refunds and credits outside Income and Transfers reduce their own spending
  category instead of becoming income. Each category is floored at zero before
  monthly spending is summed.
- Transfers are excluded in both directions using group semantics with system
  category-key fallback.
- Pending transactions remain visible in recent activity but are excluded from
  all monthly totals until posted.
- New imports infer Income only from recognizable income descriptions. Merchant
  refunds retain the matching expense category, while unknown incoming amounts
  default to Other.
- Documented the conventions and the limitation affecting previously stored
  category assignments in `docs/reference/transaction-activity-conventions.md`.
- Added focused calculator, classifier, and dashboard integration coverage.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --no-restore`:
  48 passed, 0 failed.
- `git diff --check`: passed.
- IDE diagnostics reported no errors in the edited files.
- Read-only browser verification confirmed that the dashboard rendered the
  recalculated totals, category amounts summed to monthly spending, transfers
  were absent from category totals, and pending transactions remained visible
  in Recent Transactions.
- The local dashboard API returned a category total equal to monthly spending
  and no Transfers category in spending.

## Manual verification

Zach tested the requested checklist on October 2, 2026 and confirmed it passed:

- Pending transactions remain visible in Recent Transactions but do not affect
  monthly totals.
- Posted refunds reduce spending rather than increasing income.
- Transfers do not affect income, spending, or category totals.
- Recognizable posted pay counts as income.

No migration or application-data mutation was introduced. The local API was
restarted to load the updated code.

## Remaining considerations

- Existing transactions previously assigned to Income are not rewritten.
  Refunds with that stored assignment still require manual recategorization.
- Transactions do not record category provenance, so a safe automated repair
  cannot currently distinguish importer assignments from user choices.
- Text-based income recognition is intentionally conservative. Users may need
  to categorize unfamiliar income descriptions manually.
