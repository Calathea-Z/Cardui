import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getAllocationPage } from "@/lib/api/server/allocation";
import type { AllocationPageDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

/**
 * An unsaved allocation page used when the load fails.
 * The banner carries the error. Both rules stay at zero.
 */
export function emptyAllocationPage(): AllocationPageDto {
  return {
    planningCurrency: "USD",
    isSaved: false,
    variableIncome: { debtPercent: 0, savingsPercent: 0, cashPercent: 100 },
    windfall: { debtPercent: 0, savingsPercent: 0, cashPercent: 100 },
    variableIncomeReviews: [],
  };
}

/**
 * Loads the allocation page.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadAllocationPage(): Promise<
  PageLoadState<AllocationPageDto>
> {
  const page = await safeApiCall(getAllocationPage, emptyAllocationPage());
  return {
    data: page.data,
    error: firstApiError(page),
  };
}
