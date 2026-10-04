import {
  emptyAccountSummary,
  emptyPlaidItems,
  firstApiError,
  getAccountsSummary,
  getPlaidItems,
  safeApiCall,
} from "@/lib/api/server";
import type { AccountSummaryDto, PlaidItemDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

export type AccountsPageData = {
  summary: AccountSummaryDto;
  plaidItems: PlaidItemDto[];
};

/**
 * Loads the account summary and linked institutions together.
 * A failed call still returns empty fallback data, and the page keeps the first error.
 */
export async function loadAccountsPage(): Promise<
  PageLoadState<AccountsPageData>
> {
  const [summaryResult, plaidItemsResult] = await Promise.all([
    safeApiCall(getAccountsSummary, emptyAccountSummary()),
    safeApiCall(getPlaidItems, emptyPlaidItems()),
  ]);

  return {
    data: {
      summary: summaryResult.data,
      plaidItems: plaidItemsResult.data,
    },
    error: firstApiError(summaryResult, plaidItemsResult),
  };
}
