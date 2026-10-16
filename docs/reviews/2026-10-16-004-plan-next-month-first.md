Date: 2026-10-16
Status: Approved 2026-10-16
PR: None

# Plan next month first

## Increment

Phase 3 UX increment 1 from
[`2026-10-16-003-phase-3-ux-reassessment.md`](2026-10-16-003-phase-3-ux-reassessment.md).
Overview answers whether the next 30 days are covered before any scenario
field. Increments 2 and 3 are not in this change. Phase 4 is not started.

## What you see first

Plan opens on **Overview**. The heading is Plan, with one status beside it:
**Current facts**, **Trying changes — not saved**, **Saved draft**,
**Accepted plan**, or **History** when an earlier version is open. Overview,
Try changes, and Your plan sit directly under that heading.

The first content is the steady-pay safety figure. On a wide desktop it is
the left column. The next action, debt outlook, and other issues sit in a
smaller column beside it. On a narrower window, including a phone, that
same order is one column. The view bar is in the page flow. It does not
stick over the safety heading.

## What is secondary, and how you reach it

- **Review shortfall** opens from the shortfall action. It repeats the
  Overview label, amount, lowest balance, money set aside, and basis, and
  says that is the next-30-day figure. **Typical pay** is the next section.
  **Look at typical pay** opens the cash chart. **Back to shortfall**
  returns. When the window is not short, Overview keeps a **Typical pay**
  text link so that chart is still reachable.
- Debt outlook and the issue count stay beside the figure, under the action.
- The scenario editor, **Update preview**, and **Reset changes** are on
  Try changes. Before a preview differs, that view says these are the
  current facts and does not show two matching columns. After it differs,
  the columns are **Current facts**, **Trying**, and **Change**.
- Cash outlook, debt payoff, Compare an offer, and Plan setup stay under
  the editor. Offers stay temporary.
- Your plan shows the stored shortfall when the version captured it. An
  older short version says the amount was not stored.

## How the layout adapts

From 1280px up, Overview uses a wide safety column and a supporting column.
Below that, the order is safety, next action, debt outlook, then issues.
Plan setup uses one list of full-width rows inside a `max-w-6xl` bound,
with **Back to Plan** above the heading. Bills and debts stay separate
editors on the bills row. The debt-terms warning is the debt count Plan
already lists. A failed plan load leaves that warning off.

## Stored shortfall

`lowestAvailable` and `lowestAvailableOn` are optional properties on the
existing plan JSON. A new version stores the unfloored lowest balance and
its date. Spendable stays floored at zero. An older document still opens:
those properties deserialize as null, and Your plan says the amount was not
stored. The old document is not rewritten, and today's calculation is not
copied in. No column was added. No migration was run.

## Checks

- `node ./scripts/run-plan-safety-tests.mjs` — 7 passed
- `node ./scripts/run-plan-scenario-draft-tests.mjs` — 11 passed
- `node ./scripts/run-saved-plan-tests.mjs` — 6 passed
- `pnpm exec tsc --noEmit` — passed
- Prettier and ESLint on the changed frontend files — passed
- `DecisionPlanRulesTests` — 11 passed. The running API had the normal
  `api\bin` output locked, so this run wrote to a temporary output folder.
  Restart the API before a newly saved version can store the new fields.

A signed-in local session was opened read-only. At 1440×768 the safety
figure is the first content, with the action, debt outlook, and issues
beside it. At 390×844 the same order is one column, the figure is on
screen, and the Plan view bar does not cover the heading. Review shortfall
repeats that figure and its lowest-balance date, then offers typical pay
under its own name. Back to shortfall returns. Try changes, before any
edit, says these are the current facts and does not show two columns. Plan
setup is one list of full-width rows, with specific edit actions, the
debt-terms warning on the bills row, and Back to Plan. The open older draft
says the shortfall amount was not stored. No preview was updated, no plan
was saved, and no financial record was edited. Household amounts are
omitted here.

## Limits

- The API process that is running now still composes the old plan JSON, so
  the working preview says the shortfall amount was not stored. Restart the
  API before checking a newly saved version. Do not expect the current
  process to write the new fields.
- Opening **Review shortfall** from an older version shows today's Overview
  figure. The stored fact line still says the amount was not stored.
- A draft saved before these fields will not match a new preview until it
  is saved again. The older row stays.
- Freed cash and every other scenario choice still wait for **Update
  preview**. Increment 2 is not started.
- The stored action sentence can still name the typical-pay cash date.
  That date is not the lowest-balance date.
- Detail panel keyboard focus stays tracked. Phase 3 behavior QA stays
  pending.

## Manual checklist

1. At 1440×768 and 390×844, the safety amount is visible before any
   scenario field. The only scenario text on Overview is the heading
   status. The phone heading is not covered by the view bar.
2. **Review shortfall** repeats the Overview amount and lowest-balance
   date before any typical-pay chart. Typical pay is named as a different
   view.
3. Restart the API. An older saved version does not show a shortfall
   amount taken from today. A version saved after the restart shows the
   amount it captured.
4. With no edits, Try changes does not show two matching columns. After a
   preview differs, the labels are Current facts, Trying, and Change. An
   unknown all-debts date names the first dated payoff and the debt that
   blocks a full date.
5. Plan setup rows scan at desktop width, name each edit, show the
   debt-terms warning on the bills row when Plan has one, and return to
   Plan. Bills and debts stay separate editors.

## Approval

Approved by Zach on October 16, 2026. Increment 2 is next. This approval
does not start it.

## Paste-ready prompt for increment 2

```text
Implement Phase 3 UX increment 2 only: one way to apply a change on Plan.

Read AGENTS.md, frontend/AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-16-004-plan-next-month-first.md,
docs/reviews/2026-10-16-003-phase-3-ux-reassessment.md,
docs/design/plan-page.md, docs/design/ui-direction.md, docs/roadmap.md,
and the applicable .cursor/rules files first.

The person is trying one change and needs the screen, the preview, and the
saved plan to be the same result. Keep the current visual style, tokens, and
primitives. Keep Home, Accounts, Activity, and Plan. Keep the increment 1
layout: the next-30-day figure stays first, Review shortfall stays the
steady-pay figure, and older versions still say a missing shortfall amount
was not stored. Do not start Phase 4, add a dependency, or change financial
records. Do not run a migration unless a later approval says to.

Every scenario choice, including freed cash and payment priority, stays
pending until Update preview. The result says when a selection is not
applied yet. Two edits apply together only after Update preview. Paths the
report already returned may answer that update without another request.
The figures on screen and the save payload change together. Save stays
blocked while they differ.

Replace "Avalanche stays the recommended order" and "Utilization cash" with
who is paid first and what the interest difference means. The comparison
already names an unknown all-debts date by its first dated payoff and the
debt that blocks a full date. Do not invent a date or amount, and do not
add a second explanation for that comparison.

Put the offer's constant-rate limit beside the rate field. The offer stays
temporary and is not saved.

Do not implement increment 3. Do not relabel card payments, change print, or
change save during a failed refresh. Verify focused behavior and compilation,
give Zach a short manual checklist, write one review, and update the docs
status and index. Stop for approval.
```
