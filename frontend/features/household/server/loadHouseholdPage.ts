import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getFinancialProfile } from "@/lib/api/server/households";
import type { FinancialProfileDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

/**
 * Profile used when the household request fails.
 * Planning currency is USD and the time zone is America/Denver.
 */
export function emptyFinancialProfile(): FinancialProfileDto {
  return {
    planningCurrency: "USD",
    timeZoneId: "America/Denver",
    contributors: [],
  };
}

/**
 * Loads the household financial profile for the page.
 * A failed request returns the empty profile and the error message.
 */
export async function loadHouseholdPage(): Promise<
  PageLoadState<FinancialProfileDto>
> {
  const profile = await safeApiCall(
    getFinancialProfile,
    emptyFinancialProfile(),
  );

  return {
    data: profile.data,
    error: firstApiError(profile),
  };
}
