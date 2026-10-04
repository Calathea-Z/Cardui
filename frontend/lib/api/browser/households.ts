import { browserClient } from "../browser-client";
import type {
  FinancialProfileDto,
  HouseholdContributorDto,
  UpdateFinancialProfileDto,
  UpsertHouseholdContributorDto,
} from "../types";

export async function updateFinancialProfile(
  dto: UpdateFinancialProfileDto,
): Promise<FinancialProfileDto> {
  const response = await browserClient.put<FinancialProfileDto>(
    "/api/households/current/profile",
    dto,
  );
  return response.data;
}

export async function addHouseholdContributor(
  dto: UpsertHouseholdContributorDto,
): Promise<HouseholdContributorDto> {
  const response = await browserClient.post<HouseholdContributorDto>(
    "/api/households/current/contributors",
    dto,
  );
  return response.data;
}

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

export async function removeHouseholdContributor(id: string): Promise<void> {
  await browserClient.delete(`/api/households/current/contributors/${id}`);
}
