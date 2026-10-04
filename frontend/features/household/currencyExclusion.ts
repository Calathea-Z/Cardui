/**
 * Accounts and transactions left out of a total because their currency
 * is not the household planning currency.
 */
export type CurrencyExclusion = {
  planningCurrency: string;
  excludedAccountCount: number;
  excludedTransactionCount?: number;
  excludedCurrencies: string[];
};

/**
 * Builds the notice that some rows were left out of a total.
 * Returns null when nothing was excluded.
 */
export function formatCurrencyExclusion(
  exclusion: CurrencyExclusion,
): string | null {
  const accounts = exclusion.excludedAccountCount;
  const transactions = exclusion.excludedTransactionCount ?? 0;

  if (accounts <= 0 && transactions <= 0) {
    return null;
  }

  const parts: string[] = [];
  if (accounts > 0) {
    parts.push(`${accounts} ${accounts === 1 ? "account" : "accounts"}`);
  }
  if (transactions > 0) {
    parts.push(
      `${transactions} ${transactions === 1 ? "transaction" : "transactions"}`,
    );
  }

  const currencies = exclusion.excludedCurrencies.filter(
    (currency) => currency.trim().length > 0,
  );
  const currencyText =
    currencies.length > 0 ? ` in ${currencies.join(", ")}` : "";
  const verb = accounts + transactions === 1 ? "is" : "are";

  return `Totals use ${exclusion.planningCurrency}. ${parts.join(" and ")}${currencyText} ${verb} left out until conversion is available.`;
}
