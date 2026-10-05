/**
 * How often an income source pays.
 * Biweekly is every two weeks. Semimonthly is twice a month.
 */
export const incomeCadences = [
  "Weekly",
  "Biweekly",
  "Semimonthly",
  "Monthly",
  "Quarterly",
  "Yearly",
  "Irregular",
] as const;

export type IncomeCadence = (typeof incomeCadences)[number];

/**
 * How dependable an income source is.
 * Steady is expected in full. Variable can change. Uncertain may not arrive.
 */
export const incomeReliabilities = ["Steady", "Variable", "Uncertain"] as const;

export type IncomeReliability = (typeof incomeReliabilities)[number];

/**
 * One expected raise stored on an income source.
 * `takeHomeAmount` is the new typical net pay for a single payment from `effectiveDate`.
 * It does not replace the source's current amount.
 */
export type IncomeRaiseDto = {
  id: string;
  effectiveDate: string;
  takeHomeAmount: number;
};

/**
 * One expected raise sent with an income source.
 * The date is a calendar day. The amount is the new typical net pay for one payment.
 */
export type UpsertIncomeRaiseDto = {
  effectiveDate: string;
  takeHomeAmount: number;
};

/**
 * One income source stored for the household.
 * `takeHomeAmount` is the typical net amount of a single payment, not a monthly equivalent.
 * `lowTakeHomeAmount` and `strongTakeHomeAmount` are null when that scenario is not recorded.
 * `currency` is the planning currency when the source was created. An edit does not change it.
 * `contributorId` is null when the source is not assigned to a person.
 * `raises` is ordered by the date each raise starts.
 */
export type IncomeSourceDto = {
  id: string;
  name: string;
  takeHomeAmount: number;
  lowTakeHomeAmount: number | null;
  strongTakeHomeAmount: number | null;
  currency: string;
  cadence: IncomeCadence;
  nextPaymentDate: string;
  contributorId: string | null;
  contributorName: string | null;
  reliability: IncomeReliability;
  raises: IncomeRaiseDto[];
};

export type UpsertIncomeSourceDto = {
  name: string;
  takeHomeAmount: number;
  lowTakeHomeAmount: number | null;
  strongTakeHomeAmount: number | null;
  cadence: IncomeCadence;
  nextPaymentDate: string;
  contributorId: string | null;
  reliability: IncomeReliability;
  raises: UpsertIncomeRaiseDto[];
};
