import type { FinancialProfileDto, HouseholdDto } from "../types";
import { serverClient } from "../server-client";

export async function ensureCurrentHousehold(): Promise<HouseholdDto> {
  const response = await serverClient.post<HouseholdDto>(
    "/api/households/current",
  );
  return response.data;
}

export async function getFinancialProfile(): Promise<FinancialProfileDto> {
  const response = await serverClient.get<FinancialProfileDto>(
    "/api/households/current/profile",
  );
  return response.data;
}
