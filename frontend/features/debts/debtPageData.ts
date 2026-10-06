import type {
  AccountDto,
  DebtDto,
  DebtSummaryReportDto,
} from "@/lib/api/types";

/**
 * Data the debts page renders.
 * `planningCurrency` is the household currency used for a new debt.
 * Each debt still displays the currency stored when it was saved.
 * `accounts` are the open household accounts that can be linked to a debt.
 * `summary` is null when that request failed. The debt list can still render.
 */
export type DebtsPageData = {
  debts: DebtDto[];
  summary: DebtSummaryReportDto | null;
  accounts: AccountDto[];
  planningCurrency: string;
};
