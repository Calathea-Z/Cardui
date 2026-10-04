import type { TransactionDto } from "./transactions";

export type SpendingByCategoryDto = {
  categoryId: string | null;
  categoryName: string;
  color: string | null;
  amount: number;
};

/**
 * Totals for the dashboard's current period.
 * Excluded counts are accounts or transactions left out because their currency is not the planning currency.
 */
export type DashboardSummaryDto = {
  periodStart: string;
  periodEnd: string;
  cashBalance: number;
  creditCardBalance: number;
  netWorth: number;
  monthlyIncome: number;
  monthlySpending: number;
  planningCurrency: string;
  excludedAccountCount: number;
  excludedTransactionCount: number;
  excludedCurrencies: string[];
  recentTransactions: TransactionDto[];
  spendingByCategory: SpendingByCategoryDto[];
};
