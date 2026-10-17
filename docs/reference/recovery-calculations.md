# Recovery calculations

Status: Current
Updated: 2026-10-17

These are the recovery rules in `api/Domain/Recovery`, plus the card-activity
split in `api/Domain/Transactions/CardActivityRules.cs`. Money rounds to
cents away from zero through `AccountLedger.Round`. The same inputs produce
the same cents. A missing rate, minimum, or due date stays unknown. The
rules do not load or save rows, and they do not move money.

This note was checked against those types on October 17, 2026. The October 16
behavior pass was not re-run. Its counts and limits are in
[`docs/archive/2026-10-16-phase-3-behavior-qa.md`](../archive/2026-10-16-phase-3-behavior-qa.md).
Partial balances, more than one offer, a changing rate, and a promotional
expiration on an offer stay in Phase 7 of the roadmap. They are not in
`RestructuringComparisonRules`.

## Interest and one debt

`DebtInterest.ForMonth` is one month of simple interest: balance times the
annual rate, divided by 12. It is not an average daily balance and not the
remaining interest.

`DebtAmortization.Project` charges that interest, then pays the fixed minimum
plus that debt's own extra. The stored minimum is used as given, including a
known zero. A blank minimum on an installment with a balance, a rate, and a
remaining term becomes the level payment for that term. A revolving debt
with a blank minimum stays unknown. A zero rate divides the balance by the
months. A payment that rounds a cent short leaves that cent for a later
period, because the payoff is then smaller than the level payment.

Payments step monthly from the stored due date. January 31 is followed by
February 28, then March 31. A date before the caller's start is not a
payment and does not change the balance. A promotional rate applies on its
end date and on earlier dates. A blank promotional end keeps the promotional
rate. After the end date, the regular rate is used, and a missing regular
rate stops the schedule. The schedule also stops when a payment does not
reduce the balance, at 600 months, or at the caller's last date. The higher
balance stays visible. A freed minimum is not rolled onto another debt inside
this projection.

## One round of extra

`DebtPaymentAllocation.Apply` pays each minimum, then offers one pool of
extra in the order the caller supplies. Extra left after one debt is offered
to the next debt that can take it. A debt that is paid off or missing a
date, rate, or minimum is skipped. Each debt's own extra is ignored in this
round. The round does not choose avalanche order and does not roll a freed
minimum forward. Extra that no debt can take is returned.

## Payoff order

`PayoffPriority.Compare` always includes three orders: avalanche, a
recommended order, and the user-selected order. Avalanche pays the highest
rate on the first due date first. The same rate pays the smaller balance
first. A debt that cannot be calculated stays at the end. The order does not
change mid-stream when a promotional rate ends. Ties after the balance use
the name, then the debt id, then the input order.

The recommendation leaves avalanche only to remove a minimum, to bring one
revolving debt under 90 percent of its limit, or to honor a pay-first or
pay-last constraint. A minimum or utilization change has to keep more cash
than the extra interest it costs. A constraint is honored either way. Shared
extra goes to the first debt in the order that can take it, then to the next
debt in that same month when the first is paid off. Each debt still pays its
own extra. Unused shared extra is not carried to the next month. A freed
minimum is not added to that shared extra. A debt that never pays off inside
600 months is counted as month 600.

Removing a minimum moves one debt ahead of avalanche until that debt is paid
off. Lowering utilization sends shared extra to one revolving debt that is
at or above 90 percent of its limit until it is under 90 percent, then
returns to avalanche. When both a minimum and a utilization change qualify,
the larger surplus wins. An equal surplus prefers utilization. A card that
is already first in avalanche does not move. Another currency is listed and
left out. A blank currency counts. A repeated debt id is kept once. A
non-positive balance is left out.

## Rollover and breathing room

`PayoffRollover.Compare` compares three paths. Rollover is the default.
After a modeled payment brings a balance to zero, that debt's minimum and
its planned extra go to the next debt that can take them, starting the
following month. The payoff month still pays the debt that is ending.
Reclaim keeps the requested amount of that freed cash each month. Reclaiming
all keeps every freed dollar out of the next debt. Shared extra is not part
of the freed payment.

