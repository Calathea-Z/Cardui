import type { IncomeCadence } from "@/lib/api/types";

const latestPaymentDate = "2100-12-31";

/**
 * Lists pay dates from the cadence and the next payment, on or after today,
 * through the end of the month two months after the first of those dates.
 * Irregular returns no dates. Weekly and biweekly step by days, so a month can contain three paychecks.
 * Semimonthly stays on two days of the month. The list is not a monthly amount placed on one date.
 */
export function upcomingPaymentDates(
  cadence: IncomeCadence,
  nextPaymentDate: string,
  today: string,
): string[] {
  const anchor = nextPaymentDate.slice(0, 10);
  const onOrAfter = today > anchor ? today : anchor;
  if (cadence === "Irregular") {
    return [];
  }

  let start: string | null = null;
  for (const date of enumerate(cadence, anchor)) {
    if (date >= onOrAfter) {
      start = date;
      break;
    }
  }

  if (!start) {
    return [];
  }

  let through = endOfLaterMonth(start, 2);
  if (through > latestPaymentDate) {
    through = latestPaymentDate;
  }

  const dates: string[] = [];
  for (const date of enumerate(cadence, anchor)) {
    if (date < start) {
      continue;
    }

    if (date > through) {
      break;
    }

    dates.push(date);
  }

  return dates;
}

/**
 * The average of one payment spread across a year, in dollars per month.
 * This amount has no date. Irregular returns null.
 * Biweekly uses 26 payments a year, not two payments in every month.
 */
export function averageMonthlyAmount(
  paymentAmount: number,
  cadence: IncomeCadence,
): number | null {
  const paymentsPerYear = paymentsPerYearFor(cadence);
  if (paymentsPerYear === 0) {
    return null;
  }

  const cents = Math.round(paymentAmount * 100);
  return Math.round((cents * paymentsPerYear) / 12) / 100;
}

/**
 * Walks the cadence forward from the next payment, through the latest stored date.
 * Each month step starts from the original anchor so a short month does not move later payments.
 */
function* enumerate(cadence: IncomeCadence, anchor: string): Generator<string> {
  if (cadence === "Weekly") {
    for (
      let date = anchor;
      date <= latestPaymentDate;
      date = addDays(date, 7)
    ) {
      yield date;
    }

    return;
  }

  if (cadence === "Biweekly") {
    for (
      let date = anchor;
      date <= latestPaymentDate;
      date = addDays(date, 14)
    ) {
      yield date;
    }

    return;
  }

  if (cadence === "Monthly") {
    yield* monthSteps(anchor, 1);
    return;
  }

  if (cadence === "Quarterly") {
    yield* monthSteps(anchor, 3);
    return;
  }

  if (cadence === "Yearly") {
    yield* monthSteps(anchor, 12);
    return;
  }

  if (cadence === "Semimonthly") {
    yield* semimonthlyDates(anchor);
  }
}

/**
 * Adds the same number of months from the anchor for each step.
 * A day the target month lacks uses that month's last day, then the anchor day returns.
 */
function* monthSteps(anchor: string, monthsPerStep: number): Generator<string> {
  for (let step = 0; ; step += 1) {
    const date = addMonths(anchor, step * monthsPerStep);
    if (date > latestPaymentDate) {
      return;
    }

    yield date;
  }
}

/**
 * Two paydays each month, fifteen days apart, using the next payment's day as one of them.
 * A day past the end of a short month uses that month's last day.
 */
function* semimonthlyDates(anchor: string): Generator<string> {
  const day = Number(anchor.slice(8, 10));
  const earlyDay = day <= 15 ? day : day - 15;
  const lateDay = earlyDay + 15;
  const limit = `${latestPaymentDate.slice(0, 7)}-01`;
  for (
    let month = `${anchor.slice(0, 7)}-01`;
    month <= limit;
    month = addMonths(month, 1)
  ) {
    const early = dayInMonth(month, earlyDay);
    const late = dayInMonth(month, lateDay);
    if (early >= anchor && early <= latestPaymentDate) {
      yield early;
    }

    if (late !== early && late >= anchor && late <= latestPaymentDate) {
      yield late;
    }
  }
}

/**
 * How many payments a full year contains for that cadence.
 * Irregular is zero because there is no schedule to average.
 */
function paymentsPerYearFor(cadence: IncomeCadence) {
  switch (cadence) {
    case "Weekly":
      return 52;
    case "Biweekly":
      return 26;
    case "Semimonthly":
      return 24;
    case "Monthly":
      return 12;
    case "Quarterly":
      return 4;
    case "Yearly":
      return 1;
    default:
      return 0;
  }
}

/**
 * A calendar day that many days later.
 * The calendar day stays in UTC so the date does not shift.
 */
function addDays(value: string, days: number) {
  const date = parseUtc(value);
  date.setUTCDate(date.getUTCDate() + days);
  return formatUtc(date);
}

/**
 * A calendar day that many months later, keeping the anchor day when the month has it.
 */
function addMonths(value: string, months: number) {
  const date = parseUtc(value);
  const day = date.getUTCDate();
  const shifted = new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + months, 1),
  );
  const last = daysInMonth(shifted.getUTCFullYear(), shifted.getUTCMonth());
  return formatUtc(
    new Date(
      Date.UTC(
        shifted.getUTCFullYear(),
        shifted.getUTCMonth(),
        Math.min(day, last),
      ),
    ),
  );
}

/**
 * The last calendar day of the month that many months after the given date.
 */
function endOfLaterMonth(value: string, monthsAhead: number) {
  const date = parseUtc(value);
  const shifted = new Date(
    Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + monthsAhead, 1),
  );
  const last = daysInMonth(shifted.getUTCFullYear(), shifted.getUTCMonth());
  return formatUtc(
    new Date(Date.UTC(shifted.getUTCFullYear(), shifted.getUTCMonth(), last)),
  );
}

/**
 * A day number in that month.
 * A number past the last day uses the last day.
 */
function dayInMonth(month: string, day: number) {
  const date = parseUtc(month);
  const last = daysInMonth(date.getUTCFullYear(), date.getUTCMonth());
  return formatUtc(
    new Date(
      Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), Math.min(day, last)),
    ),
  );
}

/**
 * How many days the month has.
 * `monthIndex` is 0 for January.
 */
function daysInMonth(year: number, monthIndex: number) {
  return new Date(Date.UTC(year, monthIndex + 1, 0)).getUTCDate();
}

/**
 * A YYYY-MM-DD value as a UTC calendar day.
 */
function parseUtc(value: string) {
  const [year, month, day] = value.slice(0, 10).split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

/**
 * A UTC calendar day as YYYY-MM-DD.
 */
function formatUtc(date: Date) {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}
