Date: 2026-10-17
Status: Awaiting review
PR: None

# Codebase and organization review

## Scope and conclusion

General source review of application structure, planning state and persistence,
sync lifetime, frontend boundaries, and verification coverage. This is a sampled
review, not a complete security audit or certification of every calculation.
No application code was changed. Existing documentation edits were preserved.

The architecture is serviceable and does not need a rewrite. Controllers
delegate work, pure financial rules live under `api/Domain`, EF configuration
and migrations have an authoritative API home, and frontend features separate
display, hooks, pure rules, and HTTP adapters. The main issues are the boundaries
between those pieces: identity, reviewed snapshots, transactions, and lifetimes.

## Findings, in priority order

### 1. P1 — Browser working-plan state is not scoped to the signed-in household

Evidence: `frontend/features/plan/planWorkState.ts:3` uses the single key
`cardui.plan.work`. `usePlanWork.ts:45` restores it and automatically applies
the scenario; line 133 also caches it in a module-level variable. Neither
storage nor the cache takes an owner identifier. The repository search found
no identity-change/sign-out cleanup for this state.

Trigger: user A applies a scenario, signs out, and user B signs in using the
same tab. Session storage survives that transition. B can inherit A's extra
payment, spending/cash-floor overrides, or debt constraint identifier. Server
household checks still protect record access; this finding concerns leaked
local scenario values and applying the wrong person's assumptions to B's plan.

Fix: scope stored state and the in-memory cache to authenticated identity or
household, invalidate on identity change, and avoid restoring before identity
is known. Verify A-to-B sign-in and ordinary same-user editor round trips.
Source-established path; no two-user browser reproduction performed.

### 2. P1 — Saving does not guarantee the plan matches the reviewed preview

Evidence: `frontend/features/plan/useSavedPlan.ts` sends scenario fields and
`replacesDraftId`, with no preview/fact revision. In
`api/Services/Implementations/SavedPlanService.cs:70`, SaveDraft recalculates
from current facts before opening its transaction. Draft identity checks
protect the existing draft, not the financial facts used by the preview.

Trigger: a bank sync or another tab changes cash, income, bills, or debt after
the displayed preview was calculated. Save can succeed with different figures
without asking the user to review them first.

The future chat path has a related gap: `PlanRevisions.cs:72` calculates and
checks a review fingerprint, then line 93 calls SaveDraft, which calculates
again. Line 96 accepts in a separate transaction. A change between those
operations can escape the earlier check; an acceptance failure can leave a
committed replacement draft. This chat service has no public route yet, so
that part is an integration blocker rather than a live chat failure.

Fix: define one reviewed snapshot/revision contract shared by manual and chat
flows. Validate the revision and persist the reviewed result within an
appropriate concurrency/transaction boundary. Do not blindly trust a document
posted by the browser. Test changes between preview/save, review/commit, and
failure between draft replacement and acceptance on PostgreSQL.

### 3. P2 — Sync failures can contaminate subsequent work in the worker

Evidence: `worker/Program.cs:20` creates one dependency-injection scope and
database context for the entire run. Line 60 resolves one Plaid service, and
the loop catches a failure and continues using it. There is no tracker reset
or per-item scope. `PlaidService.cs:477` records a failure with SaveChanges on
that same context, which may still contain failed pending entity changes.

Trigger: a database save fails with an added/modified entity still tracked.
Recording the failure can retry those pending changes and fail again. The
next item's SaveChanges can retry them too, causing one failed connection to
block later otherwise healthy connections. A context also accumulates tracked
rows across all households in a long worker run.

Fix: use a fresh scope/context for each item and record failure status through
a clean, narrowly scoped update/context. Verify that a failed persistence
operation for item A does not prevent item B from syncing. This is a source-
derived failure path, not an induced failure against the local database.

### 4. P2 — Full plan history is repeatedly loaded on the main calculation path

Evidence: `SavedPlanService.cs:196` loads every version, including its JSON
document, into tracking for save/accept. Line 208 loads all documents for
history; ListAsync maps every full plan. `useSavedPlan.ts:60` fetches that
entire history alongside every decision-preview load, including scenario
changes. The single-version endpoint already exists.

Impact: as versions accumulate, changing a scenario repeatedly transfers and
deserializes unrelated historical forecasts. This is especially undesirable
for phone testing and slower networks. It is a growth risk, not a measured
current latency incident.

Fix: list paginated lightweight history metadata, load a document on selection,
and decouple history refresh from scenario recalculation. For writes, query
only relevant active versions and the next-number information with proper
concurrency protection. No cache platform is needed.

### 5. P2 — Critical database behavior is not covered by the current test setup

