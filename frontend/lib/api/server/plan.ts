import type { CashFlowRecoveryReportDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/plan/recovery
 * Loads when each payoff removes a minimum, and the breathing room that follows.
 * The order is highest interest first and extra is zero, because those choices are not stored yet.
 */
export async function getPlanRecovery(): Promise<CashFlowRecoveryReportDto> {
  const response =
    await serverClient.get<CashFlowRecoveryReportDto>("/api/plan/recovery");
  return response.data;
}
