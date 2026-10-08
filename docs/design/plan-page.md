# Plan page

Status: Implemented. The three-view refinement and extra field layout are awaiting review; the underlying calculations remain approved.
Date: October 7, 2026
Updated: 2026-10-08

## Problem

The first Plan page, in `docs/reviews/2026-10-07-014-plan-recovery.md`,
rendered the cash-flow recovery report the way the report is built: two
path cards, each with a large "$0.00 a month" and a server-written
paragraph. Zach's review on October 7, 2026: the hierarchy does not tell
the eye where to look, and at a glance the screen does not say what it
does. The two cards were identical whenever nothing paid off. The things a
person can act on, a debt missing its terms and a debt that never pays
down, were buried in that paragraph.

This note designs the Plan page as an answer first, then what blocks the
plan, then the charts that show it working. Forward-looking charts live
here. Home keeps what already happened. See decision 0008.

## What exists today

- `GET /api/plan/recovery` loads the household's debts through
  `DebtsService.GetDebtsAsync`, so a followed balance and a followed
  credit limit are the amounts in use. `HouseholdRecovery.Prepare` builds
  the rollover input: avalanche order, no shared extra, no per-debt extra,
  no reclaim. `PayoffRollover.Compare` and `CashFlowRecovery.Track`
  produce the report. Nothing is saved.
- `PayoffRolloverPath` returns each debt's outcome, with its stop reason,
  payoff date, and interest. It does not return balances month by month.
- `CashForecast` (Phase 3 item 2) produces a 30-day view and 6, 12, and
  18 month horizons from income, bills, debts, savings, and starting
  cash. It pays each debt its own minimum and its own extra. It does not
  roll a freed minimum to another debt. No endpoint calls it.
- `recharts` is already used by `AccountsBalanceChart`. The chart tokens
  give three distinct colors: `--chart-1` and `--chart-3` are both
  `#1e4d3a`, and `--chart-2` and `--chart-4` are both `#c4b8a5`. Only
  `--chart-1` and `--chart-2` are used.

## The page

Plan has three tabs in one component tree: **Overview**, **Cash outlook**,
and **Debt payoff**. Overview is the default. The selected payoff behavior,
extra payment, cash timeframe, and highlighted debt stay in the page-level
state, so changing tabs or widths does not reset them. Only the selected
tab's analysis renders.

### Overview

1. **Combined status and next action.** The dated cash result controls the
   primary answer. A first cash shortfall leads with **Review shortfall**.
   Any payoff date is explicitly conditional while cash or available cash
   after protected savings runs short. When the plan is affordable, payoff
   remains the secondary result and the action opens Debt payoff.
2. **Try a scenario.** One disclosure groups **Roll payments forward**,
   **Free up cash**, and the extra monthly payment. Its closed summary names
   the applied strategy and amount. Updating or failed requests state which
   older amount the visible forecast still represents.
3. **Next 30 days.** A compact cash summary and the supported daily chart
   show ending cash and the lowest day. Cash balance is named separately
   from available cash after protected savings.
4. **Attention.** The primary issue is already in the status. A compact
   count opens to the other warning areas, healthy inputs, named debt
   blockers, freshness, exclusions, and links to the existing editors.

On a wide screen, the 30-day forecast sits beside the narrower Attention
panel. On a phone, status and its action come first, then the scenario
disclosure, chart, and collapsed attention details.

### Cash outlook

- A 30 days / 6 months / 12 months / 18 months control selects one view.
- Each view names its timeframe and shows ending cash, the lowest cash and
  date, and the relevant known minimum obligation.
- Only 30 days has daily points, so only that range draws a line. Longer
  ranges render the API's ending and low summaries and explicitly say that
  no intermediate line is available. The page does not invent values.
- The low-pay comparison follows the selected range under a disclosure.
  Source accounts, protected savings, Plan budget, omitted payments, bills,
  and currencies sit under a separate disclosure.
- Selected-period cash and protected-savings warnings name the period. The
  Overview status can still name a later first shortfall without implying
  that it occurs inside the 30-day chart.

### Debt payoff

- A compact milestone row names current minimums, the first payoff and
  removed payment, and modeled breathing room.
- The declining stacked balance chart is primary. Payoff order sits beside
  it on a wide screen and below it on a phone. Selecting a payoff row
  highlights its chart band.
