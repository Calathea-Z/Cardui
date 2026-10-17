# Cardui roadmap

Status: Approved. Direction approved October 2, 2026.
Updated: 2026-10-17

Prepared September 25, 2026. Zach approved the direction on October 2, 2026 (`docs/reviews/archive/phase-0/2026-10-02-001-recovery-roadmap-direction.md`). It was based on the repository at that time, Zach's private planning notes in an untracked `Plan.md` (gitignored, not in the repository), the planning conversation, and the supplied capability inventory. Release boundaries below are recommendations, not approved scope cuts.

On October 17, 2026, Zach approved incorporating the user-journey assessment and the mobile-testing plan. That approval does not mean their proposed checks were run. He then approved the overall direction and asked for a revised sequence before the report or the acceptance contract is treated as approved. That revision is awaiting review in [`docs/reviews/2026-10-17-004-core-journey-sequence.md`](reviews/2026-10-17-004-core-journey-sequence.md). Approving it accepts the revised sequence and contract. It does not mark a future check as done.

What is being worked on now, what comes next, and open decisions are in [`docs/README.md`](README.md). The status paragraphs below record what each phase built.

## 1. Mission and first product outcome

**Help people turn financial overwhelm into a clear, realistic path toward stability, breathing room and freedom.**

Cardui helps people build, carry out, and revise a realistic financial recovery plan through guided conversation, budgeting, debt decision support and visible progress. It is not primarily another spending dashboard: budgeting explains where money went, while Cardui helps a person decide how to get out of the situation they are in.

The first complete experience: a person starts with incomplete information, builds a debt and cash-flow inventory over multiple sessions, sees which obligations are doing the most damage, and reviews a realistic recovery plan with a 30-day cash view and 6/12/18-month projections. The plan explains which debts to address, how much cash to keep, where extra money should go, when monthly breathing room improves, and what changes after a paycheck or change in circumstances. A user with a shortfall must receive an honest, useful plan rather than an invented surplus.

Budgeting remains a first-class feature. Chat and direct editing use the same saved financial facts and planning services. Users can inspect their plan and keep budgeting without an AI response.

### 1.1 Product direction and decision principles

Cardui's recovery experience is organized around four questions:

1. Which debt should I address next, and why?
2. How much cash should I keep available before accelerating payoff?
3. What happens if my income, spending, debt terms or available lump sum changes?
4. When does my financial situation begin to feel materially better month to month?

The major product pillars are:

- **Debt health:** one inventory of balances, APRs, minimums, utilization, debt types and missing terms, with transparent indicators of high interest, cash-flow burden and utilization damage.
- **Recovery strategy:** smart payoff prioritization that starts with avalanche economics but can explain the value of removing a minimum payment, reducing dangerous utilization or satisfying a user constraint. Paid-off minimums roll into the next debt by default in projections unless the user chooses otherwise.
- **Resilience:** emergency-buffer and sinking-fund planning, realistic quality-of-life spending, variable-income rules and behavioral guardrails that reduce the risk of paying down a card only to rely on it again.
- **Scenario decisions:** comparisons for extra payments, income loss, windfalls, consolidation, balance transfers, refinancing and—with plan-specific safeguards—retirement contribution or retirement-plan-loan tradeoffs.
- **Actionable progress:** prioritized next actions, 6/12/18-month projections, cash-flow recovery tracking and milestones such as positive cash flow, lower utilization, a removed minimum payment, a funded reserve or improved refinance viability.

Recommendations must show the assumptions, tradeoffs and reason for the ordering. Cardui should not equate recovery with eliminating all enjoyable spending, optimize only for the earliest theoretical debt-free date, or imply that a lower monthly payment is automatically a better deal.

## 2. Audit scope and verification

The September 25, 2026 audit this roadmap started from is archived in [`docs/archive/2026-09-25-recovery-audit.md`](archive/2026-09-25-recovery-audit.md). Zach's original specification is a private `Plan.md`, gitignored and not in the repository.

## 3. Original specification versus current implementation

Archived with the audit. Phases 0 and 1 below closed the gaps it found.

## 4. Release boundaries

| Release                      | Outcome                                                                                                                                                                                                                                           | Boundary                                                                                                         |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Foundation checkpoint        | Trustworthy account/transaction baseline with original MVP summaries                                                                                                                                                                              | Internal use; not a public launch claim                                                                          |
| Recovery MVP / private alpha | Multi-session guided intake, debt-health view, usable life-with-debt budget, protected cash buffer, smart payoff order, automatic payment rollover, core refinance/restructuring comparisons, 6/12/18-month projections, saved actions and review | Requires ownership/access controls before inviting external users                                                |
| Public beta                  | Proven isolation, operational recovery, connection lifecycle, export/deletion, validated recovery comparisons and an affordable operating model                                                                                                   | Validate calculations, costs and usability before broad access                                                   |
| Expanded decision support    | Richer restructuring, retirement-plan loans, contribution/match and employment tradeoffs                                                                                                                                                          | Add only with validated calculations, plan-specific inputs, explicit assumptions and appropriate source handling |

## 5. Ordered implementation backlog

### Phase 0 — Close the existing baseline

1. Create a repeatable development setup guide; replace stale setup assumptions without copying credentials into new documentation.
2. Add visible monthly spending, income versus spending, and category spending to the dashboard using existing services.
3. Centralize asset/liability and transfer/refund/pending conventions. Resolve the two net-worth definitions.
4. Audit balance-history gaps, transaction classification, and repeated sync behavior. Do not treat every incoming amount as earned income.
5. Exercise account connection, synchronization, transaction editing, and worker execution with controlled data.
6. Establish CI for existing tests/lint, builds, and future integration checks.

**Exit:** dashboard and account totals agree for the same data; original Phase 1 has a documented acceptance walkthrough; repeated imports do not duplicate activity or lose user edits. Record remaining live-environment limitations explicitly.

**Status (October 3, 2026):** Closed for local development. Items 1–4 are covered by the October 2–3 reviews. Item 5's account sync and transaction-edit survival were confirmed on October 3, 2026, and Zach confirmed the same day that the one-shot worker works. Item 6 has CI for existing tests, lint, the worker build, and the frontend production build. A PostgreSQL integration job stays deferred because no integration-test suite exists. Retained limitations are recorded in `docs/checklists/original-mvp-acceptance.md`. Phase 1 followed and closed on October 4, 2026.

### Phase 1 — Ownership, manual data, and durable financial facts

1. Add authentication and household ownership. Start with one signed-in owner per household; represent multiple contributors without requiring partner invitations in MVP.
2. Scope every read/write, summary, transfer-pairing operation, background job and later AI tool to the correct household. Derive identity on the server.
3. Migrate existing personal data to an explicit owner with backup, row-count checks and a recovery procedure. Review global category keys and uniqueness constraints during migration.
4. Make accounts and transactions independent of Plaid; store optional external identifiers plus source/provenance. Preserve existing linked data.
5. Add manual account/transaction creation, editing, archiving and balance reconciliation. Define opening balances separately from income.
6. Add financial profile preferences: currency, time zone, household contributors and visibility. MVP uses one planning currency; mixed-currency totals must be blocked or clearly excluded until conversion is supported.
7. Add CSV import preview/mapping, deduplication and batch undo before public beta; manual input is sufficient for the initial alpha slice.
8. Make manual use work without configured Plaid credentials; current startup validates Plaid configuration unconditionally.

**Exit:** two test households cannot access each other's records through any ID or aggregate endpoint; existing data remains intact; a new person can use the app without connecting a bank.

