import type {
  MerchantHistoryGranularity,
  MerchantHistoryPeriodDto,
  TransactionDto,
} from "@/lib/api/types";

/**
 * Maps a transaction date onto the chart period that contains it.
 * An unreadable date is returned unchanged so it does not land in the wrong period.
 */
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

/**
 * Builds the selected period from the transactions that fall in it.
 * Totals are recalculated here so the drawer matches the rows it lists.
 */
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

/** The period summary the history sheet renders. */
export type SelectedMerchantPeriod = ReturnType<
  typeof getSelectedMerchantPeriod
>;

/**
 * Chooses the currency for a period's totals.
 * The first row's code is used when every row maps to the same code, and mixed codes use USD.
 */
export function periodCurrency(transactions: TransactionDto[]) {
  const codes = new Set(
    transactions.map((transaction) => transaction.isoCurrencyCode ?? "USD"),
  );

  if (codes.size === 1) {
    return transactions[0]?.isoCurrencyCode;
  }

  return "USD";
}
