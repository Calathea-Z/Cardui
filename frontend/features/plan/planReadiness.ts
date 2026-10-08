import type { PlanCashForecastDto, PlanRecoveryDto } from "@/lib/api/types";

export type PlanReadinessRow = {
  key: string;
  title: string;
  detail: string;
  href: string;
  linkLabel: string;
  warning: boolean;
};

export type PlanNextAction = {
  key: string;
  title: string;
  detail: string;
  href: string;
  label: string;
};

export type PlanReadinessCopy = {
  next: PlanNextAction | null;
  rows: PlanReadinessRow[];
};

type FormatDate = (date: string) => string;

/**
 * Builds Plan's setup and freshness status from facts already returned by the recovery endpoint.
 * A dated cash shortfall leads; reliability gaps follow before less urgent setup improvements.
 */
export function planReadiness(
  report: PlanRecoveryDto,
  forecast: PlanCashForecastDto,
  debtAttentionCount: number,
  date: FormatDate,
): PlanReadinessCopy {
  const cash = cashRow(report, date);
  const debts = debtRow(report, debtAttentionCount, date);
  const income = incomeRow(report);
  const bills = billsRow(report);
  const budget = budgetRow(report, forecast, date);
  const savings = savingsRow(report, forecast, date);
  const rows = [cash, debts, income, bills, budget, savings];
  const activeDebtCount = report.debtFacts.filter(
    (debt) => debt.balance !== null && debt.balance > 0,
  ).length;
  const staleDebtCount = report.debtFacts.filter(
    (debt) => debt.freshness !== null && debt.freshness !== "Current",
  ).length;
  const paymentReviewCount = report.debtFacts.filter(
    (debt) => debt.needsPaymentReview,
  ).length;

  const next =
    issue(
      "budget",
      forecast.shortfallOn !== null,
      "Fix the dated cash shortfall",
      `Cash first runs short on ${date(forecast.shortfallOn ?? "")}. Review available pay and flexible spending before relying on the payoff date.`,
      "/living",
      "Review shortfall",
    ) ??
    issue(
      "savings",
      forecast.reserveShortfallOn !== null,
      "Review protected savings",
      `Available cash after protected savings first runs short on ${date(forecast.reserveShortfallOn ?? "")}.`,
      "/savings",
      "Review Savings",
    ) ??
    issue(
      "cash",
      report.cashOutlook.startingCashAccountCount === 0,
      "Add starting cash",
      "Plan starts from active cash accounts in the household currency.",
      "/accounts",
      "Review Accounts",
    ) ??
    issue(
      "debts",
      !report.hasDebts,
      "Add a debt",
      "Add the cards or loans this recovery plan needs to address.",
      "/debts",
      "Review Debts",
    ) ??
    issue(
      "income",
      !report.cashOutlook.hasIncome,
      "Add take-home income",
      "Plan needs dated pay to judge whether the next bills and debt payments fit.",
      "/income",
      "Review Income",
    ) ??
    issue(
      "debts",
      debtAttentionCount > 0,
      "Finish the next debt",
      `${debtAttentionCount} ${debtAttentionCount === 1 ? "debt needs" : "debts need"} a term before the payoff estimate is complete.`,
      "/debts",
      "Review Debts",
    ) ??
    issue(
      "cash",
      report.cashOutlook.startingCashStaleConnectedCount > 0,
      "Refresh starting cash",
      "At least one connected cash balance used by Plan is not current.",
      "/connections",
      "Review Connections",
    ) ??
    issue(
      "debts",
      staleDebtCount > 0,
      "Refresh debt balances",
      `${staleDebtCount} followed ${staleDebtCount === 1 ? "balance is" : "balances are"} not current.`,
      "/debts",
      "Review Debts",
    ) ??
    issue(
      "debts",
      paymentReviewCount > 0,
      "Review a zero-balance debt",
      `${paymentReviewCount} zero-balance ${paymentReviewCount === 1 ? "debt keeps" : "debts keep"} a saved minimum. The terms are retained but excluded from active payments.`,
      "/debts",
      "Review Debts",
    ) ??
    issue(
      "debts",
      activeDebtCount === 0,
      "Review debt balances",
      "No saved debt has a positive balance, so there is no active payoff path.",
      "/debts",
      "Review Debts",
    ) ??
    issue(
      "bills",
      !report.cashOutlook.hasBills,
      "Review recurring bills",
      "No non-debt bills are included. Add the ones the household pays, or confirm there are none.",
      "/bills",
      "Review Bills",
    ) ??
    issue(
      "budget",
      report.livingSpendingMonthly <= 0,
      "Set a Plan budget",
      "Choose how much household pay enters Plan and include one realistic flexible-spending amount.",
      "/living",
      "Review Plan budget",
    ) ??
    issue(
      "savings",
      !report.hasCashFloor,
      "Choose cash to keep",
      "Set the cash floor Plan should protect before relying on a payoff estimate.",
      "/savings",
      "Review Savings",
    ) ??
    issue(
      "savings",
      !report.hasEmergencyGoal,
      "Review emergency savings",
      "Decide whether this plan needs a dated emergency target. Cardui does not choose a universal amount.",
      "/savings",
      "Review Savings",
    );

  return { next, rows };
}