**Status (October 4, 2026):** Sign-in is locked to Clerk Hobby. The frontend requires a signed-in Clerk user, and the API verifies that session token, creates one household per owner, and scopes financial reads and writes to that household. Contributors remain household facts, without partner invitations. The local unscoped bank connections, and the custom categories and subgroup, were assigned to that household on October 3, 2026. Linked accounts and transactions still follow the Plaid item. They can also exist without one: external ids are optional, and each row stores source and provenance. Existing linked rows remain `Plaid` / `PlaidSync` and keep their ids. System categories and subgroups stay shared. Category and sub-group names and keys are unique inside a household together with the system rows, after `20261004142305_ScopeHouseholdCategoryNames`. `AddHouseholdOwner`, `ScopeHouseholdData`, `IndependentFinancialRecords`, `ManualFinancialRecords`, and `FinancialProfile` are in the API project. Manual accounts and transactions can be created, edited, archived, and reconciled. An opening balance is stored on the account and is not income. A statement difference is an adjustment, and that adjustment is excluded from income and spending. Financial profile preferences store one planning currency, a household time zone, and named contributors with a visibility flag. Totals include a blank currency and leave any other currency out, with a notice, until conversion exists. `FinancialProfile` was applied to the API database. CSV import with preview, column mapping, duplicate detection, and batch undo was approved on October 4, 2026. Manual entry remains. `20261004174550_AddTransactionImports` is applied. Item 8 was approved on October 4, 2026. The API and worker start without Plaid credentials. Bank linking and sync stay off until client id, secret, and environment are all set. A partial set still stops startup. Phase 1 items 1–8 are done. The CSV import UX section below was approved on October 4, 2026, and Phase 2 followed. Multifactor authentication, passkeys, a configurable session lifetime, and removal of Clerk branding wait until a Pro upgrade. See `docs/reviews/archive/phase-1/2026-10-03-007-phase-1-sign-in-decision.md`, `docs/reviews/archive/phase-1/2026-10-03-008-clerk-household-owner.md`, `docs/reviews/archive/phase-1/2026-10-03-009-household-scope.md`, `docs/reviews/archive/phase-1/2026-10-03-010-assign-household-rows.md`, `docs/reviews/archive/phase-1/2026-10-03-011-independent-financial-records.md`, `docs/reviews/archive/phase-1/2026-10-03-012-manual-accounts-and-transactions.md`, `docs/reviews/archive/phase-1/2026-10-03-018-financial-profile.md`, `docs/reviews/archive/phase-1/2026-10-04-008-csv-import.md`, `docs/reviews/archive/phase-1/2026-10-04-009-optional-plaid.md`, and `docs/reviews/archive/phase-1/2026-10-04-010-csv-import-ux.md`.

### CSV import UX — after Phase 1 item 8, before Phase 2

1. Rebuild the import sheet as a clear sequence: choose the account and file, map columns, preview and choose rows, then import. Undo stays available for an open batch.
2. Keep the approved behavior: preview, column mapping, duplicate detection, opening-date and future-date checks, and batch undo.
3. Leave fitting the sheet to phone and desktop, plus the broader keyboard and screen-reader pass, for Phase 6 item 4.

**Status (October 4, 2026):** Approved. The import sheet is a sequence: choose the account and file, map columns, preview and choose rows, then import. The mapping step says which column becomes the date, the name, and the amount, and the column names from the file are already selected. Preview, column mapping, duplicate detection, opening-date and future-date checks, and batch undo are unchanged. Undo for an open batch is on the first step and again after an import. Phone and desktop layout stay in Phase 6 item 4. See `docs/reviews/archive/phase-1/2026-10-04-010-csv-import-ux.md`.

### Phase 2 — Financial inventory and real budgeting

1. Capture income sources, take-home amount, cadence, next payment date, contributor and reliability. Support low/typical/strong scenarios and expected raises with effective dates.
2. Store actual paycheck schedules alongside monthly equivalents. Biweekly monthly averages must not become fictitious cash on a date. Capture gross income separately; use user-provided net estimates until a validated tax-estimation feature exists.
3. Capture bills and obligations with amount, frequency, due date, source account, essential/flexible classification and confirmation state. User-confirm recurring suggestions.
4. Capture debts with optional linked account, dated balance, APR, minimum, due date, revolving/installment type, credit limit/utilization where relevant, remaining term and promotional terms when applicable. Unknown terms remain unknown.
5. Add a debt-inventory and health view. Surface interest cost, minimum-payment burden, utilization, delinquency/promo risks and missing inputs without collapsing them into an unexplained score or shaming language.
6. Implement monthly category targets, spent/remaining amounts, copy-forward and explicit rollover behavior. Keep income, transfers, debt principal and spending semantics consistent.
7. Add operating cash reserve, emergency goal and sinking funds with targets and dates. Reserving money is not a second expense, and moving it between owned accounts is not income.
8. Include unequal household contributions and sustainable discretionary spending for vacations, hobbies and ordinary life. Show the payoff effect of these choices without treating all nonessential spending as failure. Avoid requiring every historical transaction to be cleaned before drafting a plan.

**Exit:** a user can create and maintain a realistic life-with-debt budget, see actual progress, include irregular bills and multiple income sources, set protected cash targets, and complete a debt inventory with understandable health indicators and visible gaps.

**Status (October 8, 2026):** Items 1–8 are done, so Phase 2 is implementation-complete. Item 6 is approved in `docs/reviews/2026-10-05-019-category-targets.md`. `20261006031630_AddCategoryTargets` is applied. Items 1–6 of "Sync correctness and linked debts" are approved. Item 7 is approved in `docs/reviews/2026-10-08-002-savings.md`. `20261008143017_AddEverydaySpendingAndCashToKeep` is applied. Item 8 is approved in `docs/reviews/2026-10-08-005-simplified-living.md`. Plan budget uses one monthly flexible-spending amount instead of a second list of allowances. Contribution benchmarks keep the same share through low pay and raises. `20261008152308_AddLivingContributionsAndAllowances` and `20261008154357_SimplifyLivingModel` are applied; together they keep `MonthlyContribution` and remove the empty allowance table. The all-or-nothing living-to-debt comparison was removed. A user-selected spending-change comparison remains in Phase 3 item 6. The Phase 2 product and UX audit, the bounded experience closure, and the Phase 2 standards review were approved on October 8. The closure is in `docs/reviews/2026-10-08-007-phase-2-ux-closure.md` and does not reopen the Phase 2 data-model backlog. The standards review is in `docs/reviews/2026-10-08-011-phase-2-standards-review.md`.

| Item                                                  | Status                                                    | Reviews                                                                                                                                                              |
| ----------------------------------------------------- | --------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1. Income sources, scenarios, expected raises         | Done                                                      | `docs/reviews/2026-10-04-016-income-sources.md`, `docs/reviews/2026-10-05-009-income-scenarios-and-raises.md`, `docs/reviews/2026-10-05-010-income-form-feedback.md` |
| 2. Paycheck schedules and gross pay                   | Done                                                      | `docs/reviews/2026-10-05-011-paycheck-schedules.md`                                                                                                                  |
| 3. Bills and recurring suggestions                    | Done                                                      | `docs/reviews/2026-10-05-012-bills.md`, `docs/reviews/2026-10-05-013-recurring-suggestions.md`                                                                       |
| 4. Debts                                              | Done                                                      | `docs/reviews/2026-10-05-014-debts.md`                                                                                                                               |
| 5. Debt inventory and health view                     | Done                                                      | `docs/reviews/2026-10-05-016-debt-summary.md`                                                                                                                        |
| 6. Monthly category targets                           | Done                                                      | `docs/reviews/2026-10-05-019-category-targets.md`                                                                                                                    |
| 7. Reserve, emergency goal, sinking funds             | Done. `AddEverydaySpendingAndCashToKeep` is applied       | [`docs/reviews/2026-10-08-002-savings.md`](reviews/2026-10-08-002-savings.md)                                                                                        |
| 8. Household contributions and discretionary spending | Done. The contribution and cleanup migrations are applied | [`docs/reviews/2026-10-08-005-simplified-living.md`](reviews/2026-10-08-005-simplified-living.md)                                                                    |

