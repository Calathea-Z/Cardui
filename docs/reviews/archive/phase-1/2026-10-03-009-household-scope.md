# Household scope for API reads and writes

Date: October 3, 2026

## Increment

Scoped API reads and writes to the household resolved from the signed-in
Clerk user. Zach generated and applied `ScopeHouseholdData`. Existing
financial rows stay unassigned, so they no longer appear in the app.

## Changes

- Every API route except `/api/health` requires a verified Clerk session.
  `POST /api/households/current` still creates the household for that owner.
- After that, the API loads the household from the Clerk user id on the
  server. Callers cannot choose a household id.
- Accounts, transactions, balance history, dashboard totals, Plaid items,
  and transfer pairing read and change only that household. A guessed id
  from another household is not found.
- New bank links are stored on the signed-in household. The Plaid link
  token uses that household id.
- The daily worker still syncs every connected item. Each sync pairs
  transfers only inside that item's household. Items with no household stay
  in their own bucket and are not returned to a signed-in household.
- System groups, subgroups, and categories stay shared. Custom categories
  and subgroups belong to the household that created them. Their names are
  still unique across the whole database.
- `ScopeHouseholdData` adds a nullable `HouseholdId` on `PlaidItems`,
  `Categories`, and `SubGroups`, with a foreign key to `Households` and an
  index on each column. It does not assign or delete existing rows.

## Agent verification

- `dotnet test .\Cardui.sln -c Release`: 76 passed, 0 failed. New tests
  cover household-only account, transaction, dashboard, and Plaid reads;
  hidden custom categories and subgroups; rejection of another household's
  category delete; and transfer pairing that stays inside the bound
  household.
- `dotnet build .\worker\worker.csproj -c Release`: succeeded.
- Did not run the worker. Did not commit.

## Manual verification

Zach applied `ScopeHouseholdData` and, on October 3, 2026, confirmed the
signed-in dashboard shows no accounts, no spending, and no transactions.
That is the expected result. The previous rows are still in PostgreSQL and
are not assigned to the household yet.

A second household was not signed in during this check. Isolation between
two households is covered by the tests above.

Do not commit `frontend/.env` or `frontend/.env.local`.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- Assign the existing unscoped rows to the owner's household, with a
  backup, row-count checks, and a recovery procedure. Review the global
  category and subgroup name keys during that migration.
- System category edits still change the shared catalog.
- Hobby keeps a 7-day session, has no multifactor authentication, and shows
  Clerk branding.
