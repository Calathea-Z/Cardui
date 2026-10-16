import { browserClient } from "../browser-client";
import type {
  DecisionPlanDto,
  PayoffRolloverKind,
  PlanScenarioQuery,
  PlanVersionDto,
  SavePlanVersionDto,
} from "../types";

/**
 * GET /api/plan/decision
 * Builds the decision plan for the current facts and the selected scenario.
 * Nothing is saved.
 */
export async function getPlanDecision(
  scenario: PlanScenarioQuery,
  kind: PayoffRolloverKind,
): Promise<DecisionPlanDto> {
  const response = await browserClient.get<DecisionPlanDto>(
    "/api/plan/decision",
    {
      params: {
        kind,
        monthlyExtra: scenario.monthlyExtra,
        incomeLossMonths: scenario.incomeLossMonths,
        windfallAmount: scenario.windfallAmount,
        windfallOn: scenario.windfallOn ?? undefined,
        livingSpendingMonthly: scenario.livingSpendingMonthly ?? undefined,
        cashFloor: scenario.cashFloor ?? undefined,
      },
    },
  );
  return response.data;
}

/**
 * GET /api/plan/versions
 * Lists the household's saved plan versions, newest first.
 */
export async function getPlanVersions(): Promise<PlanVersionDto[]> {
  const response =
    await browserClient.get<PlanVersionDto[]>("/api/plan/versions");
  return response.data;
}

/**
 * POST /api/plan/versions
 * Saves the current decision plan as a draft. No money is moved.
 */
export async function savePlanVersion(
  dto: SavePlanVersionDto,
): Promise<PlanVersionDto> {
  const response = await browserClient.post<PlanVersionDto>(
    "/api/plan/versions",
    dto,
  );
  return response.data;
}

/**
 * POST /api/plan/versions/{id}/accept
 * Accepts that draft and supersedes the previous accepted plan. No money is moved.
 */
export async function acceptPlanVersion(id: string): Promise<PlanVersionDto> {
  const response = await browserClient.post<PlanVersionDto>(
    `/api/plan/versions/${id}/accept`,
  );
  return response.data;
}
