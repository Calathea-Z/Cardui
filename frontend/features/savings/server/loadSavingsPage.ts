import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getFinancialProfile } from "@/lib/api/server/households";
import { getSavingsAccounts, getSavingsGoals } from "@/lib/api/server/savings";
import type { PageLoadState } from "@/lib/pageLoadState";
import { emptyFinancialProfile } from "@/features/household/server/loadHouseholdPage";
import type { SavingsPageData } from "../savingsPageData";

/**
 * Loads savings goals, cash accounts, and the planning currency.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadSavingsPage(): Promise<
  PageLoadState<SavingsPageData>
> {
  const [goals, accounts, profile] = await Promise.all([
    safeApiCall(getSavingsGoals, []),
    safeApiCall(getSavingsAccounts, []),
    safeApiCall(getFinancialProfile, emptyFinancialProfile()),
  ]);

  return {
    data: {
      goals: goals.data,
      accounts: accounts.data,
      planningCurrency: profile.data.planningCurrency,
    },
    error: firstApiError(goals, accounts, profile),
  };
}
