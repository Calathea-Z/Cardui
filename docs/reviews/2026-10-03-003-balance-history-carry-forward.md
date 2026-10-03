# Balance history carries the last known balance

Date: October 3, 2026

## Increment

Stopped a missing account snapshot from looking like a real balance drop.
History still uses recorded snapshots only. On a later snapshot day, an active
account that was not snapshotted again keeps its last known balance.

## Changes

- Added `AccountBalanceHistory` to build one point per snapshot date.
- Each point includes every account that already has a snapshot, using the
  newest balance recorded on or before that date.
- An account is absent from points before its first snapshot.
- Two snapshots for the same account on the same day use the later one.
- Days with no snapshots still produce no point.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~AccountBalance"`:
  4 passed, 0 failed.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --no-restore`:
  62 passed, 0 failed.
- `git diff --check`: passed.
- IDE diagnostics reported no errors in the edited files.

No migration and no stored data change. The accounts chart will show the new
totals after the API is restarted. No browser check was performed.

## Manual verification

Zach reviewed the accounts balance chart on October 3, 2026 and confirmed it
looks good. He approved this increment. No stored data was changed.

## Remaining considerations

- Closed accounts stay out of history because only active accounts are loaded.
- The chart does not invent points for days when nothing was synchronized.
- This does not change how snapshots are written during sync.
