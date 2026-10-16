import type {
  PlanCashOutlookDto,
  PlanDebtFactDto,
  SpendableEstimateDto,
  SpendableUncertaintyDto,
} from "@/lib/api/types";

/**
 * Formats a money amount in the planning currency.
 */
type FormatMoney = (amount: number) => string;

/**
 * Formats a `YYYY-MM-DD` date for on-screen text.
 */
type FormatDate = (date: string) => string;

/**
 * One dated total inside the spendable horizon.
 */
export type SpendablePiece = {
  label: string;
  value: string;
};

/**
 * Whether the 30-day figure can be treated as a supported safety answer.
 * `short` means cash after money set aside is below zero. `incomplete` means a required fact is missing.
 * `limited` means something is left out or stale. `covered` means this window has none of those limits.
 */
export type SpendableStatus = "short" | "incomplete" | "limited" | "covered";

/**
 * The copy for the spendable estimate.
 * `figure` is the shortfall amount when the horizon is already short, and otherwise the amount that can be used.
 * `lowestBalance` is the lowest point and its date. That date is not a first-gap date.
 * `leadsStatus` is true when a new extra-payment warning should stay beside the result.
 * `guardrail` is null when the extra does not leave ordinary spending or near-term bills short.
 */
export type SpendableCopy = {
  status: SpendableStatus;
  sentence: string;
  figureLabel: string;
  figure: string;
  lowestBalance: string;
  basis: string;
  protectedMoney: string;
  beforeExtra: string;
  shortfall: string | null;
  guardrail: string | null;
  guardrailOn: string | null;
  leadsStatus: boolean;
  pieces: SpendablePiece[];
  freshness: string;
  uncertainties: string[];
  chartNote: string;
};

/**
 * Builds the spendable sentence, the extra-payment warning, and the freshness line.
 * The figure is the shortfall when the lowest day is below zero, and otherwise the lowest cash left after reserved funds.
 * It is not monthly income minus monthly bills, and it is calculated before any extra debt payment.
 * The lowest-balance date is labeled as that low point. This estimate has no separate first-gap date to show.
 */
export function spendableCopy(
  estimate: SpendableEstimateDto,
  cash: PlanCashOutlookDto,
  debts: PlanDebtFactDto[],
  hasDebts: boolean,
  money: FormatMoney,
  date: FormatDate,
): SpendableCopy {
  const guardrail = guardrailSentence(estimate, money, date);
  const from = date(estimate.from);
  const through = date(estimate.through);
  const shortAmount =
    estimate.alreadyShort && estimate.lowestAvailable < 0
      ? Math.abs(estimate.lowestAvailable)
      : null;
  return {
    status: spendableStatus(estimate, cash, debts),
    sentence: estimate.alreadyShort
      ? `Cash after money set aside is short from ${from} through ${through}. The lowest balance is ${money(estimate.lowestAvailable)} on ${date(estimate.lowestOn)}. That date is the lowest balance in this window.`
      : `Lowest cash left after money set aside, from ${from} through ${through}. Dated bills, debt payments, and ordinary spending are already taken out. Only steady pay is counted. This is not a monthly surplus.`,
    figureLabel:
      shortAmount === null
        ? "Spendable before extra debt payments"
        : "Shortfall",
    figure: money(shortAmount ?? estimate.spendable),
    lowestBalance: `${money(estimate.lowestAvailable)} on ${date(estimate.lowestOn)}`,
    basis: `Steady pay only, before extra debt payments, from ${from} through ${through}.`,
    protectedMoney: `Set aside ${money(estimate.reserved)}.`,
    beforeExtra:
      "This amount is calculated before any extra debt payment. It is not an after-extra result.",
    shortfall:
      shortAmount === null
        ? null
        : `${money(estimate.lowestAvailable)} on ${date(estimate.lowestOn)}`,
    guardrail,
    guardrailOn: estimate.guardrail.on ? date(estimate.guardrail.on) : null,
    leadsStatus:
      guardrail !== null &&
      hasDebts &&
      !estimate.guardrail.alreadyShortWithoutExtra,
    pieces: [
      { label: "Steady pay", value: money(estimate.reliableIncome) },
      {
        label: "Bills and debt payments",
        value: money(estimate.obligations),
      },
      { label: "Ordinary spending", value: money(estimate.ordinarySpending) },
      { label: "Set aside", value: money(estimate.reserved) },
    ],
    freshness: freshnessLine(cash, debts, date),
    uncertainties: estimate.uncertainties.map(uncertaintyLine),
    chartNote:
      "The cash outlook counts every included paycheck and uses typical pay. This figure counts steady pay only.",
  };
}

