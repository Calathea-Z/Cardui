import { safeApiCall } from "@/lib/api/server";
import { getPlanRecovery } from "@/lib/api/server/plan";
import type {
  PlanCashForecastDto,
  PlanRecoveryDto,
  PlanRecoveryPathDto,
  SpendableEstimateDto,
} from "@/lib/api/types";
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
    reclaim: emptyPath("Reclaim"),
    reclaimAll: emptyPath("ReclaimAll"),
    priority: "Avalanche",
    constraintDebtId: null,
    constraintKind: null,
    reclaimAmount: 0,
    priorityAlternatives: [],
    excludedCurrencies: [],
    missingBalance: [],
    hasDebts: false,
    monthlyExtra: 0,
    livingSpendingMonthly: 0,
    incomeLossMonths: 0,
    windfallAmount: 0,
    windfallOn: null,
    scenarioLivingSpendingMonthly: null,
    cashFloor: 0,
    scenarioCashFloor: null,
    debtFacts: [],
    hasCashFloor: false,
    hasEmergencyGoal: false,
    namedSavingsGoalCount: 0,
    cashOutlook: {
      asOf: "",
      startingCash: 0,
      startingReserve: 0,
      startingAvailable: 0,
      startingCashAccountCount: 0,
      startingCashManualAccountCount: 0,
      startingCashConnectedAccountCount: 0,
      startingCashOldestAsOf: null,
      startingCashUnknownDateCount: 0,
      startingCashStaleConnectedCount: 0,
      hasIncome: false,
      hasBills: false,
      excludedCurrencies: [],
      rollover: { typical: emptyForecast(), lowPay: null },
      reclaim: { typical: emptyForecast(), lowPay: null },
      reclaimAll: { typical: emptyForecast(), lowPay: null },
    },
    spendable: {
      rollover: emptySpendable(),
      reclaim: emptySpendable(),
      reclaimAll: emptySpendable(),
    },
  };
}

/**
 * An empty spendable estimate used when the plan request fails.
 * The failed page does not render it. The fields stay present so the type matches a successful response.
 */
function emptySpendable(): SpendableEstimateDto {
  return {
    from: "",
    through: "",
    spendable: 0,
    lowestAvailable: 0,
    lowestOn: "",
    reliableIncome: 0,
    obligations: 0,
    ordinarySpending: 0,
    reserved: 0,
    alreadyShort: false,
    guardrail: {
      active: false,
      ordinaryExpenses: false,
      nearTermObligations: false,
      on: null,
      shortfall: 0,
      alreadyShortWithoutExtra: false,
    },
    uncertainties: [],
  };
}

/**
 * An empty cash forecast with no days or horizons.
 */
function emptyForecast(): PlanCashForecastDto {
  return {
    days: [],
    dayView: {
      from: "",
      through: "",
      endingCash: 0,
      lowestCash: 0,
      lowestCashOn: "",
      cashShortfall: false,
    },
    horizons: [],
    shortfallOn: null,
    recoveredOn: null,
    reserveShortfallOn: null,
    reserveRestoredOn: null,
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
    cashReclaimed: 0,
    cashRolled: 0,
  };
}
