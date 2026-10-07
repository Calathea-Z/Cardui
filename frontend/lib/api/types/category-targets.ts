/**
 * One spending category for a month.
 * A null `target` means no target is set. `available` and `remaining` are null in that case.
 * `rolloverIn` is the previous month's remaining when that month turned rollover on. It can be negative.
 * `spent` is posted spending and is never negative.
 * `canSetTarget` is false for spending that has no category.
 */
export type CategoryTargetLineDto = {
  categoryId: string | null;
  name: string;
  color: string | null;
  icon: string | null;
  subGroupName: string;
  target: number | null;
  rollover: boolean;
  rolloverIn: number;
  spent: number;
  available: number | null;
  remaining: number | null;
  canSetTarget: boolean;
};

/**
 * Monthly category targets and how much of each has been spent.
 * `targetTotal` and `remaining` are null when no category has a target.
 * `spent` includes spending with no target. `otherSpent` is that part, and it is not subtracted from `remaining`.
 * `unassignedRolloverCount` is categories that still have last month's leftover or overspend and no target yet.
 * `saved` is false when the amounts are a preview copied from an earlier month and have not been stored.
 * `copiedFromYear` is null when this month was not copied from an earlier month.
 * `throughToday` means the current month's spending stops today.
 */
export type CategoryTargetMonthDto = {
  year: number;
  month: number;
  saved: boolean;
  copiedFromYear: number | null;
  copiedFromMonth: number | null;
  planningCurrency: string;
  periodStart: string;
  periodEnd: string;
  throughToday: boolean;
  todayYear: number;
  todayMonth: number;
  targetTotal: number | null;
  missingTargetCount: number;
  unassignedRolloverCount: number;
  spent: number;
  otherSpent: number;
  remaining: number | null;
  excludedTransactionCount: number;
  excludedCurrencies: string[];
  categories: CategoryTargetLineDto[];
};

/**
 * The year and month a target action applies to.
 */
export type CategoryTargetMonthRequest = {
  year: number;
  month: number;
};

/**
 * A target amount and rollover choice for one category in one month.
 * Amount may be zero. Rollover carries that month's remaining into the next month.
 */
export type UpsertCategoryTargetDto = {
  year: number;
  month: number;
  amount: number;
  rollover: boolean;
};
