Date: 2026-10-17
Status: Approved 2026-10-17
PR: None

# Core journey sequence

## Increment

Planning only. Zach approved the direction, then asked for this further
revision before the report or the acceptance contract is treated as
approved. No feature is implemented, no live model is called, and no phone
access is turned on. The checks in the incorporated reviews were not run.

## Revision

Phone access moves next to the minimal chat increment. Early chat uses a
scripted planning model. PostgreSQL tests join CI in the same increment that
adds them. Findings 1 and 2 from the codebase review are scheduled here.
The worker-isolation finding, the history-loading finding, and the
organization notes stay in that review.

1. Confirm the journey and what Accept changes. Current budget amounts
   beside an accepted snapshot are live comparisons. They do not update or
   revalidate that snapshot.
2. Propose a concrete phone-access setup and stop for approval.
3. Scope the existing browser working plan to the signed-in household.
   No chat in this slice.
4. Add the minimal Plan chat view and the first persisted turn. Use the
   scripted planning model only in Development and the test host. Mark every
   scripted reply **Scripted reply**, including after reload. Add the
   PostgreSQL tests and the CI job.
5. Implement the approved phone setup and verify the manual journey and that
   minimal chat on the physical phone.
6. Let that view run the existing tools, with tests in the same CI job. A
   tool does not save.
7. Confirm a fact from that view, with tests in that job.
8. Review and accept a calculated plan from that view. Manual save and chat
   accept share one reviewed snapshot. Tests cover a change after preview
   and a failure during accept.
9. Run a capped live evaluation on synthetic data, using the existing OpenAI
   client.
10. Record the initial alpha journeys. The incomplete household explicitly
    tests no recurring bills.
11. Repair observed friction and add the coverage items 4–8 did not include.

Phase 4 items 1–9 stay approved as backend components. The phase exit is not
met. Phase 5 item 1 stays approved. Phase 5 items 2–5 stay paused. Phase 6
keeps launch hardening. Phase 7 stays later.

## Acceptance contract proposed for item 1

Current facts are the saved income, bills, debts, living amount, cash to
keep, and allocation. The working preview is the on-screen calculation,
including unsaved Try changes, and Overview follows it. The draft is one
saved snapshot.

Accept stores the scenario used for that calculation and replaces the
accepted plan. The previous version stays in history. Accept does not copy
flexible spending, cash to keep, or the other scenario choices into Living,
Savings, or Allocation, and it does not move money.

When the accepted flexible spending or cash to keep differs from the saved
setting, Your plan names both amounts and links to that existing editor. The
amounts read from the saved settings are live comparisons. Showing them does
not write the snapshot, refresh its stored scenario, or revalidate its
calculation. Saving in the editor updates the live setting and leaves the
accepted snapshot unchanged. Income loss, a windfall, an extra payment,
payoff priority, and freed cash stay inside the accepted plan.

A save or accept persists the reviewed calculation only when the facts and
scenario still match that review. A later change rejects it until the person
reviews the new figures. The browser does not supply the plan document.

Chat is a Plan view. Declining consent leaves the manual path. This contract
does not add a saved "no bills" value. Item 10 tests the unset row, and item
11 adds a saved none only if that walk shows the row blocks the journey.

The scripted planning model is the provider for items 4–8. It implements
`IPlanningModel`, is selected with `PlanningModel:Provider` set to
`Scripted`, and makes no outbound call. The API accepts `Scripted` only in
Development and in the test host. Production startup fails if it is set.
Every scripted reply is marked **Scripted reply** on the message, and the
stored turn keeps that mark. The API key and model stay blank until item 9.

## Verification

Documentation only. No application code, test run, live call, device check,
credential use, database change, or network change.

## Approval

Approved by Zach on October 17, 2026, including the acceptance contract.
The approval adds three conditions, and they are recorded above and in
`docs/roadmap.md`: scripted replies are labeled on the message, `Scripted`
is limited to Development and the test host, and the former item 3 is split
into household scoping and then the minimal chat view. No phone, chat,
identity, snapshot, or model check was run. Item 2 is the phone-access
proposal in
[`2026-10-17-006-phone-access-proposal.md`](2026-10-17-006-phone-access-proposal.md).
It stops before any access is turned on.

## Pending decision

None in this report. The phone setup is the pending decision in the item 2
proposal. Which OpenAI model to use, and the spending cap for item 9, stay
open. Hosting and `Plaid:WebhookUrl` stay open.

## Correction

October 17, 2026. Zach directed that desktop guided recovery and MX
connectivity come before phone access. Items 2 and 5 stay written here and
are not the next increments. The current order is in `docs/roadmap.md` and
`docs/README.md`.
