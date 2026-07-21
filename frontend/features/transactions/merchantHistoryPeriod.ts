import type {
  MerchantHistoryGranularity,
  MerchantHistoryPeriodDto,
  TransactionDto,
} from "@/lib/api/types";

export function getMerchantPeriodKey(
  date: string,
  granularity: MerchantHistoryGranularity,
): string {
  const [yearText, monthText] = date.split("-");
  const year = Number(yearText);
  const month = Number(monthText);

  if (!year || !month) {
    return date;
  }

  if (granularity === "yearly") {
    return String(year);
  }

  if (granularity === "quarterly") {
    const quarter = Math.floor((month - 1) / 3) + 1;
    return `${year}-Q${quarter}`;
  }

  return `${yearText}-${monthText}`;
}

export function getSelectedMerchantPeriod(
  periods: MerchantHistoryPeriodDto[],
  selectedPeriodKey: string,
  transactions: TransactionDto[],
  granularity: MerchantHistoryGranularity,
) {
  const period =
    periods.find((item) => item.key === selectedPeriodKey) ??
    ({
      key: selectedPeriodKey,
      label: selectedPeriodKey,
      shortLabel: selectedPeriodKey,
      totalAmount: 0,
      transactionCount: 0,
    } satisfies MerchantHistoryPeriodDto);

  const periodTransactions = transactions.filter(
    (transaction) =>
      getMerchantPeriodKey(transaction.date, granularity) === selectedPeriodKey,
  );

  const totalAmount = periodTransactions.reduce(
    (sum, transaction) => sum + transaction.amount,
    0,
  );
  const transactionCount = periodTransactions.length;

  return {
    key: period.key,
    label: period.label,
    transactionCount,
    totalAmount,
    averageAmount: transactionCount === 0 ? 0 : totalAmount / transactionCount,
    transactions: periodTransactions,
  };
}
