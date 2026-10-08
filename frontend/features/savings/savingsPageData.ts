import type { SavingsAccountDto, SavingsGoalDto } from "@/lib/api/types";

/**
 * What the savings page renders.
 * Goals are the stored targets. Accounts are the cash accounts a goal may follow.
 * `planBudgetShortfall` is null when affordability could not be loaded.
 */
export type SavingsPageData = {
  goals: SavingsGoalDto[];
  accounts: SavingsAccountDto[];
  planningCurrency: string;
  planBudgetShortfall: number | null;
};
