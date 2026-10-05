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

/**
 * A recurring payment noticed in activity.
 * It is not a bill. `amount` is one typical payment.
 * `accountId` is null when the charges did not all leave one account.
 * `key` is the normalized merchant text. Saving a bill with that key keeps the pattern from being suggested again.
 */
export type ObligationSuggestionDto = {
  key: string;
  name: string;
  amount: number;
  currency: string;
  cadence: ObligationCadence;
  nextDueDate: string;
  accountId: string | null;
  accountName: string | null;
};

/**
 * Fields saved for a bill.
 * `suggestionKey` is set when the bill was added from a suggestion.
 * It is null for a bill entered by hand, and an edit does not change the stored key.
 */
export type UpsertObligationDto = {
  name: string;
  amount: number;
  cadence: ObligationCadence;
  nextDueDate: string;
  accountId: string | null;
  flexibility: ObligationFlexibility;
  suggestionKey: string | null;
};
