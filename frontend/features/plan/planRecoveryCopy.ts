import { formatCalendarDate } from "@/features/debts/debtDisplay";
import type { CashFlowRecoveryStepDto } from "@/lib/api/types";

/**
 * Says when a payoff's minimum leaves.
 * A missing date means that due date falls outside the projection.
 */
export function minimumLeavesOn(
  step: CashFlowRecoveryStepDto,
  minimum: string,
) {
  if (!step.startsOn) {
    return `Minimum of ${minimum} leaves on a date outside this projection.`;
  }

  return `Minimum of ${minimum} leaves ${formatCalendarDate(step.startsOn)}.`;
}

/**
 * Names currencies the plan left out.
 * An empty list means every amount uses the planning currency, so there is nothing to say.
 */
export function excludedCurrencyNote(
  codes: string[],
  planningCurrency: string,
) {
  if (codes.length === 0) {
    return null;
  }

  const verb = codes.length === 1 ? "is" : "are";
  return `${codes.join(", ")} ${verb} left out. This plan uses ${planningCurrency}.`;
}
