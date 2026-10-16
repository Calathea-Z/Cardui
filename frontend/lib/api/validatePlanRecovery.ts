import type { PlanRecoveryDto } from "./types";

/**
 * The left-out reasons a spendable estimate can name.
 * This list matches `spendableUncertaintyKinds` in the Plan types.
 */
const spendableUncertaintyKinds = [
  "VariableIncome",
  "UncertainIncome",
  "NoReliableIncome",
  "OrdinarySpendingUnset",
  "WindfallLeftOut",
  "IncomeLeftOut",
  "ExcludedCurrency",
  "DebtLeftOut",
] as const;

/**
 * Confirms that a Plan response includes the trust facts required by the current page.
 * An older API response is rejected so version skew becomes a page error instead of a render crash or misleading readiness copy.
 */
export function isCurrentPlanRecovery(
  value: unknown,
): value is PlanRecoveryDto {
  if (!isRecord(value) || !hasOfferDebtFacts(value.debtFacts)) {
    return false;
  }

  if (
    typeof value.livingSpendingMonthly !== "number" ||
    typeof value.incomeLossMonths !== "number" ||
    typeof value.windfallAmount !== "number" ||
    !isNullableString(value.windfallOn) ||
    !isNullableNumber(value.scenarioLivingSpendingMonthly) ||
    typeof value.cashFloor !== "number" ||
    !isNullableNumber(value.scenarioCashFloor) ||
    typeof value.hasCashFloor !== "boolean" ||
    typeof value.hasEmergencyGoal !== "boolean" ||
    typeof value.namedSavingsGoalCount !== "number" ||
    !isPriority(value.priority) ||
    typeof value.reclaimAmount !== "number" ||
    !Array.isArray(value.priorityAlternatives) ||
    !isRecord(value.reclaim)
  ) {
    return false;
  }

  const cash = value.cashOutlook;
  return (
    isRecord(cash) &&
    typeof cash.startingCashAccountCount === "number" &&
    typeof cash.startingCashManualAccountCount === "number" &&
    typeof cash.startingCashConnectedAccountCount === "number" &&
    isNullableString(cash.startingCashOldestAsOf) &&
    typeof cash.startingCashUnknownDateCount === "number" &&
    typeof cash.startingCashStaleConnectedCount === "number" &&
    isRecord(cash.reclaim) &&
    isSpendableReport(value.spendable)
  );
}

/**
 * Confirms both payoff paths include a 30-day spendable estimate.
 */
function isSpendableReport(value: unknown): boolean {
  return (
    isRecord(value) &&
    isSpendableEstimate(value.rollover) &&
    isSpendableEstimate(value.reclaim) &&
    isSpendableEstimate(value.reclaimAll)
  );
}

/**
 * Confirms one path names its horizon, the lowest day, and why anything was left out.
 */
function isSpendableEstimate(value: unknown): boolean {
  return (
    isRecord(value) &&
    typeof value.from === "string" &&
    typeof value.through === "string" &&
    typeof value.spendable === "number" &&
    typeof value.lowestAvailable === "number" &&
    typeof value.lowestOn === "string" &&
    typeof value.reliableIncome === "number" &&
    typeof value.obligations === "number" &&
    typeof value.ordinarySpending === "number" &&
    typeof value.reserved === "number" &&
    typeof value.alreadyShort === "boolean" &&
    isGuardrail(value.guardrail) &&
    Array.isArray(value.uncertainties) &&
    value.uncertainties.every(isUncertainty)
  );
}

/**
 * Confirms the extra-payment warning is present, including when it is inactive.
 */
function isGuardrail(value: unknown): boolean {
  return (
    isRecord(value) &&
    typeof value.active === "boolean" &&
    typeof value.ordinaryExpenses === "boolean" &&
    typeof value.nearTermObligations === "boolean" &&
    isNullableString(value.on) &&
    typeof value.shortfall === "number" &&
    typeof value.alreadyShortWithoutExtra === "boolean"
  );
}

/**
 * Confirms one left-out reason uses a known kind.
 */
function isUncertainty(value: unknown): boolean {
  return (
    isRecord(value) &&
    typeof value.kind === "string" &&
    spendableUncertaintyKinds.includes(
      value.kind as (typeof spendableUncertaintyKinds)[number],
    ) &&
    typeof value.name === "string"
  );
}

/**
 * Confirms each debt fact can be named on the offer comparison.
 * Kind, a stored APR or an explicit null, and currency are required. An empty list still passes.
 */
function hasOfferDebtFacts(value: unknown): value is unknown[] {
  return Array.isArray(value) && value.every(isOfferDebtFact);
}

/**
 * Reports whether one debt fact includes the offer-comparison fields.
 */
function isOfferDebtFact(value: unknown): boolean {
  return (
    isRecord(value) &&
    (value.kind === "Revolving" || value.kind === "Installment") &&
    (typeof value.apr === "number" || value.apr === null) &&
    typeof value.currency === "string"
  );
}

/**
 * True when the response names one of the four payment priorities.
 */
function isPriority(value: unknown) {
  return (
    value === "Avalanche" ||
    value === "MinimumRelease" ||
    value === "Utilization" ||
    value === "Constraint"
  );
}

/**
 * Reports whether a runtime value is an object with named fields.
 */
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

/**
 * Reports whether a runtime value is a string or an intentional null.
 */
function isNullableString(value: unknown): value is string | null {
  return typeof value === "string" || value === null;
}

/**
 * Reports whether a runtime value is a number or an intentional null.
 * Null means the preview kept the saved amount.
 */
function isNullableNumber(value: unknown): value is number | null {
  return typeof value === "number" || value === null;
}
