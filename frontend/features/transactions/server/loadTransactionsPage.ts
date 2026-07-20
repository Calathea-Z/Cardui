import {
  emptyAccounts,
  emptyCategories,
  emptyPagedResult,
  firstApiError,
  getAccounts,
  getCategories,
  getTransactions,
  safeApiCall,
} from "@/lib/api/server";
import type {
  AccountDto,
  CategoryDto,
  PagedResultDto,
  TransactionDto,
} from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

const DEFAULT_PAGE_SIZE = 50;

export type TransactionsPageData = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  accounts: AccountDto[];
  pageSize: number;
};

export async function loadTransactionsPage(): Promise<
  PageLoadState<TransactionsPageData>
> {
  const [transactionsResult, categoriesResult, accountsResult] =
    await Promise.all([
      safeApiCall(
        () =>
          getTransactions({
            page: 1,
            pageSize: DEFAULT_PAGE_SIZE,
          }),
        emptyPagedResult(1, DEFAULT_PAGE_SIZE),
      ),
      safeApiCall(getCategories, emptyCategories()),
      safeApiCall(getAccounts, emptyAccounts()),
    ]);

  return {
    data: {
      initialTransactionsPage: transactionsResult.data,
      categories: categoriesResult.data,
      accounts: accountsResult.data,
      pageSize: DEFAULT_PAGE_SIZE,
    },
    error: firstApiError(transactionsResult, categoriesResult, accountsResult),
  };
}
