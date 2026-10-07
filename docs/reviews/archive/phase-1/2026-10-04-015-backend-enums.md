# Backend enums

Date: October 4, 2026

## Increment

Turned closed Cardui value sets into enums, left external and slug values
as strings, and recorded that split as a standing rule.

## Changes

- `FinancialRecordSource` and `FinancialRecordProvenance` are enums in
  `api/Models`. Accounts and transactions store them with
  `HasConversion<string>()`, so the columns stay the same text.
- CSV amount sign, date order, and import row status are enums in
  `api/Domain`. The API writes those names and rejects a number.
- The balance-reconciliation display name stays the string
  "Balance reconciliation".
- `TransactionUpsertResult` moved from `Services/Plaid` to `Domain`.
- Account type, account subtype, category keys, and account group keys
  stay strings. Plaid can send types outside the four manual types, and
  the group keys are slugs such as `net-worth`.
- The Cardui rule is in `AGENTS.md`, `.cursor/rules/backend-enums.mdc`, and a
  pointer on the type-file rule. It applies to every Cardui chat.
- The portable rule is in `~/.cursor/rules/csharp-enums.mdc`. It applies
  in every project.

## Data changes

None. The source and provenance columns stay `character varying`. No
migration. `dotnet ef migrations has-pending-model-changes` reported no
model changes.

## Agent verification

- `dotnet test .\tests\Cardui.Tests\Cardui.Tests.csproj -c Release`:
  191 passed, 0 failed, 0 skipped.
- That includes the new JSON cases: member names serialize as
  `"Plaid"`, `"BalanceReconciliation"`, `"PositiveIn"`, `"DayFirst"`,
  and `"Duplicate"`, and a numeric `0` is rejected.
- Release was used so the build would not need the debug `api.exe` lock.
- Did not click through the app.

## Manual verification

Zach approved this increment on October 4, 2026. An omitted amount sign
or date order uses Money out and Month first. A value that is not one of
the known names is rejected by the API instead of the old column-map
message.

1. Open the dashboard and the accounts page.
   Expected: the same groups and totals are still there.
2. Add a manual transaction, then reconcile that account to a different
   statement balance.
   Expected: income and spending ignore the adjustment. The adjustment
   name is still "Balance reconciliation".
3. Start a CSV import. Switch Month first, Day first, Money out, and
   Money in, then preview.
   Expected: the preview dates, amounts, and Ready, Duplicate, or Error
   states still follow those choices.

## Pending decision

None for this increment. Phase 2 item 1 is still next: income sources,
take-home amount, cadence, next payment date, contributor, and reliability.