Rules that came out of items 1–5:

- An income amount is one payment, not a monthly figure. Low and strong pay are optional. An expected raise is a later typical amount, cannot be lower than typical pay, and the Income page asks the person to update typical pay or remove the raise when its date arrives.
- A biweekly payment stays a dated paycheck. A monthly figure is a yearly average, not cash on a date. Gross pay is optional and separate from net.
- A bill is one payment on a cadence, with a next due date, an optional source account, and essential or flexible. A repeated payment suggested from activity becomes a bill only when the person saves it. Not a bill hides that pattern.
- A debt records what the person knows. A blank term stays unknown, and zero is a known zero. Utilization is calculated only when the balance and the credit limit are both known.
- A connected card and a debt stay separate. Connecting a card does not create a debt, and a sync does not overwrite the debt's dated balance. When they differ, the plan keeps the debt's balance until the person chooses the connection's balance.
- The Debts summary shows one month of interest, minimums, utilization, a passed due date, a promotion ending or ended, and missing inputs, with no score.

Applied migrations: `20261005025504_AddIncomeSources`, `20261005031455_DropIncomeSourceArchivedAt`, `20261005150122_AddIncomeScenariosAndRaises`, `20261005180946_AddIncomeGrossPay`, `20261005183259_AddObligations`, `20261005191328_AddRecurringSuggestionConfirmation`, and `20261005202726_AddDebts`.

### Sync correctness and linked debts — after Phase 2 item 5, before Phase 2 item 6

A manually entered debt can optionally follow a connected credit card or loan, so the person does not retype the balance after every payment. Design: [`docs/design/linked-manual-debts.md`](design/linked-manual-debts.md), approved for the plan on October 6, 2026. Each item is one review.

**Status (October 7, 2026):** Item 1 is approved in `docs/reviews/2026-10-06-002-sync-correctness.md`. Item 2 is approved in `docs/reviews/2026-10-07-002-follow-a-balance.md`. Item 3 is approved in `docs/reviews/2026-10-07-003-balance-overrides.md`. Item 4 is approved in `docs/reviews/2026-10-07-005-suggested-matches.md`. Item 5 is approved in `docs/reviews/2026-10-07-006-credit-limit.md`. `20261007162523_AddDebtAccountFollow` and `20261007193207_AddAccountCreditLimit` are in the repo. Item 6 is approved in `docs/reviews/2026-10-07-007-reconnect.md`. No schema change.

1. Sync correctness. Add `PlaidAccountSyncService` tests, decide and guard overlapping worker and manual sync for one item, and test sync timestamps after an interrupted sync. The Plaid transaction reconciliation tests are already done (`docs/reviews/2026-10-05-008-plaid-sync-reconciliation-tests.md`). It does not wait for the linked-debt items. It pulls the concurrency part of Phase 6 item 1 forward. No schema change.
2. Follow a balance. A debt can follow an eligible connected account: active, not archived, a credit card or loan, same currency, and backing no other debt. Existing links stay reference links. Sync never writes a debt; a followed debt reads the latest snapshot. Each followed debt shows freshness, stale after two days in the household time zone. A removed bank link leaves the debt followed and stale until the person chooses. A negative balance on a followed revolving debt counts as $0 with a note. Stop following copies the last values onto the debt. Model change.
3. Overrides. Setting the person's own balance is a labeled override with "Use synced value". The form keeps synced fields read-only. "Update balance" works from a stale card. No schema change beyond item 2.
4. Suggested matches. Up to three eligible accounts with plain reasons. The person confirms. No schema change.
5. Credit limit. Store `balances.limit` from the existing account sync, follow it, and allow its override. Model change.
6. Reconnect. An update-mode link token and a reconnect action from a stale debt and the Connections page. This is the reconnect part of Phase 6 item 1, done early for stale linked debts. No schema change expected.

Following uses basic sync only: the balance and the credit limit. APR, minimum payment, due date, and statement balance stay entered by the person. Plaid Liabilities is deferred and not planned. The design keeps the analysis and Zach's rules for it under "Future: if Liabilities is ever adopted".

A model change in items 2 and 5 updates the model and `DbContext` configuration, then stops. Zach generates and applies the EF Core migration.

### Tracked UI follow-ups

These are not scheduled into a phase yet. Each is one review when it is picked up.

1. Detail panel keyboard focus. While a record panel or full-screen sheet is open, and while a nested picker is open inside it, Tab and Shift+Tab stay inside the top surface. Escape closes only that surface. When it closes, focus returns to the control that opened it. Today the panel moves focus into itself when it opens, but Tab can leave it and focus does not return to the opener. Found in `docs/reviews/2026-10-05-017-ui-review-notes.md`. Required by `.cursor/rules/ui-governance.mdc` section 3. No schema change.
2. Extra-each-month field layout on Plan. Done. The behavior is approved in `docs/reviews/2026-10-07-016-plan-extra-payment.md`. The October 8 Plan-page refinement groups it with the payment strategy under one temporary-scenario disclosure and is approved in `docs/reviews/2026-10-08-008-plan-page-refinement.md`. No schema change.

### Phase 3 — Recovery calculations, scenarios and saved plan

1. Build deterministic decimal-based services for dated cash-flow events, debt amortization, minimum payments, extra-payment allocation and savings targets.
2. Produce a 30-day cash view and 6/12/18-month forecasts of balances, minimum obligations, available cash, protected reserves and expected payoff/recovery milestones. Include conservative/typical inputs and disclose interest/payment assumptions.
3. Implement smart payoff prioritization. Use avalanche as the economic baseline, then quantify and explain when removing a minimum payment, lowering high utilization or honoring a user-selected constraint changes the recommended order. Always make the pure avalanche and user-selected alternatives available for comparison.
4. Roll a paid-off debt's freed minimum and planned extra payment into the next debt by default in projections, only after the modeled payment actually ends. Let users compare rollover against reclaiming some or all of that cash for savings or spending.
5. Track cash-flow recovery explicitly. Show when each payoff removes a monthly obligation and how much recurring breathing room it creates, rather than reporting only falling balances and a final debt-free date.
6. Support reproducible scenarios for changed extra payments, income loss, bonuses/windfalls, spending changes and protected-cash targets. Store variable-income and windfall allocation rules as reviewable preferences; never move money automatically.
7. Add core restructuring and refinance comparisons for consolidation loans, balance transfers and replacement loans. Compare APR, fees, term, monthly payment, total interest, break-even date, payoff date, resulting cash flow and the opportunity cost of using upfront cash; do not rank an option by payment alone or assume an offer is approved.
8. Compute spendable estimates from dated obligations, reserved funds and reliable income. Display horizon, data freshness and uncertainties; monthly surplus alone is insufficient. Guardrails must warn when an aggressive payment would leave ordinary expenses or near-term obligations underfunded.
9. Model credit-card purchases and card payments correctly: purchases affect category spending; payments affect cash and debt, without counting spending twice.
10. Save versioned plans with facts, assumptions, priorities, selected scenario, actions, milestones and review conditions. Separate draft, accepted and superseded versions.
11. Generate a plain-language decision plan: the three most important actions, their order, why they matter, expected cash-flow effect, assumptions, risks and missing information. Support a printable/shareable export.

**Exit:** identical inputs reproduce identical numbers; shortfalls remain visible; smart priority and rollover results reconcile to documented fixtures; refinance/restructuring comparisons include fees and break-even behavior; a saved plan survives edits with version history and can be used without chat.

