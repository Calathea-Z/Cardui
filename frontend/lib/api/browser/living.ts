import { browserClient } from "../browser-client";
import type { LivingPageDto, UpsertLivingContributionDto } from "../types";

/**
 * GET /api/living
 * Loads the living page again after a save that goes through another route.
 */
export async function getLivingPage(): Promise<LivingPageDto> {
  const response = await browserClient.get<LivingPageDto>("/api/living");
  return response.data;
}

/**
 * PUT /api/living/contributions/{contributorId}
 * Stores how much of that person's pay the shared plan may use in a month.
 * A null amount clears it. The paycheck dates stay.
 */
export async function setLivingContribution(
  contributorId: string,
  dto: UpsertLivingContributionDto,
): Promise<LivingPageDto> {
  const response = await browserClient.put<LivingPageDto>(
    `/api/living/contributions/${contributorId}`,
    dto,
  );
  return response.data;
}
