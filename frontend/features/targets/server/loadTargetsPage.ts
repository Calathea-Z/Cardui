import { firstApiError, safeApiCall } from "@/lib/api/server";
import { getCategoryTargets } from "@/lib/api/server/category-targets";
import type { CategoryTargetMonthDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";
import type { TargetsPageData } from "../targetPageData";

/**
 * Loads the household's current month of category targets.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadTargetsPage(): Promise<
  PageLoadState<TargetsPageData>
> {
  const month = await safeApiCall<CategoryTargetMonthDto | null>(
    getCategoryTargets,
    null,
  );

  return {
    data: {
      month: month.data,
    },
    error: firstApiError(month),
  };
}
