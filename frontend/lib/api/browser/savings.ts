import { browserClient } from "../browser-client";
import type { SavingsGoalDto, UpsertSavingsGoalDto } from "../types";

/**
 * POST /api/savings-goals
 * Records a goal for the signed-in household.
 * Saving it does not move money or create a transaction.
 */
export async function createSavingsGoal(
  dto: UpsertSavingsGoalDto,
): Promise<SavingsGoalDto> {
  const response = await browserClient.post<SavingsGoalDto>(
    "/api/savings-goals",
    dto,
  );
  return response.data;
}

/**
 * PUT /api/savings-goals/{id}
 * Updates a goal. The kind and the stored currency stay.
 */
export async function updateSavingsGoal(
  id: string,
  dto: UpsertSavingsGoalDto,
): Promise<SavingsGoalDto> {
  const response = await browserClient.put<SavingsGoalDto>(
    `/api/savings-goals/${id}`,
    dto,
  );
  return response.data;
}

/**
 * DELETE /api/savings-goals/{id}
 * Deletes a goal. The account and its balance stay unchanged.
 */
export async function deleteSavingsGoal(id: string): Promise<void> {
  await browserClient.delete(`/api/savings-goals/${id}`);
}
