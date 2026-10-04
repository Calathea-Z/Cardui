import { browserClient } from "../browser-client";
import type {
  FinancialProfileDto,
  HouseholdContributorDto,
  UpdateFinancialProfileDto,
  UpsertHouseholdContributorDto,
} from "../types";

/**
 * PUT /api/households/current/profile
 * Saves the household planning currency, time zone, and contributor settings.
 */
export async function updateFinancialProfile(
  dto: UpdateFinancialProfileDto,
): Promise<FinancialProfileDto> {
  const response = await browserClient.put<FinancialProfileDto>(
    "/api/households/current/profile",
    dto,
  );
  return response.data;
}

/**
 * POST /api/households/current/contributors
 * Adds a person whose income is part of the household profile.
 */
export async function addHouseholdContributor(
  dto: UpsertHouseholdContributorDto,
): Promise<HouseholdContributorDto> {
  const response = await browserClient.post<HouseholdContributorDto>(
    "/api/households/current/contributors",
    dto,
  );
  return response.data;
}

/**
 * PUT /api/households/current/contributors/{id}
 * Updates one household contributor.
 */
export async function updateHouseholdContributor(
  id: string,
  dto: UpsertHouseholdContributorDto,
): Promise<HouseholdContributorDto> {
  const response = await browserClient.put<HouseholdContributorDto>(
    `/api/households/current/contributors/${id}`,
    dto,
  );
  return response.data;
}

/**
 * DELETE /api/households/current/contributors/{id}
 * Removes a contributor from the household profile.
 */
export async function removeHouseholdContributor(id: string): Promise<void> {
  await browserClient.delete(`/api/households/current/contributors/${id}`);
}