/**
 * Describes the cash accounts behind starting cash and calls out stale or undated connected values.
 */
function cashRow(report: PlanRecoveryDto, date: FormatDate): PlanReadinessRow {
  const cash = report.cashOutlook;
  const sources = [
    countLabel(cash.startingCashManualAccountCount, "maintained by you"),
    countLabel(cash.startingCashConnectedAccountCount, "connected"),
  ].filter(Boolean);
  const freshness = cash.startingCashOldestAsOf
    ? `Oldest balance date: ${date(cash.startingCashOldestAsOf)}.`
    : cash.startingCashAccountCount > 0
      ? "Balance dates are unavailable."
      : "No active cash account is included.";
  const caveat =
    cash.startingCashStaleConnectedCount > 0
      ? ` ${cash.startingCashStaleConnectedCount} connected ${cash.startingCashStaleConnectedCount === 1 ? "balance is" : "balances are"} not current.`
      : cash.startingCashUnknownDateCount > 0
        ? ` ${cash.startingCashUnknownDateCount} ${cash.startingCashUnknownDateCount === 1 ? "balance has" : "balances have"} no date.`
        : "";

  return {
    key: "cash",
    title: "Starting cash",
    detail: `${cash.startingCashAccountCount} active cash ${cash.startingCashAccountCount === 1 ? "account" : "accounts"}${sources.length > 0 ? ` (${sources.join(", ")})` : ""}. ${freshness}${caveat}`,
    href:
      cash.startingCashStaleConnectedCount > 0 ? "/connections" : "/accounts",
    linkLabel:
      cash.startingCashStaleConnectedCount > 0
        ? "Review Connections"
        : "Review Accounts",
    warning:
      cash.startingCashAccountCount === 0 ||
      cash.startingCashStaleConnectedCount > 0 ||
      cash.startingCashUnknownDateCount > 0,
  };
}

/**
 * Describes which debt balances are active, person-maintained, synced, stale, or held for review.
 */
function debtRow(
  report: PlanRecoveryDto,
  attentionCount: number,
  date: FormatDate,
): PlanReadinessRow {
  const active = report.debtFacts.filter(
    (debt) => debt.balance !== null && debt.balance > 0,
  );
  const synced = active.filter((debt) => debt.balanceSource === "Synced");
  const maintained = active.length - synced.length;
  const dated = active
    .map((debt) => debt.balanceAsOf)
    .filter((value): value is string => value !== null)
    .sort();
  const stale = active.filter(
    (debt) => debt.freshness !== null && debt.freshness !== "Current",
  ).length;
  const review = report.debtFacts.filter(
    (debt) => debt.needsPaymentReview,
  ).length;
  const parts = [
    `${active.length} active ${active.length === 1 ? "balance" : "balances"}`,
    synced.length > 0 ? `${synced.length} synced` : "",
    maintained > 0 ? `${maintained} maintained by you` : "",
    dated.length > 0 ? `oldest dated ${date(dated[0])}` : "",
    stale > 0 ? `${stale} not current` : "",
    attentionCount > 0 ? `${attentionCount} need terms` : "",
    review > 0 ? `${review} zero-balance payment review` : "",
  ].filter(Boolean);

  return {
    key: "debts",
    title: "Debt payments",
    detail:
      parts.length > 0
        ? `${parts.join(" · ")}. Zero balances do not count as active debt or payments.`
        : "No debt balance is ready for the payoff path.",
    href: "/debts",
    linkLabel: "Review Debts",
    warning:
      active.length === 0 || stale > 0 || attentionCount > 0 || review > 0,
  };
}

