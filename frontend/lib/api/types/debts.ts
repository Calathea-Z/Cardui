/**
 * Whether a debt revolves or is paid down on a set term.
 * Revolving is a card or line of credit. Installment is a loan with a remaining term.
 */
export const debtKinds = ["Revolving", "Installment"] as const;

export type DebtKind = (typeof debtKinds)[number];

/**
 * One debt stored for the household.
 * A null balance, APR, minimum, due date, limit, term, or promotion is unknown.
 * Zero is a known zero, not a stand-in for unknown.
 * `currency` is the planning currency when the debt was created. An edit does not change it.
 * `accountId` is null when the debt is not linked to a household account.
 * `creditLimit` is set only for a revolving debt. `remainingTermMonths` is set only for an installment debt.
 * `utilization` is the share of the credit limit in use, as a ratio. 0.85 means 85 percent.
 * It is null when the balance or the credit limit is unknown, and it is not stored.
 */
export type DebtDto = {
  id: string;
  name: string;
  kind: DebtKind;
  accountId: string | null;
  accountName: string | null;
  balance: number | null;
  balanceAsOf: string | null;
  currency: string;
  apr: number | null;
  minimumPayment: number | null;
  nextDueDate: string | null;
  creditLimit: number | null;
  remainingTermMonths: number | null;
  promotionalApr: number | null;
  promotionalEndsOn: string | null;
  utilization: number | null;
};

/**
 * Fields saved for a debt.
 * A null term is stored as unknown. The linked account balance is not changed.
 */
export type UpsertDebtDto = {
  name: string;
  kind: DebtKind;
  accountId: string | null;
  balance: number | null;
  balanceAsOf: string | null;
  apr: number | null;
  minimumPayment: number | null;
  nextDueDate: string | null;
  creditLimit: number | null;
  remainingTermMonths: number | null;
  promotionalApr: number | null;
  promotionalEndsOn: string | null;
};
