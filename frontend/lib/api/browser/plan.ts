import { browserClient } from "../browser-client";
import type { PlanRecoveryDto } from "../types";

/**
 * GET /api/plan/recovery
 * Loads the payoff and cash outlook with a shared extra tried for this request.
 * Zero is the minimums-only plan. The amount is not saved.
 */
export async function getPlanRecovery(
  monthlyExtra: number,
): Promise<PlanRecoveryDto> {
  const response = await browserClient.get<PlanRecoveryDto>(
    "/api/plan/recovery",
    { params: { monthlyExtra } },
  );
  return response.data;
}