/**
 * States whether dated take-home pay is available to the forecast.
 */
function incomeRow(report: PlanRecoveryDto): PlanReadinessRow {
  return {
    key: "income",
    title: "Income",
    detail: report.cashOutlook.hasIncome
      ? "Typical take-home pay is included on its saved dates. Plan budget applies each contributor's saved share."
      : "No take-home income is included yet.",
    href: "/income",
    linkLabel: "Review Income",
    warning: !report.cashOutlook.hasIncome,
  };
}

/**
 * States whether recurring non-debt bills are available to the forecast.
 */
function billsRow(report: PlanRecoveryDto): PlanReadinessRow {
  return {
    key: "bills",
    title: "Bills",
    detail: report.cashOutlook.hasBills
      ? "Recurring non-debt bills are included on their saved dates."
      : "No recurring non-debt bills are included. Debt payments belong on Debts.",
    href: "/bills",
    linkLabel: "Review Bills",
    warning: !report.cashOutlook.hasBills,
  };
}

/**
 * States how the household contribution rule and flexible spending affect dated cash.
 */
function budgetRow(
  report: PlanRecoveryDto,
  forecast: PlanCashForecastDto,
  date: FormatDate,
): PlanReadinessRow {
  const shortfall = forecast.shortfallOn;
  return {
    key: "budget",
    title: "Plan budget",
    detail:
      report.livingSpendingMonthly <= 0
        ? "No flexible monthly spending is included. This editor also controls how much of each contributor's pay enters Plan."
        : shortfall
          ? `Household pay shares and flexible spending are included. Dated cash first runs short on ${date(shortfall)}.`
          : "Household pay shares and flexible monthly spending are included in dated cash.",
    href: "/living",
    linkLabel: "Review Plan budget",
    warning: report.livingSpendingMonthly <= 0 || shortfall !== null,
  };
}

/**
 * States which protected-cash layers are configured and whether they leave spendable cash short.
 */
function savingsRow(
  report: PlanRecoveryDto,
  forecast: PlanCashForecastDto,
  date: FormatDate,
): PlanReadinessRow {
  const parts = [
    report.hasCashFloor ? "Cash to keep set" : "Cash to keep not set",
    report.hasEmergencyGoal ? "Emergency set" : "Emergency not set",
    `${report.namedSavingsGoalCount} named ${report.namedSavingsGoalCount === 1 ? "goal" : "goals"}`,
  ];
  if (forecast.reserveShortfallOn) {
    parts.push(
      `available cash after these protections runs short ${date(forecast.reserveShortfallOn)}`,
    );
  }

  return {
    key: "savings",
    title: "Protected savings",
    detail: `${parts.join(" · ")}. Cash to keep is added to Emergency and named goals; these amounts stay in cash but reduce what is available.`,
    href: "/savings",
    linkLabel: "Review Savings",
    warning:
      !report.hasCashFloor ||
      !report.hasEmergencyGoal ||
      forecast.reserveShortfallOn !== null,
  };
}

/**
 * Returns one next action when its condition is true.
 */
function issue(
  key: string,
  condition: boolean,
  title: string,
  detail: string,
  href: string,
  label: string,
): PlanNextAction | null {
  return condition ? { key, title, detail, href, label } : null;
}

/**
 * Formats a count for a parenthetical source list.
 */
function countLabel(count: number, label: string) {
  return count > 0 ? `${count} ${label}` : "";
}
