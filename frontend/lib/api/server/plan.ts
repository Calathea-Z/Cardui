import type { PlanRecoveryDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/plan/recovery
 * Loads the payoff on rollover and on keeping every freed payment, with each debt's balance over time.
 * This first load uses no extra. A tried amount is requested in the browser and is not saved.
 */
export async function getPlanRecovery(): Promise<PlanRecoveryDto> {
  const response =
    await serverClient.get<PlanRecoveryDto>("/api/plan/recovery");
  return response.data;
}
