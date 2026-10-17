# Cardui documentation

Status: Current
Updated: 2026-10-17

## Status

This block is the single source for current work. Update it in the same
change as each review report. Reviews and phase status paragraphs record
history. They do not say what is next.

**Now**

The [planning architecture](reviews/2026-10-17-008-planning-architecture.md)
map is approved. Desktop guided recovery and MX connectivity come before
phone access. The next increment is core journey item 3: scope the working
plan to the signed-in household. Items 4, 6, 7, and 8 then build the guided
path on the desktop. Item 12 proposes MX connectivity after that desktop
path and before the phone. Its scope is not written. Phone items 2 and 5
wait. The
[conversational planning readiness](reviews/2026-10-17-007-conversational-planning-readiness.md)
report stays as written. TOR-3 and TOR-4 have not started. Linear was not
changed.

The [core journey sequence](reviews/2026-10-17-004-core-journey-sequence.md)
and its acceptance contract are approved. Scripted replies are marked
**Scripted reply** on the message. `Scripted` is limited to Development and
the test host. Phase 4 items 1–9 stay approved as backend components, and
the phase exit is not met. Phase 5 item 1 stays approved: Overview still
answers the next 30 days first, and under that action the working preview
lists the remaining ranked actions, the milestones the calculation
produced, and the unset bills, flexible spending, and cash to keep.
Calculation rules stay on Your plan. Planning calls register with the
planning model, so the sync worker starts while those settings stay blank.
There is no chat screen and no live OpenAI call. Detail panel keyboard
focus stays tracked. The
[October 15 UX assessment](reviews/2026-10-15-006-phase-3-ux-review.md)
stays as written.

**Next**

Start core journey item 3. Scope the existing browser working plan to the
signed-in household. Do not add chat, a planning-model provider, an MX
connection, or phone access in that increment. Leave the planning-model
API key and model blank until core journey item 9 asks for a live call.
Ask before generating or applying a migration.

Phase 5 items 2–5 stay paused: action states and dependencies, automatic
recalibration, richer indicators, and behavioral supports.

**Tracked, not scheduled**

- Detail panel keyboard focus. See "Tracked UI follow-ups" in
  [`roadmap.md`](roadmap.md). Core journey item 11 includes it for the
  panels on the primary path.

**Open decisions**

- The phone-access proposal remains unapproved and is not next. Nothing is
  installed. It waits behind MX connectivity.
- Whether the approved architecture report covers the work Linear still
  titles TOR-2. Linear was not changed.
- Whether a saved allocation should remain outside the recovery calculation.
- Which OpenAI model to use, and the spending cap for item 9. The API key
  and model stay blank. Early chat uses the scripted planning model in
  Development and the test host only.
- Hosting and production configuration. Until a public host exists,
  `Plaid:WebhookUrl` stays empty, so Plaid cannot report a revoked bank
  connection.
- The remaining codebase-review findings stay unscheduled: worker failure
  isolation, history loading, and the organization notes.

## Map

- [Codebase organization review](reviews/2026-10-17-005-codebase-organization-review.md):
  awaiting review, October 17, 2026. Findings 1 and 2 are incorporated into
  the core journey sequence. Their checks were not run. The other findings
  stay unscheduled.
- [Planning architecture](reviews/2026-10-17-008-planning-architecture.md):
  approved October 17, 2026. The recovery calculation, saved plan, and Plan
  page are mapped. Desktop guided recovery and MX connectivity come before
  phone access.
- [Conversational planning readiness](reviews/2026-10-17-007-conversational-planning-readiness.md):
  TOR-5, awaiting review, October 17, 2026. Read, preview, and save are
  mapped. No route or screen calls them.
- [Phone-access proposal](reviews/2026-10-17-006-phone-access-proposal.md):
  core journey item 2, awaiting review, October 17, 2026. Not next. It waits
  behind the desktop guided path and MX connectivity. Access is not turned on.
- [Core journey sequence](reviews/2026-10-17-004-core-journey-sequence.md):
  approved October 17, 2026, including the acceptance contract. Scripted
  replies stay labeled and development-only. Item 3 is split.
- [Plan follow-through](reviews/2026-10-17-001-plan-follow-through.md):
  Phase 5 item 1, approved October 17, 2026.
- [Planning call limits](reviews/2026-10-16-017-planning-call-limits.md):
  Phase 4 item 9, approved October 16, 2026.
- [Data sharing](reviews/2026-10-16-016-data-sharing.md): Phase 4 item 8,
  approved October 16, 2026.
- [Calculation forecasts](reviews/2026-10-16-015-calculation-forecasts.md):
  Phase 4 item 7, approved October 16, 2026.
- [Plan revision review](reviews/2026-10-16-014-plan-revision-review.md):
  Phase 4 item 6, approved October 16, 2026.
- [Fact basis](reviews/2026-10-16-013-fact-basis.md): Phase 4 item 5,
  approved October 16, 2026.
- [Useful questions](reviews/2026-10-16-012-useful-questions.md): Phase 4
  item 4, approved October 16, 2026.
