/**
 * Request and response shapes for the Cardui API.
 * Money amounts use the stored sign: positive is money out, negative is money in.
 */

/**
 * One household account.
 * `plaidItemId` is null when the account was created in Cardui.
 * `countsInPlanningTotals` is false when the account's currency is left out of totals.
 * `openingBalance` is the balance already in the account when it was added. It is not a transaction.
 * `source` is Plaid, Manual, or Csv. `provenance` is how the row was created, such as PlaidSync or ManualEntry.
 */
export type AccountDto = {
  id: string;
  plaidItemId: string | null;
  name: string;
  officialName: string | null;
  type: string;
  subtype: string | null;
  mask: string | null;
  currentBalance: number;
  availableBalance: number | null;
  isoCurrencyCode: string | null;
  countsInPlanningTotals: boolean;
  isActive: boolean;
  source: string;
  provenance: string;
  openingBalance: number;
  openingBalanceDate: string | null;
  archivedAt: string | null;
};

export type TransactionAccountDto = {
  id: string;
  name: string;
  type: string;
  subtype: string | null;
};

export type TransactionCategoryDto = {
  id: string;
  name: string;
  key: string | null;
  color: string | null;
  icon: string | null;
};

/**
 * One transaction.
 * A positive `amount` is money out. A negative `amount` is money in.
 * Pending rows stay visible but stay out of income and spending until they post.
 * `provenance` distinguishes Plaid sync, manual entry, CSV import, and balance reconciliation.
 */
export type TransactionDto = {
  id: string;
  date: string;
  authorizedDate: string | null;
  name: string;
  merchantName: string | null;
  amount: number;
  isoCurrencyCode: string | null;
  pending: boolean;
  account: TransactionAccountDto;
  category: TransactionCategoryDto | null;
  notes: string | null;
  source: string;
  provenance: string;
  archivedAt: string | null;
};

/**
 * Filters for a transaction page.
 * `archived` lists hidden rows instead of the active list.
 */
export type TransactionQueryDto = {
  search?: string;
  accountId?: string;
  categoryId?: string;
  from?: string;
  to?: string;
  pending?: boolean;
  archived?: boolean;
  page?: number;
  pageSize?: number;
};

/**
 * One page of a list, with enough fields for the pager to know what comes next.
 */
export type PagedResultDto<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
};

/**
 * Edits saved on an existing transaction.
 * Date, category, and notes are always included. Name and amount are included when that entry can be edited.
 */
export type UpdateTransactionDetailsDto = {
  date: string;
  categoryId: string | null;
  notes: string | null;
  name?: string | null;
  amount?: number | null;
  pending?: boolean | null;
};

/**
 * Fields for a new manual account.
 * `openingBalance` is the balance already there. It is not income or a purchase.
 */
export type CreateManualAccountDto = {
  name: string;
  type: string;
  subtype?: string | null;
  mask?: string | null;
  isoCurrencyCode?: string | null;
  openingBalance: number;
  openingBalanceDate: string;
};

/** Fields saved when a manual account is edited. */
export type UpdateManualAccountDto = CreateManualAccountDto;

/**
 * Statement balance the household wants the account to match on a date.
 */
export type ReconcileAccountBalanceDto = {
  asOfDate: string;
  statementBalance: number;
};

/**
 * Result of matching an account to a statement balance.
 * `adjustment` is the difference that was recorded. `adjustmentTransactionId` is null when the balances already matched.
 */
export type BalanceReconciliationResultDto = {
  account: AccountDto;
  calculatedBalance: number;
  statementBalance: number;
  adjustment: number;
  adjustmentTransactionId: string | null;
};

/**
 * Fields for a transaction entered by hand.
 * `amount` uses the stored sign: positive is money out, negative is money in.
 */
export type CreateManualTransactionDto = {
  accountId: string;
  date: string;
  name: string;
  amount: number;
  categoryId?: string | null;
  notes?: string | null;
  pending?: boolean;
};

/**
 * How merchant history groups transactions on the chart.
 */
export type MerchantHistoryGranularity = "monthly" | "quarterly" | "yearly";

export type MerchantHistoryPeriodDto = {
  key: string;
  label: string;
  shortLabel: string;
  totalAmount: number;
  transactionCount: number;
};

export type MerchantHistoryDto = {
  displayName: string;
  totalTransactionCount: number;
  granularity: MerchantHistoryGranularity;
  selectedPeriodKey: string;
  periods: MerchantHistoryPeriodDto[];
  transactions: TransactionDto[];
};

export type SpendingByCategoryDto = {
  categoryId: string | null;
  categoryName: string;
  color: string | null;
  amount: number;
};

/**
 * Totals for the dashboard's current period.
 * Excluded counts are accounts or transactions left out because their currency is not the planning currency.
 */
export type DashboardSummaryDto = {
  periodStart: string;
  periodEnd: string;
  cashBalance: number;
  creditCardBalance: number;
  netWorth: number;
  monthlyIncome: number;
  monthlySpending: number;
  planningCurrency: string;
  excludedAccountCount: number;
  excludedTransactionCount: number;
  excludedCurrencies: string[];
  recentTransactions: TransactionDto[];
  spendingByCategory: SpendingByCategoryDto[];
};

