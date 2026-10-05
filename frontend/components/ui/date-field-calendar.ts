/**
 * One cell in a month grid.
 * `inMonth` is false for the leading and trailing days from the neighboring months.
 */
export type CalendarDay = {
  date: string;
  inMonth: boolean;
};

/**
 * Weekday initials for a Sunday-start month, in grid order.
 */
export const weekdayLabels = [
  "Su",
  "Mo",
  "Tu",
  "We",
  "Th",
  "Fr",
  "Sa",
] as const;

/**
 * Reads a `YYYY-MM-DD` value.
 * A partial or impossible date is null.
 */
export function parseDateInput(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (month < 1 || month > 12 || day < 1 || day > daysInMonth(year, month)) {
    return null;
  }

  return { year, month, day };
}

/**
 * Builds a `YYYY-MM-DD` value from a calendar day.
 */
export function formatDateInput(year: number, month: number, day: number) {
  const monthText = String(month).padStart(2, "0");
  const dayText = String(day).padStart(2, "0");
  return `${year}-${monthText}-${dayText}`;
}

/**
 * Formats a stored date for the field and for a day's accessible name.
 * An invalid value is null so the field can show its placeholder.
 */
export function formatDateLabel(value: string) {
  const parsed = parseDateInput(value);
  if (!parsed) {
    return null;
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(parsed.year, parsed.month - 1, parsed.day)));
}

/**
 * Formats the month shown above the day grid.
 */
export function formatMonthLabel(year: number, month: number) {
  return new Intl.DateTimeFormat("en-US", {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, 1)));
}

/**
 * Today's date as `YYYY-MM-DD` in the browser's local calendar.
 */
export function todayDateInput() {
  const now = new Date();
  return formatDateInput(now.getFullYear(), now.getMonth() + 1, now.getDate());
}

/**
 * Moves a date by a number of calendar days.
 * An invalid start date is returned unchanged.
 */
export function addDays(value: string, days: number) {
  const parsed = parseDateInput(value);
  if (!parsed) {
    return value;
  }

  const utc = new Date(Date.UTC(parsed.year, parsed.month - 1, parsed.day));
  utc.setUTCDate(utc.getUTCDate() + days);
  return formatDateInput(
    utc.getUTCFullYear(),
    utc.getUTCMonth() + 1,
    utc.getUTCDate(),
  );
}

/**
 * Moves a year and month by a number of months.
 * Month is 1 through 12.
 */
export function shiftMonth(year: number, month: number, deltaMonths: number) {
  const utc = new Date(Date.UTC(year, month - 1 + deltaMonths, 1));
  return { year: utc.getUTCFullYear(), month: utc.getUTCMonth() + 1 };
}

/**
 * True when the date is a real day inside the optional inclusive bounds.
 * Bounds are `YYYY-MM-DD`. A missing bound does not limit that side.
 */
export function isDateInRange(value: string, min?: string, max?: string) {
  if (!parseDateInput(value)) {
    return false;
  }

  if (min && value < min) {
    return false;
  }

  if (max && value > max) {
    return false;
  }

  return true;
}

/**
 * True when a month still contains a day inside the optional bounds.
 * Month navigation stops on a month that is entirely outside those bounds.
 */
export function monthHasSelectableDay(
  year: number,
  month: number,
  min?: string,
  max?: string,
) {
  const first = formatDateInput(year, month, 1);
  const last = formatDateInput(year, month, daysInMonth(year, month));
  if (max && first > max) {
    return false;
  }

  if (min && last < min) {
    return false;
  }

  return true;
}

/**
 * Builds six Sunday-start weeks for a month, including neighboring days.
 */
export function buildMonthGrid(year: number, month: number): CalendarDay[] {
  const first = formatDateInput(year, month, 1);
  const firstWeekday = new Date(Date.UTC(year, month - 1, 1)).getUTCDay();
  const start = addDays(first, -firstWeekday);
  const days: CalendarDay[] = [];

  for (let index = 0; index < 42; index += 1) {
    const date = addDays(start, index);
    const parsed = parseDateInput(date);
    days.push({
      date,
      inMonth: parsed?.year === year && parsed.month === month,
    });
  }

  return days;
}

/**
 * Number of days in a month.
 */
function daysInMonth(year: number, month: number) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}
