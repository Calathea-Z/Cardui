# Phase 3 UX handoff for Cursor

Status: Increments 1, 2, and 3 are approved. Do not start Phase 4.
Updated: 2026-10-16

Companion assessment: [Phase 3 UX review](../reviews/2026-10-15-006-phase-3-ux-review.md).
This is a proposed implementation sequence, not approval to implement every increment.

## Outcome

A person should be able to answer: **Can I cover the next 30 days? What
should I change? Which plan am I keeping?** The current experience requires
them to reconcile reports before they can decide what to do.

Keep the existing light style, typography, tokens, and shared primitives.
Reorganize the workflow freely. Preserve the four main destinations in
their current order. Do not start Phase 4 or add dependencies.

## The experience to build

**Plan opens on Overview.** Lead with next-30-day safety, a clear amount,
its date range, and one relevant next action. Use three local destinations:
**Overview / Try changes / Your plan**. These are revisitable views, not a
mandatory wizard. Cash outlook and Debt payoff become supporting detail
views reached from Overview and scenario results. Compare an offer is an
optional tool reached from Try changes; its result never changes the plan.
Keep those details addressable and make returning preserve the working scenario.

### 1. Simplify navigation and organize setup

- Desktop sidebar: Home, Accounts, Activity, Plan, then compact secondary
  entries **Plan setup** and **Settings**. No permanently expanded list of
  ten editors. Main navigation must remain usable at short desktop heights.
- Phone: retain the four bottom destinations. Put Plan setup within Plan
  and Settings in the account menu. Use the same setup structure at both widths.
- Plan setup groups existing editors in an optional order:
  1. **Income:** pay sources and household contribution shares.
  2. **Bills and debt payments:** bills and debts, visibly separate lists.
  3. **Living costs:** the one flexible monthly spending amount.
  4. **Money to protect:** cash to keep, emergency savings, and named goals.
  5. **Extra money preferences:** variable-pay and windfall allocation.
- Show a concise saved summary and an Edit action for each group. Existing
  households skip completed setup; they are never forced to re-enter facts.
  Reuse existing editors and persistence. Keep existing URLs working and
  give contextual edits a return path to Plan without losing the scenario.
- Spending targets belongs with Activity; Categories is secondary to that
  workflow. Connections belongs with Accounts. Keep Categories, Connections,
  and Household discoverable in Settings as well. This groups access; it
  does not merge financial models or remove specialist screens.

### 2. Make Overview answer cash safety first

- Show **Next 30 days**, a supported safety status, and **Spendable before
  extra debt payments**. When short, lead with the shortfall amount rather
  than a reassuring zero. Distinguish the first shortfall date from the
  date of the lowest balance; do not relabel one as the other.
- Keep “steady pay only,” protected money, the horizon, and material missing
  or stale facts beside the answer. Missing facts mean an incomplete
  estimate, not a green assurance. Show one leading issue and a count/link
  for the others. Keep healthy setup checks quiet.
- An extra-payment warning must remain visible beside the scenario result.
  Explain that the existing Spendable amount is calculated before the extra.
  Do not subtract a monthly extra from a dated minimum to invent a new result.
  A trustworthy after-extra amount requires a corresponding server result.
- Put the next recommended action beneath safety, with a meaningful link
  such as Review cash shortfall, Fix debt terms, or Try a change. A shortfall
  review opens its dated explanation, not a generic budget editor.
- Follow with a small debt outlook: next payment priority, first expected
  payoff, and when its payment becomes available or is redirected. Keep
  payoff estimates explicitly conditional wherever cash is short.
- Move typical-pay charts, longer horizons, breakdowns, and assumptions to
  supporting detail. Label each chart's income basis and timeframe visibly.
  A typical-pay chart must not look like evidence for steady-pay Spendable.

### 3. Give scenarios one clear edit-and-review cycle

- Offer task choices: Pay extra, Change income, Add a windfall, Change living
  costs, Protect more cash, and Change what happens after payoff. Expand
  only the selected controls while keeping all applied changes discoverable.
- Use one working scenario above the local views. Inputs edit that draft;
  **Update preview** applies all valid fields together. **Reset changes**
  restores the current-facts baseline. Show defaults explicitly, with “Use
  saved amount” instead of making users interpret blank versus zero.
- Keep **Current facts** and **Trying changes — not saved** distinct from a
  saved draft or accepted version. Show a compact comparison of near-term
  cash safety, payoff estimate, and total interest. Compare like-for-like
  figures from the same starting facts, date, and income basis; show unknown
  values honestly. A windfall stays in cash and is excluded from steady-pay
  Spendable under the current calculation contract.
- Keep old results labeled while updating or on failure, retain entered
  values, provide Retry, and prevent saving an uncalculated working scenario.
  Show “Review this plan” after a successful calculation, including a short
  plan to address a shortfall when the result remains unsafe.

### 4. Connect the decision to the saved version

- Your plan leads with at most three ordered actions: what, why, cash-flow
  effect, and a contextual review/edit link where useful. Do not present a
  projected payoff date as the next payment due date.
