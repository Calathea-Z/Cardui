# Manual accounts, transactions, and balance reconciliation

Date: October 3, 2026

## Increment

Added manual account and transaction creation, editing, archiving, and
balance reconciliation. An opening balance is stored on the account, so it
is not income. Zach generated `ManualFinancialRecords`. It was applied to
the API database.

## Changes

- A manual account has a name, type, optional subtype, opening balance, and
  opening date. The opening balance is the starting balance. It is not a
  transaction.
- Cash and investment balances fall when money leaves. Credit card and loan
  balances are the amount owed, so a purchase increases them. Pending and
  archived transactions do not change the balance. Transactions before the
  opening date are rejected.
- A statement match compares that calculated balance with the statement. A
  difference is saved as a manual transaction with provenance
  `BalanceReconciliation`. Income, spending, and transfer pairing ignore it.
- Archiving an account hides its balance from totals and the chart. Its
  transactions stay in activity until those transactions are archived.
  Archived transactions can be listed with the Archived status and restored.
- Linked accounts keep the name and balance supplied by the bank. They can
  be archived. A manual transaction on a linked account counts in activity
  and does not replace the bank balance. The next Plaid sync does not clear
  `ArchivedAt`.
- The accounts page can add a manual account or connect Plaid, and can open
  an account to edit, reconcile, or archive it. Transactions can be added,
  and a manual entry's name and amount can be edited.
- Account, category, and account-type choices open the themed list used by
  the other pickers. During the manual pass, the Add transaction account
  menu was the browser's white list on Windows, so those fields were switched
  before the checklist was finished.

`OpeningBalance` is `numeric(18,2)`, required, with a database default of
`0`. `OpeningBalanceDate` is nullable. `ArchivedAt` is nullable on accounts
and transactions. Existing rows received `0`, an empty opening date, and no
archive time. Their bank balances were not recalculated.

Row totals match the previous recount. Nothing was inserted or deleted.

| Rows | Count |
| --- | ---: |
| Accounts | 13 |
| Accounts still linked to a Plaid item | 13 |
| Accounts with a Plaid account id | 13 |
| Accounts with opening balance 0 | 13 |
| Accounts with no opening date | 13 |
| Accounts not archived | 13 |
| Accounts marked Plaid / PlaidSync | 13 |
| Transactions | 1,271 |
| Transactions not archived | 1,271 |
| Transactions with a Plaid transaction id | 1,271 |
| Transactions marked Plaid / PlaidSync | 1,271 |
| Balance snapshots | 69 |
| Plaid items | 5 |

## Agent verification

- `dotnet test .\Cardui.sln -c Release`: 87 passed, 0 failed. That run was
  before the migration was applied. The tests cover the ledger signs, a
  reconciliation adjustment staying out of income and spending, a manual
  account visible only to its household, and a linked account keeping its
  bank balance.
- `dotnet build .\worker\worker.csproj -c Release`: succeeded.
- Frontend typecheck, frontend tests, and lint on the changed frontend files
  succeeded.
- Applied `ManualFinancialRecords`. It is the latest migration in
  `__EFMigrationsHistory`.
- Recounted after apply. Totals match the table above.
- Did not click through the app. Did not commit.

## Manual verification

Zach confirmed on October 3, 2026 that all seven steps passed.

These steps add a test account and transactions in the local database.
Archive the account when finished if you do not want it in the totals.

1. On Accounts, choose Add, then Enter manually. Name it "Test wallet",
   type Cash, opening balance 100, opening date today.
   Expected: it appears under Cash at $100. Dashboard monthly income does
   not include that $100.
2. On Transactions, add a money-out transaction of $20 on Test wallet,
   uncategorized.
   Expected: the wallet balance is $80. Monthly spending includes $20.
   Monthly income is unchanged.
3. Add a money-in transaction of $40 on Test wallet with the Income
   category.
   Expected: the wallet balance is $120. Monthly income includes $40.
4. Open Test wallet and match a statement balance of $100 for today.
   Expected: the balance becomes $100. A Balance reconciliation row appears
   with an Adjustment mark. Monthly income and spending stay the same.
5. Open the $20 transaction and archive it.
   Expected: it leaves the main list. Spending no longer includes it. The
   wallet balance increases by $20. Status: Archived lists it, and Restore
   puts it back.
6. Archive Test wallet.
   Expected: it leaves Cash and appears under Archived. Totals no longer
   include its balance. The income transaction still counts until that
   transaction is archived. Restore puts the wallet back.
7. Open a linked account.
   Expected: the sheet says the name and balance come from the bank. Archive
   is available. There is no form to rename it.

Do not commit `frontend/.env` or `frontend/.env.local`. Do not commit the
local database backup.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- A new manual account records today's balance for the chart. It does not
  rebuild a snapshot for every day back to the opening date.
- `OpeningBalance` keeps a database default of `0`.
- The stored external ids and the connection record are still Plaid's.
  Another provider would need its own connection record and its own id
  columns.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
- Financial profile preferences are the next Phase 1 item. Mixed-currency
  totals are still unhandled.
- Hobby keeps a 7-day session, has no multifactor authentication, and shows
  Clerk branding.
