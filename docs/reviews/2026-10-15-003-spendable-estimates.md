Date: 2026-10-15
Status: Approved 2026-10-15
PR:

# Spendable estimates

## Increment

Phase 3 item 8. Spendable cash is the lowest amount left over the next 30 days after dated bills, debt payments, ordinary spending, and money set aside. Only steady pay counts. A monthly surplus is not the result. An extra payment warns when it would leave ordinary spending, or a near-term bill or debt payment, short. Nothing is saved and no money moves.

## What you see first

The primary question on Overview is still whether the plan is affordable. A new shortfall caused by an extra payment leads that status. Otherwise a cash shortfall or the payoff estimate stays there. The next section that answers "how much can I use" is **Spendable**, under Try a scenario. Its figure is the lowest cash left through the horizon. The horizon dates, balance freshness, and anything left out stay visible. Ending cash and the daily chart stay underneath and are named as the cash outlook, which still counts every included paycheck.

On a phone the status, the scenario, and spendable stack. From the wide layout, spendable and its chart sit beside Attention. A household with no debts still sees spendable, then the prompt to add a debt.

## Decision

- The horizon is the next 30 days, the same daily window the cash outlook already walks. Longer horizons stay summaries and are not turned into a spendable line.
- Reliable income is steady pay on its dates, including a later raise on that source. Variable pay, uncertain pay, and a one-time windfall are left out and named. Pay the preview omits is left out and named.
- Reserved funds are the amount already set aside at the start. Spendable is cash minus that reserve at the lowest day, and it is never shown below zero. A shortfall stays visible as the lowest cash left.
- Ordinary spending is the flexible amount from Plan budget. When none is set, the estimate says it may be high.
- The extra is compared with the same payoff path at minimums. The warning fires when that extra makes a day with ordinary spending, a bill, or a debt payment short, and that day is worse than minimums. A new shortfall leads the Overview status. A gap that is already there without the extra stays in that status, and Spendable says the extra keeps it.
- The cash outlook is unchanged. It still counts every included paycheck at typical pay, with low pay under its disclosure.

## Changes

- Domain: `SpendableRules` walks the existing dated cash forecast with steady pay only, then compares the selected path with and without the extra.
- API: `GET /api/plan/recovery` now includes the estimate on both paths. Income reliability is read with the paycheck. Nothing is stored.
- Frontend: Spendable on Overview, with the horizon, freshness, uncertainties, and the extra-payment warning.
- Design: the Spendable section in `docs/design/plan-page.md`.

## Data changes

None. No migration. No debt, balance, transaction, or payment row changes.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~SpendableRules|FullyQualifiedName~HouseholdCashOutlook|FullyQualifiedName~PlanCashScenario|FullyQualifiedName~ContributionShare"`: 27 passed. With $50 on hand, $1,000 of steady pay on January 5, and a $400 bill on January 20, spendable is $50, not the $600 monthly surplus. A $400 bill on January 5 before $1,000 of pay on January 20 leaves spendable at $0 and the lowest day at -$300. $400 set aside from $500 leaves $100 spendable. Variable and uncertain pay are left out. A windfall is left out. A $200 extra on a $300 balance leaves ordinary spending $50 short on January 20. A $50 extra that still covers the bill does not warn.
- `node ./scripts/run-spendable-tests.mjs`: 5 passed. `node ./scripts/run-plan-response-tests.mjs`: 7 passed. `node ./scripts/run-plan-chart-tests.mjs`: 32 passed. `pnpm exec tsc --noEmit`: clean. Prettier and eslint on the changed Plan and API-client files: clean.
- The local API was rebuilt and restarted. `GET /api/plan/recovery` without a session returns 401, so the route is registered. The signed-in page was not exercised.

## Manual verification

Restart the API if it was running before this change. There is no migration. The Plan page rejects an older recovery response, so a stale API shows a load error until it is restarted.

1. Open Plan.
   Expected: Spendable is on Overview, under Try a scenario. It names the dates it covers, the oldest cash balance, and anything left out. It says this is not a monthly surplus. Ending cash and the chart are labeled as the cash outlook.
2. Compare the figure with a month that has a bill before the next steady paycheck.
   Expected: The figure follows the lowest day, not steady pay minus bills. A shortfall says nothing is left to spend and shows that lowest day.
3. Leave ordinary spending unset, or mark an income source variable.
   Expected: Spendable says ordinary spending may make the estimate high, or that the variable paycheck is not counted.
4. Enter an extra monthly payment large enough that ordinary spending or a bill inside 30 days would go short, then apply it.
   Expected: A warning names that date and the amount short. When the plan was affordable without the extra, that warning leads the status. Debts, Accounts, and Savings balances are unchanged.
5. Enter a small extra that still leaves those bills and ordinary spending covered.
   Expected: No extra-payment warning. The payoff estimate can still change.
6. Leave Plan and open it again.
   Expected: The extra is clear. Nothing was saved.

## Approval

Approved by Zach on October 15, 2026. A heavy QA and UX pass for Phase 3 waits until items 9–11 are built.

## Pending decision

None in this slice. Phase 3 items 9–11 have not started. Detail panel keyboard focus stays tracked and is not scheduled. Unimplemented Planning UX proposals stay unscheduled.
