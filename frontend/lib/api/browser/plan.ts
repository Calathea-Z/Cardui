import { browserClient } from "../browser-client";
import type { PlanRecoveryDto } from "../types";
import { isCurrentPlanRecovery } from "../validatePlanRecovery";

/**
 * GET /api/plan/recovery
 * Loads the payoff and cash outlook with a shared extra tried for this request.
 * Zero is the minimums-only plan. The amount is not saved.
 */
export async function getPlanRecovery(
  monthlyExtra: number,
): Promise<PlanRecoveryDto> {
  const response = await browserClient.get<unknown>("/api/plan/recovery", {
    params: { monthlyExtra },
  });
  if (!isCurrentPlanRecovery(response.data)) {
    throw new Error(
      "Plan data is temporarily out of date. Refresh after the API finishes updating.",
    );
  }

  return response.data;
}