/**
 * Classifies the estimate so a missing fact is not shown as covered.
 * A shortfall wins. Missing cash, steady pay, or ordinary spending is incomplete. Other exclusions or stale balances are limited.
 */
function spendableStatus(
  estimate: SpendableEstimateDto,
  cash: PlanCashOutlookDto,
  debts: PlanDebtFactDto[],
): SpendableStatus {
  if (estimate.alreadyShort) {
    return "short";
  }

  const missingFact = estimate.uncertainties.some(
    (item) =>
      item.kind === "NoReliableIncome" || item.kind === "OrdinarySpendingUnset",
  );
  if (missingFact || cash.startingCashAccountCount === 0) {
    return "incomplete";
  }

  const staleDebt = debts.some(
    (debt) => debt.freshness !== null && debt.freshness !== "Current",
  );
  if (
    estimate.uncertainties.length > 0 ||
    cash.startingCashStaleConnectedCount > 0 ||
    cash.startingCashUnknownDateCount > 0 ||
    staleDebt
  ) {
    return "limited";
  }

  return "covered";
}

/**
 * The warning when the extra payment leaves ordinary spending or a near-term bill or debt payment short.
 * Null when that guardrail is inactive or has no date.
 */
function guardrailSentence(
  estimate: SpendableEstimateDto,
  money: FormatMoney,
  date: FormatDate,
): string | null {
  const guard = estimate.guardrail;
  if (!guard.active || !guard.on) {
    return null;
  }

  const subject =
    guard.ordinaryExpenses && guard.nearTermObligations
      ? "ordinary spending and near-term bills"
      : guard.ordinaryExpenses
        ? "ordinary spending"
        : "near-term bills and debt payments";
  const verb = guard.alreadyShortWithoutExtra ? "keeps" : "would leave";
  const already = guard.alreadyShortWithoutExtra
    ? " That gap is already there without the extra."
    : "";
  return `This extra payment ${verb} ${subject} short on ${date(guard.on)}. Cash after money set aside would be ${money(guard.shortfall)} short.${already}`;
}

/**
 * One plain line for a reason the estimate leaves something out.
 */
function uncertaintyLine(item: SpendableUncertaintyDto): string {
  switch (item.kind) {
    case "VariableIncome":
      return `${item.name} is variable, so it is not counted.`;
    case "UncertainIncome":
      return `${item.name} is uncertain, so it is not counted.`;
    case "NoReliableIncome":
      return "No steady income is recorded, so this estimate does not add pay.";
    case "OrdinarySpendingUnset":
      return "Ordinary spending is not set, so this estimate may be high.";
    case "WindfallLeftOut":
      return "The one-time windfall is not counted as steady pay.";
    case "IncomeLeftOut":
      return "Pay left out of this preview is not counted.";
    case "ExcludedCurrency":
      return `${item.name} is left out of this estimate.`;
    case "DebtLeftOut":
      return `${item.name} is left out because a payment term is missing or the payment does not reduce the balance.`;
  }
}

/**
 * Names the cash and debt balance dates behind the estimate.
 * A followed debt that is not current is counted. A debt with no link is not.
 */
function freshnessLine(
  cash: PlanCashOutlookDto,
  debts: PlanDebtFactDto[],
  date: FormatDate,
): string {
  const cashLine = cash.startingCashOldestAsOf
    ? `Oldest cash balance: ${date(cash.startingCashOldestAsOf)}.`
    : cash.startingCashAccountCount > 0
      ? "Cash balance dates are unavailable."
      : "No active cash account is included.";
  const staleCash =
    cash.startingCashStaleConnectedCount > 0
      ? ` ${cash.startingCashStaleConnectedCount} connected cash ${cash.startingCashStaleConnectedCount === 1 ? "balance is" : "balances are"} not current.`
      : cash.startingCashUnknownDateCount > 0
        ? ` ${cash.startingCashUnknownDateCount} cash ${cash.startingCashUnknownDateCount === 1 ? "balance has" : "balances have"} no date.`
        : "";
  const staleDebts = debts.filter(
    (debt) => debt.freshness !== null && debt.freshness !== "Current",
  ).length;
  const debtLine =
    staleDebts > 0
      ? ` ${staleDebts} followed debt ${staleDebts === 1 ? "balance is" : "balances are"} not current.`
      : "";
  return `${cashLine}${staleCash}${debtLine}`;
}
