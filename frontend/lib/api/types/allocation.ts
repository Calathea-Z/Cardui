/**
 * One saved split.
 * `cashPercent` is what stays available after debt and savings.
 */
export type AllocationRuleDto = {
  debtPercent: number;
  savingsPercent: number;
  cashPercent: number;
};

/**
 * One variable source prepared for review.
 * `surplus` is null when the source is left out.
 * `leftOut` is null when `surplus` is set. The other values are
 * "Another currency" and "No strong amount above typical".
 */
export type VariableIncomeReviewDto = {
  name: string;
  currency: string;
  surplus: number | null;
  leftOut: string | null;
};

/**
 * The household allocation page.
 * `isSaved` is false when no row is stored. The percents are then zero.
 */
export type AllocationPageDto = {
  planningCurrency: string;
  isSaved: boolean;
  variableIncome: AllocationRuleDto;
  windfall: AllocationRuleDto;
  variableIncomeReviews: VariableIncomeReviewDto[];
};

export type UpsertAllocationPreferenceDto = {
  variableIncomeDebtPercent: number;
  variableIncomeSavingsPercent: number;
  windfallDebtPercent: number;
  windfallSavingsPercent: number;
};
