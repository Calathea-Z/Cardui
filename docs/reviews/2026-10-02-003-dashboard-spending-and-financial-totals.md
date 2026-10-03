# Dashboard spending and consistent financial totals

Date: October 2, 2026

## Increment

Added visible current-month activity and category spending to the dashboard,
centralized current and historical account-total calculations, and corrected
the local financial date used for balance snapshots.

## Changes

- Added a dashboard Monthly Activity panel with the exact reporting period,
  income, spending, difference, category amounts, proportional bars, and an
  empty state.
- Added API reporting-period fields so the frontend displays the same date
  range used by the dashboard calculation.
- Centralized cash, investment, credit-card, loan, asset, liability, and net
  worth calculations in one domain calculator used by dashboard, account, and
  balance-history summaries.
- Added focused backend coverage for account classification, cross-summary net
  worth agreement, monthly activity, transfer exclusion, and local financial
  dates.
- Added frontend coverage for period labels, category percentages, and
  insufficient balance-history guidance.
- Replaced the generic balance-history error with messages that distinguish
  zero snapshots from one snapshot and explain that snapshots come from
  account synchronization.
- Corrected account syncs to date snapshots using the configured local time
  instead of UTC, matching dashboard periods, and excluded future-dated
  snapshots from returned history without deleting stored records.

## Agent verification

- `dotnet test Cardui.sln -c Release --no-restore`: 38 passed, 0 failed.
  Release configuration was used for the final run because the active local
  API process held the Debug executable open.
- `pnpm test` in `frontend`: 16 passed, 0 failed.
- `pnpm lint` in `frontend`: passed.
- `pnpm build` in `frontend`: passed.
- `git diff --check`: passed.
- IDE diagnostics reported no errors in the edited files.
- Read-only browser verification confirmed the monthly activity panel,
  current period, category presentation, corrected one-week history guidance,
  and local API date behavior.

## Manual verification

Zach reviewed the dashboard on October 2, 2026 and confirmed that the monthly
activity presentation looked good. After reporting confusing short-range
balance-history behavior, Zach accepted the corrected snapshot-date behavior
and explanatory message.

No migration or application-data mutation was introduced. The local API was
restarted to load the corrected code. The existing future-dated snapshot was
not deleted; future snapshots are excluded from history responses.

## Remaining considerations

- Balance history is intentionally based on account snapshots, not inferred
  from transactions. Existing gaps remain and need the later Phase 0
  balance-history and repeated-sync audit.
- Pending transactions may carry a future effective date supplied by the bank
  feed. The inspected October 3 transactions were all pending; their stored
  dates were not rewritten.
- Existing monthly activity semantics remain unchanged: negative
  non-transfer amounts are counted as income and positive non-transfer amounts
  as spending. Refund, pending, and earned-income conventions still need the
  planned Phase 0 classification audit.
