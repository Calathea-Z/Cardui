# Financial profile preferences

Date: October 3, 2026

## Increment

Added household preferences for one planning currency, a time zone, and
named contributors. Totals include a blank currency and leave any other
currency out, with a notice, until conversion exists. Zach generated
`FinancialProfile` and applied it to the API database.

## Changes

- A household has a planning currency and a time zone. New households, and
  the existing household, use USD and `America/Denver`. Today and the current
  month follow that time zone, including the date used when a bank sync
  records a balance.
- A new manual account uses the planning currency when no currency is
  entered. A linked account keeps the currency supplied by the bank.
- Dashboard and account totals, including the balance chart, include an
  amount when its currency is blank or matches the planning currency. A
  different currency stays listed and is left out of the total. The dashboard
  and accounts pages name the currencies that were left out.
- Contributors are names stored for the household. Each one can be shown or
  hidden. Hiding a contributor does not change balances. The signed-in owner
  is not added as a contributor, and a contributor cannot sign in.
- The sidebar account area links to Household, where the currency, time zone,
  and contributors are edited. Those choice lists use the shared `Select`
  primitive, the same themed list as account type and transaction filters.
  A native select is not used, because its open menu stays the browser's
  white list.
- Standing guidance now says to decide whether a reused control should be a
  primitive, and whether a large control should stay small or use a library.
  A new library still needs approval first.

`PlanningCurrency` is `character varying(3)`, required, with a database
default of `USD`. `TimeZoneId` is `character varying(64)`, required, with a
database default of `America/Denver`. `HouseholdContributors` is a new table.
Existing household rows received those defaults. Nothing was inserted or
deleted by the migration.

The one extra manual account, its three transactions, and its balance
snapshot were already present from the previous checklist. Linked rows were
unchanged.

| Rows | Count |
| --- | ---: |
| Households | 1 |
| Households planning in USD | 1 |
| Households using America/Denver | 1 |
| Contributors | 0 |
| Accounts | 14 |
| Accounts still linked to a Plaid item | 13 |
| Accounts marked Plaid / PlaidSync | 13 |
| Manual accounts | 1 |
| Transactions | 1,274 |
| Transactions marked Plaid / PlaidSync | 1,271 |
| Manual transactions | 3 |
| Balance snapshots | 70 |
| Plaid items | 5 |

## Agent verification

- `dotnet test .\Cardui.sln -c Release`: 108 passed, 0 failed. The tests
  cover a foreign currency staying out of net worth, income, and spending, a
  blank currency staying in those totals, the household time zone choosing
  the calendar date, contributor changes staying inside one household, and a
  manual account defaulting to the planning currency.
- `dotnet build .\worker\worker.csproj -c Release`: succeeded.
- Frontend typecheck, frontend tests, and lint on the changed frontend files
  succeeded.
- `FinancialProfile` is the latest migration in `__EFMigrationsHistory`.
- Recounted after apply. Linked totals match the previous review. The extra
  manual rows match the previous checklist.
- Did not click through the app. Did not commit.

## Manual verification

These steps use the household you already have. Adding and removing a
contributor does not change balances. The test wallet from the previous
checklist is still active, so totals still include it.

1. In the sidebar, under the account area, choose Household. Open Planning
   currency, then Time zone.
   Expected: planning currency is USD. Time zone is Mountain — Denver. Each
   list is the same dark sheet used for account type, not the browser's
   white menu. No contributors are listed. The page says totals use one
   currency.
2. Choose Save profile without changing the currency or time zone.
   Expected: the page stays on USD and Mountain — Denver.
3. Add a contributor named "Test person" with Shown checked.
   Expected: the name appears. Dashboard net worth, income, and spending do
   not change.
4. Clear Shown and choose Save on that contributor.
   Expected: the person remains, unchecked. Totals stay the same.
5. Choose Remove on that contributor.
   Expected: the contributor list is empty.
6. Open Dashboard and Accounts.
   Expected: there is no notice that a currency was left out. Totals match
   what they were before this checklist.

Do not commit `frontend/.env` or `frontend/.env.local`. Do not commit the
local database backup.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- A blank currency is treated as the planning currency. An explicit other
  currency is left out of totals until conversion exists.
- The household time zone defaults to `America/Denver`.
- Contributor visibility does not hide accounts or change totals.
- The stored external ids and the connection record are still Plaid's.
  Another provider would need its own connection record and its own id
  columns.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
- CSV import is the next Phase 1 item.
- Hobby keeps a 7-day session, has no multifactor authentication, and shows
  Clerk branding.