- [Narrow tools](reviews/2026-10-16-011-narrow-tools.md): Phase 4 item 3,
  approved October 16, 2026.
- [Persisted conversations](reviews/2026-10-16-010-persisted-conversations.md):
  Phase 4 item 2, approved October 16, 2026. `20261016190413_AddConversations`
  is applied.
- [Planning model](reviews/2026-10-16-009-planning-model.md): Phase 4 item 1,
  approved October 16, 2026.
- [Phase 3 close](reviews/2026-10-16-008-phase-3-close.md): closed October 16,
  2026. The [Phase 3 UX reassessment](reviews/2026-10-16-003-phase-3-ux-reassessment.md)
  sequence is approved. Increment 1 is approved in
  [Plan next month first](reviews/2026-10-16-004-plan-next-month-first.md).
  Increment 2 is approved in
  [One way to apply a change](reviews/2026-10-16-005-one-way-to-apply.md).
  Increment 3 is approved in
  [Remaining Plan edges](reviews/2026-10-16-006-remaining-plan-edges.md).
  The [October 15 handoff](design/phase-3-ux-handoff.md)
  stays as the approved increments 1–3.

| Path                                                                                             | What it is                                                                                                                                                 |
| ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`roadmap.md`](roadmap.md)                                                                       | Product direction, the phase backlog, and what each phase built                                                                                            |
| [`decisions/`](decisions/)                                                                       | Short decision records for product and architecture choices (listed below)                                                                                 |
| [`design/linked-manual-debts.md`](design/linked-manual-debts.md)                                 | Linked manual debts design. Follow a balance, balance overrides, suggested matches, credit limit, and reconnect are approved                               |
| [`design/plan-page.md`](design/plan-page.md)                                                     | Plan page design: Overview, Try changes, and Your plan, with cash, debt, and offer as supporting views. The next-month layout is approved                     |
| [`design/savings.md`](design/savings.md)                                                         | Savings design: cash to keep, an emergency goal, and named goals under Saving for. Monthly living spending is edited on Living                             |
| [`design/living.md`](design/living.md)                                                           | Plan budget design at `/living`: unequal contribution shares, one monthly flexible-spending amount, and a monthly affordability check                      |
| [`design/allocation.md`](design/allocation.md)                                                   | Allocation setting: saved variable-income and windfall splits that can be reviewed and do not move money                                                    |
| [`design/ui-direction.md`](design/ui-direction.md)                                               | Shared UI rationale and examples; the light shell is implemented and the enforceable rules are in `.cursor/rules/ui-governance.mdc`                        |
| [`reference/transaction-activity-conventions.md`](reference/transaction-activity-conventions.md) | Income, spending, refund, transfer, and pending rules                                                                                                      |
| [`checklists/original-mvp-acceptance.md`](checklists/original-mvp-acceptance.md)                 | Manual walkthrough template and the Phase 0 record                                                                                                         |
| [`reviews/`](reviews/README.md)                                                                  | One report per increment, with an index                                                                                                                    |
| [`archive/`](archive/)                                                                           | Superseded documents kept for reference                                                                                                                    |

Decision records:

- [0001. Clerk Hobby sign-in and one household per owner](decisions/0001-clerk-sign-in-and-household-owner.md)
- [0002. Records independent of Plaid, and Plaid optional](decisions/0002-records-independent-of-plaid.md)
- [0003. Single planning currency](decisions/0003-single-planning-currency.md)
- [0004. A debt stays separate from a connected account](decisions/0004-debt-separate-from-connected-account.md)
- [0005. One light shell](decisions/0005-one-light-shell.md)
- [0006. One-shot sync worker on a cron schedule](decisions/0006-one-shot-cron-worker.md)
- [0007. Historical Plan-first navigation](decisions/0007-plan-first-in-main-nav.md) — superseded by 0009
- [0008. Forward-looking charts live on Plan](decisions/0008-forward-charts-on-plan.md)
- [0009. Home, Accounts, Activity, then Plan](decisions/0009-home-accounts-activity-plan-order.md)
- [0010. Plan setup and Settings](decisions/0010-plan-setup-and-settings.md)

Setup is in the root [`README.md`](../README.md). Agent rules are in the
root [`AGENTS.md`](../AGENTS.md) and `.cursor/rules/`.

## Document conventions

- File and folder names are lowercase kebab-case.
- Every document outside `reviews/` starts with `Status:` (`Current`,
  `Draft`, `Approved`, `Accepted`, `Implemented`, `Template`, `Historical`,
  or `Superseded by …`) and `Updated: YYYY-MM-DD`. Add `Supersedes:` when it
  replaces another document. A decision record uses `Accepted`.
- A superseded document moves to `archive/` or is deleted. Git keeps the
  history. A numbered decision stays in `decisions/` so the sequence stays
  intact, and its status says what replaced it.
- A decision record in `decisions/` is `NNNN-short-name.md`, 10–25 lines,
  and links the review that holds the detail. Code conventions stay in
  `.cursor/rules`, not in decision records.