**Status (October 16, 2026):** Items 1–5 are approved in `docs/reviews/2026-10-07-008-recovery-calculations.md`, `docs/reviews/2026-10-07-009-cash-forecast.md`, `docs/reviews/2026-10-07-010-payoff-priority.md`, `docs/reviews/2026-10-07-011-payoff-rollover.md`, and `docs/reviews/2026-10-07-012-cash-flow-recovery.md`. Plan is the fourth primary destination, after Home, Accounts, and Activity. It became primary in `docs/reviews/2026-10-07-013-plan-nav.md`; the October 8 correction and decision 0009 record its current position. The calculators, the forecast, the payoff comparison, the rollover comparison, and the cash-flow recovery report are pure domain rules in `api/Domain/Recovery`. The Plan screen work below puts them on a page. Those Plan screen items have no schema change. Item 6 has the temporary Plan previews for a shared extra payment, income loss, a one-time windfall, flexible spending, and cash to keep. The extra is approved in `docs/reviews/2026-10-07-016-plan-extra-payment.md`. Its field layout is included in the Plan page refinement, approved in `docs/reviews/2026-10-08-008-plan-page-refinement.md`. The cash previews are approved in `docs/reviews/2026-10-08-012-plan-cash-scenarios.md`. Stored variable-income and windfall allocation preferences are approved in `docs/reviews/2026-10-15-001-allocation-preferences.md`. They do not move money and do not change the temporary Plan previews. `20261015193812_AddAllocationPreferences` is applied. Item 7, one restructuring comparison at a time, is approved in `docs/reviews/2026-10-15-002-restructuring-comparisons.md`. It does not save an offer or treat it as approved. Partial balances, more than one offer, a changing rate, and a promotional expiration stay in Phase 7. Item 8, spendable estimates, is approved in `docs/reviews/2026-10-15-003-spendable-estimates.md`. Item 9, card purchases and payments, is approved in `docs/reviews/2026-10-15-004-card-activity.md`. On October 15, 2026, Zach chose to finish items 9–11 before a heavy QA and UX pass for this phase. Items 10 and 11 are approved in `docs/reviews/2026-10-15-005-saved-plan.md`. `20261016030137_AddPlanVersions` and `20261016030618_AddPlanVersionDraftIndex` are applied. Increment 1 of the UX pass, navigation and a cash-first Overview, is approved in `docs/reviews/2026-10-15-007-plan-navigation-and-safety.md`. Increment 2, one working scenario with Update preview, Reset, and saved-version clarity, is approved in `docs/reviews/2026-10-16-001-working-scenario-and-versions.md`. Increment 3, payoff-priority alternatives and partial reclaim, is approved in `docs/reviews/2026-10-16-002-payoff-priority-and-reclaim.md`. It stores the choice in the existing plan document and has no migration. It does not close this phase. The October 16 UX reassessment is approved in `docs/reviews/2026-10-16-003-phase-3-ux-reassessment.md`. Increment 1 of that reassessment, the next-month answer first, is approved in `docs/reviews/2026-10-16-004-plan-next-month-first.md`. Increment 2, one way to apply a change, is approved in `docs/reviews/2026-10-16-005-one-way-to-apply.md`. Increment 3 of that reassessment, the remaining Plan edges, is approved in `docs/reviews/2026-10-16-006-remaining-plan-edges.md`. Behavior QA is approved in `docs/reviews/2026-10-16-007-phase-3-behavior-qa.md`. No walked check failed. Zach closed this phase on October 16, 2026, in `docs/reviews/2026-10-16-008-phase-3-close.md`. The exit holds. The same inputs reproduce the same numbers, shortfalls stay visible, priority and rollover reconcile to the fixtures, an offer comparison includes fees and break-even, and a saved plan keeps its version history without chat. Phase 4 has not started. Detail panel keyboard focus stays tracked. Phase 3 reports stay in `docs/reviews/`. The Phase 2 UX closure is approved in `docs/reviews/2026-10-08-007-phase-2-ux-closure.md`.

### Plan screen — after Phase 3 item 5, before Phase 3 item 6

The approved calculations need a screen before scenarios are built on them. Item 6 scenarios and item 8 spendable estimates change what this page shows. Design: [`docs/design/plan-page.md`](design/plan-page.md), approved October 7, 2026. Forward-looking charts live on Plan and Home keeps history (decision 0008). Each item is one review.

**Status (October 8, 2026):** The first page, in `docs/reviews/2026-10-07-014-plan-recovery.md`, showed the report as two text cards. Zach's review asked for a clear hierarchy and charts, so item 1 reworked that same review. Item 1 is approved in that report. Item 2 is approved in `docs/reviews/2026-10-07-015-plan-cash-outlook.md`. Those item descriptions record the original approved one-page layout and calculation scope. The implemented Overview, Cash outlook, and Debt payoff refinement is the current layout and is approved in `docs/reviews/2026-10-08-008-plan-page-refinement.md`. The eight jewel series tokens are in `globals.css`. No schema change.

1. Page and debt charts. Approved in `docs/reviews/2026-10-07-014-plan-recovery.md`. The page shape is in `docs/design/plan-page.md`. No schema change.
2. Cash outlook. Approved in `docs/reviews/2026-10-07-015-plan-cash-outlook.md`. The page shape is in `docs/design/plan-page.md`. No schema change.

### Phase 4 — Guided conversation as the main planning entry point

Design the conversation and test scripted prototypes during Phases 1–3. Production chat depends on the ownership, fact and calculation contracts above; it should not become a disconnected demo.

1. Add server-side OpenAI integration behind an application interface. Select the model using representative quality/cost evaluations before committing to pricing.
2. Persist conversations, compact summaries, unanswered questions, user preferences and linked plan versions. Resume over multiple days.
3. Give the assistant narrow tools to read scoped summaries, propose facts, request calculations, compare supported scenarios and draft plan changes. Reuse domain services used by direct editing.
4. Ask one or two useful questions at a time. Prioritize urgent cash needs, allow uncertainty, and explain why a detail matters.
5. Distinguish user statements, imported facts, estimates and unverified suggestions. Do not silently convert an inferred recurring expense into a confirmed obligation.
6. Display proposed changes for review before accepting a plan revision. Handle edits made outside chat and stale model context with version checks.
7. Let calculation services supply every numeric forecast. Preserve assumptions and explain tradeoffs without guarantees, invented terms or shame.
8. Define data-sharing consent and minimization. Never expose bank access tokens to the model; treat transaction descriptions and uploaded text as untrusted data, not instructions.
9. Add timeout/retry/cost limits, safe logging, and a fallback that preserves the draft and lets the user continue manually.

**Exit:** a person can start with “I don't know where to begin,” return on another day, correct a fact, review a calculated plan and accept it into their budget. Tests establish that tools cannot cross households or silently apply consequential changes.

