# Domain folders

Date: October 7, 2026
Status: Awaiting review
PR: from `cursor/domain-feature-folders-2e70`

## Increment

`api/Domain` held 89 files in one folder. They now sit in eight feature
folders, one level deep, and three shared files stay at the root. Each
namespace follows its folder. No behavior, model, DbContext, or migration
changed.

## Folder map

| Folder | Files | Read by |
| --- | --- | --- |
| `Domain/` (root) | `FinancialDate`, `HouseholdTime`, `PlanningCurrencyRules` | Most services, `Models/Household`, `Security/HouseholdScope`, and four entity configurations |
| `Domain/Accounts` | `AccountBalanceHistory`, `AccountBalanceHistoryPoint`, `AccountBalanceValue`, `AccountCurrencyBalance`, `AccountGroupKeys`, `AccountLedger`, `AccountSnapshotBalance`, `AccountTotalResult`, `AccountTotals`, `AccountTotalsCalculator`, `AccountTypes`, `BalanceReconciliation`, `LedgerTransaction` | `AccountsService`, `DashboardService`, `ManualAccountBalance`, `TransferPairingService`, `DebtsService`, `TransactionsService` |
| `Domain/Categories` | `CategoryKeys`, `SystemCategoryKeys`, `SystemCategoryNames`, `SystemGroupKeys` | `CategoriesService`, `SubGroupsService`, `DataSeeder`, `DashboardService`, `CategoryTargetsService`, `TransferPairingService` |
| `Domain/CategoryTargets` | `ActivityCategoryLabel`, `CategoryMonthActivity`, `CategoryMonthSpent`, `CategoryTargetAssignment`, `CategoryTargetCalculator`, `CategoryTargetHistoryMonth`, `CategoryTargetProgress`, `CategoryTargetRules`, `DatedActivityRow`, `ExpenseCategoryRow`, `SavedTargetMonth` | `CategoryTargetsService` |
| `Domain/Debts` | `DebtAccountBalanceBlock`, `DebtBalanceComparison`, `DebtCurrencySummary`, `DebtDraft`, `DebtLinkedBalance`, `DebtRules`, `DebtSummary`, `DebtSummaryGap`, `DebtSummaryInput`, `DebtSummaryItem`, `DebtSummaryReport` | `DebtsService`, `DebtSummaryDtoMapper`, `Dtos/Debts` |
| `Domain/Income` | `IncomeRaiseDraft`, `IncomeSourceDraft`, `IncomeSourceRules`, `PaycheckSchedule` | `IncomeSourcesService` |
| `Domain/Obligations` | `ObligationDraft`, `ObligationRules`, `RecurringSuggestion`, `RecurringSuggestionCharge`, `RecurringSuggestionEvent`, `RecurringSuggestions` | `ObligationsService` |
| `Domain/Transactions` | `ActivityRow`, `CategoryBucket`, `MerchantHistoryActivity`, `MerchantHistoryPeriod`, `MerchantHistoryPeriods`, `MerchantHistorySeries`, `MerchantMatchKey`, `MonthlyActivityResult`, `TransactionActivityCalculator`, `TransactionActivityCategoryTotal`, `TransactionActivityTotals`, `TransactionActivityValue`, `TransactionCategoryClassifier`, `TransactionUpsertResult`, `TransferTextClassifier` | `DashboardService`, `CategoryTargetsService`, `TransactionsService`, `TransactionCategorizationService`, `TransferPairingService`, `PlaidTransactionReconciler` |
| `Domain/TransactionImport` | `CsvAmountParser`, `CsvAmountSign`, `CsvDataRow`, `CsvDateOrder`, `CsvDateParser`, `CsvParseResult`, `CsvTable`, `CsvTableParser`, `ImportCategoryCatalog`, `ImportCategoryMatch`, `ParsedCsvAmount`, `TransactionImportColumnGuesser`, `TransactionImportColumnMap`, `TransactionImportColumnMapRules`, `TransactionImportDates`, `TransactionImportDraft`, `TransactionImportDuplicateKey`, `TransactionImportLimits`, `TransactionImportPreviewBuilder`, `TransactionImportRowSelection`, `TransactionImportRowStatus`, `TransactionImportSuggestedColumns` | `TransactionImportService`, `Dtos/TransactionImport`, `TransactionImportConfiguration` |

No controller and nothing in `worker/` reads a Domain type directly. The
tests in `tests/Cardui.Tests/Domain` and `tests/Cardui.Tests/Services`
read them.

## Changes

- **Moves.** 86 files moved with `git mv`. Each file's only content
  change is its namespace line, plus a `using` where it reads a type in
  another folder. Ten such usings were added inside Domain, for example
  `DebtSummary` reading `AccountLedger.Round`.
