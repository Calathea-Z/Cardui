# Recovery application audit, September 25, 2026

Status: Historical. Moved out of `docs/roadmap.md` on October 7, 2026.
Date: 2026-09-25

This is the source-code audit the roadmap started from. The checks and implementation anchors below were no longer true by October 3–4, 2026: sign-in, household scope, and records independent of Plaid had landed. Current status is in [`docs/README.md`](../README.md).

## Audit scope and verification

The original specification was Zach's private `Plan.md`, which is gitignored and not in the repository. It describes a personal-use dashboard, an eight-item Phase 1 MVP, later budgeting and debt screens, and future enhancements. Its progress and technology sections are stale: the API now targets .NET 10, and the frontend uses Next.js 16 / React 19.

This is a source-code and local-check audit. It does not establish production deployment, live bank connectivity, scheduled worker execution, browser usability, or security readiness.

Checks run during this audit:

- `dotnet test Cardui.sln --no-restore`: 35 passed, 0 failed.
- `npm test` in frontend: 12 passed, 0 failed across the three existing script suites.
- `npm run lint` in frontend: passed after rerunning outside the filesystem sandbox.
- No production build, live Plaid exercise, database migration, or browser walkthrough was performed.

Existing tests cover selected transaction/category services, mapping, validation, transfer classification, and frontend utilities. Passing them does not establish coverage of syncing, household isolation, financial forecasting, or complete user journeys.

## Original specification versus current implementation

| Original requirement | Current evidence | Remaining work |
| --- | --- | --- |
| Connect bank through Plaid | Link UI, token exchange, protected token storage, initial sync | Live acceptance check; connection repair and removal lifecycle |
| Retrieve accounts | Account sync services, account API and views | Verify representative account types and balance freshness |
| Retrieve and store transactions | Cursor-based added/modified/removed sync and EF persistence | Exercise failure/retry, pending replacement, concurrent sync, and duplicate cases |
| View transactions | Search, account/category/pending filters, pagination, detail drawers | End-to-end verification and reconciliation edge cases |
| View balances | Account groups, available/current balance DTOs, history charts | Verify display completeness, missing balance handling, and history continuity |
| View spending by category | Summary API computes category totals | Current dashboard widget registry renders accounts and recent transactions only; add spending presentation |
| View monthly spending | Summary API computes current-month income/spending | Add visible monthly summary and clear period labels; month selection is a useful extension |
| Dashboard net worth/cash/credit/income versus expenses | Account summary and dashboard services exist | Unify net-worth definitions; expose missing summary information |
| Manual categorization, merchant details, notes | Implemented through transaction UI and APIs; merchant history added | Verify edits survive later synchronization |
| Monthly category budgets, progress, remaining | Budgets route is a coming-soon placeholder | Entire budget model, API, calculations, and UI |
| Debt balances, rates, payments, snowball/avalanche | Linked account balances exist | Debt terms, inventory UI, payment rules, payoff engine and comparisons |
| Background synchronization | One-shot worker and Railway scheduling instructions exist | Verify actual deployment/schedule, durable keys, concurrency and retry handling |
| Net-worth history | Balance snapshots and grouped history already exist | Validate missing dates/account changes; avoid treating partial snapshots as real drops |
| Assets, goals, rules, tags, monthly snapshots | No dedicated implementations found for these planned additions | Prioritize recovery goals; defer general tags/rules/assets unless needed |
| Webhooks, reminders, receipts, PWA/native, AI categorization | Not established by inspected implementation | Phase later according to recovery value |
| Investment tracking | Investment balance grouping exists | Holdings/performance tracking remains a separate later capability |

**Assessment:** the transaction/account foundation is substantially implemented. The original Phase 1 is not fully closed because spending presentation and live acceptance evidence remain missing. The broader original budget/debt vision and the new recovery experience remain to be built.

That assessment describes the September 25, 2026 audit. Phase 0 closed the local baseline on October 3, 2026; the current status and retained limitations are in the roadmap's section 5 and `docs/checklists/original-mvp-acceptance.md`.

Key implementation anchors:

- `frontend/features/dashboard/dashboard-widgets.tsx`: current visible dashboard widgets.
- `api/Services/Implementations/DashboardService.cs`: summary calculates net worth as cash minus credit cards.
- `api/Services/Implementations/AccountsService.cs`: account summary includes investments and loans in net worth; history aggregates snapshots by date.
- `frontend/app/budgets/page.tsx`: budget placeholder.
- `api/Models/Account.cs` and `Transaction.cs`: require Plaid linkage/identifiers; manual data needs model changes.
- `api/Data/CarduiDBContext.cs`: no user/household/budget/debt-plan/conversation entities.
- `api/Configuration/WebApplicationExtensions.cs`: no authentication/authorization middleware in inspected pipeline.
- `api/Services/Implementations/PlaidService.cs`: configured default client user ID; no repair/removal endpoints in current controller.
- `api/Configuration/ApplicationServiceCollectionExtensions.cs`: token protection and optional persisted key path exist.
- `worker/Program.cs`: iterates all Plaid items and invokes existing sync services.