/**
 * A category the household can assign to transactions.
 * `isSystem` marks a built-in category. `key` is the stable id used for emoji and activity rules.
 */
export type CategoryDto = {
  id: string;
  name: string;
  key: string;
  subGroupId: string;
  color: string | null;
  icon: string | null;
  isSystem: boolean;
};

export type CreateCategoryDto = {
  name: string;
  subGroupId: string;
  color?: string | null;
  icon?: string | null;
};

export type UpdateCategoryDto = {
  name: string;
  subGroupId: string;
  color?: string | null;
  icon?: string | null;
};

export type GroupDto = {
  id: string;
  key: string;
  name: string;
  sortOrder: number;
};

export type SubGroupDto = {
  id: string;
  groupId: string;
  key: string;
  name: string;
  isSystem: boolean;
  sortOrder: number;
};

export type AccountGroupDto = {
  key: string;
  name: string;
  total: number;
  accounts: AccountDto[];
};

export type AccountBalanceHistoryPointDto = {
  date: string;
  netWorth: number;
  cash: number;
  investments: number;
  creditCards: number;
  loans: number;
};

/**
 * Net worth, balance history, and account groups for the accounts page.
 * Excluded accounts are omitted from the totals because their currency is not the planning currency.
 */
export type AccountSummaryDto = {
  netWorth: number;
  planningCurrency: string;
  excludedAccountCount: number;
  excludedCurrencies: string[];
  groups: AccountGroupDto[];
  history: AccountBalanceHistoryPointDto[];
  archivedAccounts: AccountDto[];
};

export type CreatePlaidLinkTokenResponse = {
  linkToken: string;
};

export type ExchangePlaidPublicTokenRequest = {
  publicToken: string;
  institutionId?: string;
  institutionName?: string;
};

export type ExchangePlaidPublicTokenResponse = {
  plaidItemId: string;
};

/**
 * One connected institution.
 * Sync timestamps say whether the last pull is running, finished, or failed.
 */
export type PlaidItemDto = {
  id: string;
  institutionId: string | null;
  institutionName: string | null;
  createdAt: string;
  updatedAt: string;
  lastTransactionsSyncedAt: string | null;
  lastSyncStartedAt: string | null;
  lastSyncCompletedAt: string | null;
  lastSyncFailedAt: string | null;
  lastSyncError: string | null;
};

export type SyncTransactionsResponseDto = {
  added: number;
  modified: number;
  removed: number;
};

export type SyncPlaidItemResponseDto = {
  plaidItemId: string;
  transactions: SyncTransactionsResponseDto;
};

export type HouseholdDto = {
  id: string;
  displayName: string;
  createdAt: string;
};

/**
 * A person included in the household profile.
 * `isVisible` is the Shown flag. Hiding a contributor leaves balances unchanged.
 */
export type HouseholdContributorDto = {
  id: string;
  name: string;
  isVisible: boolean;
};

export type FinancialProfileDto = {
  planningCurrency: string;
  timeZoneId: string;
  contributors: HouseholdContributorDto[];
};

export type UpdateFinancialProfileDto = {
  planningCurrency: string;
  timeZoneId: string;
};

export type UpsertHouseholdContributorDto = {
  name: string;
  isVisible: boolean;
};

/**
 * Column indexes suggested for a CSV.
 * Null means that column was not found. `amountSign` is PositiveOut or PositiveIn. `dateOrder` is MonthFirst or DayFirst.
 */
export type TransactionImportSuggestedMapDto = {
  dateColumn: number | null;
  nameColumn: number | null;
  amountColumn: number | null;
  debitColumn: number | null;
  creditColumn: number | null;
  categoryColumn: number | null;
  notesColumn: number | null;
  amountSign: string;
  dateOrder: string;
};

export type TransactionImportInspectDto = {
  fileName: string;
  headers: string[];
  sampleRows: string[][];
  dataRowCount: number;
  suggested: TransactionImportSuggestedMapDto;
};

/**
 * One CSV row after mapping.
 * Ready rows can be imported. Duplicate rows are likely already saved. Error rows need a fix and cannot be imported.
 */
export type TransactionImportPreviewRowDto = {
  lineNumber: number;
  date: string | null;
  name: string | null;
  amount: number | null;
  categoryName: string | null;
  status: "Ready" | "Duplicate" | "Error";
  message: string | null;
};

export type TransactionImportPreviewDto = {
  readyCount: number;
  duplicateCount: number;
  errorCount: number;
  rows: TransactionImportPreviewRowDto[];
};

/**
 * A completed CSV import.
 * `undoneAt` is set after the batch is archived.
 */
export type TransactionImportBatchDto = {
  id: string;
  accountId: string;
  accountName: string;
  fileName: string;
  importedCount: number;
  createdAt: string;
  undoneAt: string | null;
};