- **Namespaces.** `Cardui.Api.Domain.<Feature>`, which matches
  `Cardui.Api.Dtos.<Feature>` and `Cardui.Api.Services.Plaid`. The root
  files stay `Cardui.Api.Domain`. The usings stayed short. The busiest
  files are `DashboardService`, `CategoryTargetsService`, and
  `TransferPairingService`, with four Domain usings each, so the
  namespaces did not need a tradeoff.
- **Usings.** 22 API files and 31 test files changed their `using` lines
  only. Each file now imports exactly the Domain namespaces it reads.
- **Names.** Folders use the code's feature names, the same as the
  services and DTO folders. Bills stay `Obligations`, and income stays
  `Income`.
- **Docs.** The moved paths are updated in
  `docs/design/linked-manual-debts.md` and three archived Phase 1 reviews
  (`2026-10-03-014`, `-015`, `-016`). Only the path strings changed, the
  same way the documentation cleanup handled moved files.
- No README was added in `api/Domain`. The repository does not keep
  per-folder READMEs for code.

## Left in place

- **Root files.** `FinancialDate`, `HouseholdTime`, and
  `PlanningCurrencyRules` are household-wide date and currency rules that
  most features read. `Models/Household.cs`, `Security/HouseholdScope.cs`,
  and the Household, Debt, Obligation, and IncomeSource configurations
  read their constants. Leaving them at the root keeps those files
  untouched.
- **Nothing moved out of Domain.** No DTO or interface is in Domain.
  - The service-only rows have no clearer home. These are `ActivityRow`,
    `MonthlyActivityResult`, `AccountCurrencyBalance`,
    `AccountTotalResult`, `DatedActivityRow`, `ExpenseCategoryRow`,
    `SavedTargetMonth`, `ActivityCategoryLabel`, and
    `CategoryMonthActivity`. They are not request or response types, so
    they do not belong in `Dtos`. Helper records stay out of `Services`.
  - `TransactionUpsertResult` is read only by `PlaidTransactionReconciler`.
    `backend-enums.mdc` does not allow an enum under `Services`.
- **CSV parsing stays with the import.** `CsvTableParser` reads
  `TransactionImportLimits`, and the column map reads `CsvAmountSign` and
  `CsvDateOrder`. A separate `Csv` folder would point both ways.
- **`AccountLedger.Round`** is also the money rounding for debts and
  import. A shared money folder would mean splitting that type. That
  changes code, not just its location.
- **Test files.** `tests/Cardui.Tests/Domain` stays flat (24 files). Only
  its usings changed. Matching subfolders there can be a later change.

## EF and data

- Two files under `api/Data` changed one `using` each:
  `Configurations/TransactionImportConfiguration.cs`, for
  `TransactionImportLimits.MaxFileNameLength`, and `DataSeeder.cs`, for the
  category keys. `DataSeeder` seeds at runtime, not with `HasData`.
- No entity, owned type, DbContext, snapshot, or migration changed. No
  Domain type is an entity or an owned type.
- No data changes.

## Agent verification

- `dotnet build Cardui.sln` (SDK 10.0.301, `--no-incremental`): 0
  warnings, 0 errors, the same as `main`.
- `dotnet test Cardui.sln`: 302 passed, 0 failed, the same as `main`. The
  sorted test-name lists from `--list-tests` are identical before and
  after. The CI commands, `dotnet test ./Cardui.sln --configuration
  Release` and the Release worker build, also pass.
- EF model: a scratch console app outside the repository built
  `CarduiDBContext` with Npgsql. `Database.HasPendingModelChanges()` was
  `False` before and after. The design-time model's debug view is
  identical, line for line.
- Unused usings: `dotnet format style --diagnostics IDE0005`, with a
  temporary `.editorconfig` that was not committed, reports the same 22
  pre-existing hits as `main`, mostly in migrations. None is new.
- Every backticked `api/Domain/*.cs` path in tracked `.md` and `.mdc`
  files resolves.

## Manual verification

Zach, check these, or waive them. No data changes.

Before pulling, commit or stash any local edits under `api\Domain` in
`C:\Repos\Cardui`. Git sees those files as moved, so an uncommitted edit
there blocks the pull or ends up in a conflict.

1. From the repository root, run `dotnet build .\Cardui.sln`.
   Expected: the build succeeds with no new warnings.
2. Open `api/Domain` in the editor.
   Expected: eight feature folders and three root files. Each folder's
   files belong to that feature.
3. Open `api/Domain/Debts/DebtSummary.cs`, then go to the definition of
   `AccountLedger`.
   Expected: the namespace is `Cardui.Api.Domain.Debts`, and the
   definition opens `api/Domain/Accounts/AccountLedger.cs`.

## Pending decision

Whether to write the folder layout into the rules. The proposed wording,
not applied, is for `.cursor/rules/backend-type-files.mdc` and
`backend-domain-rules.mdc`: "A Domain type goes in `Domain/<feature>` for
the feature that owns it. A rule that several features read stays at the
`Domain` root." Without it, the existing "with the types already there"
wording still points a new file to the matching folder.
