import type { PlanRecoveryDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/plan/recovery
 * Loads the payoff on rollover and on keeping every freed payment, with each debt's balance over time.
 * The order is highest interest first and extra is zero, because those choices are not stored yet.
 */
export async function getPlanRecovery(): Promise<PlanRecoveryDto> {
  const response =
    await serverClient.get<PlanRecoveryDto>("/api/plan/recovery");
  return response.data;
}
