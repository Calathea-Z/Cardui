Date: 2026-10-16
Status: Approved 2026-10-16
PR:

# Working scenario and saved versions

## Increment

Phase 3 UX increment 2, from
[`docs/design/phase-3-ux-handoff.md`](../design/phase-3-ux-handoff.md).
One working scenario, a current-facts comparison, and clear saved versions.
Increment 3 is not in this change.

## What you see first

Plan still opens on **Overview**. Above the three views, the working
scenario says **Current facts** or **Trying changes — not saved**. When the
calculated preview matches a stored version, that line says **Saved draft**
or **Accepted plan**.

One task is open: Pay extra, Change income, Add a windfall, Change living
costs, Protect more cash, or Change what happens after payoff. The applied
changes stay in a summary. **Update preview** sends every valid field
together. **Reset changes** restores current facts. Flexible spending and
cash to keep offer **Use saved amount** or an entered amount.

Under that, a comparison shows near-term cash safety, payoff, and total
interest against current facts. The same starting cash, date, and steady-pay
basis are named. Unknown payoff or interest stays unknown. A windfall is
named as cash that is not in steady-pay Spendable.

## What is secondary, and how you reach it

- **Review this plan** appears after a successful update and opens Your plan.
  If the window is still short, the line says so.
- Your plan names **Status**, **Version**, and the date. Save draft is
  unavailable until the working scenario has been calculated. Accepting says
  it replaces the accepted plan and keeps the older copy in history. Neither
  action schedules a payment or changes an account.
- A stored version shows its captured assumptions and scenario. The scenario
  above keeps editing the working preview. **Return to working preview**
  resumes it. Older copies are **History**.
- Invalidating risks stay visible. Assumptions and the captured scenario are
  one disclosure. Milestones and review are another.
- Compare an offer says it uses current minimum payments, leaves out the
  working extra payment, and is not saved. A balance transfer says the
  entered rate is used for the whole term. A lower payment is not called
  better. Validation sits by the fields.
- Allocation shows Debt, Savings, and Stays available as one split, with a
  $100 example that is not the household's money. Unsaved edits are named
  apart from the saved preference. Errors sit by the split.
- A card payment on Activity still says Card payment. The detail adds that
  purchases count as spending and paying the card is not spending again.

## Decision

- Update preview is the only way a field change is calculated. One invalid
  field blocks the whole update.
- A failed update keeps the entered fields and the previous figures. Those
  fields cannot be saved until a later update succeeds.
- Payoff and total interest are unknown when the path has no payoff date.
  A partial interest sum is not shown as the total.
- Cash safety is not compared directly when the income basis or the 30-day
  window differs.
- Print names the status, version, and date of the version on screen.
  Browser print remains the share path.

## Changes

- The working scenario sits above Overview, Try changes, and Your plan.
- Your plan identifies the version being viewed. History replaces the old
  version list label. Print uses that same identity.
- Offer, Allocation, and Activity copy from the handoff is on those screens.
  Offers are still temporary.

## Data changes

None. No migration. Offers are not stored. Saved plans use the existing
draft, accepted, and superseded versions.

## Layout

Desktop keeps the left sidebar from 768px up. Below that, the four bottom
destinations stay. The scenario tasks wrap. The comparison stacks on a
phone and lines up in three columns from 640px. Your plan actions wrap.
Print hides the scenario, the tabs, and the shell.

## Agent verification

- `pnpm test` in `frontend`: passed, including the new scenario draft and
  comparison script.
- `pnpm exec tsc --noEmit` in `frontend`: clean.
- Prettier and eslint on the changed frontend files: clean.
- The signed-in Plan page was not exercised in a browser. No local Plan
  response was available to this check within a few seconds, and Clerk
  sign-in is required.

## Manual verification

Use a phone width around 390×844 and a desktop around 1440×768.

1. On Plan, change extra payment and months without pay, then press Update
   preview once.
   Expected: both changes appear in the summary and the comparison. The
   status says Trying changes — not saved. One invalid field blocks the
   update and leaves what you typed.
2. Press Reset changes.
   Expected: the status returns to Current facts. Extra is 0, months are 0,
   and spending and cash to keep say Use saved amount.
3. Open Your plan, save a draft, then accept it. Open an older copy from
   History.
   Expected: the heading says Status, Version, and the date. Accepting says
   it replaces the accepted plan and keeps the older copy in history. The
   saved version does not show the scenario fields as if they edit that
   version. Return to working preview comes back to the draft you were
   trying. Print preview names that same status, version, and date.
4. Open Compare an offer, Allocation, and a card payment on Activity.
   Expected: the offer says it uses current minimum payments, leaves out the
   working extra, and is not saved. A balance transfer names the constant
   rate. Allocation shows Debt, Savings, and Stays available, a $100
   example, and an unsaved-changes line. The card payment says purchases
   count as spending and paying the card is not spending again.

## Approval

Approved by Zach on October 16, 2026.

## Pending decision

None in this increment. Do not start increment 3 until this review is
approved. Increment 3 is payoff-priority alternatives and partial reclaim.
Behavior QA remains pending. Detail panel keyboard focus stays tracked. Do
not start Phase 4.
