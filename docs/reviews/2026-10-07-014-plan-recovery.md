# Plan recovery screen

Date: October 7, 2026
Status: Approved
PR:

## Increment

Plan screen item 1 in `docs/roadmap.md`, as designed in `docs/design/plan-page.md`. This reworks the first Plan page in this same report (see "Correction" below).

Plan now answers first. A switch in the title row picks Rollover or Keep freed payments. Under it: a one-sentence summary with one big figure, Finish your plan for anything that blocks the projection, a What you owe today donut, a stacked balance chart with one band per debt, a minimums and breathing room step chart, the payoff order, and the assumptions behind a disclosure. The order stays highest interest first with no extra payment, because those choices are not stored. Nothing is saved.

## Decision

The page builds its own copy from structured fields. The server-written explanation sentences, which wrote amounts as "45.00 USD", no longer reach the page.

- **Summary.** With every debt paid off: "You're on track to be debt-free paying only your minimums. Anything extra brings that day closer." The big figure is the debt-free date, labeled "Debt-free at minimums only", because no extra payment is stored yet and later work will move it sooner. Beside it: "Back to you after payoff" ($X a month) and total interest. Breathing room was the big figure at first, but Zach read "$494.58 a month" as money available today while debts remained. With some debts paid off, it counts them ("2 of 3 debts paid off by …"), says what minimums remain, and labels the figure "Back to you each month after …". With no payoff: "2 debts need attention before your plan can project a payoff." The big figure is the total owed today, with the known monthly minimums beside it and a count of minimums not included.
- **Finish your plan.** One row per blocker, each linking to Debts with an action label: Add due date, Add rate, Add minimum, Add balance, or Update payment. A debt that stopped names the reason: a missing interest rate, a missing rate after a promotion ends, a missing minimum, a missing due date, or a payoff past the 50-year limit. A payment that does not cover interest says "Interest is about $52.13 a month, more than your $45.00 payment. Paying $53.00 or more starts paying it down." That interest and payment come from the month the debt stopped paying down, not from a frontend estimate. A debt with no balance says it is left out. A currency left out gets one row.
- **What you owe today.** Zach's direction: show the current damage as a pie. A donut with one slice per debt, sized by balance, with the total in the middle. The legend lists each debt's balance and whole-number percent, and the percentages add to 100. It does not change with the switch, and it shows whether or not anything pays off. Debts with no balance are named under it.
- **No payoff yet.** The switch is hidden, because both paths would be identical, and the line under the title says the payoff charts appear once a debt can be paid off. The projection stops at the first missing term (due date, then rate, then minimum), so a debt missing all three names only the due date until that is filled.
- **Charts.** Only debts that pay off on the selected path are drawn. The balance chart stacks them in payoff order, first payoff on top, so the top edge is the total owed and steps down at each payoff. Each month carries a debt's last balance forward until its next payment. The step chart starts at today's known minimums and steps down on each removal date, while breathing room steps up. Without a payoff both charts are hidden.
- **Color.** A debt's color comes from its position in the rollover order and does not change with the switch. The chart, the tooltip, and the payoff order share it. A ninth debt reuses the first color.
- **Highlight.** Hovering a band or a payoff row dims the other bands to 35%. A payoff row is a toggle button, so pressing or tapping it keeps the highlight; that also covers keyboard and phone, where hover does not exist.
- **Warnings.** A new `--warning` token, amber `#8f5a00` (5.78:1 on white, 5.16:1 on the canvas, 5.05:1 on its 10% tint), marks something to finish that is not a failure. While the plan cannot project every payoff, the summary sentence sits in an amber callout with a warning icon and a spoken "Warning". Finish your plan gets an amber border, an icon in its title, and an icon on each row. `ui-governance.mdc` now says when to use `--warning` instead of `--destructive`.
- **Same-size buttons.** The Finish your plan action labels use the shared outline button size with one fixed width, so Update payment and Add due date line up. `ui-governance.mdc` now says buttons on one page share one size.
- **Donut hover.** The floating tooltip covered the total in the middle. It is gone; hovering a slice or a legend row puts that debt's name, balance, and percent in the middle instead. `ui-governance.mdc` now says a tooltip never covers text inside a chart.
- **How this is calculated.** Zach found the six server paragraphs a wall of text. The page now writes its own rules, one row each: a short term and one sentence. Order, Payments, Interest, Payoff month, then Rollover or Keep freed payments for the selected path, Left out, Currency, Limit, and Saved. The text about partial reclaim amounts and a shared extra payment is gone, because this page uses neither.
- **The switch** is a new `SegmentedControl` primitive: a pressed-button group with `aria-pressed`, 44px tall on a phone. The two Accounts selectors are unchanged, as the design says.

