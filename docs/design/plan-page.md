# Plan page

Status: Current design record. The original recovery and cash views are
approved. The implemented three-view refinement is approved. The offer
comparison is approved. Spendable estimates are approved. Saved versions
and the plain-language plan are approved. The October 15 navigation and
cash-first Overview change is approved in
[`2026-10-15-007-plan-navigation-and-safety.md`](../reviews/2026-10-15-007-plan-navigation-and-safety.md).
Increment 2, the working scenario and saved-version clarity, is approved in
[`2026-10-16-001-working-scenario-and-versions.md`](../reviews/2026-10-16-001-working-scenario-and-versions.md).
Increment 3, payoff priority and partial reclaim, is approved in
[`2026-10-16-002-payoff-priority-and-reclaim.md`](../reviews/2026-10-16-002-payoff-priority-and-reclaim.md).
Date: October 7, 2026
Updated: 2026-10-16

This document records page-specific Plan choices. It is not a universal page
template. Shared hierarchy, responsive, warning, chart, action, and
accessibility rules are in
[`.cursor/rules/ui-governance.mdc`](../../.cursor/rules/ui-governance.mdc).

## How the page got here

The first Plan page was two text cards. Zach's October 7 review replaced
that with one long page. The October 8 refinement split the same report
into Overview, Cash outlook, and Debt payoff. That refinement is approved
in
[`2026-10-08-008-plan-page-refinement.md`](../reviews/2026-10-08-008-plan-page-refinement.md).

Forward-looking charts still live on Plan. Home keeps historical results. See
[decision 0008](../decisions/0008-forward-charts-on-plan.md).

## What exists today

- `GET /api/plan/recovery` returns the debt-recovery paths and cash outlook
  used by the page. Followed debt balances and limits are the current debt
  amounts in that calculation.
- The report includes debt outcomes and monthly balance points, plus 30-day
  cash points and 6, 12, and 18 month cash summaries. It does not invent
  intermediate points for the longer horizons.
- The extra monthly amount, payoff behavior, income loss, windfall, flexible
  spending, and cash floor are one working scenario above the Plan views.
  Update preview applies the valid fields together. Reset restores current
  facts. A failed update keeps the entered fields and the previous result,
  and that uncalculated draft cannot be saved. The last applied preview
  stays in this browser session, so opening a setup editor and returning
  restores it. An offer comparison stays on the Plan page and is not part
  of that preview or of a saved version. The page does not schedule a
  payment or move money.
- Your plan keeps one draft and one accepted version for the household.
  Older copies stay as superseded versions. A saved version is the facts,
  assumptions, priorities, scenario, actions, milestones, and review
  conditions at that moment. Later edits to debts, pay, or bills do not
  change it.
- `recharts` and the implemented chart and series tokens are reused. No second
  chart library or palette is part of this design.

## Current implemented layout

Plan has three primary views in one component tree: **Overview**, **Try
changes**, and **Your plan**. Overview is the default. Cash outlook and
Debt payoff are supporting details opened from Overview or from a scenario
result. Compare an offer is an optional tool opened from Try changes. The
origin view stays selected, and Back returns to it. The working scenario sits above those views. The selected payoff
behavior, extra payment, cash timeframe, highlighted debt, and offer form
stay in the page-level state, so changing views or widths does not reset
them. Only the open view renders. Your plan is approved in
[`2026-10-15-005-saved-plan.md`](../reviews/2026-10-15-005-saved-plan.md).
This grouping is approved in
[`2026-10-15-007-plan-navigation-and-safety.md`](../reviews/2026-10-15-007-plan-navigation-and-safety.md).

### Overview

The question is whether the next 30 days are covered. The answer is the
steady-pay safety figure, then one next action, then a short debt outlook.

1. **Safety.** The figure is labeled **Spendable before extra debt
   payments** when the window is not already short. When the lowest balance
   after money set aside is below zero, the figure is that shortfall amount,
   labeled **Shortfall**. The basis line says steady pay only, before extra
   debt payments, and names the horizon. Protected money, balance freshness,
   and material missing or stale facts stay beside the answer. A missing
   required fact is an incomplete estimate, not a covered window. The
   lowest-balance date is named as the lowest point in the window. This
   estimate has no separate first-gap date, and the page does not invent
   one. The typical-pay cash chart is not on this view.
