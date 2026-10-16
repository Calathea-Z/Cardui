import type { DebtKind } from "./debts";
import type { DebtScheduleStop } from "./plan";

/**
 * The kind of offer compared with debts the household already has.
 * A consolidation loan replaces two or more debts. A replacement loan replaces one.
 * A balance transfer replaces revolving debts only.
 */
export const restructuringOfferKinds = [
  "ConsolidationLoan",
  "BalanceTransfer",
  "ReplacementLoan",
] as const;

export type RestructuringOfferKind = (typeof restructuringOfferKinds)[number];

/**
 * Where the offer's fee is paid.
 * Financed adds it to the new balance. Cash pays it from cash.
 */
export const restructuringFeeFundings = ["Financed", "Cash"] as const;

export type RestructuringFeeFunding = (typeof restructuringFeeFundings)[number];

/**
 * Whether the offer's running cost stays at or below keeping the debts.
 * Reached names a date that holds through both payoffs. Never means the offer stays more expensive.
 * Unknown means a path does not pay off.
 */
export const restructuringBreakEvens = ["Reached", "Never", "Unknown"] as const;

export type RestructuringBreakEven = (typeof restructuringBreakEvens)[number];

/**
 * Terms sent to POST /api/plan/restructuring. Nothing is saved.
 */
export type CompareRestructuringDto = {
  kind: RestructuringOfferKind;
  debtIds: string[];
  apr: number;
  termMonths: number;
  fee: number;
  feeFunding: RestructuringFeeFunding;
  upfrontCash: number;
};

/**
 * One replaced debt on its current minimum.
 * `apr` is the stored rate. `paidOffOn` is null when `stop` is not PaidOff.
 */
export type RestructuringDebtOutcomeDto = {
  debtId: string;
  name: string;
  kind: DebtKind;
  balance: number;
  apr: number | null;
  promotionalApr: number | null;
  promotionalEndsOn: string | null;
  stop: DebtScheduleStop;
  paidOffOn: string | null;
};

/**
 * Cash due now compared with cash on hand, the reserve, and cash to keep.
 * This is not an investment return. `leavesLessThanCashToKeep` is false when no floor is saved.
 */
export type RestructuringCashEffectDto = {
  cashOnHand: number;
  reserve: number;
  cashToKeep: number;
  hasCashFloor: boolean;
  exceedsCashOnHand: boolean;
  usesReserve: boolean;
  leavesLessThanCashToKeep: boolean;
};

/**
 * One offer compared with keeping the selected debts on their current minimums.
 * `monthlyCashFlow` is those minimums minus the offer payment. Positive means more cash left each month.
 * It is not a rank. A total is null when that path does not pay off.
 * `cashDueNow` is a fee paid in cash plus upfront cash. A fee added to the balance is not included.
 * `offerPaymentCount` is null when the offer does not pay off, and rounding can make it longer than `termMonths`.
 */
export type RestructuringComparisonDto = {
  kind: RestructuringOfferKind;
  debts: RestructuringDebtOutcomeDto[];
  coveredBalance: number;
  fee: number;
  feeFunding: RestructuringFeeFunding;
  upfrontCash: number;
  cashDueNow: number;
  newPrincipal: number;
  offerApr: number;
  termMonths: number;
  offerPaymentCount: number | null;
  offerMonthlyPayment: number;
  keepMonthlyPayment: number | null;
  monthlyCashFlow: number | null;
  keepTotalInterest: number | null;
  offerTotalInterest: number | null;
  keepTotalCost: number | null;
  offerTotalCost: number | null;
  keepPaidOffOn: string | null;
  offerPaidOffOn: string | null;
  offerStop: DebtScheduleStop;
  breakEven: RestructuringBreakEven;
  breakEvenOn: string | null;
  cash: RestructuringCashEffectDto;
};
