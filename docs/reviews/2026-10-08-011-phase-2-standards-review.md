Date: 2026-10-08
Status: Approved 2026-10-08
PR:

# Phase 2 standards review

## Increment

Reviewed the Phase 2 inventory and budgeting code against the standing
backend, frontend, security, and UI rules before Phase 3 continues. The
review did not reopen product behavior. Two missing helper comments were
added so those files match the method-comment rule.

## Conclusion

Phase 2 meets the standing code standards. Items 1–8, linked debts, and
the UX closure already in review are the inventory and budgeting
foundation. This review does not approve the UX closure, the Plan page
refinement, or the documentation audit. Those stay separate reviews.

## What was checked

- Income, bills, debts, category targets, savings, and Plan budget
  services, controllers, and domain rules.
- The same features in the frontend, plus Household where Plan budget
  reads contributors.
- Household scope, read tracking, projections, enum storage, domain
  purity, method comments, private-method placement, frontend layers,
  types, currency display, choice and date controls, warning color, and
  the selected-navigation mark.
- The Phase 2 exit in `docs/roadmap.md` and the approved product rules in
  the income, bill, debt, savings, and Plan budget designs.

## Standards that hold

- Each financial read is limited to the signed-in household. Phase 2
  controllers are not anonymous. Domain rules do not touch the database,
  HTTP, or a clock.
- Reads use `AsNoTracking` or a column projection. Writes load the row
  they change. Enums used by these features persist as member names.
- Services keep private methods in one region. Public service methods
  use `inheritdoc` on the implementation. Controllers name the method
  and route. A scan of these services, their domain folders, and their
  controllers found no method without a summary.
- Public writes validate, load, and save in separate methods. Debt
  follow, balance override, and credit-limit override stay in
  `DebtsService`, and each method does one of those jobs.
- Frontend API calls stay in hooks and server loads. Feature barrels
  export the route component. Forms use `Select`, `DateField`, and
  `Input`. Money text uses `formatCurrency`. Object shapes use `type`.
- Debt warnings use the warning color and icon treatment. Household
  on/off settings use `Switch`. Primary navigation exposes
  `aria-current`.

Focused domain and service tests already cover income rules, paycheck
dates, bills, debt terms, debt summary, follow and credit limit, category
targets, savings, and Plan budget. This review did not re-run them.
Comment-only edits do not change those results.

## Corrected here

- `pushMissing` in `frontend/features/debts/debtSummaryCopy.ts` now says
  that a zero count is left out of the missing-input sentence.
- `roundMoney` in `frontend/features/savings/savingsCopy.ts` keeps the
  cents-rounding comment that had been left above the comma check.
- `frontend/features/living/index.ts` now has the same route-export
  comment as the other Phase 2 barrels.

## Residual, not a Phase 2 blocker

`DebtsService` loads every balance snapshot for the linked accounts and
then keeps the latest in memory. The single-account read already asks
the database for the newest row. The unique index is account and date,
so the in-memory pick is the same row. A later pass can match the
cash-position query, which asks for the latest date per account. That
change does not belong in this review because it touches the balance
read without changing the number.

`DebtsService` is long because follow, overrides, credit limit, and the
summary share one service. The methods are already split by job. Splitting
the file is not required by the current rules.

Detail-panel keyboard focus stays tracked in the roadmap. It was already
excluded from Phase 2 closure.

## Still waiting, outside this review

- Phase 2 UX closure, including the refreshed `/plan` check in its
  October 8 correction.
- Plan page refinement.
- Documentation audit, responsive UI governance, and the handoff rule.
- The Planning UX review remains a proposal.

## Data changes

None. No dependency, schema, route, API, or financial record changed.

## Agent verification

Comment placement only. No application build, lint, test, or browser
check was run.

## Manual verification

No screen behavior changed. Please confirm this conclusion:

- [ ] Phase 2 can stay as the inventory and budgeting foundation.
- [ ] The full snapshot read in `DebtsService` can wait.
- [ ] Phase 3 item 6 still waits until the UX closure review is approved.

## Approval

Approved by Zach on October 8, 2026, with the other reviews that were
waiting. Phase 2 stays the inventory and budgeting foundation. The full
snapshot read in `DebtsService` waits. Phase 3 item 6 is next.

## Pending decision

Approve this standards review as the Phase 2 code gate. Phase 3 item 6
still waits on the UX closure review.
