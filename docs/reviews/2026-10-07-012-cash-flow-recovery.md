# Cash-flow recovery

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

Phase 3 item 5. Show when each payoff removes a monthly obligation and how much recurring breathing room that creates. The payoff month still pays the debt. The minimum leaves on the next due date. While a later debt can take the cash, rollover adds no breathing room, a reclaim keeps the requested amount, and reclaiming all keeps the freed dollars. After the last debt that can take a payment stops, cash that had been rolling becomes monthly breathing room. Nothing is saved, and no screen calls it yet.

## Decision

The report lives in `api/Domain/Recovery` and reads the approved rollover comparison. It does not load or save rows. Where a Plan screen goes is still an open decision, so this slice does not add a page or an endpoint.

The rollover amounts stay as they are. Reclaimed and rolled totals still count only while a debt is open. This report is the monthly rate, including after the last debt stops. Each freed payment now also carries the date the minimum is removed. That date is the next due date counted from the first due date, so a January 31 payoff removes the minimum on February 28, and a payoff on March 31 removes it on April 30.

The obligation removed is the planned minimum. The planned extra on that debt stops with it. A last payment that was smaller, because the balance was smaller, does not reduce either amount. Shared extra is not part of the freed payment.

While a later debt is still being paid, the freed payment stays committed. Rollover keeps none of it back. Reclaiming keeps the requested amount each month, and never more than the cash that is free. Reclaiming all keeps every freed dollar. The rest stays committed. A debt still open at the month cap keeps that cash committed too.

A debt that does not pay down, or one missing a rate, minimum, or due date, does not take the cash. Its known minimum remains. The freed payment is breathing room on the date it is removed. When no debt can take another payment, freed cash that had been rolling becomes breathing room as well. Once every debt is paid off, shared extra is no longer sent and is included in that room.

A missing removal date, past the latest date the debt rules allow, is still listed. The explanation says the date falls outside the projection. A balance that is already zero is left out. A repeated debt is kept once. A row in another currency is left out. A blank currency counts.

The result always has the same three paths as the rollover comparison. The same inputs produce the same dates and the same cents.

## Changes

- `CashFlowRecovery` reads a rollover comparison and reports when each minimum is removed, what remains, and the recurring breathing room on each path.
- `PayoffFreedPayment.StartsOn` is the next due date, when that minimum is no longer paid.

## Data changes

No migration. No rows change. No screen reads this result yet. Rollover amounts are unchanged. The cash forecast and the payoff comparison are unchanged.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~CashFlowRecovery|FullyQualifiedName~PayoffRollover"`: 24 passed. The recovery cases cover a $100 store balance at 0% with a $50 minimum and $10 extra, beside a $200 card at 0% with a $25 minimum: the store's payment ends on February 15 and its minimum is removed on March 15, with the $60 still committed, and the card's minimum is removed on May 15, when recurring breathing room is $85. Reclaiming all of it sets aside $60 a month from March 15 and removes the card's minimum on September 15, and the room is $85 from then on. Reclaiming $20 of a $50 store minimum, beside a $100 card at 12% with a $25 minimum, sets aside $20 from February 15 and releases the rest when the card's minimum is removed on April 15, for $75 a month. Reclaiming all of that store minimum sets aside $50 from February 15 and removes the card's minimum on June 15. A $75 shared extra is not part of the freed $25. Once both debts are paid off, that $75 is no longer sent, and recurring breathing room is $125. A card whose payment grows the balance keeps its $10 minimum, and the store's $50 is breathing room from February 15 on every path, including a $20 reclaim. A $100,000 loan that reaches the month cap keeps the store's $20 committed, so breathing room stays $0 while the loan's $10 minimum remains. A January 31 payoff removes the minimum on February 28, and the later payoff on March 31 removes its minimum on April 30. A CAD debt is left out. A missing rate does not keep the freed cash committed. Reclaiming nothing matches rollover, and reclaiming $10,000 matches reclaiming all. A negative extra and a negative reclaim direct nothing. A repeated debt is kept once. A zero balance is left out. A $30 balance with a $50 minimum and $10 extra still frees $60, and breathing room is $70 once the later minimum is removed. The same inputs produce the same dates and the same cents. The rollover suite still passed.
- Did not click through the app. There is no screen for this slice.

## Manual verification

No migration. No screen change. Balances, bills, and income are not written.

1. There is no new page to open.
   Expected: Home, Debts, Income, Bills, and Connections look the same. Waive a click-through, or open any of those pages and confirm nothing new appeared.

## Approval

Zach approved this increment on October 7, 2026. There was no calculation on screen to click through.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 3 item 6, reproducible scenarios for changed extra payments, income loss, bonuses and windfalls, spending changes, and protected-cash targets.

## Correction

October 7, 2026. Zach decided Plan is the first primary destination. That page is in [`2026-10-07-013-plan-nav.md`](2026-10-07-013-plan-nav.md). This report still adds no payoff numbers to a screen. The app now has a Plan item in the main nav.

October 8, 2026. Zach changed the primary navigation order to Home,
Accounts, Activity, Plan. Plan remains a primary destination, but its fourth
position is intentional. The October 7 order above is historical and no longer
applies. See decision 0009.
