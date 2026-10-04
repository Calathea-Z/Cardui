import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getFinancialProfile } from "@/lib/api/server/households";
import type { FinancialProfileDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

export function emptyFinancialProfile(): FinancialProfileDto {
  return {
    planningCurrency: "USD",
    timeZoneId: "America/Denver",
    contributors: [],
  };
}

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
