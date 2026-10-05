# Paycheck schedules and gross pay

Date: October 5, 2026

## Increment

An income source stays one payment on a cadence. The card shows the next
payday and the one after it. A monthly figure is the average of that
payment across the year, and it is not cash arriving on a date. Gross pay
is optional and separate from net. No tax estimate.

## Changes

- Upcoming dates come from the cadence and the next payment date, on or
  after today in the household time zone. Weekly steps 7 days. Biweekly
  steps 14, so a month can contain three paychecks. Monthly, quarterly, and
  yearly add months from the original day, so a month-end date returns
  after a short month. Semimonthly is two days in the month, 15 apart,
  taken from the next payment's day. A short month clamps to its last day.
  Irregular has no dates and no average.
- The Income card shows those dates as month and day, such as Oct 9, under
  Upcoming pay dates. It shows the next payday and the one after it. A
  later year includes the year. The rest of the schedule stays off this
  page.
- The monthly average is the payment times payments per year, divided by
  12. Weekly uses 52, biweekly 26, semimonthly 24, monthly 12, quarterly 4,
  and yearly 1. A $1,000 biweekly payment averages $2,166.67, not $2,000.
- Gross pay is optional. When set, it has to be greater than zero and at
  least the typical net pay. Confirming a raise that would pass gross
  clears gross and says so.
- Saved sources are the page. Add income opens the form in a panel. Gross,
  low, strong, and raises sit behind a disclosure. Editing a source with
  no gross leaves that field blank.
- The list calculates the dates and the average in the browser from the
  cadence and the next payment, so it can render before the API returns
  those fields. The API calculates the same rule.

## Data changes

`20261005180946_AddIncomeGrossPay` adds nullable `GrossPayAmount`
(`numeric(18,2)`) on `IncomeSources`. Zach applied it on October 5, 2026.
Existing pay amounts, cadences, and raises are unchanged. The date list
does not write rows.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PaycheckScheduleTests|FullyQualifiedName~IncomeSourceRulesTests|FullyQualifiedName~IncomeSourcesServiceTests"`:
  34 passed. That covers three biweekly dates in January, semimonthly on
  two days, month-end and leap day, the biweekly average, and gross pay
  below net.
- `node ./scripts/run-paycheck-schedule-tests.mjs` in `frontend`: 6 passed.
- `node ./scripts/run-income-raise-review-tests.mjs` in `frontend`: 5 passed.
  Confirming a raise clears gross when gross would be lower than the new
  typical pay.
- `pnpm exec tsc --noEmit` in `frontend` passed.
- Did not click through the app. Zach reviewed the date layout in the
  running app and asked for the two-date card and the Upcoming pay dates
  label. Those are in this increment. Gross pay was not saved against
  Postgres.

## Manual verification

The list and the blank gross field can be checked on the running app.
Zach applied `20261005180946_AddIncomeGrossPay`, which adds nullable
`GrossPayAmount` on `IncomeSources`. Restart the API if it was already
running. No other records change unless you save or remove an income
source.

1. Open Income.
   Expected: the saved sources are on the page, with Add income in the
   title row. Forvis Mazars shows Upcoming pay dates Oct 9 and Oct 23, net
   $1,900.00 each payment, and monthly average $4,116.67. Snooze and Uncle
   each show two dates a week apart, and monthly average $2,166.67. No
   longer row of dates.
2. Edit a source that has no gross pay. Open Gross, low, strong, and
   raises.
   Expected: Gross pay is empty. It does not contain the word undefined.
   Save changes without filling it. The source stays, still with no gross
   line.
3. Edit a source and set gross pay above the typical
   net pay. Save.
   Expected: the card shows that gross amount. The net amount is unchanged.
4. Set gross pay below the typical net pay and save.
   Expected: the save does not stick. The message says gross pay cannot be
   lower than the typical net pay.
5. Add a source with cadence Irregular, or edit one to Irregular.
   Expected: the card shows the next payment date and no monthly average.
6. On a phone-width window, open Add income, then a choice list and a date.
   Expected: the form is a full-screen sheet. The choice and the date use
   the app's list and calendar. Escape closes an open list first, then the
   sheet.

## Approval

Zach approved this increment on October 5, 2026, and applied
`20261005180946_AddIncomeGrossPay` the same day.

## Pending decision

Overlapping worker and manual sync remains open. The next increment is
Phase 2 item 3: bills and obligations, with amount, frequency, due date,
source account, essential or flexible, and confirmation of recurring
suggestions.
