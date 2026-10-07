/**
 * Whether a debt revolves or is paid down on a set term.
 * Revolving is a card or line of credit. Installment is a loan with a remaining term.
 */
export const debtKinds = ["Revolving", "Installment"] as const;

export type DebtKind = (typeof debtKinds)[number];

/**
 * Where the balance in use came from.
 * Synced is the connection. Override is a value the person kept. Manual is the debt's own balance.
 */
export const debtFieldSources = ["Synced", "Override", "Manual"] as const;

export type DebtFieldSource = (typeof debtFieldSources)[number];

/**
 * How current a followed connection is.
 * Current means the latest snapshot is recent and the last sync succeeded.
 */
export const debtLinkFreshness = [
  "Current",
  "Stale",
  "SyncFailing",
  "Disconnected",
  "AccountMissing",
] as const;

export type DebtLinkFreshness = (typeof debtLinkFreshness)[number];

/**
 * One debt stored for the household.
 * A null balance, APR, minimum, due date, limit, term, or promotion is unknown.
 * Zero is a known zero, not a stand-in for unknown.
 * `currency` is the planning currency when the debt was created. An edit does not change it.
 * `accountId` is null when the debt is not linked to a household account.
 * `following` is true when the debt uses the connected account's balance. A reference link stays false.
 * `balance` is the amount stored on the debt. `balanceInUse` is the amount the plan uses.
 * While following without an override, `balanceInUse` is the connected balance and `balance` stays what the person last stored.
 * `balanceCredit` is the positive credit counted as zero. It is null when the balance is not a credit.
 * `freshness` is null when the debt is not following. `syncFailedOn` is set only when freshness is SyncFailing.
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
  following: boolean;
  balance: number | null;
  balanceAsOf: string | null;
  balanceInUse: number | null;
  balanceInUseAsOf: string | null;
  balanceSource: DebtFieldSource;
  syncedBalance: number | null;
  syncedBalanceAsOf: string | null;
  syncedBalanceBlock: DebtAccountBalanceBlock;
  balanceCredit: number | null;
  freshness: DebtLinkFreshness | null;
  syncFailedOn: string | null;
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
  /**
   * Followed debts in this currency whose connection is not current.
   * Their balance is still included in the totals.
   */
  staleCount: number;
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

/**
 * A connected account a debt is allowed to follow.
 * `balanceInUse` is the amount following would use before the person keeps their own.
 * `balancesDiffer` is true when the debt already has a different balance.
 * `balanceCredit` is the positive credit counted as zero. It is null when the balance is not a credit.
 */
export type DebtFollowAccountDto = {
  accountId: string;
  name: string;
  mask: string | null;
  syncedBalance: number | null;
  syncedBalanceAsOf: string | null;
  balanceInUse: number | null;
  balanceInUseAsOf: string | null;
  block: DebtAccountBalanceBlock;
  balanceCredit: number | null;
  balancesDiffer: boolean;
};

/**
 * The account to follow, and whether the person's different balance stays as their own value.
 * `keepOwnBalance` applies only when the amounts differ.
 */
export type FollowDebtAccountDto = {
  accountId: string;
  keepOwnBalance: boolean;
};
