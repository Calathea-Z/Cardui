import type { AccountBalanceHistoryPointDto } from "@/lib/api/types";

/**
 * Window the accounts chart can show.
 * `ALL` starts at the first snapshot instead of a fixed number of days.
 */
export type ChartTimeRange = "1W" | "1M" | "3M" | "6M" | "1Y" | "ALL";

/**
 * Range choices in the order the chart selector shows them.
 */
export const CHART_TIME_RANGES: { value: ChartTimeRange; label: string }[] = [
  { value: "1W", label: "1W" },
  { value: "1M", label: "1M" },
  { value: "3M", label: "3M" },
  { value: "6M", label: "6M" },
  { value: "1Y", label: "1Y" },
  { value: "ALL", label: "All" },
];

/**
 * Window shown before the household picks another range.
 */
export const DEFAULT_CHART_TIME_RANGE: ChartTimeRange = "3M";

const RANGE_DAYS: Record<Exclude<ChartTimeRange, "ALL">, number> = {
  "1W": 7,
  "1M": 30,
  "3M": 90,
  "6M": 180,
  "1Y": 365,
};

const RANGE_LABELS: Record<ChartTimeRange, string> = {
  "1W": "past week",
  "1M": "past month",
  "3M": "past 3 months",
  "6M": "past 6 months",
  "1Y": "past year",
  ALL: "all time",
};

/**
 * Change from the first snapshot in the window to the last.
 * `deltaPercent` is null when the start value is zero.
 */
export type PeriodChange = {
  delta: number;
  deltaPercent: number | null;
  startValue: number;
  endValue: number;
};

/**
 * One balance snapshot placed on a numeric time axis.
 */
export type ChartHistoryPoint = AccountBalanceHistoryPointDto & {
  timestamp: number;
};

/**
 * Start and end of the chart axis, in milliseconds.
 * The end is the start of today so the window does not extend into tomorrow.
 */
export type ChartTimeWindow = {
  startMs: number;
  endMs: number;
};

/**
 * Local midnight for today.
 * Chart windows end here so a snapshot later today still sits on today's tick.
 */
function startOfToday() {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return today;
}

/**
 * Parses an API date or date-time as local midnight on that calendar day.
 */
function parseDate(value: string) {
  // Support DateOnly ("yyyy-MM-dd") and ISO datetimes from the API.
  const dateOnly = value.slice(0, 10);
  return new Date(`${dateOnly}T00:00:00`);
}

/**
 * First day included in a fixed window.
 * `ALL` returns null because that window starts at the first snapshot.
 */
function getRangeStartDate(range: ChartTimeRange) {
  if (range === "ALL") {
    return null;
  }

  const start = startOfToday();
  start.setDate(start.getDate() - RANGE_DAYS[range]);
  return start;
}

/**
 * Builds the axis window for a range.
 * `ALL` starts at the first snapshot, or today when there is no history.
 */
export function getChartTimeWindow(
  history: AccountBalanceHistoryPointDto[],
  range: ChartTimeRange,
): ChartTimeWindow {
  const end = startOfToday();
  const endMs = end.getTime();

  if (range === "ALL") {
    const firstPoint = history[0];
    const startMs = firstPoint ? parseDate(firstPoint.date).getTime() : endMs;
    return { startMs, endMs };
  }

  const start = getRangeStartDate(range)!;
  return { startMs: start.getTime(), endMs };
}

/**
 * Keeps snapshots that fall inside the selected window.
 * `ALL` returns the history unchanged.
 */
export function filterHistoryByRange(
  history: AccountBalanceHistoryPointDto[],
  range: ChartTimeRange,
) {
  const rangeStart = getRangeStartDate(range);

  if (!rangeStart) {
    return history;
  }

  return history.filter((point) => parseDate(point.date) >= rangeStart);
}

/**
 * Places each snapshot on a numeric time axis so the chart spans the window.
 */
export function toChartHistoryPoints(
  history: AccountBalanceHistoryPointDto[],
): ChartHistoryPoint[] {
  return history.map((point) => ({
    ...point,
    date: point.date.slice(0, 10),
    timestamp: parseDate(point.date).getTime(),
  }));
}

/**
 * Evenly spaced axis ticks from the start of the window to the end.
 * A zero-length window returns the end time only.
 */
export function getRangeTicks(
  window: ChartTimeWindow,
  tickCount: number,
): number[] {
  const { startMs, endMs } = window;

  if (endMs <= startMs) {
    return [endMs];
  }

  const count = Math.max(tickCount, 2);
  const step = (endMs - startMs) / (count - 1);

  return Array.from({ length: count }, (_, index) =>
    Math.round(startMs + step * index),
  );
}

/**
 * Measures the change from the first snapshot in view to the last.
 * Returns null when fewer than two days exist, because one point is not a trend.
 */
