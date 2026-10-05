/**
 * How often a bill is due.
 * Biweekly is every two weeks. Semimonthly is twice a month.
 */
export const obligationCadences = [
  "Weekly",
  "Biweekly",
  "Semimonthly",
  "Monthly",
  "Quarterly",
  "Yearly",
  "Irregular",
] as const;

export type ObligationCadence = (typeof obligationCadences)[number];

/**
 * Whether a bill has to be paid.
 * Essential has to be paid. Flexible can be reduced or skipped.
 */
export const obligationFlexibilities = ["Essential", "Flexible"] as const;

export type ObligationFlexibility = (typeof obligationFlexibilities)[number];

/**
 * One bill stored for the household.
 * `amount` is one payment, not a monthly equivalent.
 * `currency` is the planning currency when the bill was created. An edit does not change it.
 * `accountId` is null when the bill is not paid from a household account.
 */
export type ObligationDto = {
  id: string;
  name: string;
  amount: number;
  currency: string;
  cadence: ObligationCadence;
  nextDueDate: string;
  accountId: string | null;
  accountName: string | null;
  flexibility: ObligationFlexibility;
};

export type UpsertObligationDto = {
  name: string;
  amount: number;
  cadence: ObligationCadence;
  nextDueDate: string;
  accountId: string | null;
  flexibility: ObligationFlexibility;
};
