# Plan cash outlook

Date: October 7, 2026
Status: Approved 2026-10-07
PR:

## Increment

Plan screen item 2 in `docs/roadmap.md`, as designed in `docs/design/plan-page.md`. Plan now has a Cash outlook: the next 30 days and 6, 12, and 18 months, following the Rollover or Keep freed payments switch. Nothing is saved.

## Decision

**A stored due date before today.** Zach chose: the payoff path and the cash outlook both start at the first monthly date on or after today, stepped from the stored date. A debt stored as due January 31, opened on March 10, pays March 31, April 30, May 31. Past dates are not replayed, and the balance stays today's balance until that payment. Before this, the payoff path paid from the stored date, so a debt entered in August took the August and September payments off today's balance and showed an early debt-free date. Nothing moves a stored due date forward, so most debts hit this a month after entry. There is no warning for it, because it would show on every debt every month. How this is calculated has a Due dates rule.

**Layout.** Zach chose one section after the payoff order, a 30-day chart with horizon cards, and low pay under a disclosure.

- The line under the title: "Starts from $X in Cash on Accounts today." With the switch shown, it adds what happens to a freed payment.
- One sentence about the next 18 months. With a shortfall, in the amber warning callout: "Cash runs short on Feb 27, 2027 and is back above zero on Mar 5, 2027. Lowest point: −$45.50 on Mar 1, 2027." Without one: "Cash stays above zero through Apr 6, 2028. Lowest point: …"
- Next 30 days: a step chart of cash at the end of each day in `--chart-1`, a dashed zero line, and a dot on the lowest day. A key above names all three. Nothing is written inside the plot, so the tooltip covers no text. The tooltip shows the day, ending cash, and only the income (+), bills (−), and debt payments (−) that landed that day.
- Further out: three cards, 6, 12, and 18 months, each with ending cash, the lowest point and its date, and the minimums still due (None, Unknown, or "$45.00 a month, 1 not included"). A card whose cash goes below zero gets the warning border, icon, and a spoken "Warning".
- Notes: a warning when payments are left out for a debt Finish your plan lists, or a debt with no balance; a quiet note when there are no bills; a quiet note for another currency.
- If pay comes in low: a disclosure with the same sentence, chart, and cards. Low pay uses each source's low amount where recorded, otherwise typical, and leaves out expected raises, because a raise is a typical amount. Without any low amount, it says to add one on Income.
- Without income, the section's empty state says "Income is required" and links to Income, because the forecast would only show cash draining.

**Following the switch.** The forecast pays each debt what the selected path pays. On Rollover a freed minimum stays in debt payments, so cash outflow does not drop at a payoff. On Keep freed payments it stops, and that cash stays. Without a payoff the switch is hidden, and the outlook uses Rollover, which is the same as Keep freed payments when nothing is freed.

**What the outlook leaves out.** A debt missing a rate, minimum, or due date makes no payment in the forecast. A debt whose payment does not cover interest pays once and then stops, as the approved forecast does. A debt with no balance makes none. That understates outflow, so the warning note names those debts. Savings and a protected reserve are not stored yet, so the reserve stays zero and is not sent.

## Changes

