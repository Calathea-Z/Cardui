Date: 2026-10-15
Status: Awaiting review
PR: None

# Phase 3 UX review

## Assessment

The main problem is decision hierarchy and workflow continuity, not the
visual theme. Plan asks the user to reconcile multiple financial reports
before deciding what to do. The shell also gives ten maintenance destinations
more reliable space than the four main destinations.

Recommended flow: **Check the next 30 days → Try changes → Review and keep.**
Keep deeper cash/debt analysis accessible as supporting detail. See the
[succinct implementation plan and Cursor prompt](../design/phase-3-ux-handoff.md).

## Scope and decisions from Zach

- Reviewed `feature/phase3` at `66538cd`. The working tree was clean before
  this assessment. Its difference from `dev` is five commits / 206 files,
  including scenarios, allocation, offers, spendable, card activity, and
  saved plans. Earlier Phase 3 calculation and UI work is already in `dev`.
- Inspected the branch change inventory, new user-facing components and
  state hooks, navigation, supporting calculation contracts, relevant
  reviews/designs, and the combined Phase 3 experience. This is not a
  line-by-line backend correctness or security audit of all 206 files.
- Zach authorizes substantial workflow/page reorganization while keeping
  the current style, including combining navigation by workflow. Nothing
  in the current layout must be preserved for its own sake.
- Missing payoff choices belong in a separate increment after core UX work.
  Offer comparisons remain temporary and separate from saved plans.

## Findings

| Priority | Finding and evidence | Required direction |
| --- | --- | --- |
| P1 | **Secondary navigation crowds out primary navigation.** Signed-in desktop DOM at 1440×768: the primary nav has 23px visible height for 172px of content; Settings occupies 524px. `desktop-sidebar.tsx` gives primary nav the shrinking space while `account-menu.tsx` always expands ten links. | Compact Plan setup and Settings entry points; group setup by workflow; retain contextual editing and existing routes. |
| P1 | **The cash-safety answer does not lead.** `PlanPageClient.tsx` renders the older summary and scenario control before Spendable. At 390×844, the three-row Plan navigation measured 156px tall and Spendable began around y=845 in the inspected initial Overview. | Cash-first Overview and fewer top-level Plan choices; move editing and analytics out of the initial answer. |
| P1 | **Adjacent amounts answer different questions.** The live Overview showed long-horizon lowest cash, steady-pay Spendable, and typical-pay cash values/dates. `spendableCopy.ts` explains the distinction in prose, but the reader must reconcile it. `SpendableRules.cs` calculates Spendable before extra payments; the extra is checked separately. | Label basis, timeframe, and before-extra meaning beside amounts. Show a shortfall amount prominently. Distinguish first shortfall from lowest balance: `spendableCopy.ts` currently describes `lowestOn` as when cash runs short. Do not invent an earlier date absent from the contract. |
| P1 | **Reassurance can contradict conservative safety.** `planSummary` uses typical-pay cash, while a steady-pay shortfall only replaces it for a specific extra-payment guardrail condition. Debt payoff shows dated milestones without its own prominent affordability qualification. | One consistent safety interpretation across views. Preserve steady-pay shortfall and incomplete-data conditions; qualify payoff dates locally, not only on Overview. |
| P1 | **The balance chart can look complete while excluding debts.** `balanceChart` includes only debts appearing in payoff steps, yet visible copy says the top edge is “total owed.” The inspected household had missing debt terms elsewhere. | Label the plotted subset and excluded debt count beside the chart; provide the repair path there. Distinguish priority order from chronological payoff order. |
| P1 | **Scenario editing lacks a dependable comparison cycle.** Fields commit separately on blur/Enter; each merges into the last applied report. Closely spaced changes can send an older combination and the latest response can drop a preceding edit. There is no single Reset or baseline/result summary. | One draft state and explicit Update preview; preserve pending/error input; compare the same facts; distinguish editing from displayed results. This request-order risk is source-derived, not reproduced against household data. |
| P1 | **Saving can target a different state from the displayed actions.** `useSavedPlan` retains old preview content during reload and save uses the current scenario key. Buttons disable only while saving. `PlanDecision` also mounts its own state only on that tab. | Explicit calculation readiness, no mismatched save, persistent working state, separate stored-version viewing. Test slow/failing updates with synthetic fixtures. |
| P2 | **The actions are descriptions, not a guided next step.** Live Your plan contained ordered actions but no action-level review links. Supporting content is eight disclosures. Historical viewing shares the screen with live scenario controls. | Contextual repair/review actions, grouped support, explicit working/draft/accepted contexts, direct access to the accepted version. |
| P2 | **Offer comparison needs a clearer boundary.** Its form/result are preserved across tabs, which is useful, but current-minimum versus scenario comparison scope is buried under assumptions. Results insert above a long form; mobile Compare was below the initial viewport. | Staged inputs, reachable result feedback, nearby validation, visible comparison scope and constant-rate limit. State that the result is not saved with the plan. |
| P2 | **Allocation reads like rules to decipher.** The saved status comes from `page.isSaved` even while inputs change; errors are toasts; prose repeats that money does not move. | Visible unsaved state, inline validation, one three-way split and a synthetic example, concise scope explanation. |
| P2 | **Activity naming is improved but needs a plain-language explanation.** Source adds Card payment stamps and excludes payments from displayed day totals. The detail panel adds a stamp without explaining why category spending excludes it. | Explain purchase versus payment once in detail; preserve neutral payment styling and inspect cash direction. Do not expand automatic balance behavior. |
| Functional gap | **The story exceeds the exposed payoff workflow.** `HouseholdRecovery.Prepare` passes an empty custom order and zero partial reclaim. Plan exposes only Rollover/ReclaimAll; saved versions capture those paths. | Separate increment for priority tradeoffs, pure-avalanche comparison, partial reclaim, and reproducible saved choices. Existing domain rules alone do not fulfill the user-facing story. |

