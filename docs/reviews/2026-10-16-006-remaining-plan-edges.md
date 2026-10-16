Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Remaining Plan edges

## Increment

Phase 3 UX increment 3 from
[`2026-10-16-003-phase-3-ux-reassessment.md`](2026-10-16-003-phase-3-ux-reassessment.md).
A card payment is labeled once and left out of category spending. Print names
the version on screen. Save waits until that preview has been calculated,
including while a refresh has failed. The next-month layout, the apply rule,
and the offer are unchanged. Phase 4 is not started.

## What you see first

On Activity, a card payment's row answers what the transaction was. The one
classification is **Card payment**. The row names the account and does not
also name the spending category or mark it as a transfer. The amount stays
neutral. A purchase in that category still counts. The day's total leaves
the payment out.

On Your plan, the status, version, and date beside the heading are the lines
print uses. **Save draft** stays unavailable until the working scenario
matches the calculated recovery and the decision preview for that result has
loaded. While it has not, the button says the preview has not been calculated.
A failed refresh keeps the previous plan on screen, leaves save unavailable,
and offers **Try again**.

## What is secondary, and how you reach it

- The detail repeats **Card payment** once and explains that purchases count
  as spending and paying the card is not spending again. The stored category
  can still be edited there.
- A selection that has not been applied still says so on Try changes. One
  **Update preview** still applies the draft. Save stays unavailable while
  the draft and the figures differ.
- Overview still leads with the next-30-day figure. Cash outlook, debt
  payoff, and the offer are unchanged. The offer stays temporary.
- Print includes the assumptions and risks. The shell, tabs, scenario
  controls, and action buttons stay off the printed page.

## How the layout adapts

Activity rows stay one line of account text plus the mark. On a phone the
detail is still the full-screen sheet. Your plan still stacks the actions
on a phone and wraps the buttons. No new card, tab, or destination. Home,
Accounts, Activity, and Plan stay in place.

## Checks

- `node ./scripts/run-transaction-list-presentation-tests.mjs` — 6 passed
- `node ./scripts/run-card-activity-tests.mjs` — 8 passed
- `node ./scripts/run-saved-plan-tests.mjs` — 7 passed
- `node ./scripts/run-plan-scenario-draft-tests.mjs` — 12 passed
- `pnpm exec tsc --noEmit` — passed
- Prettier and ESLint on the changed frontend files — passed

No migration was run. No financial record was edited. The signed-in Activity
and Plan pages were not exercised in the browser. The checks use synthetic
rows and plan documents.

## Limits

- If a refresh fails and the previous decision plan still matches a saved
  draft, **Accept plan** remains for that version on screen. The new
  scenario is not saved until its preview loads.
- A same-scenario reload that is still running can keep save available,
  because that preview was already calculated. A failed refresh clears it.
- Detail panel keyboard focus stays tracked. Phase 3 behavior QA stays
  pending. This does not close Phase 3.

## Manual checklist

1. On Activity, find a card payment that is still in a spending category.
   The row says **Card payment** once. It does not also say the category or
   **Transfer**. The amount is neutral, and the day's total does not include
   it. A purchase in that category still counts.
2. Open that payment. The detail says **Card payment** once, then says
   purchases count as spending and paying the card is not spending again.
3. On Your plan, use the browser print preview for the working preview and
   for a version opened from History. Each print shows the same status,
   version, and date as the screen, with the assumptions and risks. The
   sidebar, tabs, and scenario controls are absent.
4. On Try changes, change a field and do not press **Update preview**. The
   result says the selection is not applied yet. Save stays unavailable.
5. Press **Update preview**. Before Your plan's preview finishes, or if that
   refresh fails, save stays unavailable and says the preview has not been
   calculated. The previous plan can stay on screen. **Try again** retries
   a failed refresh. After the preview loads, save matches that result.

## Approval

Approved by Zach on October 16, 2026. Phase 3 behavior QA is next. This
approval does not start it, and it does not start Phase 4.

## Paste-ready prompt for Phase 3 behavior QA

```text
Run the pending Phase 3 behavior QA. Do not start Phase 4.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-006-remaining-plan-edges.md, docs/roadmap.md,
and the Phase 3 reviews indexed in docs/reviews/README.md first.

Phase 3 items 1–11 and the October 16 UX increments are approved. This
pass checks that behavior. It does not add a feature, a dependency, or a
migration. Detail panel keyboard focus stays tracked and is not this pass.

Use synthetic data for any check that would change a record. Walk the
manual checks already written in the Phase 3 reviews, including next-month
safety, one Update preview, payoff choices, a saved draft and an accepted
plan, print of the version on screen, and a card payment labeled once and
left out of category spending. Record what passed and what failed in one
review, and update the docs status and index. Stop for approval.
```
