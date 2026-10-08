Date: October 8, 2026
Status: Awaiting review
PR:

# Documentation audit

## Increment

Applied the documentation audit. Living docs that described an earlier
product now match the shipped rules, and the same instruction is no longer
copied into several files.

## Changes

- `docs/README.md` Now lists only work that is waiting. Approved history
  stays in the roadmap and the review index.
- `AGENTS.md` and `frontend/AGENTS.md` point at `.cursor/rules/` instead of
  restating each rule. The generated Next.js block in `frontend/AGENTS.md`
  is unchanged.
- Decision 0005 is retitled "One light shell" and renamed to
  `docs/decisions/0005-one-light-shell.md`. Destination order stays in
  decision 0009. Decision 0007 stays in `decisions/` so the numbers stay in
  order. A superseded numbered decision now stays in that folder by
  convention. Decision records use status `Accepted`.
- Decision 0008 keeps "forward-looking charts live on Plan." The page shape
  stays in `docs/design/plan-page.md`.
- Decision 0004 no longer reads as if follow and freshness are still to build.
- `docs/design/linked-manual-debts.md` drops the October 6 snapshot that
  said credit limit, reconnect, and account-sync tests did not exist. The
  approved follow rules and the Liabilities notes remain.
- `docs/design/ui-direction.md` keeps the rationale, type and color tables,
  and screen map. Responsive behavior, warnings, charts, and actions stay
  in `.cursor/rules/ui-governance.mdc`.
- `docs/design/plan-page.md` shortens the history of the first Plan page.
  The roadmap's Plan screen items point at that design instead of restating
  the old one-page stack.
- Flexible-spending cash timing lives in `docs/design/living.md`. Savings
  points there and still says that amount does not join the protected pile.
- The roadmap's second status list, "First development queue," is removed.
  Success measures are now section 9. Phase status paragraphs stay, because
  they hold migration names and rules the item tables do not.
- The review index points at the Phase 0 and Phase 1 archive folders instead
  of listing every closed report. It still notes that several October 4
  reports were merged without a recorded approval.
- The original MVP checklist opens Connections at `/connections` and no
  longer says the code has two net-worth definitions.

## Data changes

None. No application code, dependency, route, API, schema, migration, or
financial record changed.

## Agent verification

Documentation only. No application build, lint, test, or browser check was
run.

## Manual verification

Please read these and say if any cut went too far:

- [ ] `docs/README.md` Now lists only waiting reviews.
- [ ] `AGENTS.md` is a map and pointers, and the rules you rely on are still
      in `.cursor/rules/`.
- [ ] Decision 0005 is the light shell, and 0009 is still the destination
      order.
- [ ] `docs/design/linked-manual-debts.md` no longer says follow, credit
      limit, or reconnect are missing.
- [ ] `docs/design/ui-direction.md` still has the colors, type sizes, and
      screen map you want kept.
- [ ] The roadmap no longer has a separate "First development queue."

## Approval

Awaiting Zach's review.
