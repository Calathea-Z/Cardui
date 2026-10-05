import type {
  AccountBalanceHistoryPointDto,
  AccountGroupDto,
} from "@/lib/api/types";

/**
 * Balance series the accounts chart can show.
 * Liabilities are included so the chart can plot what is owed as well as what is owned.
 */
export type AccountChartMetric =
  "net-worth" | "cash" | "investments" | "credit-cards" | "loans";

/**
 * One chart series and the account-group total that matches it.
 * `isLiability` marks a series where an increase is money owed.
 */
export type AccountChartMetricOption = {
  value: AccountChartMetric;
  label: string;
  historyKey: keyof Pick<
    AccountBalanceHistoryPointDto,
    "netWorth" | "cash" | "investments" | "creditCards" | "loans"
  >;
  groupKey: string;
  /** When true, an increase is bad (liabilities). */
  isLiability: boolean;
};

/**
 * Chart series in the order the metric selector shows them.
 */
export const ACCOUNT_CHART_METRICS: AccountChartMetricOption[] = [
  {
    value: "net-worth",
    label: "Net worth",
    historyKey: "netWorth",
    groupKey: "net-worth",
    isLiability: false,
  },
  {
    value: "cash",
    label: "Cash",
    historyKey: "cash",
    groupKey: "cash",
    isLiability: false,
  },
  {
    value: "investments",
    label: "Investments",
    historyKey: "investments",
    groupKey: "investments",
    isLiability: false,
  },
  {
    value: "credit-cards",
    label: "Credit cards",
    historyKey: "creditCards",
    groupKey: "credit-cards",
    isLiability: true,
  },
  {
    value: "loans",
    label: "Loans",
    historyKey: "loans",
    groupKey: "loans",
    isLiability: true,
  },
];

/**
 * Series shown before the household picks another metric.
 */
export const DEFAULT_ACCOUNT_CHART_METRIC: AccountChartMetric = "net-worth";

/**
 * Resolves a chart series.
 * An unknown value falls back to the first series so the chart still has a scale.
 */
export function getAccountChartMetricOption(
  metric: AccountChartMetric,
): AccountChartMetricOption {
  return (
    ACCOUNT_CHART_METRICS.find((option) => option.value === metric) ??
    ACCOUNT_CHART_METRICS[0]
  );
}

/**
 * Reads the account-group total for a chart series.
 * Net worth can fall back to a caller total when that group is missing.
 */
export function getMetricTotal(
  groups: AccountGroupDto[],
  metric: AccountChartMetric,
  fallbackNetWorth = 0,
): number {
  const option = getAccountChartMetricOption(metric);
  const group = groups.find((item) => item.key === option.groupKey);

  if (group) {
    return group.total;
  }

  return metric === "net-worth" ? fallbackNetWorth : 0;
}

/**
 * Reads one history point's value for the selected series.
 */
export function getHistoryValue(
  point: AccountBalanceHistoryPointDto,
  metric: AccountChartMetric,
): number {
  const option = getAccountChartMetricOption(metric);
  return point[option.historyKey];
}
