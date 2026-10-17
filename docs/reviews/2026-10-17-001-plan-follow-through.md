Date: 2026-10-17
Status: Approved 2026-10-17
PR: None

# Plan follow-through

## Increment

Phase 5 item 1. Plan's Overview keeps the next-30-day figure and its one
action. Under that action, the working preview adds the remaining ranked
actions, the milestones the calculation produced, and the assumptions that
are still unset. The debt charts and the cash outlook stay as they are. No
model or price is chosen. There is no chat screen and no live OpenAI call.

## What you see first

Plan still opens on **Overview**. The first content is the steady-pay safety
figure. On a wide desktop it is the left column. The 30-day action is the
first thing in the column beside it, and the first thing under the figure on
a phone.

Under that action, **Next actions** lists the working preview's ranked steps
that are not already the 30-day action. The stored rank stays on each row, so
a list that starts at 2 is the plan's second step. The line above the list
says when the 30-day action is step 1. **Assumptions to confirm** follows
when bills, flexible spending, or cash to keep are unset. **Milestones**
follow when the calculation produced a date or amount. When a payoff date
is conditional, that note sits with the ranked action that states the date,
or with the milestone list when none of those actions does.

## What is secondary, and how you reach it

- **Assumptions to confirm** opens Bills, Living, or Savings and returns to
  Plan. A row already asked by the 30-day action, or by a listed next action,
  is not repeated. Other issues leaves out that same row. A shortfall in
  cash after protected savings stays under Other issues.
- Variable or uncertain pay stays beside the 30-day figure. An unchosen
  emergency target stays a review condition on Your plan. Calculation rules
  stay in Your plan's assumptions disclosure.
- The debt outlook under the new sections still names payment priority and
  the first payoff. Cash outlook, debt payoff, and the offer are unchanged.
- Your plan still keeps the version, save, accept, and print. Opening a
  version from History changes that view and the heading status. It does not
  replace the Overview column, which follows the working preview and the
  figures on screen.
- While the preview is loading, the column says it is preparing. **Try
  again** retries a failed preview. The previous scenario's actions are not
  shown. The 30-day figure stays.

## How the layout adapts

From 1280px up, the safety figure stays in the wide column. Next actions,
assumptions, milestones, the debt outlook, and other issues share the
narrower column. Below that, including a phone, the order is one column:
figure, 30-day action, then those sections. No new tab or destination. The
links and the retry button keep a 44px target. Home, Accounts, Activity, and
Plan stay in place.

## Decision

- The follow-through reads the decision preview Overview already loads for
  Your plan. It does not rank a second list, and it does not store a new
  action, milestone, or assumption.
- The 30-day action remains the one action for that figure. **Review
  shortfall**, missing cash, missing pay, missing debt terms, living costs,
  adding a debt, and **Try a change** stay that action.
- Assumptions to confirm are the three blanks the plan is going ahead
  without: no recurring non-debt bills, flexible spending not set, and cash
  to keep not set. A household with no such bills still sees that row,
  because there is no saved "none."
- Lowering the extra payment and holding a windfall open **Try changes**
  from this column. Those fields are not on Overview.
- Cardui does not schedule a payment or move money. Action states wait for
  item 2.

## Changes

- `planHomeFollowThrough` chooses the remaining actions, the three
  assumptions, the milestones, and the other-issue rows that would repeat
  them.
- Overview renders that result from the working preview. Your plan is
  unchanged.

## Data changes

None. No schema change and no migration. `PlanningModel:ApiKey` and
`PlanningModel:Model` stay blank. No money movement.

## Agent verification

- `node ./scripts/run-plan-follow-through-tests.mjs`: 9 passed, 0 failed. They cover a shortfall left out of the ranked list, an extra-payment action kept, add-a-debt and fix-terms covering different actions, the three assumptions and their links, a listed action replacing its assumption, living costs hiding flexible spending, a protected-savings shortfall keeping that issue, a conditional payoff milestone, and an unloaded preview.
- `pnpm exec tsc --noEmit`: passed.
- ESLint on the changed Plan files: passed.
- Prettier on those files: passed.
- No live OpenAI call was made.

The signed-in Plan page was opened against the local API. Overview led with
the 30-day figure and **Review shortfall**. **Next actions** started at step
2 and did not repeat that shortfall. The payoff caveat sat with the ranked
action that stated a payoff date. Milestones listed only calculated events.
**Assumptions to confirm** was absent because bills, flexible spending, and
cash to keep are already set. Once the terms action loaded, Other issues
dropped the general debt-payments row and kept the named debt plus the
protected-savings shortfall. **Review shortfall** opened and **Back to Overview** returned.
**Review debt payoff** from a ranked action opened the existing debt view,
and back returned. At 390px the order was the figure, the 30-day action,
next actions, milestones, debt outlook, then other issues, with no horizontal
page scroll. Your plan still showed all three ranked actions, including the
shortfall as action 1, with **Save draft** and **Print**. No financial
record was changed. A household with those three assumptions unset was not
on screen; the rule tests cover that list.

## Manual verification

Nothing to apply to the local database. Restart the frontend if it was
already open.

1. Open Plan. The 30-day figure is first. The one action for that figure is
   next. Any further ranked actions, unset assumptions, and calculated
   milestones sit under it. A covered plan with bills, flexible spending,
   and cash to keep set does not show **Assumptions to confirm**.
2. On a phone-width window, the same order is one column. The figure stays
   readable, and the links can be tapped.
3. Use a plan with no recurring bills. **Assumptions to confirm** names
   bills and opens Bills. Other issues does not list bills again.
4. When the next 30 days are short, the action is **Review shortfall**. The
   ranked list does not repeat "Cover the cash shortfall." A milestone may
   still name the shortfall date.
5. Change a scenario and press **Update preview**. The follow-through waits,
   then matches the new preview. Your plan can still save that preview.
   Nothing is paid or moved.

## Approval

Approved by Zach on October 17, 2026. This approves the remaining ranked
actions, the unset assumptions, and the calculated milestones under the
30-day action. It does not start item 2, and it does not choose a model or
a price.

The same change registers planning calls with the planning model. The sync
worker no longer builds that call service, so it can start while the
planning-model settings stay blank.

## Pending decision

None inside this slice. Which model to use, and what it costs, stays open.
There is still no chat screen. The next numbered roadmap item is Phase 5
item 2, action states and dependencies.

## Paste-ready prompt

Use this after the approval above is recorded.

```text
Phase 5 item 1 is approved. Start item 2.

Read AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-17-001-plan-follow-through.md, and docs/roadmap.md
first.

Plan follow-through is approved. No model or price is chosen. There is
still no chat screen. Detail panel keyboard focus stays tracked. Leave
PlanningModel:ApiKey and PlanningModel:Model blank. A live OpenAI call is
not part of the next increment unless the review says it is.

Phase 5 item 2 gives actions states such as proposed, chosen, waiting,
completed, and canceled. Support dependencies: unreceived loan funds cannot
finance today's payments. Ask before generating or applying a migration.
```
