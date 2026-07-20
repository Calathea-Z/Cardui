import {
  emptyAccounts,
  emptyCategories,
  emptyPagedResult,
  getAccounts,
  getCategories,
  getTransactions,
  safeApiCall,
  type AccountDto,
  type CategoryDto,
  type PagedResultDto,
  type TransactionDto,
} from "@/lib/api";

const DEFAULT_PAGE_SIZE = 50;

export type TransactionsPageData = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  accounts: AccountDto[];
  pageSize: number;
};

export async function loadTransactionsPage(): Promise<TransactionsPageData> {
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
    initialTransactionsPage: transactionsResult.data,
    categories: categoriesResult.data,
    accounts: accountsResult.data,
    pageSize: DEFAULT_PAGE_SIZE,
  };
}
