import type {
  PlanCashForecastDto,
  PlanCashHorizonDto,
  PlanRecoveryPathDto,
} from "@/lib/api/types";
import type { PlanCashRow } from "./planChartSeries";

export const planCashRanges = [
  "30-days",
  "6-months",
  "12-months",
  "18-months",
] as const;

export type PlanCashRange = (typeof planCashRanges)[number];

export type PlanCashRangeView = {
  range: PlanCashRange;
  label: string;
  available: boolean;
  through: string;
  ending: string;
  lowest: string;
  lowestOn: string;
  minimumsLabel: string;
  minimums: string;
  warning: boolean;
  warningText: string | null;
  reserveWarningText: string | null;
  chartRows: PlanCashRow[];
  chartLowest: PlanCashRow | null;
  chartLabel: string | null;
};

type FormatMoney = (amount: number) => string;
type FormatDate = (date: string) => string;

/**
 * Builds one selected cash range from existing daily or horizon data.
 * Longer ranges stay summaries because the API does not provide intermediate chart points for them.
 */
export function cashRangeView(
  forecast: PlanCashForecastDto,
  path: PlanRecoveryPathDto,
  range: PlanCashRange,
  chart: { rows: PlanCashRow[]; lowest: PlanCashRow | null },
  money: FormatMoney,
  date: FormatDate,
): PlanCashRangeView {
  const horizon = selectedHorizon(forecast, range);
  const label = rangeLabel(range);
  if (range !== "30-days" && !horizon) {
    return {
      range,
      label,
      available: false,
      through: "Unavailable",
      ending: "Unavailable",
      lowest: "Unavailable",
      lowestOn: "Unavailable",
      minimumsLabel: "Minimums still due",
      minimums: "Unavailable",
      warning: false,
      warningText: null,
      reserveWarningText: null,
      chartRows: [],
      chartLowest: null,
      chartLabel: null,
    };
  }

  const window = horizon?.window ?? forecast.dayView;
  const firstShortfall =
    forecast.shortfallOn && forecast.shortfallOn <= window.through
      ? forecast.shortfallOn
      : null;
  const reserveShortfall =
    forecast.reserveShortfallOn && forecast.reserveShortfallOn <= window.through
      ? forecast.reserveShortfallOn
      : null;
  const warningText = window.cashShortfall
    ? firstShortfall
      ? `${label} cash runs short on ${date(firstShortfall)}.`
      : `${label} cash falls below zero before ${date(window.through)}.`
    : null;
  const reserveWarningText = reserveShortfall
    ? `${label} available cash after protected savings runs short on ${date(reserveShortfall)}.`
    : null;

  return {
    range,
    label,
    available: true,
    through: date(window.through),
    ending: money(window.endingCash),
    lowest: money(window.lowestCash),
    lowestOn: date(window.lowestCashOn),
    minimumsLabel: horizon ? "Minimums still due" : "Current minimums",
    minimums: minimumsText(
      horizon ? horizon.minimumObligation : path.startingObligation,
      horizon?.unknownMinimumCount ?? unknownMinimumCount(path),
      money,
    ),
    warning: window.cashShortfall,
    warningText,
    reserveWarningText,
    chartRows: range === "30-days" ? chart.rows : [],
    chartLowest: range === "30-days" ? chart.lowest : null,
    chartLabel:
      range === "30-days"
        ? `Cash at the end of each day through ${date(window.through)}, ending at ${money(window.endingCash)}. Lowest is ${money(window.lowestCash)} on ${date(window.lowestCashOn)}.`
        : null,
  };
}

/**
 * Returns the visible label for one supported cash range.
 */
export function rangeLabel(range: PlanCashRange) {
  switch (range) {
    case "30-days":
      return "30 days";
    case "6-months":
      return "6 months";
    case "12-months":
      return "12 months";
    case "18-months":
      return "18 months";
  }
}

/**
 * Finds the API horizon represented by a range, or null for the detailed 30-day view.
 */
function selectedHorizon(
  forecast: PlanCashForecastDto,
  range: PlanCashRange,
): PlanCashHorizonDto | null {
  const months =
    range === "6-months"
      ? 6
      : range === "12-months"
        ? 12
        : range === "18-months"
          ? 18
          : null;
  return months === null
    ? null
    : (forecast.horizons.find((item) => item.months === months) ?? null);
}

/**
 * Counts debts whose current monthly minimum is unknown.
 */
function unknownMinimumCount(path: PlanRecoveryPathDto) {
  return path.debts.filter((debt) => debt.balance > 0 && debt.minimum === null)
    .length;
}

/**
 * Formats a known minimum obligation and names any debts excluded from it.
 */
function minimumsText(
  obligation: number | null,
  unknownCount: number,
  money: FormatMoney,
) {
  if (obligation === null) {
    return "Unknown";
  }

  if (unknownCount > 0) {
    return `${money(obligation)} a month, ${unknownCount} not included`;
  }

  return obligation === 0 ? "None" : `${money(obligation)} a month`;
}
