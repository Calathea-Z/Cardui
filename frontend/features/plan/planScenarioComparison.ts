import type {
  PayoffRolloverKind,
  PlanRecoveryDto,
  PlanRecoveryPathDto,
  SpendableEstimateDto,
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
 * One side of a compared figure.
 * Unknown stays unknown. A known zero is still a calculated zero.
 */
export type ComparedValue = {
  state: "known" | "unknown";
  text: string;
};

/**
 * Cash safety, payoff, or total interest for current facts and the working preview.
 * `comparable` is false when the two sides do not share the basis named above the table.
 */
export type ScenarioComparisonRow = {
  label: "Cash safety" | "Payoff" | "Total interest";
  current: ComparedValue;
  trying: ComparedValue;
  comparable: boolean;
};

/**
 * The like-for-like comparison for one payoff behavior.
 * Safety uses steady pay before extra debt payments. A windfall is named because it is left out of that figure.
 */
export type ScenarioComparison = {
  sameStartingFacts: boolean;
  sameIncomeBasis: boolean;
  sameSafetyWindow: boolean;
  windfallNote: string | null;
  rows: ScenarioComparisonRow[];
};

/**
 * Compares the page's current facts with the working preview.
 * Both sides use the same payoff behavior. Different starting cash, dates, or income bases are not treated as one comparison.
 */
export function planScenarioComparison(
  baseline: PlanRecoveryDto,
  current: PlanRecoveryDto,
  kind: PayoffRolloverKind,
  money: FormatMoney,
  date: FormatDate,
): ScenarioComparison {
  const sameStartingFacts =
    baseline.planningCurrency === current.planningCurrency &&
    baseline.cashOutlook.asOf === current.cashOutlook.asOf &&
    baseline.cashOutlook.startingCash === current.cashOutlook.startingCash;
  const sameIncomeBasis =
    baseline.incomeLossMonths === current.incomeLossMonths;
  const windfall = baseline.windfallAmount > 0 || current.windfallAmount > 0;
  const currentEstimate = spendableFor(baseline, kind);
  const tryingEstimate = spendableFor(current, kind);
  const sameSafetyWindow =
    currentEstimate.from === tryingEstimate.from &&
    currentEstimate.through === tryingEstimate.through;
  const safety = safetyRow(
    baseline,
    current,
    kind,
    money,
    sameStartingFacts && sameIncomeBasis && sameSafetyWindow,
  );

  return {
    sameStartingFacts,
    sameIncomeBasis,
    sameSafetyWindow,
    windfallNote: windfall
      ? "A windfall stays in cash and is not included in steady-pay Spendable."
      : null,
    rows: [
      safety,
      outcomeRow(
        "Payoff",
        payoffValue(baseline, kind, date),
        payoffValue(current, kind, date),
        sameStartingFacts,
      ),
      outcomeRow(
        "Total interest",
        interestValue(baseline, kind, money),
        interestValue(current, kind, money),
        sameStartingFacts,
      ),
    ],
  };
}

/**
 * The sentence above the comparison.
 * It says when the rows can be read against each other.
 */
export function scenarioComparisonNote(comparison: ScenarioComparison) {
  if (!comparison.sameStartingFacts) {
    return "These results do not start from the same cash and date, so they are not compared directly.";
  }

  if (!comparison.sameIncomeBasis || !comparison.sameSafetyWindow) {
    return "Cash safety does not use the same income basis and dates, so that row is not a direct comparison. Payoff and total interest still use the same debts when the start matches.";
  }

  return "Same starting cash, date, and steady pay. Before extra debt payments.";
}

/**
 * Near-term cash safety on both sides.
 * A missing cash account or missing steady pay is unknown. A shortfall is the amount below zero.
 */
function safetyRow(
  baseline: PlanRecoveryDto,
  current: PlanRecoveryDto,
  kind: PayoffRolloverKind,
  money: FormatMoney,
  sameBasis: boolean,
): ScenarioComparisonRow {
  const currentValue = safetyValue(
    baseline,
    spendableFor(baseline, kind),
    money,
  );
  const tryingValue = safetyValue(current, spendableFor(current, kind), money);

  return {
    label: "Cash safety",
    current: currentValue,
    trying: tryingValue,
    comparable:
      sameBasis &&
      currentValue.state === "known" &&
      tryingValue.state === "known",
  };
}

/**
 * Payoff or total interest, compared only when both values were calculated from the same start.
 */
function outcomeRow(
  label: "Payoff" | "Total interest",
  current: ComparedValue,
  trying: ComparedValue,
  sameStartingFacts: boolean,
): ScenarioComparisonRow {
  return {
    label,
    current,
    trying,
    comparable:
      sameStartingFacts &&
      current.state === "known" &&
      trying.state === "known",
  };
}

/**
 * The safety figure for one report.
 * No cash account, or no steady pay when the window is not already short, stays unknown.
 */
function safetyValue(
  report: PlanRecoveryDto,
  estimate: SpendableEstimateDto,
  money: FormatMoney,
): ComparedValue {
  if (report.cashOutlook.startingCashAccountCount === 0) {
    return { state: "unknown", text: "Unknown" };
  }

  const noSteadyPay = estimate.uncertainties.some(
    (item) => item.kind === "NoReliableIncome",
  );
  if (estimate.alreadyShort && estimate.lowestAvailable < 0) {
    return {
      state: "known",
      text: `Shortfall ${money(Math.abs(estimate.lowestAvailable))}`,
    };
  }

  if (noSteadyPay) {
    return { state: "unknown", text: "Unknown" };
  }

  return { state: "known", text: money(estimate.spendable) };
}

/**
 * The payoff date, or unknown when the path cannot date one.
 * A household with no debts has no payoff to estimate.
 */
function payoffValue(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
  date: FormatDate,
): ComparedValue {
  if (!report.hasDebts) {
    return { state: "known", text: "No debts" };
  }

  const paidOffOn = pathFor(report, kind).paidOffOn;
  if (!paidOffOn) {
    return { state: "unknown", text: "Unknown" };
  }

  return { state: "known", text: date(paidOffOn) };
}

/**
 * Total interest when every included debt has a payoff date.
 * A partial interest sum is not shown as the total.
 */
function interestValue(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
  money: FormatMoney,
): ComparedValue {
  if (!report.hasDebts) {
    return { state: "known", text: "No debts" };
  }

  const path = pathFor(report, kind);
  if (!path.paidOffOn || path.debts.some((debt) => debt.stop !== "PaidOff")) {
    return { state: "unknown", text: "Unknown" };
  }

  return { state: "known", text: money(path.totalInterest) };
}

/**
 * The spendable estimate for the selected payoff behavior.
 */
function spendableFor(report: PlanRecoveryDto, kind: PayoffRolloverKind) {
  return kind === "Rollover"
    ? report.spendable.rollover
    : report.spendable.reclaimAll;
}

/**
 * The payoff path for the selected behavior.
 */
function pathFor(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
): PlanRecoveryPathDto {
  return kind === "Rollover" ? report.rollover : report.reclaimAll;
}

/**
 * The dates named beside cash safety.
 * The comparison uses them to refuse two different windows.
 */
export function scenarioSafetyWindow(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
  date: FormatDate,
) {
  const estimate = spendableFor(report, kind);
  return `${date(estimate.from)} through ${date(estimate.through)}`;
}
