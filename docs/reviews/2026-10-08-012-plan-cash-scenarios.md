Date: 2026-10-08
Status: Approved 2026-10-08
PR:

# Plan cash scenarios

## Increment

The rest of the temporary Phase 3 item 6 previews on Plan: months without
pay, a one-time windfall, a different flexible-spending amount, and a
different cash-to-keep floor. They sit with the approved extra payment under
**Try a scenario**. Nothing is saved. Stored variable-income and windfall
allocation preferences stay for a later slice.

## Decision

These follow the approved extra-payment pattern: tried on the page, cleared
when leaving Plan, and rejected when the amount cannot be forecast.

- Months without pay leaves out included pay from today for that many months,
  then pay resumes. Zero keeps every paycheck. The range is 0 to 18.
- A windfall needs a positive amount and a date from today through the
  18-month forecast. It stays in cash and does not pay a debt or raise the
  reserve.
- Flexible spending replaces later full months equal to the saved amount. A
  smaller remainder this month stays. Blank keeps the saved amount. Zero
  removes those full months. The field waits until Plan budget has a saved
  amount.
- Cash to keep replaces only the floor inside protected savings. Other
  set-aside money stays. Blank keeps the saved floor.
- Income loss, a windfall, spending, and the floor change cash only. Payoff
  order stays the same. Extra still changes the payoff path.
- Leaving Plan clears every temporary field. A negative amount is not sent.
  While a request runs or fails, the page says the visible forecast is still
  the previous one.

## Changes

- Domain: `PlanCashScenario`, `PlanCashScenarioRules`, and
  `PlanCashScenarioAdjustment`. The forecast accepts `OmitIncomeBefore` and
  one windfall. Savings outlook exposes the stored cash floor so a preview
  can replace only that part of the reserve.
- API: `GET /api/plan/recovery` takes the temporary query through
  `PlanScenarioQuery`. The response echoes the preview fields and the saved
  cash floor. No migration.
- Frontend: `PlanCashScenarioFields`, `planScenario`, and `usePlanScenario`
  replace the extra-only hook. Copy names an applied cash preview in the
  scenario summary, cash outlook, spending line, and assumptions.
- Design: `docs/design/plan-page.md` records the temporary-scenario rules.
- Tests: domain fixtures for each preview and the reject rules. Frontend
  parser and copy tests.

## Data changes

No migration. No schema change. No rows change. Opening the page reads the
plan and writes nothing. Temporary amounts live in the page until you leave.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlanCashScenario"`:
  7 passed. Income loss omits January pay and resumes with the March raise at
  typical pay and without raises at low pay. A windfall raises cash and leaves
  the payoff date and reserve unchanged. Spending replaces a full month and
  keeps a smaller remainder. The cash floor replaces only its share of the
  reserve. Reject rules cover a negative amount, months outside 0–18, a
  windfall without a date, spending before Plan budget is set, and a date past
  the forecast.
- Earlier focused recovery filters also compiled cleanly before the income-loss
  day-list assertion was fixed.
- `node ./scripts/run-plan-chart-tests.mjs`: 32 passed. `node ./scripts/run-plan-response-tests.mjs`:
  3 passed. Prettier and eslint on the changed Plan files: clean.
- Browser: restarted the API so the new response fields load. Signed-in Plan
  shows Months without pay, One-time windfall, Flexible spending, and Cash to
  keep under Try a scenario. Automation could not finish apply clicks because
  the expanded 30-day values chart intercepted the fields. Full apply/clear
  checks await Zach.

Not run: the full .NET suite and `tsc`.

## Manual verification

Restart the API if it was running before this change. Leaving Plan clears the
preview.

1. Open Plan and open **Try a scenario**.
   Expected: Extra monthly payment is still there. Months without pay,
   One-time windfall with a date, Flexible spending, and Cash to keep are
   visible. Flexible spending help names the saved Plan budget amount. Cash to
   keep help names the saved floor.
2. Enter `2` for Months without pay and leave the field or press Enter.
   Expected: The closed summary names 2 months without pay. Near-term cash is
   lower where pay was omitted. Debt payoff order is unchanged.
3. Enter a windfall amount, choose a date today or later inside the 18-month
   range, and leave the amount field.
   Expected: Cash rises on that date. Protected savings do not rise. The
   summary names the amount and date.
4. Enter a lower Flexible spending amount and leave the field.
   Expected: Later full living-spending months use that amount. A smaller
   remainder already due this month stays. Cash outlook sources name the
   preview amount.
5. Enter a different Cash to keep amount and leave the field.
   Expected: Available cash after protection changes. The cash total on
   Accounts does not change just from this field.
6. Clear the temporary fields back to blank or zero.
   Expected: The page returns to the saved plan.
7. Leave Plan and open it again.
   Expected: Every temporary field is cleared.
8. Enter a windfall amount without a date, or a negative spending amount.
   Expected: The field asks again. The plan on screen does not change.

## Approval

Approved by Zach on October 8, 2026.

## Pending decision

None in this slice. After approval, remaining Phase 3 item 6 work is stored
variable-income and windfall allocation preferences. Phase 3 items 7–11 have
not started.
