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
 * `change` is the difference of two known comparable values. Unknown stays unknown.
 * `comparable` is false when the two sides do not share the basis named above the table.
 */
export type ScenarioComparisonRow = {
  label: "Cash safety" | "Payoff" | "Total interest";
  current: ComparedValue;
  trying: ComparedValue;
  change: ComparedValue;
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
      payoffRow(baseline, current, kind, date, sameStartingFacts),
      interestRow(baseline, current, kind, money, sameStartingFacts),
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
 * The sentence shown before any preview differs from current facts.
 * It replaces two copies of the same figures.
 */
export function scenarioBaselineSentence() {
  return "These are the current facts. A comparison appears after you update a preview.";
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

  const comparable =
    sameBasis &&
    currentValue.state === "known" &&
    tryingValue.state === "known";

  return {
    label: "Cash safety",
    current: currentValue,
    trying: tryingValue,
    change: safetyChange(
      safetyReading(baseline, kind),
      safetyReading(current, kind),
      comparable,
      money,
    ),
    comparable,
  };
}

/**
 * The all-debts payoff date on both sides.
 * An unknown date names the first dated payoff and the debt that blocks a full date.
 */
function payoffRow(
  baseline: PlanRecoveryDto,
  current: PlanRecoveryDto,
  kind: PayoffRolloverKind,
  date: FormatDate,
  sameStartingFacts: boolean,
): ScenarioComparisonRow {
  const currentValue = payoffValue(baseline, kind, date);
  const tryingValue = payoffValue(current, kind, date);
  const comparable =
    sameStartingFacts &&
    currentValue.state === "known" &&
    tryingValue.state === "known";

  return {
    label: "Payoff",
    current: currentValue,
    trying: tryingValue,
    change: payoffChange(
      baseline.hasDebts ? pathFor(baseline, kind).paidOffOn : null,
      current.hasDebts ? pathFor(current, kind).paidOffOn : null,
      currentValue,
      tryingValue,
      comparable,
    ),
    comparable,
  };
}

/**
 * Total interest on both sides.
 * The change is the difference of two complete totals. A partial sum is not a change amount.
 */
function interestRow(
  baseline: PlanRecoveryDto,
  current: PlanRecoveryDto,
  kind: PayoffRolloverKind,
  money: FormatMoney,
  sameStartingFacts: boolean,
): ScenarioComparisonRow {
  const currentValue = interestValue(baseline, kind, money);
  const tryingValue = interestValue(current, kind, money);
  const comparable =
    sameStartingFacts &&
    currentValue.state === "known" &&
    tryingValue.state === "known";
  const currentInterest = comparableInterest(baseline, kind);
  const tryingInterest = comparableInterest(current, kind);

  return {
    label: "Total interest",
    current: currentValue,
    trying: tryingValue,
    change:
      currentValue.state === "unknown" || tryingValue.state === "unknown"
        ? unknownChange
        : !comparable || currentInterest === null || tryingInterest === null
          ? notCompared
          : moneyDelta(tryingInterest - currentInterest, money),
    comparable,
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

  const path = pathFor(report, kind);
  if (!path.paidOffOn) {
    return { state: "unknown", text: unknownPayoffText(report, path, date) };
  }

  return { state: "known", text: date(path.paidOffOn) };
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
 * How far the trying safety figure moved from current facts.
 * A shortfall and a spendable amount are not subtracted into one invented number.
 */
function safetyChange(
  current: SafetyReading,
  trying: SafetyReading,
  comparable: boolean,
  money: FormatMoney,
): ComparedValue {
  if (current.kind === "unknown" || trying.kind === "unknown") {
    return unknownChange;
  }

  if (!comparable) {
    return notCompared;
  }

  if (current.kind === "shortfall" && trying.kind === "shortfall") {
    return shortfallDelta(trying.amount - current.amount, money);
  }

  if (current.kind === "spendable" && trying.kind === "spendable") {
    return moneyDelta(trying.amount - current.amount, money);
  }

  if (trying.kind === "shortfall") {
    return {
      state: "known",
      text: `Becomes a shortfall of ${money(trying.amount)}`,
    };
  }

  return {
    state: "known",
    text: `Shortfall clears. Spendable is ${money(trying.amount)}.`,
  };
}

/**
 * Whether one report's safety figure is unknown, a shortfall, or spendable.
 * The amount is the shortfall magnitude or the floored spendable figure.
 */
function safetyReading(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
): SafetyReading {
  const estimate = spendableFor(report, kind);
  if (report.cashOutlook.startingCashAccountCount === 0) {
    return { kind: "unknown", amount: 0 };
  }

  const noSteadyPay = (estimate.uncertainties ?? []).some(
    (item) => item.kind === "NoReliableIncome",
  );
  if (estimate.alreadyShort && estimate.lowestAvailable < 0) {
    return { kind: "shortfall", amount: Math.abs(estimate.lowestAvailable) };
  }

  if (noSteadyPay) {
    return { kind: "unknown", amount: 0 };
  }

  return { kind: "spendable", amount: estimate.spendable };
}

type SafetyReading = {
  kind: "unknown" | "shortfall" | "spendable";
  amount: number;
};

/**
 * Earlier, later, or unchanged, using the two stored dates.
 * A missing date stays unknown. This does not create a new date.
 */
function payoffChange(
  currentOn: string | null,
  tryingOn: string | null,
  current: ComparedValue,
  trying: ComparedValue,
  comparable: boolean,
): ComparedValue {
  if (current.state === "unknown" || trying.state === "unknown") {
    return unknownChange;
  }

  if (!comparable) {
    return notCompared;
  }

  if (current.text === "No debts" && trying.text === "No debts") {
    return noChange;
  }

  if (current.text === "No debts") {
    return { state: "known", text: "Now dated" };
  }

  if (trying.text === "No debts") {
    return { state: "known", text: "No longer dated" };
  }

  if (!currentOn || !tryingOn) {
    return unknownChange;
  }

  if (currentOn === tryingOn) {
    return noChange;
  }

  return {
    state: "known",
    text: tryingOn > currentOn ? "Later" : "Earlier",
  };
}

/**
 * Names the first dated payoff and every debt that blocks a date for all debts.
 * With neither a dated payoff nor a named blocker, the result stays unknown.
 */
function unknownPayoffText(
  report: PlanRecoveryDto,
  path: PlanRecoveryPathDto,
  date: FormatDate,
): string {
  const debts = path.debts ?? [];
  const dated = debts
    .filter((debt) => debt.paidOffOn)
    .slice()
    .sort((left, right) =>
      (left.paidOffOn ?? "").localeCompare(right.paidOffOn ?? ""),
    );
  const blockers = new Set<string>();
  for (const debt of debts) {
    if (!debt.paidOffOn && debt.name) {
      blockers.add(debt.name);
    }
  }
  for (const debt of report.missingBalance ?? []) {
    if (debt.name) {
      blockers.add(debt.name);
    }
  }

  const first = dated[0];
  const names = [...blockers];
  if (!first && names.length === 0) {
    return "Unknown";
  }

  const parts = ["Unknown."];
  if (first?.paidOffOn && first.name) {
    parts.push(
      `${first.name} is the first dated payoff, on ${date(first.paidOffOn)}.`,
    );
  } else if (first?.paidOffOn) {
    parts.push(`The first dated payoff is ${date(first.paidOffOn)}.`);
  }
  if (names.length === 1) {
    parts.push(`${names[0]} blocks a date for every debt.`);
  } else if (names.length > 1) {
    parts.push(`${names.join(", ")} block a date for every debt.`);
  }
  return parts.join(" ");
}

/**
 * Total interest when the path can date every included debt.
 * Null means the displayed total is unknown, so no difference is calculated.
 */
function comparableInterest(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
): number | null {
  if (!report.hasDebts) {
    return null;
  }

  const path = pathFor(report, kind);
  if (!path.paidOffOn || path.debts.some((debt) => debt.stop !== "PaidOff")) {
    return null;
  }

  return path.totalInterest;
}

/**
 * A larger or smaller money result. Zero is no change.
 */
function moneyDelta(delta: number, money: FormatMoney): ComparedValue {
  if (delta === 0) {
    return noChange;
  }

  return {
    state: "known",
    text: delta > 0 ? `Up ${money(delta)}` : `Down ${money(Math.abs(delta))}`,
  };
}

/**
 * A larger or smaller shortfall. The amount is the difference of the two stored shortfalls.
 */
function shortfallDelta(delta: number, money: FormatMoney): ComparedValue {
  if (delta === 0) {
    return noChange;
  }

  return {
    state: "known",
    text:
      delta > 0
        ? `Shortfall up ${money(delta)}`
        : `Shortfall down ${money(Math.abs(delta))}`,
  };
}

const unknownChange: ComparedValue = { state: "unknown", text: "Unknown" };
const notCompared: ComparedValue = { state: "unknown", text: "Not compared" };
const noChange: ComparedValue = { state: "known", text: "No change" };

/**
 * The spendable estimate for the selected payoff behavior.
 */
function spendableFor(report: PlanRecoveryDto, kind: PayoffRolloverKind) {
  if (kind === "Reclaim") {
    return report.spendable.reclaim;
  }

  return kind === "ReclaimAll"
    ? report.spendable.reclaimAll
    : report.spendable.rollover;
}

/**
 * The payoff path for the selected behavior.
 */
function pathFor(
  report: PlanRecoveryDto,
  kind: PayoffRolloverKind,
): PlanRecoveryPathDto {
  if (kind === "Reclaim") {
    return report.reclaim;
  }

  return kind === "ReclaimAll" ? report.reclaimAll : report.rollover;
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