2. **Next action.** One action sits under the safety answer. A shortfall
   opens the cash outlook. Missing cash, steady pay, debt terms, or living
   costs open that existing editor and return to Plan. A covered window
   with debts offers **Try a change**. Other issues stay as a count that
   opens the remaining warnings and debt blockers. Healthy checks stay quiet.
3. **Debt outlook.** A short row names the next payment priority, the first
   expected payoff, and whether that payment becomes available or is
   redirected. Payment priority is who an extra or rolled payment follows.
   Payoff order is when each minimum is expected to end. A payoff date is
   explicitly conditional while this cash window is short, the extra-payment
   guardrail is active, or the typical-pay or protected-cash outlook is
   short. It is not presented as a due date.

An extra payment that newly leaves ordinary spending or a near-term bill or
debt payment short stays beside this result and says the figure is
calculated before that extra. The page does not subtract the extra to invent
an after-extra amount.

### Working scenario

The question is what change is being tried, and how it compares with current
facts. The scenario sits above Overview, Try changes, and Your plan.

One task is open at a time: Pay extra, Change income, Add a windfall, Change
living costs, Protect more cash, or Change what happens after payoff. The
applied changes stay summarized. Flexible spending and cash to keep use
**Use saved amount** or an entered amount, including zero. **Update preview**
sends every valid field together. **Reset changes** restores current facts.
While a request runs or fails, the figures on screen stay the previous
result and the fields stay as entered. **Retry** sends them again.

**Change what happens after payoff** compares four payment priorities:
highest rate first, remove a minimum, lower utilization, and pay one debt
first or last. The last choice names one debt. It is not a ranking of every
debt. Freed cash is keep none, keep some, or keep all. Keep some asks for a
monthly amount. The lines beside those choices are the last calculated
preview. **Update preview** applies them. Priority interest is before a
freed payment rolls. A freed payment starts the month after the modeled
payment ends. **Reset changes** returns to highest rate first and keep none.

The status is **Current facts**, **Trying changes — not saved**, **Saved
draft**, or **Accepted plan**. A compact comparison shows near-term cash
safety, payoff, and total interest for the same starting cash, date, and
steady-pay basis. A different income basis is not presented as the same
comparison. Unknown payoff or interest stays unknown. A windfall stays in
cash and is named as excluded from steady-pay Spendable. After a successful
update, **Review this plan** opens Your plan. If the window is still short,
that action says so.

### Try changes

Buttons open the cash outlook, debt payoff, and the offer comparison. The
offer says it compares current minimum payments, leaves out the working
scenario's extra payment, and is not saved with Your plan. Plan setup edits
saved facts and returns here.

### Cash outlook

Reached from Overview or Try changes. It is not a primary tab.

- A 30 days / 6 months / 12 months / 18 months control selects one view.
- Each chart is labeled typical pay or low pay, with its timeframe. A line
  says this outlook counts every included paycheck and is not the steady-pay
  Spendable figure.
- Each view names its timeframe and shows ending cash, the lowest cash and
  date, and the relevant known minimum obligation. When that lowest day is
  below zero, the caption says it is the lowest point, not necessarily the
  first day below zero.
- Only 30 days has daily points, so only that range draws a line. Longer
  ranges render the API's ending and low summaries and explicitly say that
  no intermediate line is available. The page does not invent values.
- The low-pay comparison follows the selected range under a disclosure.
  Source accounts, protected savings, Plan budget, omitted payments, bills,
  and currencies sit under a separate disclosure.

### Debt payoff

Reached from Overview or Try changes. It is not a primary tab.

- A compact milestone row names current minimums, the first payoff and
  removed payment, and modeled breathing room. While cash is short, a
  warning says the dates are conditional.
- The declining stacked balance chart plots debts with a projected payoff.
  The caption names how many debts are included and how many are left out.
  The top edge is those plotted balances. It is not labeled total owed.
  When a debt is left out, **Fix debt terms** opens Debts and returns to
  Plan. Payoff order is chronological and is labeled separately from the
  payment priority on Overview.
- Selecting a payoff row highlights its chart band.
- Every payoff row puts the debt name first, then labeled date and payment
  details. It says whether the removed payment rolls into another debt or
  is actually available for other uses.
- The minimums/breathing-room chart, current debt-share donut, and
  calculation assumptions are optional disclosures. The payoff rows and
  chart summaries keep essential information available without hover.