Jewel series colors: emerald and teal moved from the starting values. Emerald `#0f7b5f` was about 12 ΔE from `--success`, which the breathing-room line uses right beside the bands. Emerald is now `#00806e` and teal `#127c99`, which keeps emerald clear of both `--success` and teal. Every series color is at least 18 ΔE from `--success`, `--destructive`, and `--transfer`.

| Token | Color | Contrast on white |
| --- | --- | --- |
| `--series-1` emerald | `#00806e` | 4.87:1 |
| `--series-2` sapphire | `#2b5fb3` | 6.19:1 |
| `--series-3` copper | `#b8642e` | 4.29:1 |
| `--series-4` amethyst | `#7b4fb5` | 5.79:1 |
| `--series-5` teal | `#127c99` | 4.81:1 |
| `--series-6` magenta | `#a83f74` | 5.79:1 |
| `--series-7` amber | `#a8740c` | 4.06:1 |
| `--series-8` indigo | `#4a4fb0` | 6.92:1 |

## Changes

- `PayoffRolloverProjection` records each debt's balance after every payment. `PayoffRolloverPath.BalancePoints` returns them as `PayoffBalancePoint(DebtId, DueDate, Balance, Interest, Payment)`, sorted by due date and then by payoff order. A debt that cannot be calculated has no points. Interest and payment are beyond the design's shape; the mapper copies the last month's onto each debt's outcome as `LastMonthInterest` and `LastMonthPayment`.
- `HouseholdRecovery.Prepare` returns `HouseholdRecoveryInput`: the rollover input plus the debts left out for a missing balance.
- `GET /api/plan/recovery` returns `PlanRecoveryDto`, which replaces `CashFlowRecoveryReportDto`. It carries the rollover and keep-all paths: steps, starting and remaining minimums, recurring breathing room, debt-free date, total interest, each debt's outcome (stop reason, opening balance, minimum, payoff date), and balance points. It also carries the excluded currencies and the debts with no balance. The partial-reclaim path, `ReclaimAmount`, `MonthlyExtra`, `Explanation`, and the written assumptions are gone from the response. The domain still computes them.
- `frontend/app/globals.css`: eight `--series-N` tokens with `--color-series-N` Tailwind entries. The unused `--chart-3`, `--chart-4`, and `--chart-5` are removed. `.cursor/rules/ui-governance.mdc` section 1 and `docs/design/ui-direction.md` name the series tokens.
- `frontend/components/ui/segmented-control.tsx`: the new primitive.
- `frontend/features/plan`: `PlanSummary`, `PlanFinishList`, `PlanOwedChart`, `PlanBalanceChart`, `PlanObligationChart`, and `PlanPayoffOrder`. Pure rules are in `planChartSeries.ts` (colors, owed shares, bands, monthly rows, step rows, ticks, payoff order) and `planCopy.ts` (switch options, description, summary, Finish your plan, how this is calculated). `PlanRecoveryPath.tsx` and `planRecoveryCopy.ts` are deleted.
- `frontend/scripts/run-plan-chart-tests.mjs` tests both modules and is part of `pnpm test`.

## Data changes