`CashFlowRecovery.Track` dates when each payoff removes a monthly obligation.
The minimum is removed on the next due date after the payoff month. While a
later debt can still take a payment, rollover keeps the freed cash
committed, reclaim keeps the requested amount, and reclaiming all keeps
every freed dollar. When no debt can take another payment, freed cash that
had been rolling becomes breathing room. Shared extra joins that room only
once every debt is paid off.

## Cash forecast

`CashForecast.Project` builds a 30-day view and 6, 12, and 18 month
horizons. The 30-day view is thirty calendar days from the start, including
the start. A horizon ends the day before the same date that many months
later. January 1 plus six months ends on June 30.

Income, bills, debt payments, and savings stay on their dates. Amounts are
not averaged onto one day. On one date the order is income, then bills, then
debt payments, then savings. A biweekly paycheck is not rewritten as a
monthly average. An irregular item appears once. A raise replaces the
payment on and after its date. The caller passes the income amounts already
chosen. The forecast records whether those amounts are the conservative
payments or the typical ones. It does not look up low pay versus typical pay.

Cash changes when income arrives and when a bill or debt payment leaves. A
savings contribution adds to the protected reserve and does not reduce the
cash total. Available cash is cash minus that reserve. Either one can stay
negative. A windfall adds cash on its date and does not change the reserve.
Included pay before an income-loss date is left out. Another currency is
left out. A blank currency counts. A schedule supplied for a debt replaces
that debt's own amortization, which is how a payoff path rolls a freed
payment into another debt.

`SavingsTarget.Plan` reserves a gap from a date, a monthly amount, or both.
It does not spend the money. The last month of a dated path trues up so the
contributions add up to the gap. A target date already past is due in full
at the start. A path that cannot finish within 600 months sets its horizon
flag and contributes nothing for that path.

`PlanCashScenarioAdjustment.Apply` is the temporary preview. It can leave
out pay for a number of months, add one windfall, replace later full months
of flexible spending, and replace the cash floor inside the starting
reserve. Cash on hand does not change. A smaller remainder of flexible
spending in the current month stays. Income loss is 0 to 18 months. A
windfall needs a positive amount and a date inside the forecast. Flexible
spending can be tried only after Plan budget already has an amount.

## Spendable cash

`SpendableRules.Project` uses the cash outlook's first 30 days. The figure
is the lowest day, not a monthly surplus. Only steady pay in the planning
currency is included. Variable and uncertain pay are left out. A windfall is
left out. An extra payment warns when it leaves ordinary spending, or a bill
or debt payment, short inside that horizon.

## One restructuring offer

`RestructuringComparisonRules.TryCompare` compares one consolidation loan,
balance transfer, or replacement loan with keeping the selected debts on
their current minimums. Each selected balance is included in full. The offer
rate stays fixed for the whole term. The term is 1 to 600 months. A lower
payment is not ranked above a higher total cost. Nothing is saved, and the
offer is not treated as approved. The comparison is not an input to the
saved decision plan.

## Card purchases and payments

A purchase counts in its category. A card payment changes cash or card debt
and is not a second copy of that spending. Pending rows and balance
reconciliations do nothing. A purchase or refund on a card changes debt and
leaves cash as it is. The same purchase on a cash account changes cash and
leaves debt as it is. A card payment is recognized from its description, or
from a credit on a credit card that reads as a payment. A bill autopay
without card language stays spending. A mortgage payment stays spending.
The phrase list is in `CardActivityRules`.

## What the Plan page does not feed in

`PlanService.GetRecoveryAsync` loads cash accounts, outlook incomes, saved
bills, the savings outlook, and incomes after contribution shares. It does
not load allocation preferences or activity transactions. A saved allocation
is a reviewable split of variable income and a windfall. Debt and savings
shares that add past 100 percent are rejected. Saving the split does not
move money, and it does not change the temporary Plan preview. Whether that
split should become an input to this calculation is still an open decision.

A repeated activity pattern stays a suggestion until the person saves the
bill. Suggestions are not in the bill list this forecast reads.