**Status (October 17, 2026):** Item 1 is approved in `docs/reviews/2026-10-16-009-planning-model.md`. The API can call OpenAI through `IPlanningModel` when `PlanningModel:ApiKey` and `PlanningModel:Model` are both set. No model or price is chosen. The call sets `store` to false, sends no tools, and does not log the API key. Guided conversation stays off until both settings are present. Item 2 is approved in `docs/reviews/2026-10-16-010-persisted-conversations.md`. A household can keep one open conversation, with its turns, a compact summary, unanswered questions, notes, and links to saved plan versions. `20261016190413_AddConversations` is applied. There is no chat screen and no live OpenAI call. Item 3 is approved in `docs/reviews/2026-10-16-011-narrow-tools.md`. The tools read household summaries, propose facts, request calculations, compare supported scenarios, and draft a plan change through the services direct editing uses. They do not save those proposals. Item 4 is approved in `docs/reviews/2026-10-16-012-useful-questions.md`. The next questions are one or two at a time, dated cash first, and a detail can be left unknown. Item 5 is approved in `docs/reviews/2026-10-16-013-fact-basis.md`. Planning inputs are labeled as user statements, imported facts, estimates, or unverified suggestions, and an inferred recurring expense stays a suggestion until the person saves the bill. Item 6 is approved in `docs/reviews/2026-10-16-014-plan-revision-review.md`. A proposed revision is shown before it is accepted, and an outside edit or a stale conversation rejects that accept. Item 7 is approved in `docs/reviews/2026-10-16-015-calculation-forecasts.md`. Every amount and date in that forecast is copied from the plan calculation. Item 8 is approved in `docs/reviews/2026-10-16-016-data-sharing.md`. A conversation records an explicit yes or no before household data can be prepared for the model, and a bank access token is left out. Item 9 is approved in `docs/reviews/2026-10-16-017-planning-call-limits.md`. A call stops after 30 seconds, retries a transient failure at most twice, and will not send more than 24,000 characters or ask for more than 1,024 output tokens. No price is set. A failure leaves the open draft and the conversation unchanged. Items 1–9 are approved backend and domain components. They are not a completed guided-planning experience, and this phase exit is not met. There is no chat screen, no authenticated conversation route, and no live OpenAI call. The model request sends text and instructions and no tools. A planning call can return a reply without persisting it or assembling consent-filtered context. A fact proposal is validated and not saved, and it does not yet say whether it creates a row or updates one. Plan review and accept have no route; accept does not link the conversation. On October 17, 2026, the core-journey sequence was written to finish this exit before more Phase 5 features. Detail panel keyboard focus stays tracked and is part of that sequence's repair gate. Phase 5 item 1 remains approved.

### Core journey — finish the Phase 4 exit before more Phase 5

The journey is: start with incomplete information, confirm facts, review a calculated plan, accept it, return, and correct something. Manual planning already covers most of that path. Chat has to use the same facts, editors, and calculations. Phase 5 item 1 stays as approved. On October 17, 2026, the working tree had no uncommitted Phase 5 implementation beyond that approved item.

Zach approved the sequence and the acceptance contract on October 17, 2026, in [`docs/reviews/2026-10-17-004-core-journey-sequence.md`](reviews/2026-10-17-004-core-journey-sequence.md). Later the same day, in [`docs/reviews/2026-10-17-008-planning-architecture.md`](reviews/2026-10-17-008-planning-architecture.md), he directed that desktop guided recovery and MX connectivity come before phone access. Item 1 is approved. The next increment is item 3. Items 4, 6, 7, and 8 then build the guided path on the desktop. Item 6 no longer waits for the phone. Item 12, MX connectivity, comes after that desktop path and before phone access. Its scope is not written, and it does not start inside item 3. Items 2 and 5 stay written and are not next. Leave `PlanningModel:ApiKey` and `PlanningModel:Model` blank until item 9's review authorizes a live call. Bank records stay independent of the connector, as in decision 0002. Plaid remains the connector already in the app. MX is not a claim that Plaid is removed.

Findings 1 and 2 of [`docs/reviews/2026-10-17-005-codebase-organization-review.md`](reviews/2026-10-17-005-codebase-organization-review.md) are part of this sequence. Finding 1, household scoping of the browser working plan, is item 3. Finding 2, the reviewed snapshot, is item 8. The worker-isolation finding, the history-loading finding, and the organization notes in that review stay there. Incorporating the findings does not mean those checks were run.

Early chat uses the scripted planning model, an `IPlanningModel` selected with `PlanningModel:Provider` set to `Scripted`. It returns a predetermined reply and makes no outbound call. The API accepts `Scripted` only when the host environment is Development, or in the test host. Production startup fails if `Scripted` is set. Every scripted reply is marked **Scripted reply** on the message itself, and the stored turn keeps that mark after reload. The existing OpenAI client stays unselected. A blank provider does not call OpenAI. Items 4 and 6–8 use Scripted only. The phone walk in item 5 uses it too, when that item is reached.

Each feature increment includes the focused authenticated-request and PostgreSQL tests that protect it, in the existing test project. Item 4 adds those tests to the existing GitHub Actions workflow with a `postgres:16` service, the same image as local Compose. Later increments add their tests to that job. The connection string stays in CI and is not committed. Item 11 keeps observed repairs and any remaining coverage.

1. **Journey and acceptance contract.** Approved October 17, 2026.
   - **Outcome:** A person can tell the working preview, the saved draft, the accepted plan, and current financial facts apart, including when the accepted scenario differs from the saved budget settings.
   - **Bounds:** Current facts are the saved income, bills, debts, living amount, cash to keep, and allocation. The existing editors save them. A chat acknowledgment does not. The working preview is the on-screen calculation from those facts plus unsaved Try changes. It is not a saved version, and Overview follows it. The saved draft is one snapshot; later fact edits do not change it. Accept replaces the household's accepted plan and keeps the previous version in history. It stores the scenario used for that calculation: extra payment, income loss, windfall, flexible spending, cash to keep, payoff priority, and freed-cash choice. It does not copy those values into Living, Savings, or Allocation, schedule a payment, or move money. When an accepted flexible-spending or cash-to-keep amount differs from the saved setting, Your plan names both amounts and links to that existing editor. The current amounts beside the snapshot are live readings of the saved settings. Showing them does not write the snapshot, refresh its stored scenario, or revalidate its calculation. Saving in the editor updates the live setting and leaves the accepted snapshot unchanged. Income loss, a windfall, an extra payment, payoff priority, and freed cash stay inside the accepted plan, with no apply-to-budget action. A save or accept persists the reviewed calculation only when the facts and scenario still match that review. A later change rejects it until the person reviews the new figures. The browser does not supply the plan document as the source of truth. The Phase 4 words "accept it into their budget" mean accepting that calculated version. Chat is a view on Plan, beside Overview, Try changes, and Your plan. It is not a fifth destination. Confirming a fact opens the existing editor. Declining model consent leaves the manual path. This contract does not add a saved "no bills" value. Item 10 tests the unset bills row.
   - **Depends on:** Phase 5 item 1, unchanged.
   - **Verification:** Zach approved this contract on October 17, 2026. No test, screen change, or live call was part of that approval.
   - **Exit:** Approved. The next increment is item 3. Items 2 and 5 wait behind the desktop guided path and item 12.

2. **Device-access proposal.** Checkpoint only. Not next. It waits until item 12, and until Zach asks for the phone. The proposal in [`docs/reviews/2026-10-17-006-phone-access-proposal.md`](reviews/2026-10-17-006-phone-access-proposal.md) still awaits review. Nothing is installed.
   - **Outcome:** Zach can approve or reject a concrete way to open the app on his phone before any access is turned on.
   - **Bounds:** Name his phone and browser, the provider, the destination, the access limits, what data the setup can expose, and any cost. Prefer the smallest setup that works with that device and Clerk. State that an HTTPS page cannot assume it can call a localhost HTTP API. Normal local startup stays on loopback. No bind change, CORS change, content-security change, authorized-origin change, credential, or speculative command. No native app or PWA.
   - **Depends on:** Item 1, approved. Item 12 comes before this item. Item 5 waits for this approval.
   - **Verification:** Zach reviews the named setup. Nothing is started.
   - **Exit:** He approves that setup in [`docs/reviews/2026-10-17-006-phone-access-proposal.md`](reviews/2026-10-17-006-phone-access-proposal.md), or asks for a different one. Item 5 waits for the approval.

