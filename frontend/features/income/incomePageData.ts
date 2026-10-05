import type { HouseholdContributorDto, IncomeSourceDto } from "@/lib/api/types";

/**
 * Data the income page renders.
 * `planningCurrency` is the household currency used for a new source.
 * Each source still displays the currency stored when it was saved.
 * `timeZoneId` is the household zone used to decide when a raise date has arrived.
 */
export type IncomePageData = {
  sources: IncomeSourceDto[];
  contributors: HouseholdContributorDto[];
  planningCurrency: string;
  timeZoneId: string;
};
