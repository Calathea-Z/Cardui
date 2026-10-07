# Backend method comments

Date: October 3, 2026

## Increment

Added purpose comments to backend methods, and a standing rule so new
backend methods get the same comments.

## Changes

- Public service methods are explained on the interface. Implementations
  use `/// <inheritdoc />`.
- Private methods and methods with no interface have their own summary.
  Constructors are not documented.
- Each controller action lists the HTTP method and route, then what it does.
- The same rule is in `AGENTS.md` and
  `.cursor/rules/backend-method-comments.mdc`. It applies to every Cardui
  chat.
- Generated EF Core migration files were left unchanged.
- `worker/Program.cs` and `api/Program.cs` are top-level startup code, so
  they have no methods to comment.

## Agent verification

- `dotnet build .\api\api.csproj -c Release`: succeeded, 0 warnings.
- Did not run the test suite. The comments do not change behavior.
- Did not click through the app.

## Manual verification

No screen or data change. Spot-check the comments, or waive this list.

1. Open `api/Controllers/AccountsController.cs`.
   Expected: each action names its route, such as `GET /api/accounts` and
   `POST /api/accounts/{id}/reconciliation`, and says what it does.
2. Open `api/Services/Interfaces/IAccountsService.cs` and
   `api/Services/Implementations/AccountsService.cs`.
   Expected: the interface explains each public method. The implementation
   says `<inheritdoc />` on those methods and explains the private helpers.
3. Open `api/Services/Implementations/TransactionsService.cs`.
   Expected: private helpers such as pagination, filters, and balance
   refresh have their own summaries.

Zach approved this increment on October 3, 2026.

## Remaining considerations

- Financial profile preferences are the next Phase 1 item. Mixed-currency
  totals are still unhandled.
- System category edits still change the shared catalog.
- Custom category and subgroup names and keys remain unique across every
  household until a separate migration.
