/**
 * Formats a payment date for on-screen text.
 * The calendar day is kept in UTC so a date-only value does not shift a day.
 * A value that is not YYYY-MM-DD is returned unchanged.
 */
export function formatPaymentDate(value: string) {
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
