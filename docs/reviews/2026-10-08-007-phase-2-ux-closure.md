# Phase 2 UX closure

Date: October 8, 2026
Status: Approved 2026-10-08
PR:

## Increment

This bounded increment closes the product and UX gaps approved in the Phase 2
audit before Phase 3 item 6 resumes. It makes Plan lead with dated cash
affordability, coordinates the existing editors, reconciles paid-off debt
terms, and clarifies the jobs of Plan budget, Savings, and Spending targets.

## Changes

- Plan now replaces payoff reassurance with an affordability warning whenever
  dated cash or available cash after protected savings runs short. A debt-free
  date in that state is explicitly conditional.
- The near-term cash result appears before analytical debt charts. A new Plan
  readiness section identifies one next missing, stale, inconsistent, or
  unaffordable assumption and links directly to its existing editor.
- Plan explains the source and age of starting cash and debt balances. Connected
  cash is stale after more than two days, matching the existing followed-debt
  convention.
- A zero-balance debt with a saved positive minimum keeps its terms and appears
  in a review group. It no longer raises active debt, high-rate, utilization,
  minimum-payment, Plan, or Plan-budget totals.
- Living is displayed as **Plan budget** while `/living` remains its route.
  Savings explains that Cash to keep, Emergency, and named goals are additive
  protected-cash layers. Spending targets remains activity tracking and does
  not add another Plan amount.
- Product copy now describes a financial recovery plan instead of a personal
  ledger or generic tracking. The approved Home, Accounts, Activity, Plan order
  is unchanged.
- Primary navigation exposes its selected page. Activity keeps stale rows
  visible and announces refreshes without nesting another `main`. Connections
  no longer renders a top-level back control. Debt warnings use warning
  semantics, Household uses the shared On/Off Switch, and contribution
  benchmarks display the exact saved amount consistently.
- The approved design and UI-governance documents now record these terms and
  boundaries.

## Data changes

None. This increment adds no dependency, schema change, migration, route,
saved-plan model, calculation system, or financial-record update.

## Agent verification

- Full backend Release suite: 486 passed.
- Frontend focused and aggregate test scripts: all 20 passed, including new
  Plan readiness, affordability, zero-balance debt, savings, contribution, and
  Plan-budget copy coverage.
- `pnpm lint`: passed.
- `pnpm build`: passed.
- `pnpm format:check`: passed.
- `git diff --check`: passed.
- IDE diagnostics: no errors in `api`, `frontend`, or `tests`.

## Manual QA

Zach completed the requested checklist on October 8, 2026 and asked the agent
to proceed. No issue was reported. The checklist covered:

1. Plan's shortfall headline, conditional payoff date, and cash-first order.
2. Readiness links plus cash and debt source/freshness details.
3. Zero-balance debt review and exclusion from active totals.
4. Plan budget, Savings, and Spending targets boundaries.
5. Selected navigation, Activity refresh state, Connections navigation, and
   the shared Household Switch.

The implementation made no application-data changes. Normal edits made through
the existing editors during testing would persist as usual.

## Scope retained

- Phase 3 returning-user history, saved actions, saved plans, scenarios, and AI
  remain out of scope.
- The separately tracked Plan extra-payment layout and detail-panel focus work
  remain unscheduled.
- The Planning UX review remains unapproved except where the approved audit
  independently reached the same conclusion.

## Approval

Approved by Zach on October 8, 2026, with the other reviews that were
waiting. This approval includes the response-contract correction below.
Phase 3 item 6 can resume.

## Pending decision

Approve this closure increment before resuming the remaining Phase 3 item 6
scenarios.

## Correction — October 8, 2026

After the first manual-QA response, Zach found that `/plan` crashed while the
frontend was using an API process started before this increment changed the
Plan response. That older response had no `debtFacts`, and Plan readiness
called `.filter()` without validating the runtime contract. The earlier
statement that no issue was reported is therefore superseded by this note; the
increment remains awaiting review.

The server and browser Plan clients now reject a response that lacks the
current trust and provenance fields. Initial-load version skew becomes the
existing visible page error, while an extra-payment refresh keeps the previous
plan and reports its existing update error. It no longer reaches React as a
partial typed value. The local API was rebuilt and restarted with the current
contract.

Follow-up verification:

- Frontend aggregate suite: all 21 scripts passed, including three response
  contract tests.
- `pnpm lint`: passed.
- Targeted Prettier check: passed.
- IDE diagnostics: no errors in the changed response-boundary files.

A refreshed `/plan` check by Zach is still required before approval.