3. **Household-scoped working plan.**
   - **Outcome:** Another person signing in on the same browser starts from their own facts and does not inherit the previous person's working scenario.
   - **Bounds:** Scope the stored working-plan state and its in-memory cache to the signed-in household, clear them on identity change, and do not restore them before the household is known. No chat view, route, planning-model provider, or network change.
   - **Depends on:** Item 1. It does not wait for the phone proposal.
   - **Verification:** A browser check signs in as one test person, applies a scenario, signs in as another in the same tab, and confirms the second person does not inherit it. The same person can leave and return without losing their own scenario.
   - **Exit:** The working scenario follows the signed-in household. Chat has not been added.

4. **Minimal chat and the first saved turn.**
   - **Outcome:** A signed-in person can open chat on Plan, send one message, and see that message and its reply still there after a reload. The reply is visibly a scripted reply.
   - **Bounds:** One Plan view. It shows the household's open conversation, a composer, and the latest reply. Chat state follows the household rule from item 3. The same increment adds the household-scoped routes and composes the existing conversation, consent, and planning-call services. The reply comes from the scripted planning model. `Scripted` is accepted only in Development and in the test host. Production startup fails if it is set. Each scripted reply shows **Scripted reply** on that message, and the stored turn keeps the mark. Household context is prepared only after an explicit yes. A no leaves manual Plan usable and prepares nothing. A bank access token stays out. One send stores one user turn and at most one reply. A failure leaves the draft and the accepted plan unchanged. Tools, fact confirmation, and plan acceptance wait for items 6–8. Reuse the existing shell and buttons. Show loading, empty, and error. No new test package and no second CI product. This increment does not turn phone access on.
   - **Depends on:** Item 3.
   - **Verification:** Authenticated HTTP tests for the signed-in household, another household, and a missing session. PostgreSQL tests for one saved turn, one reply, a duplicate send, declined consent, a failed call, and a stored scripted mark. A production-environment test rejects `Scripted`. Those tests run in the new CI job. Zach can send and reload in a development browser and see **Scripted reply** on the message.
   - **Exit:** The conversation is on Plan. A reload shows the stored turn and its scripted mark. Another household cannot read it. A failed or declined send does not add a second reply or change the draft. CI runs the PostgreSQL tests. Production cannot select the scripted provider.

5. **Device access and physical verification of the minimal chat.** Not next. It waits with item 2, behind item 12.
   - **Outcome:** Zach opens the documented URL on his phone, signs in, finishes the existing manual plan journey, and reloads a scripted chat turn from item 4.
   - **Bounds:** Implement only the setup approved in item 2. Keep the database private. Use a distinct test identity and synthetic household. The chat walk uses the scripted planning model in Development, and the phone shows **Scripted reply** on each scripted message. After the path works, document the real startup, shutdown, phone URL, and sign-in steps in the root README. Repair only blockers in that manual journey and the minimal chat view. Desktop narrow-width checks support development. No native app, PWA, install prompt, push notification, or offline write.
   - **Depends on:** Items 2 and 4.
   - **Verification:** Zach runs the manual rows from [`docs/reviews/2026-10-17-003-mobile-testing-plan.md`](reviews/2026-10-17-003-mobile-testing-plan.md) and the minimal chat rows: sign-in and reload, empty-household entry, sheets and pickers, Plan, save or accept and reopen, composer with the software keyboard, background and resume, rotation, larger text, and VoiceOver or TalkBack. Sign out and in as a second test person on the phone and confirm the working scenario does not carry over. The agent checks that the documented commands start and stop access.
   - **Exit:** He can finish the manual journey and reload a stored scripted chat turn on the phone without a developer. The README says how to open and close that access.

6. **Bounded tool execution, on that view.**
   - **Outcome:** A reply on the same chat view can use the existing narrow tools. A tool cannot save a fact or a plan by itself.
   - **Bounds:** The model call may request the existing `IPlanningTools` methods, under the existing timeout, retry, and size caps. Results come from the services direct editing uses. The reply still comes from the scripted planning model and keeps the **Scripted reply** mark. A proposal is shown and not saved. A foreign id fails. Malformed arguments fail that call. Untrusted text is data. Create versus update waits for item 7.
   - **Depends on:** Item 4. It does not wait for the phone.
   - **Verification:** Authenticated HTTP and PostgreSQL tests, in the CI job from item 4, that a tool runs only in the signed-in household, a foreign id fails, a proposal is not saved, a bank token is refused, and malformed arguments leave the household unchanged.
   - **Exit:** The chat view shows the tool result. The household's facts and plan are unchanged.

7. **Confirmed fact creation and correction, on that view.**
   - **Outcome:** The person confirms a proposal once from chat, and the saved income, bill, debt, or setting matches a save from the existing editor.
   - **Bounds:** The proposal names a new fact or an existing one before it can be saved. The save uses that fact's existing service. One confirmation writes one row. Chat text alone is not a saved fact. A recurring suggestion does not become a bill. This item does not add a saved "no bills" value. The scripted planning model stays selected, and only in Development or the test host.
   - **Depends on:** Items 1 and 6.
   - **Verification:** Authenticated HTTP and PostgreSQL tests, in the CI job from item 4, that confirmation and the direct service write the same row, a second confirmation does not insert another, and an outside edit is visible on the next read.
   - **Exit:** A confirmed proposal is one saved fact. An unconfirmed proposal is not.

8. **Calculated plan review and acceptance, on that view.**
   - **Outcome:** From the same chat view, the person reviews a calculated plan, accepts it under the item 1 contract, and can see when that scenario differs from the saved budget settings.
   - **Bounds:** Manual save and chat accept share one reviewed-snapshot rule. The server recalculates from the household's facts and accepts only when those facts and the scenario still match the review. The browser does not supply the plan document. Review, draft replacement, and accept commit in one transaction, so a failure does not leave a replaced draft that was not accepted. A later fact change rejects the save until the person reviews the new figures. A repeated accept does not write another version. The conversation links to the saved version through the existing link. The accepted view names a differing flexible-spending or cash-to-keep amount beside the live saved setting. Those live amounts do not update or revalidate the snapshot. Saving in the editor changes the live setting only. Accept does not change Living, Savings, or Allocation, and it does not move money. Overview keeps following current facts. The scripted planning model stays selected, and only in Development or the test host.
   - **Depends on:** Items 1 and 7.
   - **Verification:** Authenticated HTTP and PostgreSQL tests, in the CI job from item 4, for a fact change between preview and save, a change between review and commit, a failure between draft replacement and accept, a duplicate accept, and the same facts producing the same calculated plan through chat and direct editing. One case accepts a scenario whose flexible spending or cash to keep differs from the saved setting: the accepted plan names both, the live amounts stay labeled as current settings, and the snapshot's stored figures stay as reviewed.
   - **Exit:** Accept writes one plan version that matches the reviewed figures. A differing live budget amount is visible and does not rewrite or revalidate the snapshot. A stale or rejected proposal writes nothing. A failed accept does not leave a replaced draft. The previous accepted version stays in history.

9. **Bounded live evaluation.**
   - **Outcome:** One synthetic household completes a guided pass on a real model, with the cost, latency, and calculation mismatches recorded.
   - **Bounds:** Zach names the model and a spending cap in that review, before any live call. That run selects the existing OpenAI client and leaves the scripted planning model unused. Synthetic data only. Fill the planning-model settings only for that run. Judge whether the reply invents a surplus, a date, or a debt term, asks one or two useful questions, and leaves facts unsaved until confirmation. A failure leaves the draft. Reaching the cap stops the run. This item does not choose a product price.
   - **Depends on:** Item 8 and that explicit cost approval.
   - **Verification:** Record journeys attempted, completions, number mismatches, latency, and spend. No real financial data.
   - **Exit:** The cap held. Mismatches against the calculator are listed. Phase 4 stays open until Zach says the exit is met.

