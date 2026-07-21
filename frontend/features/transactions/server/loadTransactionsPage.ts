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

const DEFAULT_PAGE_SIZE = 50;

export type TransactionsPageData = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  accounts: AccountDto[];
  pageSize: number;
};

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