Without a payoff, Debt payoff explains what must be completed and Overview
keeps the debt details under the other-issues disclosure. A household with
no debts gets a Debts action. A failed load keeps the existing visible error.

### Compare an offer

Opened from Try changes. It is not a primary tab. The question is what one
offer changes compared with keeping the selected debts. The action is
**Compare offer**. Before a result, that form is the page. After a result,
the cost, the payment change, and the break-even date lead, and the form
stays underneath so the terms can be changed. The page says this comparison
is temporary and is not saved with Your plan.

The offer is a consolidation loan, a balance transfer, or a replacement
loan. A consolidation loan replaces two or more debts. A replacement loan
replaces one. A balance transfer replaces revolving debts only. Each
selected debt is included in full. The rate entered is used for the whole
term. One offer is compared at a time.

The result puts APR, fee, term, monthly payment, total interest, interest
and fees, and payoff beside the current debts. Break-even, the monthly cash
change, and upfront cash sit with that result. A lower payment is stated
and is not treated as the better offer. The page says the terms were
entered, that they are not approved, and that Cardui does not apply or move
money.

Upfront cash is a fee paid in cash plus cash applied to the new balance. A
fee added to the balance is borrowed. The comparison says when that cash is
more than the cash accounts, or would leave less than cash to keep. It does
not turn the cash into an investment return. Other income and bills stay as
they are. The current side uses today's minimums, not extra tried under Try
a scenario. That scenario stays off this tab.

On a phone the title and fields stack, and each compared fact names both
sides. From 640px up the amount fields sit in two columns and the facts
line up in three. The form stays within `max-w-xl`. The page stays
`max-w-6xl`. The three primary views stay one row at both widths.

### Your plan

The question is what to do, in order, and which version is on screen. The
heading names the status, the version, and the date. The leading action is
**Save draft**, **Accept plan** when the open draft matches the preview, or
**Print** when the accepted plan matches the preview. A stored version that
is not the working preview offers **Return to working preview**. Save stays
unavailable until the working scenario has been calculated. Accepting says
it replaces the accepted plan and keeps the older copy in history. At most
three actions lead. Each names why it matters, the expected cash-flow
effect, and a review link where one exists. Invalidating risks stay visible.
Assumptions and the captured scenario sit in one disclosure. Milestones,
other gaps, and review conditions sit in another. Older versions stay in
History.

The actions follow the preview from Try changes. Scenario controls stay on
Try changes. The preview is not a saved version until
**Save draft**. Saving the same plan again returns the open draft. Accepting
a draft marks the previous accepted plan superseded. Cardui does not pay a
bill, pay a debt, or move money.

On a phone the actions stack and the buttons wrap. Print uses the browser.
The printed page names the status, version, and date of the version on
screen, and includes its assumptions and risks. The shell, tabs, and
scenario controls stay off the printed page, and the supporting sections
print open. There is no PDF file and no public link.

### Desktop and mobile behavior

- The page uses a bounded `max-w-6xl` container. Related chart and attention
  content becomes two columns only when space permits.
- Under `md`, the page is one column with 16px horizontal padding. The
  tab bar stays visible below the fixed app header and does not add a
  second sticky panel. The three labels stay on one row at both widths.
- All tab and scenario controls retain 44px targets. Time ranges use two
  rows when needed. Charts use fewer axis labels, and the 30-day chart has
  a keyboard-readable values disclosure.
- There are no nested scroll areas or phone-only copies of the content.

### Type and controls

- One `h1`. Section titles are 0.875rem, 600 weight, sentence case. Rows
  are 0.875rem. Meta is 0.75rem. The big figure is 2rem, 600 weight,
  `tabular-nums`, one per page. See `docs/design/ui-direction.md`.
- Money uses `formatCurrency`. Chart labels use the chart formatters. The
  server-written explanation sentences, which write "45.00 USD", are not
  sent to the page. The page builds its copy from structured fields.
- Scenario and range choices use the existing pressed-button
  `segmented-control` with a 44px target on a phone. Plan's three primary
  views use tab semantics and arrow/Home/End keyboard movement.
- Each chart has `role="img"` and an `aria-label` that says what it shows.

### Chart colors and style

Zach's direction on October 7, 2026: the most beautiful charts on the
market. The chart tokens had three distinct colors, which is not enough
for one band per debt.

