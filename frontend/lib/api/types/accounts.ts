import type {
  FinancialRecordProvenance,
  FinancialRecordSource,
} from "./origin";

/**
 * Account types a household can create by hand, in form order.
 * Plaid can still send other types on `AccountDto.type`.
 */
export const MANUAL_ACCOUNT_TYPES = [
  "depository",
  "investment",
  "credit",
  "loan",
] as const;

/** One account type from `MANUAL_ACCOUNT_TYPES`. */
export type ManualAccountType = (typeof MANUAL_ACCOUNT_TYPES)[number];

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
  source: FinancialRecordSource;
  provenance: FinancialRecordProvenance;
  openingBalance: number;
  openingBalanceDate: string | null;
  archivedAt: string | null;
};

/**
 * Fields for a new manual account.
 * `openingBalance` is the balance already there. It is not income or a purchase.
 * `type` is one of the manual account types.
 */
export type CreateManualAccountDto = {
  name: string;
  type: ManualAccountType;
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
