# Plaid transaction-page retrieval boundary

Date: October 3, 2026

## Increment

Added a testable boundary around Plaid `transactions/sync` page retrieval so
cursor paging and interruption can be exercised with a hand-rolled stub. No new
package was added.

`IPlaidRequestExecutor` was not enough: it still required `PlaidClient` inside
the sync loop, and the test project has no mocking library.

## Changes

- Introduced `IPlaidTransactionPageClient` / `PlaidTransactionPageClient` as a
  thin adapter over one Plaid sync page.
- Moved page-size, credentials, and response mapping into that adapter.
- `PlaidTransactionSyncService` now walks pages through the new client, then
  reconciles, persists the cursor, and runs the existing follow-up steps.
- Added focused sync-service coverage for multi-page accumulation, empty final
  pages, failed page retrieval, and cancellation between pages.

## Agent verification

- `docker compose up -d postgres`: `cardui-postgres` is running on local port
  `5433`.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlaidTransaction"`:
  9 passed, 0 failed.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --no-restore`:
  58 passed, 0 failed.
- `git diff --check`: passed.
- IDE diagnostics reported no errors in the edited files.

This increment does not change HTTP routes, UI, or stored schema. No browser
check was performed.

## Manual verification

Zach approved this increment on October 3, 2026. No application data was mutated.

## Remaining considerations

- The page client is a thin Going.Plaid adapter and is not unit-tested against
  a live or fake `PlaidClient`.
- Sync still fetches every page before reconcile. A failed later page leaves
  earlier pages unapplied and does not advance the cursor. Retry is safe because
  Plaid will resend those pages and the reconciler is idempotent.
- Idempotency, pending-to-posted replacement, removals, and user-edit
  preservation remain covered by the existing reconciler tests.
- The working tree still contains earlier uncommitted reconciler, user-edit
  flag, and migration work that is outside this increment.
