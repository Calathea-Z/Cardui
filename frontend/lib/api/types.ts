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
  isActive: boolean;
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
};

export type TransactionQueryDto = {
  search?: string;
  accountId?: string;
  categoryId?: string;
  from?: string;
  to?: string;
  pending?: boolean;
  page?: number;
  pageSize?: number;
};

export type PagedResultDto<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
};

export type UpdateTransactionCategoryDto = {
  categoryId: string | null;
};

export type UpdateTransactionDetailsDto = {
  date: string;
  categoryId: string | null;
  notes: string | null;
};

export type SpendingByCategoryDto = {
  categoryId: string | null;
  categoryName: string;
  color: string | null;
  amount: number;
};

export type DashboardSummaryDto = {
  cashBalance: number;
  creditCardBalance: number;
  netWorth: number;
  monthlyIncome: number;
  monthlySpending: number;
  recentTransactions: TransactionDto[];
  spendingByCategory: SpendingByCategoryDto[];
};

export type CategoryDto = {
  id: string;
  name: string;
  key: string;
  parentCategoryId: string | null;
  color: string | null;
  icon: string | null;
  isSystem: boolean;
};

export type CreateCategoryDto = {
  name: string;
  parentCategoryId?: string | null;
  color?: string | null;
  icon?: string | null;
};

export type UpdateCategoryDto = {
  name: string;
  parentCategoryId?: string | null;
  color?: string | null;
  icon?: string | null;
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

export type AccountSummaryDto = {
  netWorth: number;
  groups: AccountGroupDto[];
  history: AccountBalanceHistoryPointDto[];
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
  nextCursor: string | null;
};

export type SyncPlaidItemResponseDto = {
  plaidItemId: string;
  transactions: SyncTransactionsResponseDto;
};

export type ApiHealthDto = {
  status: string;
};
