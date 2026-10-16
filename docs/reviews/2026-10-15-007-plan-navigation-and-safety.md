Date: 2026-10-15
Status: Approved 2026-10-16
PR:

# Plan navigation and cash-first Overview

## Increment

Phase 3 UX increment 1, from
[`docs/design/phase-3-ux-handoff.md`](../design/phase-3-ux-handoff.md).
Navigation, Plan setup grouping, and a cash-first Plan hierarchy. Increments
2 and 3 are not in this change.

## What you see first

Plan opens on **Overview**. The answer is the next 30 days: **Spendable
before extra debt payments** when that window is covered, or **Shortfall**
when the lowest balance after money set aside is below zero. The basis line
says steady pay only, before extra debt payments, and names the horizon.
One next action sits under that figure. A short debt outlook follows: who
is paid first, the earliest expected payoff, and whether that payment
becomes available or is redirected.

The other two primary views are **Try changes** and **Your plan**. They stay
on one row on a phone and on a desktop.

## What is secondary, and how you reach it

- **Cash outlook** and **Debt payoff** open from Overview or from Try
  changes. Back returns to the view that opened them. The typical-pay charts
  live only on the cash outlook, and each chart names typical pay or low pay
  and its timeframe.
- **Compare an offer** opens from Try changes. The page says the comparison
  is temporary and is not saved with Your plan.
- **Plan setup** is a desktop sidebar link and a Plan header link on a phone.
  It groups Income, Bills and Debts as separate lists, living costs, money
  to protect, and extra-money preferences. Each group shows the saved
  summary and Edit. The existing editor routes stay.
- **Settings** is a desktop sidebar link. On a phone it is the account menu:
  Categories, Connections, and Household. Spending targets is linked from
  Activity. Categories is also linked from Activity. Connections is also
  linked from Accounts.
- Other warnings and debt blockers stay behind a count. Healthy setup checks
  stay quiet.

## Decision

- Decision 0009's primary order stands: Home, Accounts, Activity, Plan.
- [Decision 0010](../decisions/0010-plan-setup-and-settings.md) records the
  secondary grouping. Plan setup and Settings are not a fifth destination.
  Editor data models are not merged.
- A shortfall amount is the absolute value of the lowest balance when that
  balance is below zero. The lowest-balance date is labeled as the lowest
  point. The page does not invent a first-gap date.
- A payoff date stays conditional while the 30-day window is short, the
  extra-payment guardrail is active, or the typical-pay or protected-cash
  outlook is short.
- The balance chart says which debts are plotted and which are left out. The
  top edge is those plotted balances.
- The last applied preview and payoff behavior stay in this browser session
  so a setup editor can return to Plan. Uncommitted keystrokes are not kept.
  That draft behavior is increment 2.

## Changes

- Desktop sidebar: four primary links, then Plan setup and Settings. The
  sidebar scrolls as a whole, so a short window does not shrink the primary
  links.
- Phone: four bottom destinations. Plan's view bar is one row of three.
- Plan setup at `/plan/setup` and Settings at `/settings`. Editor routes are
  unchanged. A return link appears when the editor was opened from Plan or
  Plan setup.
- Overview leads with the safety figure and one action. Cash, debt, and
  offer are supporting views in the same Plan page state.

## Data changes

None. No migration. Offers are still temporary. Saved plans are unchanged.

## Layout

Desktop keeps the left sidebar from 768px up. Plan setup and Settings sit
under the four destinations. Below that width, the bottom bar is Home,
Accounts, Activity, and Plan. Plan setup is a button in the Plan heading.
Settings opens from the account menu. Plan's three views stay one row.
Supporting views use a back control and do not add a tab.

## Agent verification

- `pnpm test` in `frontend`: passed, including the Plan chart, spendable,
  and safety scripts.
- `pnpm exec tsc --noEmit` in `frontend`: clean.
- Prettier and eslint on the changed frontend files: clean.
- The signed-in Plan page was not exercised in a browser. Clerk sign-in is
  required, and no signed-in session was available to this check.

## Manual verification

Use a phone width around 390×844 and a short desktop around 1440×768.

1. Open Home, Accounts, Activity, and Plan from the sidebar and from the
   phone tab bar.
   Expected: all four stay reachable. On the short desktop, those four links
   stay a normal height and the sidebar scrolls. The phone tab bar is one
   row of four. Plan's views are one row: Overview, Try changes, Your plan.
2. On Overview, read the safety figure before any chart.
   Expected: the label says Spendable before extra debt payments, or
   Shortfall when cash is already short. The basis names steady pay, the
   horizon, and before extra debt payments. The lowest-balance date is not
   called the first gap. One next action is under the figure.
3. Open the cash outlook and debt payoff, then Compare an offer from Try
   changes.
   Expected: cash charts say typical pay or low pay and that they are not
   the steady-pay figure. A plotted-debt caption names exclusions, and Fix
   debt terms appears when a debt is left out. A payoff stays conditional
   when cash is short. The offer says it is temporary and not saved.
4. Open Plan setup, then an editor, and use Back.
   Expected: the five groups match the saved facts, bills and debts stay
   separate, and the editor route is the existing one. Returning keeps the
   last applied scenario. Switching Overview, Try changes, and Your plan
   keeps that scenario and the offer form.

## Approval

Approved by Zach on October 16, 2026.

## Pending decision

None in this increment. Do not start increment 2 until this review is
approved. Increment 2 is one scenario draft with Update preview and Reset.
Increment 3 is payoff-priority alternatives and partial reclaim. Behavior
QA remains pending. Detail panel keyboard focus stays tracked. Do not start
Phase 4.