- Eight jewel series tokens, `--series-1` through `--series-8`, join the
  palette in `frontend/app/globals.css`, with `--color-series-N` entries
  for Tailwind. They are for charts that compare several things.
  `--chart-1` (net worth) and `--chart-2` (comparison) stay.
  `--chart-3`, `--chart-4`, and `--chart-5` are unused duplicates and
  are removed.
- Starting values, ordered so that neighbors contrast: emerald `#0f7b5f`,
  sapphire `#2b5fb3`, copper `#b8642e`, amethyst `#7b4fb5`, teal
  `#1a7f8c`, magenta `#a83f74`, amber `#a8740c`, indigo `#4a4fb0`. Each
  must be at least 3:1 against the white card, checked when built. A
  color that reads too close to `--success`, `--destructive`, or
  `--transfer` is adjusted. When built, emerald became `#00806e` and teal
  `#127c99`, because the starting emerald read close to `--success`. The
  contrast table is in `docs/reviews/2026-10-07-014-plan-recovery.md`.
- A debt's color comes from its position in the selected payment priority.
  Keep none, keep some, and keep all share that order, so the freed-cash
  choice does not recolor a debt. The chart, the tooltip, and the
  chronological payoff rows use the same color. A ninth debt reuses the
  first color, and its name still tells it apart.
- Bands: series color at about 90% opacity, a 1px white edge between
  bands, and a monotone curve.
- Grid and axes: horizontal grid lines in `--border` only. The y axis
  uses compact currency, and the x axis has four to six month ticks.
- Tooltip: a white card with a crosshair. It shows the month and the
  total, then each open debt with its swatch and a `tabular-nums` amount,
  in payoff order.
- Highlight: hovering or focusing a band or a payoff row dims the other
  bands to about 35%.
- Motion: no entrance animation. The highlight fades over 150ms, and not
  at all under `prefers-reduced-motion`.
- Phone: the chart is 220px tall, and tapping opens the tooltip.
- A debt that cannot be projected is not drawn. Overview Attention lists
  it.

## Data

- `PayoffRolloverRun` records the balance after each payment.
  `PayoffRolloverPath` returns those points as
  `PayoffBalancePoint(DebtId, DueDate, Balance)`. No schema change.
- `HouseholdRecovery.Prepare` also returns the debts left out for a
  missing balance, so the page can name them.
- The response uses structured fields for the recovery paths, cash outlook,
  exclusions, source facts, and debt blockers the page renders. The frontend
  does not depend on server-written explanation paragraphs.
- Pure frontend rules build chart series, hierarchy copy, and readiness copy.
  Focused Node tests cover those transformations.

## Items

The first four items are approved.

1. **Page and debt charts.** The original summary, blocker list, balance
   chart, minimums and breathing room chart, payoff order, and assumptions
   disclosure were approved in
   [`2026-10-07-014-plan-recovery.md`](../reviews/2026-10-07-014-plan-recovery.md).
2. **Cash outlook.** The 30-day cash view and the 6, 12, and 18 month
   horizons, on Plan, following the switch. Starting cash is the Cash
   total on Accounts, in the household currency. Typical income, Bills,
   Plan budget, protected savings, and the selected debt path feed the
   forecast. A low-pay view sits under disclosure. Rollover keeps a freed
   minimum in debt payments; Keep freed payments returns it to cash.
3. **Three-view refinement.** Overview, Cash outlook, and Debt payoff reduce
   the initial scroll while preserving the same report, path comparison,
   extra-payment request, warnings, and source links. The extra field layout
   is included. It is approved in
   [`2026-10-08-008-plan-page-refinement.md`](../reviews/2026-10-08-008-plan-page-refinement.md).
   No schema, route, dependency, or new financial calculation.
4. **Temporary cash scenarios.** Income loss, a one-time windfall, flexible
   spending, and cash to keep, tried with the extra payment under Try a
   scenario. Approved in
   [`2026-10-08-012-plan-cash-scenarios.md`](../reviews/2026-10-08-012-plan-cash-scenarios.md).
   No schema change.

## Decisions

- The Plan screen comes after Phase 3 item 5 and before item 6, so the
  approved calculations can be checked against real debts. Item 6
  scenarios and item 8 spendable estimates change what these charts show.
