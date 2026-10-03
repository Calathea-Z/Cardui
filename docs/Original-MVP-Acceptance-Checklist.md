# Original MVP acceptance checklist

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

- [ ] Open **Institutions** and start the Plaid Link flow.
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

## 7. Review dashboard summaries

- [ ] Confirm account and recent-transaction widgets load without errors.
- [ ] Call `GET http://localhost:5235/api/dashboard/summary` and confirm the response
      includes current-month income, spending, and category totals for the controlled
      data.
- [ ] Confirm transfers, refunds, and pending transactions are treated consistently;
      record uncertain or incorrect classifications.
- [ ] Record that monthly income-versus-spending and category spending are not yet
      visible dashboard widgets if that remains true.

Expected result: the API summary is available and reconcilable to controlled
transactions. Missing visible spending summaries are a known Phase 0 gap, not a reason
to claim the original MVP is complete.

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
