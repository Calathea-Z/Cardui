import type { FinancialProfileDto, HouseholdDto } from "../types";
import { serverClient } from "../server-client";

/**
 * POST /api/households/current
 * Creates the signed-in user's household when they do not have one yet.
 */
export async function ensureCurrentHousehold(): Promise<HouseholdDto> {
  const response = await serverClient.post<HouseholdDto>(
    "/api/households/current",
  );
  return response.data;
}

/**
 * GET /api/households/current/profile
 * Loads the planning currency, time zone, and contributors.
 */
export async function getFinancialProfile(): Promise<FinancialProfileDto> {
  const response = await serverClient.get<FinancialProfileDto>(
    "/api/households/current/profile",
  );
  return response.data;
}
