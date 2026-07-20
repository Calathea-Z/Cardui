import { getApiErrorMessage, type ApiError } from "./client";
import type {
  AccountDto,
  AccountSummaryDto,
  CategoryDto,
  DashboardSummaryDto,
  PagedResultDto,
} from "./types";
import type { PlaidItemDto } from "./plaid";

export type SafeApiResult<T> = {
  data: T;
  error: string | null;
};

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

export function emptyAccountSummary(): AccountSummaryDto {
  return {
    netWorth: 0,
    history: [],
    groups: [
      { key: "net-worth", name: "Net Worth", total: 0, accounts: [] },
      { key: "cash", name: "Cash", total: 0, accounts: [] },
      { key: "investments", name: "Investments", total: 0, accounts: [] },
      { key: "credit-cards", name: "Credit Cards", total: 0, accounts: [] },
      { key: "loans", name: "Loans", total: 0, accounts: [] },
    ],
  };
}

export function emptyDashboardSummary(): DashboardSummaryDto {
  return {
    cashBalance: 0,
    creditCardBalance: 0,
    netWorth: 0,
    monthlyIncome: 0,
    monthlySpending: 0,
    recentTransactions: [],
    spendingByCategory: [],
  };
}

export function emptyAccounts(): AccountDto[] {
  return [];
}

export function emptyCategories(): CategoryDto[] {
  return [];
}

export function emptyPlaidItems(): PlaidItemDto[] {
  return [];
}

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

/** Narrow unknown rejection into ApiError shape when useful for callers. */
export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === "object" &&
    error !== null &&
    "message" in error &&
    typeof (error as ApiError).message === "string"
  );
}
