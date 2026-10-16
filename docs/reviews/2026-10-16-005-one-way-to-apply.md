Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# One way to apply a change

## Increment

Phase 3 UX increment 2 from
[`2026-10-16-003-phase-3-ux-reassessment.md`](2026-10-16-003-phase-3-ux-reassessment.md).
Try changes has one way to apply a choice. The screen, the preview, and the
save stay the same result. Increment 3 is not in this change. Phase 4 is
not started.

## What you see first

On Try changes, the question is what one change does. The controls stay
above the result. **Update preview** is the action. Every choice, including
freed cash and payment priority, stays in the draft until that action. The
result says **This selection is not applied yet** while the draft and the
figures differ. Save stays unavailable for that difference.

After **Update preview**, the figures and the save use that result. A
scenario that already matches the report switches among the freed-cash
paths the report already returned, without another request. A second edit,
such as a payment priority or an extra payment beside that path, waits for
one request. The path and the figures change together when that request
succeeds.

## What is secondary, and how you reach it

- Overview still leads with the next-30-day figure. **Review shortfall**
  still explains that steady-pay figure. Typical pay stays a later view.
- Cash outlook, debt payoff, and Plan setup are unchanged.
- Compare an offer stays temporary and is not saved. The constant-rate
  limit sits beside the rate field. A balance transfer also says a
  promotional rate that changes later is not modeled.
- Payment priority names who is paid first and what the interest difference
  means, before a freed payment rolls. The unknown all-debts date in the
  comparison is unchanged: it names the first dated payoff and the debt
  that blocks a full date.

## How the layout adapts

Try changes keeps the current order. On a phone the controls come first and
the result follows, including the not-applied sentence. No new card, tab, or
destination. Home, Accounts, Activity, and Plan stay in place.

## Checks

- `node ./scripts/run-plan-scenario-draft-tests.mjs` — 12 passed
- `node ./scripts/run-restructuring-tests.mjs` — 4 passed
- `pnpm exec tsc --noEmit` — passed
- Prettier and ESLint on the changed frontend files — passed

No migration was run. No financial record was edited. The signed-in Plan
page was not exercised in the browser.

## Limits

- Increment 3 is not started. Card-payment labels, print, and save during a
  failed refresh are unchanged.
- A failed update still keeps the entered fields and the previous figures.
  Save stays unavailable until a later update succeeds.
- The comparison does not add a second explanation for an unknown all-debts
  date. Its columns compare both sides on the applied path. A path-only
  update can say no change there when the amounts did not change. The
  selected path is the one on the rest of the page and in the save.
- Detail panel keyboard focus stays tracked. Phase 3 behavior QA stays
  pending.

## Manual checklist

1. On Try changes, choose a different freed-cash path and do not press
   **Update preview**. The figures stay on the previous path. The result
   says the selection is not applied yet. Save stays unavailable.
2. Change a second field as well, such as payment priority or the extra
   payment. One **Update preview** applies both. The figures and Your plan
   then use that same result, and save becomes available.
3. With the other fields already applied, switch only among keep none, keep
   some, and keep all. **Update preview** uses the paths already listed.
   Changing the amount to keep still waits for the preview request.
4. The priority lines name who is paid first and what the interest
   difference means. They do not say "Avalanche stays the recommended
   order" or "Utilization cash".
5. An unknown all-debts date still names the first dated payoff and the
   debt that blocks a full date, and it does that once.
6. Open Compare an offer. The constant-rate limit sits beside the APR
   field. The comparison is not part of a saved plan.
7. Overview still leads with the next-30-day figure. **Review shortfall**
   is still that figure. An older version still says a missing shortfall
   amount was not stored.

## Approval

Approved by Zach on October 16, 2026. Increment 3 is next. This approval
does not start it.

## Paste-ready prompt for increment 3

```text
Implement Phase 3 UX increment 3 only: the remaining Plan edges, on synthetic data.

Read AGENTS.md, frontend/AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-005-one-way-to-apply.md,
docs/reviews/2026-10-16-003-phase-3-ux-reassessment.md,
docs/design/plan-page.md, docs/design/ui-direction.md, docs/roadmap.md,
and the applicable .cursor/rules files first.

Keep the current visual style, tokens, and primitives. Keep Home, Accounts,
Activity, and Plan. Keep the increment 1 layout and the increment 2 apply
rule: one Update preview, a pending selection says it is not applied yet,
and save matches the figures on screen. Do not start Phase 4, add a
dependency, or change financial records. Do not run a migration unless a
later approval says to.

Use synthetic data. A card payment is labeled once and left out of category
spending. Print names the version on screen. Save waits until the preview
has been calculated, including while a refresh has failed.

Do not reopen the next-month layout, the apply rule, or the offer. Verify
focused behavior and compilation, give Zach a short manual checklist, write
one review, and update the docs status and index. Stop for approval.
```
