# Recovery calculations

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

Phase 3 item 1. Dated cash-flow events, debt amortization, minimums, extra-payment allocation, and savings targets. The same inputs produce the same cents. A missing rate, minimum, or due date stays unknown. Nothing is saved, and no screen calls these rules yet.

## Decision

The rules live in `api/Domain/Recovery`. They do not load or save rows, so they are domain calculators. A later forecast can call them.

Interest is one month of simple interest on the balance at the start of the period: balance times the annual rate, divided by 12, rounded to cents away from zero. That is the same amount the Debts summary already shows. It is not an average daily balance.

A debt payment lands on the next due date, then monthly from that date. January 31 is followed by February 28, then March 31. The stored minimum is paid when it is known, including a known zero. A blank minimum on an installment with a balance, a rate, and a remaining term becomes the level payment for that term. A revolving debt with a blank minimum is not given one. The last payment is only what is still owed. A calculated payment that rounds a cent short pays that cent in a later period.

A payment that leaves the balance the same or higher stops the schedule after that period. The higher balance stays visible. The schedule also stops at 600 months, at the caller's last date, or when a later rate is unknown. A promotional rate applies through its end date, including that date.

Extra on one debt stays on that debt until it is paid off. A round of extra across debts follows the order the caller supplies. Minimums are paid first. Extra left after one debt is offered to the next debt that can take it. A debt that is missing a date, rate, or minimum is skipped. Extra that no debt can take is returned. This round does not choose avalanche order, and it does not roll a freed minimum onto the next debt.

Income and bills stay on their own dates. A biweekly paycheck is not rewritten as a monthly average. An irregular item appears once. A raise replaces the payment on and after its date. On one date the order is income, then bills, then debt payments, then savings.

A savings contribution reserves cash. It is a separate kind of event, not a bill. The amount needed to hit a date trues up on the last month so the contributions add up to the gap. A chosen monthly amount is a second list and can finish on a different date. A target date already past is due in full at the start. A path that cannot finish within 600 months is reported and is not filled with a partial list.

Paycheck dates now use the same date walker as these events. The dates themselves are unchanged.

## Changes

- `CashFlowSchedule` places income, bills, debt payments, and savings on dates.
- `DebtAmortization`, `DebtMinimum`, and `DebtPeriodCalculator` project one debt.
- `DebtPaymentAllocation` applies one round of extra in caller order.
- `SavingsTarget` plans a reserve from a date, a monthly amount, or both.
- `DebtInterest` and `DebtRate` are shared with the Debts summary.
- `CadenceDates` is the shared date walker. Paycheck schedules call it.

## Data changes

No migration. No rows change. No screen reads these results yet.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~CashFlowSchedule|FullyQualifiedName~DebtMinimum|FullyQualifiedName~DebtAmortization|FullyQualifiedName~DebtPaymentAllocation|FullyQualifiedName~SavingsTarget|FullyQualifiedName~PaycheckSchedule|FullyQualifiedName~DebtSummary"`: 56 passed. That covers three January biweekly paychecks kept at $1,000, a raise on its date, a month-end bill, a $1,000 loan at 12% over 12 months paying $88.85 then a final $88.84 with $66.19 interest, a payment that grows a balance, extra applied in list order, and a $100 target split as $33.33, $33.33, and $33.34. The existing paycheck and debt-summary tests are in that run.
- Did not click through the app. There is no screen for this slice.

## Manual verification

No migration. No screen change. Balances, bills, and income are not written.

1. There is no new page to open.
   Expected: Debts, Income, Bills, and Connections look the same. Waive a click-through, or open any of those pages and confirm nothing new appeared.

## Approval

Zach approved this increment on October 7, 2026, as built. There was no screen to click through.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 3 item 2, the 30-day cash view and 6/12/18-month forecasts.