Evidence: planning/revision service tests use EF InMemory;
`tests/Cardui.Tests/Services/PlanRevisionsTests.cs:428` explicitly suppresses
TransactionIgnoredWarning. Repository search found no UseNpgsql test setup.
There are HTTP startup/anonymous-access tests, but these do not prove complete
authenticated financial writes with PostgreSQL. CI runs existing tests and
builds without a PostgreSQL integration service.

Impact: green tests cannot establish rollback, filtered unique constraints,
real query translation, or concurrent review/save/accept behavior. This gap
matters directly to findings 2 and 3.

Fix: add a small isolated PostgreSQL integration suite with the relevant fixes
and authenticated endpoints, rather than deferring all such checks to the
end of the core journey. Keep the useful pure-rule and in-memory tests.

## Organization concerns, below the correctness findings

- **Large responsibility clusters.** `DebtsService.cs` is 1,542 lines spanning
  CRUD, linked-account state, balances, overrides, credit limits, and projection.
  `DebtList.tsx` is 1,082 lines with many local components, rules, and editing
  behavior. `PlanPageClient.tsx` is 661 lines combining navigation, working
  edits, save eligibility, and page composition. These already contain some
  useful decomposition; line count alone is not a defect. Extract cohesive
  responsibilities when fixing those areas, especially before adding chat
  state to the Plan page. Avoid a standalone repo-wide refactor phase.
- **Worker composition is too broad.** The worker references the API project
  and registers all application services, including planning/conversation
  services it does not use. The recent planning registration fix is evidence
  of coupling. Split registration into focused modules so the worker requests
  only its dependencies. A new project or microservice is not required now.
- **Shared display helpers live in features.** Plan imports money formatting
  from accounts and date formatting from debts. Move demonstrably shared pure
  formatting into a shared library as those files change. Do not promote all
  feature helpers into a generic shared folder.
- **Test runners repeat compilation plumbing.** Frontend scripts transpile
  selected modules and rewrite imports manually; package.json lists each
  runner explicitly. This works for current pure-rule coverage but creates
  maintenance work and does not exercise React hook lifecycles. Consolidate
  common runner plumbing when adding the identity/async-state tests; do not
  add a test dependency without approval.
- **Folder names are not the urgent issue.** Keeping `frontend/`, `api/`,
  and `worker/` is reasonable for this existing repository. Moving everything
  under `apps/` would not fix the user journey or any finding above. Domain
  rules separate from service implementations are a useful existing boundary.

## Verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~PlanRevisionsTests|FullyQualifiedName~PlanningCallsTests" --verbosity quiet`: 14 passed, 0 failed.
- From `frontend`: `node ./scripts/run-plan-safety-tests.mjs`: 7 passed, 0 failed.
- Documentation whitespace validation: `git diff --check` passed.

These tests do not reproduce the identity-switch or database concurrency/failure
findings. No full suite, frontend production build, automated browser pass,
live model call, bank sync, migration, or application-data mutation was performed.
No dependency-advisory audit was performed. The conclusions are based on the
checked-out code, not an externally verified framework compatibility review.

## Follow-up verification for Zach

After fixes, use isolated synthetic data. These checks change test session
state, financial facts, and plan versions:

1. Apply a scenario as test user A, then sign into B in the same tab. B must
   start with B's own facts and no inherited scenario.
2. Preview a plan, change a fact in a second tab, and save in the first.
   Require a fresh review rather than silently saving changed figures.
3. Reopen a saved version on a phone; verify its identity and figures, and
   confirm ordinary preview changes do not repeatedly reload full history.

Worker failure isolation and transactional behavior belong in focused agent
integration tests, not manual experimentation with real linked accounts.

## Approval and pending decision

Awaiting review. Recommend fixing identity isolation and reviewed-plan
consistency before expanding chat, with focused tests in those increments.
Address worker failure isolation separately and keep the other cleanup local
to touched code. This report does not change the pending core-journey roadmap
approval or authorize implementation. Stop here under the review gate.

Use a new implementation chat after prioritization: read `AGENTS.md`,
`docs/README.md`, `docs/reviews/README.md`, this report, and the current core
journey sequence. The pending decision is which finding to fix first and how
that increment fits the approved journey work.

## Note (2026-10-17)

Zach incorporated findings 1 and 2 into the approved core journey sequence.
The sequence is in [`docs/roadmap.md`](../roadmap.md).
Those checks were not run. Finding 1 is core journey item 3. Finding 2 is
item 8. The PostgreSQL coverage in finding 5, for the journey tests, starts
in item 4. Findings 3 and 4, and the organization notes, stay in this report
and are not part of that sequence.
