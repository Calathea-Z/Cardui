import type { CategoryTargetMonthDto } from "@/lib/api/types";

/**
 * Data the targets page renders.
 * `month` is null when the load failed. The banner carries the error.
 */
export type TargetsPageData = {
  month: CategoryTargetMonthDto | null;
};
