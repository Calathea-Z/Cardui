import type { AccountBalanceHistoryPointDto, AccountGroupDto } from "@/lib/api/types";

export type AccountChartMetric =
  | "net-worth"
  | "cash"
  | "investments"
  | "credit-cards"
  | "loans";

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

export const ACCOUNT_CHART_METRICS: AccountChartMetricOption[] = [
  {
    value: "net-worth",
    label: "Net Worth",
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
    label: "Credit Cards",
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

export const DEFAULT_ACCOUNT_CHART_METRIC: AccountChartMetric = "net-worth";

export function getAccountChartMetricOption(
  metric: AccountChartMetric,
): AccountChartMetricOption {
  return (
    ACCOUNT_CHART_METRICS.find((option) => option.value === metric) ??
    ACCOUNT_CHART_METRICS[0]
  );
}

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

export function getHistoryValue(
  point: AccountBalanceHistoryPointDto,
  metric: AccountChartMetric,
): number {
  const option = getAccountChartMetricOption(metric);
  return point[option.historyKey];
}