Source paths above are under `frontend/features/plan/`,
`frontend/components/navigation/`, `frontend/components/auth/`,
`frontend/features/allocation/`, `frontend/features/transactions/`, and
`api/Domain/Recovery/` as applicable.

## Verification and limits

- Read-only signed-in browser inspection covered Plan's five views and
  Allocation, including desktop and phone viewport layouts. Inspected
  headings, navigation, disclosures, displayed results, controls, and DOM
  geometry. Screenshot capture was not reliable enough for comprehensive
  pixel-level assessment; this is not a visual approval or contrast audit.
- Activity's changes were source-reviewed; no populated card-payment row
  was verified in the browser. No scenario was submitted, offer compared,
  draft saved, plan accepted, or financial record edited during this review.
  Print behavior was source-reviewed, not exercised.
- Reused the existing local frontend after the startup command detected
  the running instance. Restored the temporary viewport override. No
  dependencies, migrations, application code, or financial data were changed.
- No compilation or application tests ran: this increment changes documents
  only. The future state/race findings require focused implementation tests.
- Personal amounts, account names, and screenshots are not included in these
  repository artifacts.

## Manual acceptance for the implementation

Use synthetic data for mutation checks. These checks are pending, not passed.

1. At phone and short desktop sizes, find Plan and explain next-30-day safety
   without opening a chart. A gap leads; incomplete inputs never imply safety.
2. Try two changes together, update, reset, and return from cash/debt detail.
   All entered choices survive; before/after uses identical financial bases.
   Temporary previews change no saved records.
3. Save a draft, accept it, change an input, revisit the accepted version,
   and print it. The stored version remains unchanged and identifiable.
   This check creates plan versions and changes the synthetic input record.
4. Compare an offer with a lower payment but higher cost. Both effects are
   clear and it is not included in the saved plan. Save allocation preferences
   separately; that check changes preferences but moves no money.
5. Check one card purchase and its payment: category spending counts the
   purchase once. Inspect rather than create live transactions.

## Approval

Awaiting Zach's review of the assessment and proposed implementation sequence.
No implementation approval is inferred from the scope answers.

## Pending decision and chat handoff

Approve or revise increment 1 in the linked handoff. Stop here under the
project review gate. A new Cursor chat is appropriate after approval: the
findings, decisions, source references, and first-increment prompt are saved.
Read `AGENTS.md`, `docs/README.md`, `docs/reviews/README.md`, this report, and
`docs/design/phase-3-ux-handoff.md` first. Phase 4 remains unscheduled.
