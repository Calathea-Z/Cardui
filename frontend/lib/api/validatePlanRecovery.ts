import type { PlanRecoveryDto } from "./types";

/**
 * Confirms that a Plan response includes the trust facts required by the current page.
 * An older API response is rejected so version skew becomes a page error instead of a render crash or misleading readiness copy.
 */
export function isCurrentPlanRecovery(
  value: unknown,
): value is PlanRecoveryDto {
  if (!isRecord(value) || !Array.isArray(value.debtFacts)) {
    return false;
  }

  if (
    typeof value.livingSpendingMonthly !== "number" ||
    typeof value.hasCashFloor !== "boolean" ||
    typeof value.hasEmergencyGoal !== "boolean" ||
    typeof value.namedSavingsGoalCount !== "number"
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
    typeof cash.startingCashStaleConnectedCount === "number"
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
