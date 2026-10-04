export function formatDashboardPeriod(start: string, end: string): string {
  const startDate = parseDateOnly(start);
  const endDate = parseDateOnly(end);

  if (!startDate || !endDate) {
    return "Current month";
  }

  const sameYear = startDate.getFullYear() === endDate.getFullYear();
  const sameMonth = sameYear && startDate.getMonth() === endDate.getMonth();

  if (startDate.getTime() === endDate.getTime()) {
    return new Intl.DateTimeFormat("en-US", {
      month: "long",
      day: "numeric",
      year: "numeric",
    }).format(startDate);
  }

  if (sameMonth) {
    const month = new Intl.DateTimeFormat("en-US", {
      month: "long",
    }).format(startDate);

    return `${month} ${startDate.getDate()}–${endDate.getDate()}, ${endDate.getFullYear()}`;
  }

  const startLabel = new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: sameYear ? undefined : "numeric",
  }).format(startDate);
  const endLabel = new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(endDate);

  return `${startLabel}–${endLabel}`;
}

export function calculateCategoryPercentage(
  amount: number,
  total: number,
): number {
  if (total <= 0 || amount <= 0) {
    return 0;
  }

  return Math.min(100, (amount / total) * 100);
}

function parseDateOnly(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(year, month - 1, day);

  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day
  ) {
    return null;
  }

  return date;
}
