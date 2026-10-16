import type {
  PayoffAlternativeDto,
  PayoffPriorityChoice,
  PayoffRolloverKind,
  PlanCashOutlookPathDto,
  PlanRecoveryDto,
  PlanRecoveryPathDto,
  SpendableEstimateDto,
} from "@/lib/api/types";

/**
 * Formats a money amount for on-screen text.
 */
type FormatMoney = (amount: number) => string;

/**
 * Formats a `YYYY-MM-DD` date for on-screen text.
 */
type FormatDate = (date: string) => string;

/**
 * The payment-priority choices, in comparison order.
 * Avalanche is the baseline. The others are the supported alternatives.
 */
export const payoffPriorityOptions: {
  value: PayoffPriorityChoice;
  label: string;
}[] = [
  { value: "Avalanche", label: "Highest rate first" },
  { value: "MinimumRelease", label: "Remove a minimum" },
  { value: "Utilization", label: "Lower utilization" },
  { value: "Constraint", label: "Pay one debt first or last" },
];

/**
 * How much freed cash to keep. None rolls it. Some keeps an amount. All keeps every freed dollar.
 */
export const freedCashOptions: {
  value: PayoffRolloverKind;
  label: string;
}[] = [
  { value: "Rollover", label: "Keep none" },
  { value: "Reclaim", label: "Keep some" },
  { value: "ReclaimAll", label: "Keep all" },
];

/**
 * The payoff path for the selected freed-cash choice.
 */
export function recoveryPath(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
): PlanRecoveryPathDto {
  if (kind === "Reclaim") {
    return report.reclaim;
  }

  return kind === "ReclaimAll" ? report.reclaimAll : report.rollover;
}

/**
 * The cash outlook for the selected freed-cash choice.
 */
export function recoveryOutlook(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
): PlanCashOutlookPathDto {
  if (kind === "Reclaim") {
    return report.cashOutlook.reclaim;
  }

  return kind === "ReclaimAll"
    ? report.cashOutlook.reclaimAll
    : report.cashOutlook.rollover;
}

/**
 * The spendable estimate for the selected freed-cash choice.
 */
export function recoverySpendable(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
): SpendableEstimateDto {
  if (kind === "Reclaim") {
    return report.spendable.reclaim;
  }

  return kind === "ReclaimAll"
    ? report.spendable.reclaimAll
    : report.spendable.rollover;
}

/**
 * The name of a debt in this preview, or a plain fallback when the id is not on the report.
 */
export function payoffDebtName(report: PlanRecoveryDto, debtId: string) {
  return (
    report.debtFacts.find((debt) => debt.debtId === debtId)?.name ??
    report.rollover.debts.find((debt) => debt.debtId === debtId)?.name ??
    "A debt"
  );
}

/**
 * Names the payment-priority order. This is who is paid first, not when each debt ends.
 */
export function priorityOrderLine(report: PlanRecoveryDto, debtIds: string[]) {
  if (debtIds.length === 0) {
    return "No payment priority yet.";
  }

  const names = debtIds.map((id) => payoffDebtName(report, id));
  return `Payment priority: ${names.join(", then ")}.`;
}

/**
 * The first debt in a payment order, when this report names it.
 * A missing id stays unnamed. This does not invent a debt.
 */
export function firstPaidName(report: PlanRecoveryDto, debtIds: string[]) {
  const id = debtIds[0];
  if (!id) {
    return null;
  }

  return (
    report.debtFacts.find((debt) => debt.debtId === id)?.name ??
    report.rollover.debts.find((debt) => debt.debtId === id)?.name ??
    null
  );
}

/**
 * The tradeoff for one alternative against paying the highest rate first.
 * It names who is paid first and what the interest difference means. That interest is before a freed payment rolls.
 */
