import { browserClient } from "../browser-client";
import type {
  ObligationDto,
  ObligationSuggestionDto,
  UpsertObligationDto,
} from "../types";

/**
 * POST /api/obligations
 * Records a bill for the signed-in household.
 * The amount is one payment. The source account is omitted when the bill is not tied to one.
 */
export async function createObligation(
  dto: UpsertObligationDto,
): Promise<ObligationDto> {
  const response = await browserClient.post<ObligationDto>(
    "/api/obligations",
    dto,
  );
  return response.data;
}

/**
 * PUT /api/obligations/{id}
 * Updates a bill. The stored currency stays.
 */
export async function updateObligation(
  id: string,
  dto: UpsertObligationDto,
): Promise<ObligationDto> {
  const response = await browserClient.put<ObligationDto>(
    `/api/obligations/${id}`,
    dto,
  );
  return response.data;
}

/**
 * DELETE /api/obligations/{id}
 * Deletes a bill. The source account and its balance stay unchanged.
 */
export async function deleteObligation(id: string): Promise<void> {
  await browserClient.delete(`/api/obligations/${id}`);
}

/**
 * GET /api/obligations/suggestions
 * Loads recurring payments noticed in activity. They are not bills.
 */
export async function getObligationSuggestions(): Promise<
  ObligationSuggestionDto[]
> {
  const response = await browserClient.get<ObligationSuggestionDto[]>(
    "/api/obligations/suggestions",
  );
  return response.data;
}

/**
 * POST /api/obligations/suggestions/dismiss
 * Leaves a suggested payment out of bills. The pattern is not suggested again.
 */
export async function dismissObligationSuggestion(key: string): Promise<void> {
  await browserClient.post("/api/obligations/suggestions/dismiss", { key });
}
