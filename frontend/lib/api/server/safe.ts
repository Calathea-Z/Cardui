import type { PageLoadState } from "@/lib/pageLoadState";
import { getApiErrorMessage } from "../errors";
import type {
  AccountDto,
  AccountSummaryDto,
  CategoryDto,
  DashboardSummaryDto,
  GroupDto,
  PagedResultDto,
  PlaidItemDto,
  SubGroupDto,
} from "../types";

/**
 * Page data plus the error to show when the API call failed.
 * The fallback data lets the page render instead of crashing.
 */
export type SafeApiResult<T> = PageLoadState<T>;

/**
 * Runs a server API call and returns fallback data when it fails.
 * The page can still render, with the error passed to the banner.
 */
export async function safeApiCall<T>(
  fn: () => Promise<T>,
  fallback: T,
): Promise<SafeApiResult<T>> {
  try {
    const data = await fn();
    return { data, error: null };
  } catch (error) {
    return {
      data: fallback,
      error: getApiErrorMessage(error, "Can't reach the API."),
    };
  }
}

/**
 * Returns the first error from a set of page loads.
 * One banner is enough when several calls on the same page fail.
 */
export function firstApiError(
  ...results: Array<{ error: string | null }>
): string | null {
  for (const result of results) {
    if (result.error) {
      return result.error;
    }
  }

  return null;
}

/**
 * Empty account summary used when the accounts API call fails.
 */
export function emptyAccountSummary(): AccountSummaryDto {
  return {
    netWorth: 0,
    planningCurrency: "USD",
    excludedAccountCount: 0,
    excludedCurrencies: [],
    history: [],
    groups: [
      { key: "net-worth", name: "Net Worth", total: 0, accounts: [] },
      { key: "cash", name: "Cash", total: 0, accounts: [] },
      { key: "investments", name: "Investments", total: 0, accounts: [] },
      { key: "credit-cards", name: "Credit Cards", total: 0, accounts: [] },
      { key: "loans", name: "Loans", total: 0, accounts: [] },
    ],
    archivedAccounts: [],
  };
}

/**
 * Empty dashboard summary used when the dashboard API call fails.
 */
export function emptyDashboardSummary(): DashboardSummaryDto {
  return {
    periodStart: "",
    periodEnd: "",
    cashBalance: 0,
    creditCardBalance: 0,
    netWorth: 0,
    monthlyIncome: 0,
    monthlySpending: 0,
    planningCurrency: "USD",
    excludedAccountCount: 0,
    excludedTransactionCount: 0,
    excludedCurrencies: [],
    recentTransactions: [],
    spendingByCategory: [],
  };
}

/** Empty account list used when that API call fails. */
export function emptyAccounts(): AccountDto[] {
  return [];
}

/** Empty category list used when that API call fails. */
export function emptyCategories(): CategoryDto[] {
  return [];
}

/** Empty group list used when that API call fails. */
export function emptyGroups(): GroupDto[] {
  return [];
}

/** Empty subgroup list used when that API call fails. */
export function emptySubGroups(): SubGroupDto[] {
  return [];
}

/** Empty institution list used when that API call fails. */
export function emptyPlaidItems(): PlaidItemDto[] {
  return [];
}

/**
 * Empty page of results used when a paged API call fails.
 * The requested page and page size are kept so the pager does not jump.
 */
export function emptyPagedResult<T>(
  page = 1,
  pageSize = 50,
): PagedResultDto<T> {
  return {
    items: [],
    page,
    pageSize,
    totalCount: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  };
}
