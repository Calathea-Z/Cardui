import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getLivingPage } from "@/lib/api/server/living";
import type { LivingPageDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

/**
 * An empty living page used when the load fails.
 * The banner carries the error. The lists stay empty.
 */
export function emptyLivingPage(): LivingPageDto {
  return {
    planningCurrency: "USD",
    contributions: [],
    unassignedIncome: [],
    livingSpending: null,
    accounts: [],
    gap: {
      sharedMonthly: 0,
      billsMonthly: 0,
      minimumsMonthly: 0,
      livingSpendingMonthly: 0,
      shortfall: 0,
      leftOut: [],
    },
  };
}

/**
 * Loads the living page.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadLivingPage(): Promise<PageLoadState<LivingPageDto>> {
  const page = await safeApiCall(getLivingPage, emptyLivingPage());
  return {
    data: page.data,
    error: firstApiError(page),
  };
}