export function priorityTradeoff(
  alternative: PayoffAlternativeDto,
  money: FormatMoney,
  firstPaid: string | null,
) {
  if (alternative.choice === "Avalanche") {
    return avalancheTradeoff(alternative.applied, firstPaid);
  }

  if (!alternative.changesOrder) {
    return alternative.choice === "Constraint"
      ? "Choose a debt to pay first or last. Until that changes the order, this matches paying the highest rate first."
      : "This does not change who is paid first.";
  }

  const interest = interestDifference(alternative.interestDifference, money);
  const cash =
    alternative.choice === "Utilization"
      ? ""
      : ` Net minimum cash is ${money(alternative.tradeoffCash)}.`;
  const recommended = alternative.applied
    ? " This is the recommended order."
    : "";
  return `${whoIsPaidFirst(alternative.choice, firstPaid)} ${interest}, before a freed payment rolls.${cash}${recommended}`;
}

/**
 * One line of the freed-cash comparison for a path that was calculated.
 * Unknown payoff or interest stays unknown. The amount kept is the path's reclaimed cash.
 */
export function freedCashLine(
  kind: PayoffRolloverKind,
  path: PlanRecoveryPathDto,
  baselineInterestKnown: boolean,
  money: FormatMoney,
  date: FormatDate,
) {
  const payoff = path.paidOffOn ? date(path.paidOffOn) : "Payoff date unknown";
  if (!path.paidOffOn || path.debts.some((debt) => debt.stop !== "PaidOff")) {
    return `${freedCashLabel(kind)}. ${payoff}. Interest unknown.`;
  }

  if (kind === "Rollover" || !baselineInterestKnown) {
    return `${freedCashLabel(kind)}. ${payoff}. Interest ${money(path.totalInterest)}.`;
  }

  return `${freedCashLabel(kind)}. ${payoff}. Interest ${money(path.totalInterest)}. Keeps ${money(path.cashReclaimed)}.`;
}

/**
 * The short name for a freed-cash choice.
 */
export function freedCashLabel(kind: PayoffRolloverKind) {
  return (
    freedCashOptions.find((option) => option.value === kind)?.label ??
    "Keep none"
  );
}

/**
 * Who an avalanche order pays first.
 * The interest on that order is the comparison. A missing name does not invent a debt.
 */
function avalancheTradeoff(applied: boolean, firstPaid: string | null) {
  if (!applied) {
    return "The highest rate is the comparison. Interest here is before a freed payment rolls.";
  }

  const who = firstPaid
    ? `${firstPaid} is paid first, at the highest rate.`
    : "The highest rate is paid first.";
  return `${who} This interest is the comparison, before a freed payment rolls.`;
}

/**
 * Who this order pays first.
 * A named debt is the first id in the order. Without a name, the sentence uses the choice and does not invent a debt.
 */
function whoIsPaidFirst(
  choice: PayoffAlternativeDto["choice"],
  firstPaid: string | null,
) {
  if (firstPaid) {
    return `${firstPaid} is paid first.`;
  }

  if (choice === "Utilization") {
    return "The balance closest to its limit is paid first.";
  }

  if (choice === "MinimumRelease") {
    return "The debt that removes a minimum is paid first.";
  }

  return "The debt you chose is paid first or last.";
}

/**
 * What the interest gap against paying the highest rate first means.
 * A positive gap costs more. A negative gap costs less. Zero matches.
 */
function interestDifference(difference: number, money: FormatMoney) {
  if (difference > 0) {
    return `That costs ${money(difference)} more interest than paying the highest rate first`;
  }

  if (difference < 0) {
    return `That costs ${money(Math.abs(difference))} less interest than paying the highest rate first`;
  }

  return "Interest matches paying the highest rate first";
}

/**
 * The short name for a payment priority.
 */
export function priorityLabel(choice: PayoffPriorityChoice | undefined) {
  return (
    payoffPriorityOptions.find((option) => option.value === choice)?.label ??
    "Highest rate first"
  );
}
