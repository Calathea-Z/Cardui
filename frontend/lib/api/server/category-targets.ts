import type { CategoryTargetMonthDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/category-targets
 * Loads the household's current month of targets, spent, and remaining.
 * A month that has not been started is a preview and is not saved.
 */
export async function getCategoryTargets(): Promise<CategoryTargetMonthDto> {
  const response = await serverClient.get<CategoryTargetMonthDto>(
    "/api/category-targets",
  );
  return response.data;
}
