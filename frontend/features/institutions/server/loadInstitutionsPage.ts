import {
  emptyAccounts,
  emptyPlaidItems,
  getAccounts,
  getPlaidItems,
  safeApiCall,
  type AccountDto,
  type PlaidItemDto,
} from "@/lib/api";

export type InstitutionsPageData = {
  initialItems: PlaidItemDto[];
  accounts: AccountDto[];
};

export async function loadInstitutionsPage(): Promise<InstitutionsPageData> {
  const [plaidItemsResult, accountsResult] = await Promise.all([
    safeApiCall(getPlaidItems, emptyPlaidItems()),
    safeApiCall(getAccounts, emptyAccounts()),
  ]);

  return {
    initialItems: plaidItemsResult.data,
    accounts: accountsResult.data,
  };
}