- `DebtDueDates` (internal, `Domain/Recovery`): monthly stepping from a stored date and the first index on or after a start date. `DebtAmortization` and `PayoffRolloverProjection` both use it instead of private copies.
- `PayoffRolloverInput` has a required `AsOf`. Each `PayoffRolloverRun` starts at its first due date on or after it; removal dates step from the same index. `PayoffRolloverPath.Schedules` returns each debt's periods as a `DebtSchedule`, including rolled cash. Balance points are built from those periods.
- `CashForecast.Project` takes optional schedules. A schedule given for a debt replaces that debt's own amortization. Without one, the approved behavior is unchanged. The written payment assumption says which was used.
- `HouseholdRecovery.Prepare` takes today. New `HouseholdCashOutlook.Project` runs the forecast for both paths at typical pay and, when a low amount exists, at low pay. New records: `HouseholdIncome`, `HouseholdCashOutlookInput`, `HouseholdCashOutlookPath`, `HouseholdCashOutlookReport`.
- Focused reads for the outlook: `IAccountsService.GetCashTotalAsync` (active, unarchived cash accounts in the planning currency, the same rule as the Accounts Cash total, projecting type, balance, and currency only), `IIncomeSourcesService.GetOutlookIncomesAsync`, and `IObligationsService.GetOutlookBillsAsync`. Each projects only the columns the forecast reads.
- `PlanService` uses today in the household time zone, loads the debts and those three, and builds the plan. `GET /api/plan/recovery` now also returns `cashOutlook`: `PlanCashOutlookDto` with the starting cash, whether income and bills exist, the excluded currencies, and for each path a typical and an optional low-pay `PlanCashForecastDto` (30 days with each day's income, bills, and debt payments, the 30-day window, the three horizons, and the first shortfall and recovery dates). `PlanCashOutlookDtoMapper` shapes it; the forecast's written assumptions are not sent.
- Frontend: `PlanCashOutlook`, `PlanCashChart`, and `PlanCashHorizons`. `planChartSeries.ts` gains `cashChart`, `formatDayTick`, and `formatDayLabel`; `monthTicks` is renamed `evenTicks` because the day chart uses it too. `planCopy.ts` gains `cashOutlookDescription`, `cashOutlookSummary`, `cashHorizonCards`, `cashOutlookNotes`, and `cashChartLabel`, and How this is calculated gains Due dates, Cash outlook, and Low pay. Types are in `lib/api/types/plan.ts`.

## Data changes

No migration. No schema change. No rows change. Opening the page reads accounts, income, bills, and debts and writes nothing.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~HouseholdRecovery|FullyQualifiedName~HouseholdCashOutlook|FullyQualifiedName~PayoffRollover|FullyQualifiedName~CashFlowRecovery|FullyQualifiedName~CashForecast|FullyQualifiedName~DebtAmortization|FullyQualifiedName~PayoffPriority"`: 79 passed, which also compiles the API and the whole test project. Five are new. A debt stored January 31 and started March 10 pays March 31 through June 30, and its minimum leaves July 31. A debt stored November 30, 2025 and started January 10 gets the same first payment (January 30) and payoff (April 30) from the payoff path and from the forecast. With $1,000 in cash, a $50 debt paid off in January and a $1,000 card at $25, six months end at $550 on Rollover and $800 on Keep freed payments. At $1,000 typical pay with a $1,200 raise in March and $800 low pay, six months of pay are $6,800 typical and $4,800 low. With no low amount there is no low-pay forecast.
- `node ./scripts/run-plan-chart-tests.mjs`: 23 passed, 7 new. Day rows at UTC midnight with the lowest day marked, and none for no days; the summary for a shortfall that recovers, one that does not, and none; horizon cards with minimums as a partial sum, None, and Unknown; the description on each path and without a payoff; the notes in order, listing stopped and no-balance debts but not paid-off ones or ones that reached the 50-year limit; the chart label; the three new rules under How this is calculated, each 140 characters or fewer.
- `pnpm test`: every script passed.
- `pnpm exec eslint` on the changed Plan files, types, and test script: no errors. `pnpm exec prettier --write` on the same files.
- `pnpm exec tsc --noEmit`: only the three missing `.next/types` modules for the removed `/budgets`, `/institutions`, and `/transactions` routes, which were already there.
- Not run: the full .NET suite, a signed-in click-through (the shell requires Clerk), and a check of the new `GetCashTotalAsync` against the Accounts page with real data.

## Manual verification

No migration. Restart the API before opening Plan; the running one predates the new response.

1. Open Plan with your current debts.
   Expected: Cash outlook sits after the payoff order (or after What you owe today when nothing pays off) and before How this is calculated. The line under its title names the same Cash total as Accounts.
2. Compare the debt-free date and payoff dates with what you saw before.
   Expected: A debt whose stored due date has passed now starts paying next on the same day of the month on or after today, so its payoff date can be a month or two later than before. Nothing else moves.
3. Read the outlook sentence and the cards.
   Expected: One sentence about the next 18 months, with the lowest point. If cash goes below zero it sits in an amber box with a warning triangle, and each card that goes short has an amber border and icon beside its lowest point. Cards read 6, 12, and 18 months with Through dates.
4. Hover the 30-day chart, then tap it on a phone width.
   Expected: The tooltip shows the day, ending cash, and only that day's income with a plus and bills or debt payments with a minus. A dashed zero line and a dot on the lowest day; the key above names Cash at end of day, Lowest day, and Zero. No text inside the chart is covered.
5. With PayPal Credit missing a due date and REI Mastercard not paying down, look under the cards.
   Expected: An amber note says payments are left out for REI Mastercard and PayPal Credit, so cash may be lower than shown.
6. Press Keep freed payments (when the switch is shown).
   Expected: The outlook line says a paid-off debt's payment comes back as cash. The 12 and 18 month ending cash is the same or higher than on Rollover once a debt is paid off.
7. Open If pay comes in low.
   Expected: With a low amount on an income source, the same sentence, chart, and cards at lower amounts. Without one, a line saying to add a low amount on Income.
8. Open How this is calculated.
   Expected: New rows for Due dates, Cash outlook, and Low pay, one line each.
9. If you can, view a household with no income.
   Expected: Cash outlook says Income is required and has a Go to Income button.
10. Tab through the outlook.
    Expected: The low-pay disclosure shows a visible focus ring and opens with Enter or Space.

## Approval

Approved by Zach on October 7, 2026. He waived the manual checklist and said he is not yet sure he understands the goal of the work. The next chat should restate where the Plan work is heading before it builds anything.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 3 item 6, reproducible scenarios.
