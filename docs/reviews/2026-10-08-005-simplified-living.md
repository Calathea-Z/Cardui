# Simplified Living

Date: October 8, 2026
Status: Approved 2026-10-08
PR:

## Increment

Phase 2 item 8 was simplified after auditing Living against everyday
spending. Living now has one canonical monthly flexible-spending input.
The duplicate allowance list and the unrealistic all-or-nothing payoff
comparison are gone.

## Decision

- Living remains the setting for unequal household contributions and
  sustainable flexible spending.
- Monthly living spending is the existing operating savings-goal row,
  renamed for its current purpose. It excludes scheduled obligations on
  Bills.
- A contributor amount is today's monthly benchmark. Its share of that
  person's scheduled pay is retained through low pay and future raises.
  Blank still means all recorded pay enters the shared plan.
- Savings owns protected cash and dated goals. Targets compare categorized
  Activity with intentions and do not add another amount to Plan.
- A future spending-change scenario must use an amount the person chooses.
  It must not assume all ordinary living spending can go to debt.

## Changes

- Removed `LifeAllowance`, its persistence configuration, CRUD endpoints,
  scheduling rules, payoff calculation, DTOs, and form/list UI.
- Renamed active everyday-spending concepts to living spending. Historical
  migration names remain unchanged.
- Living now shows pay available to the plan, monthly living spending, and
  monthly affordability. Current-month account tracking is optional detail.
- Plan schedules one living-spending cash-flow event and names the monthly
  amount in its summary and chart tooltip.
- Savings points monthly living spending to Living instead of rendering a
  duplicate card. Targets now states that it reports categorized Activity
  and does not alter Plan.
- Added integration coverage from saved contribution and living-spending
  inputs through Plan's forecast.

## Data changes

Zach approved and the agent ran:

```powershell
dotnet ef migrations add SimplifyLivingModel --project .\api --startup-project .\api
dotnet ef database update --project .\api --startup-project .\api
```

`20261008152308_AddLivingContributionsAndAllowances` had already been
applied. Before cleanup, `LifeAllowances` had zero rows and no contributor
had a non-null `MonthlyContribution`. The applied
`20261008154357_SimplifyLivingModel` migration drops only
`LifeAllowances`. It preserves contributor settings, savings goals,
transactions, balances, and other financial records.

## Agent verification

- Full backend suite: 482 passed.
- Focused Living and Plan integration suite: 33 passed.
- Plan chart tests: 24 passed.
- Savings tests: 4 passed.
- `dotnet ef migrations has-pending-model-changes`: no model changes remain
  outside the migration snapshot.
- Targeted ESLint and Prettier checks passed, including the clarified
  Targets copy.
- IDE lint inspection found no errors in edited files.
- `pnpm exec tsc --noEmit` still reports only three stale generated
  `.next/types` imports for the removed `/budgets`, `/institutions`, and
  `/transactions` routes. It reports no implementation error from this
  increment.

## Browser verification

Zach delegated the local walkthrough to the agent. No form was submitted
and no financial record was changed.

- At phone width, Living shows only pay available to the plan, monthly
  living spending, and monthly affordability.
- Existing values load. The monthly living-spending panel opens, Escape
  closes it, and focus returns to Edit.
- Savings has no duplicate living-spending card and points editing to
  Living.
- Targets says it compares categorized Activity and does not add another
  spending amount to Plan.
- Plan shows the monthly flexible living-spending amount once.
- Desktop accessibility inspection found the Living heading and content
  visible in the document. The screenshot tool rendered the main pane
  blank under temporary device emulation even though its DOM bounds,
  styles, and text were present. Emulation was cleared afterward.

## Approval

Approved by Zach on October 8, 2026.

## Pending decision

None for this increment. After approval, Phase 2 item 8 is complete and
the remaining Phase 3 item 6 scenarios are next.
