import {
  emptyAccountSummary,
  emptyPlaidItems,
  firstApiError,
  getAccountsSummary,
  getPlaidItems,
  safeApiCall,
  type AccountSummaryDto,
  type PlaidItemDto,
} from "@/lib/api";
import type { PageLoadState } from "@/lib/pageLoadState";

export type AccountsPageData = {
  summary: AccountSummaryDto;
  plaidItems: PlaidItemDto[];
};

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
