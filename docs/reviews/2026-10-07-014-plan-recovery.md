# Plan recovery screen

Date: October 7, 2026
Status: Awaiting review
PR:

## Increment

The Plan page shows the cash-flow recovery report for this household's debts. Rollover and keeping every freed payment are both on the page. Shared extra, a custom order, and a chosen reclaim amount are not stored, so those stay at zero. Nothing is saved.

## Decision

The screen sits after Phase 3 item 5 and before item 6, so the approved report can be checked against real debts. Phase 5 item 1 stays the later follow-through home.

`GET /api/plan/recovery` loads the household's debts through the existing debt list, so a followed balance and a followed credit limit are the amounts already in use. A pure mapper in `api/Domain/Recovery` turns those facts into the rollover input. A missing balance is left out, because zero would be read as already paid off. Another currency is passed through so the existing rule can leave it out. A missing rate, minimum, or due date stays unknown.

The page shows rollover and keeping every freed payment. Reclaiming zero matches rollover, so that third path is not repeated on the screen. The report's assumptions stay behind a disclosure. A household with no debts is asked to add one on Debts.

## Changes

- `HouseholdRecovery` builds the baseline rollover input from the debts the plan reads.
- `GET /api/plan/recovery` returns the cash-flow recovery report. It does not write rows.
- `/plan` shows the two paths, the currencies left out, and the assumptions. An empty household links to Debts. A failed load keeps the error banner.

## Data changes

No migration. No rows change. Opening the page does not write balances, bills, or income.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~HouseholdRecovery"`: 4 passed. A balance in use of $180 is the amount owed, with no shared extra, no per-debt extra, and an empty order. A CAD debt stays in the input and the report leaves CAD out. A missing rate, minimum, or due date stays unknown and the explanation says so. A missing balance is left out.
- `pnpm exec eslint` on the Plan page, its types, and the server client: no errors.
- `pnpm exec tsc --noEmit` still reports three missing modules under `.next/types` for the removed `/budgets`, `/institutions`, and `/transactions` routes. Those errors were already there. This change did not add a type error.
- Did not click through a signed-in session. The shell requires Clerk.

## Manual verification

No migration. Opening Plan does not write balances, bills, or income. If the API was already running, restart it before opening Plan so the new route is loaded.

1. Open Plan with no debts.
   Expected: The title is Plan. The page says a debt is required and links to Debts. The account menu still does not list Plan.
2. Open Plan with at least one debt that has a balance, a rate, a minimum, and a due date.
   Expected: Rollover lists each payoff, the date the minimum leaves, the minimum removed, and the breathing room after that step. A second section, Keeping every freed payment, uses the same row shape. The page says debts are paid highest interest first, with no extra payment. Assumptions stay closed until opened.
3. If a debt is in another currency, look at the open page.
   Expected: That currency is named as left out. The plan uses the household currency.
4. If a debt is missing a rate, a minimum, or a due date, read the rollover explanation.
   Expected: It says that debt is missing a rate, minimum, or due date.
5. If a debt follows a connected card, compare the balance on Debts with the payoff on Plan.
   Expected: Plan uses the balance already in use on Debts, including a followed balance.
6. Narrow the window below 768px and open Plan from the tab bar.
   Expected: The same page. Go to Debts, when it is shown, is easy to tap.

## Approval

Awaiting Zach's review.

## Pending decision

None in this slice. After approval, the next roadmap item is Phase 3 item 6, reproducible scenarios for changed extra payments, income loss, bonuses and windfalls, spending changes, and protected-cash targets.

## Correction

October 7, 2026. Zach reviewed the page and did not approve it. With PayPal Credit missing its terms and REI Mastercard not paying off, the page showed two identical cards, each with "$0.00 a month" and a long paragraph. Nothing told the eye where to look. The rework is Plan screen item 1 in `docs/roadmap.md`, designed in `docs/design/plan-page.md`: a one-sentence answer, Finish your plan, two debt charts, and a Rollover or Keep freed payments switch. This report will be updated with that page and a new checklist. After approval, the next item is Plan screen item 2, the cash outlook, not Phase 3 item 6.
