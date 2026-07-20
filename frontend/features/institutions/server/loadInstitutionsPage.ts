import {
  emptyAccounts,
  emptyPlaidItems,
  firstApiError,
  getAccounts,
  getPlaidItems,
  safeApiCall,
  type AccountDto,
  type PlaidItemDto,
} from "@/lib/api";
import type { PageLoadState } from "@/lib/pageLoadState";

export type InstitutionsPageData = {
  initialItems: PlaidItemDto[];
  accounts: AccountDto[];
};

export async function loadInstitutionsPage(): Promise<
  PageLoadState<InstitutionsPageData>
> {
  const [plaidItemsResult, accountsResult] = await Promise.all([
    safeApiCall(getPlaidItems, emptyPlaidItems()),
    safeApiCall(getAccounts, emptyAccounts()),
  ]);

  return {
    data: {
      initialItems: plaidItemsResult.data,
      accounts: accountsResult.data,
    },
    error: firstApiError(plaidItemsResult, accountsResult),
  };
}
