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
 * A fact the summary view needs and does not have.
 * A known zero is not one of these.
 */
export const debtSummaryGaps = [
  "Balance",
  "Apr",
  "MinimumPayment",
  "DueDate",
  "CreditLimit",
  "RemainingTerm",
  "PromotionalEnd",
  "PromotionalRate",
  "RateAfterPromotion",
] as const;

export type DebtSummaryGap = (typeof debtSummaryGaps)[number];

/**
 * Why a linked account balance is shown and not copied onto the debt.
 * None means the person can choose that balance. The plan does not copy it on its own.
 */
export const debtAccountBalanceBlocks = [
  "None",
  "DateUnknown",
  "NegativeBalance",
  "CurrencyDiffers",
  "AmountTooLarge",
] as const;

export type DebtAccountBalanceBlock = (typeof debtAccountBalanceBlocks)[number];

/**
 * A linked card or loan balance that is not the same amount as the debt.
 * The plan keeps the debt's dated balance until the person chooses this one.
 * `canUseAccountBalance` is false when the account balance has no date, a different currency, or an amount the debt cannot store.
 */
export type DebtBalanceComparisonDto = {
  accountBalance: number;
  accountBalanceAsOf: string | null;
  accountCurrency: string | null;
  canUseAccountBalance: boolean;
  block: DebtAccountBalanceBlock;
};

/**
 * Summary of one debt. There is no score.
 * `monthlyInterest` is null when the balance or the rate in effect is unknown.
 * It is one month on the recorded balance, not the total interest left to pay.
 * `rateIsPromotional` means that estimate uses the promotional APR.
 * `dueDatePassed` means the recorded due date is before today. It does not say the payment was missed.
 * `balanceComparison` is null when the linked account balance matches, or the account is not an amount owed.
 */
export type DebtSummaryItemDto = {
  debtId: string;
  monthlyInterest: number | null;
  rateInEffect: number | null;
  rateIsPromotional: boolean;
  promotionEnded: boolean;
  promotionalEndsOn: string | null;
  promoEndsWithinNotice: boolean;
  dueDatePassed: boolean;
  aprReachesNotice: boolean;
  utilizationReachesNotice: boolean;
  utilizationReachesLimitNotice: boolean;
  gaps: DebtSummaryGap[];
  balanceComparison: DebtBalanceComparisonDto | null;
};

/**
 * Totals for debts that share one currency.
 * A null total means every amount in that total is unknown. A known zero stays zero.
 */
export type DebtCurrencySummaryDto = {
  currency: string;
  debtCount: number;
  recordedBalance: number | null;
  unknownBalanceCount: number;
  monthlyInterest: number | null;
  unknownInterestCount: number;
  minimumPayments: number | null;
  unknownMinimumCount: number;
  utilization: number | null;
  utilizationDebtCount: number;
  unknownUtilizationCount: number;
  dueDatePassedCount: number;
  promoEndingCount: number;
  promotionEndedCount: number;
  aprNoticeCount: number;
  utilizationNoticeCount: number;
  utilizationLimitNoticeCount: number;
  balanceDifferenceCount: number;
  missingDueDateCount: number;
  missingRemainingTermCount: number;
  missingPromotionalEndCount: number;
  missingPromotionalRateCount: number;
  missingRateAfterPromotionCount: number;
};

/**
 * Inventory summary for the household's debts.
 * The notice numbers are the thresholds the screen names. They are not a score.
 * `aprNoticePercent` is a percent. 20 means 20 percent.
 * `utilizationNotice` and `utilizationLimitNotice` are ratios. 0.30 means 30 percent.
 */
export type DebtSummaryReportDto = {
  aprNoticePercent: number;
  utilizationNotice: number;
  utilizationLimitNotice: number;
  promotionalNoticeDays: number;
  currencies: DebtCurrencySummaryDto[];
  debts: DebtSummaryItemDto[];
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
