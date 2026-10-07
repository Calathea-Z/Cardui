# Original MVP acceptance checklist

Status: Template. Phase 0 was closed with it on October 3, 2026; that record is at the end.
Updated: 2026-10-07

The empty boxes are the reusable walkthrough, not open work.

Use this walkthrough to record the current account, transaction, balance, and
synchronization baseline before recovery-planning features change those areas.

This is a controlled local-development check, not evidence of production readiness.
Use Plaid sandbox data or another explicitly approved test environment. Do not include
credentials, access tokens, full connection strings, or private financial data in the
recorded results.

## Result labels

- **Pass:** observed result matches the expected result.
- **Fail:** behavior completed but did not match.
- **Blocked:** the check could not be completed; record the reason.
- **Not applicable:** the check does not apply to the controlled data.

Record the date, commit or branch, browser, database type, and Plaid environment. For
each non-pass result, record a concise observation and a follow-up issue or review item.

## Preconditions

- [ ] Local setup in the root `README.md` is complete.
- [ ] PostgreSQL is healthy and checked-in migrations have been applied.
- [ ] `GET http://localhost:5235/api/health` returns `{"status":"ok"}`.
- [ ] The frontend opens at `http://localhost:3000` without an API error banner.
- [ ] Only controlled test data is in scope, and any needed backup has been made.
- [ ] The starting number of institutions, accounts, and transactions is recorded.

## 1. Connect an institution

Data change: creates a Plaid item, accounts, balance snapshots, and transactions in the
local database.

- [ ] Open **Connections** from the account menu (`/institutions`) and start the Plaid
      Link flow.
- [ ] Connect a sandbox institution and return successfully to Cardui.
- [ ] Confirm the institution appears once and reports a successful synchronization.
- [ ] Refresh the page and confirm the institution remains present.
- [ ] Confirm its accounts appear on **Accounts** with recognizable names and types.

Expected result: one connection produces one institution entry and its associated
accounts. No credentials or access tokens are displayed.

## 2. Review accounts and balances

- [ ] Confirm active accounts are grouped by their displayed account type.
- [ ] Confirm each account clearly displays available/current balance information when
      that information exists.
- [ ] Compare displayed account balances with the controlled source data.
- [ ] Compare account-page totals with the dashboard for the same data and record every
      disagreement, including which account types are included or excluded.
- [ ] Open available balance-history views and confirm dates are ordered and gaps are
      not rendered as zero-value drops without evidence.

Expected result: account values match the controlled source and missing values are not
presented as real zero balances. The current code has differing net-worth definitions;
recording any resulting disagreement is part of this baseline check.

## 3. Review and find transactions

- [ ] Confirm recent transactions appear with merchant/name, date, amount, account,
      category, and pending state where applicable.
- [ ] Search for a known transaction and confirm unrelated results are excluded.
- [ ] Filter by account, category, and pending status using controlled examples.
- [ ] Move between result pages and confirm records are neither skipped nor repeated.
- [ ] Open a transaction detail drawer and confirm its available source details are
      understandable.
- [ ] Open merchant history and confirm its entries and chart correspond to the selected
      merchant.

Expected result: search, filters, paging, details, and merchant history agree with the
same transaction set.

## 4. Edit transaction metadata

Data change: updates the selected transaction's user-managed fields.

- [ ] Choose a controlled posted transaction and record its current category and notes.
- [ ] Change its category and notes, then close and reopen the detail view.
- [ ] Refresh the page and confirm both edits persist.
- [ ] Run a synchronization for the institution.
- [ ] Confirm the edited category and notes still persist after synchronization.
- [ ] Restore the original values if the test data should remain unchanged.

Expected result: user-managed edits survive refresh and subsequent synchronization.

## 5. Repeat synchronization

Data change: may add, modify, or remove synchronized accounts, balance snapshots, and
transactions according to the sandbox source.

- [ ] Record transaction counts and stable identifiers available before synchronization.
- [ ] Synchronize the institution once and record added, modified, and removed counts.
- [ ] Synchronize it again without changing the sandbox source.
- [ ] Confirm the second run does not create duplicate institutions, accounts, or
      transactions.
- [ ] Confirm removed source transactions disappear and modified transactions update
      when the controlled source provides those cases.
