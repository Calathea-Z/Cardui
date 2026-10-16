import { browserClient } from "../browser-client";
import type {
  AllocationPageDto,
  UpsertAllocationPreferenceDto,
} from "../types";

/**
 * GET /api/allocation
 * Loads the saved variable-income and windfall allocation preference.
 */
export async function getAllocationPage(): Promise<AllocationPageDto> {
  const response =
    await browserClient.get<AllocationPageDto>("/api/allocation");
  return response.data;
}

/**
 * PUT /api/allocation
 * Stores both allocation rules. No money is moved.
 */
export async function saveAllocationPreference(
  dto: UpsertAllocationPreferenceDto,
): Promise<AllocationPageDto> {
  const response = await browserClient.put<AllocationPageDto>(
    "/api/allocation",
    dto,
  );
  return response.data;
}
