import { moneyDigits } from "@/features/accounts/formatCurrency";
import type { PlanScenarioQuery } from "@/lib/api/types";

/**
 * A parsed optional money field.
 * Empty keeps the saved amount. Invalid is not sent. An amount includes zero.
 */
export type ParsedScenarioAmount =
  | { state: "empty" }
  | { state: "amount"; amount: number }
  | { state: "invalid" };

/**
 * True when the preview matches the plan loaded with the page.
 * Zero extra, no income loss, no windfall, and null spending and floor keep the saved figures.
 */
export function isSavedPlanScenario(scenario: PlanScenarioQuery) {
  return (
    scenario.monthlyExtra <= 0 &&
    scenario.incomeLossMonths === 0 &&
    scenario.windfallAmount <= 0 &&
    scenario.windfallOn === null &&
    scenario.livingSpendingMonthly === null &&
    scenario.cashFloor === null
  );
}

/**
 * Reads the months-without-pay field.
 * Blank is zero, so every paycheck stays. A whole number from 0 to 18 is that many months. Anything else is not sent.
 */
export function parseIncomeLossMonths(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return 0;
  }

  if (!/^\d{1,2}$/.test(trimmed)) {
    return null;
  }

  const months = Number(trimmed);
  if (!Number.isInteger(months) || months > 18) {
    return null;
  }

  return months;
}

/**
 * Reads an optional money field.
 * Blank keeps the saved amount. Zero is an explicit amount. A negative amount or other text is invalid.
 */
export function parseScenarioAmount(value: string): ParsedScenarioAmount {
  const trimmed = value.trim();
  if (!trimmed) {
    return { state: "empty" };
  }

  const digits = moneyDigits(trimmed);
  if (digits === null) {
    return { state: "invalid" };
  }

  const parsed = Number(digits);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return { state: "invalid" };
  }

  return { state: "amount", amount: Math.round(parsed * 100) / 100 };
}

/**
 * The last day of the 18-month forecast that starts on asOf.
 * That day is the day before the same calendar day 18 months later. A short month keeps its last day.
 * An unusable date returns null.
 */
export function planForecastEnd(asOf: string) {
  const start = parseIsoDate(asOf);
  if (!start) {
    return null;
  }

  const end = addMonths(start, 18);
  end.setUTCDate(end.getUTCDate() - 1);
  return formatIsoDate(end);
}

/**
 * Reads the windfall amount and date together.
 * Both blank clears the windfall. A positive amount needs a date from asOf through the forecast end.
 * An unusable amount, a missing date, or a date outside that window is not sent.
 */
export function parseWindfall(
  amountText: string,
  date: string,
  asOf: string,
  forecastEnd: string,
) {
  const amount = parseScenarioAmount(amountText);
  const trimmedDate = date.trim();
  if (amount.state === "invalid") {
    return null;
  }

  if (amount.state === "empty" || amount.amount === 0) {
    return trimmedDate ? null : { amount: 0, on: null };
  }

  if (!trimmedDate || trimmedDate < asOf || trimmedDate > forecastEnd) {
    return null;
  }

  return { amount: amount.amount, on: trimmedDate };
}

/**
 * The message when months without pay is not a whole number from 0 to 18.
 */
export function incomeLossInvalid() {
  return "Enter a whole number of months from 0 to 18.";
}

/**
 * The message when a preview amount is not blank, zero, or positive.
 */
export function scenarioAmountInvalid() {
  return "Enter a zero or positive amount, or leave it blank.";
}

/**
 * The message when a windfall is missing its amount or a date inside the forecast.
 */
export function windfallInvalid() {
  return "Enter an amount and a date inside the 18-month forecast, or clear both.";
}

/**
 * Reads a YYYY-MM-DD date as UTC.
 * A day that does not exist returns null.
 */
function parseIsoDate(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));
  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    return null;
  }

  return date;
}

/**
 * Adds calendar months the way the forecast does.
 * A 31st in a shorter month lands on that month's last day.
 */
function addMonths(date: Date, months: number) {
  const target = new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + months, 1),
  );
  const lastDay = new Date(
    Date.UTC(target.getUTCFullYear(), target.getUTCMonth() + 1, 0),
  ).getUTCDate();
  const day = Math.min(date.getUTCDate(), lastDay);
  return new Date(Date.UTC(target.getUTCFullYear(), target.getUTCMonth(), day));
}

/**
 * Formats a UTC date as YYYY-MM-DD.
 */
function formatIsoDate(date: Date) {
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${date.getUTCFullYear()}-${month}-${day}`;
}
