# Domain folders

Date: October 7, 2026
Status: Approved 2026-10-07
PR: [#26](https://github.com/Calathea-Z/Cardui/pull/26), into `dev`

## Increment

`api/Domain` held 98 files in one folder. They now sit in nine feature
folders, one level deep, and three shared files stay at the root. Each
namespace follows its folder. No behavior, model, DbContext, or migration
changed.

The branch started from `main`. When the PR moved to `dev`, `dev` was
merged in so the debt-follow and Plaid sync work is in the same layout.
This report took the next free number for its date, `004`, because `dev`
already had `2026-10-07-002` and `-003`.

## Folder map

| Folder | Files | Read by |
| --- | --- | --- |
| `api/Domain` (root) | `FinancialDate`, `HouseholdTime`, `PlanningCurrencyRules` | Most services, `api/Models/Household.cs`, `api/Security/HouseholdScope.cs`, and four entity configurations |
| `api/Domain/Accounts` | `AccountBalanceHistory`, `AccountBalanceHistoryPoint`, `AccountBalanceValue`, `AccountCurrencyBalance`, `AccountGroupKeys`, `AccountLedger`, `AccountSnapshotBalance`, `AccountTotalResult`, `AccountTotals`, `AccountTotalsCalculator`, `AccountTypes`, `BalanceReconciliation`, `LedgerTransaction` | `AccountsService`, `DashboardService`, `ManualAccountBalance`, `TransferPairingService`, `DebtsService`, `TransactionsService` |
| `api/Domain/Categories` | `CategoryKeys`, `SystemCategoryKeys`, `SystemCategoryNames`, `SystemGroupKeys` | `CategoriesService`, `SubGroupsService`, `DataSeeder`, `DashboardService`, `CategoryTargetsService`, `TransferPairingService` |
| `api/Domain/CategoryTargets` | `ActivityCategoryLabel`, `CategoryMonthActivity`, `CategoryMonthSpent`, `CategoryTargetAssignment`, `CategoryTargetCalculator`, `CategoryTargetHistoryMonth`, `CategoryTargetProgress`, `CategoryTargetRules`, `DatedActivityRow`, `ExpenseCategoryRow`, `SavedTargetMonth` | `CategoryTargetsService` |
| `api/Domain/Debts` | `DebtAccountBalanceBlock`, `DebtBalanceComparison`, `DebtBalanceFacts`, `DebtBalanceResolution`, `DebtCurrencySummary`, `DebtDraft`, `DebtFieldSource`, `DebtFollowAccountState`, `DebtFollowedBalance`, `DebtFollowEligibility`, `DebtLinkedBalance`, `DebtLinkFreshness`, `DebtRules`, `DebtSummary`, `DebtSummaryGap`, `DebtSummaryInput`, `DebtSummaryItem`, `DebtSummaryReport`, `DebtSyncedField` | `DebtsService`, `IDebtsService`, `DebtsController`, `DebtSummaryDtoMapper`, `api/Dtos/Debts` |
| `api/Domain/Income` | `IncomeRaiseDraft`, `IncomeSourceDraft`, `IncomeSourceRules`, `PaycheckSchedule` | `IncomeSourcesService` |
| `api/Domain/Obligations` | `ObligationDraft`, `ObligationRules`, `RecurringSuggestion`, `RecurringSuggestionCharge`, `RecurringSuggestionEvent`, `RecurringSuggestions` | `ObligationsService` |
| `api/Domain/Plaid` | `PlaidItemSync`, `TransactionUpsertResult` | `PlaidService`, `PlaidTransactionReconciler` |
| `api/Domain/Transactions` | `ActivityRow`, `CategoryBucket`, `MerchantHistoryActivity`, `MerchantHistoryPeriod`, `MerchantHistoryPeriods`, `MerchantHistorySeries`, `MerchantMatchKey`, `MonthlyActivityResult`, `TransactionActivityCalculator`, `TransactionActivityCategoryTotal`, `TransactionActivityTotals`, `TransactionActivityValue`, `TransactionCategoryClassifier`, `TransferTextClassifier` | `DashboardService`, `CategoryTargetsService`, `TransactionsService`, `TransactionCategorizationService`, `TransferPairingService` |
| `api/Domain/TransactionImport` | `CsvAmountParser`, `CsvAmountSign`, `CsvDataRow`, `CsvDateOrder`, `CsvDateParser`, `CsvParseResult`, `CsvTable`, `CsvTableParser`, `ImportCategoryCatalog`, `ImportCategoryMatch`, `ParsedCsvAmount`, `TransactionImportColumnGuesser`, `TransactionImportColumnMap`, `TransactionImportColumnMapRules`, `TransactionImportDates`, `TransactionImportDraft`, `TransactionImportDuplicateKey`, `TransactionImportLimits`, `TransactionImportPreviewBuilder`, `TransactionImportRowSelection`, `TransactionImportRowStatus`, `TransactionImportSuggestedColumns` | `TransactionImportService`, `api/Dtos/TransactionImport`, `TransactionImportConfiguration` |

`DebtsController` is the only controller that reads a Domain type
(`DebtSyncedField`). Nothing in `worker/` reads one directly. The tests in
`tests/Cardui.Tests/Domain` and `tests/Cardui.Tests/Services` read them.

## Changes

- **Moves.** 95 files moved with `git mv`. Each file's only content
  change is its namespace line, plus a `using` where it reads a type in
  another folder. Twelve such usings were added inside Domain, for example
  `DebtSummary` reading `AccountLedger.Round`.
- **Namespaces.** Each file is in `Cardui.Api.Domain.<Feature>`, which
  matches `Cardui.Api.Dtos.<Feature>` and `Cardui.Api.Services.Plaid`. The
  root files stay `Cardui.Api.Domain`. The busiest files are
  `DashboardService`, `CategoryTargetsService`, and
  `TransferPairingService`, with four Domain usings each, so there was no
  tradeoff to weigh.
- **Usings.** 27 API files and 35 test files changed their `using` lines
  only. Each file now imports exactly the Domain namespaces it reads.
- **Names.** Folders use the code's feature names, the same as the
  services and DTO folders. Bills stay `Obligations`, and income stays
  `Income`.
- **Plaid.** `PlaidItemSync` came from `dev`. With
  `TransactionUpsertResult` it makes `api/Domain/Plaid`, next to
  `api/Services/Plaid` and `api/Dtos/Plaid`. Both are Plaid sync rules.
- **Docs.** The moved paths are updated in
  `docs/design/linked-manual-debts.md` and three archived Phase 1 reviews
  (`2026-10-03-014`, `-015`, `-016`). Only the path strings changed, the
  same way the documentation cleanup handled moved files.
- No README was added in `api/Domain`. The repository does not keep
  per-folder READMEs for code.

## Left in place

- **Root files.** `FinancialDate`, `HouseholdTime`, and
  `PlanningCurrencyRules` are household-wide date and currency rules that
  most features read. `api/Models/Household.cs`,
  `api/Security/HouseholdScope.cs`, and the Household, Debt, Obligation,
  and IncomeSource configurations read their constants. Leaving them at
  the root keeps those files untouched.
- **Nothing moved out of Domain.** No DTO or interface is in Domain.
  - The service-only rows have no clearer home. These are `ActivityRow`,
    `MonthlyActivityResult`, `AccountCurrencyBalance`,
    `AccountTotalResult`, `DatedActivityRow`, `ExpenseCategoryRow`,
    `SavedTargetMonth`, `ActivityCategoryLabel`, `CategoryMonthActivity`,
    and `DebtFollowAccountState`. They are not request or response types,
    so they do not belong in `Dtos`. Helper records stay out of
    `Services`.
  - `backend-enums.mdc` does not allow an enum under `Services`. That
    covers `TransactionUpsertResult` and the debt enums `IDebtsService`
    reads.
- **CSV parsing stays with the import.** `CsvTableParser` reads
  `TransactionImportLimits`, and the column map reads `CsvAmountSign` and
  `CsvDateOrder`. A separate `Csv` folder would point both ways.
- **`AccountLedger.Round`** is also the money rounding for debts and
  import. A shared money folder would mean splitting that type. That
  changes code, not just its location.
- **Test files.** `tests/Cardui.Tests/Domain` stays flat. Only its usings
  changed. Matching subfolders there can be a later change.

## EF and data

- Two files under `api/Data` changed one `using` each:
  `api/Data/Configurations/TransactionImportConfiguration.cs`, for
  `TransactionImportLimits.MaxFileNameLength`, and `api/Data/DataSeeder.cs`,
  for the category keys. `DataSeeder` seeds at runtime, not with `HasData`.
- No entity, owned type, DbContext, snapshot, or migration changed. No
  Domain type is an entity or an owned type, and no entity stores a Domain
  enum. The `AddDebtAccountFollow` migration is `dev`'s and is untouched.
- No data changes.

## Agent verification

Each check compares the branch with `dev` at `0ad08f1`.

- `dotnet build Cardui.sln --no-incremental` (SDK 10.0.301): 0 warnings,
  0 errors, the same as `dev`.
- `dotnet test Cardui.sln`: 330 passed, 0 failed, the same as `dev`. The
  sorted test-name lists from `--list-tests` are identical. The CI
  commands also pass: `dotnet test ./Cardui.sln --configuration Release`
  and the Release worker build.
- EF model: a scratch console app outside the repository built
  `CarduiDBContext` with Npgsql. `Database.HasPendingModelChanges()` is
  `False`, and the design-time model's debug view matches `dev` line for
  line.
- Unused usings: `dotnet format style --diagnostics IDE0005`, with a
  temporary `.editorconfig` that was not committed, reports the same 22
  pre-existing hits as `dev`, mostly in migrations. None is new.
- Against `dev`, every changed C# line is a namespace line, a Domain
  `using`, or a blank line.
- Every backticked `api/Domain/*.cs` path in tracked `.md` and `.mdc`
  files resolves.
- The merge commit builds and passes the same checks on its own. Dev's
  new files are still at the root there.

## Manual verification

Zach, check these, or waive them. No data changes.

Before pulling, commit or stash any local edits under `api\Domain` in
`C:\Repos\Cardui`. Git sees those files as moved, so an uncommitted edit
there blocks the pull or ends up in a conflict.

1. Check out the PR branch and run `dotnet build .\Cardui.sln` from the
   repository root.
   Expected: the build succeeds with no new warnings.
2. Open `api/Domain` in the editor.
   Expected: nine feature folders and three root files. Each folder's
   files belong to that feature.
3. Open `api/Domain/Debts/DebtSummary.cs`, then go to the definition of
   `AccountLedger`.
   Expected: the namespace is `Cardui.Api.Domain.Debts`, and the
   definition opens `api/Domain/Accounts/AccountLedger.cs`.

## Approval

Zach approved the folder layout on October 7, 2026, as built. The proposed rule wording was not part of that approval.

## Pending decision

Whether to write the folder layout into the rules. The proposed wording,
not applied, is for `.cursor/rules/backend-type-files.mdc` and
`backend-domain-rules.mdc`: "A Domain type goes in `Domain/<feature>` for
the feature that owns it. A rule that several features read stays at the
`Domain` root." Without it, the existing "with the types already there"
wording still points a new file to the matching folder.

## Correction

October 7, 2026: Zach approved that wording the same day, with reconnect. `.cursor/rules/backend-type-files.mdc`, `backend-domain-rules.mdc`, and `AGENTS.md` now include it. See `2026-10-07-007-reconnect.md`.