- Every payoff row puts the debt name first, then labeled date and payment
  details. It says whether the removed payment rolls into another debt or
  is actually available for other uses.
- The minimums/breathing-room chart, current debt-share donut, and
  calculation assumptions are optional disclosures. The payoff rows and
  chart summaries keep essential information available without hover.

Without a payoff, Debt payoff explains what must be completed and Overview
keeps the debt details under Attention. A household with no debts gets a
Debts action. A failed load keeps the existing visible error.

### Desktop and mobile behavior

- The page uses a bounded `max-w-6xl` container. Related chart and attention
  content becomes two columns only when space permits.
- Under `md`, the page is one column with 16px horizontal padding. The
  three-tab bar stays visible below the fixed app header and does not add a
  second sticky panel.
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
- A debt's color comes from its position in the rollover order. It does
  not change when the switch changes. The chart, the tooltip, and the
  payoff order use the same color. A ninth debt reuses the first color,
  and its name still tells it apart.
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
- The plan DTO carries only what the page renders, for the rollover and
  keep-all paths: steps, starting and remaining obligation, recurring
  room, debt-free date, total interest, each debt's outcome (stop reason,
  balance, minimum, payoff date), and balance points. It also carries
  excluded currencies and the debts with no balance. The partial-reclaim
  path, `ReclaimAmount`, `Explanation`, and the written assumptions leave
  the DTO.
- Pure frontend rules build the chart series and the copy:
  `planChartSeries.ts` and `planCopy.ts`, tested from a node script in
  `pnpm test`.

## Items

Each item is one review.

1. **Page and debt charts.** The original summary, blocker list, balance
   chart, minimums and breathing room chart, payoff order, and assumptions
   disclosure were approved in `2026-10-07-014-plan-recovery.md`.
2. **Cash outlook.** The 30-day cash view and the 6, 12, and 18 month
   horizons, on Plan, following the switch. Starting cash is the Cash
   total on Accounts, in the household currency. Income uses typical pay.
   A low-pay view sits under a disclosure. Bills come from Bills. Savings
   contributions stay empty until Phase 2 item 7 adds a reserve. The
   forecast pays each debt from the selected path, not from its own
   minimum, so Rollover keeps a freed minimum in debt payments and Keep
   freed payments returns it to cash. No schema change expected.
3. **Three-view refinement.** Overview, Cash outlook, and Debt payoff reduce
   the initial scroll while preserving the same report, path comparison,
   extra-payment request, warnings, and source links. The extra field layout
   is included. It is awaiting review in
   `docs/reviews/2026-10-08-008-plan-page-refinement.md`. No schema, route,
   dependency, or new financial calculation.

## Decisions

- The Plan screen comes after Phase 3 item 5 and before item 6, so the
  approved calculations can be checked against real debts. Item 6
  scenarios and item 8 spendable estimates change what these charts show.
- Forward-looking charts live on Plan. Home keeps what already happened.
  Decision 0008.
- Two reviews: the page and both debt charts first, the cash outlook
  next.
- Rollover versus keeping freed payments is one scenario choice, not two
  stacked projections.
- The balance chart is stacked areas, one band per debt, in eight new
  jewel series colors. Zach chose jewel over earth and tonal sets, and
  stacked bands over one line per debt.
- The cash outlook uses typical pay, with low pay under a disclosure.

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

### Extra each month

Zach chose on October 7, 2026: the first scenario is one shared extra,
tried on the page, and the page replaces its numbers with that amount.

- The field is under **Try a scenario**, labeled Extra monthly payment.
  Zero is shown explicitly and is the minimums-only plan. A positive amount
  is applied when the field is left or Enter is pressed.
- The summary, both payoff charts, the payoff order, and the cash outlook
  all use that amount. The extra goes to the highest-interest debt that
  can take it, then to the next debt after that one is gone. It is part
  of the debt payments in the cash outlook.
- Leaving Plan clears it. Nothing is saved. A negative amount is not sent.
  While a request runs or fails, the page names the applied amount still
  represented by the visible results.

## Out of scope

- A custom payoff order or a reclaim amount. Those remain for a later
  scenario and for saved-plan facts in Phase 3 item 10.
- Saving the extra amount. The field above is tried on the page only.
- Opening one debt directly from a Finish your plan row. Rows link to
  Debts.
- Moving the two Accounts selectors onto the new primitive.
