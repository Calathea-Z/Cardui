# Payoff rollover

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

Phase 3 item 4. Roll a paid-off debt's freed minimum and planned extra into the next debt by default. The freed cash starts the month after the modeled payment ends. The same result shows reclaiming some or all of that cash for savings or spending. Nothing is saved, and no screen calls it yet.

## Decision

The comparison lives in `api/Domain/Recovery` and calls the approved calculators. It does not load or save rows. Where a Plan screen goes is still an open decision, so this slice does not add a page or an endpoint.

The approved payoff comparison and the cash forecast stay as they are. They still do not roll a freed minimum. This comparison is the projection that does.

Rollover is the default. After a payment brings a balance to zero, that debt's planned minimum and the extra planned for that debt roll into the next debt that can take them. The next debt is the first one in the order that is still open, which may be earlier in the list than the debt that just ended. The month that pays the debt off still pays that debt. The freed cash starts the following round.

The amount is the planned minimum plus that debt's own extra, even when the last payment was smaller because the balance was smaller. Shared extra is not part of it. Shared extra still goes to the first debt that can take it, including leftover in the same month. An open debt keeps its own extra until its payment ends. Unused shared extra and unused rolled cash are not carried to the next month.

An empty order uses avalanche. Debts left off a partial list follow avalanche. The order decides which open debt receives the rolled cash.

A debt that never pays off does not free its payment. A stopped debt is not reopened to receive a roll. A missing rate, minimum, or due date stays unknown and is not given rolled cash. A balance that is already zero is left out. A repeated debt is kept once. A row in another currency is left out. A blank currency counts.

The result always has three paths: rollover, reclaim, and reclaim all. Reclaim keeps the requested amount of freed cash each month for savings or spending, and the rest rolls. An amount above that month's freed cash keeps all of it. Zero keeps nothing, so that path matches rollover. Reclaim all keeps every freed dollar out of the next debt. Reclaimed and rolled totals count only while a debt is still open. Breathing room after the last debt stops is Phase 3 item 5.

Interest is one month of simple interest, the same rule the debt summary uses. One round is one payment on each debt, stepped monthly from its own due date.

## Changes

- `PayoffRollover` compares rollover, a monthly reclaim, and reclaiming all freed cash.
- `PayoffRolloverProjection` applies one monthly round, then adds a freed minimum and planned extra on the following round.
- `PayoffRolloverExplanation` states what rolls, what is kept for savings or spending, and the interest gap against rollover.

## Data changes

No migration. No rows change. No screen reads this result yet. The payoff comparison and the cash forecast are unchanged.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PayoffRollover"`: 12 passed. That covers a $100 store balance at 0% with a $50 minimum and $10 extra, beside a $200 card at 0% with a $25 minimum: the store ends on February 15, the $60 does not move that month, and the card is paid off on April 15 with $100 rolled. Reclaiming all of it leaves the card until August 15 and keeps $360. A $50 store balance at 0% with a $50 minimum beside a $100 card at 12% with a $25 minimum costs $1.78 of interest when the $50 rolls, $1.98 when $20 a month is kept, and $2.58 when all of it is kept. The card is paid off on March 15 unless all of the freed cash is kept, which moves it to May 15. Putting Alpha ahead of Beta pays Alpha off on April 15 and Beta on May 15; the reverse order swaps those dates. A $75 shared extra is not added to the freed $25. A $30 balance with a $50 minimum and $10 extra still frees $60. A card whose payment grows the balance to $1,010 frees nothing. A January 31 payoff rolls on the March 31 round, not on February 28. A CAD debt is left out. A missing rate takes no rolled cash. Reclaiming nothing matches rollover, and reclaiming $10,000 matches reclaiming all. A negative extra and a negative reclaim direct nothing. A repeated debt is kept once. A zero balance is left out. The same inputs produce the same payments and the same cents.
- Did not click through the app. There is no screen for this slice.

## Manual verification

No migration. No screen change. Balances, bills, and income are not written.

1. There is no new page to open.
   Expected: Home, Debts, Income, Bills, and Connections look the same. Waive a click-through, or open any of those pages and confirm nothing new appeared.

## Approval

Zach approved this increment on October 7, 2026, as built. There was no screen to click through.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 3 item 5, tracking when each payoff removes a monthly obligation and how much recurring breathing room it creates.
