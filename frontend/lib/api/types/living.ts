import type { SavingsAccountDto, SavingsGoalDto } from "./savings";

/**
 * How a person's monthly amount relates to their recorded pay.
 * FullPay means the amount is blank. Shared means the plan uses only that amount.
 * AllRecordedPay means the amount covers their scheduled pay. Unplaced means there is no scheduled paycheck to spread it across.
 */
export const contributionLimits = [
  "FullPay",
  "Shared",
  "AllRecordedPay",
  "Unplaced",
] as const;

export type ContributionLimit = (typeof contributionLimits)[number];

/**
 * One person's current monthly contribution benchmark.
 * `monthlyAmount` is null when all recorded pay is shared. Zero shares none; low pay and raises keep the derived share.
 * `recordedMonthly` is null when they have no scheduled pay in the planning currency. It is an average, not cash on a date.
 * `hasUnscheduledPay` means a paycheck with no schedule still arrives on its own date.
 */
export type LivingContributionDto = {
  contributorId: string;
  name: string;
  monthlyAmount: number | null;
  recordedMonthly: number | null;
  sharedMonthly: number;
  keptMonthly: number;
  limit: ContributionLimit;
  hasUnscheduledPay: boolean;
};

/**
 * One person's monthly contribution.
 * `monthlyAmount` null clears it so their full recorded pay stays shared.
 */
export type UpsertLivingContributionDto = {
  monthlyAmount: number | null;
};

/**
 * The monthly picture of shared pay against bills, minimums, and living spending.
 * `shortfall` is zero when those amounts are covered. `leftOut` names amounts that are not in the totals.
 * The figures are averages, not cash on a date.
 */
export type LivingGapDto = {
  sharedMonthly: number;
  billsMonthly: number;
  minimumsMonthly: number;
  livingSpendingMonthly: number;
  shortfall: number;
  leftOut: string[];
};

/**
 * The Living page.
 * `unassignedIncome` names paychecks with no person. Those stay fully shared.
 * `livingSpending` is null until it is set. `accounts` are the cash accounts it may follow.
 */
export type LivingPageDto = {
  planningCurrency: string;
  contributions: LivingContributionDto[];
  unassignedIncome: string[];
  livingSpending: SavingsGoalDto | null;
  accounts: SavingsAccountDto[];
  gap: LivingGapDto;
};