- Keep three explicit contexts: **Working preview**, **Saved draft**, and
  **Accepted plan**. An accepted plan remains directly reachable even when
  new inputs make the working preview different. Older versions are History.
- State before acceptance that it replaces the accepted version and keeps
  the older copy in history. Saving or accepting only records a decision;
  neither operation schedules payments or changes financial accounts.
- While viewing a stored version, show its captured assumptions and scenario;
  do not display live editing controls as though they edit that version.
  “Return to working preview” resumes the retained working state.
- Keep invalidating risks visible; group supporting content into a few
  purposeful disclosures rather than eight equally prominent accordions.
  Print must identify the exact displayed version, date, and status and
  include its assumptions and risks. Browser print/Save as PDF is the share
  path; do not introduce a public sharing service.

### 5. Keep optional tools understandable

- Offer flow: choose debts, enter terms, compare. Lead results with payment
  change, total interest-and-fee change, and upfront cash impact; then show
  the full comparison and break-even. Explicitly say it compares current
  minimum payments, excludes the active extra-payment scenario, and is not
  saved with Your plan. Show constant-rate limitations beside balance-transfer
  inputs. A lower payment is not a “better” recommendation or approval.
  On update, bring the result into view and announce it; place validation by
  the fields. Do not add offer persistence in this pass.
- Allocation: present Debt / Savings / Stays available as one split with a
  clearly labeled synthetic example. Distinguish unsaved edits from saved
  preferences and show inline errors. Keep it in Extra money preferences,
  separate from the temporary windfall calculator.
- Activity: retain Card payment labels and add a short detail explanation:
  purchases count as spending; paying the card is not spending again. Keep
  cash direction understandable. Do not claim a manually maintained debt
  balance automatically updates unless the implemented data flow does so.

## Implementation sequence and acceptance

| Increment | Scope | Acceptance |
| --- | --- | --- |
| 1 — Find your way and understand safety | Compact navigation, setup grouping, three Plan views, cash-first Overview, labeled financial bases and conditional/excluded results | At 390×844 and 1440×768, main destinations remain reachable; safety and its next action precede charts and optional controls; no contradictory unqualified cash/payoff claims |
| 2 — Try, review, keep | Atomic scenario editing, current-facts comparison, retained state, saved-version clarity, offer/Allocation/Activity explanations | Multiple edits apply together; failure retains values and prevents mismatched saves; draft/accepted/history are distinguishable; print matches the viewed version |
| 3 — Complete payoff choices | Expose explained priority alternatives and partial payment reclaim through the full calculation/DTO/UI/saved-plan path | Compare pure avalanche with supported minimum-release, utilization, and user-constraint alternatives; compare keeping none/some/all freed cash; saved versions reproduce the selected path |

Increment 3 is functional work, not a styling task. Reuse the existing domain
rules, quantify tradeoffs, distinguish payment-priority order from chronological
payoff order, and preserve old saved versions. Review schema needs before
implementation. Release freed payments only after the modeled payment ends.

For each increment: focused type/lint checks and behavior tests appropriate
to the changed logic; Zach performs visual and end-to-end acceptance. No
broad test suite or new browser-testing dependency is required. Write one
review report, update its index and docs status, and stop for approval.

## Paste into Cursor after approving this assessment

```text
Review the Phase 3 UX handoff and implement increment 1 only.

Read AGENTS.md, frontend/AGENTS.md, docs/README.md, docs/reviews/README.md,
docs/reviews/2026-10-15-006-phase-3-ux-review.md,
docs/design/phase-3-ux-handoff.md, docs/design/plan-page.md,
docs/roadmap.md, and the applicable .cursor/rules files first.

The user is overwhelmed by data and destinations. Keep the current visual
style, but reorganize the experience around checking the next 30 days,
trying changes, and keeping a plan. Preserve Home / Accounts / Activity /
Plan. Replace the expanded secondary sidebar with Plan setup and Settings;
group existing setup editors by workflow without merging their data models.
Plan uses Overview / Try changes / Your plan, with cash and debt analysis
as supporting detail and offer comparison as an optional tool.

Make Overview cash-first. Label Spendable as before extra debt payments,
steady-pay only, with its horizon and material reliability limits. Lead with
shortfalls and a useful next action. Keep payoff estimates conditional and
explicitly label chart exclusions, income bases, and timeframes. Do not
invent financial values or treat the lowest-balance date as the first gap.
Fix the short-desktop navigation collapse and the phone's three-row tab bar.
Keep existing routes accessible and scenario state intact during local views.

Do not implement increments 2 or 3 yet. Offers remain temporary and separate
from saved plans. No dependency additions, data changes, migrations, or
deployment are authorized by this prompt. Use the existing shared primitives.
Align the design record and navigation governance with the approved grouping;
preserve historical reviews. Verify focused behavior and compilation, give
Zach a short manual checklist, write exactly one increment review and update
the docs status/index. Stop for Zach's explicit approval before increment 2.
```
