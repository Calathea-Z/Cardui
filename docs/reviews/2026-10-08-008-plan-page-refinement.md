# Plan page refinement

Date: October 8, 2026
Status: Approved 2026-10-08
PR:

## Increment

This increment reduces visual overload and scrolling on Plan without changing
its recovery calculations, response contract, routes, or saved records. It
organizes the existing report into Overview, Cash outlook, and Debt payoff,
and includes the previously deferred extra-payment field layout.

## Changes

- Plan defaults to a short Overview with the affordability status, one
  contextual next action, a 30-day cash preview, and compact Attention
  details. Cash shortfalls and protected-savings pressure lead before less
  urgent setup improvements.
- Overview, Cash outlook, and Debt payoff are accessible tabs. Scenario,
  timeframe, and debt-highlight state stay in the page while tabs change.
  Under `md`, the tab bar stays below the existing fixed app header.
- The former readiness checklist and Finish your plan card are consolidated
  into one collapsed Attention summary. Missing, stale, inconsistent, and
  excluded inputs and named debt blockers remain available with their source
  links.
- Try a scenario groups Roll payments forward, Free up cash, and the extra
  monthly payment. It shows the applied zero amount explicitly, states that
  the preview resets on leaving Plan, and identifies the older amount still
  on screen while an update runs or fails.
- Cash outlook selects 30 days, 6 months, 12 months, or 18 months. The 30-day
  API points draw the existing chart and have a textual values disclosure.
  Longer ranges show only their supported ending, low, and minimum-obligation
  summaries; no intermediate points are invented.
- Debt payoff leads with the declining balance chart and adjacent payoff
  order. Rows state whether a removed payment rolls into another debt or is
  available for other uses. Debt share, minimum/breathing-room chart, and
  assumptions start under disclosures.
- The page uses a wider bounded desktop container and deliberate single-column
  mobile order. Existing chart colors, currency formatters, loading/error
  behavior, and financial contracts remain unchanged.
- Superseded full-readiness, Finish-list, and three-horizon-card components
  were removed.

## Data changes

None. This increment adds no dependency, API or DTO change, schema change,
migration, saved scenario, financial calculation, action tracking, or
application-record update. Scenario values remain temporary previews.

## Agent verification

- Frontend aggregate test command: passed all 21 scripts.
- Post-review focused Plan suite: 30 tests passed, including tab movement,
  selected cash ranges, unavailable horizons, affordability priority,
  applied-versus-pending scenario copy, and debt milestone meaning.
- `pnpm lint`: passed.
- `pnpm build`: passed, including TypeScript and the `/plan` production route.
- Targeted Prettier check for every changed Plan, test, and design file:
  passed.
- Full `pnpm format:check`: did not pass because 302 existing checkout files
  use formatting or line endings that differ from Prettier. Unrelated files
  were not rewritten; the changed-file check above passed.
- `git diff --check`: passed.
- IDE diagnostics for the changed frontend files: no errors.
- No automated browser QA was run.

## Manual QA

Zach completed the requested desktop/mobile checklist on October 8, 2026 and
reported that the expected behavior passed. The checklist covered:

1. Overview hierarchy, one next action, desktop forecast/attention layout,
   and all three tabs.
2. Scenario strategy and extra amount across tab changes, explicit zero, and
   reset-on-leave behavior.
3. Honest 30-day chart versus 6/12/18-month summaries, low-pay comparison,
   and source details.
4. Debt chart/order highlighting, rolled-versus-available payment copy, and
   collapsed secondary charts.
5. Narrow-width sticky tabs, collapsed scenario/attention details, clipping,
   and horizontal overflow.
6. Keyboard tab movement and access to controls, disclosures, payoff rows,
   and the cash values alternative.

The implementation itself made no application-data changes. Normal edits made
through linked source screens would persist as usual.

## Scope retained

- Income loss, windfalls, spending changes, protected-cash scenarios, saved
  plans, saved actions, and new calculations remain out of scope.
- The prior Phase 2 UX closure review remains awaiting approval. This report
  does not approve or rewrite it.
- The separate Planning UX review remains unapproved except for the Plan
  refinements explicitly requested and recorded here.

## Approval

Approved by Zach on October 8, 2026, with the other reviews that were
waiting. Overview, Cash outlook, and Debt payoff stay the Plan layout.

## Pending decision

Approve this Plan-page refinement. The bounded Phase 2 UX closure still needs
its own explicit approval before the remaining Phase 3 item 6 scenarios
resume.
