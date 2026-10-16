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
 * The copy for the spendable estimate.
 * `figure` is the amount that can be used. It is zero when the horizon is already short.
 * `leadsStatus` is true when the extra-payment warning should replace the payoff status.
 * `guardrail` is null when the extra does not leave ordinary spending or near-term bills short.
 */
export type SpendableCopy = {
  sentence: string;
  figure: string;
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
 * The figure is the lowest cash left after reserved funds. It is not monthly income minus monthly bills.
 * A household with debts lets a new shortfall caused by the extra lead the page status.
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
  return {
    sentence: estimate.alreadyShort
      ? `Nothing is left to spend from ${from} through ${through}. Cash after money set aside runs short on ${date(estimate.lowestOn)}. Only steady pay is counted, on its dates.`
      : `Lowest cash left after money set aside, from ${from} through ${through}. Dated bills, debt payments, and ordinary spending are already taken out. Only steady pay is counted. This is not a monthly surplus.`,
    figure: money(estimate.spendable),
    shortfall: estimate.alreadyShort
      ? `${money(estimate.lowestAvailable)} on ${date(estimate.lowestOn)}`
      : null,
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
      "Ending cash and the chart follow the cash outlook. That outlook counts every included paycheck. This estimate counts steady pay only.",
  };
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
