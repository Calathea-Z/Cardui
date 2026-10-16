import type {
  DebtScheduleStop,
  RestructuringComparisonDto,
  RestructuringDebtOutcomeDto,
  RestructuringFeeFunding,
  RestructuringOfferKind,
} from "@/lib/api/types";

/**
 * Shown with the form. The offer is not treated as approved, and no money moves.
 */
export const offerNotApproved =
  "These are terms you enter. Cardui does not treat them as approved, and it does not apply or move money.";

/**
 * What the comparison includes. It is not part of the working scenario or a saved plan.
 */
export const offerScope =
  "This compares current minimum payments. It leaves out any extra payment in the working scenario, and it is not saved with Your plan.";

/**
 * Shown beside a balance-transfer rate. The entered rate does not change later in the comparison.
 */
export const balanceTransferRateLimit =
  "The rate you enter is used for the whole term. A promotional rate that changes later is not modeled.";

/**
 * Shown with every comparison. A lower payment is not a rank.
 */
export const offerNotRanked =
  "A lower monthly payment is not treated as the better offer.";

/**
 * Limits that stay visible under How this comparison works.
 * Partial balances, a later rate, and more than one offer at a time are outside this comparison.
 */
export const comparisonAssumptions = [
  "The rate you enter is used for the whole term. A later rate is not modeled.",
  "Each selected debt is included in full. One offer is compared at a time.",
  "The current side uses today's minimums, not extra payments tried on Plan.",
  "A final payment can be smaller. Rounding can add a month, and the payoff date is the projected one.",
] as const;

/**
 * What the selected offer kind replaces.
 */
export function offerKindHelp(kind: RestructuringOfferKind): string {
  switch (kind) {
    case "ConsolidationLoan":
      return "Replaces two or more debts with one loan.";
    case "BalanceTransfer":
      return "Replaces revolving debts. The rate you enter is used for the whole term.";
    case "ReplacementLoan":
      return "Replaces one debt with one loan.";
  }
}

/**
 * The interest-and-fee difference. Null when either path does not pay off.
 * The sentence does not call either side better.
 */
export function costSentence(
  comparison: RestructuringComparisonDto,
  money: (amount: number) => string,
): string | null {
  if (comparison.keepTotalCost === null || comparison.offerTotalCost === null) {
    return null;
  }

  const difference = comparison.offerTotalCost - comparison.keepTotalCost;
  if (difference > 0) {
    return `This offer costs ${money(difference)} more in interest and fees.`;
  }

  if (difference < 0) {
    return `This offer costs ${money(-difference)} less in interest and fees.`;
  }

  return "Interest and fees match the current debts.";
}

/**
 * Why a total is missing. Null when both paths pay off.
 */
export function unknownCostSentence(
  comparison: RestructuringComparisonDto,
): string | null {
  const unfinished = comparison.debts.filter((debt) => debt.stop !== "PaidOff");
  if (unfinished.length === 0 && comparison.offerStop === "PaidOff") {
    return null;
  }

  const parts = unfinished.map(
    (debt) => `${debt.name}: ${stopPhrase(debt.stop)}`,
  );
  if (comparison.offerStop !== "PaidOff") {
    parts.push(`This offer: ${stopPhrase(comparison.offerStop)}`);
  }

  return `Total interest is unknown. ${parts.join(" ")}`;
}

/**
 * The monthly cash change while the current minimums would still be due.
 * Other income and bills are unchanged. Unknown when a current minimum is missing.
 */
export function paymentSentence(
  comparison: RestructuringComparisonDto,
  money: (amount: number) => string,
): string {
  if (
    comparison.monthlyCashFlow === null ||
    comparison.keepMonthlyPayment === null
  ) {
    return "The current monthly payment is unknown, so the cash-flow change is unknown.";
  }

  if (comparison.monthlyCashFlow > 0) {
    return `The offer payment is ${money(comparison.monthlyCashFlow)} lower each month while those minimums would still be due. Other income and bills stay as they are.`;
  }

  if (comparison.monthlyCashFlow < 0) {
    return `The offer payment is ${money(-comparison.monthlyCashFlow)} higher each month. Other income and bills stay as they are.`;
  }

  return "The monthly payment matches the current minimums. Other income and bills stay as they are.";
}

/**
 * The break-even date, or why it is not shown.
 */
