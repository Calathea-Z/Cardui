/**
 * Formats a calendar date for on-screen text.
 * The calendar day is kept in UTC so a date-only value does not shift a day.
 * A value that is not YYYY-MM-DD is returned unchanged.
 */
export function formatCalendarDate(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (!match) {
    return value;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, day)));
}

/**
 * Formats an APR for on-screen text.
 * The value is a percent. 19.99 is 19.99%, and 0 is a known 0% rate.
 */
export function formatApr(value: number) {
  const text = value.toFixed(3).replace(/\.?0+$/, "");
  return `${text}%`;
}

/**
 * Formats a share as a percent.
 * The ratio is a portion of a whole. 0.30 is 30%, and the screen keeps one decimal when needed.
 */
export function formatSharePercent(ratio: number) {
  const percent = Math.round(ratio * 1000) / 10;
  const text = Number.isInteger(percent) ? String(percent) : percent.toFixed(1);
  return `${text}%`;
}

/**
 * Formats utilization for on-screen text.
 * The ratio is the share of the credit limit in use. The screen rounds it to one decimal percent.
 */
export function formatUtilization(ratio: number) {
  return `${formatSharePercent(ratio)} of the limit`;
}

/**
 * How much of the utilization bar to fill, from 0 to 100.
 * A share above the whole limit fills the bar. The label still shows the full percent.
 */
export function utilizationFill(ratio: number) {
  if (!Number.isFinite(ratio) || ratio <= 0) {
    return 0;
  }

  return Math.min(Math.round(ratio * 1000) / 10, 100);
}
