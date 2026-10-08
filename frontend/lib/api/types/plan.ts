/**
 * Which way a path treats cash freed by a paid-off debt.
 * Rollover sends it to the next debt. ReclaimAll keeps every freed dollar.
 */
export type PayoffRolloverKind = "Rollover" | "ReclaimAll";

/**
 * Why the projection stopped for one debt.
 * Anything but `PaidOff` blocks the plan and is listed under Finish your plan.
 * `HorizonReached` means the payoff falls past the 50-year limit.
 */
export type DebtScheduleStop =
  | "PaidOff"
  | "DoesNotPayDown"
  | "RateUnknown"
  | "MinimumUnknown"
  | "DueDateUnknown"
  | "HorizonReached";

/**
 * One payoff that removes a monthly obligation.
 * `endedOn` is the payment that clears the balance. `startsOn` is the date the minimum is no longer paid, and null when that date falls outside the projection.
 * `breathingRoom` is the recurring freed cash after this step. It does not include shared extra.
 */
export type PlanRecoveryStepDto = {
  debtId: string;
  name: string;
  endedOn: string;
  startsOn: string | null;
  minimum: number;
  breathingRoom: number;
};

/**
 * How one debt fares on one path.
 * `balance` is the opening balance. `minimum` is null when a rate, minimum, or due date is missing.
 * `lastMonthInterest` and `lastMonthPayment` are the last modeled month; for a debt that does not pay down, the month the payment fell short. Both are null when no payment could be modeled.
 */
export type PlanDebtOutcomeDto = {
  debtId: string;
  name: string;
  stop: DebtScheduleStop;
  balance: number;
  minimum: number | null;
  paidOffOn: string | null;
  lastMonthInterest: number | null;
  lastMonthPayment: number | null;
};

/**
 * One debt's balance right after one payment. Zero once it is paid off.
 */
export type PlanBalancePointDto = {
  debtId: string;
  dueDate: string;
  balance: number;
};

/**
 * One path from today to the last payoff.
 * `startingObligation` is the known minimums before any payoff, and null when every minimum is unknown.
 * `remainingObligation` is the known minimums still due after the last payoff.
 * `paidOffOn` is null while any debt in the plan is still open or cannot be calculated.
 * `debts` is in the order rolled cash follows, which also fixes each debt's chart color.
 * `balancePoints` is sorted by due date, then in the order of `debts`.
 */
export type PlanRecoveryPathDto = {
  kind: PayoffRolloverKind;
  steps: PlanRecoveryStepDto[];
  startingObligation: number | null;
  remainingObligation: number | null;
  recurringRoom: number;
  paidOffOn: string | null;
  totalInterest: number;
  debts: PlanDebtOutcomeDto[];
  balancePoints: PlanBalancePointDto[];
};

/**
 * A debt left out of the plan because its balance is unknown.
 */
export type PlanMissingBalanceDto = {
  debtId: string;
  name: string;
};

/**
 * The household's payoff on rollover and on keeping every freed payment.
 * `excludedCurrencies` are codes left out of the planning currency.
 * `hasDebts` is false when no debt is recorded. A debt with no balance still counts and is listed in `missingBalance`.
 */
export type PlanRecoveryDto = {
  planningCurrency: string;
  rollover: PlanRecoveryPathDto;
  reclaimAll: PlanRecoveryPathDto;
  excludedCurrencies: string[];
  missingBalance: PlanMissingBalanceDto[];
  hasDebts: boolean;
};