export function breakEvenSentence(
  comparison: RestructuringComparisonDto,
  formatDate: (value: string) => string,
): string {
  if (comparison.breakEven === "Reached" && comparison.breakEvenOn) {
    return `The offer's interest and fee stay at or below the current interest from ${formatDate(comparison.breakEvenOn)}.`;
  }

  if (comparison.breakEven === "Never") {
    return "The offer's interest and fee stay higher through both payoffs. There is no break-even date.";
  }

  return "A break-even date is not shown because a path cannot be projected through payoff.";
}

/**
 * What cash paid at the start can no longer do.
 * A fee added to the balance is borrowed, not taken from cash. No investment return is assumed.
 */
export function opportunitySentence(
  comparison: RestructuringComparisonDto,
  money: (amount: number) => string,
): string {
  if (comparison.cashDueNow <= 0) {
    return comparison.fee > 0 && comparison.feeFunding === "Financed"
      ? "This offer does not use cash at the start. The fee is added to the balance and is borrowed."
      : "This offer does not use cash at the start.";
  }

  const used = `This uses ${money(comparison.cashDueNow)} now. That cash cannot also stay set aside or pay another debt. This comparison does not treat it as an investment return.`;
  if (comparison.cash.exceedsCashOnHand) {
    return `${used} That is more than the ${money(comparison.cash.cashOnHand)} in cash accounts.`;
  }

  if (comparison.cash.leavesLessThanCashToKeep) {
    const left = comparison.cash.cashOnHand - comparison.cashDueNow;
    return `${used} ${money(left)} would remain, which is less than the ${money(comparison.cash.cashToKeep)} cash to keep.`;
  }

  if (comparison.cash.usesReserve) {
    return `${used} That includes cash already set aside.`;
  }

  return used;
}

/**
 * True when upfront cash needs a warning.
 * Spending into the reserve while cash to keep still holds stays in the sentence, without a warning.
 */
export function opportunityWarns(
  comparison: RestructuringComparisonDto,
): boolean {
  return (
    comparison.cashDueNow > 0 &&
    (comparison.cash.exceedsCashOnHand ||
      comparison.cash.leavesLessThanCashToKeep)
  );
}

/**
 * The fee as none, added to the balance, or paid in cash.
 */
export function feeText(
  amount: number,
  funding: RestructuringFeeFunding,
  money: (amount: number) => string,
): string {
  if (amount === 0) {
    return "None";
  }

  const where =
    funding === "Financed" ? "added to the balance" : "paid in cash";
  return `${money(amount)} ${where}`;
}

/**
 * The rate shown for one current debt, including a promotional rate when one is stored.
 */
export function debtRateLabel(
  debt: RestructuringDebtOutcomeDto,
  formatApr: (value: number) => string,
  formatDate: (value: string) => string,
): string {
  if (
    debt.promotionalApr !== null &&
    debt.promotionalEndsOn &&
    debt.apr !== null
  ) {
    return `${formatApr(debt.promotionalApr)} until ${formatDate(debt.promotionalEndsOn)}, then ${formatApr(debt.apr)}`;
  }

  if (debt.promotionalApr !== null) {
    return debt.promotionalEndsOn
      ? `${formatApr(debt.promotionalApr)} until ${formatDate(debt.promotionalEndsOn)}`
      : formatApr(debt.promotionalApr);
  }

  if (debt.apr === null) {
    return "Rate missing";
  }

  return formatApr(debt.apr);
}

/**
 * A payoff date, or a short statement that it was not calculated.
 */
export function payoffText(
  date: string | null,
  formatDate: (value: string) => string,
): string {
  return date ? formatDate(date) : "Not calculated";
}

/**
 * Shown when rounding makes the offer last longer than the term entered.
 */
export function termNote(
  comparison: RestructuringComparisonDto,
): string | null {
  if (
    comparison.offerPaymentCount !== null &&
    comparison.offerPaymentCount !== comparison.termMonths
  ) {
    const longer =
      comparison.offerPaymentCount - comparison.termMonths === 1
        ? "one month longer"
        : "longer";
    return `The projected payoff is ${comparison.offerPaymentCount} payments, ${longer} than the ${comparison.termMonths} month term, because the last payment is smaller.`;
  }

  return null;
}

/**
 * The plain reason a projection stopped before payoff.
 */
function stopPhrase(stop: DebtScheduleStop): string {
  switch (stop) {
    case "DoesNotPayDown":
      return "the payment does not reduce the balance.";
    case "RateUnknown":
      return "the rate is missing.";
    case "MinimumUnknown":
      return "the minimum is missing.";
    case "DueDateUnknown":
      return "the due date is missing.";
    case "HorizonReached":
      return "the payoff is past 50 years.";
    case "PaidOff":
      return "paid off.";
  }
}
