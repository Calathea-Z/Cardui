# Query and index access

Date: October 4, 2026

## Increment

Aligned transaction and snapshot reads with the account they belong to,
and replaced the single-column transaction indexes with the composite
the hot paths actually use.

## Audit

Household transaction and snapshot queries joined each row to its account
and then to the Plaid item. Account lists stay on that rule, because a
linked account follows the Plaid item. The large tables now take the
account ids from that rule and seek by account id.

Transaction indexes were separate columns for date, account, and archived
time. Reads filter by account, then date, and almost every row is not
archived. A standalone archived index does not help that shape. Sync still
looks up `PlaidTransactionId` by its unique index. Category filters still
use the category foreign-key index.

Merchant history compares `lower(name)` or `lower(merchant name)`, and
transaction search uses a leading-wildcard `ILIKE`. Those predicates cannot
use a normal btree index. At a household's row count they run after the
account filter. A trigram index, or a stored match key, is the follow-up
if either lookup gets slow.

Category and subgroup names are still case-sensitive in the unique indexes
and case-insensitive in the service checks. That gap is unchanged. It was
already recorded in the household-assignment review.

## Changes

- Transaction and balance-snapshot household filters resolve account ids
  first, then limit the large table to those ids.
- Transfer pairing and uncategorized categorization seek the account ids
  they already loaded.
- Balance history selects the snapshot columns the chart needs.
- A manual balance refresh reads ledger values without tracking every
  transaction, then applies unsaved tracked changes.
- Recent dashboard transactions on the same date follow creation time,
  matching the transaction list.
- Transaction indexes in the model are now `(AccountId, Date)`, a partial
  `AccountId` index where `CategoryId` is null, and the existing unique
  `PlaidTransactionId` index. The separate date, account, and archived
  indexes are removed from the model.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`:
  100 passed, 0 failed, 0 skipped.
- That includes household scope, dashboard, manual balances, transfer
  pairing, and Plaid sync.
- `20261004062852_TransactionLookupIndexes` drops the separate date,
  account, and archived indexes and creates `(AccountId, Date)` plus
  `IX_Transactions_AccountId_Uncategorized`. It does not add a second
  plain `AccountId` index.
- Zach generated that migration. `dotnet ef database update` then applied
  it to the local `cardui` database. The transaction indexes there now
  match the migration.
- Did not click through the app.

## Manual verification

The migration is applied. It only adds and drops indexes. Please run this,
or waive it.

1. Open the dashboard, the account balance chart, and the transaction list.
   Expected: the same balances, history, and transactions as before.
   Same-day recent transactions follow creation time.
2. Open merchant history for a transaction.
   Expected: the same period totals.
3. Sync a linked bank, if you have one.
   Expected: new uncategorized transactions still receive a keyword
   category, and transfers between your accounts still pair.
4. Add or edit a manual transaction on an account you entered yourself.
   Expected: today's balance still includes that transaction.

## Remaining considerations

- The local database history also contains `20261004042313_FinancialProfile`,
  and the database has a `HouseholdContributors` table. That migration is
  not in this repository. The next model change should account for it
  before a new migration is generated.
- Merchant search and `lower()` merchant history still cannot seek a btree.
- Linked accounts still follow the Plaid item when `Account.HouseholdId`
  disagrees. Sync copies the item's household onto the account, so the
  columns should match. Making `Account.HouseholdId` the only ownership
  column would let account reads seek one index. That changes the current
  ownership rule and needs its own decision.
- The query-access rule proposed below was added afterward. See
  [2026-10-04 — Backend query access](2026-10-04-003-backend-query-access.md).

## Proposed rule

Add `.cursor/rules/backend-query-access.mdc`, and a short section in
`AGENTS.md`, only if you want it:

When adding or changing a query in `api/` or `worker/`:

- A read that does not update rows uses `AsNoTracking`, or `Select`, so
  the rows are not tracked.
- A query that needs a few columns projects those columns. It does not
  load a whole related row for one field.
- A value used for every row in a loop is loaded once, before the loop.
- A filter on transactions or balance snapshots seeks by account id. The
  household rule stays on accounts.
- An index on a growing table leads with the columns the query filters
  and sorts by. A column that is almost always the same value does not
  get its own index.
- A comparison that wraps the column, such as `ToLower()` or a
  leading-wildcard search, is not a seek unless an index matches that
  expression.
