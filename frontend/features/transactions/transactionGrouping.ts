import type { TransactionDto } from "@/lib/api/types";

/**
 * Transactions that share one calendar day, in the order the API returned them.
 */
export type TransactionDateGroup = {
  date: string;
  transactions: TransactionDto[];
};

/**
 * Keeps the calendar date from an API date or date-time.
 * Grouping uses the date only, so a time on the same day stays in that group.
 */
export function toDateKey(value: string) {
  return value.slice(0, 10);
}

/**
 * Formats a local date as `YYYY-MM-DD` so it can be compared with API date keys.
 */
export function formatDateKey(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

/**
 * Labels a date group.
 * Today and yesterday use those words. Other dates include the year only when it is not this year.
 */
export function formatDateSectionHeader(dateKey: string) {
  const [year, month, day] = dateKey.split("-").map(Number);
  const date = new Date(year, month - 1, day);
  const todayKey = formatDateKey(new Date());
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  const yesterdayKey = formatDateKey(yesterday);

  if (dateKey === todayKey) {
    return "Today";
  }

  if (dateKey === yesterdayKey) {
    return "Yesterday";
  }

  return new Intl.DateTimeFormat("en-US", {
    weekday: "long",
    month: "long",
    day: "numeric",
    ...(date.getFullYear() !== new Date().getFullYear()
      ? { year: "numeric" }
      : {}),
  }).format(date);
}

/**
 * Groups an already date-sorted list into day sections.
 * A new group starts only when the date changes, so the API order is preserved.
 */
export function groupTransactionsByDate(
  transactions: TransactionDto[],
): TransactionDateGroup[] {
  const groups: TransactionDateGroup[] = [];

  for (const transaction of transactions) {
    const date = toDateKey(transaction.date);
    const lastGroup = groups.at(-1);

    if (lastGroup?.date === date) {
      lastGroup.transactions.push(transaction);
      continue;
    }

    groups.push({ date, transactions: [transaction] });
  }

  return groups;
}
