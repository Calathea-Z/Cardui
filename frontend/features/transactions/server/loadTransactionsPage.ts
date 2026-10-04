import {
  emptyAccounts,
  emptyCategories,
  emptyGroups,
  emptyPagedResult,
  emptySubGroups,
  firstApiError,
  getAccounts,
  getCategories,
  getGroups,
  getSubGroups,
  getTransactions,
  safeApiCall,
} from "@/lib/api/server";
import type {
  AccountDto,
  CategoryDto,
  GroupDto,
  PagedResultDto,
  SubGroupDto,
  TransactionDto,
} from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

/**
 * How many transactions the first page request asks for.
 */
const DEFAULT_PAGE_SIZE = 50;

/**
 * Everything the transactions page needs on first render.
 * Includes the first transaction page, categories, groups, subgroups, and accounts.
 */
export type TransactionsPageData = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  accounts: AccountDto[];
  pageSize: number;
};

/**
 * Loads the first transaction page and the lists the filters use.
 * A failed request keeps an empty stand-in so the page can still render, and the first error is returned with the data.
 */
export async function loadTransactionsPage(): Promise<
  PageLoadState<TransactionsPageData>
> {
  const [
    transactionsResult,
    categoriesResult,
    groupsResult,
    subGroupsResult,
    accountsResult,
  ] = await Promise.all([
    safeApiCall(
      () =>
        getTransactions({
          page: 1,
          pageSize: DEFAULT_PAGE_SIZE,
        }),
      emptyPagedResult(1, DEFAULT_PAGE_SIZE),
    ),
    safeApiCall(getCategories, emptyCategories()),
    safeApiCall(getGroups, emptyGroups()),
    safeApiCall(() => getSubGroups(), emptySubGroups()),
    safeApiCall(getAccounts, emptyAccounts()),
  ]);

  return {
    data: {
      initialTransactionsPage: transactionsResult.data,
      categories: categoriesResult.data,
      groups: groupsResult.data,
      subGroups: subGroupsResult.data,
      accounts: accountsResult.data,
      pageSize: DEFAULT_PAGE_SIZE,
    },
    error: firstApiError(
      transactionsResult,
      categoriesResult,
      groupsResult,
      subGroupsResult,
      accountsResult,
    ),
  };
}
