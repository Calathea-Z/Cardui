export type HouseholdDto = {
  id: string;
  displayName: string;
  createdAt: string;
};

/**
 * A person included in the household profile.
 * `isVisible` is the Shown flag. Hiding a contributor leaves balances unchanged.
 */
export type HouseholdContributorDto = {
  id: string;
  name: string;
  isVisible: boolean;
};

export type FinancialProfileDto = {
  planningCurrency: string;
  timeZoneId: string;
  contributors: HouseholdContributorDto[];
};

export type UpdateFinancialProfileDto = {
  planningCurrency: string;
  timeZoneId: string;
};

export type UpsertHouseholdContributorDto = {
  name: string;
  isVisible: boolean;
};
