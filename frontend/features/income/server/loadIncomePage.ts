import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getFinancialProfile } from "@/lib/api/server/households";
import { getIncomeSources } from "@/lib/api/server/income";
import type { PageLoadState } from "@/lib/pageLoadState";
import { emptyFinancialProfile } from "@/features/household/server/loadHouseholdPage";
import type { IncomePageData } from "../incomePageData";

/**
 * Loads income sources and household contributors for the income page.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadIncomePage(): Promise<PageLoadState<IncomePageData>> {
  const sources = await safeApiCall(getIncomeSources, []);
  const profile = await safeApiCall(
    getFinancialProfile,
    emptyFinancialProfile(),
  );

  return {
    data: {
      sources: sources.data,
      contributors: profile.data.contributors,
      planningCurrency: profile.data.planningCurrency,
    },
    error: firstApiError(sources, profile),
  };
}
