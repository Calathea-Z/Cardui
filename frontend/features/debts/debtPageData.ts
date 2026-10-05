import type { AccountDto, DebtDto } from "@/lib/api/types";

/**
 * Data the debts page renders.
 * `planningCurrency` is the household currency used for a new debt.
 * Each debt still displays the currency stored when it was saved.
 * `accounts` are the open household accounts that can be linked to a debt.
 */
export type DebtsPageData = {
  debts: DebtDto[];
  accounts: AccountDto[];
  planningCurrency: string;
};