No migration. No schema change. No rows change. Opening the page does not write balances, bills, or income.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~HouseholdRecovery|FullyQualifiedName~PayoffRollover|FullyQualifiedName~CashFlowRecovery|FullyQualifiedName~PayoffPriority"`: 46 passed. New: the interest and payment of a month that does not pay down (a $1,000 balance at 24% with a $15 minimum records $20 interest, a $15 payment, and $1,005 owed). New: each balance after every payment on rollover and keep-all, sorted by due date then payoff order, and no points for a debt missing its minimum. New: a missing balance is left out of the input and listed, including one in another currency. The missing-terms test now checks the stop reason the page reads instead of the explanation sentence.
- After the warning, button, and donut changes: `node ./scripts/run-plan-chart-tests.mjs` passed 15, now also checking the summary's warning flag for full, partial, and no payoff. eslint on the Plan files is clean, and tsc shows only the three existing `.next/types` errors. The warning color's contrast was computed with a script.
- After the How this is calculated change: the same recovery tests without `PayoffPriority` rebuilt the API and passed (30). `node ./scripts/run-plan-chart-tests.mjs` passed 15, and eslint and tsc gave the same results as below. The full `pnpm test` was not rerun; only the plan script changed.
- `node ./scripts/run-plan-chart-tests.mjs`: 14 passed before that change; the 15th checks that each rule is one short line, that only the freed-payment rule follows the switch, and that the currency rule names the planning currency. Colors by rollover position with a ninth debt wrapping; owed shares as whole percents that add to 100, largest first, with a zero balance left out; the no-payoff summary counting debts that need attention and leading with the total owed; the interest shortfall copy and each action label; bands in payoff order with balances carried forward month by month; a debt that does not pay off left out of the bands and the total; no payoff draws neither chart; minimums stepping down on removal dates and a removal date outside the projection skipped; six or fewer ticks; the summary for full, partial, and no payoff; every Finish your plan reason; the switch options.
- `pnpm test`: every script passed.
- `pnpm exec eslint` on the changed Plan, primitive, API, and script files: no errors. `pnpm exec prettier --write` on the changed frontend files.
- `pnpm exec tsc --noEmit`: only the three missing `.next/types` modules for the removed `/budgets`, `/institutions`, and `/transactions` routes, which were already there.
- Contrast and color distance for the series tokens were computed with a script, not checked by eye.
- Did not click through a signed-in session. The shell requires Clerk.

## Manual verification

No migration. Restart the API before opening Plan so the new response shape is loaded.

1. Open Plan with no debts.
   Expected: The title is Plan, with no switch. The page says a debt is required and links to Debts.
2. Open Plan with your current debts (PayPal Credit missing a due date, REI Mastercard not paying down).
   Expected: No switch, and the line under the title says payoff charts appear once a debt can be paid off. The summary reads "2 debts need attention before your plan can project a payoff," with the total owed as the big figure and monthly minimums beside it. The summary sentence sits in an amber box with a warning triangle. Finish your plan has an amber border and an amber icon in its title and on each row. It shows REI Mastercard with its monthly interest, your $45.00 payment, and the payment that starts paying it down, with Update payment; and PayPal Credit with Missing a due date and Add due date. The two action buttons are the same width and height. Each row goes to Debts.
3. Look at What you owe today.
   Expected: A donut with one slice per debt and the total in the middle. The legend lists each debt's balance and percent, and the percents add to 100. Hovering a slice or a legend row dims the others and replaces Total in the middle with that debt's name, balance, and percent. No floating box covers the middle. Moving away brings Total back.
4. Raise REI's payment above the amount Plan suggests and add PayPal's due date, or use a household where every debt pays off, then reopen Plan.
   Expected: The switch appears with Rollover pressed. "You're on track to be debt-free paying only your minimums. Anything extra brings that day closer." The big figure is the debt-free date under "Debt-free at minimums only", with "Back to you after payoff" ($… a month) and total interest beside it. The sentence is plain text, with no amber box. Finish your plan is gone. What you owe today stays.
5. Look at the balance chart.
   Expected: One colored band per debt, first payoff on top. The top edge steps down as each band thins to nothing. Hovering shows the month, the total owed, and each open debt with its color and amount. No entrance animation.
6. Look at the minimums and breathing room chart.
   Expected: Monthly minimums step down on each removal date. On Rollover, breathing room stays flat until the last debt is paid off. Monthly minimums are a dashed gray line and breathing room a solid green line over a light fill. The key above the chart shows the same dashed gray and solid green.
7. Press Keep freed payments.
   Expected: The description, summary, both payoff charts, and the payoff order change. Breathing room rises at each payoff, and the debt-free date is later. Each debt keeps its color, including in What you owe today.
8. In Payoff order, hover a row, then press it.
   Expected: Hovering dims the other bands. Pressing keeps the highlight until it is pressed again. Each row shows a swatch, the name, the payoff date, and the minimum it frees.
