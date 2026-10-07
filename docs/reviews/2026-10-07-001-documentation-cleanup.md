# Documentation cleanup

Date: October 7, 2026
Status: Awaiting review
PR: from `cursor/docs-cleanup-9b44`

## Increment

Applied the approved documentation audit. Status that was decided, merged,
or superseded no longer reads as pending. The docs folder has one status
page, a map, kebab-case names, status headers, and archived closed-phase
reviews. Zach's answers and the four approved governance changes are in
the rules. No application code changed.

## Changes

- **Status fixes.**
  - The "paused Plaid sync reconciliation tests" wording is corrected in
    the roadmap and the linked-debts design. Those tests were approved on
    October 5 (`2026-10-05-008`). The next increment is
    `PlaidAccountSyncService` tests, a guard for overlapping worker and
    manual sync, and interrupted-sync timestamps.
  - The roadmap no longer says Phase 1 has not started, or that Phase 2 is
    next. It explains that `Plan.md` is private and gitignored, and calls
    the roadmap approved.
  - Phase 2 status is a table of items with their reviews. Section 9
    marks done items. Adopted defaults and the Clerk choice are marked.
  - `docs/design/ui-direction.md` is marked Implemented, and
    `.cursor/rules/ui-governance.mdc` wins where they differ. The old
    shell description and the first-increment scope are summarized as
    historical. Settings list all six items. Plan as a fourth tab is now
    an open decision instead of a conflicting rule.
  - The acceptance checklist is marked as a template, and its UI labels
    match the current app.
- **Zach's answers.**
  - The Accounts more-options menu approval is recorded in
    `2026-10-05-015`.
  - The security rule from `2026-10-04-007` is `.cursor/rules/security.mdc`,
    with pointers in `AGENTS.md` and `frontend/AGENTS.md`.
  - Detail panel keyboard focus is under "Tracked UI follow-ups" in
    `docs/roadmap.md`.
- **Single status source.** `docs/README.md` holds Now / Next / Tracked /
  Open decisions and the docs map. The roadmap's current-work paragraph is
  now a pointer to it.
- **Moves.**
  - `docs/Recovery-Application-Action-Plan.md` → `docs/roadmap.md`
  - `docs/ui-direction.md` → `docs/design/ui-direction.md`
  - `docs/Transaction-Activity-Conventions.md` →
    `docs/reference/transaction-activity-conventions.md`
  - `docs/Original-MVP-Acceptance-Checklist.md` →
    `docs/checklists/original-mvp-acceptance.md`
  - Roadmap sections 2–3, the September 25 audit, →
    `docs/archive/2026-09-25-recovery-audit.md`
  - Phase 0 reviews (10) → `docs/reviews/archive/phase-0/`
  - Phase 1 reviews (29) → `docs/reviews/archive/phase-1/`
- **Duplicate review numbers.** One report from each pair took its date's
  next free number. Only the file name changed.
  - `2026-10-03-013-financial-profile` → `2026-10-03-018`
  - `2026-10-04-008-dotrush-solution-pin` → `2026-10-04-017`
  - `2026-10-04-009-dotrush-hover-docs` → `2026-10-04-018`
  - `2026-10-05-005-ui-review-notes` → `2026-10-05-017`
  - `2026-10-05-009-ui-governance` → `2026-10-05-018`
- **Old reports.** The only changes inside them are path strings and links
  updated to the new locations. There are two additions: an Approval
  section in `2026-10-05-015`, and a dated correction note in
  `2026-10-06-001`.
- **Review index.** Each line has a status, grouped by phase, newest first.
  Twelve reviews are marked "Merged in PR #N; approval not recorded". The
  audit counted 11 and missed `2026-10-04-007`.
- **Governance.** These are applied the same way in `AGENTS.md`,
  `frontend/AGENTS.md`, `.cursor/rules/handoff.mdc`, and
  `docs/reviews/README.md`.
  - The handoff names `docs/README.md` and the next roadmap item, not
    "next Phase 1 item".
  - Each review updates the status block.
  - Closed phases may move to `docs/reviews/archive/<phase>/`.
  - `AGENTS.md` has a docs map and review-report conventions.
  - `AGENTS.md` runs `dotnet ef` from the repository root, which matches
    `migrations.mdc`.
- **Decision records.** Six were added in `docs/decisions/`: sign-in and
  household, records independent of Plaid, single planning currency, debt
  separate from the connected account, light shell and three destinations,
  and the one-shot cron worker.
- **Setup docs.**
  - `frontend/README.md` lists the Clerk env keys.
  - `worker/README.md` uses Railway's five-field cron (`0 9 * * *`, which
    was `0 0 9 * * *`) and is titled "Cardui sync worker".
  - The root README marks Plaid as optional and links `docs/README.md`.

## Data changes

None.

## Agent verification

- A script resolved every relative Markdown link and every backticked
  repository path in tracked `.md` and `.mdc` files. Every link and path
  resolves.
- `rg` finds no old file names, old review numbers, "paused Plaid sync",
  "next Phase 1 item", or "Phase 2 is next" outside this report, the review
  index, and the old reports' own point-in-time text.
- `git diff -M` on the moved reviews shows only path-string changes.
- `prettier@3.9.4 --check` on `frontend/README.md`, `frontend/AGENTS.md`,
  and `frontend/CLAUDE.md` passed.
- Railway's cron format was checked against its documentation: five
  fields, UTC.
- No application build or test was run, because no code changed.

## Manual verification

Zach, check these on the PR branch in GitHub, or waive them. No data
changes.

1. Open `docs/README.md`.
   Expected: Now, Next, Tracked, and Open decisions match your
   understanding, and every map link opens.
2. Open `docs/reviews/README.md`.
   Expected: three phase groups, a status on each line, and the archived
   links open.
3. Open `docs/roadmap.md`, "Phase 2" and "Sync correctness and linked
   debts".
   Expected: the item table, the next step, and no "paused" tests.
4. Read `.cursor/rules/security.mdc` and `.cursor/rules/handoff.mdc`.
   Expected: the wording you approved.

## Pending decision

Where Plan (`/budgets`) goes once it has a real screen. Today
`ui-governance.mdc` keeps three primary destinations. After this review,
the next increment is sync correctness, item 1 of "Sync correctness and
linked debts".