- Forward-looking charts live on Plan. Home keeps what already happened.
  See [decision 0008](../decisions/0008-forward-charts-on-plan.md).
- The original implementation sequence used separate reviews for debt charts
  and cash outlook. The later three-view refinement has its own review.
- Rollover versus keeping freed payments is one scenario choice, not two
  stacked projections.
- The balance chart is stacked areas, one band per debt, in eight new
  jewel series colors. Zach chose jewel over earth and tonal sets, and
  stacked bands over one line per debt.
- The cash outlook uses typical pay, with low pay under a disclosure.
- Spendable uses the next 30 days. Only steady pay counts. Variable pay,
  uncertain pay, and a one-time windfall are left out and named. When the
  lowest day after money set aside is below zero, the figure is that
  shortfall. Otherwise it is the lowest cash left after money set aside.
  It is not monthly income minus monthly bills. The lowest-balance date is
  not treated as the first day below zero. The typical-pay cash outlook is
  a separate supporting view.
- An extra payment is compared with the same path at minimums. It warns
  when ordinary spending, or a bill or debt payment inside those 30 days,
  would be short. That warning stays beside the safety result and says the
  figure is calculated before the extra. A gap that is already there
  without the extra stays in that result. The page does not invent an
  after-extra amount.

## Settled when item 2 started

- A stored due date before today. Zach chose on October 7, 2026: the
  payoff path and the cash outlook both start at the first monthly date
  on or after today, stepped from the stored date, so January 31 still
  goes to February 28 and then March 31. Past dates are not replayed, and
  the balance stays today's balance until that payment. Nothing moves a
  stored due date forward, so every date goes stale a month after entry;
  for that reason there is no warning. How this is calculated has a Due
  dates rule.

### Cash outlook layout

The original 30-day chart and three horizon cards were consolidated during
the October 8 refinement. The selected range now owns the heading, warning,
ending cash, low point, and minimum obligation. The API supports daily
inspection only for 30 days; 6/12/18 months remain honest horizon summaries.
Low pay follows the same selection under a disclosure. Without income, the
section still asks for an income source and links to Income.

### Temporary scenarios

Zach chose on October 7, 2026: the first scenario is one shared extra,
tried on the page, and the page replaces its numbers with that amount. The
rest of Phase 3 item 6 follows the same pattern for cash changes.

- Fields sit under **Try a scenario**. Extra monthly payment is shown with
  an explicit zero for the minimums-only plan. Months without pay, a
  one-time windfall with a date, flexible spending, and cash to keep are
  optional. A blank spending or floor field keeps the saved amount.
- Extra changes the payoff path and the cash outlook. Payment priority and
  the freed-cash choice change who is paid first and how much freed cash
  stays back. Income loss, a windfall, spending, and the cash floor change
  cash only. A windfall stays in cash and does not pay a debt.
- Months without pay leave out included pay from today for that many months,
  then pay resumes. Flexible spending replaces later full months; a smaller
  remainder this month stays. Cash to keep replaces only the floor inside
  protected savings.
- The last applied preview stays in this browser session. Uncommitted
  keystrokes stay through a failed update and are not written to the
  session. A version saved on Your plan stays. A negative amount is not
  sent. While a request runs or fails, the page says the visible forecast
  is still the previous one.

## Proposed future redesign

The awaiting
[`2026-10-08-003-planning-ux-review.md`](../reviews/2026-10-08-003-planning-ux-review.md)
proposes broader guidance for the next payment action, affordability
breakdowns, scenario comparisons, source links, and beginner-facing language.
Those recommendations are an assessment, not an approved design. They are not
implemented by the three-view refinement or by the shared governance update.

Any later Plan redesign must reconcile that proposal with the roadmap,
approved calculations, and this page's financial qualifications. It should be
recorded here as page-specific work before implementation. The shared
governance does not prescribe the number of Plan tabs, cards, charts, or
sections.

## Out of scope

- A ranking of every debt. The supported constraint pays one debt first or
  last. A saved version stores that choice and how much freed cash to keep.
- Variable-income and windfall allocation preferences stay the Allocation
  setting. They are not a plan version.
- Opening one debt directly from an Attention item. Current source links open
  Debts.
- Implementing the broader Planning UX proposals. Those stay unscheduled.
  Partial balances, more than one offer at a time, a rate that changes, and
  a promotional expiration stay in Phase 7.
