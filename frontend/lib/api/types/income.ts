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
 * One income source stored for the household.
 * `takeHomeAmount` is the net amount of a single payment, not a monthly equivalent.
 * `currency` is the planning currency when the source was created. An edit does not change it.
 * `contributorId` is null when the source is not assigned to a person.
 */
export type IncomeSourceDto = {
  id: string;
  name: string;
  takeHomeAmount: number;
  currency: string;
  cadence: IncomeCadence;
  nextPaymentDate: string;
  contributorId: string | null;
  contributorName: string | null;
  reliability: IncomeReliability;
};

export type UpsertIncomeSourceDto = {
  name: string;
  takeHomeAmount: number;
  cadence: IncomeCadence;
  nextPaymentDate: string;
  contributorId: string | null;
  reliability: IncomeReliability;
};
