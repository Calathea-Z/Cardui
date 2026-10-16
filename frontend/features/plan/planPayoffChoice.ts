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
 * The tradeoff for one alternative against avalanche.
 * Interest here is before a freed payment rolls. A missing change says the order matches avalanche.
 */
export function priorityTradeoff(
  alternative: PayoffAlternativeDto,
  money: FormatMoney,
) {
  if (alternative.choice === "Avalanche") {
    return alternative.applied
      ? "Avalanche stays the recommended order. Interest here is before a freed payment rolls."
      : "The comparison baseline. Interest here is before a freed payment rolls.";
  }

  if (!alternative.changesOrder) {
    return alternative.choice === "Constraint"
      ? "Choose a debt to pay first or last. Until that changes the order, this matches avalanche."
      : "This does not change who is paid first.";
  }

  const interest =
    alternative.interestDifference > 0
      ? `Costs ${money(alternative.interestDifference)} more interest than avalanche`
      : alternative.interestDifference < 0
        ? `Costs ${money(Math.abs(alternative.interestDifference))} less interest than avalanche`
        : "Interest matches avalanche";
  const cashName =
    alternative.choice === "Utilization"
      ? "Utilization cash"
      : "Net minimum cash";
  const cash = `${cashName} is ${money(alternative.tradeoffCash)}.`;
  const recommended = alternative.applied
    ? " This is the recommended order."
    : "";
  return `${interest} before a freed payment rolls. ${cash}${recommended}`;
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
 * The short name for a payment priority.
 */
export function priorityLabel(choice: PayoffPriorityChoice | undefined) {
  return (
    payoffPriorityOptions.find((option) => option.value === choice)?.label ??
    "Highest rate first"
  );
}
