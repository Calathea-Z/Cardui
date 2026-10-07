# Accounts and transactions independent of Plaid

Date: October 3, 2026

## Increment

Made accounts and transactions able to exist without a Plaid link. Existing
linked rows keep their Plaid item, external ids, and the household on that
item. Zach generated `IndependentFinancialRecords`. The migration was edited
so existing rows are marked as Plaid sync and each account copies the
household from its Plaid item, then applied.

## Changes

- `PlaidItemId`, `PlaidAccountId`, and `PlaidTransactionId` are optional.
  Existing values are unchanged. PostgreSQL still keeps those external ids
  unique, and more than one empty id is allowed.
- Each account and transaction stores `Source` and `Provenance`. Existing
  rows are `Plaid` / `PlaidSync`. The constants `Manual` and `ManualEntry`
  exist for a later manual row. The database does not default new rows to
  Plaid.
- An account with no Plaid item uses its own `HouseholdId`. A linked account
  still follows the Plaid item's household for reads and writes. The account
  also stores a copy of that household. Transactions follow the account.
- Removing a Plaid item clears the account's Plaid link instead of deleting
  the account. Nothing in the app deletes a Plaid item today.
- Plaid sync still writes the Plaid source, provenance, external id, and the
  item's household. It does not match or remove a transaction that has no
  external id.
- System categories and subgroups stay shared. Category and subgroup names
  and keys stay globally unique.

Row totals match the household assignment. Nothing was inserted or deleted.

| Rows | Count |
| --- | ---: |
| Accounts | 13 |
| Accounts still linked to a Plaid item | 13 |
| Accounts with a Plaid account id | 13 |
| Accounts whose household matches the Plaid item | 13 |
| Accounts marked Plaid / PlaidSync | 13 |
| Transactions | 1,271 |
| Transactions with a Plaid transaction id | 1,271 |
| Transactions marked Plaid / PlaidSync | 1,271 |
| Balance snapshots | 69 |
| Plaid items | 5 |

`Source` and `Provenance` were added with a temporary default so the
existing rows received `Plaid` and `PlaidSync`. That default was then
removed. The household copy is one update from `PlaidItems` inside the
same migration transaction.

## Agent verification

- `dotnet test .\Cardui.sln -c Release`: 79 passed, 0 failed. New tests
  cover a linked account still following the Plaid item, an account with no
  Plaid link visible only to its household, Plaid sync marking a new
  transaction as Plaid sync, and a removal payload leaving a transaction
  with no external id in place.
- `dotnet build .\worker\worker.csproj -c Release`: succeeded.
- Applied `IndependentFinancialRecords`. It is the latest migration in
  `__EFMigrationsHistory`.
- Recounted after apply. Totals match the table above. No account household
  disagrees with its Plaid item. The Plaid-item foreign key is set null.
  `Source` and `Provenance` have no database default.
- Did not click through the app. Did not commit.

## Manual verification

Zach confirmed on October 3, 2026 that accounts, transactions, and dashboard
totals still show the linked data.

Do not commit `frontend/.env` or `frontend/.env.local`. Do not commit the
local database backup.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- Manual account and transaction creation is the next Phase 1 item.
  Opening balances stay separate from income.
- The stored external ids and the connection record are still Plaid's.
  Another provider would need its own connection record and its own id
  columns.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
- Hobby keeps a 7-day session, has no multifactor authentication, and shows
  Clerk branding.
