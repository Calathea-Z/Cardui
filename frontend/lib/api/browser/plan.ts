import { browserClient } from "../browser-client";
import type { PlanRecoveryDto, PlanScenarioQuery } from "../types";
import { isCurrentPlanRecovery } from "../validatePlanRecovery";

/**
 * GET /api/plan/recovery
 * Loads the payoff and cash outlook for a temporary preview.
 * Omitted spending and floor amounts keep the saved figures. Nothing is saved.
 */
export async function getPlanRecovery(
  scenario: PlanScenarioQuery,
): Promise<PlanRecoveryDto> {
  const response = await browserClient.get<unknown>("/api/plan/recovery", {
    params: {
      monthlyExtra: scenario.monthlyExtra,
      incomeLossMonths: scenario.incomeLossMonths,
      windfallAmount: scenario.windfallAmount,
      windfallOn: scenario.windfallOn ?? undefined,
      livingSpendingMonthly: scenario.livingSpendingMonthly ?? undefined,
      cashFloor: scenario.cashFloor ?? undefined,
    },
  });
  if (!isCurrentPlanRecovery(response.data)) {
    throw new Error(
      "Plan data is temporarily out of date. Refresh after the API finishes updating.",
    );
  }

  return response.data;
}