export function computePeriodChange(
  history: AccountBalanceHistoryPointDto[],
  getValue: (point: AccountBalanceHistoryPointDto) => number = (point) =>
    point.netWorth,
): PeriodChange | null {
  if (history.length < 2) {
    return null;
  }

  const startValue = getValue(history[0]);
  const endValue = getValue(history[history.length - 1]);
  const delta = endValue - startValue;
  const deltaPercent =
    startValue !== 0 ? (delta / Math.abs(startValue)) * 100 : null;

  return {
    delta,
    deltaPercent,
    startValue,
    endValue,
  };
}

/**
 * Phrase used in the change line and the empty-history message, such as "past 3 months".
 */
export function getRangeLabel(range: ChartTimeRange) {
  return RANGE_LABELS[range];
}

/**
 * Explains why the chart cannot draw a trend for this window.
 */
export function getInsufficientHistoryMessage(
  pointCount: number,
  range: ChartTimeRange,
) {
  const rangeLabel = getRangeLabel(range);

  if (pointCount === 0) {
    return `No balance snapshots were recorded in the ${rangeLabel}. Balance history is captured during account sync.`;
  }

  return `Only one day of balance history was recorded in the ${rangeLabel}. At least two days are needed to show a trend.`;
}

/**
 * Turns a tick value into a date.
 * Numeric ticks are milliseconds. Strings are API dates.
 */
function resolveDate(value: string | number) {
  if (typeof value === "number") {
    return new Date(value);
  }

  return parseDate(value);
}

/**
 * Axis date labels for a range.
 * A week uses weekdays. A year or all-time uses months. Shorter ranges use month and day.
 */
export function getDateTickFormatter(range: ChartTimeRange) {
  return (value: string | number) => {
    const date = resolveDate(value);

    if (range === "1W") {
      return new Intl.DateTimeFormat("en-US", { weekday: "short" }).format(
        date,
      );
    }

    if (range === "1Y" || range === "ALL") {
      return new Intl.DateTimeFormat("en-US", {
        month: "short",
        year: range === "ALL" ? "2-digit" : undefined,
      }).format(date);
    }

    return new Intl.DateTimeFormat("en-US", {
      month: "short",
      day: "numeric",
    }).format(date);
  };
}

/**
 * Full date shown in the chart tooltip.
 */
export function formatTooltipDate(value: string | number) {
  return new Intl.DateTimeFormat("en-US", {
    weekday: "short",
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(resolveDate(value));
}

/**
 * Formats a chart label as whole currency units.
 * Cents are dropped so the label stays a trend figure.
 */
export function formatChartCurrency(value: number, currency = "USD") {
  const code = /^[A-Z]{3}$/i.test(currency) ? currency.toUpperCase() : "USD";

  try {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: code,
      maximumFractionDigits: 0,
    }).format(value);
  } catch {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
      maximumFractionDigits: 0,
    }).format(value);
  }
}

/**
 * Compact axis label for large balances, such as $663K or $1.2M.
 * Amounts under one thousand use the whole-currency chart label.
 */
export function formatChartAxisCurrency(value: number, currency = "USD") {
  const abs = Math.abs(value);
  const sign = value < 0 ? "-" : "";
  const symbol = chartCurrencySymbol(currency);

  /**
   * Formats a compact number and drops a trailing .0.
   */
  const formatCompact = (n: number, suffix: string) => {
    const digits = n >= 100 ? 0 : 1;
    const formatted = n.toFixed(digits).replace(/\.0$/, "");
    return `${sign}${symbol}${formatted}${suffix}`;
  };

  if (abs >= 1_000_000) {
    return formatCompact(abs / 1_000_000, "M");
  }

  if (abs >= 1_000) {
    return formatCompact(abs / 1_000, "K");
  }

  return formatChartCurrency(value, currency);
}

/**
 * Narrow currency symbol for compact axis labels.
 * An unrecognized code uses the dollar sign.
 */
function chartCurrencySymbol(currency: string) {
  const code = /^[A-Z]{3}$/i.test(currency) ? currency.toUpperCase() : "USD";

  try {
    const parts = new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: code,
      currencyDisplay: "narrowSymbol",
      maximumFractionDigits: 0,
    }).formatToParts(0);
    return parts.find((part) => part.type === "currency")?.value ?? "$";
  } catch {
    return "$";
  }
}

/**
 * Signed amount and percent for the change line under the chart.
 * The percent is omitted when the period started at zero.
 */
export function formatPeriodDelta(change: PeriodChange, currency = "USD") {
  const sign = change.delta >= 0 ? "+" : "-";
  const amount = formatChartCurrency(Math.abs(change.delta), currency);
  const percent =
    change.deltaPercent === null
      ? null
      : `${change.delta >= 0 ? "+" : "-"}${Math.abs(change.deltaPercent).toFixed(1)}%`;

  return {
    amount: `${sign}${amount}`,
    percent,
  };
}
