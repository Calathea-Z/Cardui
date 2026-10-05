# Income sources

Date: October 4, 2026

## Increment

A household can record income sources: net pay for one payment, cadence,
next payment date, contributor, and reliability. Low, typical, and strong
scenarios and expected raises remain for the rest of Phase 2 item 1.

## Changes

- An income source belongs to the household. The form asks for net pay for
  one payment, after taxes and deductions. Cadence is weekly, every two
  weeks, twice a month, monthly, quarterly, yearly, or no set schedule.
  Reliability is steady, variable, or uncertain. Those values are stored and
  returned as their names.
- The next payment date is a calendar date. A source can name a household
  contributor, or no one. A new source uses the planning currency at the time
  it is saved. A later edit keeps that currency.
- Remove deletes an income source after confirmation. The row is gone, and
  the name can be used again. Balances stay unchanged. Accounts and
  transactions still archive, because those rows affect balances.
- Removing a contributor clears that person from their income sources. The
  sources stay.
- The Income page is in the sidebar and the phone tab bar. A saved amount
  is shown as net each payment. The page does not compute a monthly
  equivalent, and dashboard income is unchanged. Choice lists use the shared
  `Select`. Dates use `DateField`, the same dark sheet, including the
  account opening date, statement date, and transaction date.
- `20261005025504_AddIncomeSources` adds the `IncomeSources` table. Cadence
  and reliability are `character varying`. The household link deletes a
  source with the household. The contributor link becomes empty when that
  person is removed. Indexes are on household plus name, and on contributor.
- `20261005031455_DropIncomeSourceArchivedAt` drops the unused
  `ArchivedAt` column from `IncomeSources`. Account and transaction archive
  columns stay.

## Data changes

`20261005025504_AddIncomeSources` is applied. It created an empty
`IncomeSources` table. `20261005031455_DropIncomeSourceArchivedAt` is
applied after that. It drops only `IncomeSources.ArchivedAt`. Other rows
stay.

The checklist below adds income sources in the household you already have.
Remove deletes that income source. Removing a contributor, if you do that
step, deletes that contributor and clears the link on any source that named
them.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter IncomeSourcesServiceTests`:
  6 passed after Remove replaced archive. The earlier full run was 206
  passed, before that change and before the migration. The tests use an
  in-memory database. The full suite was not re-run after Remove.
- The tests cover one payment stored in the planning currency, a duplicate
  name, a contributor from another household, household isolation, delete
  freeing the name, an edit that keeps the original currency, and a removed
  contributor leaving the source in place. Amount, cadence, date, and
  reliability checks are covered.
  JSON writes `Biweekly` and `Uncertain` and rejects a numeric `0`.
- Frontend typecheck and lint on the income page passed.
- `dotnet ef database update` applied `20261005025504_AddIncomeSources`, then
  `20261005031455_DropIncomeSourceArchivedAt`. Release was used so the
  build would not need the debug `api.exe` lock. The second migration drops
  only `IncomeSources.ArchivedAt`.
- Did not click through the app. Zach confirmed Remove deletes the source.

## Manual verification

Restart the API before these steps so it loads this build. The database
already has the new table. These steps use the household you already have.
Dashboard income should stay the same throughout.

1. Open Income from the sidebar. On a phone, open it from the tab bar.
   Expected: the page is empty and the form is ready for a new source.
   Cadence and reliability start unset. The note says a new amount uses the
   planning currency, and a biweekly paycheck is not turned into a monthly
   amount. The amount field is labeled Net pay and says to enter net pay for
   one payment, after taxes and deductions. Open the next payment date.
   Expected: the calendar is the same dark sheet as the other choices, with
   the month and weekday initials. The browser's white calendar does not
   open. Open cadence. Expected: that list is the same dark sheet.
2. Add a source named "Paycheck", with a net pay amount, Every two weeks,
   a next payment date, No contributor, and Steady.
   Expected: it appears as that amount net each payment, with Every two weeks,
   the date, No contributor, and Steady. Dashboard income is unchanged.
3. Choose Edit, change the amount, and save.
   Expected: the new amount is shown as net each payment.
4. Choose Remove and confirm.
   Expected: Paycheck is gone. Dashboard income is unchanged.
   Zach confirmed this on October 4, 2026.
5. Add Paycheck again.
   Expected: the name is available.
6. On Household, add a contributor and assign them on an income source.
   Then remove that contributor.
   Expected: the income source remains, with no contributor. Dashboard
   totals stay the same.
7. Start a manual transaction, or open a manual account's opening date.
   Expected: that date uses the same dark calendar.

## Approval

Zach approved this increment on October 4, 2026. Remove was confirmed.
The rest of the checklist was not repeated.

## Pending decision

None for this capture. The rest of Phase 2 item 1 is still open: low,
typical, and strong scenarios, and expected raises with effective dates.
