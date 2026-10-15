import type { AllocationPageDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/allocation
 * Loads the saved variable-income and windfall allocation preference.
 */
export async function getAllocationPage(): Promise<AllocationPageDto> {
  const response = await serverClient.get<AllocationPageDto>("/api/allocation");
  return response.data;
}
