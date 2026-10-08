# Payoff priority

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

Phase 3 item 3. Smart payoff prioritization. Avalanche is the economic baseline. The result quantifies when removing a minimum, lowering high utilization, or honoring a selected constraint changes the recommended order. Pure avalanche and the user-selected order are always included. Nothing is saved, and no screen calls it yet.

## Decision

The comparison lives in `api/Domain/Recovery` and calls the approved calculators. It does not load or save rows. Where a Plan screen goes is still an open decision, so this slice does not add a page or an endpoint.

Avalanche pays the highest rate on the first due date first. The same rate pays the smaller balance first. A debt that cannot be calculated stays at the end. The order does not change mid-stream when a promotional rate ends.

The result always has three orders: avalanche, recommended, and user-selected. It also always has three adjustments, in this order: minimum release, utilization, and constraint. An adjustment says whether it became the recommendation, the extra interest against avalanche, the cash figure set beside that interest, the order it would use, and a plain explanation.

Shared extra goes to the first debt in the order that can take it. Each debt keeps its own extra. Unused extra is not carried to the next month. A freed minimum is not rolled onto the next debt. That rollover is Phase 3 item 4. One round is one payment on each debt, stepped monthly from its own due date. Debts with different due dates still share a round. Interest is one month of simple interest, the same rule the debt summary uses.

Removing a minimum moves one debt ahead of avalanche until that debt is paid off. It changes the recommendation only when the net minimum cash is greater than the extra interest. Net minimum cash is each minimum times the months it ends sooner, minus minimums that end later. A debt that never pays off inside 600 months is counted as month 600.

Lowering utilization moves one revolving debt that is at or above 90 percent of its limit. 90 percent is the debt summary's limit notice. Shared extra goes to that debt only until it is under 90 percent, then returns to avalanche. The cash figure is that debt's minimum times the months sooner it crosses under 90 percent. It changes the recommendation only when that cash is greater than the extra interest.

When both a minimum and a utilization change qualify, the larger surplus wins. An equal surplus prefers utilization. A card that is already first in avalanche does not move.

A pay-first or pay-last constraint is honored ahead of those comparisons, even when it costs more interest. The first constraint on a debt is the one used. A debt that is missing a rate, minimum, or due date does not move. The user-selected order is a separate alternative. An empty selection matches avalanche. Debts left off a partial list follow avalanche.

A missing rate, minimum, or due date stays unknown and is not given shared extra. A balance that is already zero is left out. A repeated debt is kept once, the first copy. A payment that leaves the balance the same or higher stops that debt, and the higher balance stays visible. A row in another currency is left out. A blank currency counts.

## Changes

- `PayoffPriority` compares avalanche, the recommended order, and the user-selected order.
- `PayoffOrdering` ranks avalanche, a user list, a single promotion, and pay-first or pay-last constraints.
- `PayoffProjection` applies one monthly round. A utilization target receives shared extra only while it stays at or above 90 percent.
- `PayoffTradeoff` turns months saved into the cash figure set beside extra interest.
- `PayoffExplanation` states whether each reason changed the recommendation.

## Data changes

No migration. No rows change. No screen reads this result yet.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PayoffPriority"`: 16 passed. That covers a $100 store card at 10% with a $25 minimum and a $500 card at 20% with a $25 minimum, with $100 of shared extra: avalanche interest is $24.02, paying the store card first costs $3.89 more and keeps $100 of minimums, so the store card moves first and is paid off on January 15 while the other card is paid off on May 15 either way. An $8,000 loan at 4% does not jump ahead of a $2,000 card at 28%. A card at 95% of its limit receives extra only until it is under 90%, which delays the higher-rate card by one month, and paying that card off completely costs more. When both reasons qualify, the larger tradeoff wins. Pay-first and pay-last constraints are honored while avalanche and the user order stay available. A CAD debt is left out. A missing rate takes no extra. A 0% promotion ranks behind a 20% card. The same rate pays the smaller balance first. January 31 is followed by February 28. A payment that grows a balance stops at $1,010. Own extra stays on its debt. A negative extra directs nothing. A repeated debt is kept once. A card already first in avalanche does not move for utilization. The same inputs produce the same orders and the same cents.
- Did not click through the app. There is no screen for this slice.

## Manual verification

No migration. No screen change. Balances, bills, and income are not written.

1. There is no new page to open.
   Expected: Home, Debts, Income, Bills, and Connections look the same. Waive a click-through, or open any of those pages and confirm nothing new appeared.

## Approval

Zach approved this increment on October 7, 2026, as built. There was no screen to click through.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 3 item 4, rolling a freed minimum into the next debt.
