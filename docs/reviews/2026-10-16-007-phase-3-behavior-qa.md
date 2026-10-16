Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Phase 3 behavior QA

## Pass

This checks the approved Phase 3 behavior. It adds no feature, dependency,
or migration. Detail panel keyboard focus stays tracked and was not part of
this pass. Phase 4 is not started.

No walked check failed.

## Records this pass wrote

The signed-in household already had one saved draft. This pass saved a new
draft of a temporary working scenario and then accepted it. That document is
version 2, as of October 16, 2026, with status Accepted plan. It stores the
temporary choices used for the check: an extra monthly payment, two months
without included pay, remove a minimum first, and a chosen amount of each
freed payment kept. It also stores the shortfall amount from that preview.
Version 1 remains History, as of October 15, 2026, and still says its
shortfall amount was not stored. Accepting replaced the open draft. There is
one accepted plan and no second draft.

No debt, bill, transaction, or allocation percent was changed. After the
save, the working preview was reset to current facts. A later failed-refresh
check changed the browser preview and then reset it. That preview was not
saved. Cardui did not move money.

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

## Passed on the signed-in household

Next month. On desktop and at 390×844, Overview leads with the next-30-day
safety figure before any scenario field. This household's figure is a
shortfall. The heading status is Current facts. The basis names steady pay,
the window, and before extra debt payments. The lowest-balance date is named
as the lowest balance. One next action sits under the figure. Other issues
are secondary. The page does not use "first gap" wording. The phone heading
is clear of the header and the tab bar, and the safety figure is in view.
Plan's three views are one row. The phone tab bar is one row of Home,
Accounts, Activity, and Plan, and those tabs reached Home and Accounts. At
768px the desktop sidebar still fits its links. The page does not scroll
sideways at 390px.

Review shortfall repeats the Overview amount and the lowest-balance date
before typical pay. Typical pay and low pay are labeled apart from the
steady-pay figure. Opening Review shortfall from the older version shows
today's Overview figure. Version 1 still says its shortfall amount was not
stored.

Debt payoff dates are conditional because cash is short. The plotted caption
names one excluded debt and the way to fix its terms. The payoff order is
chronological and says payment priority is separate. The payment-priority
list and the payoff-date list are different lists.

One Update preview. With no edits, Try changes says a comparison appears
after update, and it does not show two matching columns. Changing the extra
and the months without Update says the selection is not applied yet, and the
summary stays on the previous result. Your plan keeps Save unavailable and
says the working scenario has not been calculated. An invalid month count
stays in the field, says to enter a whole number from 0 to 18, and is not
applied. One Update applies the extra and the months together. The status
becomes Trying changes — not saved. The comparison columns are Current
facts, Trying, and Change. The cash-safety row says the income basis
differs, so it is not a direct comparison. When only the current-facts side
is unknown, that column names the first dated payoff and the blocking debt.
When both sides are unknown, each column repeats that sentence and Change
says Unknown.

Payoff choices. The priority choices name who is paid first and the interest
difference versus the highest rate, and they say interest is counted before
a freed payment rolls. The screen does not say "Avalanche stays the
recommended order" or "Utilization cash." Selecting another priority, or
Keep all, without Update leaves the figures on the previous path and says
the selection is not applied. One Update applies both. During the update,
the screen says the results still show the previous forecast. Keep some
shows the monthly amount to keep, and changing that amount waits for
Update. The freed-payment copy says the freed payment starts the month
after the modeled payment ends. On the highest-rate paths, keep none and
keep all differed in date, interest, and cash kept. After the priority
change, the three freed-cash paths on this household all said payoff and
interest were unknown, because a debt still blocks a full date.

Extra-payment warning. When the horizon is already short, the cash
shortfall stays the first action. The extra-payment action names a date and
an amount short and says the horizon is already short without the extra.

