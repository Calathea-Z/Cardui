Date: 2026-10-16
Status: Approved 2026-10-16
PR:

# Payoff priority and partial reclaim

## Increment

Phase 3 UX increment 3, from
[`docs/design/phase-3-ux-handoff.md`](../design/phase-3-ux-handoff.md).
Payoff-priority alternatives and partial reclaim, through the existing
calculation, API, Plan screen, and saved-plan path. Increments 1 and 2
stay approved. This does not start Phase 4.

## What you see first

On Plan, **Change what happens after payoff** is one task in the working
scenario. It asks which payment order to follow and how much freed cash to
keep. **Update preview** still applies the choice. **Reset changes** returns
to highest rate first and keep none.

Payment priority is highest rate first, remove a minimum, lower utilization,
or pay one debt first or last. The last choice names one debt. Each choice
shows who is paid first and the interest tradeoff against avalanche. That
interest is before a freed payment rolls.

Freed cash is keep none, keep some, or keep all. Keep some asks for a monthly
amount. Each choice shows the last calculated payoff date, interest, and, when
both paths pay off, the cash kept. Unknown payoff or interest stays unknown.

## What is secondary, and how you reach it

- Debt payoff still lists chronological payoff: when each minimum ends.
  That list is separate from payment priority, which is who extra and a
  rolled payment follow.
- A debt's chart color follows its place in the selected payment priority.
  Keep none, keep some, and keep all share that order.
- Your plan stores the selected priority and freed-cash choice with the
  version. Opening an older version reads it. It is not rewritten by opening
  it.
- Assumptions say a freed payment starts the month after the modeled payment
  ends. The payoff month still pays that debt.

## Decision

- No migration. A saved plan is JSON on the existing plan-version document.
  New fields default to highest rate first, no constraint, and a reclaim
  amount of zero, so an older document still deserializes. The calculation
  version stays `1`.
- The comparison is pure avalanche plus the three supported alternatives:
  remove a minimum, lower utilization, and one pay-first or pay-last
  constraint. A full ranking of every debt is not a choice.
- One recovery response carries all three freed-cash paths for the selected
  payment order, plus the priority summaries. Changing priority, the
  constraint, or the amount requires Update preview. Switching among the
  three paths already returned does not.
- A saved version stores the constraint only when that priority is selected,
  and the reclaim amount only when the path is keep some. Keep none and keep
  all store a reclaim amount of zero.
- Saving an older draft again can create a new version, because the stored
  JSON now includes the default fields. The older row stays.

## Changes

- The recovery request accepts the priority, an optional one-debt constraint,
  and a reclaim amount. The response adds the keep-some path and the four
  priority summaries.
- The Plan task replaces the two-way payment switch with those choices.
- A saved draft and an accepted plan reproduce the selected path.

## Data changes

None. No new column and no migration. Older saved versions remain.

## Layout

The choices sit in the existing payoff task. Radios wrap on a phone. The
constraint debt and pay-first or pay-last choice sit side by side from the
small breakpoint and stack below it. The amount field appears only for keep
some. Desktop stays the left sidebar from 768px up. Below that, the four
bottom destinations stay.

## Agent verification

- `dotnet test` for payoff choice, decision-plan rules, payoff rollover,
  household recovery, cash outlook, spendable, and saved plan: 50 passed.
  The running API held `api\bin\Debug`, so this build wrote to `bin\verify`.
- Frontend behavior scripts for the Plan response, charts, safety, scenario
  draft, saved plan, and spendable copy: passed.
- `pnpm exec tsc --noEmit` in `frontend`: clean.
- Prettier and eslint on the changed frontend files: clean.
- The signed-in Plan page was not exercised in the browser. The API process
  already running is the previous build, so these choices are not on that
  server until it is restarted.

## Manual verification

Restart the API before this check. Use a phone width around 390×844 and a
desktop around 1440×768.

1. On Plan, open Change what happens after payoff. Choose a priority other
   than highest rate first and press Update preview.
   Expected: the tradeoff names avalanche, and it says the interest is before
   a freed payment rolls. Payment priority names who is paid first. Debt
   payoff still dates when each minimum ends. Those two orders are not the
   same list.
2. Compare keep none, keep some with an amount, and keep all, then update.
   Expected: payoff, interest, and cash kept change with the choice. Copy
   says the freed payment starts the month after the modeled payment ends.
   An unknown payoff stays unknown.
3. Save a draft and open that version.
   Expected: the stored version shows the priority and freed-cash choice you
   selected.
4. Open an older history version.
   Expected: it still opens, reads as highest rate first with its previous
   freed-cash choice, and is not rewritten by opening it.
5. Press Reset changes.
   Expected: payment priority returns to highest rate first and freed cash
   returns to keep none.

## Approval

Approved by Zach on October 16, 2026.

## Pending decision

Stop for approval. Behavior QA remains pending. Detail panel keyboard focus
stays tracked. Do not start Phase 4.
