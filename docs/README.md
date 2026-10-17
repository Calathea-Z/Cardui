# Cardui documentation

Status: Current
Updated: 2026-10-17

## Status

Linear is the source of truth for current work. The selected issue is the
task. This block records durable priorities and open decisions. Update it
only when an approved task changes them. It does not choose the next issue.

**Now**

The [planning architecture](design/planning-architecture.md) map is approved.
Desktop guided recovery and MX connectivity come before phone access. The
next increment is core journey item 3 in [`roadmap.md`](roadmap.md): scope
the working plan to the signed-in household. Items 4, 6, 7, and 8 then build
the guided path on the desktop. Item 12 proposes MX connectivity after that
desktop path and before the phone. Its scope is not written. Phone items 2
and 5 wait.

The core journey sequence and its acceptance contract are approved in the
roadmap. The scripted planning provider in that sequence is not in the
application code. Phase 4 items 1–9 stay approved as backend components, and
the phase exit is not met. Phase 5 item 1 stays approved: Overview still
answers the next 30 days first, and under that action the working preview
lists the remaining ranked actions, the milestones the calculation
produced, and the unset bills, flexible spending, and cash to keep.
Calculation rules are in
[`reference/recovery-calculations.md`](reference/recovery-calculations.md).
Conversation limits are in
[`reference/planning-conversation.md`](reference/planning-conversation.md).
Planning calls register with the planning model, so the sync worker starts
while those settings stay blank. There is no chat screen and no live OpenAI
call. Detail panel keyboard focus stays tracked.

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
  installed. It waits behind MX connectivity. The note is in
  [`reviews/2026-10-17-006-phone-access-proposal.md`](reviews/2026-10-17-006-phone-access-proposal.md).
- Whether the approved architecture map covers the work Linear still titles
  TOR-2. Linear was not changed.
- Whether a saved allocation should remain outside the recovery calculation.
- Which OpenAI model to use, and the spending cap for item 9. The API key
  and model stay blank. The scripted provider named in the roadmap is not
  built.
- Hosting and production configuration. Until a public host exists,
  `Plaid:WebhookUrl` stays empty, so Plaid cannot report a revoked bank
  connection.
- The remaining codebase-review findings stay unscheduled: worker failure
  isolation, history loading, and the organization notes. The note is in
  [`reviews/2026-10-17-005-codebase-organization-review.md`](reviews/2026-10-17-005-codebase-organization-review.md).
  Findings 1 and 2 are already core journey items 3 and 8. Their checks
  were not run.
- Conversational read, preview, and save are mapped and awaiting review in
  [`reviews/2026-10-17-007-conversational-planning-readiness.md`](reviews/2026-10-17-007-conversational-planning-readiness.md).
  No route or screen calls them. TOR-3 and TOR-4 have not started.

## Map

| Path | What it is |
| --- | --- |
| [`roadmap.md`](roadmap.md) | Product direction, the phase backlog, and what each phase built |
| [`decisions/`](decisions/) | Short decision records for product and architecture choices |
| [`design/planning-architecture.md`](design/planning-architecture.md) | Approved map of the recovery calculation, the saved plan, and the Plan page. Desktop guided recovery and MX connectivity come before phone access |
| [`design/linked-manual-debts.md`](design/linked-manual-debts.md) | Linked manual debts. Follow a balance, balance overrides, suggested matches, credit limit, and reconnect are approved |
| [`design/plan-page.md`](design/plan-page.md) | Plan page: Overview, Try changes, and Your plan |
| [`design/savings.md`](design/savings.md) | Cash to keep, an emergency goal, and named goals |
| [`design/living.md`](design/living.md) | Plan budget at `/living` |
| [`design/allocation.md`](design/allocation.md) | Saved variable-income and windfall splits that do not move money |
| [`design/ui-direction.md`](design/ui-direction.md) | Shared UI rationale. The enforceable rules are in `.cursor/rules/ui-governance.mdc` |
| [`reference/recovery-calculations.md`](reference/recovery-calculations.md) | Interest, payoff, forecast, spendable cash, offers, and card activity |
| [`reference/planning-conversation.md`](reference/planning-conversation.md) | What the planning model can and cannot do. The phase exit is not met |
| [`reference/transaction-activity-conventions.md`](reference/transaction-activity-conventions.md) | Income, spending, refund, transfer, and pending rules |
| [`checklists/original-mvp-acceptance.md`](checklists/original-mvp-acceptance.md) | Manual walkthrough template and the Phase 0 record |
| [`archive/`](archive/) | Superseded documents and the October 16 behavior-QA record |
| [`reviews/`](reviews/README.md) | Open investigations only. Closed increment reports are in Git history |

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
  and links the design, reference, or decision detail that holds the
  substance. Code conventions stay in `.cursor/rules`, not in decision
  records.
- `reviews/` holds an open investigation only until it is reviewed. Actionable
  findings then go to Linear. Lasting knowledge goes to a decision, design,
  reference, or the roadmap. The folder goes away when it is empty. Do not
  add a report for a routine increment.
