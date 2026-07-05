export type AccountDto = {
    id: string;
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