import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getAccounts } from "@/lib/api/server/accounts";
import { getFinancialProfile } from "@/lib/api/server/households";
import {
  getObligationSuggestions,
  getObligations,
} from "@/lib/api/server/obligations";
import type { PageLoadState } from "@/lib/pageLoadState";
import { emptyFinancialProfile } from "@/features/household/server/loadHouseholdPage";
import type { BillsPageData } from "../billPageData";

/**
 * Loads bills, open accounts, and the planning currency for the bills page.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadBillsPage(): Promise<PageLoadState<BillsPageData>> {
  const [obligations, suggestions, accounts, profile] = await Promise.all([
    safeApiCall(getObligations, []),
    safeApiCall(getObligationSuggestions, []),
    safeApiCall(getAccounts, []),
    safeApiCall(getFinancialProfile, emptyFinancialProfile()),
  ]);

  return {
    data: {
      obligations: obligations.data,
      suggestions: suggestions.data,
      accounts: accounts.data,
      planningCurrency: profile.data.planningCurrency,
    },
    error: firstApiError(obligations, suggestions, accounts, profile),
  };
}
