# Plan page

Status: Implemented. Items 1 and 2 are approved.
Date: October 7, 2026
Updated: 2026-10-07

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

Top to bottom.

1. **Title row.** The title is Plan. A switch with two options,
   Rollover and Keep freed payments, sits in the title row. It changes the
   summary, both charts, and the payoff order. A partial reclaim is not
   shown, because no reclaim amount is stored and zero matches rollover.
2. **Summary.** One sentence and one big figure. With a payoff, for
   example: "Debt-free by Aug 15, 2028. Minimums drop from $145 to $0."
   The big figure is recurring breathing room a month. Debt-free date and
   total interest are two quieter figures beside it. Without a payoff:
   "Your plan can't project a payoff yet," and the figure is today's
   monthly minimums.
3. **Finish your plan.** Shown only when something blocks the projection.
   One row per debt, with the fix, linking to Debts:
   - Missing a rate, a minimum, or a due date: name what is missing.
   - Does not pay down: "$45 a month doesn't pay this down."
   - Stops at the 50-year cap: say the payoff falls past that limit.
   - No balance: ask for the balance.
   - Another currency: name the currency the plan leaves out.
4. **Balance chart.** Stacked areas, one band per debt. The top edge is
   the total owed, and each band thins to nothing at its payoff. Bands
   stack in payoff order, first payoff on top, so the total steps down as
   each band goes. The tooltip and the payoff order name every debt, so
   color is not the only signal. See "Chart colors and style."
5. **Minimums and breathing room chart.** A step chart with two series:
   monthly minimums in `--chart-1` stepping down on each removal date, and
   breathing room in `--success` stepping up over a faint fill. On
   Rollover, breathing room stays flat until the last debt that can take a
   payment stops. On Keep freed payments, it rises at each payoff. Both
   series are labeled in text.
6. **Payoff order.** One row per debt: a swatch in that debt's chart
   color, the name, the payoff date, and the minimum it frees. Hovering or
   focusing a row highlights that debt's band.
7. **How this is calculated.** A disclosure with one row per rule: a
   short term (Order, Payments, Interest, Payoff month, Rollover or Keep
   freed payments, Left out, Currency, Limit, Saved) and one plain
   sentence. Only the freed-payment row follows the switch. The page
   writes these; the server's assumption paragraphs are not sent.

Without a payoff, both charts are hidden and Finish your plan leads. A
household with no debts sees the empty state that links to Debts. A
failed load keeps the error banner.

Revised October 7, 2026, after Zach saw the page with nothing paying off
(two identical screens and no chart):

- **What you owe today** is always shown after Finish your plan: a donut
  with one slice per debt sized by balance, the total in the middle, and a
  legend with each debt's balance and whole-number percent. Debts with no
  balance are named under it.
- Until a debt can be paid off, the switch is hidden, because both paths
  are the same. The line under the title says payoff charts appear once a
  debt can be paid off.
- Without a payoff, the summary says how many debts need attention, the
  big figure is the total owed today, and the known minimums sit beside it.
- With a full payoff, the big figure is the debt-free date, labeled
  "Debt-free at minimums only", and the sentence says anything extra
  brings that day closer. Breathing room sits beside it as "Back to you
  after payoff", so it is not read as money available today. Once extra
  payments exist, the same figure shows the earlier date.
- A debt whose payment does not cover interest names that interest and the
  whole-dollar payment that starts paying it down. Each Finish your plan
  row names its action, such as Add due date or Update payment. Each
  `PayoffBalancePoint` also carries that month's interest and payment for
  this.

### Type and controls

- One `h1`. Section titles are 0.875rem, 600 weight, sentence case. Rows
  are 0.875rem. Meta is 0.75rem. The big figure is 2rem, 600 weight,
  `tabular-nums`, one per page. See `docs/design/ui-direction.md`.
- Money uses `formatCurrency`. Chart labels use the chart formatters. The
  server-written explanation sentences, which write "45.00 USD", are not
  sent to the page. The page builds its copy from structured fields.
- The switch is a pressed-button group with `aria-pressed` and a 44px
  target on a phone. Three screens now use that pattern, so it becomes a
  shared `segmented-control` primitive in `frontend/components/ui`. The
  two Accounts selectors stay as they are.
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
- A debt that cannot be projected is not drawn. Finish your plan lists
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

1. **Page and debt charts.** The title-row switch, the summary, Finish
   your plan, the balance chart, the minimums and breathing room chart,
   the payoff order, and the assumptions disclosure. This reworks review
   `2026-10-07-014-plan-recovery.md`, which is still awaiting review.
   No schema change.
2. **Cash outlook.** The 30-day cash view and the 6, 12, and 18 month
   horizons, on Plan, following the switch. Starting cash is the Cash
   total on Accounts, in the household currency. Income uses typical pay.
   A low-pay view sits under a disclosure. Bills come from Bills. Savings
   contributions stay empty until Phase 2 item 7 adds a reserve. The
   forecast pays each debt from the selected path, not from its own
   minimum, so Rollover keeps a freed minimum in debt payments and Keep
   freed payments returns it to cash. No schema change expected.

## Decisions

- The Plan screen comes after Phase 3 item 5 and before item 6, so the
  approved calculations can be checked against real debts. Item 6
  scenarios and item 8 spendable estimates change what these charts show.
- Forward-looking charts live on Plan. Home keeps what already happened.
  Decision 0008.
- Two reviews: the page and both debt charts first, the cash outlook
  next.
- Rollover versus keeping freed payments is a switch, not two stacked
  sections.
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

Zach chose on October 7, 2026:

- One Cash outlook section after the payoff order, before How this is
  calculated. The debt answer stays first.
- The line under its title names the starting cash and, when the switch
  shows, what happens to a freed payment.
- One sentence about the next 18 months: the first short day and when
  cash recovers, or that it stays above zero, then the lowest point. A
  shortfall is a `--warning` callout.
- A 30-day step chart of cash at the end of each day, with a dashed zero
  line and a dot on the lowest day. Nothing is written inside the plot,
  so the tooltip covers no text. The tooltip names the day's income,
  bills, and debt payments with a plus or a minus.
- Three cards for 6, 12, and 18 months: ending cash, the lowest point,
  and the minimums still due. A card that goes short gets the warning
  border and icon.
- Notes: payments the plan can't project yet are left out for the debts
  Finish your plan lists, as a warning; no bills; another currency.
- "If pay comes in low" is a disclosure with the same sentence, chart,
  and cards at low pay. Low pay uses each source's low amount where
  recorded and leaves out raises. Without any low amount it says so.
- Without income, the section asks for an income source and links to
  Income.

## Out of scope

- Entering shared extra, a custom payoff order, or a reclaim amount.
  Those are scenario inputs for Phase 3 item 6 and saved-plan facts for
  item 10.
- Opening one debt directly from a Finish your plan row. Rows link to
  Debts.
- Moving the two Accounts selectors onto the new primitive.
