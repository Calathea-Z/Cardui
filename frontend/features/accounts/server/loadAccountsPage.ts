import {
  emptyAccountSummary,
  emptyPlaidItems,
  getAccountsSummary,
  getPlaidItems,
  safeApiCall,
  type AccountSummaryDto,
  type PlaidItemDto,
} from "@/lib/api";

export type AccountsPageData = {
  summary: AccountSummaryDto;
  plaidItems: PlaidItemDto[];
};

export async function loadAccountsPage(): Promise<AccountsPageData> {
  const [summaryResult, plaidItemsResult] = await Promise.all([
    safeApiCall(getAccountsSummary, emptyAccountSummary()),
    safeApiCall(getPlaidItems, emptyPlaidItems()),
  ]);

  return {
    summary: summaryResult.data,
    plaidItems: plaidItemsResult.data,
  };
}
