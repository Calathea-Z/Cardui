# Plan extra payment

Date: October 7, 2026
Status: Approved 2026-10-08
PR:

## Increment

The first Phase 3 item 6 scenario: one shared extra payment, tried on Plan. Blank or zero stays the minimums-only plan. A positive amount replaces the summary, the payoff charts, the payoff order, and the cash outlook. Nothing is saved.

## Decision

Zach chose the extra payment first, tried on the page only, with the page replacing its numbers the way the Rollover switch does.

- The field sits above the summary, labeled Extra each month. Blank or zero is minimums only. The amount applies when the field is left or Enter is pressed.
- That amount goes to the highest-interest debt that can take it, then to the next debt after that one is gone. It is not part of a freed minimum, so Keep freed payments still returns only the minimum. The extra keeps going to debt until nothing is left to pay.
- The cash outlook includes it in the debt payments, so cash is lower while it is being sent.
- Leaving Plan clears it. Refreshing clears it. No migration.
- A negative amount or other text is not sent. The previous plan stays on screen while the new amount is loading, and stays if the request fails.

## Changes

- `HouseholdRecovery.Prepare` takes the tried extra. Zero is still the baseline. A custom order and a reclaim amount stay unset.
- `GET /api/plan/recovery?monthlyExtra=` passes that amount through the payoff path and the cash outlook. Omit it, or pass zero, for minimums only. A negative amount is rejected. `PlanRecoveryDto.monthlyExtra` is the amount the calculation used.
- The first page load still uses zero. The browser requests a positive amount and does not store it.
- Frontend: `PlanExtraField` and `usePlanExtra`. `planExtra.ts` reads the field. `planCopy.ts` names the amount in the title line, the summary, the cash outlook line, and How this is calculated.

## Data changes

No migration. No schema change. No rows change. Opening the page reads the plan and writes nothing. The extra amount lives in the page until you leave.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~HouseholdRecovery|FullyQualifiedName~HouseholdCashOutlook|FullyQualifiedName~PayoffRollover|FullyQualifiedName~CashFlowRecovery"`: 36 passed, which also compiles the API and the whole test project. One is new. A $300 debt at $50 a month, started January 1 and due January 15, is debt-free June 15 with no extra and March 15 with $50 extra. On January 15, with $1,000 starting cash and no income or bills, the debt payment is $50 and cash ends at $950 with no extra, and $100 and $900 with the extra.
- `node ./scripts/run-plan-chart-tests.mjs`: 24 passed, 1 new. Blank is zero. $200.005 rounds to $200.01. A negative amount and other text are rejected. The title, the debt-free sentence, the figure label, the order rule, the saved rule, and the cash outlook line name the extra. Each assumption stays 140 characters or fewer.
- `pnpm test`: every script passed.
- `pnpm exec eslint` on the changed Plan files, the plan API client, the types, and the test script: no errors. `pnpm exec prettier --write` on the same files.
- Not run: the full .NET suite, `tsc`, and a signed-in click-through. The shell requires Clerk.

## Manual verification

No migration. Restart the API before opening Plan. The running one predates the extra query.

1. Open Plan.
   Expected: Extra each month sits above the summary. The field is blank. The help line says blank is minimums only, it is tried on this page, and leaving clears it. The debt-free label still says minimums only.
2. Type an extra amount and click away, or press Enter.
   Expected: The page says it is updating, then the debt-free date is the same or sooner. The big figure is labeled Debt-free, and the sentence names the amount. The line under Plan and the line under Cash outlook name it too. Ending cash is the same or lower.
3. Press Keep freed payments, when the switch is shown.
   Expected: The outlook still names the extra. A paid-off debt's minimum comes back as cash. The extra keeps going to the next debt.
4. Clear the field and click away.
   Expected: The page returns to the minimums-only plan.
5. Leave Plan and open it again.
   Expected: The field is blank. The plan is minimums only.
6. Type a negative amount, or a word, and click away.
   Expected: The field says to enter a zero or positive amount. The plan on screen does not change.
7. With an extra applied, open How this is calculated.
   Expected: Order names the amount and says it goes to the first debt that can take it. Saved says nothing is saved and the amount stays until you leave.
8. Tab to the field.
   Expected: The focus ring is visible. Enter applies the amount.

## Approval

Approved by Zach on October 8, 2026. He found the extra field's layout poor and said to leave it for now and keep moving. That layout is tracked in the roadmap and is not the next slice.

## Pending decision

None in this slice. After approval, the rest of Phase 3 item 6 is still open: income loss, bonuses and windfalls, spending changes, and protected-cash targets. Item 10, saved plans, has not started.
