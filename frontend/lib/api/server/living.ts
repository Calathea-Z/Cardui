import type { LivingPageDto } from "../types";
import { serverClient } from "../server-client";

/**
 * GET /api/living
 * Loads contribution shares, monthly living spending, and the affordability gap.
 */
export async function getLivingPage(): Promise<LivingPageDto> {
  const response = await serverClient.get<LivingPageDto>("/api/living");
  return response.data;
}
