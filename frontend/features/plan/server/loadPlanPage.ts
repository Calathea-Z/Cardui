import { safeApiCall } from "@/lib/api/server";
import { getPlanRecovery } from "@/lib/api/server/plan";
import type { PlanRecoveryDto, PlanRecoveryPathDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

/**
 * Loads the household's payoff paths for the Plan page.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadPlanPage(): Promise<PageLoadState<PlanRecoveryDto>> {
  return safeApiCall(getPlanRecovery, emptyPlanRecovery());
}

/**
 * Empty plan used when the request fails.
 * hasDebts stays false so a failed load is not shown as a household with no debts.
 */
function emptyPlanRecovery(): PlanRecoveryDto {
  return {
    planningCurrency: "USD",
    rollover: emptyPath("Rollover"),
    reclaimAll: emptyPath("ReclaimAll"),
    excludedCurrencies: [],
    missingBalance: [],
    hasDebts: false,
  };
}

/**
 * An empty path with no payoffs, debts, or balances.
 */
function emptyPath(kind: PlanRecoveryPathDto["kind"]): PlanRecoveryPathDto {
  return {
    kind,
    steps: [],
    startingObligation: null,
    remainingObligation: null,
    recurringRoom: 0,
    paidOffOn: null,
    totalInterest: 0,
    debts: [],
    balancePoints: [],
  };
}
