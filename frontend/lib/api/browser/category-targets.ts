import { browserClient } from "../browser-client";
import type { CategoryTargetMonthDto, UpsertCategoryTargetDto } from "../types";

/**
 * GET /api/category-targets
 * Loads targets, spent, and remaining for one month.
 * Omitting the year and month uses the household's current month.
 * A month that has not been started is a preview and is not saved.
 */
export async function getCategoryTargets(
  year?: number,
  month?: number,
): Promise<CategoryTargetMonthDto> {
  const response = await browserClient.get<CategoryTargetMonthDto>(
    "/api/category-targets",
    {
      params:
        year === undefined || month === undefined ? undefined : { year, month },
    },
  );
  return response.data;
}

/**
 * POST /api/category-targets/copy-forward
 * Starts the month with the nearest earlier month's targets and rollover choices.
 * Leftover money is not copied.
 */
export async function copyCategoryTargets(
  year: number,
  month: number,
): Promise<CategoryTargetMonthDto> {
  const response = await browserClient.post<CategoryTargetMonthDto>(
    "/api/category-targets/copy-forward",
    { year, month },
  );
  return response.data;
}

/**
 * POST /api/category-targets/start-fresh
 * Starts the month with no targets so a later visit does not copy an earlier month.
 */
export async function startFreshCategoryTargets(
  year: number,
  month: number,
): Promise<CategoryTargetMonthDto> {
  const response = await browserClient.post<CategoryTargetMonthDto>(
    "/api/category-targets/start-fresh",
    { year, month },
  );
  return response.data;
}

/**
 * PUT /api/category-targets/{categoryId}
 * Saves one category's target and rollover choice.
 * The first save in a month also copies the other categories from the nearest earlier month.
 */
export async function saveCategoryTarget(
  categoryId: string,
  dto: UpsertCategoryTargetDto,
): Promise<CategoryTargetMonthDto> {
  const response = await browserClient.put<CategoryTargetMonthDto>(
    `/api/category-targets/${categoryId}`,
    dto,
  );
  return response.data;
}

/**
 * DELETE /api/category-targets/{categoryId}
 * Removes one category's target. The month stays started.
 */
export async function clearCategoryTarget(
  categoryId: string,
  year: number,
  month: number,
): Promise<CategoryTargetMonthDto> {
  const response = await browserClient.delete<CategoryTargetMonthDto>(
    `/api/category-targets/${categoryId}`,
    { params: { year, month } },
  );
  return response.data;
}
