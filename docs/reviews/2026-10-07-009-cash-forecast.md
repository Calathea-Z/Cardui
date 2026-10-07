# Cash forecast

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

Phase 3 item 2. A 30-day cash view and 6, 12, and 18 month forecasts of cash, debt balances, minimums still due, the protected reserve, and payoff or shortfall dates. The result names the interest and payment rules it used. Nothing is saved, and no screen calls it yet.

## Decision

The forecast lives in `api/Domain/Recovery` and calls the approved calculators. It does not load or save rows. Where a Plan screen goes is still an open decision, so this slice does not add a page or an endpoint.

The caller passes the income amounts already chosen. `ForecastIncomeBasis` records whether those amounts are the conservative payments or the typical ones. The forecast does not look up low pay versus typical pay.

The 30-day view is thirty calendar days from the start, including the start. A 6, 12, or 18 month horizon ends the day before that same date the given number of months later. January 1 plus six months ends on June 30.

Cash changes when income arrives and when a bill or debt payment leaves. A savings contribution adds to the protected reserve and does not reduce cash. Available cash is cash minus that reserve. Either one can stay negative. A reserve shortfall is reported only when a reserve is set aside and available cash is below zero.

A debt uses its amortization schedule from the start date through the horizon. A due date before the start is not replayed. The next payment is the first monthly date on or after the start, still stepped from the original due date, so January 31 is followed by February 28 and then March 31. The balance stays the balance given until that payment. Each debt keeps its own extra. A freed minimum is not rolled onto another debt.

A payment that leaves a balance the same or higher stops that debt's later payments. The higher balance and the minimum stay visible. A missing rate, minimum, or due date stays unknown and is left out of the minimum total. A row in another currency is left out. A blank currency counts.

The first day cash goes below zero, and the first later day it is back to zero or above, are milestones. The reserve uses the same pair. A debt that reaches zero records the due date and the monthly minimum that ends.

## Changes

- `CashForecast` walks the dated events and returns the 30-day view, the three horizons, the milestones, and the assumptions.
- `DebtAmortization.Project` takes an optional start date. Callers that omit it behave as before.
- Income, bills, debt payments, and savings stay on their own dates. A repeating amount is not rewritten as a monthly average.

## Data changes

No migration. No rows change. No screen reads this result yet.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~CashForecast|FullyQualifiedName~DebtAmortization"`: 28 passed. That covers three January biweekly paychecks kept at $1,000 and $13,000 of those paychecks across six months, a raise on its date, a bill before payday that reaches -$400 and recovers on payday, same-day income covering a bill, a savings contribution that raises the reserve and leaves cash unchanged, a $1,000 loan at 12% over 12 months paying $88.85 with the balance cleared on December 15, a missing rate left unknown, a promotion that stops when the later rate is unknown, a payment that grows a balance, a CAD amount left out, a January 31 debt that skips January and pays off on May 31, extra kept on that debt, and the same inputs producing the same result.
- Did not click through the app. There is no screen for this slice.

## Manual verification

No migration. No screen change. Balances, bills, and income are not written.

1. There is no new page to open.
   Expected: Home, Debts, Income, Bills, and Connections look the same. Waive a click-through, or open any of those pages and confirm nothing new appeared.

## Approval

Zach approved this increment on October 7, 2026, as built. There was no screen to click through.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 3 item 3, smart payoff prioritization.
