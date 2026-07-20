import type { TransactionDto } from "@/lib/api";

export type TransactionDateGroup = {
  date: string;
  transactions: TransactionDto[];
};

export function toDateKey(value: string) {
  return value.slice(0, 10);
}

export function formatDateKey(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

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