10. **Initial alpha journeys.**
    - **Outcome:** The situations below have a recorded result, including the ones whose numbers do not balance.
    - **Bounds:** The initial set is an empty or incomplete household, negative cash flow, unknown debt terms, accepting and reopening a plan, correcting a fact outside chat, a stale proposal, declined consent, and an interrupted request. The incomplete household explicitly includes no recurring non-debt bills. The screen shows bills as unset, not as a saved "none" and not as $0, and a provisional next step is still available. The walk records whether that unset row blocks the person. The desktop walk does not wait for the phone. Repeating the guided path on the phone, including a dropped connection, waits for item 5. The rest of Phase 5 item 7 stays after item 11 and before the full alpha exit: steady income, variable income, household contributions, an irregular expense, a windfall, a changed extra payment, a refinance offer, and mid-plan income loss.
    - **Depends on:** Item 9 for the live cases. Declined consent and an interrupted manual retry also pass with the model unset.
    - **Verification:** Zach performs each case, including the no-bills household, and records hesitations. The agent does not mark a case passed.
    - **Exit:** Each initial case has a result. The no-bills result says whether the unset row blocked the journey. A blocker is fixed in item 11 before more Phase 5 features. The remaining alpha cases are not claimed.

11. **Observed repairs and remaining coverage.**
    - **Outcome:** Friction Zach recorded on the core path is repaired, and the checks the feature increments did not cover are filled in.
    - **Bounds:** Repair the recorded core-path issues, including focus return from the panels that path opens, and a saved "no bills" value only if item 10 shows the unset row blocks the journey. Add remaining PostgreSQL and authenticated-request coverage to the CI job from item 4 where items 4–8 did not already cover it. Do not rerun every old test or automate every screen. Do not add an analytics platform, a scenario engine, a visual rebrand, or a second CI product. Zach remains the manual UX and end-to-end tester.
    - **Depends on:** Item 10.
    - **Verification:** The added tests run in the PostgreSQL CI job. Zach's core-path notes are addressed or still listed.
    - **Exit:** The remaining listed cases pass in the CI job, or the gap is still named. Phase 5 items 2–5 stay paused until he says this gate is met.

12. **MX connectivity.** After the desktop guided path, and before phone access.
    - **Outcome:** A later review names how an MX connection writes balances and transactions into the existing account and transaction records.
    - **Bounds:** The scope is not written. Starting this item writes that proposal and stops for approval. It does not add an MX package, credential, migration, or sync. Plaid stays the connector already in the app. Decision 0002 stays: a record does not require a bank connector. This item does not turn phone access on.
    - **Depends on:** Item 8. It does not wait for items 9–11. Items 2 and 5 wait for it.
    - **Verification:** Zach reviews the proposal. Nothing is connected.
    - **Exit:** He approves that proposal, or asks for a different one. No MX call has been made.

### Phase 5 — Follow-through and private-alpha validation

Items 2–5 stay listed so the backlog is not lost. They are not next. Core journey items 1–12 come first. Phone access stays behind item 12.

1. Add a Plan home view with next actions, near-term cash outlook, milestones and assumptions needing confirmation. The debt charts and the cash outlook are planned earlier, in "Plan screen" after Phase 3 item 5. This item adds next actions, milestones, and assumptions needing confirmation to that page.
2. Give actions states such as proposed, chosen, waiting, completed and canceled. Support dependencies: unreceived loan funds cannot finance today's payments.
3. Compare actual paychecks/balances with expected values; propose recalibration after material changes.
4. Track recovery indicators such as utilization thresholds, removed minimum payments, reserve targets, positive cash flow and refinance-readiness inputs. Celebrate intermediate progress without implying that one threshold guarantees a credit or lending outcome.
5. Track user-selected behavioral supports, contribution restart conditions, review dates and quality-of-life goals. Detect when accelerated payoff repeatedly creates a cash shortage and recommend revisiting the buffer or budget assumptions.
6. Keep budget/account/transaction pages directly accessible. Place institution/category configuration in secondary navigation.
7. Conduct complete alpha journeys: steady income, variable income, household contributions, missing debt terms, negative cash flow, irregular expense, windfall allocation, changed extra payment, refinance offer, and mid-plan income loss.

**Exit:** users can complete the first-plan journey and first review without developer intervention, understand why the plan changed, and see useful next steps even when the numbers do not balance.

**Status (October 17, 2026):** Item 1 is approved in [`docs/reviews/2026-10-17-001-plan-follow-through.md`](reviews/2026-10-17-001-plan-follow-through.md). Overview keeps the next-30-day figure and its one action. Under that action, the working preview shows the remaining ranked actions, the milestones the calculation produced, and unset bills, flexible spending, and cash to keep. The debt charts and the cash outlook are unchanged. No schema change. Item 6 is already the shell in decisions 0009 and 0010: Home, Accounts, Activity, and Plan stay primary, and institution and category setup stay in Settings. This sequence adds no destination. Items 2–5 are paused until core journey item 11 is met. The initial part of item 7 is core journey item 10. The remaining item 7 cases stay after that gate. No uncommitted Phase 5 implementation was in progress when this sequence was written. There is no chat screen and no live OpenAI call. Detail panel keyboard focus stays tracked.

### Phase 6 — Public-beta readiness and affordability

1. Complete bank repair/disconnect/removal and retention behavior. Verify scheduled worker deployment, concurrency protection, retry behavior and visible stale-data states. Overlapping-sync protection and reconnect for linked debts are planned earlier, in "Sync correctness and linked debts".
2. Verify API and worker use durable compatible Data Protection keys; test token decryption after restart/redeployment and recovery. Plaintext tokens left from before October 4, 2026 are rewrapped at API and worker startup (`docs/reviews/archive/phase-1/2026-10-04-007-security-hardening.md`).
3. Add export/deletion flows, retention policies, authorization integration tests, rate limits, redacted logs, backup/restore drills and migration deployment procedures.
4. Test mobile layouts, keyboard/screen-reader flows, slow networks, empty states and interrupted conversations. The core journey's phone checks wait with items 2 and 5, behind item 12. Keyboard, empty-state, and interruption checks for the desktop guided path are in items 10 and 11. This item keeps broader device coverage, the CSV import sheet's phone and desktop fit, and the remaining slow-network and recovery checks. Detail panel keyboard focus on the core path is in core journey item 11; the tracked follow-up remains until that gate closes it.
5. Measure AI usage per completed plan and follow-up session; measure bank-link, infrastructure, support and payment-processing costs. Price from actual bills and observed usage.
6. Target accessible core planning/manual budgeting plus an included conversation allowance; consider paid automation and additional usage. Do not lock a saved plan when usage runs out. Avoid promises of unlimited chat or a specific price before validation.
7. Add billing only when launching a paid offering, with clear limits, cancellation and downgrade behavior. Explore sponsored access after the core experience is validated.

**Exit:** documented operational checks pass, cost limits work without losing user data, and a small external cohort can safely use and leave the service.

### Phase 7 — Expanded decision support

1. Expand core refinancing/consolidation comparisons to partial coverage, multiple simultaneous offers, variable rates, promotional expirations and richer sensitivity analysis.
2. Compare minimum versus accelerated payments and flexibility without implying an application is approved or money has arrived.
3. Add retirement contribution/match and restart scenarios, explicitly separating user inputs, applicable plan rules, tax effects and uncertain investment-return assumptions.
4. Add retirement-plan loan and employment-change scenarios only after defining and verifying relevant plan-specific terms, repayment behavior, tax consequences and date/jurisdiction-sensitive rules. Do not encode an example conclusion as universal advice.
5. Expand opportunity-cost analysis with ranges and clear distinctions between debt cost and uncertain future returns.
6. Consider partner invitations and permissions, richer forecasting, in-app reminders, webhooks, investment detail, receipt attachments and native/PWA work based on actual demand.

