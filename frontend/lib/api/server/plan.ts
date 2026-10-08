import type { PlanRecoveryDto } from "../types";
import { isCurrentPlanRecovery } from "../validatePlanRecovery";
import { serverClient } from "../server-client";

/**
 * GET /api/plan/recovery
 * Loads the payoff on rollover and on keeping every freed payment, with each debt's balance over time.
 * This first load uses no extra. A tried amount is requested in the browser and is not saved.
 */
export async function getPlanRecovery(): Promise<PlanRecoveryDto> {
  const response = await serverClient.get<unknown>("/api/plan/recovery");
  if (!isCurrentPlanRecovery(response.data)) {
    throw new Error(
      "Plan data is temporarily out of date. Refresh after the API finishes updating.",
    );
  }

  return response.data;
}
