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
 * Cash at the end of one day. Negative `cash` is a shortfall.
 * `income`, `bills`, `debtPayments`, and `livingSpending` are that day's totals, each zero or more.
 * Living spending leaves cash and is not a bill.
 */
export type PlanCashDayDto = {
  date: string;
  cash: number;
  income: number;
  bills: number;
  debtPayments: number;
  livingSpending: number;
};

/**
 * Cash across a stretch of days, both ends included.
 * `lowestCashOn` is the first day that reaches `lowestCash`. `cashShortfall` is true when any day ends below zero.
 */
export type PlanCashWindowDto = {
  from: string;
  through: string;
  endingCash: number;
  lowestCash: number;
  lowestCashOn: string;
  cashShortfall: boolean;
};

/**
 * Cash at 6, 12, or 18 months.
 * `minimumObligation` is the known monthly minimums still due then, and null when every remaining debt is missing a term.
 * `unknownMinimumCount` is how many remaining debts that sum leaves out.
 */
export type PlanCashHorizonDto = {
  months: number;
  window: PlanCashWindowDto;
  minimumObligation: number | null;
  unknownMinimumCount: number;
};

/**
 * One cash forecast: the first 30 days, that 30-day window, and the horizons in month order.
 * `shortfallOn` is the first day cash is below zero inside 18 months, and `recoveredOn` the first later day back at zero or above.
 * `reserveShortfallOn` is the first day what is left after the reserve is below zero. Each is null when it does not happen.
 */
export type PlanCashForecastDto = {
  days: PlanCashDayDto[];
  dayView: PlanCashWindowDto;
  horizons: PlanCashHorizonDto[];
  shortfallOn: string | null;
  recoveredOn: string | null;
  reserveShortfallOn: string | null;
  reserveRestoredOn: string | null;
};

/**
 * The cash forecast for one payoff path.
 * `lowPay` uses low pay where recorded and leaves out raises. It is null when no income source has a low amount.
 */
export type PlanCashOutlookPathDto = {
  typical: PlanCashForecastDto;
  lowPay: PlanCashForecastDto | null;
};

/**
 * The cash outlook on both payoff paths.
 * `startingCash` is the Cash total on Accounts on `asOf`, before that day's payments.
 * `startingReserve` is the amount already set aside. `startingAvailable` is cash minus that reserve and can be negative.
 * `hasIncome` and `hasBills` are true when at least one counts in the planning currency.
 * `excludedCurrencies` are income, bill, and debt codes left out.
 */
export type PlanCashOutlookDto = {
  asOf: string;
  startingCash: number;
  startingReserve: number;
  startingAvailable: number;
  hasIncome: boolean;
  hasBills: boolean;
  excludedCurrencies: string[];
  rollover: PlanCashOutlookPathDto;
  reclaimAll: PlanCashOutlookPathDto;
};

/**
 * The household's payoff on rollover and on keeping every freed payment, and the cash outlook on each.
 * `excludedCurrencies` are debt codes left out of the planning currency.
 * `hasDebts` is false when no debt is recorded. A debt with no balance still counts and is listed in `missingBalance`.
 * `monthlyExtra` is shared extra tried for this response. Zero is minimums only, and the amount is not saved.
 * `livingSpendingMonthly` is the one monthly flexible-spending amount from Living.
 */
export type PlanRecoveryDto = {
  planningCurrency: string;
  rollover: PlanRecoveryPathDto;
  reclaimAll: PlanRecoveryPathDto;
  excludedCurrencies: string[];
  missingBalance: PlanMissingBalanceDto[];
  hasDebts: boolean;
  cashOutlook: PlanCashOutlookDto;
  monthlyExtra: number;
  livingSpendingMonthly: number;
};
