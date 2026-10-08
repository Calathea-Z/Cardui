/**
 * Which savings row this is.
 * A household has one everyday-spending plan, one cash floor, and one emergency goal. Named goals, stored as Sinking, repeat.
 */
export const savingsGoalKinds = ["Operating", "Floor", "Emergency", "Sinking"] as const;

export type SavingsGoalKind = (typeof savingsGoalKinds)[number];

/**
 * One savings goal.
 * `reservedAmount` is what was typed. Zero means nothing was typed.
 * `amountInUse` is the followed balance, or the typed amount when the goal does not follow one.
 * `accountBalance` is null when no account is followed.
 * `accountUnavailable` means the stored account can no longer be followed, so the typed amount is in use.
 * `negativeBalance` means the followed balance is below zero, so nothing is set aside from it.
 * `amountNeededPerMonth` is the calculated monthly amount for a goal that finishes. Null when that goal is funded, the date has passed, the date is too far out, or the row is everyday spending or cash to keep.
 * `monthlyAmount` and `readyDay` are set for everyday spending. `floorAmount` is the cash to always keep.
 * `targetAmount` and `targetDate` are null when the row does not finish on a date.
 */
export type SavingsGoalDto = {
  id: string;
  kind: SavingsGoalKind;
  name: string;
  targetAmount: number | null;
  targetDate: string | null;
  monthlyAmount: number | null;
  readyDay: number | null;
  floorAmount: number | null;
  reservedAmount: number;
  amountInUse: number;
  currency: string;
  accountId: string | null;
  accountName: string | null;
  accountMask: string | null;
  accountBalance: number | null;
  following: boolean;
  reservedOverridden: boolean;
  accountUnavailable: boolean;
  negativeBalance: boolean;
  remaining: number;
  alreadyMet: boolean;
  datePassed: boolean;
  beyondHorizon: boolean;
  amountNeededPerMonth: number | null;
  finalAmountNeeded: number | null;
};

/**
 * A cash account a goal may follow.
 * `followedByGoalId` is set when another goal already uses this balance.
 * `currency` is null when the account has no currency, which still counts.
 */
export type SavingsAccountDto = {
  id: string;
  name: string;
  mask: string | null;
  balance: number;
  currency: string | null;
  followedByGoalId: string | null;
};

/**
 * The body for creating or updating a goal.
 * `reservedAmount` is available now, or the amount set aside on a goal that finishes. Zero means nothing was typed.
 * `useAccountBalance` replaces that amount with the account balance.
 * `monthlyAmount` and `readyDay` are sent for everyday spending. `floorAmount` is sent for cash to keep.
 */
export type UpsertSavingsGoalDto = {
  kind: SavingsGoalKind;
  name: string | null;
  targetAmount: number | null;
  targetDate: string | null;
  monthlyAmount: number | null;
  readyDay: number | null;
  floorAmount: number | null;
  reservedAmount: number;
  accountId: string | null;
  useAccountBalance: boolean;
};
