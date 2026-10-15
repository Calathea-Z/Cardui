Date: 2026-10-15
Status: Approved 2026-10-15
PR:

# Restructuring comparisons

## Increment

Phase 3 item 7. One consolidation loan, balance transfer, or replacement loan can be compared with keeping the selected debts. The comparison covers APR, fees, term, monthly payment, total interest, break-even, payoff, the monthly cash change, and cash used at the start. It does not rank an option by payment, and it does not treat the offer as approved. Nothing is saved and no money moves.

## What you see first

The primary question is what this offer changes compared with keeping the selected debts. The next action is **Compare offer**. Before a result, the form is the work. After a result, the cost, the payment change, and the break-even date lead, and the form stays below them.

Compare an offer is a Plan tab after Debt payoff. On a phone the tab bar is two columns and the fields stack. From 640px up the amount fields sit in two columns. From `md` up the four tabs sit on one row. The form stays within a narrow width on the existing Plan page.

## Decision

- The comparison lives on Plan. It is not a new primary destination and not a saved setting.
- One offer at a time. Each selected debt is included in full. A consolidation loan needs two or more debts. A replacement loan needs one. A balance transfer can include only revolving debts.
- The rate entered is used for the whole term. A later rate, a promotional expiration on the offer, and a partial balance stay in Phase 7.
- The current side uses today's minimums. Extra payments tried under Try a scenario are not part of this comparison, and that control is not on this tab.
- A lower monthly payment is shown and is not treated as the better offer. Interest and fees are the cost. Break-even is the first date the offer's interest and fee stay at or below the current interest through both payoffs. If either path does not pay off, the date is not shown.
- Upfront cash is a fee paid in cash plus cash applied to the new balance. A fee added to the balance is borrowed. The page says when that cash is more than the cash accounts, or would leave less than cash to keep. It does not treat the cash as an investment return.
- The terms are ones you enter. Cardui does not treat them as approved, apply for them, or move money. Other income and bills stay as they are.

## Changes

- Domain: `RestructuringComparisonRules` checks the offer and projects the current debts and the new balance with the existing monthly interest rule.
- API: `POST /api/plan/restructuring`. The response is the comparison. Plan's debt facts now include kind, stored APR, and currency so the form can name each debt. Nothing is stored.
- Frontend: Compare an offer on Plan, with the result, the warning when cash used at the start crosses cash on hand or cash to keep, and the statement that a lower payment is not a rank.
- Design: the Compare an offer section in `docs/design/plan-page.md`.

## Data changes

None. No migration. No debt, balance, transaction, or payment row changes.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~RestructuringComparison"`: 14 passed. A longer, cheaper payment that costs more has no break-even date. A $30 balance-transfer fee on a $1,000 balance at 18% breaks even on April 1, 2026. A fee added to the balance raises principal. A fee paid in cash is cash due now. Upfront cash above the cash accounts is flagged. A debt that does not pay down leaves break-even unknown. A balance transfer rejects an installment loan. Another currency is rejected.
- `node ./scripts/run-restructuring-tests.mjs`: 4 passed. `node ./scripts/run-plan-response-tests.mjs` and `node ./scripts/run-plan-chart-tests.mjs`: passed. `pnpm exec tsc --noEmit`: clean. Prettier and eslint on the changed Plan and API-client files: clean.
- `POST /api/plan/restructuring` without a session returns 401, so the route is registered. The signed-in page was not exercised. The local API was rebuilt and restarted so the route is the one now listening on localhost.

## Manual verification

Restart the API if it was running before this change. There is no migration.

1. Open Plan and choose **Compare an offer**.
   Expected: Compare offer is the action. The page says these are terms you enter, that they are not approved, and that Cardui does not apply or move money. Try a scenario is not on this tab.
2. Choose Replacement loan, one debt, an APR, a term, a fee of 0, and upfront cash of 0, then compare.
   Expected: APR, fee, term, monthly payment, total interest, interest and fees, and payoff are shown for the current debt and the offer. The page says a lower monthly payment is not treated as the better offer.
3. Enter a longer term that lowers the payment and raises interest and fees.
   Expected: The page states both the lower payment and the higher cost. It does not call the lower payment better. If the offer stays more expensive through both payoffs, it says there is no break-even date.
4. Enter a fee and compare again.
   Expected: A fee added to the balance increases the new balance and is not cash used at the start. A fee paid in cash is named as cash used now.
5. Enter upfront cash larger than the cash accounts, then compare.
   Expected: A warning says the cash used is more than the cash accounts. Debts, Accounts, and Savings balances are unchanged.
6. Choose Balance transfer and an installment loan.
   Expected: The field asks again. No comparison is sent that includes that loan.
7. Leave Plan and open it again.
   Expected: The offer form is clear. Nothing was saved.

## Approval

Approved by Zach on October 15, 2026.

## Pending decision

None in this slice. Phase 3 items 8–11 have not started. Partial balances, more than one offer, a changing rate, and a promotional expiration stay in Phase 7.
