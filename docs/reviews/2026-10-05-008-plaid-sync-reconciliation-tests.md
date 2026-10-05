# Plaid sync reconciliation tests

Date: October 5, 2026

## Increment

Covered two reconciler rules that the sync already follows and the tests
did not. An unknown Plaid account is skipped. A manual transaction on the
same account stays put when a bank row is added. No sync behavior changed.

## Changes

- A modified transaction whose Plaid account is not in the household is
  ignored, including a row that is already stored. A known transaction in
  the same page is still added.
- Adding a bank transaction leaves a manual row on that account, with its
  name, source, and missing external id unchanged.
- The action plan now treats the signed-in UI plan as approved and these
  tests as the active engineering increment.

## Data changes

None.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --filter "FullyQualifiedName~PlaidTransactionReconciler"`:
  8 passed, 0 failed.
- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release --no-restore`:
  207 passed, 0 failed.
- `git diff --check` on the edited files passed.
- No screen, route, or stored schema changed.

## Manual verification

Waived. Nothing a person can click changed, and no household data was
written.

## Approval

Zach approved this increment on October 5, 2026.

## Pending decision

Overlapping worker and manual sync, beyond one reconcile call, is still
open. Phase 2 scenarios and expected raises also remain. Which of those
to do next is the decision.