9. Open How this is calculated.
   Expected: Closed until opened. Then one row per rule, a bold term beside a single line (stacked under 640px). The fifth row reads Rollover, and after pressing Keep freed payments it reads Keep freed payments. No mention of reclaim amounts or a shared extra payment.
10. Narrow the window below 768px.
    Expected: The top bar shows the Tortoise wordmark on the left and your account on the right. The switch, when shown, spans the width under the title, and each option is easy to tap. The donut sits above its legend. Charts are about 220px tall. Tapping a chart opens its tooltip.
11. Tab through the page.
    Expected: The switch options, Finish your plan rows, payoff rows, and the disclosure each show a visible focus ring. Enter or Space presses the switch and the payoff rows.

## Approval

Approved by Zach on October 7, 2026, after the follow-up fixes in the correction notes. He expects more UI polish on Plan as later work brings it up.

## Pending decision

None in this slice. After approval, the next item is Plan screen item 2, the cash outlook. Before it starts, settle the stored due date before today noted in `docs/design/plan-page.md` under "To settle when item 2 starts".

## Correction

October 7, 2026. Zach reviewed the page and did not approve it. With PayPal Credit missing its terms and REI Mastercard not paying off, the page showed two identical cards, each with "$0.00 a month" and a long paragraph. Nothing told the eye where to look. The rework is Plan screen item 1 in `docs/roadmap.md`, designed in `docs/design/plan-page.md`: a one-sentence answer, Finish your plan, two debt charts, and a Rollover or Keep freed payments switch. This report will be updated with that page and a new checklist. After approval, the next item is Plan screen item 2, the cash outlook, not Phase 3 item 6.

October 7, 2026. The rework is built. The sections above now describe the reworked page and its checklist. The first page showed rollover and keep-all as two text cards from the server's explanation sentences; that page, `PlanRecoveryPath.tsx`, and `CashFlowRecoveryReportDto` are gone.

October 7, 2026. Zach opened the reworked page with nothing paying off and saw no chart, and the same screen on both sides of the switch. He asked for the current damage as a pie, the payment that starts paying REI down, and the total owed as the big figure. The sections above now include What you owe today, the hidden switch until a payoff exists, the attention-first summary, and the action labels.

October 7, 2026. Zach found How this is calculated a wall of text. The page now writes short labeled rules instead of the server's paragraphs, and the assumptions field is removed from the response.

October 7, 2026. Zach saw the donut tooltip overlap the total, Finish your plan buttons of different widths, and a warning shown as plain text. The donut now shows the hovered debt in its middle, the action buttons share one size, and a new `--warning` token marks the summary and Finish your plan. He chose amber over the existing red. `ui-governance.mdc` gained the warning, button-size, and chart-tooltip rules.

October 7, 2026. Zach saw "Interest is about $NaN a month" on REI Mastercard. The page had loaded from an API run built before `LastMonthInterest` and `LastMonthPayment` existed, so both fields were missing, and the copy checked only for null. The shortfall copy now falls back to "$45.00 a month doesn't pay this down" whenever either value is not a number, and a 16th plan test covers a response without those fields. The API running at 19:19 includes both fields.

October 7, 2026. Zach saw both lines in the minimums and breathing room key as the same green: `--chart-1` and `--success` are both dark greens at 2px. Monthly minimums are now a dashed `--muted-foreground` line, in the chart and the key. Breathing room stays solid `--success`. eslint is clean on the chart; tsc shows only the three existing `.next/types` errors.

October 7, 2026. Zach asked why the summary said $494.58 a month of breathing room while debts remained. That figure was the room after the last payoff, not today's. He wants the summary to encourage and make people feel in control, and noted that later work will bring the date sooner than minimums only. The big figure is now the debt-free date at minimums only, the sentence says anything extra brings that day closer, and the room is labeled "Back to you after payoff". The partial-payoff figure is labeled "Back to you each month after …". The 16 plan tests pass with the new copy; eslint is clean; tsc shows only the three existing `.next/types` errors.

October 7, 2026. Zach noticed the phone top bar had no app name, only the account. The leading slot fell back to nothing. `BrandMark` now has a compact `header` variant, the Tortoise wordmark alone at 1.125rem, and the phone bar uses it as the fallback. A page that puts back in that slot still shows back. This touches the shell on every screen, not only Plan. eslint is clean; tsc shows only the three existing `.next/types` errors.
