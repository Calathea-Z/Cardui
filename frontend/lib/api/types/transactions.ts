import type {
  FinancialRecordProvenance,
  FinancialRecordSource,
} from "./origin";

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
  source: FinancialRecordSource;
  provenance: FinancialRecordProvenance;
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
