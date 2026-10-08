import { safeApiCall } from "@/lib/api/server";
import { getPlanRecovery } from "@/lib/api/server/plan";
import type {
  CashFlowRecoveryPathDto,
  CashFlowRecoveryReportDto,
} from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

/**
 * Loads the cash-flow recovery report.
 * A failed request still returns a page, with the error for the banner.
 */
export async function loadPlanPage(): Promise<
  PageLoadState<CashFlowRecoveryReportDto>
> {
  return safeApiCall(getPlanRecovery, emptyCashFlowRecovery());
}

/**
 * Empty recovery used when the plan request fails.
 * hasDebts stays false so a failed load is not shown as a household with no debts.
 */
function emptyCashFlowRecovery(): CashFlowRecoveryReportDto {
  return {
    planningCurrency: "USD",
    monthlyExtra: 0,
    reclaimAmount: 0,
    rollover: emptyPath("Rollover"),
    reclaim: emptyPath("Reclaim"),
    reclaimAll: emptyPath("ReclaimAll"),
    excludedCurrencies: [],
    assumptions: [],
    hasDebts: false,
  };
}

/**
 * An empty path. The explanation is blank because the report never arrived.
 */
function emptyPath(
  kind: CashFlowRecoveryPathDto["kind"],
): CashFlowRecoveryPathDto {
  return {
    kind,
    steps: [],
    startingObligation: null,
    remainingObligation: null,
    unknownRemaining: 0,
    breathingRoom: 0,
    releasedExtra: 0,
    recurringRoom: 0,
    explanation: "",
  };
}