- [ ] Confirm a pending-to-posted replacement does not appear as two lasting purchases
      when the controlled source provides that case.

Expected result: repeated imports are idempotent for unchanged source data and preserve
user-managed metadata.

## 6. Run the scheduled worker path

Data change: synchronizes every connected Plaid item in the configured database.

- [ ] Confirm the worker is configured for the controlled local database and sandbox.
- [ ] Run `dotnet run --project .\worker`.
- [ ] Confirm each connected item reports success or a useful per-item failure.
- [ ] Confirm the process exits successfully only when all items succeed.
- [ ] Recheck transaction counts and edited metadata for duplicates or regressions.

Expected result: the one-shot worker processes all items, reports its outcome, and exits.
A failed item sets a non-zero exit code. An empty item list exits without syncing.

**Recorded result:** Pass. On October 3, 2026, Zach confirmed the local one-shot worker
works and does what this section expects. This record does not include worker logs,
per-item added, modified, or removed counts, or a new transaction-count check.
Account synchronization and user edits surviving sync remain the confirmation in
`docs/reviews/2026-10-03-002-preserve-transaction-user-edits.md`.

## 7. Review dashboard summaries

- [ ] Confirm Home's net worth card and Recent activity load without errors.
- [ ] Call `GET http://localhost:5235/api/dashboard/summary` and confirm the response
      includes current-month income, spending, and category totals for the controlled
      data.
- [ ] Confirm transfers, refunds, and pending transactions are treated consistently;
      record uncertain or incorrect classifications.
- [ ] Confirm Home's This month panel shows the reporting period, income,
      spending, the difference, and category spending for the controlled data.

Expected result: the API summary and the visible This month panel reconcile to
the same controlled transactions. Home showed this panel as Monthly Activity before
October 5, 2026. Those summaries were added on October 2, 2026 and
confirmed in `docs/reviews/2026-10-02-003-dashboard-spending-and-financial-totals.md`
and `docs/reviews/2026-10-02-004-transaction-activity-conventions.md`.

## 8. Record the outcome

- [ ] Summarize passes, failures, blockers, and known gaps.
- [ ] Record whether repeated imports changed row counts unexpectedly.
- [ ] Record whether any user edit was lost.
- [ ] Record every dashboard/account total disagreement.
- [ ] Record limitations caused by sandbox behavior or unavailable live connectivity.
- [ ] Confirm no secrets or private financial data were captured in evidence.

The original MVP baseline is accepted only when failures and blockers are either fixed
or explicitly retained as tracked limitations. This checklist does not cover
authentication, household isolation, public deployment, or production Plaid readiness.

## Phase 0 baseline record

Closed October 3, 2026 for local development. This is the foundation checkpoint for
continued internal work. It is not a production, live-bank, or scheduled-deployment
claim.

Confirmed earlier, and not repeated for this close:

- Dashboard monthly activity, category spending, and shared account-total calculations
  were confirmed on October 2, 2026.
- Posted income, spending, refund, transfer, and pending conventions were confirmed on
  October 2, 2026.
- Balance history carries the last known balance. Zach confirmed the accounts chart on
  October 3, 2026.
- User edits to a transaction date, category, and notes survive a later sync. Zach
  confirmed that on October 3, 2026.
- CI runs API tests, frontend tests, frontend lint, the worker Release build, and the
  frontend production build. Zach reported the GitHub Actions runs succeeded. Those
  sessions did not read the Actions logs.

Limitations retained with this close:

- No integration-test suite exists, so CI does not start PostgreSQL or apply migrations.
- The frontend production build in CI uses local development URLs. It is not a
  deployable production bundle.
- Node.js is pinned to major version 22. The repository does not pin a Node patch.
- The worker result is a local one-shot run. It does not prove a deployed schedule,
  durable Data Protection keys, concurrency protection, retry behavior, or live bank
  connectivity.
- Edits saved before migration `20261003050300_PreserveTransactionUserEdits` are not
  marked, so a later sync can still replace those dates.
- Sections 1–5 and 7 were not re-run as one recorded pass with row counts, browser,
  database type, and Plaid environment. Their evidence is the October 2–3 reviews.
