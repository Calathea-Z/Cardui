# Preserve transaction user edits during Plaid sync

Date: October 3, 2026

## Increment

Reviewed the existing uncommitted work that keeps a user's transaction date,
category, and notes when Plaid syncs again. No new code was written in this
review.

## Changes

- `Transaction` has `IsDateUserEdited` and `IsCategoryUserEdited`.
- Saving transaction details marks both flags. Saving only the category marks
  the category flag, including an explicit choice to leave it uncategorized.
- `PlaidTransactionReconciler` updates bank-owned fields and leaves a
  user-edited date, a user-chosen category, and notes in place.
- A repeated Plaid transaction updates the existing row. A pending transaction
  that posts keeps the same row and its user edits. A removed transaction
  deletes the stored row.
- Migration `20261003050300_PreserveTransactionUserEdits` adds both columns as
  non-nullable booleans defaulting to false. Existing rows stay unmarked.

## Agent verification

- Read the model, migration, snapshot, reconciler, transaction update methods,
  and their tests.
- The same code passed earlier in this session:
  `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --no-restore`
  — 58 passed, 0 failed. That run included the reconciler and transaction-update
  tests.
- `dotnet ef migrations list --project .\api --startup-project .\api --no-build`
  shows `20261003050300_PreserveTransactionUserEdits` in the local migration
  history. No pending migration was listed.

## Manual verification

Zach confirmed on October 3, 2026 that this migration was already applied and
the local database is up to date. The migration list above matches that. Zach
approved this increment on October 3, 2026, then tested it and confirmed the
change is working.

## Remaining considerations

- Edits made before this migration are not marked, so the next sync can still
  replace those dates. Categories that already have a value are kept. Notes are
  kept because sync never writes them.
- The local database already has these columns.
