# Phase 3 behavior QA

Status: Historical
Updated: 2026-10-17

Approved by Zach on October 16, 2026. The approval records the pass. It did
not, by itself, close Phase 3. Phase 3 was closed later the same day. This
copy keeps the check results and the limits of the pass. It leaves out the
signed-in household's balances, versions, and shortfall.

No walked check failed. Detail panel keyboard focus was not part of this
pass. No debt, bill, transaction, or allocation percent was changed by the
pass. Cardui did not move money. A failed-refresh check changed the browser
preview and then reset it. That preview was not saved.

## Automated checks

- `pnpm test` in `frontend` passed. Counted scripts from this pass:
  transaction list presentation 6, plan scenario draft 12, card activity 8,
  saved plan 7, restructuring 4, and allocation 4. Spendable, plan safety,
  plan chart, and plan response also passed.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release` for the
  Phase 3 domain rules: 193 passed, 0 failed. Release was used so the
  running Debug API was not rebuilt. The filter covered cash-flow schedule,
  debt minimums, amortization, payment allocation, savings targets, cash
  forecast, payoff priority, payoff rollover, cash-flow recovery, household
  recovery, household cash outlook, plan cash scenarios, allocation
  preferences, restructuring comparison, spendable rules, card activity,
  transaction activity, decision plan, payoff choices, and transaction
  category classification.

These counts were not re-run on October 17, 2026.

## What the walk confirmed

The walk used the signed-in household on desktop and at 390×844. It confirmed
behavior, not a set of published figures.

- Overview leads with the next-30-day safety figure before any scenario
  field. The heading status can read Current facts. The basis names steady
  pay, the window, and before extra debt payments. One next action sits
  under the figure. The page does not use "first gap" wording. Plan's three
  views are one row. The phone tab bar is Home, Accounts, Activity, and
  Plan. At 768px the desktop sidebar still fits its links. The page does
  not scroll sideways at 390px.
- Try changes does not show a comparison until Update preview. An edit that
  is not updated stays unapplied. An invalid month count stays in the field
  and is not applied. One Update applies the extra and the months together.
  The status becomes Trying changes — not saved. Comparison columns are
  Current facts, Trying, and Change.
- A priority or freed-cash change waits for Update. During the update, the
  screen says the results still show the previous forecast. The freed
  payment starts the month after the modeled payment ends.
- When the horizon is already short, the cash shortfall stays the first
  action. The extra-payment warning names a date and an amount short.
- Your plan keeps Save unavailable until a preview has been calculated.
  Saving replaces the open draft. Accepting says later edits do not change
  that version. Print matched the status, version, and date on screen,
  included assumptions and risks, and hid the shell, tabs, and scenario
  controls. Print includes the line that Cardui does not schedule a payment
  or change an account. Reset returned the working scenario to current
  facts.
- An offer comparison stays temporary, uses current minimums, leaves out
  the working extra, and says it is not approved and does not move money.
  A lower payment is not treated as the better offer. A fee added to the
  balance does not use cash at the start. A fee paid in cash is named as
  cash used now. An upfront amount above the cash accounts warns. Leaving
  Plan and returning cleared the form.
- Plan setup has five groups and Back to Plan. Bills and debts are separate
  routes. Returning to Plan kept Current facts.
- Allocation says saving does not move money. Shares over 100 percent were
  marked invalid and did not save. Reload restored the saved split.
- On Activity, a card payment is marked Card payment, does not show a
  spending category, and is left out of that day's spending total. A
  purchase on that day still shows a category and is in the total.
- Blocking the recovery request left the previous forecast on screen, kept
  the edits, and said the preview could not be applied. Retry after the
  block was removed applied the edit. Reset then returned the working
  scenario to current facts.
- Navigation order is Home, Accounts, Activity, Plan, then Plan setup and
  Settings. Plan setup is not a fifth phone tab. On a phone, Settings is
  the account menu.

October 7 domain slices that have no screen were waived by those increments.
Their tests were included in the Release run above.

## Not exercised

These are limits of the pass. They are not failed checks.

- The Your plan **Try again** control was not induced, because that requires
  the saved plan itself to fail to load. The recovery-refresh **Retry** was
  checked.
- Save was not sampled while a successful refresh was still running.
- No live debt or bill was edited after accept. The accepted version stayed
  unchanged when the working scenario was reset, and it says later edits do
  not change it.
- No Plaid sync and no CSV import. An uncategorized card payment was not
  created. The card-activity and classifier tests cover that path.
- A mortgage payment, a card fee, and a bill autopay were not on the first
  Activity page. The tests cover a mortgage and an electric autopay.
- Home spending and Spending targets were not re-summed against every
  purchase. The Activity day total excluded the card payment and included
  the categorized purchase.
- The windfall sentence was read on Allocation. A windfall was not submitted
  on Plan.
- Empty income and empty debt screens were not opened. The domain tests
  cover those cases.
- No installment loan was in the offer list, so that balance-transfer
  rejection was not available on screen. The restructuring tests cover it.
- The accepted document was not saved again, so the pass did not create
  another version after accept. The new draft replaced the open draft.
- Detail panel keyboard focus was not checked.
