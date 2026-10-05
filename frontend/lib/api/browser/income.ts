import { browserClient } from "../browser-client";
import type { IncomeSourceDto, UpsertIncomeSourceDto } from "../types";

/**
 * POST /api/income-sources
 * Records an income source for the signed-in household.
 * The amount is one take-home payment.
 */
export async function createIncomeSource(
  dto: UpsertIncomeSourceDto,
): Promise<IncomeSourceDto> {
  const response = await browserClient.post<IncomeSourceDto>(
    "/api/income-sources",
    dto,
  );
  return response.data;
}

/**
 * PUT /api/income-sources/{id}
 * Updates an income source. The stored currency stays.
 */
export async function updateIncomeSource(
  id: string,
  dto: UpsertIncomeSourceDto,
): Promise<IncomeSourceDto> {
  const response = await browserClient.put<IncomeSourceDto>(
    `/api/income-sources/${id}`,
    dto,
  );
  return response.data;
}

/**
 * DELETE /api/income-sources/{id}
 * Deletes an income source. Balances stay unchanged.
 */
export async function deleteIncomeSource(id: string): Promise<void> {
  await browserClient.delete(`/api/income-sources/${id}`);
}
