import type { HouseholdDto } from "../types";
import { serverClient } from "../server-client";

export async function ensureCurrentHousehold(): Promise<HouseholdDto> {
  const response = await serverClient.post<HouseholdDto>(
    "/api/households/current",
  );
  return response.data;
}
