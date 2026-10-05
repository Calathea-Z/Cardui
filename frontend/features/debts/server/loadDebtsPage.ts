import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getAccounts } from "@/lib/api/server/accounts";
import { getDebts } from "@/lib/api/server/debts";
import { getFinancialProfile } from "@/lib/api/server/households";
import type { PageLoadState } from "@/lib/pageLoadState";
import { emptyFinancialProfile } from "@/features/household/server/loadHouseholdPage";
import type { DebtsPageData } from "../debtPageData";

/**
 * Loads debts, open accounts, and the planning currency for the debts page.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadDebtsPage(): Promise<PageLoadState<DebtsPageData>> {
  const [debts, accounts, profile] = await Promise.all([
    safeApiCall(getDebts, []),
    safeApiCall(getAccounts, []),
    safeApiCall(getFinancialProfile, emptyFinancialProfile()),
  ]);

  return {
    data: {
      debts: debts.data,
      accounts: accounts.data,
      planningCurrency: profile.data.planningCurrency,
    },
    error: firstApiError(debts, accounts, profile),
  };
}