Saved plan and print. Before a calculated preview, Your plan is Current
facts, Working preview, dated today, and Save is unavailable. After the
preview, save is available. The screen says the preview is not saved and
that saving replaces the open draft. Saving created version 2 as the draft
and moved version 1 to History. Accepting version 2 set Accepted plan and
says later edits do not change this version. Print of the working preview,
of version 1 while it was the saved draft, and of accepted version 2
matched the status, version, and date on screen, included assumptions and
risks, and hid the shell, tabs, and scenario controls. Print includes the
line that Cardui does not schedule a payment or change an account. After
save, the status line says Cardui did not move money. Reset returned the
working scenario to current facts. Reopening the accepted version showed
the same stored choices and no scenario inputs.

Offer. The comparison stays temporary, uses current minimums, leaves out
the working extra, and says it is not approved and does not move money. The
constant-rate sentence sits beside the APR. A balance transfer says a later
promotional rate is not modeled and that it replaces revolving debts. One
comparison showed both a lower payment and a higher cost, said a lower
payment is not the better offer, and said there is no break-even date.
Changing the offer type left the last numbers until Compare offer was
pressed again. A fee added to the balance increased the new balance and
said it does not use cash at the start. A fee paid in cash is named as cash
used now. An upfront amount above the cash accounts warned that it is more
than the cash accounts. Leaving Plan and returning cleared the form.

Plan setup. Five groups, with the existing editors, and Back to Plan. Bills
and debts are separate routes. The debt-terms warning is in the Bills and
debt payments group. Edit bills returns to Plan setup. Returning to Plan
kept Current facts.

Allocation. The saved split was already the example from its review. The
page says saving does not move money, and a Plan windfall stays in cash.
With no variable-marked source, the surplus sentence says the rule waits.
Entering shares over 100 percent marked the fields invalid and did not
save. Reload restored the saved split.

Card payment. On Activity, one payment is marked Card payment once, names
the account, and does not show a spending category or Transfer. The amount
uses the transfer color and has no plus. That day's total matches the other
rows and leaves the payment out. A purchase on that day still shows a
category and is in the total. A card credit on that day still shows a
category and a plus amount. The detail repeats Card payment once, explains
that purchases count as spending and paying the card is not spending again,
and still shows the stored category. The category was not edited.

Failed refresh. Blocking the recovery request left the previous forecast on
screen, kept the edits, and said the preview could not be applied. Save
draft stayed unavailable and said the working scenario has not been
calculated. Retry while the request was still blocked did not apply the
edit. After the block was removed, Retry applied it. Reset then returned
the working scenario to current facts. That preview was not saved. The
failed recovery refresh uses **Retry** on Try changes. **Try again** is the
Your plan control when the saved plan fails to load.

Navigation. The order is Home, Accounts, Activity, Plan, then Plan setup
and Settings. Plan setup is not a fifth phone tab. On a phone, Settings is
the account menu.

October 7 domain slices that have no screen were waived by those reviews.
Their tests were included in the Release run above. The older Plan-first
navigation expectation is historical. The current order is the one above.

## Not exercised

These are limits of the pass. They are not failed checks.

- The Your plan **Try again** control was not induced, because that requires
  the saved plan itself to fail to load. The recovery-refresh **Retry** was
  checked.
- Save was not sampled while a successful refresh was still running. The
  preview finished before Your plan was opened. The failed refresh and the
  uncalculated draft were checked.
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
- This household has income and debts, so the empty income and empty debt
  screens were not opened. The domain tests cover those cases.
- No installment loan was in the offer list, so that balance-transfer
  rejection was not available on screen. The restructuring tests cover it.
- The accepted document was not saved again, so this pass did not create a
  version 3. The new draft replaced the open draft.
- Detail panel keyboard focus was not checked.

## Approval

Approved by Zach on October 16, 2026. This approves the QA record. It does
not close Phase 3, and it does not start Phase 4. Detail panel keyboard
focus stays tracked.

## Paste-ready prompt

```text
Phase 3 behavior QA is approved. Do not start Phase 4.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-007-phase-3-behavior-qa.md, and docs/roadmap.md
first.

Phase 3 items 1–11, the October 16 UX increments, and the behavior QA are
approved. Phase 3 is not closed, and Phase 4 has not started. Detail panel
keyboard focus stays tracked.

The pending decision is whether to close Phase 3. Do not begin Phase 4
until Zach says to.
```