**Exit:** each comparison has traceable inputs, tested numerical outputs, visible missing terms, understandable risks and a useful second-opinion summary.

## 6. Full capability traceability

| Supplied capability                | First useful version                                                                                                                                           | Expansion                                                              |
| ---------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| Income normalization               | Phase 2: cadence, dated net pay, scenarios                                                                                                                     | Validated gross-to-net estimates                                       |
| Household budgeting                | Phase 2: contributors and unequal contributions                                                                                                                | Shared access/permissions                                              |
| Debt inventory                     | Phase 2: balances, APR, minimums, utilization, types and transparent health indicators; optional linked debts that follow a connected balance and credit limit | Richer statement/term capture; Plaid Liabilities deferred, not planned |
| Debt prioritization                | Phase 3: avalanche baseline plus explained minimum-release, utilization and user-constraint tradeoffs                                                          | Complex promotions and constraints                                     |
| Automatic payment rollover         | Phase 3: roll freed minimum/extra payments into the next debt with alternatives                                                                                | Adaptive rollover proposals based on actuals                           |
| Debt restructuring                 | Phase 3: core consolidation, balance-transfer and replacement-loan comparisons                                                                                 | Phase 7 multiple offers, partial coverage and richer sensitivities     |
| Refinance analysis                 | Phase 3: APR, fees, term, interest, break-even and cash-flow comparison                                                                                        | Variable rates and offer monitoring                                    |
| Opportunity cost                   | Phase 3: upfront cash, reserve and debt-cost tradeoffs                                                                                                         | Phase 7 retirement/investment ranges and sensitivities                 |
| Cash-flow recovery                 | Phase 3: dated baseline, obligation removal and recurring breathing-room changes                                                                               | Actual-versus-plan recovery history                                    |
| Emergency buffers                  | Phases 2–3: operating reserve and emergency goal                                                                                                               | Adaptive goals with user review                                        |
| Sinking funds                      | Phase 2: target/date/reserved amounts                                                                                                                          | Recurring target automation                                            |
| Behavioral guardrails              | Phases 2–3: protected cash, realistic spending, and warnings. Phase 5 action states wait for the core journey                                                  | Personalized opt-in check-ins                                          |
| Life-with-debt budgeting           | Phase 2: sustainable discretionary and quality-of-life allocations                                                                                             | Scenario-based preference tuning                                       |
| Retirement contributions           | Phase 2 records actual payroll effect. Phase 5 review conditions wait for the core journey                                                                     | Phase 7 strategy/match comparisons                                     |
| Employment risk                    | Phase 3 supports income-loss scenarios                                                                                                                         | Phase 7 plan-loan-specific consequences                                |
| Scenario planning                  | Phase 3: extra payments, income changes, windfalls, protected cash and core restructuring over 6/12/18 months                                                  | Phase 7 richer comparisons                                             |
| Milestones and recovery indicators | Phases 3 and 5 item 1: calculated milestones on Plan. Richer indicators wait with paused Phase 5 items 4 and 5                                                  | Richer celebrations/progress history                                   |
| Variable-income rules              | Phases 2–3: lean base and extra allocation                                                                                                                     | Automated proposals from actuals                                       |
| Windfall rules                     | Phase 3: saved allocation preferences                                                                                                                          | Phase 5 review when funds arrive, after the core journey               |
| Quality of life                    | Phase 2 budget and goal priorities                                                                                                                             | Phase 5 new goals after the core journey and stabilization             |
| Decision support and action plans  | Phases 3–4: ordered actions, reasons, cash-flow effects and supported calculations                                                                             | Phase 7 specialized decisions                                          |
| Second-opinion summary             | Phases 3–4 printable/exportable plan                                                                                                                           | Source-backed specialized comparisons                                  |
| Action generation                  | Phases 3–4 ordered actions. Phase 5 dependencies and completion wait for the core journey                                                                      | Optional reminders                                                     |
| Ongoing recalibration              | Core journey: correct a fact and reject a stale proposal. Phase 5 automatic actuals review waits                                                               | Event-driven proposals                                                 |

## 7. Technical shape and contracts

Keep the current Next.js frontend, .NET API, PostgreSQL and worker. There is no demonstrated need for a rewrite or microservices.

Proposed domain areas: Identity/Households; Accounts/Transactions; Income/Obligations; Budgets/Goals; Debts; Forecasts; Plans/Actions; Conversations. Add services incrementally following the existing architecture.

Candidate records: Household, Membership, IncomeSource, ScheduledObligation, DebtTerms, BudgetPeriod, BudgetLine, SavingsGoal, Plan, PlanVersion, Scenario, ActionItem, Milestone, Conversation, Message and ProposedChange. These are planning concepts, not a mandate for one table per concept.

Financial facts need household ownership, currency, effective/as-of date, source and confirmation state. Forecasts need a fixed input snapshot, calculation version and explicit assumptions. Plan acceptance should be transactional and protect against stale versions or repeated requests.

## 8. Tests that establish trust

- Ownership: foreign IDs, aggregates, exports, jobs, chat tools and guessed object IDs cannot cross households.
- Import/sync: repeated batches, pending-to-posted transitions, removed transactions, edited categories, unknown accounts, interrupted pagination and overlapping worker/manual sync.
- Money: transfer/card-payment double counting, refunds, opening balances, absent balances, decimal rounding, final loan payment and changing minimums.
- Time: biweekly three-paycheck months, monthly dates near month-end, leap years, time zones, bill-before-payday shortfalls and forecast start dates.
- Recovery: no surplus, missing APR, inconsistent obligations, extra income, reserved cash, partial payoff, paid-off debt, automatic rollover, utilization-driven priority, removed minimums, revised paycheck estimates and sustainable discretionary spending.
- Offers: refinance fees, break-even dates, longer terms with lower payments, balance-transfer expirations, partial consolidation and offers that worsen total cost.
- Chat: user corrections, invented facts, unsupported scenarios, prompt injection in transaction text, stale plan changes, provider failure, cross-session continuity and cost caps.
- Operations: backup restoration, migration recovery, token-key continuity, unlinking, deletion, and access after subscription downgrade.

Use focused unit fixtures for calculations, database integration tests for isolation and reconciliation, and a small number of complete browser journeys. Avoid substituting UI snapshots for financial correctness.

## 9. Success measures and decisions to revisit

Measure completed first plans, time/effort to reach them, unresolved inputs, successful return sessions, completed first reviews, user understanding of next actions, protected-buffer adherence, recurring cash flow freed, minimum payments removed, high utilization reduced, plan relapse/re-borrowing signals, calculation errors and cost per active household. Do not optimize for message count or the fastest theoretical debt-free date alone.

Adopted defaults: single planning currency, one household owner with multiple contributors, and manual input available. Proposed defaults for Phase 3 and later: conservative income baseline, protected operating cash, avalanche as the comparison baseline, payment rollover enabled in projections, user-reviewed plan changes, no automated financial transactions, and direct editing alongside chat.

Authentication is chosen: Clerk Hobby (`docs/reviews/archive/phase-1/2026-10-03-007-phase-1-sign-in-decision.md`). Before release, choose the hosting configuration, validate model quality and actual pricing, confirm the supported launch jurisdiction/currency, and establish boundaries for specialized financial topics. The current roadmap does not require those choices to block the baseline cleanup.

OpenAI integration references used in this planning conversation: [function calling](https://developers.openai.com/api/docs/guides/function-calling) and [model optimization](https://developers.openai.com/api/docs/guides/model-optimization). Recheck current API/model documentation during implementation. This roadmap specifies product behavior, not a frozen SDK contract.
